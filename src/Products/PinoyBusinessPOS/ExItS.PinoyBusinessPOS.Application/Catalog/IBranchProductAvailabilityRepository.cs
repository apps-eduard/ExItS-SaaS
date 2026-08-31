using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;

namespace ExItS.PinoyBusinessPOS.Application.Catalog;

/// <summary>
/// Sparse per-branch product offering overrides. Bulk-friendly for MB2-01B resolvers.
/// </summary>
public interface IBranchProductAvailabilityRepository
{
    Task<BranchProductAvailability?> GetAsync(
        PosOrganizationId organizationId,
        PosBranchId branchId,
        CatalogProductId productId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BranchProductAvailability>> ListByBranchAsync(
        PosOrganizationId organizationId,
        PosBranchId branchId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BranchProductAvailability>> ListByProductIdsAsync(
        PosOrganizationId organizationId,
        PosBranchId branchId,
        IReadOnlyCollection<CatalogProductId> productIds,
        CancellationToken cancellationToken = default);

    /// <summary>All sparse availability overrides for one product across branches (bulk read).</summary>
    Task<IReadOnlyList<BranchProductAvailability>> ListByProductAsync(
        PosOrganizationId organizationId,
        CatalogProductId productId,
        CancellationToken cancellationToken = default);

    Task AddAsync(BranchProductAvailability availability, CancellationToken cancellationToken = default);

    Task UpdateAsync(BranchProductAvailability availability, CancellationToken cancellationToken = default);

    Task DeleteAsync(
        PosOrganizationId organizationId,
        PosBranchId branchId,
        CatalogProductId productId,
        CancellationToken cancellationToken = default);
}
