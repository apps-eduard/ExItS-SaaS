using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.Domain.Inventory;

/// <summary>
/// One receive wave against an in-transit / partially received transfer.
/// Application persists this and may use <see cref="Id"/> as TransferIn SourceId.
/// </summary>
public sealed class InventoryTransferReceipt
{
    private readonly List<InventoryTransferReceiptLine> _lines;

    public InventoryTransferReceiptId Id { get; }
    public PosOrganizationId OrganizationId { get; }
    public InventoryTransferId TransferId { get; }
    public int Sequence { get; }
    public DateTimeOffset ReceivedAtUtc { get; }
    public Guid ReceivedBy { get; }

    public IReadOnlyList<InventoryTransferReceiptLine> Lines => _lines;

    private InventoryTransferReceipt(
        InventoryTransferReceiptId id,
        PosOrganizationId organizationId,
        InventoryTransferId transferId,
        int sequence,
        DateTimeOffset receivedAtUtc,
        Guid receivedBy,
        List<InventoryTransferReceiptLine> lines)
    {
        Id = id;
        OrganizationId = organizationId;
        TransferId = transferId;
        Sequence = sequence;
        ReceivedAtUtc = receivedAtUtc;
        ReceivedBy = receivedBy;
        _lines = lines;
    }

    internal static InventoryTransferReceipt Create(
        PosOrganizationId organizationId,
        InventoryTransferId transferId,
        int sequence,
        DateTimeOffset receivedAtUtc,
        Guid receivedBy,
        IReadOnlyList<InventoryTransferReceiptLineDraft> lines,
        InventoryTransferReceiptId? id = null)
    {
        if (sequence < 1)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidInventoryTransferReceiveQty,
                "Receipt sequence must be at least 1.");
        }

        if (lines is null || lines.Count == 0)
        {
            throw new DomainException(
                DomainErrorCodes.InventoryTransferReceiveRequiresLines,
                "A receipt must contain at least one line.");
        }

        var receiptId = id ?? InventoryTransferReceiptId.New();
        var receiptLines = new List<InventoryTransferReceiptLine>(lines.Count);
        foreach (var line in lines)
        {
            receiptLines.Add(InventoryTransferReceiptLine.Create(
                receiptId,
                line.TransferLineId,
                line.ProductId,
                line.QuantityReceived,
                line.QuantityDamaged,
                line.QuantityMissing,
                line.QuantityOther,
                line.OtherReasonCode,
                line.OtherReasonNote,
                line.MissingDisposition,
                line.Note));
        }

        return new InventoryTransferReceipt(
            receiptId,
            organizationId,
            transferId,
            sequence,
            receivedAtUtc,
            receivedBy,
            receiptLines);
    }

    public static InventoryTransferReceipt Rehydrate(
        InventoryTransferReceiptId id,
        PosOrganizationId organizationId,
        InventoryTransferId transferId,
        int sequence,
        DateTimeOffset receivedAtUtc,
        Guid receivedBy,
        IReadOnlyList<InventoryTransferReceiptLine> lines) =>
        new(
            id,
            organizationId,
            transferId,
            sequence,
            receivedAtUtc,
            receivedBy,
            lines.ToList());
}

internal sealed record InventoryTransferReceiptLineDraft(
    InventoryTransferLineId TransferLineId,
    CatalogProductId ProductId,
    decimal QuantityReceived,
    decimal QuantityDamaged = 0m,
    decimal QuantityMissing = 0m,
    decimal QuantityOther = 0m,
    string? OtherReasonCode = null,
    string? OtherReasonNote = null,
    InventoryTransferMissingDisposition? MissingDisposition = null,
    string? Note = null);

public sealed class InventoryTransferReceiptLine
{
    public InventoryTransferReceiptLineId Id { get; }
    public InventoryTransferReceiptId ReceiptId { get; }
    public InventoryTransferLineId TransferLineId { get; }
    public CatalogProductId ProductId { get; }
    /// <summary>Good (sellable) quantity received in this wave.</summary>
    public decimal QuantityReceived { get; }
    public decimal QuantityDamaged { get; }
    public decimal QuantityMissing { get; }
    public decimal QuantityOther { get; }
    public string? OtherReasonCode { get; }
    public string? OtherReasonNote { get; }
    public InventoryTransferMissingDisposition? MissingDisposition { get; }
    public string? Note { get; }

    private InventoryTransferReceiptLine(
        InventoryTransferReceiptLineId id,
        InventoryTransferReceiptId receiptId,
        InventoryTransferLineId transferLineId,
        CatalogProductId productId,
        decimal quantityReceived,
        decimal quantityDamaged,
        decimal quantityMissing,
        decimal quantityOther,
        string? otherReasonCode,
        string? otherReasonNote,
        InventoryTransferMissingDisposition? missingDisposition,
        string? note)
    {
        Id = id;
        ReceiptId = receiptId;
        TransferLineId = transferLineId;
        ProductId = productId;
        QuantityReceived = quantityReceived;
        QuantityDamaged = quantityDamaged;
        QuantityMissing = quantityMissing;
        QuantityOther = quantityOther;
        OtherReasonCode = otherReasonCode;
        OtherReasonNote = otherReasonNote;
        MissingDisposition = missingDisposition;
        Note = note;
    }

    internal static InventoryTransferReceiptLine Create(
        InventoryTransferReceiptId receiptId,
        InventoryTransferLineId transferLineId,
        CatalogProductId productId,
        decimal quantityReceived,
        decimal quantityDamaged = 0m,
        decimal quantityMissing = 0m,
        decimal quantityOther = 0m,
        string? otherReasonCode = null,
        string? otherReasonNote = null,
        InventoryTransferMissingDisposition? missingDisposition = null,
        string? note = null,
        InventoryTransferReceiptLineId? id = null)
    {
        if (quantityReceived < 0m || quantityDamaged < 0m || quantityMissing < 0m || quantityOther < 0m)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidInventoryTransferReceiveQty,
                "Receipt line quantities cannot be negative.");
        }

        if (quantityReceived + quantityDamaged + quantityMissing + quantityOther <= 0m)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidInventoryTransferReceiveQty,
                "Receipt line must record at least one of good, damaged, missing, or other quantity.");
        }

        ExItS.PinoyBusinessPOS.Domain.Purchasing.ReceiveDiscrepancyOtherReason.EnsureValid(
            otherReasonCode,
            otherReasonNote,
            quantityOther);

        if (quantityMissing > 0m && missingDisposition is null)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidInventoryTransferMissingDisposition,
                "Missing disposition is required when missing quantity is greater than zero.");
        }

        return new InventoryTransferReceiptLine(
            id ?? InventoryTransferReceiptLineId.New(),
            receiptId,
            transferLineId,
            productId,
            quantityReceived,
            quantityDamaged,
            quantityMissing,
            quantityOther,
            otherReasonCode,
            ExItS.PinoyBusinessPOS.Domain.Purchasing.ReceiveDiscrepancyOtherReason.NormalizeNote(otherReasonNote),
            missingDisposition,
            note);
    }

    public static InventoryTransferReceiptLine Rehydrate(
        InventoryTransferReceiptLineId id,
        InventoryTransferReceiptId receiptId,
        InventoryTransferLineId transferLineId,
        CatalogProductId productId,
        decimal quantityReceived,
        decimal quantityDamaged = 0m,
        decimal quantityMissing = 0m,
        decimal quantityOther = 0m,
        string? otherReasonCode = null,
        string? otherReasonNote = null,
        InventoryTransferMissingDisposition? missingDisposition = null,
        string? note = null) =>
        new(
            id,
            receiptId,
            transferLineId,
            productId,
            quantityReceived,
            quantityDamaged,
            quantityMissing,
            quantityOther,
            otherReasonCode,
            otherReasonNote,
            missingDisposition,
            note);
}
