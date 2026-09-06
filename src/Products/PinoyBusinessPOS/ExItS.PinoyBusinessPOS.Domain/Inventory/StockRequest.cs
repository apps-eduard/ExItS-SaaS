using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.Domain.Inventory;

public sealed class StockRequest
{
    public const int NotesMaxLength = 512;
    public const int RejectionReasonMaxLength = 512;
    public const int MaxLineCount = 200;

    private readonly List<StockRequestLine> _lines;

    public StockRequestId Id { get; }
    public PosOrganizationId OrganizationId { get; }
    public PosBranchId DestinationLocationId { get; }
    public PosBranchId RequestedSourceLocationId { get; }
    public string? RequestNumber { get; private set; }
    public string? Notes { get; private set; }
    public StockRequestStatus Status { get; private set; }
    public Guid RequestedBy { get; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public Guid? ApprovedBy { get; private set; }
    public DateTimeOffset? ApprovedAtUtc { get; private set; }
    public Guid? PreparingStartedBy { get; private set; }
    public DateTimeOffset? PreparingStartedAtUtc { get; private set; }
    public Guid? DispatchedBy { get; private set; }
    public DateTimeOffset? DispatchedAtUtc { get; private set; }
    public Guid? LinkedInventoryTransferId { get; private set; }
    public Guid? RejectedBy { get; private set; }
    public DateTimeOffset? RejectedAtUtc { get; private set; }
    public string? RejectionReason { get; private set; }
    public Guid? CancelledBy { get; private set; }
    public DateTimeOffset? CancelledAtUtc { get; private set; }

    public IReadOnlyList<StockRequestLine> Lines => _lines;

    private StockRequest(
        StockRequestId id,
        PosOrganizationId organizationId,
        PosBranchId destinationLocationId,
        PosBranchId requestedSourceLocationId,
        string? requestNumber,
        string? notes,
        StockRequestStatus status,
        Guid requestedBy,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc,
        Guid? approvedBy,
        DateTimeOffset? approvedAtUtc,
        Guid? preparingStartedBy,
        DateTimeOffset? preparingStartedAtUtc,
        Guid? dispatchedBy,
        DateTimeOffset? dispatchedAtUtc,
        Guid? linkedInventoryTransferId,
        Guid? rejectedBy,
        DateTimeOffset? rejectedAtUtc,
        string? rejectionReason,
        Guid? cancelledBy,
        DateTimeOffset? cancelledAtUtc,
        List<StockRequestLine> lines)
    {
        Id = id;
        OrganizationId = organizationId;
        DestinationLocationId = destinationLocationId;
        RequestedSourceLocationId = requestedSourceLocationId;
        RequestNumber = requestNumber;
        Notes = notes;
        Status = NormalizeLegacyStatus(status);
        RequestedBy = requestedBy;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
        ApprovedBy = approvedBy;
        ApprovedAtUtc = approvedAtUtc;
        PreparingStartedBy = preparingStartedBy;
        PreparingStartedAtUtc = preparingStartedAtUtc;
        DispatchedBy = dispatchedBy;
        DispatchedAtUtc = dispatchedAtUtc;
        LinkedInventoryTransferId = linkedInventoryTransferId;
        RejectedBy = rejectedBy;
        RejectedAtUtc = rejectedAtUtc;
        RejectionReason = rejectionReason;
        CancelledBy = cancelledBy;
        CancelledAtUtc = cancelledAtUtc;
        _lines = lines;
    }

    public static StockRequest Create(
        PosOrganizationId organizationId,
        PosBranchId destinationLocationId,
        PosBranchId requestedSourceLocationId,
        IReadOnlyList<StockRequestLineDraft> lines,
        Guid requestedBy,
        DateTimeOffset utcNow,
        string? requestNumber = null,
        string? notes = null,
        StockRequestId? id = null)
    {
        SaleMoney.EnsureUtc(utcNow);
        EnsureActor(requestedBy);
        EnsureDistinctLocations(requestedSourceLocationId, destinationLocationId);
        EnsureLines(lines);

        var stockRequestId = id ?? StockRequestId.New();
        return new StockRequest(
            stockRequestId,
            organizationId,
            destinationLocationId,
            requestedSourceLocationId,
            requestNumber is null ? null : StockRequestNumbers.Normalize(requestNumber),
            NormalizeNotes(notes),
            StockRequestStatus.Pending,
            requestedBy,
            utcNow,
            utcNow,
            approvedBy: null,
            approvedAtUtc: null,
            preparingStartedBy: null,
            preparingStartedAtUtc: null,
            dispatchedBy: null,
            dispatchedAtUtc: null,
            linkedInventoryTransferId: null,
            rejectedBy: null,
            rejectedAtUtc: null,
            rejectionReason: null,
            cancelledBy: null,
            cancelledAtUtc: null,
            BuildLines(stockRequestId, lines));
    }

    public void Approve(
        Guid actorId,
        DateTimeOffset utcNow,
        IReadOnlyDictionary<Guid, decimal> lineApprovals)
    {
        SaleMoney.EnsureUtc(utcNow);
        EnsureActor(actorId);
        if (Status != StockRequestStatus.Pending)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidStockRequestStatusTransition,
                "Only a pending stock request can be approved.");
        }

        if (lineApprovals is null || lineApprovals.Count == 0)
        {
            throw new DomainException(
                DomainErrorCodes.StockRequestRequiresLines,
                "At least one line approval is required.");
        }

        var byProduct = _lines.ToDictionary(l => l.ProductId.Value);
        if (lineApprovals.Count != byProduct.Count
            || lineApprovals.Keys.Any(id => !byProduct.ContainsKey(id)))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidStockRequestLine,
                "Line approvals must cover every stock request product exactly once.");
        }

        foreach (var (productId, approvedQty) in lineApprovals)
        {
            byProduct[productId].SetApprovedQuantity(approvedQty);
        }

        ApprovedBy = actorId;
        ApprovedAtUtc = utcNow;
        Status = StockRequestStatus.Approved;
        UpdatedAtUtc = utcNow;
    }

    public void StartPreparing(Guid actorId, DateTimeOffset utcNow)
    {
        SaleMoney.EnsureUtc(utcNow);
        EnsureActor(actorId);

        if (Status == StockRequestStatus.Preparing)
        {
            return;
        }

        if (Status != StockRequestStatus.Approved)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidStockRequestStatusTransition,
                "Only an approved stock request can start preparing.");
        }

        PreparingStartedBy = actorId;
        PreparingStartedAtUtc = utcNow;
        Status = StockRequestStatus.Preparing;
        UpdatedAtUtc = utcNow;
    }

    /// <summary>Legacy alias: maps to <see cref="StartPreparing"/> when pending/approved workflow is not used.</summary>
    public void MarkInProgress(DateTimeOffset utcNow)
    {
        SaleMoney.EnsureUtc(utcNow);
        EnsureNotTerminal();
        if (Status is StockRequestStatus.Pending or StockRequestStatus.Approved)
        {
            Status = StockRequestStatus.Preparing;
            UpdatedAtUtc = utcNow;
        }
    }

    public void MarkDispatched(Guid actorId, DateTimeOffset utcNow, Guid transferId)
    {
        SaleMoney.EnsureUtc(utcNow);
        EnsureActor(actorId);
        if (transferId == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidStockRequestStatusTransition,
                "A linked inventory transfer id is required to mark dispatched.");
        }

        if (Status == StockRequestStatus.InTransit
            && LinkedInventoryTransferId == transferId)
        {
            return;
        }

        if (Status is not (StockRequestStatus.Approved or StockRequestStatus.Preparing))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidStockRequestStatusTransition,
                "Only an approved or preparing stock request can be dispatched.");
        }

        if (LinkedInventoryTransferId is Guid existing && existing != transferId)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidStockRequestStatusTransition,
                "Stock request is already linked to a different inventory transfer.");
        }

        DispatchedBy = actorId;
        DispatchedAtUtc = utcNow;
        LinkedInventoryTransferId = transferId;
        Status = StockRequestStatus.InTransit;
        UpdatedAtUtc = utcNow;
    }

    public void Reject(Guid actorId, DateTimeOffset utcNow, string reason)
    {
        SaleMoney.EnsureUtc(utcNow);
        EnsureActor(actorId);
        if (Status is not (
            StockRequestStatus.Pending
            or StockRequestStatus.Approved
            or StockRequestStatus.Preparing))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidStockRequestStatusTransition,
                "Only pending, approved, or preparing stock requests can be rejected.");
        }

        RejectedBy = actorId;
        RejectedAtUtc = utcNow;
        RejectionReason = NormalizeRejectionReason(reason);
        Status = StockRequestStatus.Rejected;
        UpdatedAtUtc = utcNow;
    }

    public void Cancel(Guid actorId, DateTimeOffset utcNow)
    {
        SaleMoney.EnsureUtc(utcNow);
        EnsureActor(actorId);
        if (Status is not (
            StockRequestStatus.Pending
            or StockRequestStatus.Approved
            or StockRequestStatus.Preparing))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidStockRequestStatusTransition,
                "Only pending, approved, or preparing stock requests can be cancelled.");
        }

        CancelledBy = actorId;
        CancelledAtUtc = utcNow;
        Status = StockRequestStatus.Cancelled;
        UpdatedAtUtc = utcNow;
    }

    public void RecalculateStatusFromReceivedQuantities(
        IReadOnlyDictionary<Guid, decimal> receivedByProduct,
        DateTimeOffset utcNow)
    {
        SaleMoney.EnsureUtc(utcNow);
        if (Status is StockRequestStatus.Rejected or StockRequestStatus.Cancelled)
        {
            return;
        }

        var anyReceived = false;
        var allFulfilled = true;
        foreach (var line in _lines)
        {
            var received = receivedByProduct.GetValueOrDefault(line.ProductId.Value);
            var target = line.FulfillmentTargetQuantity;
            if (received > 0m)
            {
                anyReceived = true;
            }

            if (received < target)
            {
                allFulfilled = false;
            }
        }

        if (allFulfilled && anyReceived)
        {
            Status = StockRequestStatus.Fulfilled;
        }
        else if (anyReceived)
        {
            Status = StockRequestStatus.PartiallyFulfilled;
        }
        else if (Status == StockRequestStatus.InTransit)
        {
            // Keep InTransit when nothing has been received yet.
        }
        else if (Status == StockRequestStatus.Pending)
        {
            Status = StockRequestStatus.Pending;
        }

        UpdatedAtUtc = utcNow;
    }

    public static StockRequest Rehydrate(
        StockRequestId id,
        PosOrganizationId organizationId,
        PosBranchId destinationLocationId,
        PosBranchId requestedSourceLocationId,
        string? requestNumber,
        string? notes,
        StockRequestStatus status,
        Guid requestedBy,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc,
        Guid? rejectedBy,
        DateTimeOffset? rejectedAtUtc,
        string? rejectionReason,
        Guid? cancelledBy,
        DateTimeOffset? cancelledAtUtc,
        IReadOnlyList<StockRequestLine> lines,
        Guid? approvedBy = null,
        DateTimeOffset? approvedAtUtc = null,
        Guid? preparingStartedBy = null,
        DateTimeOffset? preparingStartedAtUtc = null,
        Guid? dispatchedBy = null,
        DateTimeOffset? dispatchedAtUtc = null,
        Guid? linkedInventoryTransferId = null) =>
        new(
            id,
            organizationId,
            destinationLocationId,
            requestedSourceLocationId,
            requestNumber,
            notes,
            status,
            requestedBy,
            createdAtUtc,
            updatedAtUtc,
            approvedBy,
            approvedAtUtc,
            preparingStartedBy,
            preparingStartedAtUtc,
            dispatchedBy,
            dispatchedAtUtc,
            linkedInventoryTransferId,
            rejectedBy,
            rejectedAtUtc,
            rejectionReason,
            cancelledBy,
            cancelledAtUtc,
            lines.ToList());

    private void EnsureNotTerminal()
    {
        if (Status is StockRequestStatus.Rejected or StockRequestStatus.Cancelled)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidStockRequestStatusTransition,
                "Stock request is already closed.");
        }
    }

    private static StockRequestStatus NormalizeLegacyStatus(StockRequestStatus status) =>
        status == StockRequestStatus.InProgress ? StockRequestStatus.Preparing : status;

    private static void EnsureActor(Guid actorId)
    {
        if (actorId == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidSaleActor,
                "A non-empty actor identifier is required.");
        }
    }

    private static void EnsureDistinctLocations(PosBranchId source, PosBranchId destination)
    {
        if (source == destination)
        {
            throw new DomainException(
                DomainErrorCodes.SupplyRouteSameLocation,
                "Requested source and destination locations must be different.");
        }
    }

    private static void EnsureLines(IReadOnlyList<StockRequestLineDraft> lines)
    {
        if (lines is null || lines.Count == 0)
        {
            throw new DomainException(
                DomainErrorCodes.StockRequestRequiresLines,
                "A stock request must contain at least one line.");
        }

        if (lines.Count > MaxLineCount)
        {
            throw new DomainException(
                DomainErrorCodes.StockRequestRequiresLines,
                $"A stock request may contain at most {MaxLineCount} lines.");
        }

        var keys = lines.Select(l => l.ProductId.Value).ToList();
        if (keys.Count != keys.Distinct().Count())
        {
            throw new DomainException(
                DomainErrorCodes.StockRequestDuplicateProduct,
                "A stock request cannot contain duplicate products.");
        }
    }

    private static List<StockRequestLine> BuildLines(StockRequestId stockRequestId, IReadOnlyList<StockRequestLineDraft> lines)
    {
        var built = new List<StockRequestLine>(lines.Count);
        for (var i = 0; i < lines.Count; i++)
        {
            built.Add(StockRequestLine.Create(stockRequestId, i + 1, lines[i]));
        }

        return built;
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
                DomainErrorCodes.InvalidStockRequestNotes,
                $"Stock request notes must be at most {NotesMaxLength} characters.");
        }

        return trimmed;
    }

    private static string NormalizeRejectionReason(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidStockRequestRejectionReason,
                "Rejection reason is required.");
        }

        var trimmed = reason.Trim();
        if (trimmed.Length > RejectionReasonMaxLength)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidStockRequestRejectionReason,
                $"Rejection reason must be at most {RejectionReasonMaxLength} characters.");
        }

        return trimmed;
    }
}
