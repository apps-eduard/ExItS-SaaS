using System.Security.Cryptography;
using System.Text;

namespace ExItS.PinoyBusinessPOS.Application.Offline;

/// <summary>PBKDF2-SHA256 salted PIN verifier. Never stores or logs the raw PIN.</summary>
public static class OfflinePinHasher
{
    public const string Algorithm = "PBKDF2-SHA256";
    public const int SaltSizeBytes = 16;
    public const int HashSizeBytes = 32;

    public static bool IsValidPinFormat(string? pin, int minLength)
    {
        if (string.IsNullOrEmpty(pin) || pin.Length < minLength)
        {
            return false;
        }

        foreach (var ch in pin)
        {
            if (ch is < '0' or > '9')
            {
                return false;
            }
        }

        return true;
    }

    public static OfflinePinVerifier Create(string pin, int iterations, Guid? userId = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(pin);
        if (iterations < 10_000)
        {
            throw new ArgumentOutOfRangeException(nameof(iterations), "PIN hash iterations are too low.");
        }

        var salt = RandomNumberGenerator.GetBytes(SaltSizeBytes);
        var hash = Pbkdf2(pin, salt, iterations);
        return new OfflinePinVerifier(
            Algorithm,
            iterations,
            Convert.ToBase64String(salt),
            Convert.ToBase64String(hash),
            FailedAttempts: 0,
            LockedUntilUtc: null,
            UserId: userId);
    }

    public static bool Verify(string pin, OfflinePinVerifier verifier)
    {
        ArgumentNullException.ThrowIfNull(verifier);
        if (!string.Equals(verifier.Algorithm, Algorithm, StringComparison.Ordinal))
        {
            return false;
        }

        byte[] salt;
        byte[] expected;
        try
        {
            salt = Convert.FromBase64String(verifier.SaltBase64);
            expected = Convert.FromBase64String(verifier.HashBase64);
        }
        catch (FormatException)
        {
            return false;
        }

        var actual = Pbkdf2(pin, salt, verifier.Iterations);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }

    private static byte[] Pbkdf2(string pin, byte[] salt, int iterations)
    {
        var passwordBytes = Encoding.UTF8.GetBytes(pin);
        try
        {
            return Rfc2898DeriveBytes.Pbkdf2(
                passwordBytes,
                salt,
                iterations,
                HashAlgorithmName.SHA256,
                HashSizeBytes);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(passwordBytes);
        }
    }
}
