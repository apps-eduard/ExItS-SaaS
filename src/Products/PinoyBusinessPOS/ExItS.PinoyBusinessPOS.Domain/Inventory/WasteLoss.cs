using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.Domain.Inventory;

/// <summary>
/// Durable waste/loss document. Stock is decreased atomically on create for tracked products.
/// Correction is void-only (compensating restoration movements). Does not route through StockUse,
/// Production, or ManualDecrease. Cost snapshots use acquisition cost only (never SellingPrice).
/// </summary>
public sealed class WasteLoss
{
    public const int ReferenceNumberMaxLength = 128;
    public const int NotesMaxLength = 512;
    public const int IdempotencyKeyMaxLength = 128;

    private readonly List<WasteLossLine> _lines;

    public WasteLossId Id { get; }
    public PosOrganizationId OrganizationId { get; }
    public PosBranchId? BranchId { get; }
    public string WasteLossNumber { get; }
    public string? ReferenceNumber { get; }
    public DateTimeOffset OccurredAtUtc { get; }
    public WasteLossReason Reason { get; }
    public string? Notes { get; }
    public WasteLossStatus Status { get; private set; }
    /// <summary>Reuses <see cref="ProductionCostStatus"/> Complete/Partial/Unavailable semantics.</summary>
    public ProductionCostStatus CostStatus { get; }
    public decimal? TotalCostSnapshot { get; }
    public Guid CreatedByUserId { get; }
    public DateTimeOffset CreatedAtUtc { get; }
    public Guid? VoidedByUserId { get; private set; }
    public DateTimeOffset? VoidedAtUtc { get; private set; }
    public string? IdempotencyKey { get; }

    public IReadOnlyList<WasteLossLine> Lines => _lines;

    private WasteLoss(
        WasteLossId id,
        PosOrganizationId organizationId,
        PosBranchId? branchId,
        string wasteLossNumber,
        string? referenceNumber,
        DateTimeOffset occurredAtUtc,
        WasteLossReason reason,
        string? notes,
        WasteLossStatus status,
        ProductionCostStatus costStatus,
        decimal? totalCostSnapshot,
        Guid createdByUserId,
        DateTimeOffset createdAtUtc,
        Guid? voidedByUserId,
        DateTimeOffset? voidedAtUtc,
        string? idempotencyKey,
        List<WasteLossLine> lines)
    {
        Id = id;
        OrganizationId = organizationId;
        BranchId = branchId;
        WasteLossNumber = wasteLossNumber;
        ReferenceNumber = referenceNumber;
        OccurredAtUtc = occurredAtUtc;
        Reason = reason;
        Notes = notes;
        Status = status;
        CostStatus = costStatus;
        TotalCostSnapshot = totalCostSnapshot;
        CreatedByUserId = createdByUserId;
        CreatedAtUtc = createdAtUtc;
        VoidedByUserId = voidedByUserId;
        VoidedAtUtc = voidedAtUtc;
        IdempotencyKey = idempotencyKey;
        _lines = lines;
    }

    public static WasteLoss Create(
        PosOrganizationId organizationId,
        string wasteLossNumber,
        WasteLossReason reason,
        IReadOnlyList<WasteLossLineDraft> lines,
        Guid createdByUserId,
        DateTimeOffset utcNow,
        DateTimeOffset? occurredAtUtc = null,
        PosBranchId? branchId = null,
        string? referenceNumber = null,
        string? notes = null,
        string? idempotencyKey = null,
        WasteLossId? id = null)
    {
        SaleMoney.EnsureUtc(utcNow);
        SaleMoney.EnsureActor(createdByUserId);

        var occurred = occurredAtUtc ?? utcNow;
        SaleMoney.EnsureUtc(occurred);

        if (lines is null || lines.Count == 0)
        {
            throw new DomainException(
                DomainErrorCodes.WasteLossRequiresLines,
                "At least one waste/loss line is required.");
        }

        var normalizedNotes = NormalizeOptional(notes, NotesMaxLength, DomainErrorCodes.InvalidWasteLossNotes, "Notes");
        if (reason == WasteLossReason.Other && normalizedNotes is null)
        {
            throw new DomainException(
                DomainErrorCodes.WasteLossOtherRequiresNotes,
                "Notes are required when the waste/loss reason is Other.");
        }

        var wasteLossId = id ?? WasteLossId.New();
        var built = new List<WasteLossLine>(lines.Count);
        var lineNumber = 1;
        foreach (var draft in lines)
        {
            built.Add(WasteLossLine.Create(wasteLossId, organizationId, lineNumber++, draft));
        }

        var costStatus = ProductionCostStatuses.FromMaterialCosts(
            built.Select(l => l.UnitCostSnapshot).ToList());
        decimal? totalCost = costStatus == ProductionCostStatus.Unavailable
            ? null
            : SaleMoney.RoundMoney(built.Where(l => l.LineCostSnapshot is not null).Sum(l => l.LineCostSnapshot!.Value));

        return new WasteLoss(
            wasteLossId,
            organizationId,
            branchId,
            WasteLossNumbers.Normalize(wasteLossNumber),
            NormalizeOptional(referenceNumber, ReferenceNumberMaxLength, DomainErrorCodes.InvalidWasteLossReference, "Reference number"),
            occurred,
            reason,
            normalizedNotes,
            WasteLossStatus.Posted,
            costStatus,
            totalCost,
            createdByUserId,
            utcNow,
            voidedByUserId: null,
            voidedAtUtc: null,
            NormalizeIdempotencyKey(idempotencyKey),
            built);
    }

    /// <summary>
    /// Marks the document voided. Inventory restoration is applied by the use case
    /// (compensating <see cref="StockMovementType.WasteLossVoidRestoration"/> movements).
    /// </summary>
    public void Void(DateTimeOffset utcNow, Guid actorId)
    {
        SaleMoney.EnsureUtc(utcNow);
        SaleMoney.EnsureActor(actorId);

        if (Status == WasteLossStatus.Voided)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidWasteLossStatusTransition,
                "Waste/loss is already voided.");
        }

        if (Status != WasteLossStatus.Posted)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidWasteLossStatusTransition,
                "Only a posted waste/loss can be voided.");
        }

        Status = WasteLossStatus.Voided;
        VoidedAtUtc = utcNow;
        VoidedByUserId = actorId;
    }

    public static WasteLoss Rehydrate(
        WasteLossId id,
        PosOrganizationId organizationId,
        PosBranchId? branchId,
        string wasteLossNumber,
        string? referenceNumber,
        DateTimeOffset occurredAtUtc,
        WasteLossReason reason,
        string? notes,
        WasteLossStatus status,
        ProductionCostStatus costStatus,
        decimal? totalCostSnapshot,
        Guid createdByUserId,
        DateTimeOffset createdAtUtc,
        Guid? voidedByUserId,
        DateTimeOffset? voidedAtUtc,
        string? idempotencyKey,
        IReadOnlyList<WasteLossLine> lines) =>
        new(
            id,
            organizationId,
            branchId,
            wasteLossNumber,
            referenceNumber,
            occurredAtUtc,
            reason,
            notes,
            status,
            costStatus,
            totalCostSnapshot,
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
                DomainErrorCodes.InvalidWasteLossIdempotencyKey,
                $"Idempotency key must be at most {IdempotencyKeyMaxLength} characters.");
        }

        return trimmed;
    }
}
