using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using Fouroam.Shared;

namespace Fouroam.Client.Test;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        if (args.Contains("--help") || args.Contains("-h"))
        {
            PrintUsage();
            return 0;
        }

        string host = "127.0.0.1";
        int port = 7777;
        string name = "Player";
        string? fingerprint = null;
        string defaultIdentityRoot = Environment.GetFolderPath(
            Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrWhiteSpace(defaultIdentityRoot))
            defaultIdentityRoot = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string identityFile = Path.Combine(defaultIdentityRoot, "Fouroam", "identity.key");

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--host" when i + 1 < args.Length:
                    host = args[++i];
                    break;
                case "--port" when i + 1 < args.Length:
                    if (!int.TryParse(args[++i], out port) || port is < 1 or > 65535)
                    {
                        Console.Error.WriteLine("Invalid port.");
                        return 2;
                    }
                    break;
                case "--name" when i + 1 < args.Length:
                    name = args[++i];
                    break;
                case "--fingerprint" when i + 1 < args.Length:
                    if (!TlsFingerprint.TryNormalize(args[++i], out fingerprint))
                    {
                        Console.Error.WriteLine("Fingerprint must be 64 hexadecimal characters.");
                        return 2;
                    }
                    break;
                case "--identity-file" when i + 1 < args.Length:
                    identityFile = args[++i];
                    break;
                default:
                    Console.Error.WriteLine($"Unknown or incomplete option: {args[i]}");
                    PrintUsage();
                    return 2;
            }
        }

        if (fingerprint is null)
        {
            Console.Error.WriteLine("TLS fingerprint is required. Get it from the host through a trusted channel.");
            PrintUsage();
            return 2;
        }
        if (Console.IsInputRedirected)
        {
            Console.Error.WriteLine("The test client requires an interactive terminal to enter the password.");
            return 2;
        }

        using var tcp = new TcpClient { NoDelay = true };
        using var shutdown = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            shutdown.Cancel();
        };

        try
        {
            string identity = ClientIdentity.LoadOrCreate(identityFile);
            using (var connectTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(10)))
                await tcp.ConnectAsync(host, port, connectTimeout.Token);

            using var tls = new SslStream(tcp.GetStream(), false,
                (_, certificate, _, _) => TlsFingerprint.Matches(certificate, fingerprint));

            using (var tlsTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(10)))
                await tls.AuthenticateAsClientAsync(new SslClientAuthenticationOptions
                {
                    TargetHost = "Fouroam.Private",
                    EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
                    CertificateRevocationCheckMode =
                        System.Security.Cryptography.X509Certificates.X509RevocationMode.NoCheck
                }, tlsTimeout.Token);

            Console.Write("Session password (hidden): ");
            string password = ReadSecret();

            using var wire = new JsonLineConnection(tls);
            await wire.SendAsync(new WireMessage
            {
                Type = MessageTypes.Hello,
                ProtocolVersion = JsonLineConnection.ProtocolVersion,
                Name = name,
                Password = password,
                IdentityToken = identity
            }, shutdown.Token);

            var response = await wire.ReceiveAsync(shutdown.Token);
            if (response?.Type != MessageTypes.Welcome)
            {
                Console.Error.WriteLine(
                    $"Connection rejected: {response?.Text ?? "server disconnected"}");
                return 1;
            }

            Console.WriteLine($"[CONNECTED] {response.SessionName} | ID #{response.PlayerId}, players: {response.PlayerCount}/{response.MaxPlayers}");
            PrintRoster(response);
            Console.WriteLine("Commands: /players, /ping, /quit. Other text sends chat.");

            var receiveTask = ReceiveLoopAsync(wire, shutdown.Token);
            var inputTask = InputLoopAsync(wire, shutdown.Token);
            await Task.WhenAny(receiveTask, inputTask);

            shutdown.Cancel();
            tcp.Close();
            try { await receiveTask; }
            catch (Exception ex) when (ex is OperationCanceledException or
                ObjectDisposedException or IOException) { }

            // Console input can remain blocked on certain terminal implementations.
            // Do not hold process exit waiting for an input read to be cancelled.
            Console.WriteLine("[DISCONNECTED]");
            return 0;
        }
        catch (Exception ex) when (ex is SocketException or IOException or
            InvalidDataException or OperationCanceledException or AuthenticationException or
            UnauthorizedAccessException or ArgumentException)
        {
            Console.Error.WriteLine($"[ERROR] {ex.Message}");
            return 1;
        }
    }

    private static string ReadSecret()
    {
        var characters = new List<char>();
        while (true)
        {
            var key = Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Enter)
            {
                Console.WriteLine();
                return new string(characters.ToArray());
            }
            if (key.Key == ConsoleKey.Backspace)
            {
                if (characters.Count > 0) characters.RemoveAt(characters.Count - 1);
            }
            else if (!char.IsControl(key.KeyChar) && characters.Count < 128)
                characters.Add(key.KeyChar);
        }
    }

    private static async Task ReceiveLoopAsync(
        JsonLineConnection wire, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var message = await wire.ReceiveAsync(cancellationToken);
            if (message is null) return;
            switch (message.Type)
            {
                case MessageTypes.PlayerJoined:
                    Console.WriteLine($"[JOIN] #{message.PlayerId} {message.Name}");
                    break;
                case MessageTypes.PlayerLeft:
                    Console.WriteLine($"[LEAVE] #{message.PlayerId} {message.Name}");
                    break;
                case MessageTypes.Players:
                    PrintRoster(message);
                    break;
                case MessageTypes.Chat:
                    Console.WriteLine($"[{message.Name} #{message.PlayerId}] {message.Text}");
                    break;
                case MessageTypes.Announcement:
                    Console.WriteLine($"[ADMIN] {message.Text}");
                    break;
                case MessageTypes.Kicked:
                    Console.WriteLine($"[KICKED] {message.Text}");
                    return;
                case MessageTypes.Pong:
                    if (message.Timestamp is long sent)
                        Console.WriteLine($"[PING] {DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - sent} ms");
                    break;
                case MessageTypes.Error:
                case MessageTypes.ServerFull:
                    Console.WriteLine($"[SERVER] {message.Text}");
                    break;
                default:
                    Console.WriteLine($"[NETWORK] {message.Type}");
                    break;
            }
        }
    }

    private static async Task InputLoopAsync(
        JsonLineConnection wire, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var line = await Console.In.ReadLineAsync(cancellationToken);
            if (line is null || line.Trim() == "/quit") return;

            WireMessage? message = line.Trim() switch
            {
                "/players" => new WireMessage { Type = MessageTypes.PlayersRequest },
                "/ping" => new WireMessage
                {
                    Type = MessageTypes.Ping,
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                },
                "" => null,
                _ => new WireMessage { Type = MessageTypes.Chat, Text = line }
            };
            if (message is not null)
                await wire.SendAsync(message, cancellationToken);
        }
    }

    private static void PrintRoster(WireMessage message)
    {
        Console.WriteLine($"[PLAYERS] {message.PlayerCount}/{message.MaxPlayers}");
        foreach (var player in message.Players ?? [])
            Console.WriteLine($"  #{player.Id}: {player.Name}");
    }

    private static void PrintUsage()
    {
        Console.WriteLine("Fouroam v0.2.1-dev CLI test client (not integrated with GTA V).");
        Console.WriteLine("Usage: dotnet run --project src/Fouroam.Client.Test --");
        Console.WriteLine("  --host 127.0.0.1 --port 7777 --name Athilio");
        Console.WriteLine("  --fingerprint <TLS-SHA256-from-host> [--identity-file local-path]");
        Console.WriteLine("Password is prompted privately; never supply it on the command line.");
    }
}
