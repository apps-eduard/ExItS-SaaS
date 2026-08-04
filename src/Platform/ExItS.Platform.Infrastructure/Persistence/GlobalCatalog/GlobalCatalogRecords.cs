namespace ExItS.Platform.Infrastructure.Persistence.GlobalCatalog;

internal sealed class GlobalCategoryRecord
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NormalizedName { get; set; } = string.Empty;
    public Guid? ParentId { get; set; }
    public string? IconReference { get; set; }
    public int SortOrder { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public uint Xmin { get; set; }

    public List<GlobalCategoryBusinessTypeRecord> BusinessTypes { get; set; } = [];
}

internal sealed class GlobalCategoryBusinessTypeRecord
{
    public Guid CategoryId { get; set; }
    public string BusinessType { get; set; } = string.Empty;
}

internal sealed class GlobalProductRecord
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Sku { get; set; }
    public string? Barcode { get; set; }
    public Guid? GlobalCategoryId { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal? SuggestedPrice { get; set; }
    public decimal? SuggestedCost { get; set; }
    public string? ImageReference { get; set; }
    public string Status { get; set; } = string.Empty;
    public string[] SearchTags { get; set; } = [];
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public uint Xmin { get; set; }

    public List<GlobalProductBusinessTypeRecord> BusinessTypes { get; set; } = [];
}

internal sealed class GlobalProductBusinessTypeRecord
{
    public Guid ProductId { get; set; }
    public string BusinessType { get; set; } = string.Empty;
}

internal sealed class CatalogTemplateRecord
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? IconReference { get; set; }
    public string PrimaryBusinessType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int DefaultBatchSize { get; set; }
    public string SelectionMode { get; set; } = string.Empty;
    public DateTimeOffset? PublishedAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public uint Xmin { get; set; }

    public List<CatalogTemplateProductRecord> Products { get; set; } = [];
}

internal sealed class CatalogTemplateProductRecord
{
    public Guid Id { get; set; }
    public Guid CatalogTemplateId { get; set; }
    public Guid GlobalProductId { get; set; }
    public int SortOrder { get; set; }
    public bool IsFeatured { get; set; }
    public bool IsFirstBatch { get; set; }
}

internal sealed class CatalogImportJobRecord
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FileFormat { get; set; } = string.Empty;
    public string? ContentType { get; set; }
    public long FileSizeBytes { get; set; }
    public string FileSha256 { get; set; } = string.Empty;
    public string? IdempotencyKey { get; set; }
    public string RequestedBy { get; set; } = string.Empty;
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
    public uint Xmin { get; set; }

    public List<CatalogImportItemRecord> Items { get; set; } = [];
}

internal sealed class CatalogImportItemRecord
{
    public Guid Id { get; set; }
    public Guid CatalogImportJobId { get; set; }
    public int RowNumber { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Sku { get; set; }
    public string? Barcode { get; set; }
    public Guid? GlobalCategoryId { get; set; }
    public string? CategoryName { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal? SuggestedPrice { get; set; }
    public decimal? SuggestedCost { get; set; }
    public string? ImageReference { get; set; }
    public string? SearchTagsRaw { get; set; }
    public string? BusinessTypesRaw { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public Guid? CreatedGlobalProductId { get; set; }
    public int AttemptCount { get; set; }
    public DateTimeOffset? ProcessedAtUtc { get; set; }
}
