using System.Security.Cryptography;

namespace Fouroam.Server;

/// <summary>
/// First-run private setup. Input delegates make the wizard independently testable.
/// Neither the password nor the certificate private key is printed or committed.
/// </summary>
public static class ServerSetup
{
    public static bool NeedsInitialization(string dataDir) =>
        !File.Exists(ServerSettings.ConfigPath(dataDir));

    public static bool TryInitialize(
        string dataDir,
        Func<string?> readSessionName,
        Func<string> readPassword,
        Func<string> readConfirmation,
        Action<string> output,
        Action<string> error)
    {
        ArgumentNullException.ThrowIfNull(dataDir);

        if (!NeedsInitialization(dataDir))
        {
            error("[SETUP] A private session already exists in this directory. Refusing to replace it.");
            return false;
        }

        try
        {
            string? nameInput = readSessionName();
            string name = string.IsNullOrWhiteSpace(nameInput)
                ? "Fouroam Private Session" : nameInput;

            string password = readPassword();
            if (password.Length < 12)
                output("[SECURITY] Short password: use only on a trusted private LAN/VPN. A longer password is recommended.");

            string confirmation = readConfirmation();
            if (password != confirmation)
            {
                error("[SETUP] Passwords do not match.");
                return false;
            }

            ServerSettings settings = ServerSettings.Create(name, password);
            settings.Save(dataDir);
            using var certificate = LocalCertificate.LoadOrCreate(dataDir);

            output($"[SETUP] Private session: {settings.SessionName}");
            output($"[SETUP] Config saved in {dataDir}");
            output($"[SETUP] TLS SHA-256 fingerprint: {LocalCertificate.Fingerprint(certificate)}");
            output("[SETUP] Never upload the .fouroam directory to GitHub.");
            return true;
        }
        catch (Exception ex) when (ex is IOException or ArgumentException or
            UnauthorizedAccessException or CryptographicException)
        {
            error($"[SETUP ERROR] {ex.Message}");
            return false;
        }
    }
}
