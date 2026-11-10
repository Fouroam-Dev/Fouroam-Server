using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Fouroam.Shared;

namespace Fouroam.Server;

public sealed record PlayerAdminInfo(int Id, string Name, string IdentityFingerprint);

/// <summary>
/// TLS-only four-player network prototype with local-console administration.
/// No GTA V integration. Remote clients have no administrative privileges.
/// </summary>
public sealed class ServerHost : IAsyncDisposable
{
    public const int MaxPlayers = 4;

    private readonly TcpListener _listener;
    private readonly Action<string> _log;
    private readonly ServerSettings _settings;
    private readonly X509Certificate2 _certificate;
    private readonly BanStore _bans;
    private readonly FailedLoginGate _failedLogins = new();
    private readonly object _gate = new();
    private readonly Dictionary<int, ConnectedPlayer> _players = [];
    private readonly HashSet<TcpClient> _sockets = [];
    private readonly HashSet<Task> _handlers = [];
    private readonly SemaphoreSlim _pendingHandshakes = new(8, 8);
    private readonly CancellationTokenSource _shutdown = new();

    private Task? _acceptTask;
    private int _nextPlayerId;
    private int _stopped;
    private bool _started;

    public int Port { get; private set; }
    public string SessionName => _settings.SessionName;
    public string CertificateFingerprint => LocalCertificate.Fingerprint(_certificate);

    public ServerHost(
        IPAddress bindAddress, int port,
        ServerSettings settings, X509Certificate2 certificate,
        BanStore bans, Action<string>? log = null)
    {
        ArgumentNullException.ThrowIfNull(bindAddress);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(certificate);
        ArgumentNullException.ThrowIfNull(bans);
        if (port is < 0 or > 65535) throw new ArgumentOutOfRangeException(nameof(port));

        _listener = new TcpListener(bindAddress, port);
        _settings = settings;
        _certificate = certificate;
        _bans = bans;
        _log = log ?? (_ => { });
    }

    public PlayerAdminInfo[] GetPlayers()
    {
        lock (_gate)
            return _players.Values.OrderBy(p => p.Id)
                .Select(p => new PlayerAdminInfo(p.Id, p.Name, p.IdentityFingerprint))
                .ToArray();
    }

    public string[] GetBans() => _bans.Snapshot();

    public bool Unban(string fingerprintPrefix) => _bans.RemoveByPrefix(fingerprintPrefix);

    public void Start()
    {
        if (_started) throw new InvalidOperationException("Server already started.");
        _listener.Start();
        Port = ((IPEndPoint)_listener.LocalEndpoint).Port;
        _started = true;

        _log($"[NETWORK] TLS listening on {_listener.LocalEndpoint}");
        _log($"[SESSION] {_settings.SessionName} | max players: {MaxPlayers}");
        _log($"[SECURITY] Certificate SHA-256: {CertificateFingerprint}");
        _log("[SECURITY] Share the fingerprint with friends by a trusted channel.");
        _acceptTask = AcceptLoopAsync(_shutdown.Token);
    }

    private async Task AcceptLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            TcpClient client;
            try
            {
                client = await _listener.AcceptTcpClientAsync(cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { break; }
            catch (ObjectDisposedException) when (cancellationToken.IsCancellationRequested) { break; }
            catch (SocketException) when (cancellationToken.IsCancellationRequested) { break; }

            client.NoDelay = true;
            if (!_pendingHandshakes.Wait(0))
            {
                client.Dispose();
                _log("[SECURITY] Rejected connection: too many pending handshakes.");
                continue;
            }

            lock (_gate) _sockets.Add(client);
            var task = HandleClientAsync(client, cancellationToken);
            lock (_gate) _handlers.Add(task);
            _ = task.ContinueWith(completed =>
            {
                lock (_gate) _handlers.Remove(completed);
                if (completed.IsFaulted)
                    _log("[SECURITY] Connection handler terminated unexpectedly.");
            }, TaskScheduler.Default);
        }
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
    {
        var ownsHandshakeSlot = true;
        ConnectedPlayer? player = null;
        string remoteAddress = (client.Client.RemoteEndPoint as IPEndPoint)
            ?.Address.ToString() ?? "unknown";

        try
        {
            using (client)
            {
                using var handshake = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                handshake.CancelAfter(TimeSpan.FromSeconds(10));

                if (_failedLogins.IsBlocked(remoteAddress))
                {
                    _log("[SECURITY] Temporarily blocked a connection after repeated authentication failures.");
                    return;
                }

                using var tls = new SslStream(client.GetStream(), leaveInnerStreamOpen: false);
                await tls.AuthenticateAsServerAsync(
                    new SslServerAuthenticationOptions
                    {
                        ServerCertificate = _certificate,
                        EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
                        ClientCertificateRequired = false
                    }, handshake.Token);

                using var wire = new JsonLineConnection(tls);
                var hello = await wire.ReceiveAsync(handshake.Token);

                if (hello?.Type != MessageTypes.Hello ||
                    hello.ProtocolVersion != JsonLineConnection.ProtocolVersion ||
                    !TryValidateName(hello.Name, out var name) ||
                    !ClientIdentity.TryHashToken(hello.IdentityToken, out var identityFingerprint) ||
                    !_settings.Verify(hello.Password))
                {
                    var failures = _failedLogins.RecordFailure(remoteAddress);
                    _log($"[SECURITY] Authentication rejected ({failures}/5 recent failures for one endpoint).");
                    await TrySendAsync(wire, new WireMessage
                    {
                        Type = MessageTypes.Error,
                        Text = "Authentication failed (name, password, identity or protocol version)."
                    }, handshake.Token);
                    return;
                }

                if (_bans.Contains(identityFingerprint))
                {
                    _log("[SECURITY] Banned client identity rejected.");
                    await TrySendAsync(wire, new WireMessage
                    {
                        Type = MessageTypes.Error, Text = "This client identity is banned."
                    }, handshake.Token);
                    return;
                }

                List<PlayerInfo> roster;
                bool identityInUse;
                lock (_gate)
                {
                    identityInUse = _players.Values.Any(p =>
                        p.IdentityFingerprint == identityFingerprint);
                    if (!identityInUse && _players.Count < MaxPlayers)
                    {
                        player = new ConnectedPlayer(
                            ++_nextPlayerId, name, identityFingerprint, client, wire);
                        _players.Add(player.Id, player);
                    }
                    roster = GetRosterUnsafe();
                }

                if (identityInUse)
                {
                    await TrySendAsync(wire, new WireMessage
                    {
                        Type = MessageTypes.Error,
                        Text = "This client identity is already connected. Use a separate identity file for each local test client."
                    }, handshake.Token);
                    return;
                }

                if (player is null)
                {
                    await TrySendAsync(wire, new WireMessage
                    {
                        Type = MessageTypes.ServerFull,
                        PlayerCount = MaxPlayers,
                        MaxPlayers = MaxPlayers,
                        Text = "Session is full (4/4)."
                    }, handshake.Token);
                    return;
                }

                await wire.SendAsync(new WireMessage
                {
                    Type = MessageTypes.Welcome,
                    SessionName = _settings.SessionName,
                    ProtocolVersion = JsonLineConnection.ProtocolVersion,
                    PlayerId = player.Id,
                    PlayerCount = roster.Count,
                    MaxPlayers = MaxPlayers,
                    Players = roster
                }, handshake.Token);

                _pendingHandshakes.Release();
                ownsHandshakeSlot = false;

                _log($"[JOIN] Player #{player.Id}: {player.Name} ({roster.Count}/{MaxPlayers})");
                await BroadcastAsync(new WireMessage
                {
                    Type = MessageTypes.PlayerJoined,
                    PlayerId = player.Id,
                    Name = player.Name
                }, cancellationToken, exceptId: player.Id);

                while (!cancellationToken.IsCancellationRequested)
                {
                    var message = await wire.ReceiveAsync(cancellationToken);
                    if (message is null) break;

                    if (!player.TryConsumeCommand())
                    {
                        await TrySendAsync(wire, new WireMessage
                        {
                            Type = MessageTypes.Error, Text = "Rate limit exceeded. Disconnecting."
                        }, cancellationToken);
                        break;
                    }

                    switch (message.Type)
                    {
                        case MessageTypes.Ping:
                            await wire.SendAsync(new WireMessage
                            {
                                Type = MessageTypes.Pong, Timestamp = message.Timestamp
                            }, cancellationToken);
                            break;

                        case MessageTypes.PlayersRequest:
                            List<PlayerInfo> current;
                            lock (_gate) current = GetRosterUnsafe();
                            await wire.SendAsync(new WireMessage
                            {
                                Type = MessageTypes.Players,
                                PlayerCount = current.Count,
                                MaxPlayers = MaxPlayers,
                                Players = current
                            }, cancellationToken);
                            break;

                        case MessageTypes.Chat:
                            var text = message.Text?.Trim();
                            if (string.IsNullOrEmpty(text) || text.Length > 280 ||
                                text.Any(char.IsControl))
                            {
                                await wire.SendAsync(new WireMessage
                                {
                                    Type = MessageTypes.Error,
                                    Text = "Chat must contain 1-280 printable characters."
                                }, cancellationToken);
                                break;
                            }
                            await BroadcastAsync(new WireMessage
                            {
                                Type = MessageTypes.Chat,
                                PlayerId = player.Id,
                                Name = player.Name,
                                Text = text
                            }, cancellationToken);
                            break;

                        default:
                            // Remote administration is deliberately unsupported.
                            await wire.SendAsync(new WireMessage
                            {
                                Type = MessageTypes.Error, Text = "Unknown or unauthorized command."
                            }, cancellationToken);
                            break;
                    }
                }
            }
        }
        catch (Exception ex) when (ex is IOException or SocketException or
            ObjectDisposedException or InvalidDataException or OperationCanceledException or
            AuthenticationException)
        {
            // Corrupt frame, TLS handshake failure, network disconnect or shutdown.
        }
        finally
        {
            if (ownsHandshakeSlot) _pendingHandshakes.Release();

            if (player is not null)
            {
                bool removed;
                int count;
                lock (_gate)
                {
                    removed = _players.Remove(player.Id);
                    count = _players.Count;
                }
                if (removed)
                {
                    _log($"[LEAVE] Player #{player.Id}: {player.Name} ({count}/{MaxPlayers})");
                    await BroadcastAsync(new WireMessage
                    {
                        Type = MessageTypes.PlayerLeft,
                        PlayerId = player.Id,
                        Name = player.Name
                    }, cancellationToken);
                }
            }

            lock (_gate) _sockets.Remove(client);
        }
    }

    private List<PlayerInfo> GetRosterUnsafe() =>
        _players.Values.OrderBy(p => p.Id)
            .Select(p => new PlayerInfo(p.Id, p.Name)).ToList();

    public async Task<bool> KickAsync(int id, string reason, CancellationToken token = default)
    {
        ConnectedPlayer? player;
        lock (_gate) _players.TryGetValue(id, out player);
        if (player is null) return false;

        using var sendTimeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        sendTimeout.CancelAfter(TimeSpan.FromSeconds(2));
        await TrySendAsync(player.Wire, new WireMessage
        {
            Type = MessageTypes.Kicked,
            Text = reason.Length > 200 ? reason[..200] : reason
        }, sendTimeout.Token);
        player.Socket.Dispose();
        _log($"[ADMIN] Kicked player #{player.Id}: {player.Name}");
        return true;
    }

    public async Task<bool> BanAsync(int id, string reason, CancellationToken token = default)
    {
        ConnectedPlayer? player;
        lock (_gate) _players.TryGetValue(id, out player);
        if (player is null) return false;
        _bans.Add(player.IdentityFingerprint);
        _log($"[ADMIN] Banned identity {player.IdentityFingerprint[..12]}...");
        return await KickAsync(id, reason, token);
    }

    public async Task AnnounceAsync(string text, CancellationToken token = default)
    {
        if (string.IsNullOrWhiteSpace(text) || text.Length > 280 || text.Any(char.IsControl))
            throw new ArgumentException("Announcement must be 1-280 printable characters.");

        await BroadcastAsync(new WireMessage
        {
            Type = MessageTypes.Announcement, Text = text
        }, token);
    }

    private async Task BroadcastAsync(
        WireMessage message, CancellationToken cancellationToken, int? exceptId = null)
    {
        ConnectedPlayer[] recipients;
        lock (_gate)
            recipients = _players.Values.Where(p => p.Id != exceptId).ToArray();
        await Task.WhenAll(recipients.Select(p =>
            TrySendAsync(p.Wire, message, cancellationToken)));
    }

    private static async Task TrySendAsync(
        JsonLineConnection wire, WireMessage message, CancellationToken cancellationToken)
    {
        try { await wire.SendAsync(message, cancellationToken); }
        catch (Exception ex) when (ex is IOException or SocketException or
            ObjectDisposedException or OperationCanceledException or AuthenticationException)
        {
            // The recipient went offline while data was being sent.
        }
    }

    private static bool TryValidateName(string? candidate, out string name)
    {
        name = candidate?.Trim() ?? string.Empty;
        return name.Length is >= 2 and <= 24 &&
               name.All(c => char.IsLetterOrDigit(c) || c is ' ' or '_' or '-');
    }

    public async Task StopAsync()
    {
        if (Interlocked.Exchange(ref _stopped, 1) != 0) return;

        _shutdown.Cancel();
        _listener.Stop();

        TcpClient[] sockets;
        lock (_gate) sockets = _sockets.ToArray();
        foreach (var socket in sockets) socket.Dispose();

        if (_acceptTask is not null)
        {
            try { await _acceptTask; }
            catch (OperationCanceledException) { }
        }

        Task[] handlers;
        lock (_gate) handlers = _handlers.ToArray();
        await Task.WhenAll(handlers);
        _log("[NETWORK] Server stopped.");
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        _pendingHandshakes.Dispose();
        _shutdown.Dispose();
    }

    private sealed class ConnectedPlayer(
        int id, string name, string identityFingerprint, TcpClient socket, JsonLineConnection wire)
    {
        private readonly Queue<long> _recentCommands = new();
        private readonly object _rateGate = new();

        public int Id { get; } = id;
        public string Name { get; } = name;
        public string IdentityFingerprint { get; } = identityFingerprint;
        public TcpClient Socket { get; } = socket;
        public JsonLineConnection Wire { get; } = wire;

        public bool TryConsumeCommand()
        {
            lock (_rateGate)
            {
                long now = Environment.TickCount64;
                while (_recentCommands.Count > 0 && now - _recentCommands.Peek() > 5000)
                    _recentCommands.Dequeue();
                if (_recentCommands.Count >= 20) return false;
                _recentCommands.Enqueue(now);
                return true;
            }
        }
    }
}
