using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.Domain.Inventory;

/// <summary>
/// Intra-organization branch-to-branch inventory transfer. Draft has no stock effect.
/// Dispatch freezes sent quantities and leaves destination sellable stock unchanged until receive.
/// </summary>
public sealed class InventoryTransfer
{
    public const int NotesMaxLength = 512;
    public const int MaxLineCount = 200;

    private readonly List<InventoryTransferLine> _lines;

    public InventoryTransferId Id { get; }
    public PosOrganizationId OrganizationId { get; }
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

    public IReadOnlyList<InventoryTransferLine> Lines => _lines;

    public decimal TotalSentQty => _lines.Sum(l => l.SentQty);

    public decimal TotalReceivedQty => _lines.Sum(l => l.ReceivedQty);

    public decimal TotalDifferenceQty => TotalSentQty - TotalReceivedQty;

    private InventoryTransfer(
        InventoryTransferId id,
        PosOrganizationId organizationId,
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
        List<InventoryTransferLine> lines)
    {
        Id = id;
        OrganizationId = organizationId;
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
        _lines = lines;
    }

    public static InventoryTransfer CreateDraft(
        PosOrganizationId organizationId,
        PosBranchId sourceBranchId,
        PosBranchId destinationBranchId,
        IReadOnlyList<InventoryTransferLineDraft> lines,
        Guid createdBy,
        DateTimeOffset utcNow,
        string? notes = null,
        InventoryTransferId? id = null)
    {
        SaleMoney.EnsureUtc(utcNow);
        EnsureActor(createdBy);
        EnsureDistinctBranches(sourceBranchId, destinationBranchId);
        EnsureLines(lines);

        var transferId = id ?? InventoryTransferId.New();
        return new InventoryTransfer(
            transferId,
            organizationId,
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
            BuildDraftLines(transferId, organizationId, lines));
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

    public void Receive(
        IReadOnlyList<InventoryTransferReceiveLineDraft> receiveLines,
        Guid actorId,
        DateTimeOffset utcNow)
    {
        SaleMoney.EnsureUtc(utcNow);
        EnsureActor(actorId);
        if (Status != InventoryTransferStatus.InTransit)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidInventoryTransferStatusTransition,
                Status is InventoryTransferStatus.Received or InventoryTransferStatus.PartiallyReceived
                    ? "This transfer has already been received."
                    : "Only in-transit transfers can be received.");
        }

        if (receiveLines is null || receiveLines.Count == 0)
        {
            throw new DomainException(
                DomainErrorCodes.InventoryTransferReceiveRequiresLines,
                "At least one receive line is required.");
        }

        var lineByProduct = _lines.ToDictionary(l => l.ProductId.Value);
        var seen = new HashSet<Guid>();
        foreach (var receive in receiveLines)
        {
            if (!seen.Add(receive.ProductId.Value))
            {
                throw new DomainException(
                    DomainErrorCodes.InventoryTransferDuplicateProduct,
                    "Receive lines cannot repeat the same product.");
            }

            if (!lineByProduct.TryGetValue(receive.ProductId.Value, out var line))
            {
                throw new DomainException(
                    DomainErrorCodes.InvalidInventoryTransferLine,
                    "Receive line product is not on this transfer.");
            }

            line.ApplyReceipt(receive);
        }

        foreach (var line in _lines)
        {
            if (!seen.Contains(line.ProductId.Value))
            {
                throw new DomainException(
                    DomainErrorCodes.InventoryTransferReceiveRequiresLines,
                    "Every transfer line must be included in the receive.");
            }
        }

        Status = _lines.All(l => l.DifferenceQty == 0m)
            ? InventoryTransferStatus.Received
            : InventoryTransferStatus.PartiallyReceived;
        ReceivedAtUtc = utcNow;
        ReceivedBy = actorId;
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
        IReadOnlyList<InventoryTransferLine> lines) =>
        new(
            id,
            organizationId,
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
            lines.ToList());

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

        var productIds = lines.Select(l => l.ProductId.Value).ToList();
        if (productIds.Count != productIds.Distinct().Count())
        {
            throw new DomainException(
                DomainErrorCodes.InventoryTransferDuplicateProduct,
                "A transfer cannot contain the same product more than once.");
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
