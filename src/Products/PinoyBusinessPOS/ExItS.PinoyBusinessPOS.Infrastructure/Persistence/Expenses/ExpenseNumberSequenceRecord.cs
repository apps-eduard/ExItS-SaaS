namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Expenses;

/// <summary>
/// One counter row per organization and business date. Bumped under an advisory lock inside the same
/// transaction that inserts the expense, which keeps concurrent creates from colliding.
/// </summary>
internal sealed class ExpenseNumberSequenceRecord
{
    public Guid OrganizationId { get; set; }
    public DateOnly BusinessDate { get; set; }
    public long LastValue { get; set; }
}
