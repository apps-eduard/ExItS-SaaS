using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.Domain.Inventory;

/// <summary>
/// Durable stock-use / internal consumption document. Stock is decreased atomically on create
/// for tracked products. Correction is void-only (compensating restoration movements).
/// </summary>
public sealed class StockUse
{
    public const int ReferenceNumberMaxLength = 128;
    public const int NotesMaxLength = 512;
    public const int IdempotencyKeyMaxLength = 128;

    private readonly List<StockUseLine> _lines;

    public StockUseId Id { get; }
    public PosOrganizationId OrganizationId { get; }
    public PosBranchId? BranchId { get; }
    public string StockUseNumber { get; }
    public string? ReferenceNumber { get; }
    public DateTimeOffset OccurredAtUtc { get; }
    public StockUseReason Reason { get; }
    public string? Notes { get; }
    public StockUseStatus Status { get; private set; }
    public Guid CreatedByUserId { get; }
    public DateTimeOffset CreatedAtUtc { get; }
    public Guid? VoidedByUserId { get; private set; }
    public DateTimeOffset? VoidedAtUtc { get; private set; }
    public string? IdempotencyKey { get; }

    public IReadOnlyList<StockUseLine> Lines => _lines;

    private StockUse(
        StockUseId id,
        PosOrganizationId organizationId,
        PosBranchId? branchId,
        string stockUseNumber,
        string? referenceNumber,
        DateTimeOffset occurredAtUtc,
        StockUseReason reason,
        string? notes,
        StockUseStatus status,
        Guid createdByUserId,
        DateTimeOffset createdAtUtc,
        Guid? voidedByUserId,
        DateTimeOffset? voidedAtUtc,
        string? idempotencyKey,
        List<StockUseLine> lines)
    {
        Id = id;
        OrganizationId = organizationId;
        BranchId = branchId;
        StockUseNumber = stockUseNumber;
        ReferenceNumber = referenceNumber;
        OccurredAtUtc = occurredAtUtc;
        Reason = reason;
        Notes = notes;
        Status = status;
        CreatedByUserId = createdByUserId;
        CreatedAtUtc = createdAtUtc;
        VoidedByUserId = voidedByUserId;
        VoidedAtUtc = voidedAtUtc;
        IdempotencyKey = idempotencyKey;
        _lines = lines;
    }

    public static StockUse Create(
        PosOrganizationId organizationId,
        string stockUseNumber,
        StockUseReason reason,
        IReadOnlyList<StockUseLineDraft> lines,
        Guid createdByUserId,
        DateTimeOffset utcNow,
        DateTimeOffset? occurredAtUtc = null,
        PosBranchId? branchId = null,
        string? referenceNumber = null,
        string? notes = null,
        string? idempotencyKey = null,
        StockUseId? id = null)
    {
        SaleMoney.EnsureUtc(utcNow);
        SaleMoney.EnsureActor(createdByUserId);

        var occurred = occurredAtUtc ?? utcNow;
        SaleMoney.EnsureUtc(occurred);

        if (lines is null || lines.Count == 0)
        {
            throw new DomainException(
                DomainErrorCodes.StockUseRequiresLines,
                "At least one stock use line is required.");
        }

        var stockUseId = id ?? StockUseId.New();
        var built = new List<StockUseLine>(lines.Count);
        var lineNumber = 1;
        foreach (var draft in lines)
        {
            built.Add(StockUseLine.Create(stockUseId, organizationId, lineNumber++, draft));
        }

        return new StockUse(
            stockUseId,
            organizationId,
            branchId,
            StockUseNumbers.Normalize(stockUseNumber),
            NormalizeOptional(referenceNumber, ReferenceNumberMaxLength, DomainErrorCodes.InvalidStockUseReference, "Reference number"),
            occurred,
            reason,
            NormalizeOptional(notes, NotesMaxLength, DomainErrorCodes.InvalidStockUseNotes, "Notes"),
            StockUseStatus.Posted,
            createdByUserId,
            utcNow,
            voidedByUserId: null,
            voidedAtUtc: null,
            NormalizeIdempotencyKey(idempotencyKey),
            built);
    }

    /// <summary>
    /// Marks the document voided. Inventory restoration is applied by the use case
    /// (compensating <see cref="StockMovementType.StockUseVoidRestoration"/> movements).
    /// </summary>
    public void Void(DateTimeOffset utcNow, Guid actorId)
    {
        SaleMoney.EnsureUtc(utcNow);
        SaleMoney.EnsureActor(actorId);

        if (Status == StockUseStatus.Voided)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidStockUseStatusTransition,
                "Stock use is already voided.");
        }

        if (Status != StockUseStatus.Posted)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidStockUseStatusTransition,
                "Only a posted stock use can be voided.");
        }

        Status = StockUseStatus.Voided;
        VoidedAtUtc = utcNow;
        VoidedByUserId = actorId;
    }

    public static StockUse Rehydrate(
        StockUseId id,
        PosOrganizationId organizationId,
        PosBranchId? branchId,
        string stockUseNumber,
        string? referenceNumber,
        DateTimeOffset occurredAtUtc,
        StockUseReason reason,
        string? notes,
        StockUseStatus status,
        Guid createdByUserId,
        DateTimeOffset createdAtUtc,
        Guid? voidedByUserId,
        DateTimeOffset? voidedAtUtc,
        string? idempotencyKey,
        IReadOnlyList<StockUseLine> lines) =>
        new(
            id,
            organizationId,
            branchId,
            stockUseNumber,
            referenceNumber,
            occurredAtUtc,
            reason,
            notes,
            status,
            createdByUserId,
            createdAtUtc,
            voidedByUserId,
            voidedAtUtc,
            idempotencyKey,
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

    private static string? NormalizeIdempotencyKey(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return null;
        }

        var trimmed = key.Trim();
        if (trimmed.Length > IdempotencyKeyMaxLength)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidStockUseIdempotencyKey,
                $"Idempotency key must be at most {IdempotencyKeyMaxLength} characters.");
        }

        return trimmed;
    }
}
