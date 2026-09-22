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
/// One receive-wave line. <see cref="GoodQty"/> is sellable quantity for this wave only (not cumulative).
/// </summary>
public sealed record InventoryTransferReceiveLineDraft(
    CatalogProductId ProductId,
    decimal GoodQty,
    InventoryTransferDiscrepancyReason? DiscrepancyReason = null,
    string? DiscrepancyNote = null,
    SellingMode SellingMode = SellingMode.PerItem,
    InventoryTransferLineId? LineId = null,
    decimal DamagedQty = 0m,
    decimal MissingQty = 0m,
    decimal OtherQty = 0m,
    string? OtherReasonCode = null,
    string? OtherReasonNote = null,
    InventoryTransferMissingDisposition? MissingDisposition = null,
    InventoryTransferDiscrepancyFollowUp? DamagedFollowUp = null,
    InventoryTransferDiscrepancyFollowUp? OtherFollowUp = null,
    InventoryTransferDamagedCustodyDecision? DamagedCustodyDecision = null)
{
    /// <summary>Backward-compatible alias for <see cref="GoodQty"/>.</summary>
    public decimal ReceivedQty => GoodQty;
}

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
    /// <summary>Quantity permanently accepted as shortage (does not increase remaining to dispatch).</summary>
    public decimal WaivedQty { get; private set; }
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
        decimal? unitCostSnapshot = null,
        decimal waivedQty = 0m)
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
        WaivedQty = waivedQty;
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
    /// Classifies outstanding quantity for one receive wave. Returns good, damaged-closed, and missing-closed deltas.
    /// </summary>
    internal (decimal GoodDelta, decimal DamagedWave, decimal MissingWave, decimal OtherWave, decimal MissingClosed, decimal WaivedDelta) ApplyReceiptClassification(
        InventoryTransferReceiveLineDraft receive)
    {
        var outstandingBefore = OutstandingQty;
        var good = NormalizeWaveQty(receive.GoodQty, receive.SellingMode);
        var damaged = NormalizeWaveQty(receive.DamagedQty, receive.SellingMode);
        var missing = NormalizeWaveQty(receive.MissingQty, receive.SellingMode);
        var other = NormalizeWaveQty(receive.OtherQty, receive.SellingMode);
        var waveTotal = good + damaged + missing + other;

        if (waveTotal <= 0m)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidInventoryTransferReceiveQty,
                "At least one of good, damaged, missing, or other quantity must be greater than zero.");
        }

        if (good < 0m || damaged < 0m || missing < 0m || other < 0m)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidInventoryTransferReceiveQty,
                "Receive quantities cannot be negative.");
        }

        ReceiveDiscrepancyOtherReason.EnsureValid(receive.OtherReasonCode, receive.OtherReasonNote, other);

        if (waveTotal > outstandingBefore || good > outstandingBefore)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidInventoryTransferReceiveQty,
                "Receive quantities cannot exceed outstanding quantity.");
        }

        if (missing > 0m && receive.MissingDisposition is null)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidInventoryTransferMissingDisposition,
                "Missing disposition is required when missing quantity is greater than zero.");
        }

        if (damaged > 0m && receive.DamagedFollowUp is null)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidInventoryTransferDiscrepancyFollowUp,
                "Damaged follow-up is required when damaged quantity is greater than zero.");
        }

        if (other > 0m && receive.OtherFollowUp is null)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidInventoryTransferDiscrepancyFollowUp,
                "Other follow-up is required when other quantity is greater than zero.");
        }

        var difference = outstandingBefore - good;
        if (damaged + missing + other > difference)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidInventoryTransferReceiveClassification,
                "Damaged, missing, and other quantities cannot exceed the shortfall against outstanding quantity.");
        }

        if ((damaged > 0m || missing > 0m || other > 0m) && damaged + missing + other != difference)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidInventoryTransferReceiveClassification,
                "When classifying a shortfall, damaged plus missing plus other must equal the discrepancy quantity.");
        }

        var missingClosed = receive.MissingDisposition is InventoryTransferMissingDisposition.CloseMissing
            or InventoryTransferMissingDisposition.AcceptShortage
            ? missing
            : 0m;

        var waivedDelta = 0m;
        if (missing > 0m && receive.MissingDisposition == InventoryTransferMissingDisposition.AcceptShortage)
        {
            waivedDelta += missing;
        }

        if (damaged > 0m && receive.DamagedFollowUp == InventoryTransferDiscrepancyFollowUp.AcceptShortage)
        {
            waivedDelta += damaged;
        }

        if (other > 0m && receive.OtherFollowUp == InventoryTransferDiscrepancyFollowUp.AcceptShortage)
        {
            waivedDelta += other;
        }

        ReceivedQty += good;
        var closedDelta = damaged + other + missingClosed;
        if (closedDelta > 0m)
        {
            ClosedQty += closedDelta;
            if (damaged > 0m)
            {
                DiscrepancyReason = InventoryTransferDiscrepancyReason.Damaged;
            }
            else if (missingClosed > 0m)
            {
                DiscrepancyReason = InventoryTransferDiscrepancyReason.ShortShipment;
            }
            else if (other > 0m)
            {
                DiscrepancyReason = ReceiveDiscrepancyOtherReason.ResolveTransferReason(receive.OtherReasonCode);
            }

            var note = NormalizeNote(receive.DiscrepancyNote);
            if (note is not null)
            {
                DiscrepancyNote = note;
            }
        }

        if (waivedDelta > 0m)
        {
            WaivedQty += waivedDelta;
        }

        return (good, damaged, missing, other, missingClosed, waivedDelta);
    }

    private decimal NormalizeWaveQty(decimal qty, SellingMode sellingMode) =>
        qty == 0m ? 0m : SaleLine.NormalizeQuantity(qty, UnitOfMeasure, sellingMode);

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
        decimal closedQty = 0m,
        decimal waivedQty = 0m) =>
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
            unitCostSnapshot,
            waivedQty);

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
