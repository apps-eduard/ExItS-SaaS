namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Sales;

/// <summary>
/// One counter row per organization and business date. Bumped under an advisory lock inside the same
/// transaction that inserts the sale, which is what keeps concurrent checkouts from colliding.
/// </summary>
internal sealed class SaleNumberSequenceRecord
{
    public Guid OrganizationId { get; set; }
    public DateOnly BusinessDate { get; set; }
    public long LastValue { get; set; }
}
