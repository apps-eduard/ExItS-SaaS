using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.UnitTests.Support;

/// <summary>Empty branch store for plan-change / downgrade unit tests.</summary>
internal sealed class EmptyOrganizationBranchRepository : IOrganizationBranchRepository
{
    public Task<OrganizationBranch?> GetByIdAsync(
        OrganizationBranchId id,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<OrganizationBranch?>(null);

    public Task<OrganizationBranch?> GetPrimaryAsync(
        PlatformOrganizationId organizationId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<OrganizationBranch?>(null);

    public Task<IReadOnlyList<OrganizationBranch>> ListByOrganizationAsync(
        PlatformOrganizationId organizationId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<OrganizationBranch>>([]);

    public Task<int> CountActiveAsync(
        PlatformOrganizationId organizationId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(0);

    public Task AddAsync(OrganizationBranch branch, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task UpdateAsync(OrganizationBranch branch, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
