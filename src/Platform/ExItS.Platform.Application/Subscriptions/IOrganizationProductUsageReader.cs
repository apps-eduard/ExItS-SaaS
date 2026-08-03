using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Products;

namespace ExItS.Platform.Application.Subscriptions;

public sealed record OrganizationProductUsageSnapshot(
    int ActiveStaffCount,
    int? ActiveBranchCount,
    bool BranchCountAvailable,
    string? BranchCountUnavailableReason);

public interface IOrganizationProductUsageReader
{
    Task<OrganizationProductUsageSnapshot> GetUsageAsync(
        PlatformOrganizationId organizationId,
        ProductCode productCode,
        CancellationToken cancellationToken = default);
}
