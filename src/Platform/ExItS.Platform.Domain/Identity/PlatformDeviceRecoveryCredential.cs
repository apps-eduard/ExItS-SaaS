using ExItS.Platform.Domain.Common;

namespace ExItS.Platform.Domain.Identity;

/// <summary>
/// Per-user, per-installation-device rotating credential used by MAUI to obtain a fresh
/// access token after local PIN verification. Only the hash is persisted server-side.
/// </summary>
public sealed class PlatformDeviceRecoveryCredential
{
    public PlatformDeviceRecoveryCredentialId Id { get; }
    public PlatformUserId UserId { get; }
    public string InstallationDeviceId { get; }
    public string TokenHash { get; }
    public string SecurityStampAtIssue { get; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset LastUsedAtUtc { get; private set; }
    public DateTimeOffset IdleExpiresAtUtc { get; private set; }
    public DateTimeOffset AbsoluteExpiresAtUtc { get; }
    public DateTimeOffset? RevokedAtUtc { get; private set; }
    public int RotationVersion { get; private set; }

    private PlatformDeviceRecoveryCredential(
        PlatformDeviceRecoveryCredentialId id,
        PlatformUserId userId,
        string installationDeviceId,
        string tokenHash,
        string securityStampAtIssue,
        DateTimeOffset createdAtUtc,
        DateTimeOffset lastUsedAtUtc,
        DateTimeOffset idleExpiresAtUtc,
        DateTimeOffset absoluteExpiresAtUtc,
        DateTimeOffset? revokedAtUtc,
        int rotationVersion)
    {
        Id = id;
        UserId = userId;
        InstallationDeviceId = installationDeviceId;
        TokenHash = tokenHash;
        SecurityStampAtIssue = securityStampAtIssue;
        CreatedAtUtc = createdAtUtc;
        LastUsedAtUtc = lastUsedAtUtc;
        IdleExpiresAtUtc = idleExpiresAtUtc;
        AbsoluteExpiresAtUtc = absoluteExpiresAtUtc;
        RevokedAtUtc = revokedAtUtc;
        RotationVersion = rotationVersion;
    }

    public static PlatformDeviceRecoveryCredential Create(
        PlatformUserId userId,
        string installationDeviceId,
        string tokenHash,
        string securityStampAtIssue,
        DateTimeOffset utcNow,
        TimeSpan idleLifetime,
        TimeSpan absoluteLifetime,
        PlatformDeviceRecoveryCredentialId? id = null,
        int rotationVersion = 1)
    {
        ArgumentNullException.ThrowIfNull(userId);
        EnsureUtc(utcNow);
        if (idleLifetime <= TimeSpan.Zero || absoluteLifetime <= TimeSpan.Zero)
        {
            throw new DomainException(DomainErrorCodes.InvalidUtcTimestamp, "Recovery credential lifetimes must be positive.");
        }

        if (string.IsNullOrWhiteSpace(installationDeviceId) || installationDeviceId.Length > 128)
        {
            throw new DomainException(DomainErrorCodes.InvalidPosDeviceInstallationId, "Installation device id is invalid.");
        }

        if (string.IsNullOrWhiteSpace(tokenHash) || tokenHash.Length > 128)
        {
            throw new DomainException(DomainErrorCodes.InvalidAccountStatusTransition, "Recovery credential hash is invalid.");
        }

        if (string.IsNullOrWhiteSpace(securityStampAtIssue) || securityStampAtIssue.Length > 64)
        {
            throw new DomainException(DomainErrorCodes.InvalidAccountStatusTransition, "Security stamp is invalid.");
        }

        return new PlatformDeviceRecoveryCredential(
            id ?? PlatformDeviceRecoveryCredentialId.New(),
            userId,
            installationDeviceId.Trim(),
            tokenHash.Trim(),
            securityStampAtIssue.Trim(),
            utcNow,
            utcNow,
            utcNow.Add(idleLifetime),
            utcNow.Add(absoluteLifetime),
            revokedAtUtc: null,
            rotationVersion);
    }

    public static PlatformDeviceRecoveryCredential Rehydrate(
        PlatformDeviceRecoveryCredentialId id,
        PlatformUserId userId,
        string installationDeviceId,
        string tokenHash,
        string securityStampAtIssue,
        DateTimeOffset createdAtUtc,
        DateTimeOffset lastUsedAtUtc,
        DateTimeOffset idleExpiresAtUtc,
        DateTimeOffset absoluteExpiresAtUtc,
        DateTimeOffset? revokedAtUtc,
        int rotationVersion) =>
        new(
            id,
            userId,
            installationDeviceId,
            tokenHash,
            securityStampAtIssue,
            createdAtUtc,
            lastUsedAtUtc,
            idleExpiresAtUtc,
            absoluteExpiresAtUtc,
            revokedAtUtc,
            rotationVersion);

    public bool IsActive(DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        return RevokedAtUtc is null
               && IdleExpiresAtUtc > utcNow
               && AbsoluteExpiresAtUtc > utcNow;
    }

    public bool IsIdleExpired(DateTimeOffset utcNow) =>
        RevokedAtUtc is null && IdleExpiresAtUtc <= utcNow && AbsoluteExpiresAtUtc > utcNow;

    public bool IsAbsolutelyExpired(DateTimeOffset utcNow) =>
        RevokedAtUtc is null && AbsoluteExpiresAtUtc <= utcNow;

    public void RecordSuccessfulExchange(DateTimeOffset utcNow, TimeSpan idleLifetime)
    {
        EnsureUtc(utcNow);
        if (RevokedAtUtc is not null)
        {
            throw new DomainException(DomainErrorCodes.RecoveryCredentialInvalid, "Recovery credential is not active.");
        }

        if (AbsoluteExpiresAtUtc <= utcNow)
        {
            throw new DomainException(DomainErrorCodes.RecoveryCredentialExpired, "Recovery credential has expired.");
        }

        LastUsedAtUtc = utcNow;
        var refreshedIdle = utcNow.Add(idleLifetime);
        IdleExpiresAtUtc = refreshedIdle > AbsoluteExpiresAtUtc ? AbsoluteExpiresAtUtc : refreshedIdle;
    }

    public void Revoke(DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        if (RevokedAtUtc is not null)
        {
            return;
        }

        RevokedAtUtc = utcNow;
    }

    /// <summary>
    /// Creates the next credential in a rotation chain, preserving the original absolute expiry boundary.
    /// </summary>
    public static PlatformDeviceRecoveryCredential CreateRotated(
        PlatformDeviceRecoveryCredential previous,
        string tokenHash,
        string securityStampAtIssue,
        DateTimeOffset utcNow,
        TimeSpan idleLifetime)
    {
        ArgumentNullException.ThrowIfNull(previous);
        if (previous.RevokedAtUtc is not null)
        {
            throw new DomainException(DomainErrorCodes.RecoveryCredentialInvalid, "Recovery credential is not active.");
        }

        var refreshedIdle = utcNow.Add(idleLifetime);
        var idleExpiresAtUtc = refreshedIdle > previous.AbsoluteExpiresAtUtc
            ? previous.AbsoluteExpiresAtUtc
            : refreshedIdle;

        return new PlatformDeviceRecoveryCredential(
            PlatformDeviceRecoveryCredentialId.New(),
            previous.UserId,
            previous.InstallationDeviceId,
            tokenHash.Trim(),
            securityStampAtIssue.Trim(),
            utcNow,
            utcNow,
            idleExpiresAtUtc,
            previous.AbsoluteExpiresAtUtc,
            revokedAtUtc: null,
            rotationVersion: previous.RotationVersion + 1);
    }

    private static void EnsureUtc(DateTimeOffset value)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new DomainException(DomainErrorCodes.InvalidUtcTimestamp, "Recovery credential timestamps must be UTC.");
        }
    }
}

public sealed class PlatformDeviceRecoveryCredentialId : IEquatable<PlatformDeviceRecoveryCredentialId>
{
    public Guid Value { get; }

    private PlatformDeviceRecoveryCredentialId(Guid value) => Value = value;

    public static PlatformDeviceRecoveryCredentialId New() => new(Guid.NewGuid());

    public static PlatformDeviceRecoveryCredentialId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(DomainErrorCodes.InvalidAccountStatusTransition, "Recovery credential id is required.");
        }

        return new PlatformDeviceRecoveryCredentialId(value);
    }

    public bool Equals(PlatformDeviceRecoveryCredentialId? other) =>
        other is not null && Value.Equals(other.Value);

    public override bool Equals(object? obj) =>
        obj is PlatformDeviceRecoveryCredentialId other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString("D");

    public static bool operator ==(PlatformDeviceRecoveryCredentialId? left, PlatformDeviceRecoveryCredentialId? right) =>
        Equals(left, right);

    public static bool operator !=(PlatformDeviceRecoveryCredentialId? left, PlatformDeviceRecoveryCredentialId? right) =>
        !Equals(left, right);
}
