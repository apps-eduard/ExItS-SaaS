using System.Security.Cryptography;
using ExItS.Platform.Application.Identity;
using ExItS.Platform.Domain.Identity;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace ExItS.Platform.Infrastructure.Identity;

/// <summary>
/// PBKDF2-SHA256 password hasher (100k iterations). Not ASP.NET Identity.
/// Format: {base64(salt)}.{base64(subkey)}
/// </summary>
public sealed class Pbkdf2PlatformPasswordHasher : IPlatformPasswordHasher
{
    private const int SaltSize = 16;
    private const int KeySize = 32;
    private const int Iterations = 100_000;

    public string Algorithm => PlatformUserCredential.Pbkdf2Sha256V1;

    public string HashPassword(string password)
    {
        ArgumentException.ThrowIfNullOrEmpty(password);
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var subkey = KeyDerivation.Pbkdf2(
            password,
            salt,
            KeyDerivationPrf.HMACSHA256,
            Iterations,
            KeySize);
        return $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(subkey)}";
    }

    public bool VerifyHashedPassword(string hashedPassword, string providedPassword)
    {
        if (string.IsNullOrWhiteSpace(hashedPassword) || providedPassword is null)
        {
            return false;
        }

        var parts = hashedPassword.Split('.', 2);
        if (parts.Length != 2)
        {
            return false;
        }

        byte[] salt;
        byte[] expected;
        try
        {
            salt = Convert.FromBase64String(parts[0]);
            expected = Convert.FromBase64String(parts[1]);
        }
        catch (FormatException)
        {
            return false;
        }

        if (salt.Length != SaltSize || expected.Length != KeySize)
        {
            return false;
        }

        var actual = KeyDerivation.Pbkdf2(
            providedPassword,
            salt,
            KeyDerivationPrf.HMACSHA256,
            Iterations,
            KeySize);

        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }
}
