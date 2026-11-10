namespace Fouroam.Server;

/// <summary>Operator commands exist ONLY in the host process, never over the wire.</summary>
public static class AdminConsole
{
    public static async Task RunAsync(ServerHost server, CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            Console.Write("fouroam> ");
            string? command = await Console.In.ReadLineAsync(token);
            if (command is null) return;
            if (!await ExecuteAsync(server, command, Console.WriteLine, token)) return;
        }
    }

    /// <returns>False if the console should stop the server.</returns>
    public static async Task<bool> ExecuteAsync(
        ServerHost server, string input, Action<string> output,
        CancellationToken token = default)
    {
        var parts = input.Trim().Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return true;

        switch (parts[0].ToLowerInvariant())
        {
            case "help":
                output("Commands: help, status, players, kick <id> [reason], ban <id> [reason],");
                output("          bans, unban <fingerprint-prefix>, say <message>, stop");
                break;

            case "status":
                output($"[STATUS] {server.SessionName}: {server.GetPlayers().Length}/{ServerHost.MaxPlayers} players");
                output($"[STATUS] TCP/TLS port {server.Port}, protocol v2");
                break;

            case "players":
                var players = server.GetPlayers();
                output($"[PLAYERS] {players.Length}/{ServerHost.MaxPlayers}");
                foreach (var player in players)
                    output($"#{player.Id} {player.Name} | identity {player.IdentityFingerprint[..12]}...");
                break;

            case "kick":
            case "ban":
                if (parts.Length < 2 || !int.TryParse(parts[1], out int id) || id < 1)
                {
                    output($"Usage: {parts[0]} <player-id> [reason]");
                    break;
                }

                var reason = parts.Length > 2 ? parts[2].Trim() : "Removed by host";
                if (reason.Length is < 1 or > 200 || reason.Any(char.IsControl))
                {
                    output("Reason must have 1-200 printable characters.");
                    break;
                }

                var done = parts[0].Equals("ban", StringComparison.OrdinalIgnoreCase)
                    ? await server.BanAsync(id, reason, token)
                    : await server.KickAsync(id, reason, token);
                output(done ? $"[ADMIN] {parts[0].ToLowerInvariant()} applied to #{id}"
                    : $"[ADMIN] Player #{id} not found.");
                break;

            case "bans":
                var bans = server.GetBans();
                output($"[BANS] {bans.Length} persistent client identities");
                foreach (var ban in bans) output(ban);
                break;

            case "unban":
                if (parts.Length != 2)
                {
                    output("Usage: unban <at least 8 hex characters of a unique fingerprint>");
                    break;
                }
                output(server.Unban(parts[1])
                    ? "[ADMIN] Ban removed."
                    : "[ADMIN] Ban not found, ambiguous or prefix too short.");
                break;

            case "say":
                if (input.Length < 5)
                {
                    output("Usage: say <message>");
                    break;
                }
                try
                {
                    await server.AnnounceAsync(input[4..].Trim(), token);
                    output("[ADMIN] Announcement delivered.");
                }
                catch (ArgumentException ex) { output($"[ADMIN] {ex.Message}"); }
                break;

            case "stop":
                output("[ADMIN] Stopping Fouroam Server.");
                return false;

            default:
                output("[ADMIN] Unknown command. Type 'help'.");
                break;
        }
        return true;
    }
}
