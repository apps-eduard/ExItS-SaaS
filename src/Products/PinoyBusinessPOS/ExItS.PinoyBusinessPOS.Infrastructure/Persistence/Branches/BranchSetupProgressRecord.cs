namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Branches;

internal sealed class BranchSetupProgressRecord
{
    public Guid OrganizationId { get; set; }
    public Guid BranchId { get; set; }
    public string? LastVisitedStep { get; set; }
    public DateTimeOffset? StartedAtUtc { get; set; }
    public DateTimeOffset? LastVisitedAtUtc { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
}
