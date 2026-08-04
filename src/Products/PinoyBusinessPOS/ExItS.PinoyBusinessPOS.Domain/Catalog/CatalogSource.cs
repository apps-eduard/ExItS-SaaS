namespace ExItS.PinoyBusinessPOS.Domain.Catalog;

/// <summary>
/// How a local POS catalog product entered the organization catalog.
/// Informational only — Platform never overwrites local price/stock/tax/name/category/active.
/// </summary>
public enum CatalogSource
{
    Manual = 0,
    Template = 1,
    GlobalSearch = 2,
    BulkImport = 3
}
