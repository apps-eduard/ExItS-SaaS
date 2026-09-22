using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Platform;

namespace ExItS.PinoyBusinessPOS.Application.ConnectedSuppliers;

/// <summary>
/// POS API → Platform gateway for branch fulfillment settings.
/// Kept separate from Maui/Web <see cref="IPlatformAccessClient"/> (not registered in the API host).
/// </summary>
public interface IPlatformBranchFulfillmentGateway
{
    Task<ApplicationResult<BranchFulfillmentReadinessDto>> GetReadinessAsync(
        Guid organizationId,
        Guid branchId,
        CancellationToken cancellationToken = default);

    Task<ApplicationResult<BranchFulfillmentReadinessDto>> UpdateSettingsAsync(
        Guid organizationId,
        Guid branchId,
        UpdateBranchFulfillmentSettingsRequest request,
        CancellationToken cancellationToken = default);
}
