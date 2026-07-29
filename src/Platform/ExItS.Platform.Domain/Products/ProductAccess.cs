using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.Domain.Products;

/// <summary>
/// Minimal product-access concept: organization (and optional user) may access a product.
/// Does not replace subscription entitlement, product-local permissions, or resource scope.
/// </summary>
public sealed class ProductAccess
{
    public PlatformOrganizationId OrganizationId { get; }
    public PlatformUserId? UserId { get; }
    public ProductCode ProductCode { get; }
    public ProductAccessStatus Status { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    private ProductAccess(
        PlatformOrganizationId organizationId,
        PlatformUserId? userId,
        ProductCode productCode,
        ProductAccessStatus status,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc)
    {
        OrganizationId = organizationId;
        UserId = userId;
        ProductCode = productCode;
        Status = status;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
    }

    public static ProductAccess Grant(
        PlatformOrganizationId organizationId,
        ProductCode productCode,
        DateTimeOffset utcNow,
        PlatformUserId? userId = null)
    {
        ArgumentNullException.ThrowIfNull(organizationId);
        ArgumentNullException.ThrowIfNull(productCode);
        EnsureUtc(utcNow);

        return new ProductAccess(
            organizationId,
            userId,
            productCode,
            ProductAccessStatus.Active,
            utcNow,
            utcNow);
    }

    public void Revoke(DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        if (Status == ProductAccessStatus.Revoked)
        {
            return;
        }

        Status = ProductAccessStatus.Revoked;
        UpdatedAtUtc = utcNow;
    }

    private static void EnsureUtc(DateTimeOffset value)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidUtcTimestamp,
                "Timestamps must be UTC (offset zero).");
        }
    }
}
