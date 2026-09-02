using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Inventory;

namespace ExItS.PinoyBusinessPOS.Application.Catalog;

public static class CatalogBranchStockEnrichment
{
    public static PosCatalogProductDto Apply(
        PosCatalogProductDto product,
        CatalogBranchStockSnapshot? snapshot)
    {
        if (snapshot is null)
        {
            return product;
        }

        return product with
        {
            OnHandQuantity = snapshot.SaleEligibleQuantity,
            OrganizationOnHandQuantity = snapshot.OrganizationOnHand,
            BranchOnHandQuantity = snapshot.BranchOnHand,
            BranchAvailableQuantity = snapshot.BranchAvailable,
            SellableQuantity = snapshot.SellableQuantity,
            IsLowStock = snapshot.IsLowStock,
            StockStatus = snapshot.StockStatus,
        };
    }

    public static IReadOnlyList<PosCatalogProductDto> ApplyMany(
        IReadOnlyList<PosCatalogProductDto> products,
        IReadOnlyDictionary<Guid, CatalogBranchStockSnapshot> snapshots) =>
        products
            .Select(p =>
                snapshots.TryGetValue(p.ProductId, out var snapshot)
                    ? Apply(p, snapshot)
                    : p)
            .ToList();

    public static CatalogBranchStockSnapshot BuildSnapshot(
        bool isTracked,
        BranchInventoryProductRead branchRead,
        decimal? sellableQuantity)
    {
        var branchAvailable = branchRead.BranchAvailable;
        decimal? sellable = null;
        decimal saleEligible = branchAvailable;
        if (sellableQuantity is decimal sellableValue)
        {
            sellable = sellableValue;
            saleEligible = Math.Min(branchAvailable, sellableValue);
        }

        var stockStatus = isTracked
            ? InventoryStockStatuses.ToCode(
                InventoryStockStatuses.Derive(isTracked, saleEligible, branchRead.ReorderLevel))
            : InventoryStockStatuses.ToCode(InventoryStockStatus.InStock);

        var isLow = isTracked
            && branchRead.IsLowStock
            && saleEligible > 0m;

        return new CatalogBranchStockSnapshot(
            branchRead.OrganizationOnHand,
            branchRead.BranchOnHand,
            branchAvailable,
            saleEligible,
            sellable,
            isLow,
            stockStatus);
    }
}
