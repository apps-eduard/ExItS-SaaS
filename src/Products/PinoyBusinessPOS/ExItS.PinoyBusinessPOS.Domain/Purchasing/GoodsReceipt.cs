using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Sales;
using ExItS.PinoyBusinessPOS.Domain.Suppliers;

namespace ExItS.PinoyBusinessPOS.Domain.Purchasing;

/// <summary>
/// Immutable goods receipt against a purchase order. Created atomically with PO receive state
/// and optional inventory movements for tracked products. Correction is void-only
/// (compensating reversal movements); the original receipt is never deleted.
/// </summary>
public sealed class GoodsReceipt
{
    public const int DeliveryReferenceMaxLength = 128;
    public const int NotesMaxLength = 512;
    public const int VoidReasonMaxLength = 512;

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
    public PosBranchId? ReceivingBranchId { get; }
    public GoodsReceiptStatus Status { get; private set; }
    public DateTimeOffset? VoidedAtUtc { get; private set; }
    public Guid? VoidedByUserId { get; private set; }
    public string? VoidReason { get; private set; }

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
        PosBranchId? receivingBranchId,
        List<GoodsReceiptLine> lines,
        GoodsReceiptStatus status = GoodsReceiptStatus.Posted,
        DateTimeOffset? voidedAtUtc = null,
        Guid? voidedByUserId = null,
        string? voidReason = null)
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
        ReceivingBranchId = receivingBranchId;
        Status = status;
        VoidedAtUtc = voidedAtUtc;
        VoidedByUserId = voidedByUserId;
        VoidReason = voidReason;
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
        PosBranchId? receivingBranchId = null,
        GoodsReceiptId? id = null)
    {
        SaleMoney.EnsureUtc(utcNow);
        SaleMoney.EnsureActor(receivedBy);

        if (receivingBranchId is null)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidBranchId,
                "A receiving branch is required for goods receipts.");
        }

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
                receive));
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
            receivingBranchId,
            lines,
            GoodsReceiptStatus.Posted);
    }

    /// <summary>
    /// Marks the document voided. Inventory reversal and PO unwind are applied by the use case.
    /// </summary>
    public void Void(DateTimeOffset utcNow, Guid actorId, string reason)
    {
        SaleMoney.EnsureUtc(utcNow);
        SaleMoney.EnsureActor(actorId);

        if (Status == GoodsReceiptStatus.Voided)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidGoodsReceiptStatusTransition,
                "Goods receipt is already voided.");
        }

        if (Status != GoodsReceiptStatus.Posted)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidGoodsReceiptStatusTransition,
                "Only a posted goods receipt can be voided.");
        }

        VoidReason = NormalizeVoidReason(reason);
        Status = GoodsReceiptStatus.Voided;
        VoidedAtUtc = utcNow;
        VoidedByUserId = actorId;
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
        PosBranchId? receivingBranchId,
        IReadOnlyList<GoodsReceiptLine> lines,
        GoodsReceiptStatus status = GoodsReceiptStatus.Posted,
        DateTimeOffset? voidedAtUtc = null,
        Guid? voidedByUserId = null,
        string? voidReason = null) =>
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
            receivingBranchId,
            lines.ToList(),
            status,
            voidedAtUtc,
            voidedByUserId,
            voidReason);

    private static string NormalizeVoidReason(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidGoodsReceiptVoidReason,
                "A void reason is required.");
        }

        var trimmed = reason.Trim();
        if (trimmed.Length > VoidReasonMaxLength)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidGoodsReceiptVoidReason,
                $"Void reason must be at most {VoidReasonMaxLength} characters.");
        }

        return trimmed;
    }

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
