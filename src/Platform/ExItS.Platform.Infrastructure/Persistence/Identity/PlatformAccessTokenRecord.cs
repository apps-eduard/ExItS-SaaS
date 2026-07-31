namespace ExItS.Platform.Infrastructure.Persistence.Identity;

internal sealed class PlatformAccessTokenRecord
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public string SecurityStampAtIssue { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset ExpiresAtUtc { get; set; }
    public DateTimeOffset? RevokedAtUtc { get; set; }
    public Guid? OrganizationId { get; set; }
    public string? ProductCode { get; set; }
    public uint Xmin { get; set; }
}
