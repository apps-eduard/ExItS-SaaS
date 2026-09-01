using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.Domain.Inventory;

/// <summary>
/// Branch-specific reorder thresholds for a tracked catalog product (MB2-02A).
/// Organization <see cref="InventoryAccount"/> reorder fields remain legacy defaults for Primary/Main.
/// </summary>
public sealed class InventoryBranchReorderSetting
{
    public PosOrganizationId OrganizationId { get; }
    public PosBranchId BranchId { get; }
    public CatalogProductId ProductId { get; }
    public decimal? ReorderLevel { get; private set; }
    public decimal? ReorderQuantity { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public Guid UpdatedBy { get; private set; }

    private InventoryBranchReorderSetting(
        PosOrganizationId organizationId,
        PosBranchId branchId,
        CatalogProductId productId,
        decimal? reorderLevel,
        decimal? reorderQuantity,
        DateTimeOffset updatedAtUtc,
        Guid updatedBy)
    {
        OrganizationId = organizationId;
        BranchId = branchId;
        ProductId = productId;
        ReorderLevel = reorderLevel;
        ReorderQuantity = reorderQuantity;
        UpdatedAtUtc = updatedAtUtc;
        UpdatedBy = updatedBy;
    }

    public static InventoryBranchReorderSetting Create(
        PosOrganizationId organizationId,
        PosBranchId branchId,
        CatalogProductId productId,
        decimal? reorderLevel,
        decimal? reorderQuantity,
        UnitOfMeasure unitOfMeasure,
        Guid updatedBy,
        DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        if (updatedBy == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidInventoryReorderActor,
                "Actor id must be a non-empty GUID.");
        }

        return new InventoryBranchReorderSetting(
            organizationId,
            branchId,
            productId,
            InventoryAccount.NormalizeReorderLevel(reorderLevel, unitOfMeasure),
            InventoryAccount.NormalizeReorderQuantity(reorderQuantity, unitOfMeasure),
            utcNow,
            updatedBy);
    }

    public static InventoryBranchReorderSetting Rehydrate(
        PosOrganizationId organizationId,
        PosBranchId branchId,
        CatalogProductId productId,
        decimal? reorderLevel,
        decimal? reorderQuantity,
        DateTimeOffset updatedAtUtc,
        Guid updatedBy) =>
        new(organizationId, branchId, productId, reorderLevel, reorderQuantity, updatedAtUtc, updatedBy);

    public void SetConfiguration(
        decimal? reorderLevel,
        decimal? reorderQuantity,
        UnitOfMeasure unitOfMeasure,
        Guid updatedBy,
        DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        if (updatedBy == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidInventoryReorderActor,
                "Actor id must be a non-empty GUID.");
        }

        ReorderLevel = InventoryAccount.NormalizeReorderLevel(reorderLevel, unitOfMeasure);
        ReorderQuantity = InventoryAccount.NormalizeReorderQuantity(reorderQuantity, unitOfMeasure);
        UpdatedAtUtc = utcNow;
        UpdatedBy = updatedBy;
    }

    private static void EnsureUtc(DateTimeOffset utcNow)
    {
        if (utcNow.Offset != TimeSpan.Zero)
        {
            throw new DomainException(DomainErrorCodes.InvalidUtcTimestamp, "Timestamp must be UTC.");
        }
    }
}
