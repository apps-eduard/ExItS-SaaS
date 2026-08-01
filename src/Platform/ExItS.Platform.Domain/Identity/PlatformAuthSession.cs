using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.Domain.Identity;

/// <summary>
/// Server-side browser session for an authenticated <see cref="PlatformUser"/>.
/// Stores only a hash of the opaque session token — never the raw token, password, or bearer JWT.
/// Optional <see cref="SelectedOrganizationId"/> is server-owned organization context (never client-trusted alone).
/// Session is bound to one <see cref="AccountProfileId"/> / <see cref="AccountClass"/> (ADR-017).
/// </summary>
public sealed class PlatformAuthSession
{
    public PlatformAuthSessionId Id { get; }
    public PlatformUserId UserId { get; }
    public AccountProfileId AccountProfileId { get; }
    public AccountClass AccountClass { get; }
    public AllowedScope AllowedScope => AccountClassScope.ToScope(AccountClass);
    public string TokenHash { get; }
    public string SecurityStampAtIssue { get; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset ExpiresAtUtc { get; private set; }
    public DateTimeOffset AbsoluteExpiresAtUtc { get; }
    public DateTimeOffset LastActivityAtUtc { get; private set; }
    public DateTimeOffset? RevokedAtUtc { get; private set; }
    public string? IpAddress { get; }
    public string? UserAgentHash { get; }
    public PlatformOrganizationId? SelectedOrganizationId { get; private set; }

    private PlatformAuthSession(
        PlatformAuthSessionId id,
        PlatformUserId userId,
        AccountProfileId accountProfileId,
        AccountClass accountClass,
        string tokenHash,
        string securityStampAtIssue,
        DateTimeOffset createdAtUtc,
        DateTimeOffset expiresAtUtc,
        DateTimeOffset absoluteExpiresAtUtc,
        DateTimeOffset lastActivityAtUtc,
        DateTimeOffset? revokedAtUtc,
        string? ipAddress,
        string? userAgentHash,
        PlatformOrganizationId? selectedOrganizationId)
    {
        Id = id;
        UserId = userId;
        AccountProfileId = accountProfileId;
        AccountClass = accountClass;
        TokenHash = tokenHash;
        SecurityStampAtIssue = securityStampAtIssue;
        CreatedAtUtc = createdAtUtc;
        ExpiresAtUtc = expiresAtUtc;
        AbsoluteExpiresAtUtc = absoluteExpiresAtUtc;
        LastActivityAtUtc = lastActivityAtUtc;
        RevokedAtUtc = revokedAtUtc;
        IpAddress = ipAddress;
        UserAgentHash = userAgentHash;
        SelectedOrganizationId = selectedOrganizationId;
    }

    public static PlatformAuthSession Create(
        PlatformUserId userId,
        AccountProfileId accountProfileId,
        AccountClass accountClass,
        string tokenHash,
        string securityStampAtIssue,
        DateTimeOffset utcNow,
        TimeSpan idleLifetime,
        TimeSpan absoluteLifetime,
        string? ipAddress = null,
        string? userAgentHash = null,
        PlatformAuthSessionId? id = null,
        PlatformOrganizationId? selectedOrganizationId = null)
    {
        ArgumentNullException.ThrowIfNull(userId);
        ArgumentNullException.ThrowIfNull(accountProfileId);
        EnsureUtc(utcNow);
        if (!Enum.IsDefined(accountClass))
        {
            throw new DomainException(DomainErrorCodes.InvalidAccountStatusTransition, "Account class is invalid.");
        }

        if (accountClass is not AccountClass.Organization && selectedOrganizationId is not null)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidAccountStatusTransition,
                "Selected organization is only valid for Organization account sessions.");
        }

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
            accountProfileId,
            accountClass,
            tokenHash.Trim(),
            securityStampAtIssue.Trim(),
            utcNow,
            expires,
            absolute,
            utcNow,
            revokedAtUtc: null,
            NormalizeOptional(ipAddress, 64),
            NormalizeOptional(userAgentHash, 128),
            selectedOrganizationId);
    }

    public static PlatformAuthSession Rehydrate(
        PlatformAuthSessionId id,
        PlatformUserId userId,
        AccountProfileId accountProfileId,
        AccountClass accountClass,
        string tokenHash,
        string securityStampAtIssue,
        DateTimeOffset createdAtUtc,
        DateTimeOffset expiresAtUtc,
        DateTimeOffset absoluteExpiresAtUtc,
        DateTimeOffset lastActivityAtUtc,
        DateTimeOffset? revokedAtUtc,
        string? ipAddress,
        string? userAgentHash,
        PlatformOrganizationId? selectedOrganizationId = null) =>
        new(
            id,
            userId,
            accountProfileId,
            accountClass,
            tokenHash,
            securityStampAtIssue,
            createdAtUtc,
            expiresAtUtc,
            absoluteExpiresAtUtc,
            lastActivityAtUtc,
            revokedAtUtc,
            ipAddress,
            userAgentHash,
            selectedOrganizationId);

    /// <summary>Sets trusted organization context after Application has verified an active membership.</summary>
    public void SelectOrganization(PlatformOrganizationId organizationId)
    {
        ArgumentNullException.ThrowIfNull(organizationId);
        if (RevokedAtUtc is not null)
        {
            throw new DomainException(DomainErrorCodes.InvalidAccountStatusTransition, "Session is not active.");
        }

        if (AccountClass is not AccountClass.Organization)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidAccountStatusTransition,
                "Organization context requires an Organization account session.");
        }

        SelectedOrganizationId = organizationId;
    }

    public void ClearSelectedOrganization()
    {
        SelectedOrganizationId = null;
    }

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
