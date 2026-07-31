using ExItS.Platform.Domain.Common;

namespace ExItS.Platform.Domain.Identity;

/// <summary>
/// Server-side browser session for an authenticated <see cref="PlatformUser"/>.
/// Stores only a hash of the opaque session token — never the raw token, password, or bearer JWT.
/// </summary>
public sealed class PlatformAuthSession
{
    public PlatformAuthSessionId Id { get; }
    public PlatformUserId UserId { get; }
    public string TokenHash { get; }
    public string SecurityStampAtIssue { get; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset ExpiresAtUtc { get; private set; }
    public DateTimeOffset AbsoluteExpiresAtUtc { get; }
    public DateTimeOffset LastActivityAtUtc { get; private set; }
    public DateTimeOffset? RevokedAtUtc { get; private set; }
    public string? IpAddress { get; }
    public string? UserAgentHash { get; }

    private PlatformAuthSession(
        PlatformAuthSessionId id,
        PlatformUserId userId,
        string tokenHash,
        string securityStampAtIssue,
        DateTimeOffset createdAtUtc,
        DateTimeOffset expiresAtUtc,
        DateTimeOffset absoluteExpiresAtUtc,
        DateTimeOffset lastActivityAtUtc,
        DateTimeOffset? revokedAtUtc,
        string? ipAddress,
        string? userAgentHash)
    {
        Id = id;
        UserId = userId;
        TokenHash = tokenHash;
        SecurityStampAtIssue = securityStampAtIssue;
        CreatedAtUtc = createdAtUtc;
        ExpiresAtUtc = expiresAtUtc;
        AbsoluteExpiresAtUtc = absoluteExpiresAtUtc;
        LastActivityAtUtc = lastActivityAtUtc;
        RevokedAtUtc = revokedAtUtc;
        IpAddress = ipAddress;
        UserAgentHash = userAgentHash;
    }

    public static PlatformAuthSession Create(
        PlatformUserId userId,
        string tokenHash,
        string securityStampAtIssue,
        DateTimeOffset utcNow,
        TimeSpan idleLifetime,
        TimeSpan absoluteLifetime,
        string? ipAddress = null,
        string? userAgentHash = null,
        PlatformAuthSessionId? id = null)
    {
        ArgumentNullException.ThrowIfNull(userId);
        EnsureUtc(utcNow);
        if (idleLifetime <= TimeSpan.Zero || absoluteLifetime <= TimeSpan.Zero)
        {
            throw new DomainException(DomainErrorCodes.InvalidUtcTimestamp, "Session lifetimes must be positive.");
        }

        if (string.IsNullOrWhiteSpace(tokenHash) || tokenHash.Length > 128)
        {
            throw new DomainException(DomainErrorCodes.InvalidAccountStatusTransition, "Session token hash is invalid.");
        }

        if (string.IsNullOrWhiteSpace(securityStampAtIssue) || securityStampAtIssue.Length > 64)
        {
            throw new DomainException(DomainErrorCodes.InvalidAccountStatusTransition, "Security stamp is invalid.");
        }

        var absolute = utcNow.Add(absoluteLifetime);
        var sliding = utcNow.Add(idleLifetime);
        var expires = sliding < absolute ? sliding : absolute;

        return new PlatformAuthSession(
            id ?? PlatformAuthSessionId.New(),
            userId,
            tokenHash.Trim(),
            securityStampAtIssue.Trim(),
            utcNow,
            expires,
            absolute,
            utcNow,
            revokedAtUtc: null,
            NormalizeOptional(ipAddress, 64),
            NormalizeOptional(userAgentHash, 128));
    }

    public static PlatformAuthSession Rehydrate(
        PlatformAuthSessionId id,
        PlatformUserId userId,
        string tokenHash,
        string securityStampAtIssue,
        DateTimeOffset createdAtUtc,
        DateTimeOffset expiresAtUtc,
        DateTimeOffset absoluteExpiresAtUtc,
        DateTimeOffset lastActivityAtUtc,
        DateTimeOffset? revokedAtUtc,
        string? ipAddress,
        string? userAgentHash) =>
        new(
            id,
            userId,
            tokenHash,
            securityStampAtIssue,
            createdAtUtc,
            expiresAtUtc,
            absoluteExpiresAtUtc,
            lastActivityAtUtc,
            revokedAtUtc,
            ipAddress,
            userAgentHash);

    public bool IsActive(DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        return RevokedAtUtc is null
               && ExpiresAtUtc > utcNow
               && AbsoluteExpiresAtUtc > utcNow;
    }

    public void RecordActivity(DateTimeOffset utcNow, TimeSpan idleLifetime)
    {
        EnsureUtc(utcNow);
        if (!IsActive(utcNow))
        {
            throw new DomainException(DomainErrorCodes.InvalidAccountStatusTransition, "Session is not active.");
        }

        if (idleLifetime <= TimeSpan.Zero)
        {
            throw new DomainException(DomainErrorCodes.InvalidUtcTimestamp, "Idle lifetime must be positive.");
        }

        LastActivityAtUtc = utcNow;
        var sliding = utcNow.Add(idleLifetime);
        ExpiresAtUtc = sliding < AbsoluteExpiresAtUtc ? sliding : AbsoluteExpiresAtUtc;
    }

    public void Revoke(DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        if (RevokedAtUtc is not null)
        {
            return;
        }

        RevokedAtUtc = utcNow;
        if (ExpiresAtUtc > utcNow)
        {
            ExpiresAtUtc = utcNow;
        }
    }

    private static string? NormalizeOptional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }

    private static void EnsureUtc(DateTimeOffset value)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new DomainException(DomainErrorCodes.InvalidUtcTimestamp, "Session timestamps must be UTC.");
        }
    }
}

public sealed class PlatformAuthSessionId : IEquatable<PlatformAuthSessionId>
{
    public Guid Value { get; }

    private PlatformAuthSessionId(Guid value) => Value = value;

    public static PlatformAuthSessionId New() => new(Guid.NewGuid());

    public static PlatformAuthSessionId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(DomainErrorCodes.InvalidAccountStatusTransition, "Session id is required.");
        }

        return new PlatformAuthSessionId(value);
    }

    public bool Equals(PlatformAuthSessionId? other) =>
        other is not null && Value.Equals(other.Value);

    public override bool Equals(object? obj) =>
        obj is PlatformAuthSessionId other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString("D");

    public static bool operator ==(PlatformAuthSessionId? left, PlatformAuthSessionId? right) =>
        Equals(left, right);

    public static bool operator !=(PlatformAuthSessionId? left, PlatformAuthSessionId? right) =>
        !Equals(left, right);
}
