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
        IReadOnlyList<(InventoryTransferLineId TransferLineId, CatalogProductId ProductId, decimal QuantityReceived)> lines,
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
                line.QuantityReceived));
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

public sealed class InventoryTransferReceiptLine
{
    public InventoryTransferReceiptLineId Id { get; }
    public InventoryTransferReceiptId ReceiptId { get; }
    public InventoryTransferLineId TransferLineId { get; }
    public CatalogProductId ProductId { get; }
    public decimal QuantityReceived { get; }

    private InventoryTransferReceiptLine(
        InventoryTransferReceiptLineId id,
        InventoryTransferReceiptId receiptId,
        InventoryTransferLineId transferLineId,
        CatalogProductId productId,
        decimal quantityReceived)
    {
        Id = id;
        ReceiptId = receiptId;
        TransferLineId = transferLineId;
        ProductId = productId;
        QuantityReceived = quantityReceived;
    }

    internal static InventoryTransferReceiptLine Create(
        InventoryTransferReceiptId receiptId,
        InventoryTransferLineId transferLineId,
        CatalogProductId productId,
        decimal quantityReceived,
        InventoryTransferReceiptLineId? id = null)
    {
        if (quantityReceived <= 0m)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidInventoryTransferReceiveQty,
                "Receipt line quantity must be greater than zero.");
        }

        return new InventoryTransferReceiptLine(
            id ?? InventoryTransferReceiptLineId.New(),
            receiptId,
            transferLineId,
            productId,
            quantityReceived);
    }

    public static InventoryTransferReceiptLine Rehydrate(
        InventoryTransferReceiptLineId id,
        InventoryTransferReceiptId receiptId,
        InventoryTransferLineId transferLineId,
        CatalogProductId productId,
        decimal quantityReceived) =>
        new(id, receiptId, transferLineId, productId, quantityReceived);
}
