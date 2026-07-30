using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.Domain.Purchasing;

/// <summary>
/// Immutable goods receipt against a purchase order. Created atomically with PO receive state
/// and optional inventory movements for tracked products.
/// </summary>
public sealed class GoodsReceipt
{
    private readonly List<GoodsReceiptLine> _lines;

    public GoodsReceiptId Id { get; }
    public PosOrganizationId OrganizationId { get; }
    public PurchaseOrderId PurchaseOrderId { get; }
    public string GrnNumber { get; }
    public DateTimeOffset ReceivedAtUtc { get; }
    public Guid ReceivedBy { get; }

    public IReadOnlyList<GoodsReceiptLine> Lines => _lines;

    private GoodsReceipt(
        GoodsReceiptId id,
        PosOrganizationId organizationId,
        PurchaseOrderId purchaseOrderId,
        string grnNumber,
        DateTimeOffset receivedAtUtc,
        Guid receivedBy,
        List<GoodsReceiptLine> lines)
    {
        Id = id;
        OrganizationId = organizationId;
        PurchaseOrderId = purchaseOrderId;
        GrnNumber = grnNumber;
        ReceivedAtUtc = receivedAtUtc;
        ReceivedBy = receivedBy;
        _lines = lines;
    }

    public static GoodsReceipt Create(
        PosOrganizationId organizationId,
        PurchaseOrderId purchaseOrderId,
        string grnNumber,
        PurchaseOrder purchaseOrder,
        IReadOnlyList<PurchaseOrderReceiveLineDraft> receiveLines,
        Guid receivedBy,
        DateTimeOffset utcNow,
        GoodsReceiptId? id = null)
    {
        SaleMoney.EnsureUtc(utcNow);
        SaleMoney.EnsureActor(receivedBy);

        if (receiveLines is null || receiveLines.Count == 0)
        {
            throw new DomainException(
                DomainErrorCodes.PurchaseReceiveRequiresLines,
                "At least one receive line is required.");
        }

        var poLineByProduct = purchaseOrder.Lines.ToDictionary(l => l.ProductId.Value);
        var grnId = id ?? GoodsReceiptId.New();
        var lines = new List<GoodsReceiptLine>(receiveLines.Count);
        var lineNumber = 1;
        foreach (var receive in receiveLines.OrderBy(r => r.ProductId.Value))
        {
            if (!poLineByProduct.TryGetValue(receive.ProductId.Value, out var poLine))
            {
                throw new DomainException(
                    DomainErrorCodes.InvalidPurchaseOrderLine,
                    "Receive line product is not on this purchase order.");
            }

            lines.Add(GoodsReceiptLine.Create(grnId, organizationId, lineNumber++, poLine, receive.ReceiveQty));
        }

        return new GoodsReceipt(
            grnId,
            organizationId,
            purchaseOrderId,
            GoodsReceiptNumbers.Normalize(grnNumber),
            utcNow,
            receivedBy,
            lines);
    }

    public static GoodsReceipt Rehydrate(
        GoodsReceiptId id,
        PosOrganizationId organizationId,
        PurchaseOrderId purchaseOrderId,
        string grnNumber,
        DateTimeOffset receivedAtUtc,
        Guid receivedBy,
        IReadOnlyList<GoodsReceiptLine> lines) =>
        new(
            id,
            organizationId,
            purchaseOrderId,
            grnNumber,
            receivedAtUtc,
            receivedBy,
            lines.ToList());
}
