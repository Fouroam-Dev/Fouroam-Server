using System.Security.Cryptography;

namespace Fouroam.Shared;

/// <summary>
/// Random per-installation bearer identity. This is NOT an account or a
/// hardware fingerprint. Rotating the local file bypasses identity bans.
/// </summary>
public static class ClientIdentity
{
    public static string NewToken() => Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

    public static bool TryHashToken(string? token, out string fingerprint)
    {
        fingerprint = string.Empty;
        if (token is null || token.Length != 64) return false;
        try
        {
            byte[] raw = Convert.FromHexString(token);
            fingerprint = Convert.ToHexString(SHA256.HashData(raw));
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    public static string LoadOrCreate(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        string fullPath = Path.GetFullPath(path);
        if (File.Exists(fullPath))
        {
            var existing = File.ReadAllText(fullPath).Trim();
            if (TryHashToken(existing, out _)) return existing;
            throw new InvalidDataException("Client identity file is invalid; do not overwrite it automatically.");
        }

        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        string token = NewToken();
        using (var file = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        using (var writer = new StreamWriter(file))
            writer.WriteLine(token);

        if (!OperatingSystem.IsWindows())
            File.SetUnixFileMode(fullPath, UnixFileMode.UserRead | UnixFileMode.UserWrite);

        return token;
    }
}
