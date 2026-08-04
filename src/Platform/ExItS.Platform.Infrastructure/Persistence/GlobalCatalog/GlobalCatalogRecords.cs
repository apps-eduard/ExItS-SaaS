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
