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

    Task AddAsync(BranchProductAvailability availability, CancellationToken cancellationToken = default);

    Task UpdateAsync(BranchProductAvailability availability, CancellationToken cancellationToken = default);
}
