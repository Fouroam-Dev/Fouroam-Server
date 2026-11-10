using System.Net;

namespace Fouroam.Server;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        if (args.Contains("--help") || args.Contains("-h"))
        {
            PrintUsage();
            return 0;
        }

        var bind = IPAddress.Loopback; // Safe default: local machine only.
        var port = 7777;

        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--bind" when i + 1 < args.Length:
                    if (!IPAddress.TryParse(args[++i], out var parsedAddress) || parsedAddress is null)
                    {
                        Console.Error.WriteLine("Invalid --bind address.");
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
            await using var server = new ServerHost(bind, port, Console.WriteLine);
            using var shutdown = new CancellationTokenSource();

            Console.CancelKeyPress += (_, e) =>
            {
                e.Cancel = true;
                shutdown.Cancel();
            };

            server.Start();
            Console.WriteLine("[INFO] Press Ctrl+C to stop.");
            if (!bind.Equals(IPAddress.Loopback) && !bind.Equals(IPAddress.IPv6Loopback))
                Console.WriteLine("[WARN] No authentication or encryption yet. Use a trusted LAN/VPN only; do not port-forward this server.");

            try
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, shutdown.Token);
            }
            catch (OperationCanceledException)
            {
                // Normal Ctrl+C shutdown.
            }

            await server.StopAsync();
            return 0;
        }
        catch (Exception ex) when (ex is System.Net.Sockets.SocketException or IOException)
        {
            Console.Error.WriteLine($"Unable to start the server: {ex.Message}");
            return 1;
        }
    }

    private static void PrintUsage()
    {
        Console.WriteLine("Fouroam Server (standalone networking prototype)");
        Console.WriteLine("Usage: dotnet run --project src/Fouroam.Server -- [--bind IP] [--port PORT]");
        Console.WriteLine("Defaults: --bind 127.0.0.1 --port 7777");
        Console.WriteLine("For a trusted LAN/VPN: --bind 0.0.0.0 --port 7777");
    }
}
