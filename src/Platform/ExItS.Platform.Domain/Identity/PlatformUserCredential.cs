using ExItS.Platform.Domain.Common;

namespace ExItS.Platform.Domain.Identity;

/// <summary>
/// Platform-owned local credential and lockout state for a single <see cref="PlatformUser"/>.
/// Stores only hashed secrets — never plaintext passwords, reset tokens, or MFA secrets.
/// MFA factor secrets belong in a future dedicated store behind <c>IPlatformMfaFactorStore</c> (readiness only in P13-WP07).
/// </summary>
public sealed class PlatformUserCredential
{
    public const string AspNetCoreIdentityV3 = "ASPNET-CORE-IDENTITY-V3";

    /// <summary>Legacy label from the initial P13-WP02 custom PBKDF2 format (superseded; do not write new hashes).</summary>
    public const string Pbkdf2Sha256V1 = "PBKDF2-SHA256-V1";

    public PlatformUserId UserId { get; }
    public string PasswordHash { get; private set; }
    public string PasswordHashAlgorithm { get; private set; }
    public string SecurityStamp { get; private set; }
    public DateTimeOffset PasswordChangedAtUtc { get; private set; }
    public DateTimeOffset? EmailVerifiedAtUtc { get; private set; }
    public int FailedAccessCount { get; private set; }
    public DateTimeOffset? LockoutEndUtc { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    private PlatformUserCredential(
        PlatformUserId userId,
        string passwordHash,
        string passwordHashAlgorithm,
        string securityStamp,
        DateTimeOffset passwordChangedAtUtc,
        DateTimeOffset? emailVerifiedAtUtc,
        int failedAccessCount,
        DateTimeOffset? lockoutEndUtc,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc)
    {
        UserId = userId;
        PasswordHash = passwordHash;
        PasswordHashAlgorithm = passwordHashAlgorithm;
        SecurityStamp = securityStamp;
        PasswordChangedAtUtc = passwordChangedAtUtc;
        EmailVerifiedAtUtc = emailVerifiedAtUtc;
        FailedAccessCount = failedAccessCount;
        LockoutEndUtc = lockoutEndUtc;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
    }

    public static PlatformUserCredential Create(
        PlatformUserId userId,
        string passwordHash,
        string passwordHashAlgorithm,
        DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(userId);
        EnsureUtc(utcNow);
        var hash = NormalizeHash(passwordHash);
        var algorithm = NormalizeAlgorithm(passwordHashAlgorithm);

        return new PlatformUserCredential(
            userId,
            hash,
            algorithm,
            NewSecurityStamp(),
            utcNow,
            emailVerifiedAtUtc: null,
            failedAccessCount: 0,
            lockoutEndUtc: null,
            createdAtUtc: utcNow,
            updatedAtUtc: utcNow);
    }

    public static PlatformUserCredential Rehydrate(
        PlatformUserId userId,
        string passwordHash,
        string passwordHashAlgorithm,
        string securityStamp,
        DateTimeOffset passwordChangedAtUtc,
        DateTimeOffset? emailVerifiedAtUtc,
        int failedAccessCount,
        DateTimeOffset? lockoutEndUtc,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc) =>
        new(
            userId,
            passwordHash,
            passwordHashAlgorithm,
            securityStamp,
            passwordChangedAtUtc,
            emailVerifiedAtUtc,
            failedAccessCount,
            lockoutEndUtc,
            createdAtUtc,
            updatedAtUtc);

    public void ReplacePasswordHash(string passwordHash, string passwordHashAlgorithm, DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        PasswordHash = NormalizeHash(passwordHash);
        PasswordHashAlgorithm = NormalizeAlgorithm(passwordHashAlgorithm);
        PasswordChangedAtUtc = utcNow;
        SecurityStamp = NewSecurityStamp();
        FailedAccessCount = 0;
        LockoutEndUtc = null;
        UpdatedAtUtc = utcNow;
    }

    public void MarkEmailVerified(DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        EmailVerifiedAtUtc = utcNow;
        UpdatedAtUtc = utcNow;
    }

    public void ClearEmailVerification(DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        EmailVerifiedAtUtc = null;
        UpdatedAtUtc = utcNow;
    }

    public bool IsLockedOut(DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        return LockoutEndUtc is not null && LockoutEndUtc > utcNow;
    }

    public void RegisterFailedAccess(int maxFailedAttempts, TimeSpan lockoutDuration, DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        if (maxFailedAttempts < 1)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidAccountStatusTransition,
                "Max failed attempts must be at least 1.");
        }

        if (lockoutDuration <= TimeSpan.Zero)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidAccountStatusTransition,
                "Lockout duration must be positive.");
        }

        if (IsLockedOut(utcNow))
        {
            return;
        }

        FailedAccessCount++;
        if (FailedAccessCount >= maxFailedAttempts)
        {
            LockoutEndUtc = utcNow.Add(lockoutDuration);
            FailedAccessCount = 0;
        }

        UpdatedAtUtc = utcNow;
    }

    public void RegisterSuccessfulAccess(DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        FailedAccessCount = 0;
        LockoutEndUtc = null;
        UpdatedAtUtc = utcNow;
    }

    public void Unlock(DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        FailedAccessCount = 0;
        LockoutEndUtc = null;
        UpdatedAtUtc = utcNow;
    }

    private static string NewSecurityStamp() => Guid.NewGuid().ToString("N");

    private static string NormalizeHash(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new DomainException(DomainErrorCodes.InvalidAccountStatusTransition, "Password hash is required.");
        }

        var trimmed = passwordHash.Trim();
        if (trimmed.Length > 512)
        {
            throw new DomainException(DomainErrorCodes.InvalidAccountStatusTransition, "Password hash is too long.");
        }

        return trimmed;
    }

    private static string NormalizeAlgorithm(string algorithm)
    {
        if (string.IsNullOrWhiteSpace(algorithm))
        {
            throw new DomainException(DomainErrorCodes.InvalidAccountStatusTransition, "Password hash algorithm is required.");
        }

        var trimmed = algorithm.Trim();
        if (trimmed.Length > 64)
        {
            throw new DomainException(DomainErrorCodes.InvalidAccountStatusTransition, "Password hash algorithm is too long.");
        }

        return trimmed;
    }

    private static void EnsureUtc(DateTimeOffset value)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new DomainException(DomainErrorCodes.InvalidUtcTimestamp, "Credential timestamps must be UTC.");
        }
    }
}
