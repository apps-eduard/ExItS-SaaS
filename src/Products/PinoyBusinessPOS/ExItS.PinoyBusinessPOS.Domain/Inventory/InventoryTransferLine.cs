using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Purchasing;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.Domain.Inventory;

public sealed record InventoryTransferLineDraft(
    CatalogProductId ProductId,
    decimal Quantity,
    string NameSnapshot,
    UnitOfMeasure UnitOfMeasure,
    SellingMode SellingMode = SellingMode.PerItem,
    InventoryLotId? SourceLotId = null,
    string? LotNumber = null,
    DateOnly? ExpirationDate = null,
    decimal? UnitCostSnapshot = null);

/// <summary>
/// One receive-wave line. <see cref="ReceivedQty"/> is the quantity received in this wave (ReceiveNowQty),
/// not the cumulative total on the transfer line.
/// </summary>
public sealed record InventoryTransferReceiveLineDraft(
    CatalogProductId ProductId,
    decimal ReceivedQty,
    InventoryTransferDiscrepancyReason? DiscrepancyReason = null,
    string? DiscrepancyNote = null,
    SellingMode SellingMode = SellingMode.PerItem,
    InventoryTransferLineId? LineId = null);

/// <summary>Per-line close of remaining outstanding quantity after one or more receive waves.</summary>
public sealed record InventoryTransferCloseRemainderLineDraft(
    InventoryTransferDiscrepancyReason DiscrepancyReason,
    string? DiscrepancyNote = null,
    InventoryTransferLineId? LineId = null,
    CatalogProductId? ProductId = null);

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
    /// <summary>Quantity closed as discrepancy (not received). Set by <see cref="CloseRemainder"/>.</summary>
    public decimal ClosedQty { get; private set; }
    public InventoryTransferDiscrepancyReason? DiscrepancyReason { get; private set; }
    public string? DiscrepancyNote { get; private set; }
    public InventoryLotId? SourceLotId { get; }
    public string? LotNumber { get; }
    public DateOnly? ExpirationDate { get; }
    /// <summary>
    /// Optional acquisition cost per base unit from InventoryCostResolver at create/dispatch.
    /// Dispatch-time value is authoritative for TransferOut/TransferIn movements.
    /// </summary>
    public decimal? UnitCostSnapshot { get; private set; }

    /// <summary>Sent minus received (display shortage; does not subtract closed qty).</summary>
    public decimal DifferenceQty => SentQty - ReceivedQty;

    /// <summary>Remaining open quantity that can still be received or closed.</summary>
    public decimal OutstandingQty => SentQty - ReceivedQty - ClosedQty;

    public string LineStatus
    {
        get
        {
            if (OutstandingQty > 0m)
            {
                return ReceivedQty <= 0m ? "Missing" : "Short";
            }

            if (ClosedQty > 0m)
            {
                return ReceivedQty <= 0m ? "Missing" : "ClosedShort";
            }

            return "Received";
        }
    }

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
        decimal closedQty,
        InventoryTransferDiscrepancyReason? discrepancyReason,
        string? discrepancyNote,
        InventoryLotId? sourceLotId = null,
        string? lotNumber = null,
        DateOnly? expirationDate = null,
        decimal? unitCostSnapshot = null)
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
        ClosedQty = closedQty;
        DiscrepancyReason = discrepancyReason;
        DiscrepancyNote = discrepancyNote;
        SourceLotId = sourceLotId;
        LotNumber = lotNumber;
        ExpirationDate = expirationDate;
        UnitCostSnapshot = unitCostSnapshot;
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
            closedQty: 0m,
            discrepancyReason: null,
            discrepancyNote: null,
            draft.SourceLotId,
            draft.LotNumber,
            draft.ExpirationDate,
            NormalizeOptionalUnitCost(draft.UnitCostSnapshot));
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

    /// <summary>Refreshes acquisition cost snapshot (dispatch-time is authoritative).</summary>
    internal void SetUnitCostSnapshot(decimal? unitCostSnapshot) =>
        UnitCostSnapshot = NormalizeOptionalUnitCost(unitCostSnapshot);

    /// <summary>
    /// Accumulates a receive-wave quantity. Discrepancy reason is not required here;
    /// use <see cref="CloseRemainder"/> to close remaining outstanding with a reason.
    /// </summary>
    internal decimal ApplyReceiptDelta(InventoryTransferReceiveLineDraft receive)
    {
        var qty = receive.ReceivedQty == 0m
            ? 0m
            : SaleLine.NormalizeQuantity(receive.ReceivedQty, UnitOfMeasure, receive.SellingMode);

        if (qty <= 0m || qty > OutstandingQty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidInventoryTransferReceiveQty,
                "Receive quantity must be greater than zero and not exceed outstanding quantity.");
        }

        ReceivedQty += qty;
        return qty;
    }

    /// <summary>Closes all remaining outstanding quantity as discrepancy.</summary>
    internal void CloseRemainder(InventoryTransferDiscrepancyReason reason, string? note)
    {
        var outstanding = OutstandingQty;
        if (outstanding <= 0m)
        {
            return;
        }

        ClosedQty += outstanding;
        DiscrepancyReason = reason;
        DiscrepancyNote = NormalizeNote(note);
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
        string? discrepancyNote,
        InventoryLotId? sourceLotId = null,
        string? lotNumber = null,
        DateOnly? expirationDate = null,
        decimal? unitCostSnapshot = null,
        decimal closedQty = 0m) =>
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
            closedQty,
            discrepancyReason,
            discrepancyNote,
            sourceLotId,
            lotNumber,
            expirationDate,
            unitCostSnapshot);

    private static decimal? NormalizeOptionalUnitCost(decimal? unitCost)
    {
        if (unitCost is null)
        {
            return null;
        }

        if (unitCost.Value < 0m)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPurchaseUnitCost,
                "Transfer unit cost snapshot cannot be negative.");
        }

        if (unitCost.Value > PurchaseOrderLine.MaxUnitPurchaseCost)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPurchaseUnitCost,
                "Transfer unit cost snapshot is too large.");
        }

        return SaleMoney.RoundMoney(unitCost.Value);
    }

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
