using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.Domain.Inventory;

/// <summary>
/// Intra-organization branch-to-branch inventory transfer. Draft has no stock effect.
/// Dispatch freezes sent quantities and leaves destination sellable stock unchanged until receive.
/// Receive supports multiple waves; remaining outstanding may be closed with discrepancy.
/// </summary>
public sealed class InventoryTransfer
{
    public const int NotesMaxLength = 512;
    public const int MaxLineCount = 200;

    private readonly List<InventoryTransferLine> _lines;
    private readonly List<InventoryTransferReceipt> _receipts;

    public InventoryTransferId Id { get; }
    public PosOrganizationId OrganizationId { get; }
    public StockRequestId? StockRequestId { get; private set; }
    public string? TransferNumber { get; private set; }
    public PosBranchId SourceBranchId { get; }
    public PosBranchId DestinationBranchId { get; }
    public InventoryTransferStatus Status { get; private set; }
    public string? Notes { get; private set; }
    public Guid CreatedBy { get; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public DateTimeOffset? DispatchedAtUtc { get; private set; }
    public Guid? DispatchedBy { get; private set; }
    public DateTimeOffset? ReceivedAtUtc { get; private set; }
    public Guid? ReceivedBy { get; private set; }
    public DateTimeOffset? CancelledAtUtc { get; private set; }
    public Guid? CancelledBy { get; private set; }
    public DateTimeOffset? ClosedAtUtc { get; private set; }
    public Guid? ClosedBy { get; private set; }

    public IReadOnlyList<InventoryTransferLine> Lines => _lines;

    public IReadOnlyList<InventoryTransferReceipt> Receipts => _receipts;

    public decimal TotalSentQty => _lines.Sum(l => l.SentQty);

    public decimal TotalReceivedQty => _lines.Sum(l => l.ReceivedQty);

    public decimal TotalClosedQty => _lines.Sum(l => l.ClosedQty);

    public decimal TotalOutstandingQty => _lines.Sum(l => l.OutstandingQty);

    public decimal TotalDifferenceQty => TotalSentQty - TotalReceivedQty;

    private InventoryTransfer(
        InventoryTransferId id,
        PosOrganizationId organizationId,
        StockRequestId? stockRequestId,
        string? transferNumber,
        PosBranchId sourceBranchId,
        PosBranchId destinationBranchId,
        InventoryTransferStatus status,
        string? notes,
        Guid createdBy,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc,
        DateTimeOffset? dispatchedAtUtc,
        Guid? dispatchedBy,
        DateTimeOffset? receivedAtUtc,
        Guid? receivedBy,
        DateTimeOffset? cancelledAtUtc,
        Guid? cancelledBy,
        DateTimeOffset? closedAtUtc,
        Guid? closedBy,
        List<InventoryTransferLine> lines,
        List<InventoryTransferReceipt>? receipts = null)
    {
        Id = id;
        OrganizationId = organizationId;
        StockRequestId = stockRequestId;
        TransferNumber = transferNumber;
        SourceBranchId = sourceBranchId;
        DestinationBranchId = destinationBranchId;
        Status = status;
        Notes = notes;
        CreatedBy = createdBy;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
        DispatchedAtUtc = dispatchedAtUtc;
        DispatchedBy = dispatchedBy;
        ReceivedAtUtc = receivedAtUtc;
        ReceivedBy = receivedBy;
        CancelledAtUtc = cancelledAtUtc;
        CancelledBy = cancelledBy;
        ClosedAtUtc = closedAtUtc;
        ClosedBy = closedBy;
        _lines = lines;
        _receipts = receipts ?? [];
    }

    public static InventoryTransfer CreateDraft(
        PosOrganizationId organizationId,
        PosBranchId sourceBranchId,
        PosBranchId destinationBranchId,
        IReadOnlyList<InventoryTransferLineDraft> lines,
        Guid createdBy,
        DateTimeOffset utcNow,
        string? notes = null,
        InventoryTransferId? id = null,
        StockRequestId? stockRequestId = null)
    {
        SaleMoney.EnsureUtc(utcNow);
        EnsureActor(createdBy);
        EnsureDistinctBranches(sourceBranchId, destinationBranchId);
        EnsureLines(lines);

        var transferId = id ?? InventoryTransferId.New();
        return new InventoryTransfer(
            transferId,
            organizationId,
            stockRequestId,
            transferNumber: null,
            sourceBranchId,
            destinationBranchId,
            InventoryTransferStatus.Draft,
            NormalizeNotes(notes),
            createdBy,
            utcNow,
            utcNow,
            dispatchedAtUtc: null,
            dispatchedBy: null,
            receivedAtUtc: null,
            receivedBy: null,
            cancelledAtUtc: null,
            cancelledBy: null,
            closedAtUtc: null,
            closedBy: null,
            BuildDraftLines(transferId, organizationId, lines));
    }

    public InventoryTransfer WithStockRequest(StockRequestId stockRequestId)
    {
        StockRequestId = stockRequestId;
        return this;
    }

    public void UpdateDraft(
        IReadOnlyList<InventoryTransferLineDraft> lines,
        DateTimeOffset utcNow,
        string? notes = null)
    {
        SaleMoney.EnsureUtc(utcNow);
        EnsureDraft();
        EnsureLines(lines);
        _lines.Clear();
        _lines.AddRange(BuildDraftLines(Id, OrganizationId, lines));
        Notes = NormalizeNotes(notes);
        UpdatedAtUtc = utcNow;
    }

    public void Dispatch(string transferNumber, Guid actorId, DateTimeOffset utcNow)
    {
        SaleMoney.EnsureUtc(utcNow);
        EnsureActor(actorId);
        EnsureDraft();
        TransferNumber = InventoryTransferNumbers.Normalize(transferNumber);
        Status = InventoryTransferStatus.InTransit;
        DispatchedAtUtc = utcNow;
        DispatchedBy = actorId;
        UpdatedAtUtc = utcNow;
    }

    /// <summary>
    /// Refreshes per-line acquisition cost snapshots while the transfer is still draft.
    /// Dispatch-time refresh is authoritative for TransferOut/TransferIn movements.
    /// </summary>
    public void RefreshLineUnitCosts(IReadOnlyDictionary<Guid, decimal?> costsByProductId)
    {
        EnsureDraft();
        foreach (var line in _lines)
        {
            costsByProductId.TryGetValue(line.ProductId.Value, out var cost);
            line.SetUnitCostSnapshot(cost);
        }
    }

    /// <summary>
    /// Applies a receive wave for lines with quantity greater than zero.
    /// Returns the receipt so Application can persist it and use receipt.Id as TransferIn SourceId.
    /// </summary>
    public InventoryTransferReceipt Receive(
        IReadOnlyList<InventoryTransferReceiveLineDraft> receiveLines,
        Guid actorId,
        DateTimeOffset utcNow)
    {
        SaleMoney.EnsureUtc(utcNow);
        EnsureActor(actorId);
        if (Status is not (InventoryTransferStatus.InTransit or InventoryTransferStatus.PartiallyReceived))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidInventoryTransferStatusTransition,
                Status is InventoryTransferStatus.Received or InventoryTransferStatus.ClosedWithDiscrepancy
                    ? "This transfer has already been completed."
                    : "Only in-transit or partially received transfers can be received.");
        }

        if (receiveLines is null || receiveLines.Count == 0)
        {
            throw new DomainException(
                DomainErrorCodes.InventoryTransferReceiveRequiresLines,
                "At least one receive line is required.");
        }

        var lineById = _lines.ToDictionary(l => l.Id.Value);
        var lineByProduct = _lines
            .GroupBy(l => l.ProductId.Value)
            .ToDictionary(g => g.Key, g => g.ToList());
        var seen = new HashSet<Guid>();
        var applied = new List<InventoryTransferReceiptLineDraft>();

        foreach (var receive in receiveLines)
        {
            if (receive.GoodQty + receive.DamagedQty + receive.MissingQty + receive.OtherQty <= 0m)
            {
                continue;
            }

            var line = ResolveLine(receive.LineId, receive.ProductId, lineById, lineByProduct);

            if (!seen.Add(line.Id.Value))
            {
                throw new DomainException(
                    DomainErrorCodes.InventoryTransferDuplicateProduct,
                    "Receive lines cannot repeat the same transfer line.");
            }

            var (goodDelta, damagedWave, missingWave, otherWave, _, waivedDelta) = line.ApplyReceiptClassification(receive);
            applied.Add(new InventoryTransferReceiptLineDraft(
                line.Id,
                line.ProductId,
                goodDelta,
                damagedWave,
                missingWave,
                otherWave,
                receive.OtherReasonCode,
                receive.OtherReasonNote,
                receive.MissingDisposition,
                receive.DamagedFollowUp,
                receive.OtherFollowUp,
                waivedDelta,
                receive.DiscrepancyNote));
        }

        if (applied.Count == 0)
        {
            throw new DomainException(
                DomainErrorCodes.InventoryTransferReceiveRequiresLines,
                "At least one receive line with quantity greater than zero is required.");
        }

        var receipt = InventoryTransferReceipt.Create(
            OrganizationId,
            Id,
            sequence: _receipts.Count + 1,
            utcNow,
            actorId,
            applied);
        _receipts.Add(receipt);

        if (TotalOutstandingQty == 0m)
        {
            if (TotalClosedQty > 0m)
            {
                Status = InventoryTransferStatus.ClosedWithDiscrepancy;
                ClosedAtUtc ??= utcNow;
                ClosedBy ??= actorId;
            }
            else
            {
                Status = InventoryTransferStatus.Received;
            }
        }
        else if (TotalReceivedQty > 0m || TotalClosedQty > 0m)
        {
            Status = InventoryTransferStatus.PartiallyReceived;
        }
        else
        {
            Status = InventoryTransferStatus.InTransit;
        }

        if (ReceivedAtUtc is null)
        {
            ReceivedAtUtc = utcNow;
            ReceivedBy = actorId;
        }

        UpdatedAtUtc = utcNow;
        return receipt;
    }

    /// <summary>
    /// Closes all remaining outstanding quantity with discrepancy reasons.
    /// Only allowed while partially received. Provide per-line closures and/or a transfer-level reason.
    /// </summary>
    public void CloseRemainder(
        Guid actorId,
        DateTimeOffset utcNow,
        IReadOnlyList<InventoryTransferCloseRemainderLineDraft>? closeLines = null,
        InventoryTransferDiscrepancyReason? transferLevelReason = null,
        string? transferLevelNote = null)
    {
        SaleMoney.EnsureUtc(utcNow);
        EnsureActor(actorId);
        if (Status != InventoryTransferStatus.PartiallyReceived)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidInventoryTransferStatusTransition,
                "Only partially received transfers can close remaining quantity with discrepancy.");
        }

        var outstanding = _lines.Where(l => l.OutstandingQty > 0m).ToList();
        if (outstanding.Count == 0)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidInventoryTransferStatusTransition,
                "There is no outstanding quantity to close.");
        }

        var lineById = _lines.ToDictionary(l => l.Id.Value);
        var lineByProduct = _lines
            .GroupBy(l => l.ProductId.Value)
            .ToDictionary(g => g.Key, g => g.ToList());
        var closureByLineId = new Dictionary<Guid, InventoryTransferCloseRemainderLineDraft>();

        foreach (var close in closeLines ?? Array.Empty<InventoryTransferCloseRemainderLineDraft>())
        {
            InventoryTransferLine line;
            if (close.LineId is not null)
            {
                if (!lineById.TryGetValue(close.LineId.Value, out line!))
                {
                    throw new DomainException(
                        DomainErrorCodes.InvalidInventoryTransferLine,
                        "Close line is not on this transfer.");
                }
            }
            else if (close.ProductId is not null
                     && lineByProduct.TryGetValue(close.ProductId.Value, out var matches)
                     && matches.Count == 1)
            {
                line = matches[0];
            }
            else
            {
                throw new DomainException(
                    DomainErrorCodes.InvalidInventoryTransferLine,
                    "Close line must specify a transfer line id or unique product.");
            }

            if (!closureByLineId.TryAdd(line.Id.Value, close))
            {
                throw new DomainException(
                    DomainErrorCodes.InventoryTransferDuplicateProduct,
                    "Close lines cannot repeat the same transfer line.");
            }
        }

        foreach (var line in outstanding)
        {
            InventoryTransferDiscrepancyReason reason;
            string? note;
            if (closureByLineId.TryGetValue(line.Id.Value, out var closure))
            {
                reason = closure.DiscrepancyReason;
                note = closure.DiscrepancyNote;
            }
            else if (transferLevelReason is not null)
            {
                reason = transferLevelReason.Value;
                note = transferLevelNote;
            }
            else
            {
                throw new DomainException(
                    DomainErrorCodes.InvalidInventoryTransferDiscrepancyReason,
                    "Discrepancy reason is required for each outstanding line when closing remainder.");
            }

            line.CloseRemainder(reason, note);
        }

        Status = InventoryTransferStatus.ClosedWithDiscrepancy;
        ClosedAtUtc = utcNow;
        ClosedBy = actorId;
        UpdatedAtUtc = utcNow;
    }

    public void Cancel(Guid actorId, DateTimeOffset utcNow)
    {
        SaleMoney.EnsureUtc(utcNow);
        EnsureActor(actorId);
        if (Status == InventoryTransferStatus.Draft)
        {
            Status = InventoryTransferStatus.Cancelled;
            CancelledAtUtc = utcNow;
            CancelledBy = actorId;
            UpdatedAtUtc = utcNow;
            return;
        }

        if (Status is InventoryTransferStatus.PartiallyReceived
            or InventoryTransferStatus.Received
            or InventoryTransferStatus.ClosedWithDiscrepancy
            || TotalReceivedQty > 0m
            || _receipts.Count > 0)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidInventoryTransferStatusTransition,
                "Only draft or in-transit transfers that have not been received can be cancelled.");
        }

        if (Status != InventoryTransferStatus.InTransit)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidInventoryTransferStatusTransition,
                "Only draft or in-transit transfers that have not been received can be cancelled.");
        }

        Status = InventoryTransferStatus.Cancelled;
        CancelledAtUtc = utcNow;
        CancelledBy = actorId;
        UpdatedAtUtc = utcNow;
    }

    public static InventoryTransfer Rehydrate(
        InventoryTransferId id,
        PosOrganizationId organizationId,
        StockRequestId? stockRequestId,
        string? transferNumber,
        PosBranchId sourceBranchId,
        PosBranchId destinationBranchId,
        InventoryTransferStatus status,
        string? notes,
        Guid createdBy,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc,
        DateTimeOffset? dispatchedAtUtc,
        Guid? dispatchedBy,
        DateTimeOffset? receivedAtUtc,
        Guid? receivedBy,
        DateTimeOffset? cancelledAtUtc,
        Guid? cancelledBy,
        DateTimeOffset? closedAtUtc,
        Guid? closedBy,
        IReadOnlyList<InventoryTransferLine> lines,
        IReadOnlyList<InventoryTransferReceipt>? receipts = null) =>
        new(
            id,
            organizationId,
            stockRequestId,
            transferNumber,
            sourceBranchId,
            destinationBranchId,
            status,
            notes,
            createdBy,
            createdAtUtc,
            updatedAtUtc,
            dispatchedAtUtc,
            dispatchedBy,
            receivedAtUtc,
            receivedBy,
            cancelledAtUtc,
            cancelledBy,
            closedAtUtc,
            closedBy,
            lines.ToList(),
            receipts?.OrderBy(r => r.Sequence).ToList());

    private static InventoryTransferLine ResolveLine(
        InventoryTransferLineId? lineId,
        CatalogProductId productId,
        Dictionary<Guid, InventoryTransferLine> lineById,
        Dictionary<Guid, List<InventoryTransferLine>> lineByProduct)
    {
        if (lineId is not null)
        {
            if (!lineById.TryGetValue(lineId.Value, out var byId))
            {
                throw new DomainException(
                    DomainErrorCodes.InvalidInventoryTransferLine,
                    "Receive line is not on this transfer.");
            }

            return byId;
        }

        if (lineByProduct.TryGetValue(productId.Value, out var matches) && matches.Count == 1)
        {
            return matches[0];
        }

        throw new DomainException(
            DomainErrorCodes.InvalidInventoryTransferLine,
            "Receive line product is not on this transfer.");
    }

    private void EnsureDraft()
    {
        if (Status != InventoryTransferStatus.Draft)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidInventoryTransferStatusTransition,
                "Only draft transfers can be edited.");
        }
    }

    private static void EnsureDistinctBranches(PosBranchId source, PosBranchId destination)
    {
        if (source == destination)
        {
            throw new DomainException(
                DomainErrorCodes.InventoryTransferSameBranch,
                "Source and destination branches must be different.");
        }
    }

    private static void EnsureActor(Guid actorId)
    {
        if (actorId == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidSaleActor,
                "A non-empty actor identifier is required.");
        }
    }

    private static void EnsureLines(IReadOnlyList<InventoryTransferLineDraft> lines)
    {
        if (lines is null || lines.Count == 0)
        {
            throw new DomainException(
                DomainErrorCodes.InventoryTransferRequiresLines,
                "A transfer must contain at least one line.");
        }

        if (lines.Count > MaxLineCount)
        {
            throw new DomainException(
                DomainErrorCodes.InventoryTransferRequiresLines,
                $"A transfer may contain at most {MaxLineCount} lines.");
        }

        var keys = lines
            .Select(l => (l.ProductId.Value, Lot: l.SourceLotId?.Value ?? Guid.Empty))
            .ToList();
        if (keys.Count != keys.Distinct().Count())
        {
            throw new DomainException(
                DomainErrorCodes.InventoryTransferDuplicateProduct,
                "A transfer cannot contain the same product lot more than once.");
        }
    }

    private static List<InventoryTransferLine> BuildDraftLines(
        InventoryTransferId transferId,
        PosOrganizationId organizationId,
        IReadOnlyList<InventoryTransferLineDraft> lines)
    {
        var result = new List<InventoryTransferLine>(lines.Count);
        for (var i = 0; i < lines.Count; i++)
        {
            result.Add(InventoryTransferLine.CreateDraft(transferId, organizationId, i + 1, lines[i]));
        }

        return result;
    }

    private static string? NormalizeNotes(string? notes)
    {
        if (string.IsNullOrWhiteSpace(notes))
        {
            return null;
        }

        var trimmed = notes.Trim();
        if (trimmed.Length > NotesMaxLength)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidInventoryTransferNotes,
                $"Notes must be at most {NotesMaxLength} characters.");
        }

        return trimmed;
    }
}
