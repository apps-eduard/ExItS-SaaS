using ExItS.Platform.Domain.Common;

namespace ExItS.Platform.Domain.GlobalCatalog;

/// <summary>
/// Platform-owned bulk CSV/XLSX import job for global products.
/// Soft lifecycle; item-level partial success; restart-safe via Pending items + heartbeat.
/// </summary>
public sealed class CatalogImportJob
{
    private readonly List<CatalogImportItem> _items = [];

    public CatalogImportJobId Id { get; }
    public string FileName { get; }
    public CatalogImportFileFormat FileFormat { get; }
    public string? ContentType { get; }
    public long FileSizeBytes { get; }
    public string FileSha256 { get; }
    public string? IdempotencyKey { get; }
    public string RequestedBy { get; }
    public CatalogImportJobStatus Status { get; private set; }
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
    public IReadOnlyList<CatalogImportItem> Items => _items;

    private CatalogImportJob(
        CatalogImportJobId id,
        string fileName,
        CatalogImportFileFormat fileFormat,
        string? contentType,
        long fileSizeBytes,
        string fileSha256,
        string? idempotencyKey,
        string requestedBy,
        CatalogImportJobStatus status,
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
        IEnumerable<CatalogImportItem> items)
    {
        Id = id;
        FileName = fileName;
        FileFormat = fileFormat;
        ContentType = contentType;
        FileSizeBytes = fileSizeBytes;
        FileSha256 = fileSha256;
        IdempotencyKey = idempotencyKey;
        RequestedBy = requestedBy;
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
        _items.AddRange(items.OrderBy(i => i.RowNumber).ThenBy(i => i.Id.Value));
    }

    public static CatalogImportJob CreateValidated(
        string fileName,
        CatalogImportFileFormat fileFormat,
        long fileSizeBytes,
        string fileSha256,
        string requestedBy,
        IReadOnlyList<CatalogImportItem> items,
        DateTimeOffset utcNow,
        string? contentType = null,
        string? idempotencyKey = null,
        CatalogImportJobId? id = null)
    {
        DomainTime.EnsureUtc(utcNow);
        CatalogImportRules.EnsureFileSize(fileSizeBytes);

        if (items.Count == 0)
        {
            throw new DomainException(
                DomainErrorCodes.CatalogImportFileInvalid,
                "Import file contains no data rows.");
        }

        if (items.Count > CatalogImportRules.MaxRows)
        {
            throw new DomainException(
                DomainErrorCodes.CatalogImportFileInvalid,
                $"Import exceeds the maximum of {CatalogImportRules.MaxRows} rows.");
        }

        if (string.IsNullOrWhiteSpace(requestedBy))
        {
            throw new DomainException(
                DomainErrorCodes.CatalogImportFileInvalid,
                "RequestedBy is required.");
        }

        if (string.IsNullOrWhiteSpace(fileSha256) || fileSha256.Length != CatalogImportRules.Sha256HexLength)
        {
            throw new DomainException(
                DomainErrorCodes.CatalogImportFileInvalid,
                "File SHA-256 hash is required.");
        }

        var pending = items.Count(i => i.Status == CatalogImportItemStatus.Pending);
        var skipped = items.Count(i => i.Status == CatalogImportItemStatus.Skipped);
        var failed = items.Count(i => i.Status == CatalogImportItemStatus.Failed);

        return new CatalogImportJob(
            id ?? CatalogImportJobId.New(),
            CatalogImportRules.NormalizeFileName(fileName),
            fileFormat,
            string.IsNullOrWhiteSpace(contentType)
                ? null
                : contentType.Trim()[..Math.Min(contentType.Trim().Length, CatalogImportRules.ContentTypeMaxLength)],
            fileSizeBytes,
            fileSha256.ToLowerInvariant(),
            string.IsNullOrWhiteSpace(idempotencyKey)
                ? null
                : CatalogImportRules.NormalizeIdempotencyKey(idempotencyKey),
            requestedBy.Trim(),
            CatalogImportJobStatus.Validated,
            items.Count,
            processedCount: skipped + failed,
            importedCount: 0,
            skippedCount: skipped,
            failedCount: failed,
            currentStage: "Validated",
            errorSummary: pending == 0
                ? "No rows are eligible for import after validation."
                : null,
            utcNow,
            utcNow,
            startedAtUtc: null,
            completedAtUtc: null,
            lastHeartbeatAtUtc: null,
            items);
    }

    public static CatalogImportJob Rehydrate(
        CatalogImportJobId id,
        string fileName,
        CatalogImportFileFormat fileFormat,
        string? contentType,
        long fileSizeBytes,
        string fileSha256,
        string? idempotencyKey,
        string requestedBy,
        CatalogImportJobStatus status,
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
        IEnumerable<CatalogImportItem> items) =>
        new(
            id,
            fileName,
            fileFormat,
            contentType,
            fileSizeBytes,
            fileSha256,
            idempotencyKey,
            requestedBy,
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

    public int PendingCount => _items.Count(i => i.Status == CatalogImportItemStatus.Pending);

    public void Confirm(DateTimeOffset utcNow)
    {
        DomainTime.EnsureUtc(utcNow);
        if (Status is not CatalogImportJobStatus.Validated)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCatalogImportStatusTransition,
                $"Cannot confirm import from status {Status}.");
        }

        if (PendingCount == 0)
        {
            throw new DomainException(
                DomainErrorCodes.CatalogImportNoConfirmableRows,
                "No valid rows are available to import.");
        }

        Status = CatalogImportJobStatus.Queued;
        CurrentStage = "Queued";
        UpdatedAtUtc = utcNow;
    }

    public void BeginProcessing(DateTimeOffset utcNow)
    {
        DomainTime.EnsureUtc(utcNow);
        if (Status is not (CatalogImportJobStatus.Queued or CatalogImportJobStatus.Processing))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCatalogImportStatusTransition,
                $"Cannot begin processing from status {Status}.");
        }

        Status = CatalogImportJobStatus.Processing;
        CurrentStage = "Processing";
        StartedAtUtc ??= utcNow;
        LastHeartbeatAtUtc = utcNow;
        UpdatedAtUtc = utcNow;
    }

    public void Heartbeat(DateTimeOffset utcNow)
    {
        DomainTime.EnsureUtc(utcNow);
        if (Status is not CatalogImportJobStatus.Processing)
        {
            return;
        }

        LastHeartbeatAtUtc = utcNow;
        UpdatedAtUtc = utcNow;
    }

    public void RecalculateProgress(DateTimeOffset utcNow)
    {
        DomainTime.EnsureUtc(utcNow);
        ImportedCount = _items.Count(i => i.Status == CatalogImportItemStatus.Imported);
        SkippedCount = _items.Count(i => i.Status == CatalogImportItemStatus.Skipped);
        FailedCount = _items.Count(i => i.Status == CatalogImportItemStatus.Failed);
        ProcessedCount = Math.Min(TotalCount, ImportedCount + SkippedCount + FailedCount);
        LastHeartbeatAtUtc = utcNow;
        UpdatedAtUtc = utcNow;
        CurrentStage = "Processing";
    }

    public void Complete(DateTimeOffset utcNow)
    {
        DomainTime.EnsureUtc(utcNow);
        RecalculateProgress(utcNow);

        if (PendingCount > 0)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCatalogImportStatusTransition,
                "Cannot complete import while pending items remain.");
        }

        Status = FailedCount > 0 || SkippedCount > 0
            ? CatalogImportJobStatus.CompletedWithWarnings
            : CatalogImportJobStatus.Completed;
        CurrentStage = Status.ToString();
        CompletedAtUtc = utcNow;
        ErrorSummary = Status == CatalogImportJobStatus.CompletedWithWarnings
            ? $"Imported {ImportedCount}, skipped {SkippedCount}, failed {FailedCount}."
            : null;
        UpdatedAtUtc = utcNow;
    }

    public void Fail(string summary, DateTimeOffset utcNow)
    {
        DomainTime.EnsureUtc(utcNow);
        Status = CatalogImportJobStatus.Failed;
        CurrentStage = "Failed";
        ErrorSummary = CatalogImportRules.NormalizeOptionalError(summary);
        CompletedAtUtc = utcNow;
        LastHeartbeatAtUtc = utcNow;
        UpdatedAtUtc = utcNow;
    }

    public CatalogImportItem? FindItem(CatalogImportItemId itemId) =>
        _items.FirstOrDefault(i => i.Id == itemId);
}
