using System.Net;
using System.Net.Sockets;
using Fouroam.Server;
using Fouroam.Shared;

namespace Fouroam.SmokeTests;

internal static class Program
{
    private static async Task<int> Main()
    {
        try
        {
            await RunAsync();
            Console.WriteLine("[PASS] Network smoke tests passed.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[FAIL] {ex}");
            return 1;
        }
    }

    private static async Task RunAsync()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(25));
        await using var server = new ServerHost(IPAddress.Loopback, port: 0);
        server.Start(); // OS chooses an available ephemeral port.
        Console.WriteLine($"[TEST] Server listening on 127.0.0.1:{server.Port}");

        var peers = new List<TestPeer>();
        try
        {
            for (var index = 1; index <= 4; index++)
            {
                var peer = await TestPeer.ConnectAsync(server.Port, $"Player{index}", timeout.Token);
                peers.Add(peer);
                var welcome = await peer.ReadUntilAsync(MessageTypes.Welcome, timeout.Token);
                Assert(welcome.PlayerCount == index, $"Expected {index} players after join.");
                Assert(welcome.MaxPlayers == 4, "Server must advertise four-player capacity.");
                Assert(welcome.ProtocolVersion == 1, "Protocol version should be 1.");
            }

            Console.WriteLine("[PASS] Four connections joined successfully.");

            using (var fifth = await TestPeer.ConnectAsync(server.Port, "Player5", timeout.Token))
            {
                var rejected = await fifth.ReadUntilAsync(MessageTypes.ServerFull, timeout.Token);
                Assert(rejected.MaxPlayers == 4, "Fifth connection must be rejected.");
            }

            Console.WriteLine("[PASS] Fifth connection rejected as full.");

            await peers[0].Wire.SendAsync(new WireMessage
            {
                Type = MessageTypes.PlayersRequest
            }, timeout.Token);

            var players = await peers[0].ReadUntilAsync(MessageTypes.Players, timeout.Token);
            Assert(players.Players?.Count == 4, "Expected four names in the player roster.");
            Assert(players.Players!.Select(p => p.Id).Distinct().Count() == 4,
                "Player IDs must be unique.");

            await peers[0].Wire.SendAsync(new WireMessage
            {
                Type = MessageTypes.Ping,
                Timestamp = 12345
            }, timeout.Token);

            var pong = await peers[0].ReadUntilAsync(MessageTypes.Pong, timeout.Token);
            Assert(pong.Timestamp == 12345, "Ping timestamp must be echoed.");

            await peers[0].Wire.SendAsync(new WireMessage
            {
                Type = MessageTypes.Chat,
                Text = "Hello from smoke test"
            }, timeout.Token);

            var chat = await peers[1].ReadUntilAsync(MessageTypes.Chat, timeout.Token);
            Assert(chat.Text == "Hello from smoke test", "Chat must reach other players.");
            Assert(chat.Name == "Player1", "Chat sender must be set by server.");

            Console.WriteLine("[PASS] Roster, ping and chat messages.");

            peers[0].Dispose();
            peers.RemoveAt(0);
            var left = await peers[0].ReadUntilAsync(MessageTypes.PlayerLeft, timeout.Token);
            Assert(left.Name == "Player1", "Disconnect must be broadcast.");

            using (var replacement = await TestPeer.ConnectAsync(
                server.Port, "Replacement", timeout.Token))
            {
                var welcome = await replacement.ReadUntilAsync(MessageTypes.Welcome, timeout.Token);
                Assert(welcome.PlayerCount == 4, "A replacement player should join after disconnect.");
            }

            Console.WriteLine("[PASS] Disconnect frees a slot for another player.");

            using var invalid = await TestPeer.ConnectAsync(
                server.Port, "x", timeout.Token);
            var error = await invalid.ReadUntilAsync(MessageTypes.Error, timeout.Token);
            Assert(error.Type == MessageTypes.Error, "Invalid player name must be rejected.");
            Console.WriteLine("[PASS] Invalid handshake rejected.");
        }
        finally
        {
            foreach (var peer in peers)
                peer.Dispose();

            await server.StopAsync();
        }
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    private sealed class TestPeer : IDisposable
    {
        private readonly TcpClient _client;
        public JsonLineConnection Wire { get; }

        private TestPeer(TcpClient client)
        {
            _client = client;
            Wire = new JsonLineConnection(client.GetStream());
        }

        public static async Task<TestPeer> ConnectAsync(
            int port, string name, CancellationToken cancellationToken)
        {
            var client = new TcpClient { NoDelay = true };
            try
            {
                await client.ConnectAsync(IPAddress.Loopback, port, cancellationToken);
                var peer = new TestPeer(client);
                await peer.Wire.SendAsync(new WireMessage
                {
                    Type = MessageTypes.Hello,
                    Name = name,
                    ProtocolVersion = JsonLineConnection.ProtocolVersion
                }, cancellationToken);
                return peer;
            }
            catch
            {
                client.Dispose();
                throw;
            }
        }

        public async Task<WireMessage> ReadUntilAsync(
            string messageType, CancellationToken cancellationToken)
        {
            for (var i = 0; i < 32; i++)
            {
                var message = await Wire.ReceiveAsync(cancellationToken);
                if (message is null)
                    throw new IOException("Connection closed before expected message.");

                if (message.Type == messageType)
                    return message;
            }

            throw new InvalidDataException($"Expected {messageType}, but received too many other messages.");
        }

        public void Dispose()
        {
            _client.Dispose();
            Wire.Dispose();
        }
    }
}
