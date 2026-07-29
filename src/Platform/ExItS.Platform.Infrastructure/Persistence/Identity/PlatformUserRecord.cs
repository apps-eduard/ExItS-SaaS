namespace ExItS.Platform.Infrastructure.Persistence.Identity;

internal sealed class PlatformUserRecord
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string NormalizedUsername { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string NormalizedEmail { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public DateTimeOffset? SuspendedAtUtc { get; set; }
    public string? SuspensionReason { get; set; }
    public uint Xmin { get; set; }
}
