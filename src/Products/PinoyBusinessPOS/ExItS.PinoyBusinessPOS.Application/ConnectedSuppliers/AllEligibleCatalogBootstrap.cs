using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.Application.ConnectedSuppliers;

/// <summary>
/// When a connection uses AllEligible, stage Default PO from SellingPrice (when missing)
/// and sync exposures so buyer catalog + pricing can resolve without per-product Share clicks.
/// Only inventory-tracked products are eligible.
/// </summary>
internal static class AllEligibleCatalogBootstrap
{
    private const int PageSize = 200;

    public static async Task EnsureExposuresFromSellingPriceAsync(
        PosOrganizationId supplier,
        ICatalogProductRepository? products,
        ISupplierProductExposureRepository? exposures,
        DateTimeOffset utcNow,
        CancellationToken ct,
        IInventoryRepository? inventory = null)
    {
        if (products is null || exposures is null)
        {
            return;
        }

        var skip = 0;
        while (true)
        {
            var (items, total) = await products.ListAsync(
                    supplier,
                    new CatalogProductFilter(Status: CatalogProductStatus.Active),
                    skip,
                    PageSize,
                    ct)
                .ConfigureAwait(false);

            foreach (var product in items)
            {
                if (product.IsBlockedFromConnectedBuyers)
                {
                    continue;
                }

                if (!product.CanBeSold)
                {
                    continue;
                }

                if (inventory is not null)
                {
                    var tracked = await ConnectedBuyerSharingRules
                        .IsTrackedAsync(inventory, supplier, product.Id, ct)
                        .ConfigureAwait(false);
                    if (!tracked)
                    {
                        continue;
                    }
                }

                var baseline = product.DefaultConnectedPoPrice is > 0m
                    ? product.DefaultConnectedPoPrice.Value
                    : product.SellingPrice is > 0m
                        ? product.SellingPrice
                        : (decimal?)null;
                if (baseline is null)
                {
                    continue;
                }

                if (product.DefaultConnectedPoPrice is null)
                {
                    product.SetDefaultConnectedPoPrice(baseline.Value, utcNow);
                }

                if (!product.CanExposeToConnectedBuyers)
                {
                    product.AllowForConnectedBuyers(utcNow);
                }

                await products.UpdateAsync(product, ct).ConfigureAwait(false);
                // Already verified tracked above — pass knownIsTracked so Sync does not re-read
                // inventory in a way that can miss the same-UoW enable.
                await ConnectedProductExposureSync.SyncAsync(
                        product,
                        exposures,
                        utcNow,
                        ct,
                        inventory: null,
                        categories: null,
                        knownIsTracked: true)
                    .ConfigureAwait(false);
            }

            skip += items.Count;
            if (skip >= total || items.Count == 0)
            {
                break;
            }
        }
    }
}
