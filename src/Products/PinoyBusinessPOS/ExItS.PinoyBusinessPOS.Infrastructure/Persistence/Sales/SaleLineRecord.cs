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
    public decimal GrossLineTotal { get; set; }
    public decimal LineDiscountAmount { get; set; }
    public decimal SaleDiscountAllocatedAmount { get; set; }
    public Guid? SellingUnitId { get; set; }
    public string? SellingUnitNameSnapshot { get; set; }
    public decimal? EnteredQuantity { get; set; }
    public decimal? MultiplierToBaseSnapshot { get; set; }
    public decimal? UnitCostSnapshot { get; set; }
    public decimal? LineCostSnapshot { get; set; }
}
