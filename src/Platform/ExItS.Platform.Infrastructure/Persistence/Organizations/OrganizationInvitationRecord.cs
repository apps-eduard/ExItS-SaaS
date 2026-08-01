namespace ExItS.Platform.Infrastructure.Persistence.Organizations;

internal sealed class OrganizationInvitationRecord
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string NormalizedEmail { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string TokenHash { get; set; } = string.Empty;
    public Guid? InvitedByUserId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public DateTimeOffset ExpiresAtUtc { get; set; }
    public DateTimeOffset? AcceptedAtUtc { get; set; }
    public DateTimeOffset? RevokedAtUtc { get; set; }
    public Guid? AcceptedByUserId { get; set; }
    public uint Xmin { get; set; }
}
