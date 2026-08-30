using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.Application.Organizations;

public interface IBranchDeliveryServiceAreaRepository
{
    Task<BranchDeliveryServiceArea?> GetByIdAsync(
        BranchDeliveryServiceAreaId id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BranchDeliveryServiceArea>> ListByBranchAsync(
        OrganizationBranchId branchId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BranchDeliveryServiceArea>> ListByOrganizationAsync(
        PlatformOrganizationId organizationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Active area counts keyed by branch id (for ListBranches readiness without N+1).
    /// </summary>
    Task<IReadOnlyDictionary<Guid, int>> CountActiveByBranchIdsAsync(
        PlatformOrganizationId organizationId,
        IReadOnlyCollection<OrganizationBranchId> branchIds,
        CancellationToken cancellationToken = default);

    Task AddAsync(BranchDeliveryServiceArea area, CancellationToken cancellationToken = default);

    Task UpdateAsync(BranchDeliveryServiceArea area, CancellationToken cancellationToken = default);
}
