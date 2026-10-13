namespace ExItS.Platform.Infrastructure.Persistence.Organizations;

internal sealed class OrganizationStaffIdSettingsRecord
{
    public Guid OrganizationId { get; set; }
    public string Prefix { get; set; } = OrganizationStaffIdSettingsDefaults.Prefix;
    public int NextNumber { get; set; } = 1;
    public int PadDigits { get; set; } = 4;
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

internal static class OrganizationStaffIdSettingsDefaults
{
    public const string Prefix = "STF-";
}
