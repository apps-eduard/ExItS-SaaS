namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Catalog;

internal sealed class CatalogImportItemResultRecord
{
    public Guid Id { get; set; }
    public Guid CatalogImportJobId { get; set; }
    public Guid PlatformGlobalProductId { get; set; }
    public int SortOrder { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Sku { get; set; }
    public string? Barcode { get; set; }
    public string UnitOfMeasure { get; set; } = string.Empty;
    public string SellingMode { get; set; } = "PerItem";
    public decimal SuggestedPrice { get; set; }
    public Guid? SourceGlobalCategoryId { get; set; }
    public string? SourceCategoryName { get; set; }
    public string Status { get; set; } = string.Empty;
    public Guid? LocalProductId { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTimeOffset? ProcessedAtUtc { get; set; }
    public CatalogImportJobRecord? Job { get; set; }
}
