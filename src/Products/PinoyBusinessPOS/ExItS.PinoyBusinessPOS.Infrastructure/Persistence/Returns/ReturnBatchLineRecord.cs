namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Returns;

internal sealed class ReturnBatchLineRecord
{
    public Guid Id { get; set; }
    public Guid ReturnBatchId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid? SaleLineId { get; set; }
    public Guid? PurchaseOrderLineId { get; set; }
    public Guid ProductId { get; set; }
    public Guid? SupplierProductId { get; set; }
    public string ProductNameSnapshot { get; set; } = string.Empty;
    public string UomSnapshot { get; set; } = string.Empty;
    public decimal UnitPriceSnapshot { get; set; }
    public decimal LineTotalSnapshot { get; set; }
    public decimal AcceptedQuantity { get; set; }
    public decimal RefundAmountSnapshot { get; set; }
    public decimal? SellableQuantity { get; set; }
    public decimal? DamagedQuantity { get; set; }
    public string? InspectionNote { get; set; }
    public DateTimeOffset? ClassifiedAtUtc { get; set; }
    public Guid? ClassifiedBy { get; set; }
}
