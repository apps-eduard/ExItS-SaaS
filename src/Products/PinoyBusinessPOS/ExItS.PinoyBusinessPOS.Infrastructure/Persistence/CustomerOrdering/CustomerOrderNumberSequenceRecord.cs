namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.CustomerOrdering;

/// <summary>
/// One counter row per seller organization and business date. Bumped under an advisory lock inside the
/// same transaction that inserts the customer order.
/// </summary>
internal sealed class CustomerOrderNumberSequenceRecord
{
    public Guid OrganizationId { get; set; }
    public DateOnly BusinessDate { get; set; }
    public long LastValue { get; set; }
}
