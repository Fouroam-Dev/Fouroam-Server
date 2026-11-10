using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using Fouroam.Server;
using Fouroam.Shared;

namespace Fouroam.SmokeTests;

internal static class Program
{
    private const string Password = "very-private-test-password-2026";

    private static async Task<int> Main()
    {
        string directory = Path.Combine(Path.GetTempPath(), "fouroam-smoke-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);

        try
        {
            await RunAsync(directory);
            Console.WriteLine("[PASS] All v0.2.0 security and network smoke tests passed.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[FAIL] {ex}");
            return 1;
        }
        finally
        {
            try { Directory.Delete(directory, recursive: true); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }

    private static async Task RunAsync(string dataDir)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(50));

        // Regressions for a freshly downloaded EXE with no .fouroam directory.
        string wizardDir = Path.Combine(dataDir, "fresh-exe");
        Assert(ServerSetup.NeedsInitialization(wizardDir),
            "Unconfigured portable folder must be detected on first run.");
        var wizardMessages = new List<string>();
        var wizardErrors = new List<string>();
        Assert(ServerSetup.TryInitialize(
            wizardDir, () => "Fresh Portable", () => "abcd", () => "abcd",
            wizardMessages.Add, wizardErrors.Add),
            "First-run wizard should create a valid private session.");
        Assert(!ServerSetup.NeedsInitialization(wizardDir),
            "New configuration must be found on subsequent launches.");
        Assert(ServerSettings.Load(wizardDir).Verify("abcd"),
            "First-run wizard must save the chosen password securely.");
        Assert(File.Exists(LocalCertificate.CertificatePath(wizardDir)),
            "First-run wizard must generate the host TLS certificate.");
        Assert(!wizardMessages.Any(m => m.Contains("abcd")),
            "Setup messages must not include the plaintext password.");
        Assert(!ServerSetup.TryInitialize(
            wizardDir, () => "Replace", () => "newpassword123", () => "newpassword123",
            wizardMessages.Add, wizardErrors.Add),
            "Reinitializing must never overwrite an existing session.");
        Assert(ServerSettings.Load(wizardDir).SessionName == "Fresh Portable",
            "Existing session must remain untouched.");

        string mismatchDir = Path.Combine(dataDir, "mismatched-password");
        Assert(!ServerSetup.TryInitialize(
            mismatchDir, () => "Bad Setup", () => "abcd", () => "wxyz",
            wizardMessages.Add, wizardErrors.Add),
            "Mismatched passwords must not create a private session.");
        Assert(ServerSetup.NeedsInitialization(mismatchDir),
            "Failed first-run setup must not leave a config file.");
        Console.WriteLine("[PASS] Portable first-run wizard, TLS setup and no overwrites.");

        // The minimum of 4 characters is intentional for small private sessions.
        var shortPasswordSettings = ServerSettings.Create("Short Pass", "abcd");
        Assert(shortPasswordSettings.Verify("abcd"), "Exactly four characters must be accepted.");
        Assert(!shortPasswordSettings.Verify("abc"), "Short password verifier must reject a wrong password.");
        bool tooShortRejected = false;
        try { ServerSettings.Create("Short Pass", "abc"); }
        catch (ArgumentException) { tooShortRejected = true; }
        Assert(tooShortRejected, "Three-character passwords must be rejected.");
        bool tooLongRejected = false;
        try { ServerSettings.Create("Short Pass", new string('x', 129)); }
        catch (ArgumentException) { tooLongRejected = true; }
        Assert(tooLongRejected, "129-character passwords must be rejected.");
        Console.WriteLine("[PASS] Password length boundaries: 3 rejected, 4 accepted, 129 rejected.");
        var settings = ServerSettings.Create("Test Session", Password);
        settings.Save(dataDir);
        var loaded = ServerSettings.Load(dataDir);
        Assert(loaded.Verify(Password), "Valid password verification failed.");
        Assert(!loaded.Verify("wrong-password"), "Invalid password must not verify.");
        Assert(!File.ReadAllText(ServerSettings.ConfigPath(dataDir)).Contains(Password),
            "Session password was written in plaintext.");
        Console.WriteLine("[PASS] Private PBKDF2 server configuration.");

        using var certificate = LocalCertificate.LoadOrCreate(dataDir);
        Assert(certificate.HasPrivateKey, "Certificate must have its private key.");
        string fingerprint = LocalCertificate.Fingerprint(certificate);
        Assert(TlsFingerprint.Matches(certificate, fingerprint), "Pinned certificate should match.");
        Assert(!TlsFingerprint.Matches(certificate, new string('0', 64)),
            "Incorrect TLS certificate pin must not match.");
        Console.WriteLine("[PASS] TLS certificate fingerprint verification.");

        string banPath = Path.Combine(dataDir, "bans.json");
        var bans = new BanStore(banPath);
        await using var server = new ServerHost(
            IPAddress.Loopback, 0, loaded, certificate, bans);
        server.Start();

        var peers = new List<TestPeer>();
        var identities = Enumerable.Range(0, 6).Select(_ => ClientIdentity.NewToken()).ToArray();

        try
        {
            for (int i = 0; i < 4; i++)
            {
                var peer = await TestPeer.ConnectAsync(
                    server.Port, fingerprint, $"Player{i + 1}", Password,
                    identities[i], timeout.Token);
                peers.Add(peer);
                var welcome = await peer.ReadUntilAsync(MessageTypes.Welcome, timeout.Token);
                Assert(welcome.ProtocolVersion == 2, "Expected protocol v2.");
                Assert(welcome.PlayerCount == i + 1, "Incorrect join count.");
                Assert(welcome.SessionName == "Test Session", "Session name not delivered.");
            }
            Assert(server.GetPlayers().Length == 4, "Expected four connected clients.");
            Console.WriteLine("[PASS] Four authenticated TLS clients joined.");

            var helpLines = new List<string>();
            Assert(await AdminConsole.ExecuteAsync(server, "/help", helpLines.Add, timeout.Token),
                "Slash help should keep the host console running.");
            Assert(helpLines.Any(l => l.Contains("kick <id|name>")),
                "Slash help should describe ID/name targets.");
            var statusLines = new List<string>();
            await AdminConsole.ExecuteAsync(server, "/status", statusLines.Add, timeout.Token);
            Assert(statusLines.Any(l => l.Contains("4/4")),
                "Slash status should report the current number of players.");
            var playerLines = new List<string>();
            await AdminConsole.ExecuteAsync(server, "/players", playerLines.Add, timeout.Token);
            Assert(playerLines.Any(l => l.Contains("Player4")),
                "Slash players should show the roster.");
            await AdminConsole.ExecuteAsync(server, "/kick Nobody", _ => { }, timeout.Token);
            Assert(server.GetPlayers().Length == 4, "Unknown name must not kick anyone.");
            Console.WriteLine("[PASS] Host slash commands, status and name validation.");

            using (var fifth = await TestPeer.ConnectAsync(
                server.Port, fingerprint, "Fifth", Password, identities[4], timeout.Token))
            {
                var rejected = await fifth.ReadUntilAsync(MessageTypes.ServerFull, timeout.Token);
                Assert(rejected.MaxPlayers == 4, "Fifth client should be rejected.");
            }
            Console.WriteLine("[PASS] Fifth client refused at capacity.");

            await peers[0].Wire.SendAsync(new WireMessage { Type = MessageTypes.PlayersRequest }, timeout.Token);
            var roster = await peers[0].ReadUntilAsync(MessageTypes.Players, timeout.Token);
            Assert(roster.Players?.Count == 4, "Roster should contain four clients.");

            await peers[0].Wire.SendAsync(new WireMessage
            {
                Type = MessageTypes.Ping, Timestamp = 1234567
            }, timeout.Token);
            var pong = await peers[0].ReadUntilAsync(MessageTypes.Pong, timeout.Token);
            Assert(pong.Timestamp == 1234567, "Ping must echo timestamp.");

            await peers[0].Wire.SendAsync(new WireMessage
            {
                Type = MessageTypes.Chat, Text = "Hello from authenticated client"
            }, timeout.Token);
            var chat = await peers[1].ReadUntilAsync(MessageTypes.Chat, timeout.Token);
            Assert(chat.Name == "Player1", "Server must select sender name.");
            Assert(chat.Text == "Hello from authenticated client", "Chat was not delivered.");
            Console.WriteLine("[PASS] Original roster, ping and chat features remain working.");

            // A client must not have any remote administration access.
            await peers[0].Wire.SendAsync(new WireMessage
            {
                Type = "kick", PlayerId = 2, Text = "forged admin command"
            }, timeout.Token);
            var unauthorized = await peers[0].ReadUntilAsync(MessageTypes.Error, timeout.Token);
            Assert(unauthorized.Text?.Contains("unauthorized") == true,
                "Client administration attempt must be refused.");
            Assert(server.GetPlayers().Length == 4, "Forged kick should not disconnect anyone.");
            Console.WriteLine("[PASS] Remote admin command denied.");

            // Duplicate client identity is prohibited while connected.
            using (var duplicate = await TestPeer.ConnectAsync(
                server.Port, fingerprint, "Duplicated", Password, identities[0], timeout.Token))
            {
                var error = await duplicate.ReadUntilAsync(MessageTypes.Error, timeout.Token);
                Assert(error.Text?.Contains("already connected") == true,
                    "Duplicate active identity must be refused.");
            }

            bool kicked = await AdminConsole.ExecuteAsync(
                server, "/kick Player4 testing-kick", _ => { }, timeout.Token);
            Assert(kicked, "Admin console should continue after kick.");
            var kickedMessage = await peers[3].ReadUntilAsync(MessageTypes.Kicked, timeout.Token);
            Assert(kickedMessage.Text == "testing-kick", "Kick reason not delivered.");
            peers[3].Dispose();
            await WaitForPlayersAsync(server, expected: 3, timeout.Token);
            Console.WriteLine("[PASS] Host kick disconnects target and frees capacity.");

            using (var replacement = await TestPeer.ConnectAsync(
                server.Port, fingerprint, "Replacement", Password, identities[4], timeout.Token))
            {
                var welcome = await replacement.ReadUntilAsync(MessageTypes.Welcome, timeout.Token);
                Assert(welcome.PlayerCount == 4, "Replacement should occupy available slot.");

                await AdminConsole.ExecuteAsync(server, "/say server-announcement", _ => { }, timeout.Token);
                var announcement = await replacement.ReadUntilAsync(MessageTypes.Announcement, timeout.Token);
                Assert(announcement.Text == "server-announcement", "Admin announcement missing.");
            }
            Console.WriteLine("[PASS] Announcement and replacement connection.");

            int bannedId = server.GetPlayers().Single(p => p.Name == "Player1").Id;
            await AdminConsole.ExecuteAsync(server, $"/ban #{bannedId} banned-by-test", _ => { }, timeout.Token);
            var removed = await peers[0].ReadUntilAsync(MessageTypes.Kicked, timeout.Token);
            Assert(removed.Text == "banned-by-test", "Ban did not disconnect target.");
            peers[0].Dispose();
            await WaitForPlayersAsync(server, expected: 2, timeout.Token);

            Assert(ClientIdentity.TryHashToken(identities[0], out string bannedFingerprint),
                "Invalid identity token in test.");
            Assert(new BanStore(banPath).Contains(bannedFingerprint),
                "Ban should persist to disk.");

            using (var blocked = await TestPeer.ConnectAsync(
                server.Port, fingerprint, "Player1", Password, identities[0], timeout.Token))
            {
                var error = await blocked.ReadUntilAsync(MessageTypes.Error, timeout.Token);
                Assert(error.Text?.Contains("banned") == true,
                    "Persistently banned identity must be rejected.");
            }
            Console.WriteLine("[PASS] Ban persists and blocks rejoin.");

            // Admin commands should accept unique fingerprint prefixes for unban.
            var entries = new List<string>();
            await AdminConsole.ExecuteAsync(server, "bans", entries.Add, timeout.Token);
            Assert(entries.Any(e => e.Contains(bannedFingerprint)),
                "Ban listing must contain the persistent fingerprint.");

            await AdminConsole.ExecuteAsync(server, $"/unban {bannedFingerprint[..16]}", _ => { }, timeout.Token);
            Assert(!new BanStore(banPath).Contains(bannedFingerprint),
                "Ban removal must update the persisted file.");

            using (var allowed = await TestPeer.ConnectAsync(
                server.Port, fingerprint, "Unbanned", Password, identities[0], timeout.Token))
            {
                var welcome = await allowed.ReadUntilAsync(MessageTypes.Welcome, timeout.Token);
                Assert(welcome.Type == MessageTypes.Welcome, "Unbanned client should rejoin.");
            }
            Console.WriteLine("[PASS] Host unban restores access.");

            using (var wrongPassword = await TestPeer.ConnectAsync(
                server.Port, fingerprint, "WrongPassword", "bad-password",
                identities[5], timeout.Token))
            {
                var rejected = await wrongPassword.ReadUntilAsync(MessageTypes.Error, timeout.Token);
                Assert(rejected.Text?.Contains("Authentication failed") == true,
                    "Incorrect password must be rejected.");
            }

            // Direct unit check for the 5 failures / 60 seconds limiter.
            var limiter = new FailedLoginGate();
            for (int i = 0; i < 5; i++) limiter.RecordFailure("127.0.0.1");
            Assert(limiter.IsBlocked("127.0.0.1"), "Rate limiter should block after five failures.");
            Console.WriteLine("[PASS] Wrong password and login attempt limiter.");

            Assert(!await AdminConsole.ExecuteAsync(server, "/stop", _ => { }, timeout.Token),
                "Slash stop should signal the host console to exit.");
            Console.WriteLine("[PASS] Slash stop recognized.");
        }
        finally
        {
            foreach (var peer in peers)
                peer.Dispose();
            await server.StopAsync();
        }

        // Reopen the ban database to confirm it remains consistent across restarts.
        var reloadedBans = new BanStore(banPath);
        Assert(reloadedBans.Snapshot().Length == 0,
            "Unbans must remain effective after restarting the server.");
        Console.WriteLine("[PASS] Persistent ban file reload after server shutdown.");
    }

    private static async Task WaitForPlayersAsync(
        ServerHost server, int expected, CancellationToken cancellationToken)
    {
        while (server.GetPlayers().Length != expected)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Delay(20, cancellationToken);
        }
    }

    private static void Assert(bool success, string message)
    {
        if (!success) throw new InvalidOperationException(message);
    }

    private sealed class TestPeer : IDisposable
    {
        private readonly TcpClient _client;
        private readonly SslStream _tls;
        public JsonLineConnection Wire { get; }

        private TestPeer(TcpClient client, SslStream tls)
        {
            _client = client;
            _tls = tls;
            Wire = new JsonLineConnection(tls);
        }

        public static async Task<TestPeer> ConnectAsync(
            int port, string fingerprint, string name,
            string password, string identity, CancellationToken token)
        {
            var tcp = new TcpClient { NoDelay = true };
            SslStream? tls = null;
            try
            {
                await tcp.ConnectAsync(IPAddress.Loopback, port, token);
                tls = new SslStream(tcp.GetStream(), false,
                    (_, cert, _, _) => TlsFingerprint.Matches(cert, fingerprint));
                await tls.AuthenticateAsClientAsync(new SslClientAuthenticationOptions
                {
                    TargetHost = "Fouroam.Private",
                    EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
                    CertificateRevocationCheckMode = X509RevocationMode.NoCheck
                }, token);

                var peer = new TestPeer(tcp, tls);
                await peer.Wire.SendAsync(new WireMessage
                {
                    Type = MessageTypes.Hello,
                    ProtocolVersion = JsonLineConnection.ProtocolVersion,
                    Name = name,
                    Password = password,
                    IdentityToken = identity
                }, token);
                return peer;
            }
            catch
            {
                tls?.Dispose();
                tcp.Dispose();
                throw;
            }
        }

        public async Task<WireMessage> ReadUntilAsync(
            string expectedType, CancellationToken token)
        {
            for (int i = 0; i < 48; i++)
            {
                var message = await Wire.ReceiveAsync(token);
                if (message is null)
                    throw new IOException($"Disconnected before {expectedType}.");
                if (message.Type == expectedType)
                    return message;
            }
            throw new InvalidDataException($"Did not see expected message: {expectedType}.");
        }

        public void Dispose()
        {
            _client.Dispose();
            Wire.Dispose();
            _tls.Dispose();
        }
    }
}
