using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.Application.Organizations;

public sealed record PlatformOrganizationDto(
    Guid Id,
    string DisplayName,
    string Slug,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed class OrganizationQueryService
{
    private readonly IPlatformOrganizationRepository _organizations;

    public OrganizationQueryService(IPlatformOrganizationRepository organizations)
    {
        _organizations = organizations;
    }

    public async Task<PlatformOrganizationDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var organization = await _organizations
            .GetByIdAsync(PlatformOrganizationId.From(id), cancellationToken)
            .ConfigureAwait(false);

        return organization is null ? null : Map(organization);
    }

    private static PlatformOrganizationDto Map(PlatformOrganization organization) =>
        new(
            organization.Id.Value,
            organization.DisplayName,
            organization.Slug,
            organization.Status.ToString(),
            organization.CreatedAtUtc,
            organization.UpdatedAtUtc);
}
