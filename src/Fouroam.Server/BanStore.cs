using System.Text.Json;

namespace Fouroam.Server;

/// <summary>
/// Bans are keyed to a hash of a client-controlled random token, not to
/// a temporary player number or IP address. No central account verification.
/// </summary>
public sealed class BanStore
{
    private readonly string _path;
    private readonly object _gate = new();
    private readonly HashSet<string> _bans;

    public BanStore(string path)
    {
        _path = Path.GetFullPath(path);
        _bans = File.Exists(_path)
            ? new HashSet<string>(JsonSerializer.Deserialize<string[]>(File.ReadAllText(_path))
                ?? throw new InvalidDataException("Invalid ban file."), StringComparer.Ordinal)
            : new HashSet<string>(StringComparer.Ordinal);

        if (_bans.Any(b => b.Length != 64 || !b.All(Uri.IsHexDigit)))
            throw new InvalidDataException("Ban file contains invalid fingerprints.");
    }

    public bool Contains(string fingerprint)
    {
        lock (_gate) return _bans.Contains(fingerprint);
    }

    public string[] Snapshot()
    {
        lock (_gate) return _bans.Order(StringComparer.Ordinal).ToArray();
    }

    public bool Add(string fingerprint)
    {
        lock (_gate)
        {
            if (!_bans.Add(fingerprint)) return false;
            SaveUnsafe();
            return true;
        }
    }

    public bool RemoveByPrefix(string prefix)
    {
        prefix = prefix.Trim().ToUpperInvariant();
        if (prefix.Length < 8 || !prefix.All(Uri.IsHexDigit)) return false;
        lock (_gate)
        {
            var matches = _bans.Where(b => b.StartsWith(prefix, StringComparison.Ordinal)).ToArray();
            if (matches.Length != 1) return false;
            _bans.Remove(matches[0]);
            SaveUnsafe();
            return true;
        }
    }

    private void SaveUnsafe()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        string temp = _path + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(_bans.Order(StringComparer.Ordinal).ToArray(),
            new JsonSerializerOptions { WriteIndented = true }));
        if (!OperatingSystem.IsWindows())
            File.SetUnixFileMode(temp, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        File.Move(temp, _path, overwrite: true);
    }
}
