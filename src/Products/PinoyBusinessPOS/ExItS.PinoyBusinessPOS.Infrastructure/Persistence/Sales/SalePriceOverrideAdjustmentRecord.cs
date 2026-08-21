namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Sales;

internal sealed class SalePriceOverrideAdjustmentRecord
{
    public Guid Id { get; set; }
    public Guid SaleId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid SaleLineId { get; set; }
    public decimal BaselineUnitPrice { get; set; }
    public decimal AppliedUnitPrice { get; set; }
    public string Reason { get; set; } = string.Empty;
    public Guid AppliedBy { get; set; }
    public DateTimeOffset RecordedAtUtc { get; set; }
}
