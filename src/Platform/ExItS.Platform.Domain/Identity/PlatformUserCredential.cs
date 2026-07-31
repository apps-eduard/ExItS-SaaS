using ExItS.Platform.Domain.Common;

namespace ExItS.Platform.Domain.Identity;

/// <summary>
/// Platform-owned local credential and lockout state for a single <see cref="PlatformUser"/>.
/// Stores only hashed secrets — never plaintext passwords, reset tokens, or MFA secrets.
/// Optional recovery email is for account recovery only (never grants roles/membership/entitlements).
/// </summary>
public sealed class PlatformUserCredential
{
    public const string AspNetCoreIdentityV3 = "ASPNET-CORE-IDENTITY-V3";

    /// <summary>Legacy label from the initial P13-WP02 custom PBKDF2 format (superseded; do not write new hashes).</summary>
    public const string Pbkdf2Sha256V1 = "PBKDF2-SHA256-V1";

    /// <summary>
    /// Credential exists for session security-stamp only; password login is impossible until a real hash is set.
    /// </summary>
    public const string ExternalNoPassword = "EXTERNAL-NO-PASSWORD";

    public PlatformUserId UserId { get; }
    public string PasswordHash { get; private set; }
    public string PasswordHashAlgorithm { get; private set; }
    public string SecurityStamp { get; private set; }
    public DateTimeOffset PasswordChangedAtUtc { get; private set; }
    public DateTimeOffset? EmailVerifiedAtUtc { get; private set; }
    public string? PendingRecoveryNormalizedEmail { get; private set; }
    public string? RecoveryNormalizedEmail { get; private set; }
    public DateTimeOffset? RecoveryEmailVerifiedAtUtc { get; private set; }
    public DateTimeOffset? RecoveryEmailPromptSkippedAtUtc { get; private set; }
    public int FailedAccessCount { get; private set; }
    public DateTimeOffset? LockoutEndUtc { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public bool SupportsPasswordLogin =>
        !string.Equals(PasswordHashAlgorithm, ExternalNoPassword, StringComparison.Ordinal);

    public bool HasVerifiedRecoveryEmail =>
        !string.IsNullOrWhiteSpace(RecoveryNormalizedEmail) && RecoveryEmailVerifiedAtUtc is not null;

    public bool NeedsRecoveryEmailPrompt =>
        !SupportsPasswordLogin
        && !HasVerifiedRecoveryEmail
        && RecoveryEmailPromptSkippedAtUtc is null;

    private PlatformUserCredential(
        PlatformUserId userId,
        string passwordHash,
        string passwordHashAlgorithm,
        string securityStamp,
        DateTimeOffset passwordChangedAtUtc,
        DateTimeOffset? emailVerifiedAtUtc,
        string? pendingRecoveryNormalizedEmail,
        string? recoveryNormalizedEmail,
        DateTimeOffset? recoveryEmailVerifiedAtUtc,
        DateTimeOffset? recoveryEmailPromptSkippedAtUtc,
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
        PendingRecoveryNormalizedEmail = pendingRecoveryNormalizedEmail;
        RecoveryNormalizedEmail = recoveryNormalizedEmail;
        RecoveryEmailVerifiedAtUtc = recoveryEmailVerifiedAtUtc;
        RecoveryEmailPromptSkippedAtUtc = recoveryEmailPromptSkippedAtUtc;
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
            pendingRecoveryNormalizedEmail: null,
            recoveryNormalizedEmail: null,
            recoveryEmailVerifiedAtUtc: null,
            recoveryEmailPromptSkippedAtUtc: null,
            failedAccessCount: 0,
            lockoutEndUtc: null,
            createdAtUtc: utcNow,
            updatedAtUtc: utcNow);
    }

    public static PlatformUserCredential CreateForExternalLogin(
        PlatformUserId userId,
        DateTimeOffset utcNow,
        bool emailVerified)
    {
        ArgumentNullException.ThrowIfNull(userId);
        EnsureUtc(utcNow);

        return new PlatformUserCredential(
            userId,
            passwordHash: "!",
            passwordHashAlgorithm: ExternalNoPassword,
            NewSecurityStamp(),
            utcNow,
            emailVerifiedAtUtc: emailVerified ? utcNow : null,
            pendingRecoveryNormalizedEmail: null,
            recoveryNormalizedEmail: null,
            recoveryEmailVerifiedAtUtc: null,
            recoveryEmailPromptSkippedAtUtc: null,
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
        DateTimeOffset updatedAtUtc,
        string? pendingRecoveryNormalizedEmail = null,
        string? recoveryNormalizedEmail = null,
        DateTimeOffset? recoveryEmailVerifiedAtUtc = null,
        DateTimeOffset? recoveryEmailPromptSkippedAtUtc = null) =>
        new(
            userId,
            passwordHash,
            passwordHashAlgorithm,
            securityStamp,
            passwordChangedAtUtc,
            emailVerifiedAtUtc,
            pendingRecoveryNormalizedEmail,
            recoveryNormalizedEmail,
            recoveryEmailVerifiedAtUtc,
            recoveryEmailPromptSkippedAtUtc,
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

    public void BeginRecoveryEmailChange(string pendingNormalizedEmail, DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        if (string.IsNullOrWhiteSpace(pendingNormalizedEmail))
        {
            throw new DomainException(DomainErrorCodes.InvalidEmail, "Recovery email cannot be blank.");
        }

        PendingRecoveryNormalizedEmail = pendingNormalizedEmail.Trim().ToLowerInvariant();
        RecoveryEmailPromptSkippedAtUtc = null;
        UpdatedAtUtc = utcNow;
    }

    public void ConfirmRecoveryEmail(DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        if (string.IsNullOrWhiteSpace(PendingRecoveryNormalizedEmail))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidAccountStatusTransition,
                "No pending recovery email to confirm.");
        }

        RecoveryNormalizedEmail = PendingRecoveryNormalizedEmail;
        RecoveryEmailVerifiedAtUtc = utcNow;
        PendingRecoveryNormalizedEmail = null;
        RecoveryEmailPromptSkippedAtUtc = null;
        UpdatedAtUtc = utcNow;
    }

    public void ClearRecoveryEmail(DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        PendingRecoveryNormalizedEmail = null;
        RecoveryNormalizedEmail = null;
        RecoveryEmailVerifiedAtUtc = null;
        UpdatedAtUtc = utcNow;
    }

    public void SkipRecoveryEmailPrompt(DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        RecoveryEmailPromptSkippedAtUtc = utcNow;
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

    private static string NewSecurityStamp() => Convert.ToHexString(Guid.NewGuid().ToByteArray());

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
