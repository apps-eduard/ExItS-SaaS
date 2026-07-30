using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.Application.Catalog;

/// <summary>Product catalog filter for list and search. Online-only; no offline cache contract.</summary>
public sealed record CatalogProductFilter(
    CatalogProductStatus? Status = null,
    ProductCategoryId? CategoryId = null,
    UnitOfMeasure? UnitOfMeasure = null,
    string? Search = null);

public interface ICatalogProductRepository
{
    Task<CatalogProduct?> GetByIdAsync(
        PosOrganizationId organizationId,
        CatalogProductId productId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds a product by normalized SKU regardless of status. SKUs of inactive products stay
    /// reserved so historical identifiers are never silently reused.
    /// </summary>
    Task<CatalogProduct?> FindByNormalizedSkuAsync(
        PosOrganizationId organizationId,
        string normalizedSku,
        CancellationToken cancellationToken = default);

    /// <summary>Finds a product by exact barcode regardless of status (inactive barcodes stay reserved).</summary>
    Task<CatalogProduct?> FindByBarcodeAsync(
        PosOrganizationId organizationId,
        string barcode,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<CatalogProduct> Items, int TotalCount)> ListAsync(
        PosOrganizationId organizationId,
        CatalogProductFilter filter,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task AddAsync(CatalogProduct product, CancellationToken cancellationToken = default);

    Task UpdateAsync(CatalogProduct product, CancellationToken cancellationToken = default);
}
