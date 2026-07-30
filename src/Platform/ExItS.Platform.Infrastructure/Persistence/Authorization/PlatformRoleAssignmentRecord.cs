namespace ExItS.Platform.Infrastructure.Persistence.Authorization;

internal sealed class PlatformRoleAssignmentRecord
{
    public Guid Id { get; set; }
    public Guid PlatformUserId { get; set; }
    public string Role { get; set; } = string.Empty;
    public Guid? OrganizationId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string GrantedByActor { get; set; } = string.Empty;
    public DateTimeOffset GrantedAtUtc { get; set; }
    public string? Reason { get; set; }
    public string? RevokedByActor { get; set; }
    public DateTimeOffset? RevokedAtUtc { get; set; }
    public string? RevokeReason { get; set; }
    public uint Xmin { get; set; }
}
