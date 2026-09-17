namespace ExItS.Platform.Infrastructure.Persistence.Organizations;

internal sealed class OrganizationMembershipRecord
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid UserId { get; set; }
    public string Role { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string BranchAccessScope { get; set; } = nameof(Domain.Organizations.BranchAccessScope.Explicit);
    public string? Department { get; set; }
    public string? JobTitle { get; set; }
    public string? WorkPhone { get; set; }
    public string? WorkEmail { get; set; }
    public bool IsBusinessContact { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public DateTimeOffset? SuspendedAtUtc { get; set; }
    public DateTimeOffset? RemovedAtUtc { get; set; }
    public string? Reason { get; set; }
    public string? ActorReference { get; set; }
    public uint Xmin { get; set; }
}
