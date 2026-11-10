using System.Security.Cryptography;
using System.Text.Json;

namespace Fouroam.Server;

/// <summary>Private config stores a salted PBKDF2 verifier, never the password itself.</summary>
public sealed record ServerSettings
{
    public string SessionName { get; init; } = string.Empty;
    public string PasswordSalt { get; init; } = string.Empty;
    public string PasswordHash { get; init; } = string.Empty;
    public int Iterations { get; init; } = 210_000;

    public static ServerSettings Create(string sessionName, string password)
    {
        sessionName = sessionName.Trim();
        if (sessionName.Length is < 2 or > 48 || sessionName.Any(char.IsControl))
            throw new ArgumentException("Session name must be 2-48 printable characters.");
        if (password.Length is < 4 or > 128)
            throw new ArgumentException("Session password must be 4-128 characters.");

        byte[] salt = RandomNumberGenerator.GetBytes(16);
        byte[] derived = Rfc2898DeriveBytes.Pbkdf2(
            password, salt, 210_000, HashAlgorithmName.SHA256, 32);
        return new ServerSettings
        {
            SessionName = sessionName,
            PasswordSalt = Convert.ToBase64String(salt),
            PasswordHash = Convert.ToBase64String(derived)
        };
    }

    public bool Verify(string? password)
    {
        if (password is null || password.Length is < 4 or > 128) return false;
        byte[] salt = Convert.FromBase64String(PasswordSalt);
        byte[] expected = Convert.FromBase64String(PasswordHash);
        byte[] actual = Rfc2898DeriveBytes.Pbkdf2(
            password, salt, Iterations, HashAlgorithmName.SHA256, expected.Length);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }

    public static string ConfigPath(string dataDir) =>
        Path.Combine(dataDir, "server.local.json");

    public void Save(string dataDir)
    {
        Directory.CreateDirectory(dataDir);
        var path = ConfigPath(dataDir);
        if (File.Exists(path))
            throw new IOException("A configuration already exists. Refusing to overwrite it.");
        var json = JsonSerializer.Serialize(this,
            new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true });
        File.WriteAllText(path, json + Environment.NewLine);
        if (!OperatingSystem.IsWindows())
            File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
    }

    public static ServerSettings Load(string dataDir)
    {
        string path = ConfigPath(dataDir);
        if (!File.Exists(path))
            throw new FileNotFoundException(
                "Missing private configuration. Run 'dotnet run --project src/Fouroam.Server -- init' first.",
                path);
        var settings = JsonSerializer.Deserialize<ServerSettings>(
            File.ReadAllText(path), new JsonSerializerOptions(JsonSerializerDefaults.Web));
        if (settings is null || settings.SessionName.Length is < 2 or > 48 ||
            settings.Iterations is < 210_000 or > 2_000_000 ||
            Convert.FromBase64String(settings.PasswordSalt).Length != 16 ||
            Convert.FromBase64String(settings.PasswordHash).Length != 32)
            throw new InvalidDataException("Invalid local server configuration.");
        return settings;
    }
}
