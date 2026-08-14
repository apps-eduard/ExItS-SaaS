using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.Application.Organizations;

public enum OrganizationListSortBy
{
    DisplayName = 0,
    Slug = 1,
    Status = 2,
    CreatedAtUtc = 3,
    UpdatedAtUtc = 4
}

public interface IPlatformOrganizationRepository
{
    Task<PlatformOrganization?> GetByIdAsync(PlatformOrganizationId id, CancellationToken cancellationToken = default);

    Task<PlatformOrganization?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);

    Task<PlatformOrganization?> GetByPublicOrganizationIdAsync(
        string publicOrganizationId,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<PlatformOrganization> Items, int TotalCount)> ListAsync(
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<PlatformOrganization> Items, int TotalCount)> ListAsync(
        OrganizationStatus? status,
        string? search,
        OrganizationListSortBy sortBy,
        bool sortDescending,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task AddAsync(PlatformOrganization organization, CancellationToken cancellationToken = default);

    Task UpdateAsync(PlatformOrganization organization, CancellationToken cancellationToken = default);
}
