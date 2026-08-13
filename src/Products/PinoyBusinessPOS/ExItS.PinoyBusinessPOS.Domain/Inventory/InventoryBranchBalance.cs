using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.Domain.Inventory;

/// <summary>
/// Per-branch on-hand for an organization catalog product. Complements the org-level
/// <see cref="InventoryAccount"/> used by sales/PO/counts. Not a duplicate product.
/// </summary>
public sealed class InventoryBranchBalance
{
    public PosOrganizationId OrganizationId { get; }
    public PosBranchId BranchId { get; }
    public CatalogProductId ProductId { get; }
    public decimal OnHandQuantity { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    private InventoryBranchBalance(
        PosOrganizationId organizationId,
        PosBranchId branchId,
        CatalogProductId productId,
        decimal onHandQuantity,
        DateTimeOffset updatedAtUtc)
    {
        OrganizationId = organizationId;
        BranchId = branchId;
        ProductId = productId;
        OnHandQuantity = onHandQuantity;
        UpdatedAtUtc = updatedAtUtc;
    }

    public static InventoryBranchBalance Create(
        PosOrganizationId organizationId,
        PosBranchId branchId,
        CatalogProductId productId,
        decimal onHandQuantity,
        DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        if (onHandQuantity < 0m)
        {
            throw new DomainException(
                DomainErrorCodes.InventoryInsufficientStock,
                "Branch on-hand cannot be negative.");
        }

        return new InventoryBranchBalance(organizationId, branchId, productId, onHandQuantity, utcNow);
    }

    public static InventoryBranchBalance Rehydrate(
        PosOrganizationId organizationId,
        PosBranchId branchId,
        CatalogProductId productId,
        decimal onHandQuantity,
        DateTimeOffset updatedAtUtc) =>
        new(organizationId, branchId, productId, onHandQuantity, updatedAtUtc);

    public void Apply(decimal signedQuantity, DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        if (signedQuantity == 0m)
        {
            return;
        }

        var next = OnHandQuantity + signedQuantity;
        if (next < 0m)
        {
            throw new DomainException(
                DomainErrorCodes.InventoryInsufficientStock,
                "Insufficient branch stock for this movement.");
        }

        OnHandQuantity = next;
        UpdatedAtUtc = utcNow;
    }

    private static void EnsureUtc(DateTimeOffset utcNow)
    {
        if (utcNow.Offset != TimeSpan.Zero)
        {
            throw new DomainException(DomainErrorCodes.InvalidUtcTimestamp, "Timestamp must be UTC.");
        }
    }
}
