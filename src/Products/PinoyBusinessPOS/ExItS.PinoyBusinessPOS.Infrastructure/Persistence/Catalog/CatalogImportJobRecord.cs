namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Catalog;

internal sealed class CatalogImportJobRecord
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string JobKind { get; set; } = string.Empty;
    public Guid? PlatformTemplateId { get; set; }
    public int? BatchNumber { get; set; }
    public string CatalogSource { get; set; } = string.Empty;
    public string RequestedBy { get; set; } = string.Empty;
    public string? IdempotencyKey { get; set; }
    public string Status { get; set; } = string.Empty;
    public int TotalCount { get; set; }
    public int ProcessedCount { get; set; }
    public int ImportedCount { get; set; }
    public int SkippedCount { get; set; }
    public int FailedCount { get; set; }
    public string? CurrentStage { get; set; }
    public string? ErrorSummary { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public DateTimeOffset? StartedAtUtc { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
    public DateTimeOffset? LastHeartbeatAtUtc { get; set; }
    public List<CatalogImportItemResultRecord> Items { get; set; } = [];
}
