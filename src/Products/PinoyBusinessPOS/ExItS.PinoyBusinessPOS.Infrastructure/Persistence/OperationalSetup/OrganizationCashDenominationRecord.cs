namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.OperationalSetup;

internal sealed class OrganizationCashDenominationRecord
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public decimal Value { get; set; }
    public string? DisplayLabel { get; set; }
    public bool IsEnabled { get; set; } = true;
    public int SortOrder { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}
