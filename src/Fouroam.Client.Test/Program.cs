using System.Net.Sockets;
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

        var host = "127.0.0.1";
        var port = 7777;
        var name = "Player";

        for (var i = 0; i < args.Length; i++)
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
                default:
                    Console.Error.WriteLine($"Unknown or incomplete option: {args[i]}");
                    PrintUsage();
                    return 2;
            }
        }

        using var client = new TcpClient { NoDelay = true };
        using var shutdown = new CancellationTokenSource();

        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            shutdown.Cancel();
        };

        try
        {
            using (var connectTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(10)))
                await client.ConnectAsync(host, port, connectTimeout.Token);

            using var wire = new JsonLineConnection(client.GetStream());
            await wire.SendAsync(new WireMessage
            {
                Type = MessageTypes.Hello,
                ProtocolVersion = JsonLineConnection.ProtocolVersion,
                Name = name
            }, shutdown.Token);

            var response = await wire.ReceiveAsync(shutdown.Token);
            if (response?.Type != MessageTypes.Welcome)
            {
                Console.Error.WriteLine($"Connection rejected: {response?.Text ?? "server disconnected"}");
                return 1;
            }

            Console.WriteLine($"[CONNECTED] ID #{response.PlayerId}, players: {response.PlayerCount}/{response.MaxPlayers}");
            PrintRoster(response);
            Console.WriteLine("Commands: /players, /ping, /quit. Any other text sends a chat message.");

            var receiveTask = ReceiveLoopAsync(wire, shutdown.Token);
            var inputTask = InputLoopAsync(wire, shutdown.Token);

            await Task.WhenAny(receiveTask, inputTask);
            shutdown.Cancel();
            client.Close();

            try { await Task.WhenAll(receiveTask, inputTask); }
            catch (OperationCanceledException) { }
            catch (IOException) { }
            catch (ObjectDisposedException) { }

            Console.WriteLine("[DISCONNECTED]");
            return 0;
        }
        catch (Exception ex) when (ex is SocketException or IOException or
                                   InvalidDataException or OperationCanceledException)
        {
            Console.Error.WriteLine($"Connection failed: {ex.Message}");
            return 1;
        }
    }

    private static async Task ReceiveLoopAsync(
        JsonLineConnection wire, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var message = await wire.ReceiveAsync(cancellationToken);
            if (message is null)
                return;

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
                case MessageTypes.Pong:
                    if (message.Timestamp is long sent)
                        Console.WriteLine($"[PING] {DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - sent} ms (round trip)");
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
            if (line is null || line.Trim() == "/quit")
                return;

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
        Console.WriteLine("Fouroam CLI test client (not integrated with GTA V)");
        Console.WriteLine("Usage: dotnet run --project src/Fouroam.Client.Test -- [--host IP] [--port PORT] [--name NAME]");
        Console.WriteLine("Defaults: --host 127.0.0.1 --port 7777 --name Player");
    }
}
