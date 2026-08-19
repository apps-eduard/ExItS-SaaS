namespace ExItS.Platform.Infrastructure.Persistence.Organizations;

internal sealed class OrganizationMembershipBranchAssignmentRecord
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid MembershipId { get; set; }
    public Guid BranchId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public string? ActorReference { get; set; }
}
