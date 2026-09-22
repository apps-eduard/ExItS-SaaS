using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Catalog;

namespace ExItS.PinoyBusinessPOS.Application.Inventory;

/// <summary>
/// Keeps supplier exposures aligned when inventory tracking flips so AllEligible
/// buyer catalogs update without a manual share/resync step.
/// </summary>
internal static class ConnectedBuyerTrackingExposureSync
{
    public static async Task AfterTrackingEnabledAsync(
        CatalogProduct product,
        ICatalogProductRepository products,
        ISupplierProductExposureRepository? exposures,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        if (exposures is null)
        {
            return;
        }

        if (product.Scope == CatalogProductScope.BranchLocal
            || product.IsBlockedFromConnectedBuyers
            || !product.CanBeSold)
        {
            return;
        }

        var baseline = product.DefaultConnectedPoPrice is > 0m
            ? product.DefaultConnectedPoPrice.Value
            : product.SellingPrice is > 0m
                ? product.SellingPrice
                : (decimal?)null;
        if (baseline is null)
        {
            return;
        }

        if (product.DefaultConnectedPoPrice is null)
        {
            product.SetDefaultConnectedPoPrice(baseline.Value, utcNow);
        }

        if (!product.CanExposeToConnectedBuyers)
        {
            product.AllowForConnectedBuyers(utcNow);
        }

        await products.UpdateAsync(product, cancellationToken).ConfigureAwait(false);
        // knownIsTracked: true — caller must persist inventory tracking before/with this sync.
        // Do not re-read inventory (same-UoW AsNoTracking / race before SaveChanges).
        await ConnectedProductExposureSync
            .SyncAsync(
                product,
                exposures,
                utcNow,
                cancellationToken,
                inventory: null,
                categories: null,
                knownIsTracked: true)
            .ConfigureAwait(false);
    }

    public static async Task AfterTrackingDisabledAsync(
        CatalogProduct product,
        ISupplierProductExposureRepository? exposures,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        if (exposures is null)
        {
            return;
        }

        await ConnectedProductExposureSync
            .SyncAsync(
                product,
                exposures,
                utcNow,
                cancellationToken,
                inventory: null,
                categories: null,
                knownIsTracked: false)
            .ConfigureAwait(false);
    }
}
