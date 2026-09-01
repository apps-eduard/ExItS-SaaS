using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;

namespace ExItS.PinoyBusinessPOS.Application.Catalog;

/// <summary>Sparse per-branch product price overrides. Bulk-friendly for effective-price resolvers.</summary>
public interface IBranchProductPriceOverrideRepository
{
    Task<BranchProductPriceOverride?> GetAsync(
        PosOrganizationId organizationId,
        PosBranchId branchId,
        CatalogProductId productId,
        Guid productUnitId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BranchProductPriceOverride>> ListByBranchAndProductIdsAsync(
        PosOrganizationId organizationId,
        PosBranchId branchId,
        IReadOnlyCollection<CatalogProductId> productIds,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BranchProductPriceOverride>> ListByProductAsync(
        PosOrganizationId organizationId,
        CatalogProductId productId,
        CancellationToken cancellationToken = default);

    Task AddAsync(BranchProductPriceOverride priceOverride, CancellationToken cancellationToken = default);

    Task UpdateAsync(BranchProductPriceOverride priceOverride, CancellationToken cancellationToken = default);

    Task DeleteAsync(
        PosOrganizationId organizationId,
        PosBranchId branchId,
        CatalogProductId productId,
        Guid productUnitId,
        CancellationToken cancellationToken = default);
}
