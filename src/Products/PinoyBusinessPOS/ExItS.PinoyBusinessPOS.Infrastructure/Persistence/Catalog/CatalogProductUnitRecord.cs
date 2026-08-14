namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Catalog;

internal sealed class CatalogProductUnitRecord
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid ProductId { get; set; }
    public int Kind { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string ShortLabel { get; set; } = string.Empty;
    public decimal MultiplierToBase { get; set; }
    public decimal? SellingPrice { get; set; }
    public bool AllowsCustomQuantity { get; set; }
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public uint Xmin { get; set; }
}
