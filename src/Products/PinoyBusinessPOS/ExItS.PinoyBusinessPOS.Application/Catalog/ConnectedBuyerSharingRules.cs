using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.Application.Catalog;

/// <summary>
/// Canonical rule: only inventory-tracked products may be shared via Connected Buyer catalog.
/// On-hand quantity (including 0) is irrelevant to share eligibility.
/// </summary>
public static class ConnectedBuyerSharingRules
{
    public const string ShareRequiresTrackedTitle = "Product can't be shared";
    public const string ShareRequiresTrackedMessage =
        "Only inventory-tracked products can be shared with connected businesses. Enable inventory tracking first.";

    public const string DisableTrackingWhileSharedTitle = "Product is currently shared";
    public const string DisableTrackingWhileSharedMessage =
        "Stop sharing this product before disabling inventory tracking.";

    public static ApplicationResult ValidateCanEnableSharing(bool isTracked)
    {
        if (!isTracked)
        {
            return ApplicationResult.Failure(
                DomainErrorCodes.ConnectedShareRequiresTrackedInventory,
                ShareRequiresTrackedMessage);
        }

        return ApplicationResult.Success();
    }

    public static ApplicationResult ValidateCanDisableTracking(bool isShared)
    {
        if (isShared)
        {
            return ApplicationResult.Failure(
                DomainErrorCodes.ConnectedShareBlocksDisableTracking,
                DisableTrackingWhileSharedMessage);
        }

        return ApplicationResult.Success();
    }

    public static async Task<bool> IsTrackedAsync(
        IInventoryRepository inventory,
        PosOrganizationId organizationId,
        CatalogProductId productId,
        CancellationToken cancellationToken = default)
    {
        var account = await inventory
            .GetByProductIdAsync(organizationId, productId, cancellationToken)
            .ConfigureAwait(false);
        return account is { IsTracked: true };
    }
}
