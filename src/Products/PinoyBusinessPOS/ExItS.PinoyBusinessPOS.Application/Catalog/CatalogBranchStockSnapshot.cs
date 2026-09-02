namespace ExItS.PinoyBusinessPOS.Application.Catalog;

/// <summary>Branch-scoped sale-eligible stock summary for catalog/sell surfaces.</summary>
public sealed record CatalogBranchStockSnapshot(
    decimal OrganizationOnHand,
    decimal BranchOnHand,
    decimal BranchAvailable,
    decimal SaleEligibleQuantity,
    decimal? SellableQuantity,
    bool IsLowStock,
    string StockStatus);
