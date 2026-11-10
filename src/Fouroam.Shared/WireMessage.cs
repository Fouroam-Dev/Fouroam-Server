namespace Fouroam.Shared;

public static class MessageTypes
{
    public const string Hello = "hello";
    public const string Welcome = "welcome";
    public const string ServerFull = "server_full";
    public const string PlayerJoined = "player_joined";
    public const string PlayerLeft = "player_left";
    public const string PlayersRequest = "players_request";
    public const string Players = "players";
    public const string Chat = "chat";
    public const string Ping = "ping";
    public const string Pong = "pong";
    public const string Error = "error";
}

public sealed record PlayerInfo(int Id, string Name);

public sealed record WireMessage
{
    public string Type { get; init; } = string.Empty;
    public int? ProtocolVersion { get; init; }
    public int? PlayerId { get; init; }
    public string? Name { get; init; }
    public string? Text { get; init; }
    public int? PlayerCount { get; init; }
    public int? MaxPlayers { get; init; }
    public IReadOnlyList<PlayerInfo>? Players { get; init; }
    public long? Timestamp { get; init; }
}
