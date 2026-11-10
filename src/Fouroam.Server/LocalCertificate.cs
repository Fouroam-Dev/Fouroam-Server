using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Fouroam.Server;

public static class LocalCertificate
{
    public static string CertificatePath(string dataDir) =>
        Path.Combine(dataDir, "server-cert.pfx");

    public static X509Certificate2 LoadOrCreate(string dataDir)
    {
        Directory.CreateDirectory(dataDir);
        string path = CertificatePath(dataDir);
        if (!File.Exists(path))
        {
            using RSA rsa = RSA.Create(3072);
            var request = new CertificateRequest(
                "CN=Fouroam Private Server", rsa,
                HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            request.CertificateExtensions.Add(
                new X509BasicConstraintsExtension(false, false, 0, true));
            request.CertificateExtensions.Add(new X509KeyUsageExtension(
                X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, true));
            using var generated = request.CreateSelfSigned(
                DateTimeOffset.UtcNow.AddMinutes(-5), DateTimeOffset.UtcNow.AddYears(2));
            using (var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                var bytes = generated.Export(X509ContentType.Pfx);
                file.Write(bytes);
            }
            if (!OperatingSystem.IsWindows())
                File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }

        // Windows Schannel needs a provider-backed private key; ephemeral PFX keys may
        // be unavailable to its TLS credential acquisition. Other platforms can
        // load the key ephemerally. The PFX remains private local server state.
        var flags = OperatingSystem.IsWindows()
            ? X509KeyStorageFlags.UserKeySet | X509KeyStorageFlags.PersistKeySet
            : X509KeyStorageFlags.EphemeralKeySet;
        var loaded = X509CertificateLoader.LoadPkcs12FromFile(path, null, flags);
        if (!loaded.HasPrivateKey)
        {
            loaded.Dispose();
            throw new CryptographicException("TLS certificate does not have a private key.");
        }
        return loaded;
    }

    public static string Fingerprint(X509Certificate certificate) =>
        Convert.ToHexString(SHA256.HashData(certificate.GetRawCertData()));

    public static bool MatchesFingerprint(X509Certificate? certificate, string expected)
    {
        if (certificate is null || expected.Length != 64) return false;
        try
        {
            byte[] wanted = Convert.FromHexString(expected);
            byte[] actual = SHA256.HashData(certificate.GetRawCertData());
            return CryptographicOperations.FixedTimeEquals(wanted, actual);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
