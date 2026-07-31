namespace ExItS.Platform.Application.Identity;

public enum PlatformPasswordVerificationResult
{
    Failed = 0,
    Success = 1,
    SuccessRehashNeeded = 2
}

/// <summary>
/// Platform-owned password hashing. Implementations must never log plaintext or hash material.
/// </summary>
public interface IPlatformPasswordHasher
{
    /// <summary>Stable algorithm label stored alongside the hash (framework hasher identity).</summary>
    string Algorithm { get; }

    string HashPassword(string password);

    PlatformPasswordVerificationResult VerifyHashedPassword(string hashedPassword, string providedPassword);
}

/// <summary>Constant-time bootstrap shared-secret comparison (hashes compared; never log secrets).</summary>
public static class BootstrapSecretComparer
{
    public static bool EqualsConfigured(string configuredSecret, string? providedSecret)
    {
        if (string.IsNullOrEmpty(configuredSecret))
        {
            return false;
        }

        var expected = System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(configuredSecret));
        var actual = System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(providedSecret ?? string.Empty));
        return System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(expected, actual);
    }
}
