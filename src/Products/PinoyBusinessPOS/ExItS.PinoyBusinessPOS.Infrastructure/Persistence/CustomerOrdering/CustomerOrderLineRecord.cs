namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.CustomerOrdering;

internal sealed class CustomerOrderLineRecord
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid SellerOrganizationId { get; set; }
    public Guid ProductId { get; set; }
    public int LineNumber { get; set; }
    public string NameSnapshot { get; set; } = string.Empty;
    public string? SkuSnapshot { get; set; }
    public string UnitSnapshot { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Discount { get; set; }
    public decimal LineTotal { get; set; }
}
