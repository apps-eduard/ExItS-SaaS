namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Returns;

internal sealed class SaleReturnNumberSequenceRecord
{
    public Guid OrganizationId { get; set; }
    public DateOnly BusinessDate { get; set; }
    public long LastValue { get; set; }
}
