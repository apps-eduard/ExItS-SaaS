using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Inventory;

namespace ExItS.PinoyBusinessPOS.Application.Branches;

/// <summary>
/// Authoritative retail-sale eligibility for a Platform branch type.
/// Warehouse branches remain inventory/purchasing locations; they cannot finalize retail sales.
/// </summary>
public static class BranchRetailSalesGuard
{
    public const string WarehouseSalesMessage = "Sales are not available for warehouse branches.";

    public static async Task<ApplicationResult?> RejectIfWarehouseAsync(
        IOrganizationBranchDirectory? branches,
        Guid organizationId,
        Guid? branchId,
        CancellationToken cancellationToken = default)
    {
        if (branches is null || branchId is not Guid id || id == Guid.Empty)
        {
            return null;
        }

        var type = await branches
            .GetBranchTypeAsync(organizationId, id, cancellationToken)
            .ConfigureAwait(false);
        if (!string.Equals(type, "Warehouse", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return ApplicationResult.Failure(
            ApplicationErrorCodes.WarehouseBranchSalesForbidden,
            WarehouseSalesMessage);
    }
}
