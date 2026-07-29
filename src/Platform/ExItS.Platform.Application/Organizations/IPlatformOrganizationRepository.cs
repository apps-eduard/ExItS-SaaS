using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.Application.Organizations;

public interface IPlatformOrganizationRepository
{
    Task<PlatformOrganization?> GetByIdAsync(PlatformOrganizationId id, CancellationToken cancellationToken = default);

    Task<PlatformOrganization?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<PlatformOrganization> Items, int TotalCount)> ListAsync(
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task AddAsync(PlatformOrganization organization, CancellationToken cancellationToken = default);

    Task UpdateAsync(PlatformOrganization organization, CancellationToken cancellationToken = default);
}
