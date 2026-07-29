using ExItS.Platform.Application.Catalog;
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

    public async Task<PagedResult<PlatformOrganizationDto>> ListAsync(
        int? page,
        int? pageSize,
        CancellationToken cancellationToken = default)
    {
        var (skip, take) = CatalogPagination.Normalize(page, pageSize);
        var (items, totalCount) = await _organizations.ListAsync(skip, take, cancellationToken).ConfigureAwait(false);
        var pageNumber = Math.Max(page ?? 1, 1);
        return new PagedResult<PlatformOrganizationDto>(
            items.Select(Map).ToList(),
            totalCount,
            pageNumber,
            take);
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
