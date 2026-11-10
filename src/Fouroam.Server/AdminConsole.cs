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
        var command = input.Trim();
        // Friendly optional slash, familiar from game server consoles.
        if (command.StartsWith('/')) command = command[1..].TrimStart();

        var parts = command.Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return true;

        switch (parts[0].ToLowerInvariant())
        {
            case "help":
                output("Commands (optional /): help, status, players, kick <id|name> [reason],");
                output("          ban <id|name> [reason], bans, unban <fingerprint-prefix>,");
                output("          say <message>, stop");
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
                var arguments = command[parts[0].Length..].Trim();
                if (arguments.Length == 0)
                {
                    output($"Usage: {parts[0]} <player-id|player-name> [reason]");
                    break;
                }

                int id;
                string reason;
                var firstSpace = arguments.IndexOf(' ');
                string firstToken = firstSpace >= 0 ? arguments[..firstSpace] : arguments;

                if (int.TryParse(firstToken.TrimStart('#'), out int numericId) && numericId > 0)
                {
                    id = numericId;
                    reason = firstSpace >= 0 ? arguments[(firstSpace + 1)..].Trim() : "";
                }
                else
                {
                    // Match full names, including spaces. A longer player name wins
                    // over its shorter prefix, and duplicate exact names are ambiguous.
                    var matches = server.GetPlayers()
                        .Where(p =>
                            arguments.StartsWith(p.Name, StringComparison.OrdinalIgnoreCase) &&
                            (arguments.Length == p.Name.Length || arguments[p.Name.Length] == ' '))
                        .OrderByDescending(p => p.Name.Length)
                        .ToArray();

                    if (matches.Length == 0)
                    {
                        output("[ADMIN] Player not found. Type 'players' for IDs and names.");
                        break;
                    }
                    if (matches.Length > 1 && matches[0].Name.Length == matches[1].Name.Length)
                    {
                        output("[ADMIN] Ambiguous player name. Use the numeric ID from 'players'.");
                        break;
                    }

                    id = matches[0].Id;
                    reason = arguments[matches[0].Name.Length..].Trim();
                }

                if (reason.Length == 0) reason = "Removed by host";
                if (reason.Length > 200 || reason.Any(char.IsControl))
                {
                    output("[ADMIN] Reason must have 1-200 printable characters.");
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
                var message = command[parts[0].Length..].Trim();
                if (message.Length == 0)
                {
                    output("Usage: say <message>");
                    break;
                }
                try
                {
                    await server.AnnounceAsync(message, token);
                    output("[ADMIN] Announcement delivered.");
                }
                catch (ArgumentException ex) { output($"[ADMIN] {ex.Message}"); }
                break;

            case "stop":
                output("[ADMIN] Stopping Fouroam Server.");
                return false;

            default:
                output("[ADMIN] Unknown command. Type 'help' (or '/help').");
                break;
        }
        return true;
    }
}
