namespace ExItS.Platform.Infrastructure.Persistence.Organizations;

internal sealed class OrganizationAreaRecord
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

internal sealed class OrganizationMembershipAreaAssignmentRecord
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid MembershipId { get; set; }
    public Guid AreaId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public string? ActorReference { get; set; }
}
