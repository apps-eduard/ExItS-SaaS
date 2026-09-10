using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.Domain.Inventory;

/// <summary>
/// Branch-wide default low-stock threshold and reorder quantity.
/// Products without an explicit <see cref="InventoryBranchReorderSetting"/> inherit these values.
/// </summary>
public sealed class InventoryBranchReorderDefault
{
    public PosOrganizationId OrganizationId { get; }
    public PosBranchId BranchId { get; }
    public decimal? ReorderLevel { get; private set; }
    public decimal? ReorderQuantity { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public Guid UpdatedBy { get; private set; }

    private InventoryBranchReorderDefault(
        PosOrganizationId organizationId,
        PosBranchId branchId,
        decimal? reorderLevel,
        decimal? reorderQuantity,
        DateTimeOffset updatedAtUtc,
        Guid updatedBy)
    {
        OrganizationId = organizationId;
        BranchId = branchId;
        ReorderLevel = reorderLevel;
        ReorderQuantity = reorderQuantity;
        UpdatedAtUtc = updatedAtUtc;
        UpdatedBy = updatedBy;
    }

    public static InventoryBranchReorderDefault Create(
        PosOrganizationId organizationId,
        PosBranchId branchId,
        decimal? reorderLevel,
        decimal? reorderQuantity,
        Guid updatedBy,
        DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        EnsureActor(updatedBy);
        return new(
            organizationId,
            branchId,
            NormalizeDefaultLevel(reorderLevel),
            NormalizeDefaultQuantity(reorderQuantity),
            utcNow,
            updatedBy);
    }

    public static InventoryBranchReorderDefault Rehydrate(
        PosOrganizationId organizationId,
        PosBranchId branchId,
        decimal? reorderLevel,
        decimal? reorderQuantity,
        DateTimeOffset updatedAtUtc,
        Guid updatedBy) =>
        new(organizationId, branchId, reorderLevel, reorderQuantity, updatedAtUtc, updatedBy);

    public void SetConfiguration(
        decimal? reorderLevel,
        decimal? reorderQuantity,
        Guid updatedBy,
        DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        EnsureActor(updatedBy);
        ReorderLevel = NormalizeDefaultLevel(reorderLevel);
        ReorderQuantity = NormalizeDefaultQuantity(reorderQuantity);
        UpdatedAtUtc = utcNow;
        UpdatedBy = updatedBy;
    }

    private static decimal? NormalizeDefaultLevel(decimal? reorderLevel)
    {
        if (reorderLevel is null)
        {
            return null;
        }

        if (reorderLevel.Value < 0m)
        {
            throw new DomainException(
                DomainErrorCodes.InventoryReorderLevelInvalid,
                "Low stock level cannot be negative.");
        }

        return decimal.Round(reorderLevel.Value, 3, MidpointRounding.AwayFromZero);
    }

    private static decimal? NormalizeDefaultQuantity(decimal? reorderQuantity)
    {
        if (reorderQuantity is null)
        {
            return null;
        }

        if (reorderQuantity.Value <= 0m)
        {
            throw new DomainException(
                DomainErrorCodes.InventoryReorderQuantityInvalid,
                "Reorder quantity must be greater than zero when set.");
        }

        return decimal.Round(reorderQuantity.Value, 3, MidpointRounding.AwayFromZero);
    }

    private static void EnsureActor(Guid updatedBy)
    {
        if (updatedBy == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidInventoryReorderActor,
                "Actor id must be a non-empty GUID.");
        }
    }

    private static void EnsureUtc(DateTimeOffset utcNow)
    {
        if (utcNow.Offset != TimeSpan.Zero)
        {
            throw new DomainException(DomainErrorCodes.InvalidUtcTimestamp, "Timestamp must be UTC.");
        }
    }
}

/// <summary>How a product participates in branch low-stock monitoring.</summary>
public static class InventoryReorderMonitoringModes
{
    public const string BranchDefault = "BranchDefault";
    public const string Custom = "Custom";
    public const string NotMonitored = "NotMonitored";

    public static string Derive(InventoryBranchReorderSetting? productSetting)
    {
        if (productSetting is null)
        {
            return BranchDefault;
        }

        return productSetting.ReorderLevel is null ? NotMonitored : Custom;
    }
}
