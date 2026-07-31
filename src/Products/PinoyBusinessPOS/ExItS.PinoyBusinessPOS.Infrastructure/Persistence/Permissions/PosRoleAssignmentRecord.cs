namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Permissions;

internal sealed class PosRoleAssignmentRecord
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid ActorId { get; set; }
    public string Role { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset AssignedAtUtc { get; set; }
    public Guid AssignedBy { get; set; }
    public DateTimeOffset? RevokedAtUtc { get; set; }
    public Guid? RevokedBy { get; set; }
    public string? RevocationReason { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public uint Xmin { get; set; }
}
