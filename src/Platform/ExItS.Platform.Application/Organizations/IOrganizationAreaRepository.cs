using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.Application.Organizations;

public interface IOrganizationAreaRepository
{
    Task<OrganizationArea?> GetByIdAsync(OrganizationAreaId id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OrganizationArea>> ListByOrganizationAsync(PlatformOrganizationId organizationId, CancellationToken cancellationToken = default);
    Task<int> CountActiveAsync(PlatformOrganizationId organizationId, CancellationToken cancellationToken = default);
    Task AddAsync(OrganizationArea area, CancellationToken cancellationToken = default);
    Task UpdateAsync(OrganizationArea area, CancellationToken cancellationToken = default);
}
