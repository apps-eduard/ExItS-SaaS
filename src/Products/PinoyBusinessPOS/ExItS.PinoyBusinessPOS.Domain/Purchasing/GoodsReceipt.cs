using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Sales;
using ExItS.PinoyBusinessPOS.Domain.Suppliers;

namespace ExItS.PinoyBusinessPOS.Domain.Purchasing;

/// <summary>
/// Immutable goods receipt against a purchase order. Created atomically with PO receive state
/// and optional inventory movements for tracked products.
/// </summary>
public sealed class GoodsReceipt
{
    public const int DeliveryReferenceMaxLength = 128;
    public const int NotesMaxLength = 512;

    private readonly List<GoodsReceiptLine> _lines;

    public GoodsReceiptId Id { get; }
    public PosOrganizationId OrganizationId { get; }
    public PurchaseOrderId PurchaseOrderId { get; }
    public SupplierId SupplierId { get; }
    public string GrnNumber { get; }
    public DateOnly ReceivedDate { get; }
    public string? DeliveryReference { get; }
    public string? Notes { get; }
    public DateTimeOffset ReceivedAtUtc { get; }
    public Guid ReceivedBy { get; }

    public IReadOnlyList<GoodsReceiptLine> Lines => _lines;

    private GoodsReceipt(
        GoodsReceiptId id,
        PosOrganizationId organizationId,
        PurchaseOrderId purchaseOrderId,
        SupplierId supplierId,
        string grnNumber,
        DateOnly receivedDate,
        string? deliveryReference,
        string? notes,
        DateTimeOffset receivedAtUtc,
        Guid receivedBy,
        List<GoodsReceiptLine> lines)
    {
        Id = id;
        OrganizationId = organizationId;
        PurchaseOrderId = purchaseOrderId;
        SupplierId = supplierId;
        GrnNumber = grnNumber;
        ReceivedDate = receivedDate;
        DeliveryReference = deliveryReference;
        Notes = notes;
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
        DateOnly? receivedDate = null,
        string? deliveryReference = null,
        string? notes = null,
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

        var seenLines = new HashSet<Guid>();
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

            if (!seenLines.Add(poLine.Id.Value))
            {
                throw new DomainException(
                    DomainErrorCodes.InvalidGoodsReceiptLine,
                    "Duplicate purchase-order line references are not allowed on a receipt.");
            }

            lines.Add(GoodsReceiptLine.Create(
                grnId,
                organizationId,
                lineNumber++,
                poLine,
                receive.ReceiveQty,
                sellingMode: receive.SellingMode));
        }

        return new GoodsReceipt(
            grnId,
            organizationId,
            purchaseOrderId,
            purchaseOrder.SupplierId,
            GoodsReceiptNumbers.Normalize(grnNumber),
            receivedDate ?? DateOnly.FromDateTime(utcNow.UtcDateTime),
            NormalizeOptional(deliveryReference, DeliveryReferenceMaxLength, DomainErrorCodes.InvalidGoodsReceiptNotes, "Delivery reference"),
            NormalizeOptional(notes, NotesMaxLength, DomainErrorCodes.InvalidGoodsReceiptNotes, "Notes"),
            utcNow,
            receivedBy,
            lines);
    }

    public static GoodsReceipt Rehydrate(
        GoodsReceiptId id,
        PosOrganizationId organizationId,
        PurchaseOrderId purchaseOrderId,
        SupplierId supplierId,
        string grnNumber,
        DateOnly receivedDate,
        string? deliveryReference,
        string? notes,
        DateTimeOffset receivedAtUtc,
        Guid receivedBy,
        IReadOnlyList<GoodsReceiptLine> lines) =>
        new(
            id,
            organizationId,
            purchaseOrderId,
            supplierId,
            grnNumber,
            receivedDate,
            deliveryReference,
            notes,
            receivedAtUtc,
            receivedBy,
            lines.ToList());

    private static string? NormalizeOptional(string? value, int maxLength, string errorCode, string label)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
        {
            throw new DomainException(errorCode, $"{label} must be at most {maxLength} characters.");
        }

        return trimmed;
    }
}
