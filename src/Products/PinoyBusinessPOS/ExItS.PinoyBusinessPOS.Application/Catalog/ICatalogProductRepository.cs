using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.Application.Catalog;

/// <summary>Product catalog filter for list and search. Online-only; no offline cache contract.</summary>
public sealed record CatalogProductFilter(
    CatalogProductStatus? Status = null,
    ProductCategoryId? CategoryId = null,
    UnitOfMeasure? UnitOfMeasure = null,
    string? Search = null,
    bool? CanExposeToConnectedBuyers = null,
    bool UncategorizedOnly = false);

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

    /// <summary>
    /// Loads the products with the given identifiers inside one organization, regardless of status.
    /// Callers decide how missing and inactive products are reported.
    /// </summary>
    Task<IReadOnlyList<CatalogProduct>> ListByIdsAsync(
        PosOrganizationId organizationId,
        IReadOnlyCollection<CatalogProductId> productIds,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<CatalogProduct> Items, int TotalCount)> ListAsync(
        PosOrganizationId organizationId,
        CatalogProductFilter filter,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns matching product ids only (ordered by name then id). Used for select-all-matching
    /// bulk Level-1 connected-buyer availability operations.
    /// </summary>
    Task<IReadOnlyList<Guid>> ListIdsAsync(
        PosOrganizationId organizationId,
        CatalogProductFilter filter,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Active-catalog connected-buyer availability summary counts (not filter-scoped).
    /// </summary>
    Task<(int TotalCount, int AvailableCount, int NotAvailableCount)> CountConnectedBuyerAvailabilityAsync(
        PosOrganizationId organizationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Category facets for Active products matching the current Level-1 availability filters.
    /// </summary>
    Task<IReadOnlyList<(Guid? CategoryId, int Count)>> ListConnectedBuyerAvailabilityCategoryFacetsAsync(
        PosOrganizationId organizationId,
        CatalogProductFilter filter,
        CancellationToken cancellationToken = default);

    /// <summary>Finds a product by Platform global product id regardless of status.</summary>
    Task<CatalogProduct?> FindByPlatformGlobalProductIdAsync(
        PosOrganizationId organizationId,
        Guid platformGlobalProductId,
        CancellationToken cancellationToken = default);

    /// <summary>Returns which of the given Platform global product ids already exist locally.</summary>
    Task<IReadOnlySet<Guid>> ListPlatformGlobalProductIdsAsync(
        PosOrganizationId organizationId,
        IReadOnlyCollection<Guid> platformGlobalProductIds,
        CancellationToken cancellationToken = default);

    Task AddAsync(CatalogProduct product, CancellationToken cancellationToken = default);

    Task UpdateAsync(CatalogProduct product, CancellationToken cancellationToken = default);
}
