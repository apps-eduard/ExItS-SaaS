using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.Domain.Purchasing;

/// <summary>
/// One immutable line on a goods receipt. <see cref="QuantityReceived"/> is in purchase-unit terms.
/// Inventory movements should use <see cref="BaseQuantity"/>.
/// </summary>
public sealed class GoodsReceiptLine
{
    public GoodsReceiptLineId Id { get; }
    public GoodsReceiptId GoodsReceiptId { get; }
    public PosOrganizationId OrganizationId { get; }
    public PurchaseOrderLineId PurchaseOrderLineId { get; }
    public CatalogProductId ProductId { get; }
    public int LineNumber { get; }
    public string NameSnapshot { get; }
    public UnitOfMeasure UomSnapshot { get; }
    public decimal QuantityReceived { get; }
    public decimal UnitPurchaseCostSnapshot { get; }
    public decimal LineTotalSnapshot { get; }
    public decimal MultiplierToBaseSnapshot { get; }
    public Guid? InventoryMovementId { get; private set; }

    /// <summary>Alias for persistence/DTO mapping compatibility.</summary>
    public decimal ReceivedQty => QuantityReceived;

    /// <summary>Base inventory quantity = purchase-unit received qty × multiplier.</summary>
    public decimal BaseQuantity =>
        ProductUnitConversion.ToBaseQuantity(QuantityReceived, MultiplierToBaseSnapshot);

    private GoodsReceiptLine(
        GoodsReceiptLineId id,
        GoodsReceiptId goodsReceiptId,
        PosOrganizationId organizationId,
        PurchaseOrderLineId purchaseOrderLineId,
        CatalogProductId productId,
        int lineNumber,
        string nameSnapshot,
        UnitOfMeasure uomSnapshot,
        decimal quantityReceived,
        decimal unitPurchaseCostSnapshot,
        decimal lineTotalSnapshot,
        decimal multiplierToBaseSnapshot,
        Guid? inventoryMovementId)
    {
        Id = id;
        GoodsReceiptId = goodsReceiptId;
        OrganizationId = organizationId;
        PurchaseOrderLineId = purchaseOrderLineId;
        ProductId = productId;
        LineNumber = lineNumber;
        NameSnapshot = nameSnapshot;
        UomSnapshot = uomSnapshot;
        QuantityReceived = quantityReceived;
        UnitPurchaseCostSnapshot = unitPurchaseCostSnapshot;
        LineTotalSnapshot = lineTotalSnapshot;
        MultiplierToBaseSnapshot = multiplierToBaseSnapshot;
        InventoryMovementId = inventoryMovementId;
    }

    internal static GoodsReceiptLine Create(
        GoodsReceiptId goodsReceiptId,
        PosOrganizationId organizationId,
        int lineNumber,
        PurchaseOrderLine poLine,
        decimal receiveQty,
        GoodsReceiptLineId? id = null,
        SellingMode sellingMode = SellingMode.PerItem)
    {
        if (poLine.NameSnapshot is null || poLine.UomSnapshot is null)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidGoodsReceiptLine,
                "Cannot receive against an unordered line.");
        }

        var normalized = PurchaseOrderLine.NormalizeQuantity(
            receiveQty,
            poLine.UomSnapshot.Value,
            sellingMode);
        if (normalized <= 0m)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPurchaseReceiveQuantity,
                "Receive quantity must be greater than zero.");
        }

        var multiplier = CatalogProductUnit.NormalizeMultiplier(poLine.MultiplierToBaseSnapshot);
        var cost = poLine.UnitPurchaseCost;
        return new GoodsReceiptLine(
            id ?? GoodsReceiptLineId.New(),
            goodsReceiptId,
            organizationId,
            poLine.Id,
            poLine.ProductId,
            lineNumber,
            poLine.NameSnapshot,
            poLine.UomSnapshot.Value,
            normalized,
            cost,
            SaleMoney.RoundMoney(cost * normalized),
            multiplier,
            inventoryMovementId: null);
    }

    public void AttachInventoryMovement(StockMovementId movementId)
    {
        if (InventoryMovementId is not null)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidGoodsReceiptLine,
                "Inventory movement is already linked to this receipt line.");
        }

        InventoryMovementId = movementId.Value;
    }

    public static GoodsReceiptLine Rehydrate(
        GoodsReceiptLineId id,
        GoodsReceiptId goodsReceiptId,
        PosOrganizationId organizationId,
        PurchaseOrderLineId purchaseOrderLineId,
        CatalogProductId productId,
        int lineNumber,
        string nameSnapshot,
        UnitOfMeasure uomSnapshot,
        decimal quantityReceived,
        decimal unitPurchaseCostSnapshot,
        decimal lineTotalSnapshot,
        Guid? inventoryMovementId,
        decimal multiplierToBaseSnapshot = 1m) =>
        new(
            id,
            goodsReceiptId,
            organizationId,
            purchaseOrderLineId,
            productId,
            lineNumber,
            nameSnapshot,
            uomSnapshot,
            quantityReceived,
            unitPurchaseCostSnapshot,
            lineTotalSnapshot,
            multiplierToBaseSnapshot,
            inventoryMovementId);
}
