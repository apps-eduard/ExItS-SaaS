using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;

namespace ExItS.PinoyBusinessPOS.Domain.Catalog;

/// <summary>
/// Explicit per-branch commercial offering override for a catalog product.
/// Sparse: OrganizationStandard with no row means offered by default (resolved in MB2-01B).
/// </summary>
public sealed class BranchProductAvailability
{
    public PosOrganizationId OrganizationId { get; }
    public PosBranchId BranchId { get; }
    public CatalogProductId ProductId { get; }
    public bool IsOffered { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public Guid? UpdatedByActorId { get; private set; }

    private BranchProductAvailability(
        PosOrganizationId organizationId,
        PosBranchId branchId,
        CatalogProductId productId,
        bool isOffered,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc,
        Guid? updatedByActorId)
    {
        OrganizationId = organizationId;
        BranchId = branchId;
        ProductId = productId;
        IsOffered = isOffered;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
        UpdatedByActorId = updatedByActorId;
    }

    public static BranchProductAvailability Create(
        PosOrganizationId organizationId,
        PosBranchId branchId,
        CatalogProductId productId,
        bool isOffered,
        DateTimeOffset utcNow,
        Guid? updatedByActorId = null)
    {
        CatalogGuards.EnsureUtc(utcNow);
        return new BranchProductAvailability(
            organizationId,
            branchId,
            productId,
            isOffered,
            utcNow,
            utcNow,
            updatedByActorId == Guid.Empty ? null : updatedByActorId);
    }

    public static BranchProductAvailability Rehydrate(
        PosOrganizationId organizationId,
        PosBranchId branchId,
        CatalogProductId productId,
        bool isOffered,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc,
        Guid? updatedByActorId = null) =>
        new(
            organizationId,
            branchId,
            productId,
            isOffered,
            createdAtUtc,
            updatedAtUtc,
            updatedByActorId == Guid.Empty ? null : updatedByActorId);

    public void SetOffered(bool isOffered, DateTimeOffset utcNow, Guid? updatedByActorId = null)
    {
        CatalogGuards.EnsureUtc(utcNow);
        IsOffered = isOffered;
        UpdatedAtUtc = utcNow;
        if (updatedByActorId is not null && updatedByActorId != Guid.Empty)
        {
            UpdatedByActorId = updatedByActorId;
        }
    }
}
