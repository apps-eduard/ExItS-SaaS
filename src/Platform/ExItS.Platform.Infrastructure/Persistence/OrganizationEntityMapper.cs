using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Infrastructure.Persistence.Organizations;

namespace ExItS.Platform.Infrastructure.Persistence;

internal static class OrganizationEntityMapper
{
    public static PlatformOrganization ToDomain(PlatformOrganizationRecord record) =>
        PlatformOrganization.Rehydrate(
            PlatformOrganizationId.From(record.Id),
            record.DisplayName,
            record.Slug,
            Enum.Parse<OrganizationStatus>(record.Status),
            record.CreatedAtUtc,
            record.UpdatedAtUtc);

    public static PlatformOrganizationRecord ToRecord(PlatformOrganization organization) =>
        new()
        {
            Id = organization.Id.Value,
            DisplayName = organization.DisplayName,
            Slug = organization.Slug,
            Status = organization.Status.ToString(),
            CreatedAtUtc = organization.CreatedAtUtc,
            UpdatedAtUtc = organization.UpdatedAtUtc
        };

    public static void ApplyToRecord(PlatformOrganization organization, PlatformOrganizationRecord record)
    {
        record.DisplayName = organization.DisplayName;
        record.Slug = organization.Slug;
        record.Status = organization.Status.ToString();
        record.UpdatedAtUtc = organization.UpdatedAtUtc;
    }
}
