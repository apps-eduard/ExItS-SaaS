using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;

namespace ExItS.PinoyBusinessPOS.Domain.Catalog;

/// <summary>
/// Explicit per-branch selling price override for a catalog product base price or sell unit.
/// Sparse: no row means inherit organization default (MB2-03).
/// </summary>
public sealed class BranchProductPriceOverride
{
    /// <summary>Sentinel for base product <see cref="CatalogProduct.SellingPrice"/> in composite keys.</summary>
    public static readonly Guid BaseProductUnitKey = Guid.Empty;

    public PosOrganizationId OrganizationId { get; }
    public PosBranchId BranchId { get; }
    public CatalogProductId ProductId { get; }
    /// <summary><see cref="BaseProductUnitKey"/> for base product price; otherwise a sell unit id.</summary>
    public Guid ProductUnitId { get; }
    public decimal SellingPrice { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public Guid? UpdatedByActorId { get; private set; }

    private BranchProductPriceOverride(
        PosOrganizationId organizationId,
        PosBranchId branchId,
        CatalogProductId productId,
        Guid productUnitId,
        decimal sellingPrice,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc,
        Guid? updatedByActorId)
    {
        OrganizationId = organizationId;
        BranchId = branchId;
        ProductId = productId;
        ProductUnitId = productUnitId;
        SellingPrice = sellingPrice;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
        UpdatedByActorId = updatedByActorId;
    }

    public static BranchProductPriceOverride Create(
        PosOrganizationId organizationId,
        PosBranchId branchId,
        CatalogProductId productId,
        Guid productUnitId,
        decimal sellingPrice,
        DateTimeOffset utcNow,
        Guid? updatedByActorId = null)
    {
        CatalogGuards.EnsureUtc(utcNow);
        EnsureSellingPrice(sellingPrice);
        return new BranchProductPriceOverride(
            organizationId,
            branchId,
            productId,
            productUnitId,
            sellingPrice,
            utcNow,
            utcNow,
            updatedByActorId == Guid.Empty ? null : updatedByActorId);
    }

    public static BranchProductPriceOverride Rehydrate(
        PosOrganizationId organizationId,
        PosBranchId branchId,
        CatalogProductId productId,
        Guid productUnitId,
        decimal sellingPrice,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc,
        Guid? updatedByActorId = null) =>
        new(
            organizationId,
            branchId,
            productId,
            productUnitId,
            sellingPrice,
            createdAtUtc,
            updatedAtUtc,
            updatedByActorId == Guid.Empty ? null : updatedByActorId);

    public void SetSellingPrice(decimal sellingPrice, DateTimeOffset utcNow, Guid? updatedByActorId = null)
    {
        CatalogGuards.EnsureUtc(utcNow);
        EnsureSellingPrice(sellingPrice);
        SellingPrice = sellingPrice;
        UpdatedAtUtc = utcNow;
        if (updatedByActorId is not null && updatedByActorId != Guid.Empty)
        {
            UpdatedByActorId = updatedByActorId;
        }
    }

    public bool IsBaseProductPrice => ProductUnitId == BaseProductUnitKey;

    private static void EnsureSellingPrice(decimal sellingPrice)
    {
        if (sellingPrice < 0m)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidProductSellingPrice,
                "Branch price override must be greater than or equal to zero.");
        }
    }
}
