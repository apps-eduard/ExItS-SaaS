namespace ExItS.Platform.Infrastructure.Persistence.Identity;

internal sealed class PlatformDeviceRecoveryCredentialRecord
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string InstallationDeviceId { get; set; } = string.Empty;
    public string TokenHash { get; set; } = string.Empty;
    public string SecurityStampAtIssue { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset LastUsedAtUtc { get; set; }
    public DateTimeOffset IdleExpiresAtUtc { get; set; }
    public DateTimeOffset AbsoluteExpiresAtUtc { get; set; }
    public DateTimeOffset? RevokedAtUtc { get; set; }
    public int RotationVersion { get; set; }
    public uint Xmin { get; set; }
}
