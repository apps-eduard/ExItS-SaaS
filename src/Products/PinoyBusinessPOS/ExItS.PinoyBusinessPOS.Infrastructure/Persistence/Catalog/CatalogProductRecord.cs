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
    public decimal SellingPrice { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public uint Xmin { get; set; }
}
