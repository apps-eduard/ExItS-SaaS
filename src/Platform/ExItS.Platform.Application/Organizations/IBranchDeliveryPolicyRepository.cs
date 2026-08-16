using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.Application.Organizations;

public interface IBranchDeliveryPolicyRepository
{
    Task<BranchDeliveryPolicy?> GetByBranchIdAsync(OrganizationBranchId branchId, CancellationToken cancellationToken = default);
    Task AddAsync(BranchDeliveryPolicy policy, CancellationToken cancellationToken = default);
    Task UpdateAsync(BranchDeliveryPolicy policy, CancellationToken cancellationToken = default);
}
