namespace ExItS.Platform.Infrastructure.Persistence.Organizations;

internal sealed class OrganizationBusinessTypeActivationRecord
{
    public Guid OrganizationId { get; set; }
    public Guid BusinessTypeId { get; set; }
    public DateTimeOffset ActivatedAtUtc { get; set; }
}
