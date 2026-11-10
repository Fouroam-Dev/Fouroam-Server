using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Fouroam.Shared;

public static class TlsFingerprint
{
    public static string Get(X509Certificate certificate) =>
        Convert.ToHexString(SHA256.HashData(certificate.GetRawCertData()));

    public static bool Matches(X509Certificate? certificate, string expectedHex)
    {
        if (certificate is null || expectedHex.Length != 64) return false;
        try
        {
            var expected = Convert.FromHexString(expectedHex);
            var actual = SHA256.HashData(certificate.GetRawCertData());
            return CryptographicOperations.FixedTimeEquals(expected, actual);
        }
        catch (FormatException) { return false; }
    }

    public static bool TryNormalize(string? input, out string fingerprint)
    {
        fingerprint = (input ?? "").Replace(":", "").Replace("-", "")
            .Trim().ToUpperInvariant();
        return fingerprint.Length == 64 && fingerprint.All(Uri.IsHexDigit);
    }
}
