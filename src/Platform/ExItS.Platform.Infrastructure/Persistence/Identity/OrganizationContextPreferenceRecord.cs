namespace ExItS.Platform.Infrastructure.Persistence.Identity;

internal sealed class OrganizationContextPreferenceRecord
{
    public Guid UserIdentityId { get; set; }
    public Guid? LastActiveOrganizationId { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public uint Xmin { get; set; }
}
