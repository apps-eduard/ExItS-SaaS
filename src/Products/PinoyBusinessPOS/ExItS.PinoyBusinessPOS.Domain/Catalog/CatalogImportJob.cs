using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.Domain.Catalog;

/// <summary>
/// POS-owned merchant catalog import job. Creates local product snapshots; stock remains 0 until
/// OpeningStock via existing inventory workflow. Soft lifecycle with heartbeat reclaim.
/// </summary>
public sealed class CatalogImportJob
{
    private readonly List<CatalogImportItemResult> _items = [];

    public CatalogImportJobId Id { get; }
    public PosOrganizationId OrganizationId { get; }
    public PosCatalogImportJobKind JobKind { get; }
    public Guid? PlatformTemplateId { get; }
    public int? BatchNumber { get; }
    public CatalogSource CatalogSource { get; }
    public string RequestedBy { get; }
    public string? IdempotencyKey { get; }
    public PosCatalogImportJobStatus Status { get; private set; }
    public int TotalCount { get; private set; }
    public int ProcessedCount { get; private set; }
    public int ImportedCount { get; private set; }
    public int SkippedCount { get; private set; }
    public int FailedCount { get; private set; }
    public string? CurrentStage { get; private set; }
    public string? ErrorSummary { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public DateTimeOffset? StartedAtUtc { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }
    public DateTimeOffset? LastHeartbeatAtUtc { get; private set; }
    public IReadOnlyList<CatalogImportItemResult> Items => _items;

    private CatalogImportJob(
        CatalogImportJobId id,
        PosOrganizationId organizationId,
        PosCatalogImportJobKind jobKind,
        Guid? platformTemplateId,
        int? batchNumber,
        CatalogSource catalogSource,
        string requestedBy,
        string? idempotencyKey,
        PosCatalogImportJobStatus status,
        int totalCount,
        int processedCount,
        int importedCount,
        int skippedCount,
        int failedCount,
        string? currentStage,
        string? errorSummary,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc,
        DateTimeOffset? startedAtUtc,
        DateTimeOffset? completedAtUtc,
        DateTimeOffset? lastHeartbeatAtUtc,
        IEnumerable<CatalogImportItemResult> items)
    {
        Id = id;
        OrganizationId = organizationId;
        JobKind = jobKind;
        PlatformTemplateId = platformTemplateId;
        BatchNumber = batchNumber;
        CatalogSource = catalogSource;
        RequestedBy = requestedBy;
        IdempotencyKey = idempotencyKey;
        Status = status;
        TotalCount = totalCount;
        ProcessedCount = processedCount;
        ImportedCount = importedCount;
        SkippedCount = skippedCount;
        FailedCount = failedCount;
        CurrentStage = currentStage;
        ErrorSummary = errorSummary;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
        StartedAtUtc = startedAtUtc;
        CompletedAtUtc = completedAtUtc;
        LastHeartbeatAtUtc = lastHeartbeatAtUtc;
        _items.AddRange(items.OrderBy(i => i.SortOrder).ThenBy(i => i.Id.Value));
    }

    public static CatalogImportJob CreateQueued(
        PosOrganizationId organizationId,
        PosCatalogImportJobKind jobKind,
        CatalogSource catalogSource,
        string requestedBy,
        IReadOnlyList<CatalogImportItemResult> items,
        DateTimeOffset utcNow,
        Guid? platformTemplateId = null,
        int? batchNumber = null,
        string? idempotencyKey = null,
        CatalogImportJobId? id = null)
    {
        CatalogGuards.EnsureUtc(utcNow);

        if (string.IsNullOrWhiteSpace(requestedBy))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCatalogImportJob,
                "RequestedBy is required.");
        }

        if (items.Count == 0)
        {
            throw new DomainException(
                DomainErrorCodes.CatalogImportEmpty,
                "Import contains no products.");
        }

        if (items.Count > CatalogImportRules.MaxItemsPerJob)
        {
            throw new DomainException(
                DomainErrorCodes.CatalogImportTooLarge,
                $"Import exceeds the maximum of {CatalogImportRules.MaxItemsPerJob} products.");
        }

        if (jobKind == PosCatalogImportJobKind.TemplateBatch)
        {
            if (platformTemplateId is null || platformTemplateId == Guid.Empty)
            {
                throw new DomainException(
                    DomainErrorCodes.InvalidCatalogImportJob,
                    "PlatformTemplateId is required for template batch imports.");
            }

            if (batchNumber is null || batchNumber < 1)
            {
                throw new DomainException(
                    DomainErrorCodes.InvalidCatalogImportJob,
                    "BatchNumber must be >= 1 for template batch imports.");
            }
        }

        var distinct = items.Select(i => i.PlatformGlobalProductId).Distinct().Count();
        if (distinct != items.Count)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCatalogImportJob,
                "Duplicate PlatformGlobalProductId values are not allowed in one import job.");
        }

        return new CatalogImportJob(
            id ?? CatalogImportJobId.New(),
            organizationId,
            jobKind,
            platformTemplateId == Guid.Empty ? null : platformTemplateId,
            batchNumber,
            catalogSource,
            requestedBy.Trim(),
            CatalogImportRules.NormalizeOptionalIdempotencyKey(idempotencyKey),
            PosCatalogImportJobStatus.Queued,
            items.Count,
            processedCount: 0,
            importedCount: 0,
            skippedCount: 0,
            failedCount: 0,
            currentStage: "Queued",
            errorSummary: null,
            utcNow,
            utcNow,
            startedAtUtc: null,
            completedAtUtc: null,
            lastHeartbeatAtUtc: null,
            items);
    }

    public static CatalogImportJob Rehydrate(
        CatalogImportJobId id,
        PosOrganizationId organizationId,
        PosCatalogImportJobKind jobKind,
        Guid? platformTemplateId,
        int? batchNumber,
        CatalogSource catalogSource,
        string requestedBy,
        string? idempotencyKey,
        PosCatalogImportJobStatus status,
        int totalCount,
        int processedCount,
        int importedCount,
        int skippedCount,
        int failedCount,
        string? currentStage,
        string? errorSummary,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc,
        DateTimeOffset? startedAtUtc,
        DateTimeOffset? completedAtUtc,
        DateTimeOffset? lastHeartbeatAtUtc,
        IEnumerable<CatalogImportItemResult> items) =>
        new(
            id,
            organizationId,
            jobKind,
            platformTemplateId,
            batchNumber,
            catalogSource,
            requestedBy,
            idempotencyKey,
            status,
            totalCount,
            processedCount,
            importedCount,
            skippedCount,
            failedCount,
            currentStage,
            errorSummary,
            createdAtUtc,
            updatedAtUtc,
            startedAtUtc,
            completedAtUtc,
            lastHeartbeatAtUtc,
            items);

    public int PendingCount => _items.Count(i => i.Status == PosCatalogImportItemStatus.Pending);

    public void BeginProcessing(DateTimeOffset utcNow)
    {
        CatalogGuards.EnsureUtc(utcNow);
        if (Status is not (PosCatalogImportJobStatus.Queued or PosCatalogImportJobStatus.Processing))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCatalogImportStatusTransition,
                $"Cannot begin processing from status {Status}.");
        }

        Status = PosCatalogImportJobStatus.Processing;
        CurrentStage = "Processing";
        StartedAtUtc ??= utcNow;
        LastHeartbeatAtUtc = utcNow;
        UpdatedAtUtc = utcNow;
    }

    public void RecalculateProgress(DateTimeOffset utcNow)
    {
        CatalogGuards.EnsureUtc(utcNow);
        ImportedCount = _items.Count(i => i.Status == PosCatalogImportItemStatus.Imported);
        SkippedCount = _items.Count(i => i.Status == PosCatalogImportItemStatus.Skipped);
        FailedCount = _items.Count(i => i.Status == PosCatalogImportItemStatus.Failed);
        ProcessedCount = Math.Min(TotalCount, ImportedCount + SkippedCount + FailedCount);
        LastHeartbeatAtUtc = utcNow;
        UpdatedAtUtc = utcNow;
        CurrentStage = "Processing";
    }

    public void Complete(DateTimeOffset utcNow)
    {
        CatalogGuards.EnsureUtc(utcNow);
        RecalculateProgress(utcNow);

        if (PendingCount > 0)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCatalogImportStatusTransition,
                "Cannot complete import while pending items remain.");
        }

        Status = FailedCount > 0 || SkippedCount > 0
            ? PosCatalogImportJobStatus.CompletedWithWarnings
            : PosCatalogImportJobStatus.Completed;
        CurrentStage = Status.ToString();
        CompletedAtUtc = utcNow;
        ErrorSummary = Status == PosCatalogImportJobStatus.CompletedWithWarnings
            ? $"Imported {ImportedCount}, skipped {SkippedCount}, failed {FailedCount}."
            : null;
        UpdatedAtUtc = utcNow;
    }

    public void Fail(string summary, DateTimeOffset utcNow)
    {
        CatalogGuards.EnsureUtc(utcNow);
        Status = PosCatalogImportJobStatus.Failed;
        CurrentStage = "Failed";
        ErrorSummary = CatalogImportRules.NormalizeOptionalError(summary);
        CompletedAtUtc = utcNow;
        LastHeartbeatAtUtc = utcNow;
        UpdatedAtUtc = utcNow;
    }

    public CatalogImportItemResult? FindItem(CatalogImportItemResultId itemId) =>
        _items.FirstOrDefault(i => i.Id == itemId);
}
