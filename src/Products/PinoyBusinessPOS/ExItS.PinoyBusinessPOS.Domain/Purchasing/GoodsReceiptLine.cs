using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.Domain.Purchasing;

/// <summary>
/// One immutable line on a goods receipt. <see cref="QuantityReceived"/> is good/usable qty in purchase-unit terms
/// and is the only quantity that may create inventory movements. Damaged/rejected/short-closed quantities are
/// recorded for discrepancy visibility and do not enter sellable stock.
/// Expiry/lot belong to the receipt (actual delivery), not the purchase order.
/// </summary>
public sealed class GoodsReceiptLine
{
    public const int DiscrepancyNoteMaxLength = 280;

    public GoodsReceiptLineId Id { get; }
    public GoodsReceiptId GoodsReceiptId { get; }
    public PosOrganizationId OrganizationId { get; }
    public PurchaseOrderLineId PurchaseOrderLineId { get; }
    public CatalogProductId ProductId { get; }
    public int LineNumber { get; }
    public string NameSnapshot { get; }
    public UnitOfMeasure UomSnapshot { get; }
    public decimal QuantityReceived { get; }
    public decimal DamagedQty { get; }
    public decimal RejectedQty { get; }
    public decimal ShortClosedQty { get; }
    public ConnectedPoReceivingDiscrepancyKind DiscrepancyKind { get; }
    public string? DiscrepancyNote { get; }
    public decimal UnitPurchaseCostSnapshot { get; }
    public decimal LineTotalSnapshot { get; }
    public decimal MultiplierToBaseSnapshot { get; }
    public DateOnly? ExpiryDate { get; }
    public string? LotNumber { get; }
    public Guid? InventoryMovementId { get; private set; }

    /// <summary>Alias for persistence/DTO mapping compatibility.</summary>
    public decimal ReceivedQty => QuantityReceived;

    /// <summary>Base inventory quantity = purchase-unit good qty × multiplier.</summary>
    public decimal BaseQuantity =>
        ProductUnitConversion.ToBaseQuantity(QuantityReceived, MultiplierToBaseSnapshot);

    /// <summary>Acquisition cost per base inventory unit (purchase-unit cost ÷ multiplier).</summary>
    public decimal BaseUnitCost =>
        ProductUnitConversion.ToBaseUnitCost(UnitPurchaseCostSnapshot, MultiplierToBaseSnapshot);

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
        decimal damagedQty,
        decimal rejectedQty,
        decimal shortClosedQty,
        ConnectedPoReceivingDiscrepancyKind discrepancyKind,
        string? discrepancyNote,
        decimal unitPurchaseCostSnapshot,
        decimal lineTotalSnapshot,
        decimal multiplierToBaseSnapshot,
        Guid? inventoryMovementId,
        DateOnly? expiryDate,
        string? lotNumber)
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
        DamagedQty = damagedQty;
        RejectedQty = rejectedQty;
        ShortClosedQty = shortClosedQty;
        DiscrepancyKind = discrepancyKind;
        DiscrepancyNote = discrepancyNote;
        UnitPurchaseCostSnapshot = unitPurchaseCostSnapshot;
        LineTotalSnapshot = lineTotalSnapshot;
        MultiplierToBaseSnapshot = multiplierToBaseSnapshot;
        InventoryMovementId = inventoryMovementId;
        ExpiryDate = expiryDate;
        LotNumber = lotNumber;
    }

    internal static GoodsReceiptLine Create(
        GoodsReceiptId goodsReceiptId,
        PosOrganizationId organizationId,
        int lineNumber,
        PurchaseOrderLine poLine,
        PurchaseOrderReceiveLineDraft receive,
        GoodsReceiptLineId? id = null)
    {
        if (poLine.NameSnapshot is null || poLine.UomSnapshot is null)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidGoodsReceiptLine,
                "Cannot receive against an unordered line.");
        }

        var good = receive.ReceiveQty <= 0m
            ? 0m
            : PurchaseOrderLine.NormalizeQuantity(receive.ReceiveQty, poLine.UomSnapshot.Value, receive.SellingMode);
        var damaged = receive.DamagedQty <= 0m
            ? 0m
            : PurchaseOrderLine.NormalizeQuantity(receive.DamagedQty, poLine.UomSnapshot.Value, receive.SellingMode);
        var rejected = receive.RejectedQty <= 0m
            ? 0m
            : PurchaseOrderLine.NormalizeQuantity(receive.RejectedQty, poLine.UomSnapshot.Value, receive.SellingMode);
        var shortClosed = receive.ShortClosedQty <= 0m
            ? 0m
            : PurchaseOrderLine.NormalizeQuantity(receive.ShortClosedQty, poLine.UomSnapshot.Value, receive.SellingMode);

        if (good + damaged + rejected + shortClosed <= 0m)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPurchaseReceiveQuantity,
                "Receive quantity must include good, damaged, rejected, or short-closed quantity.");
        }

        var multiplier = CatalogProductUnit.NormalizeMultiplier(poLine.MultiplierToBaseSnapshot);
        var cost = poLine.UnitPurchaseCost;
        var lotNumber = good > 0m ? NormalizeLotNumber(receive.LotNumber) : null;
        var expiry = good > 0m ? receive.ExpiryDate : null;
        return new GoodsReceiptLine(
            id ?? GoodsReceiptLineId.New(),
            goodsReceiptId,
            organizationId,
            poLine.Id,
            poLine.ProductId,
            lineNumber,
            poLine.NameSnapshot,
            poLine.UomSnapshot.Value,
            good,
            damaged,
            rejected,
            shortClosed,
            receive.DiscrepancyKind,
            NormalizeNote(receive.DiscrepancyNote),
            cost,
            SaleMoney.RoundMoney(cost * good),
            multiplier,
            inventoryMovementId: null,
            expiry,
            lotNumber);
    }

    public void AttachInventoryMovement(StockMovementId movementId)
    {
        if (InventoryMovementId is not null)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidGoodsReceiptLine,
                "Inventory movement is already linked to this receipt line.");
        }

        if (QuantityReceived <= 0m)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidGoodsReceiptLine,
                "Only good received quantity can create an inventory movement.");
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
        decimal multiplierToBaseSnapshot = 1m,
        decimal damagedQty = 0m,
        decimal rejectedQty = 0m,
        decimal shortClosedQty = 0m,
        ConnectedPoReceivingDiscrepancyKind discrepancyKind = ConnectedPoReceivingDiscrepancyKind.None,
        string? discrepancyNote = null,
        DateOnly? expiryDate = null,
        string? lotNumber = null) =>
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
            damagedQty,
            rejectedQty,
            shortClosedQty,
            discrepancyKind,
            discrepancyNote,
            unitPurchaseCostSnapshot,
            lineTotalSnapshot,
            multiplierToBaseSnapshot,
            inventoryMovementId,
            expiryDate,
            lotNumber);

    private static string? NormalizeNote(string? note)
    {
        if (string.IsNullOrWhiteSpace(note))
        {
            return null;
        }

        var trimmed = note.Trim();
        if (trimmed.Length > DiscrepancyNoteMaxLength)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidGoodsReceiptNotes,
                $"Discrepancy note must be at most {DiscrepancyNoteMaxLength} characters.");
        }

        return trimmed;
    }

    private static string? NormalizeLotNumber(string? lotNumber)
    {
        var (display, _) = InventoryLot.NormalizeLotNumber(lotNumber);
        return display;
    }
}
