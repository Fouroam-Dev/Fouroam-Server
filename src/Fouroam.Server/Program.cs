using System.Net;

namespace Fouroam.Server;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        string dataDir = Path.GetFullPath(".fouroam");

        if (args.Contains("--help") || args.Contains("-h"))
        {
            PrintUsage();
            return 0;
        }

        if (args.Length == 1 && args[0].Equals("init", StringComparison.OrdinalIgnoreCase))
            return Initialize(dataDir);

        var bind = IPAddress.Loopback;
        var port = 7777;

        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--bind" when i + 1 < args.Length:
                    if (!IPAddress.TryParse(args[++i], out var parsedAddress) || parsedAddress is null)
                    {
                        Console.Error.WriteLine("Invalid bind address.");
                        return 2;
                    }
                    bind = parsedAddress;
                    break;

                case "--port" when i + 1 < args.Length:
                    if (!int.TryParse(args[++i], out port) || port is < 1 or > 65535)
                    {
                        Console.Error.WriteLine("Port must be between 1 and 65535.");
                        return 2;
                    }
                    break;

                default:
                    Console.Error.WriteLine($"Unknown or incomplete option: {args[i]}");
                    PrintUsage();
                    return 2;
            }
        }

        try
        {
            var settings = ServerSettings.Load(dataDir);
            using var certificate = LocalCertificate.LoadOrCreate(dataDir);
            var bans = new BanStore(Path.Combine(dataDir, "bans.json"));
            await using var server = new ServerHost(bind, port, settings, certificate, bans, Console.WriteLine);
            using var shutdown = new CancellationTokenSource();

            Console.CancelKeyPress += (_, e) =>
            {
                e.Cancel = true;
                shutdown.Cancel();
            };

            server.Start();
            if (!bind.Equals(IPAddress.Loopback) && !bind.Equals(IPAddress.IPv6Loopback))
                Console.WriteLine("[SECURITY] Only expose this experimental server to a trusted LAN or VPN; NEVER port-forward it.");

            Console.WriteLine("[ADMIN] Type 'help' for commands, 'stop' to shut down.");
            try
            {
                await AdminConsole.RunAsync(server, shutdown.Token);
            }
            catch (OperationCanceledException) when (shutdown.IsCancellationRequested) { }

            await server.StopAsync();
            return 0;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or
            System.Security.Cryptography.CryptographicException or
            System.Text.Json.JsonException or ArgumentException or FormatException or
            System.Net.Sockets.SocketException)
        {
            Console.Error.WriteLine($"[ERROR] {ex.Message}");
            return 1;
        }
    }

    private static int Initialize(string dataDir)
    {
        if (File.Exists(ServerSettings.ConfigPath(dataDir)))
        {
            Console.Error.WriteLine("Server already initialized. Refusing to overwrite credentials.");
            return 1;
        }

        if (Console.IsInputRedirected)
        {
            Console.Error.WriteLine("Initialization requires an interactive terminal to enter a secret safely.");
            return 1;
        }

        try
        {
            Console.Write("Session name [Fouroam Private Session]: ");
            string name = Console.ReadLine() is { Length: > 0 } response
                ? response : "Fouroam Private Session";

            Console.Write("Password (12-128 characters; hidden): ");
            string password = ReadSecret();
            Console.Write("Confirm password (hidden): ");
            string confirmation = ReadSecret();
            if (password != confirmation)
            {
                Console.Error.WriteLine("Passwords do not match.");
                return 1;
            }

            var settings = ServerSettings.Create(name, password);
            settings.Save(dataDir);
            using var certificate = LocalCertificate.LoadOrCreate(dataDir);

            Console.WriteLine($"[SETUP] Private session: {settings.SessionName}");
            Console.WriteLine($"[SETUP] Config saved in {dataDir}");
            Console.WriteLine($"[SETUP] TLS SHA-256 fingerprint: {LocalCertificate.Fingerprint(certificate)}");
            Console.WriteLine("[SETUP] Never upload the .fouroam directory to GitHub.");
            return 0;
        }
        catch (Exception ex) when (ex is IOException or ArgumentException or
            UnauthorizedAccessException or System.Security.Cryptography.CryptographicException)
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
                continue;
            }
            if (!char.IsControl(key.KeyChar) && characters.Count < 128)
                characters.Add(key.KeyChar);
        }
    }

    private static void PrintUsage()
    {
        Console.WriteLine("Fouroam Server v0.2.0-dev (TLS-only standalone prototype)");
        Console.WriteLine("First time: dotnet run --project src/Fouroam.Server -- init");
        Console.WriteLine("Run: dotnet run --project src/Fouroam.Server -- [--bind IP] [--port PORT]");
        Console.WriteLine("Defaults: --bind 127.0.0.1 --port 7777");
        Console.WriteLine("Trusted LAN/VPN: --bind 0.0.0.0 --port 7777 (use firewall restrictions)");
    }
}
