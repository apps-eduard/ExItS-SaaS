using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.Domain.Identity;

/// <summary>
/// Platform-issued opaque API access token for product clients (MAUI/POS).
/// Stores only a hash of the opaque bearer — never the raw token.
/// Organization and product code bind product-entry context after membership + product-access checks.
/// </summary>
public sealed class PlatformAccessToken
{
    public PlatformAccessTokenId Id { get; }
    public PlatformUserId UserId { get; }
    public string TokenHash { get; }
    public string SecurityStampAtIssue { get; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset ExpiresAtUtc { get; private set; }
    public DateTimeOffset? RevokedAtUtc { get; private set; }
    public PlatformOrganizationId? OrganizationId { get; private set; }
    public string? ProductCode { get; private set; }

    private PlatformAccessToken(
        PlatformAccessTokenId id,
        PlatformUserId userId,
        string tokenHash,
        string securityStampAtIssue,
        DateTimeOffset createdAtUtc,
        DateTimeOffset expiresAtUtc,
        DateTimeOffset? revokedAtUtc,
        PlatformOrganizationId? organizationId,
        string? productCode)
    {
        Id = id;
        UserId = userId;
        TokenHash = tokenHash;
        SecurityStampAtIssue = securityStampAtIssue;
        CreatedAtUtc = createdAtUtc;
        ExpiresAtUtc = expiresAtUtc;
        RevokedAtUtc = revokedAtUtc;
        OrganizationId = organizationId;
        ProductCode = productCode;
    }

    public static PlatformAccessToken Create(
        PlatformUserId userId,
        string tokenHash,
        string securityStampAtIssue,
        DateTimeOffset utcNow,
        TimeSpan lifetime,
        PlatformOrganizationId? organizationId = null,
        string? productCode = null,
        PlatformAccessTokenId? id = null)
    {
        ArgumentNullException.ThrowIfNull(userId);
        EnsureUtc(utcNow);
        if (lifetime <= TimeSpan.Zero)
        {
            throw new DomainException(DomainErrorCodes.InvalidUtcTimestamp, "Access token lifetime must be positive.");
        }

        if (string.IsNullOrWhiteSpace(tokenHash) || tokenHash.Length > 128)
        {
            throw new DomainException(DomainErrorCodes.InvalidAccountStatusTransition, "Access token hash is invalid.");
        }

        if (string.IsNullOrWhiteSpace(securityStampAtIssue) || securityStampAtIssue.Length > 64)
        {
            throw new DomainException(DomainErrorCodes.InvalidAccountStatusTransition, "Security stamp is invalid.");
        }

        var normalizedProduct = NormalizeProductCode(productCode);
        if (organizationId is null && normalizedProduct is not null)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidAccountStatusTransition,
                "Product-scoped access tokens require an organization.");
        }

        return new PlatformAccessToken(
            id ?? PlatformAccessTokenId.New(),
            userId,
            tokenHash.Trim(),
            securityStampAtIssue.Trim(),
            utcNow,
            utcNow.Add(lifetime),
            revokedAtUtc: null,
            organizationId,
            normalizedProduct);
    }

    public static PlatformAccessToken Rehydrate(
        PlatformAccessTokenId id,
        PlatformUserId userId,
        string tokenHash,
        string securityStampAtIssue,
        DateTimeOffset createdAtUtc,
        DateTimeOffset expiresAtUtc,
        DateTimeOffset? revokedAtUtc,
        PlatformOrganizationId? organizationId,
        string? productCode) =>
        new(
            id,
            userId,
            tokenHash,
            securityStampAtIssue,
            createdAtUtc,
            expiresAtUtc,
            revokedAtUtc,
            organizationId,
            productCode);

    public bool IsActive(DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        return RevokedAtUtc is null && ExpiresAtUtc > utcNow;
    }

    public void BindProductContext(PlatformOrganizationId organizationId, string productCode)
    {
        ArgumentNullException.ThrowIfNull(organizationId);
        if (RevokedAtUtc is not null)
        {
            throw new DomainException(DomainErrorCodes.InvalidAccountStatusTransition, "Access token is not active.");
        }

        var normalized = NormalizeProductCode(productCode)
            ?? throw new DomainException(DomainErrorCodes.InvalidAccountStatusTransition, "Product code is required.");

        OrganizationId = organizationId;
        ProductCode = normalized;
    }

    public void ClearProductContext()
    {
        OrganizationId = null;
        ProductCode = null;
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

    private static string? NormalizeProductCode(string? productCode)
    {
        if (string.IsNullOrWhiteSpace(productCode))
        {
            return null;
        }

        var trimmed = productCode.Trim().ToLowerInvariant();
        return trimmed.Length <= 64 ? trimmed : trimmed[..64];
    }

    private static void EnsureUtc(DateTimeOffset value)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new DomainException(DomainErrorCodes.InvalidUtcTimestamp, "Access token timestamps must be UTC.");
        }
    }
}

public sealed class PlatformAccessTokenId : IEquatable<PlatformAccessTokenId>
{
    public Guid Value { get; }

    private PlatformAccessTokenId(Guid value) => Value = value;

    public static PlatformAccessTokenId New() => new(Guid.NewGuid());

    public static PlatformAccessTokenId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(DomainErrorCodes.InvalidAccountStatusTransition, "Access token id is required.");
        }

        return new PlatformAccessTokenId(value);
    }

    public bool Equals(PlatformAccessTokenId? other) =>
        other is not null && Value.Equals(other.Value);

    public override bool Equals(object? obj) =>
        obj is PlatformAccessTokenId other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString("D");

    public static bool operator ==(PlatformAccessTokenId? left, PlatformAccessTokenId? right) =>
        Equals(left, right);

    public static bool operator !=(PlatformAccessTokenId? left, PlatformAccessTokenId? right) =>
        !Equals(left, right);
}
