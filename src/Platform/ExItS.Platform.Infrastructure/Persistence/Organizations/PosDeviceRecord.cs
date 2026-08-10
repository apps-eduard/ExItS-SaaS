namespace ExItS.Platform.Infrastructure.Persistence.Organizations;

internal sealed class PosDeviceRecord
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid BranchId { get; set; }
    public string InstallationDeviceId { get; set; } = string.Empty;
    public string FriendlyName { get; set; } = string.Empty;
    public string? Platform { get; set; }
    public string? Model { get; set; }
    public string? AppVersion { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset RegisteredAtUtc { get; set; }
    public DateTimeOffset LastSeenAtUtc { get; set; }
    public DateTimeOffset? RevokedAtUtc { get; set; }
    public Guid? RevokedByUserId { get; set; }
}
