namespace ExItS.Platform.Infrastructure.Persistence.Organizations;

internal sealed class PlatformOrganizationRecord
{
    public Guid Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public uint Xmin { get; set; }
}
