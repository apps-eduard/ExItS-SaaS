using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.Application.Organizations;

public interface IOrganizationBranchRepository
{
    Task<OrganizationBranch?> GetByIdAsync(OrganizationBranchId id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OrganizationBranch>> ListByOrganizationAsync(PlatformOrganizationId organizationId, CancellationToken cancellationToken = default);
    Task<int> CountActiveAsync(PlatformOrganizationId organizationId, CancellationToken cancellationToken = default);
    Task<OrganizationBranch?> GetPrimaryAsync(PlatformOrganizationId organizationId, CancellationToken cancellationToken = default);
    Task AddAsync(OrganizationBranch branch, CancellationToken cancellationToken = default);
    Task UpdateAsync(OrganizationBranch branch, CancellationToken cancellationToken = default);
}
