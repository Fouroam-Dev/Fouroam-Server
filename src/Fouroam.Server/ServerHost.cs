using System.Net;
using System.Net.Sockets;
using Fouroam.Shared;

namespace Fouroam.Server;

/// <summary>
/// Standalone four-player TCP session. No GTA V/game integration.
/// This prototype has no authentication or transport encryption.
/// </summary>
public sealed class ServerHost : IAsyncDisposable
{
    public const int MaxPlayers = 4;

    private readonly TcpListener _listener;
    private readonly Action<string> _log;
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

    public ServerHost(IPAddress bindAddress, int port = 7777, Action<string>? log = null)
    {
        ArgumentNullException.ThrowIfNull(bindAddress);
        if (port is < 0 or > 65535)
            throw new ArgumentOutOfRangeException(nameof(port));

        _listener = new TcpListener(bindAddress, port);
        _log = log ?? (_ => { });
    }

    public void Start()
    {
        if (_started)
            throw new InvalidOperationException("The server is already started.");

        _listener.Start();
        Port = ((IPEndPoint)_listener.LocalEndpoint).Port;
        _started = true;

        _log($"[NETWORK] Listening on {_listener.LocalEndpoint}");
        _log($"[SESSION] Max players: {MaxPlayers}");
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
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (ObjectDisposedException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (SocketException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            client.NoDelay = true;

            if (!_pendingHandshakes.Wait(0))
            {
                client.Dispose(); // Limit simultaneous incomplete handshakes.
                continue;
            }

            lock (_gate)
                _sockets.Add(client);

            var task = HandleClientAsync(client, cancellationToken);
            lock (_gate)
                _handlers.Add(task);

            _ = task.ContinueWith(completed =>
            {
                lock (_gate)
                    _handlers.Remove(completed);
            }, TaskScheduler.Default);
        }
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
    {
        var ownsHandshakeSlot = true;
        ConnectedPlayer? player = null;

        using (client)
        using (var wire = new JsonLineConnection(client.GetStream()))
        {
            try
            {
                using var helloTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                helloTimeout.CancelAfter(TimeSpan.FromSeconds(10));

                var hello = await wire.ReceiveAsync(helloTimeout.Token);
                if (hello?.Type != MessageTypes.Hello ||
                    hello.ProtocolVersion != JsonLineConnection.ProtocolVersion ||
                    !TryValidateName(hello.Name, out var name))
                {
                    await TrySendAsync(wire, new WireMessage
                    {
                        Type = MessageTypes.Error,
                        Text = "Invalid hello, protocol version or player name."
                    }, cancellationToken);
                    return;
                }

                List<PlayerInfo> roster;
                lock (_gate)
                {
                    if (_players.Count < MaxPlayers)
                    {
                        player = new ConnectedPlayer(++_nextPlayerId, name, client, wire);
                        _players.Add(player.Id, player);
                    }

                    roster = GetRosterUnsafe();
                }

                if (player is null)
                {
                    await TrySendAsync(wire, new WireMessage
                    {
                        Type = MessageTypes.ServerFull,
                        PlayerCount = MaxPlayers,
                        MaxPlayers = MaxPlayers,
                        Text = "Session is full (4/4)."
                    }, cancellationToken);
                    return;
                }

                await wire.SendAsync(new WireMessage
                {
                    Type = MessageTypes.Welcome,
                    ProtocolVersion = JsonLineConnection.ProtocolVersion,
                    PlayerId = player.Id,
                    PlayerCount = roster.Count,
                    MaxPlayers = MaxPlayers,
                    Players = roster
                }, cancellationToken);

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
                    if (message is null)
                        break;

                    if (!player.TryConsumeCommand())
                    {
                        await TrySendAsync(wire, new WireMessage
                        {
                            Type = MessageTypes.Error,
                            Text = "Rate limit exceeded. Disconnecting."
                        }, cancellationToken);
                        break;
                    }

                    switch (message.Type)
                    {
                        case MessageTypes.Ping:
                            await wire.SendAsync(new WireMessage
                            {
                                Type = MessageTypes.Pong,
                                Timestamp = message.Timestamp
                            }, cancellationToken);
                            break;

                        case MessageTypes.PlayersRequest:
                            List<PlayerInfo> current;
                            lock (_gate)
                                current = GetRosterUnsafe();

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
                                    Text = "Chat message must contain 1-280 printable characters."
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
                            await wire.SendAsync(new WireMessage
                            {
                                Type = MessageTypes.Error,
                                Text = "Unknown command type."
                            }, cancellationToken);
                            break;
                    }
                }
            }
            catch (Exception ex) when (
                ex is IOException or SocketException or ObjectDisposedException or
                    InvalidDataException or OperationCanceledException)
            {
                // Invalid input, network loss or regular shutdown: disconnect cleanly.
            }
            finally
            {
                if (ownsHandshakeSlot)
                    _pendingHandshakes.Release();

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

                lock (_gate)
                    _sockets.Remove(client);
            }
        }
    }

    private List<PlayerInfo> GetRosterUnsafe() =>
        _players.Values
            .OrderBy(p => p.Id)
            .Select(p => new PlayerInfo(p.Id, p.Name))
            .ToList();

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
        try
        {
            await wire.SendAsync(message, cancellationToken);
        }
        catch (Exception ex) when (ex is IOException or SocketException or
                                   ObjectDisposedException or OperationCanceledException)
        {
            // Another participant disconnected while the message was in flight.
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
        if (Interlocked.Exchange(ref _stopped, 1) != 0)
            return;

        _shutdown.Cancel();
        _listener.Stop();

        TcpClient[] sockets;
        lock (_gate)
            sockets = _sockets.ToArray();

        foreach (var socket in sockets)
            socket.Dispose();

        if (_acceptTask is not null)
        {
            try { await _acceptTask; }
            catch (OperationCanceledException) { }
        }

        Task[] handlers;
        lock (_gate)
            handlers = _handlers.ToArray();

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
        int id, string name, TcpClient socket, JsonLineConnection wire)
    {
        private readonly Queue<long> _recentCommands = new();
        private readonly object _rateGate = new();

        public int Id { get; } = id;
        public string Name { get; } = name;
        public TcpClient Socket { get; } = socket;
        public JsonLineConnection Wire { get; } = wire;

        public bool TryConsumeCommand()
        {
            lock (_rateGate)
            {
                var now = Environment.TickCount64;
                while (_recentCommands.Count > 0 && now - _recentCommands.Peek() > 5000)
                    _recentCommands.Dequeue();

                if (_recentCommands.Count >= 20)
                    return false;

                _recentCommands.Enqueue(now);
                return true;
            }
        }
    }
}
