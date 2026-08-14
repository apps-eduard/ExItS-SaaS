using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.Application.Catalog;

public interface ICatalogProductUnitRepository
{
    Task<CatalogProductUnit?> GetByIdAsync(
        PosOrganizationId organizationId,
        ProductUnitId unitId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CatalogProductUnit>> ListByProductAsync(
        PosOrganizationId organizationId,
        CatalogProductId productId,
        CancellationToken cancellationToken = default);

    /// <summary>Batch-loads units for many products in one query (keyed by product id).</summary>
    Task<IReadOnlyDictionary<Guid, IReadOnlyList<CatalogProductUnit>>> ListByProductIdsAsync(
        PosOrganizationId organizationId,
        IReadOnlyCollection<CatalogProductId> productIds,
        CancellationToken cancellationToken = default);

    Task AddAsync(CatalogProductUnit unit, CancellationToken cancellationToken = default);

    Task UpdateAsync(CatalogProductUnit unit, CancellationToken cancellationToken = default);

    /// <summary>
    /// Replaces active units of the given kind for a product: deactivates existing active rows
    /// of that kind, then inserts the provided units.
    /// </summary>
    Task ReplaceActiveUnitsAsync(
        PosOrganizationId organizationId,
        CatalogProductId productId,
        ProductUnitKind kind,
        IReadOnlyList<CatalogProductUnit> units,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken = default);
}
