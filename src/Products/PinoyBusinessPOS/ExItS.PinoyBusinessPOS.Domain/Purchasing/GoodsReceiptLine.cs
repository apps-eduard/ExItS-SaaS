using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.Domain.Purchasing;

/// <summary>One immutable line on a goods receipt.</summary>
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
    public decimal ReceivedQty { get; }

    private GoodsReceiptLine(
        GoodsReceiptLineId id,
        GoodsReceiptId goodsReceiptId,
        PosOrganizationId organizationId,
        PurchaseOrderLineId purchaseOrderLineId,
        CatalogProductId productId,
        int lineNumber,
        string nameSnapshot,
        UnitOfMeasure uomSnapshot,
        decimal receivedQty)
    {
        Id = id;
        GoodsReceiptId = goodsReceiptId;
        OrganizationId = organizationId;
        PurchaseOrderLineId = purchaseOrderLineId;
        ProductId = productId;
        LineNumber = lineNumber;
        NameSnapshot = nameSnapshot;
        UomSnapshot = uomSnapshot;
        ReceivedQty = receivedQty;
    }

    internal static GoodsReceiptLine Create(
        GoodsReceiptId goodsReceiptId,
        PosOrganizationId organizationId,
        int lineNumber,
        PurchaseOrderLine poLine,
        decimal receiveQty,
        GoodsReceiptLineId? id = null)
    {
        if (poLine.NameSnapshot is null || poLine.UomSnapshot is null)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidGoodsReceiptLine,
                "Cannot receive against an unordered line.");
        }

        var normalized = PurchaseOrderLine.NormalizeQuantity(receiveQty, poLine.UomSnapshot.Value);
        if (normalized <= 0m)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPurchaseReceiveQuantity,
                "Receive quantity must be greater than zero.");
        }

        return new GoodsReceiptLine(
            id ?? GoodsReceiptLineId.New(),
            goodsReceiptId,
            organizationId,
            poLine.Id,
            poLine.ProductId,
            lineNumber,
            poLine.NameSnapshot,
            poLine.UomSnapshot.Value,
            normalized);
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
        decimal receivedQty) =>
        new(
            id,
            goodsReceiptId,
            organizationId,
            purchaseOrderLineId,
            productId,
            lineNumber,
            nameSnapshot,
            uomSnapshot,
            receivedQty);
}
