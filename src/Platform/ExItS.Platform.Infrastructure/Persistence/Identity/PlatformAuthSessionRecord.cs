namespace ExItS.Platform.Infrastructure.Persistence.Identity;

internal sealed class PlatformAuthSessionRecord
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public string SecurityStampAtIssue { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset ExpiresAtUtc { get; set; }
    public DateTimeOffset AbsoluteExpiresAtUtc { get; set; }
    public DateTimeOffset LastActivityAtUtc { get; set; }
    public DateTimeOffset? RevokedAtUtc { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgentHash { get; set; }
    public Guid? SelectedOrganizationId { get; set; }
    public uint Xmin { get; set; }
}
