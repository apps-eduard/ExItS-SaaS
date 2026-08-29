using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.Application.Inventory;

/// <summary>
/// Resolves latest authoritative acquisition unit costs for checkout COGS snapshots.
/// Uses batch repository queries to avoid N+1; never accepts client-supplied cost authority.
/// </summary>
public sealed class InventoryCostResolver(IInventoryRepository inventory)
{
    public Task<decimal?> ResolveUnitCostAsync(
        PosOrganizationId organizationId,
        CatalogProductId productId,
        CancellationToken cancellationToken = default) =>
        inventory.GetLatestAcquisitionUnitCostAsync(organizationId, productId, cancellationToken);

    public async Task<IReadOnlyDictionary<Guid, decimal?>> ResolveUnitCostsAsync(
        PosOrganizationId organizationId,
        IEnumerable<CatalogProductId> productIds,
        CancellationToken cancellationToken = default)
    {
        var ids = productIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return new Dictionary<Guid, decimal?>();
        }

        var resolved = await inventory
            .GetLatestAcquisitionUnitCostsAsync(organizationId, ids, cancellationToken)
            .ConfigureAwait(false);

        return ids.ToDictionary(
            id => id.Value,
            id => resolved.TryGetValue(id.Value, out var cost) ? cost : null);
    }

    public async Task<IReadOnlyList<SaleLineDraft>> EnrichDraftsWithCostsAsync(
        PosOrganizationId organizationId,
        IReadOnlyList<SaleLineDraft> drafts,
        CancellationToken cancellationToken = default)
    {
        if (drafts.Count == 0)
        {
            return drafts;
        }

        var costs = await ResolveUnitCostsAsync(
                organizationId,
                drafts.Select(d => d.ProductId),
                cancellationToken)
            .ConfigureAwait(false);

        return drafts
            .Select(d => d with { UnitCostSnapshot = costs.GetValueOrDefault(d.ProductId.Value) })
            .ToList();
    }
}
