namespace ExItS.Platform.Infrastructure.Persistence;

internal sealed class OrganizationComplianceProfileRecord
{
    public Guid OrganizationId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public string? UpdatedByActorReference { get; set; }
}
