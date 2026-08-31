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

public enum ProductLocalRoleGrantStatus
{
    Active = 0,
    Revoked = 1
}

/// <summary>
/// Platform-recorded product-local role catalog and POS boundary mapping (WP09).
/// Entitlement enables the product for the Organization; this grant authorizes operations.
/// </summary>
public static class ProductLocalRoleCodes
{
    public const string Owner = "Owner";
    public const string Manager = "Manager";
    public const string Cashier = "Cashier";
    public const string InventoryStaff = "InventoryStaff";
    public const string ReportingUser = "ReportingUser";

    /// <summary>Legacy alias for <see cref="ReportingUser"/>.</summary>
    public const string Viewer = "Viewer";

    /// <summary>Legacy alias used by Start a Business (WP08).</summary>
    public const string PosOwnerRoleCode = Owner;

    public static readonly IReadOnlyList<string> All =
        [Owner, Manager, Cashier, InventoryStaff, ReportingUser];

    public static readonly IReadOnlyList<string> Assignable =
        [Owner, Manager, Cashier, InventoryStaff, ReportingUser];

    public static bool IsKnown(string? roleCode) =>
        !string.IsNullOrWhiteSpace(roleCode)
        && (All.Contains(Normalize(roleCode), StringComparer.Ordinal)
            || string.Equals(Normalize(roleCode), Viewer, StringComparison.Ordinal));

    public static string Normalize(string roleCode) => roleCode.Trim();

    /// <summary>Canonical catalog code; legacy Viewer maps to ReportingUser.</summary>
    public static string NormalizeCatalogCode(string roleCode) =>
        Normalize(roleCode) switch
        {
            Viewer => ReportingUser,
            var code when All.Contains(code, StringComparer.Ordinal) => code,
            _ => Normalize(roleCode)
        };

    /// <summary>
    /// Maps Platform product-local role codes onto PinoyBusinessPOS role codes for product DB sync.
    /// </summary>
    public static string MapToPosRoleCode(string roleCode) =>
        NormalizeCatalogCode(roleCode) switch
        {
            Owner => "Owner",
            Manager => "StoreManager",
            Cashier => "Cashier",
            InventoryStaff => "InventoryStaff",
            ReportingUser => "ReportingUser",
            _ => throw new DomainException(
                DomainErrorCodes.InvalidProductLocalRoleCode,
                $"Unrecognized product-local role '{roleCode}'.")
        };

    public static string EnsureKnown(string roleCode)
    {
        var normalized = NormalizeCatalogCode(roleCode);
        if (!All.Contains(normalized, StringComparer.Ordinal))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidProductLocalRoleCode,
                $"Unrecognized product-local role '{roleCode}'. Expected Owner, Manager, Cashier, InventoryStaff, or ReportingUser.");
        }

        return normalized;
    }
}

/// <summary>
/// Platform-recorded product-local role grant (e.g. POS Owner). Separate from Organization Owner and entitlement.
/// Consumed by product navigation and POS boundary sync (WP09).
/// </summary>
public sealed class ProductLocalRoleGrant
{
    public const string PosOwnerRoleCode = ProductLocalRoleCodes.Owner;
    public const string StartBusinessSource = "StartBusiness";
    public const string AssignmentSource = "Assignment";

    public ProductLocalRoleGrantId Id { get; }
    public PlatformOrganizationId OrganizationId { get; }
    public PlatformUserId UserIdentityId { get; }
    public string ProductCode { get; }
    public string RoleCode { get; private set; }
    public ProductLocalRoleGrantStatus Status { get; private set; }
    public DateTimeOffset GrantedAtUtc { get; }
    public PlatformUserId GrantedByUserIdentityId { get; }
    public string Source { get; }
    public DateTimeOffset? RevokedAtUtc { get; private set; }
    public PlatformUserId? RevokedByUserIdentityId { get; private set; }
    public string? Reason { get; private set; }

    private ProductLocalRoleGrant(
        ProductLocalRoleGrantId id,
        PlatformOrganizationId organizationId,
        PlatformUserId userIdentityId,
        string productCode,
        string roleCode,
        ProductLocalRoleGrantStatus status,
        DateTimeOffset grantedAtUtc,
        PlatformUserId grantedByUserIdentityId,
        string source,
        DateTimeOffset? revokedAtUtc,
        PlatformUserId? revokedByUserIdentityId,
        string? reason)
    {
        Id = id;
        OrganizationId = organizationId;
        UserIdentityId = userIdentityId;
        ProductCode = productCode;
        RoleCode = roleCode;
        Status = status;
        GrantedAtUtc = grantedAtUtc;
        GrantedByUserIdentityId = grantedByUserIdentityId;
        Source = source;
        RevokedAtUtc = revokedAtUtc;
        RevokedByUserIdentityId = revokedByUserIdentityId;
        Reason = reason;
    }

    public string MappedPosRoleCode => ProductLocalRoleCodes.MapToPosRoleCode(RoleCode);

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
            NormalizeProductCode(productCode),
            ProductLocalRoleCodes.EnsureKnown(roleCode),
            ProductLocalRoleGrantStatus.Active,
            utcNow,
            grantedByUserIdentityId,
            string.IsNullOrWhiteSpace(source) ? StartBusinessSource : source.Trim(),
            revokedAtUtc: null,
            revokedByUserIdentityId: null,
            reason: null);
    }

    public void Revoke(PlatformUserId revokedByUserIdentityId, string? reason, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(revokedByUserIdentityId);
        EnsureUtc(utcNow);

        if (Status == ProductLocalRoleGrantStatus.Revoked)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidProductLocalRoleStatusTransition,
                "Product-local role grant is already revoked.");
        }

        Status = ProductLocalRoleGrantStatus.Revoked;
        RevokedAtUtc = utcNow;
        RevokedByUserIdentityId = revokedByUserIdentityId;
        Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
    }

    public static ProductLocalRoleGrant Rehydrate(
        ProductLocalRoleGrantId id,
        PlatformOrganizationId organizationId,
        PlatformUserId userIdentityId,
        string productCode,
        string roleCode,
        ProductLocalRoleGrantStatus status,
        DateTimeOffset grantedAtUtc,
        PlatformUserId grantedByUserIdentityId,
        string source,
        DateTimeOffset? revokedAtUtc = null,
        PlatformUserId? revokedByUserIdentityId = null,
        string? reason = null) =>
        new(
            id,
            organizationId,
            userIdentityId,
            productCode,
            roleCode,
            status,
            grantedAtUtc,
            grantedByUserIdentityId,
            source,
            revokedAtUtc,
            revokedByUserIdentityId,
            reason);

    private static string NormalizeProductCode(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException(DomainErrorCodes.InvalidProductCode, "Product code is required.");
        }

        var trimmed = value.Trim();
        if (trimmed.Length > 64)
        {
            throw new DomainException(DomainErrorCodes.InvalidProductCode, "Product code is invalid.");
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
