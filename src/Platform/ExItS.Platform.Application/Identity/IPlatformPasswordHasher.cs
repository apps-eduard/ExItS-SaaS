namespace ExItS.Platform.Application.Identity;

/// <summary>
/// Platform-owned password hashing. Implementations must never log plaintext or hash material.
/// </summary>
public interface IPlatformPasswordHasher
{
    string Algorithm { get; }

    string HashPassword(string password);

    bool VerifyHashedPassword(string hashedPassword, string providedPassword);
}
