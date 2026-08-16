namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Catalog;

internal sealed class CatalogProductRecord
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Sku { get; set; }
    public string? NormalizedSku { get; set; }
    public string? Barcode { get; set; }
    public Guid? CategoryId { get; set; }
    public string UnitOfMeasure { get; set; } = string.Empty;
    public string SellingMode { get; set; } = "PerItem";
    public decimal SellingPrice { get; set; }
    public string Status { get; set; } = string.Empty;
    public Guid? PlatformGlobalProductId { get; set; }
    public Guid? PlatformTemplateId { get; set; }
    public string CatalogSource { get; set; } = "Manual";
    public DateTimeOffset? CatalogImportedAt { get; set; }
    public int? CatalogSnapshotVersion { get; set; }
    public Guid? SourceGlobalCategoryId { get; set; }
    public bool TracksExpiration { get; set; }
    public int? ExpirationWarningDays { get; set; }
    public bool CanBePurchased { get; set; } = true;
    public bool CanBeSold { get; set; } = true;
    public bool CanBeUsedAsIngredient { get; set; }
    public bool IsProduced { get; set; }
    public string? UsagePreset { get; set; } = "BuyAndSell";
    public bool CanExposeToConnectedBuyers { get; set; }
    public decimal? DefaultConnectedPoPrice { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public uint Xmin { get; set; }
}
