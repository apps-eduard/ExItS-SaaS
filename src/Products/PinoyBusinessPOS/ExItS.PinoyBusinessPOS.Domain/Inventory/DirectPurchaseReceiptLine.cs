using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Purchasing;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.Domain.Inventory;

/// <summary>
/// One immutable line on a direct purchase receipt. Quantity and unit cost are operational;
/// inventory movements use base quantity (same UOM as the product).
/// </summary>
public sealed class DirectPurchaseReceiptLine
{
    public const int LotNumberMaxLength = 64;

    public DirectPurchaseReceiptLineId Id { get; }
    public DirectPurchaseReceiptId ReceiptId { get; }
    public PosOrganizationId OrganizationId { get; }
    public CatalogProductId ProductId { get; }
    public int LineNumber { get; }
    public string ProductNameSnapshot { get; }
    public string? SkuSnapshot { get; }
    public UnitOfMeasure UnitOfMeasureSnapshot { get; }
    public decimal Quantity { get; }
    public decimal UnitCost { get; }
    public decimal LineTotal { get; }
    public DateOnly? ExpiryDate { get; }
    public string? LotNumber { get; }
    public Guid? InventoryMovementId { get; private set; }

    private DirectPurchaseReceiptLine(
        DirectPurchaseReceiptLineId id,
        DirectPurchaseReceiptId receiptId,
        PosOrganizationId organizationId,
        CatalogProductId productId,
        int lineNumber,
        string productNameSnapshot,
        string? skuSnapshot,
        UnitOfMeasure unitOfMeasureSnapshot,
        decimal quantity,
        decimal unitCost,
        decimal lineTotal,
        DateOnly? expiryDate,
        string? lotNumber,
        Guid? inventoryMovementId)
    {
        Id = id;
        ReceiptId = receiptId;
        OrganizationId = organizationId;
        ProductId = productId;
        LineNumber = lineNumber;
        ProductNameSnapshot = productNameSnapshot;
        SkuSnapshot = skuSnapshot;
        UnitOfMeasureSnapshot = unitOfMeasureSnapshot;
        Quantity = quantity;
        UnitCost = unitCost;
        LineTotal = lineTotal;
        ExpiryDate = expiryDate;
        LotNumber = lotNumber;
        InventoryMovementId = inventoryMovementId;
    }

    internal static DirectPurchaseReceiptLine Create(
        DirectPurchaseReceiptId receiptId,
        PosOrganizationId organizationId,
        int lineNumber,
        DirectPurchaseReceiptLineDraft draft,
        DirectPurchaseReceiptLineId? id = null)
    {
        if (draft.Quantity <= 0m)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidDirectPurchaseQuantity,
                "Direct purchase quantity must be greater than zero.");
        }

        var quantity = PurchaseOrderLine.NormalizeQuantity(draft.Quantity, draft.UnitOfMeasure, draft.SellingMode);
        var unitCost = NormalizeUnitCost(draft.UnitCost);
        var name = NormalizeName(draft.ProductNameSnapshot);
        var (skuDisplay, _) = CatalogProduct.NormalizeOptionalSku(draft.SkuSnapshot);

        return new DirectPurchaseReceiptLine(
            id ?? DirectPurchaseReceiptLineId.New(),
            receiptId,
            organizationId,
            draft.ProductId,
            lineNumber,
            name,
            skuDisplay,
            draft.UnitOfMeasure,
            quantity,
            unitCost,
            SaleMoney.RoundMoney(unitCost * quantity),
            draft.ExpiryDate,
            NormalizeLotNumber(draft.LotNumber),
            inventoryMovementId: null);
    }

    public void AttachInventoryMovement(StockMovementId movementId)
    {
        if (InventoryMovementId is not null)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidDirectPurchaseReceiptLine,
                "Inventory movement is already linked to this receipt line.");
        }

        InventoryMovementId = movementId.Value;
    }

    public static DirectPurchaseReceiptLine Rehydrate(
        DirectPurchaseReceiptLineId id,
        DirectPurchaseReceiptId receiptId,
        PosOrganizationId organizationId,
        CatalogProductId productId,
        int lineNumber,
        string productNameSnapshot,
        string? skuSnapshot,
        UnitOfMeasure unitOfMeasureSnapshot,
        decimal quantity,
        decimal unitCost,
        decimal lineTotal,
        DateOnly? expiryDate,
        string? lotNumber,
        Guid? inventoryMovementId) =>
        new(
            id,
            receiptId,
            organizationId,
            productId,
            lineNumber,
            productNameSnapshot,
            skuSnapshot,
            unitOfMeasureSnapshot,
            quantity,
            unitCost,
            lineTotal,
            expiryDate,
            lotNumber,
            inventoryMovementId);

    private static decimal NormalizeUnitCost(decimal cost)
    {
        if (cost <= 0m)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidDirectPurchaseUnitCost,
                "Unit cost must be greater than zero.");
        }

        if (cost > PurchaseOrderLine.MaxUnitPurchaseCost)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidDirectPurchaseUnitCost,
                "Unit cost is too large.");
        }

        return SaleMoney.RoundMoney(cost);
    }

    private static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidDirectPurchaseReceiptLine,
                "Product name snapshot is required.");
        }

        var trimmed = name.Trim();
        if (trimmed.Length > PurchaseOrderLine.NameSnapshotMaxLength)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidDirectPurchaseReceiptLine,
                $"Product name snapshot must be at most {PurchaseOrderLine.NameSnapshotMaxLength} characters.");
        }

        return trimmed;
    }

    private static string? NormalizeLotNumber(string? lotNumber)
    {
        if (string.IsNullOrWhiteSpace(lotNumber))
        {
            return null;
        }

        var trimmed = lotNumber.Trim();
        if (trimmed.Length > LotNumberMaxLength)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidDirectPurchaseReceiptLine,
                $"Lot number must be at most {LotNumberMaxLength} characters.");
        }

        return trimmed;
    }
}
