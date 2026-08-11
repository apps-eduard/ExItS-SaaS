namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Sales;

internal sealed class SaleLineRecord
{
    public Guid Id { get; set; }
    public Guid SaleId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid ProductId { get; set; }
    public int LineNumber { get; set; }
    public string NameSnapshot { get; set; } = string.Empty;
    public string? SkuSnapshot { get; set; }
    public string? BarcodeSnapshot { get; set; }
    public string UnitOfMeasureSnapshot { get; set; } = string.Empty;
    public string SellingModeSnapshot { get; set; } = "PerItem";
    public decimal UnitPrice { get; set; }
    public decimal Quantity { get; set; }
    public decimal LineTotal { get; set; }
}
