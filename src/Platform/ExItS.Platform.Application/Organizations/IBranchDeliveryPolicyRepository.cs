using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.Application.Organizations;

public interface IBranchDeliveryPolicyRepository
{
    Task<BranchDeliveryPolicy?> GetByBranchIdAsync(OrganizationBranchId branchId, CancellationToken cancellationToken = default);

    /// <summary>
    /// One-shot load of all delivery policies for an organization (avoids ListBranches N+1).
    /// </summary>
    Task<IReadOnlyList<BranchDeliveryPolicy>> ListByOrganizationAsync(
        PlatformOrganizationId organizationId,
        CancellationToken cancellationToken = default);

    Task AddAsync(BranchDeliveryPolicy policy, CancellationToken cancellationToken = default);
    Task UpdateAsync(BranchDeliveryPolicy policy, CancellationToken cancellationToken = default);
}
