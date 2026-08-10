using System.Security.Cryptography;
using System.Text;

namespace ExItS.PinoyBusinessPOS.Application.Catalog;

/// <summary>
/// Fixed-time comparison for the Platform support shared API key.
/// Blank configured key always denies.
/// </summary>
public static class PlatformSupportApiKeyGuard
{
    public const string HeaderName = "X-ExItS-Platform-Support-Key";

    public static bool IsAuthorized(string? configuredApiKey, string? providedApiKey)
    {
        if (string.IsNullOrEmpty(configuredApiKey))
        {
            return false;
        }

        if (providedApiKey is null)
        {
            return false;
        }

        var expected = Encoding.UTF8.GetBytes(configuredApiKey);
        var actual = Encoding.UTF8.GetBytes(providedApiKey);
        if (expected.Length != actual.Length)
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(expected, actual);
    }
}
