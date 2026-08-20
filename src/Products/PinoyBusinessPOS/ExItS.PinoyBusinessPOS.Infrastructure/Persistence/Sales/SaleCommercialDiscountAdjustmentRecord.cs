namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Sales;

internal sealed class SaleCommercialDiscountAdjustmentRecord
{
    public Guid Id { get; set; }
    public Guid SaleId { get; set; }
    public Guid OrganizationId { get; set; }
    public string Scope { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public string Source { get; set; } = "Manual";
    public decimal RequestedValue { get; set; }
    public decimal CalculatedAmount { get; set; }
    public string Reason { get; set; } = string.Empty;
    public Guid? SaleLineId { get; set; }
    public Guid AppliedBy { get; set; }
    public DateTimeOffset RecordedAtUtc { get; set; }
}
