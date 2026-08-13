using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.Domain.Inventory;

public sealed record InventoryTransferLineDraft(
    CatalogProductId ProductId,
    decimal Quantity,
    string NameSnapshot,
    UnitOfMeasure UnitOfMeasure,
    SellingMode SellingMode = SellingMode.PerItem);

public sealed record InventoryTransferReceiveLineDraft(
    CatalogProductId ProductId,
    decimal ReceivedQty,
    InventoryTransferDiscrepancyReason? DiscrepancyReason = null,
    string? DiscrepancyNote = null,
    SellingMode SellingMode = SellingMode.PerItem);

public sealed class InventoryTransferLine
{
    public const int NameSnapshotMaxLength = 200;
    public const int DiscrepancyNoteMaxLength = 512;

    public InventoryTransferLineId Id { get; }
    public InventoryTransferId TransferId { get; }
    public PosOrganizationId OrganizationId { get; }
    public CatalogProductId ProductId { get; }
    public int LineNumber { get; }
    public string NameSnapshot { get; }
    public UnitOfMeasure UnitOfMeasure { get; }
    public decimal SentQty { get; private set; }
    public decimal ReceivedQty { get; private set; }
    public InventoryTransferDiscrepancyReason? DiscrepancyReason { get; private set; }
    public string? DiscrepancyNote { get; private set; }

    public decimal DifferenceQty => SentQty - ReceivedQty;

    public string LineStatus =>
        ReceivedQty <= 0m && SentQty > 0m
            ? "Missing"
            : DifferenceQty > 0m
                ? "Short"
                : "Received";

    private InventoryTransferLine(
        InventoryTransferLineId id,
        InventoryTransferId transferId,
        PosOrganizationId organizationId,
        CatalogProductId productId,
        int lineNumber,
        string nameSnapshot,
        UnitOfMeasure unitOfMeasure,
        decimal sentQty,
        decimal receivedQty,
        InventoryTransferDiscrepancyReason? discrepancyReason,
        string? discrepancyNote)
    {
        Id = id;
        TransferId = transferId;
        OrganizationId = organizationId;
        ProductId = productId;
        LineNumber = lineNumber;
        NameSnapshot = nameSnapshot;
        UnitOfMeasure = unitOfMeasure;
        SentQty = sentQty;
        ReceivedQty = receivedQty;
        DiscrepancyReason = discrepancyReason;
        DiscrepancyNote = discrepancyNote;
    }

    internal static InventoryTransferLine CreateDraft(
        InventoryTransferId transferId,
        PosOrganizationId organizationId,
        int lineNumber,
        InventoryTransferLineDraft draft,
        InventoryTransferLineId? id = null)
    {
        var name = NormalizeName(draft.NameSnapshot);
        if (draft.Quantity <= 0m)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidInventoryTransferQuantity,
                "Transfer quantity must be greater than zero.");
        }

        var qty = SaleLine.NormalizeQuantity(draft.Quantity, draft.UnitOfMeasure, draft.SellingMode);

        return new InventoryTransferLine(
            id ?? InventoryTransferLineId.New(),
            transferId,
            organizationId,
            draft.ProductId,
            lineNumber,
            name,
            draft.UnitOfMeasure,
            qty,
            receivedQty: 0m,
            discrepancyReason: null,
            discrepancyNote: null);
    }

    internal void ReplaceDraftQuantity(decimal quantity, SellingMode sellingMode)
    {
        if (quantity <= 0m)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidInventoryTransferQuantity,
                "Transfer quantity must be greater than zero.");
        }

        var qty = SaleLine.NormalizeQuantity(quantity, UnitOfMeasure, sellingMode);

        SentQty = qty;
    }

    internal void ApplyReceipt(InventoryTransferReceiveLineDraft receive)
    {
        var qty = receive.ReceivedQty == 0m
            ? 0m
            : SaleLine.NormalizeQuantity(receive.ReceivedQty, UnitOfMeasure, receive.SellingMode);

        if (qty < 0m || qty > SentQty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidInventoryTransferReceiveQty,
                "Received quantity must be between zero and the sent quantity.");
        }

        ReceivedQty = qty;
        DiscrepancyNote = NormalizeNote(receive.DiscrepancyNote);
        if (qty < SentQty)
        {
            DiscrepancyReason = receive.DiscrepancyReason;
        }
        else
        {
            DiscrepancyReason = null;
            DiscrepancyNote = null;
        }
    }

    public static InventoryTransferLine Rehydrate(
        InventoryTransferLineId id,
        InventoryTransferId transferId,
        PosOrganizationId organizationId,
        CatalogProductId productId,
        int lineNumber,
        string nameSnapshot,
        UnitOfMeasure unitOfMeasure,
        decimal sentQty,
        decimal receivedQty,
        InventoryTransferDiscrepancyReason? discrepancyReason,
        string? discrepancyNote) =>
        new(
            id,
            transferId,
            organizationId,
            productId,
            lineNumber,
            nameSnapshot,
            unitOfMeasure,
            sentQty,
            receivedQty,
            discrepancyReason,
            discrepancyNote);

    private static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidInventoryTransferLine,
                "Transfer line product name is required.");
        }

        var trimmed = name.Trim();
        if (trimmed.Length > NameSnapshotMaxLength)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidInventoryTransferLine,
                $"Product name must be at most {NameSnapshotMaxLength} characters.");
        }

        return trimmed;
    }

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
                DomainErrorCodes.InvalidInventoryTransferDiscrepancyNote,
                $"Discrepancy note must be at most {DiscrepancyNoteMaxLength} characters.");
        }

        return trimmed;
    }
}
