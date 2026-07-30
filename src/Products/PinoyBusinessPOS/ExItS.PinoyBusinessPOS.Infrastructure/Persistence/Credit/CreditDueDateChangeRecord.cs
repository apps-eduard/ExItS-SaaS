namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Credit;

internal sealed class CreditDueDateChangeRecord
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid CreditEntryId { get; set; }
    public Guid CustomerId { get; set; }
    public DateOnly? PreviousDueDate { get; set; }
    public DateOnly? NewDueDate { get; set; }
    public string Reason { get; set; } = string.Empty;
    public Guid ChangedBy { get; set; }
    public DateTimeOffset ChangedAtUtc { get; set; }
}
