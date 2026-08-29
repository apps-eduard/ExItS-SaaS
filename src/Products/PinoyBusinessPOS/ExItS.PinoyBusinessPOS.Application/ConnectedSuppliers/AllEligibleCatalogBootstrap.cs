using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.Application.ConnectedSuppliers;

/// <summary>
/// When a connection uses AllEligible, stage Default PO from SellingPrice (when missing)
/// and sync exposures so buyer catalog + pricing can resolve without per-product Share clicks.
/// </summary>
internal static class AllEligibleCatalogBootstrap
{
    private const int PageSize = 200;

    public static async Task EnsureExposuresFromSellingPriceAsync(
        PosOrganizationId supplier,
        ICatalogProductRepository? products,
        ISupplierProductExposureRepository? exposures,
        DateTimeOffset utcNow,
        CancellationToken ct)
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
                await ConnectedProductExposureSync.SyncAsync(product, exposures, utcNow, ct)
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
