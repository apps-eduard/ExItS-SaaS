namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.CustomerOrdering;

/// <summary>
/// One counter row per seller organization. Bumped under an advisory lock inside the same
/// transaction that inserts the customer order.
/// </summary>
internal sealed class CustomerOrderNumberSequenceRecord
{
    public Guid OrganizationId { get; set; }
    public long LastValue { get; set; }
}
