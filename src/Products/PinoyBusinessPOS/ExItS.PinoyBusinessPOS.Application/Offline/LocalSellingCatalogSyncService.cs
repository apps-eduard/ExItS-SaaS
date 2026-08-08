using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Domain.CashierShifts;

namespace ExItS.PinoyBusinessPOS.Application.Offline;

/// <summary>
/// Refreshes the merchant selling catalog + open-shift snapshot into the local SQLite cache while online.
/// </summary>
public sealed class LocalSellingCatalogSyncService(
    IPosCatalogClient catalog,
    IPosCashierShiftClient shifts,
    ILocalSellingCatalogStore localStore,
    ILocalContextManager contextManager,
    IConnectivityService connectivity) : ILocalSellingCatalogSyncService
{
    private const int PageSize = 100;
    private const int MaxPages = 20;

    public async Task RefreshFromServerAsync(CancellationToken ct = default)
    {
        if (contextManager.ActiveContext is null)
        {
            return;
        }

        if (!await connectivity.IsConnectedAsync(ct).ConfigureAwait(false))
        {
            return;
        }

        var categories = new List<PosProductCategoryDto>();
        var catPage = 1;
        while (catPage <= MaxPages)
        {
            var result = await catalog
                .ListCategoriesAsync(
                    status: PosCatalogOptions.ActiveStatus,
                    page: catPage,
                    pageSize: PageSize,
                    ct: ct)
                .ConfigureAwait(false);
            if (!result.IsSuccess || result.Data is null || result.Data.Items.Count == 0)
            {
                break;
            }

            categories.AddRange(result.Data.Items);
            if (categories.Count >= result.Data.TotalCount || result.Data.Items.Count < PageSize)
            {
                break;
            }

            catPage++;
        }

        var products = new List<PosCatalogProductDto>();
        var productPage = 1;
        while (productPage <= MaxPages)
        {
            var result = await catalog
                .ListProductsAsync(
                    search: null,
                    status: PosCatalogOptions.ActiveStatus,
                    categoryId: null,
                    unitOfMeasure: null,
                    page: productPage,
                    pageSize: PageSize,
                    ct: ct)
                .ConfigureAwait(false);
            if (!result.IsSuccess || result.Data is null || result.Data.Items.Count == 0)
            {
                break;
            }

            products.AddRange(result.Data.Items);
            if (products.Count >= result.Data.TotalCount || result.Data.Items.Count < PageSize)
            {
                break;
            }

            productPage++;
        }

        if (categories.Count > 0 || products.Count > 0)
        {
            await localStore.ReplaceCatalogAsync(categories, products, ct).ConfigureAwait(false);
        }

        var shiftResult = await shifts.GetCurrentAsync(ct).ConfigureAwait(false);
        if (shiftResult.IsSuccess && shiftResult.Data is not null
            && string.Equals(shiftResult.Data.Status, nameof(CashierShiftStatus.Open), StringComparison.Ordinal))
        {
            await localStore.SaveOpenShiftSnapshotAsync(shiftResult.Data, ct).ConfigureAwait(false);
        }
        else if (shiftResult.IsSuccess)
        {
            await localStore.ClearOpenShiftSnapshotAsync(ct).ConfigureAwait(false);
        }
    }
}
