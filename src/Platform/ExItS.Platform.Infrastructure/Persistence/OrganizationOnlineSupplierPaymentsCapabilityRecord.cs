namespace ExItS.Platform.Infrastructure.Persistence;

internal sealed class OrganizationOnlineSupplierPaymentsCapabilityRecord
{
    public Guid OrganizationId { get; set; }
    public string Status { get; set; } = "Disabled";
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public string? UpdatedByActorReference { get; set; }
    public string? Reason { get; set; }
}
