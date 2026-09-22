namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Returns;

internal sealed class ReturnBatchNumberSequenceRecord
{
    public Guid OrganizationId { get; set; }
    public DateOnly BusinessDate { get; set; }
    public long LastValue { get; set; }
}
