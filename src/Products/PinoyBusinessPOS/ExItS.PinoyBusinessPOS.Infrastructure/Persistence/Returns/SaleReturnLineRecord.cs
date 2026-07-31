namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Returns;

internal sealed class SaleReturnLineRecord
{
    public Guid Id { get; set; }
    public Guid SaleReturnId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid SaleLineId { get; set; }
    public Guid ProductId { get; set; }
    public string ProductNameSnapshot { get; set; } = string.Empty;
    public string UomSnapshot { get; set; } = string.Empty;
    public decimal QuantityReturned { get; set; }
    public decimal UnitPriceSnapshot { get; set; }
    public decimal RefundAmount { get; set; }
    public string RestockDisposition { get; set; } = string.Empty;
    public string? LineReason { get; set; }
    public Guid? InventoryMovementId { get; set; }
}
