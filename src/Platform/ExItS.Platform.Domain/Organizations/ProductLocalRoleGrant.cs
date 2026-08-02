using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;

namespace ExItS.Platform.Domain.Organizations;

public sealed class ProductLocalRoleGrantId : IEquatable<ProductLocalRoleGrantId>
{
    public Guid Value { get; }

    private ProductLocalRoleGrantId(Guid value) => Value = value;

    public static ProductLocalRoleGrantId New() => new(Guid.NewGuid());

    public static ProductLocalRoleGrantId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidProductLocalRoleGrantId,
                "Product local role grant id is required.");
        }

        return new ProductLocalRoleGrantId(value);
    }

    public bool Equals(ProductLocalRoleGrantId? other) =>
        other is not null && Value.Equals(other.Value);

    public override bool Equals(object? obj) =>
        obj is ProductLocalRoleGrantId other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString("D");
}

/// <summary>
/// Platform-recorded product-local role grant (e.g. POS Owner). Separate from Organization Owner and entitlement.
/// Provisional until product DB consumes the grant (WP09).
/// </summary>
public sealed class ProductLocalRoleGrant
{
    public const string PosOwnerRoleCode = "Owner";
    public const string StartBusinessSource = "StartBusiness";

    public ProductLocalRoleGrantId Id { get; }
    public PlatformOrganizationId OrganizationId { get; }
    public PlatformUserId UserIdentityId { get; }
    public string ProductCode { get; }
    public string RoleCode { get; }
    public DateTimeOffset GrantedAtUtc { get; }
    public PlatformUserId GrantedByUserIdentityId { get; }
    public string Source { get; }

    private ProductLocalRoleGrant(
        ProductLocalRoleGrantId id,
        PlatformOrganizationId organizationId,
        PlatformUserId userIdentityId,
        string productCode,
        string roleCode,
        DateTimeOffset grantedAtUtc,
        PlatformUserId grantedByUserIdentityId,
        string source)
    {
        Id = id;
        OrganizationId = organizationId;
        UserIdentityId = userIdentityId;
        ProductCode = productCode;
        RoleCode = roleCode;
        GrantedAtUtc = grantedAtUtc;
        GrantedByUserIdentityId = grantedByUserIdentityId;
        Source = source;
    }

    public static ProductLocalRoleGrant Create(
        PlatformOrganizationId organizationId,
        PlatformUserId userIdentityId,
        string productCode,
        string roleCode,
        PlatformUserId grantedByUserIdentityId,
        DateTimeOffset utcNow,
        string source = StartBusinessSource,
        ProductLocalRoleGrantId? id = null)
    {
        ArgumentNullException.ThrowIfNull(organizationId);
        ArgumentNullException.ThrowIfNull(userIdentityId);
        ArgumentNullException.ThrowIfNull(grantedByUserIdentityId);
        EnsureUtc(utcNow);

        return new ProductLocalRoleGrant(
            id ?? ProductLocalRoleGrantId.New(),
            organizationId,
            userIdentityId,
            NormalizeCode(productCode, "Product code"),
            NormalizeCode(roleCode, "Role code"),
            utcNow,
            grantedByUserIdentityId,
            string.IsNullOrWhiteSpace(source) ? StartBusinessSource : source.Trim());
    }

    public static ProductLocalRoleGrant Rehydrate(
        ProductLocalRoleGrantId id,
        PlatformOrganizationId organizationId,
        PlatformUserId userIdentityId,
        string productCode,
        string roleCode,
        DateTimeOffset grantedAtUtc,
        PlatformUserId grantedByUserIdentityId,
        string source) =>
        new(id, organizationId, userIdentityId, productCode, roleCode, grantedAtUtc, grantedByUserIdentityId, source);

    private static string NormalizeCode(string value, string label)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException(DomainErrorCodes.InvalidProductCode, $"{label} is required.");
        }

        var trimmed = value.Trim();
        if (trimmed.Length > 64)
        {
            throw new DomainException(DomainErrorCodes.InvalidProductCode, $"{label} is invalid.");
        }

        return trimmed;
    }

    private static void EnsureUtc(DateTimeOffset value)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new DomainException(DomainErrorCodes.InvalidUtcTimestamp, "Timestamps must be UTC.");
        }
    }
}
