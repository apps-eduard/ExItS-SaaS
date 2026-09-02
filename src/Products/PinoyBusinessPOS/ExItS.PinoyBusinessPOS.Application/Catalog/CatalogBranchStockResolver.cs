using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;

namespace ExItS.PinoyBusinessPOS.Application.Catalog;

/// <summary>
/// Batch branch stock resolver for catalog list/detail enrichment (no per-product HTTP or DB round-trips).
/// </summary>
public sealed class CatalogBranchStockResolver
{
    private readonly IInventoryRepository _inventory;
    private readonly BranchInventoryReadService _branchReads;
    private readonly IInventoryLotRepository _lots;
    private readonly IClock _clock;

    public CatalogBranchStockResolver(
        IInventoryRepository inventory,
        BranchInventoryReadService branchReads,
        IInventoryLotRepository lots,
        IClock clock)
    {
        _inventory = inventory;
        _branchReads = branchReads;
        _lots = lots;
        _clock = clock;
    }

    public async Task<IReadOnlyDictionary<Guid, CatalogBranchStockSnapshot>> ResolveAsync(
        BranchInventoryContext context,
        IReadOnlyList<PosCatalogProductDto> products,
        CancellationToken cancellationToken = default)
    {
        if (products.Count == 0)
        {
            return new Dictionary<Guid, CatalogBranchStockSnapshot>();
        }

        var orgId = PosOrganizationId.From(context.OrganizationId);
        var productIds = products.Select(p => CatalogProductId.From(p.ProductId)).Distinct().ToList();
        var accounts = await _inventory
            .ListByProductIdsAsync(orgId, productIds, cancellationToken)
            .ConfigureAwait(false);
        var accountByProduct = accounts.ToDictionary(a => a.ProductId.Value);
        var branchReads = await _branchReads
            .ResolveAsync(context, accounts, cancellationToken)
            .ConfigureAwait(false);

        var expirationProducts = products
            .Where(p => p.TracksExpiration && accountByProduct.TryGetValue(p.ProductId, out var a) && a.IsTracked)
            .Select(p => CatalogProductId.From(p.ProductId))
            .Distinct()
            .ToList();

        var sellableByProduct = await ResolveSellableByProductAsync(
                orgId,
                context,
                expirationProducts,
                cancellationToken)
            .ConfigureAwait(false);

        var result = new Dictionary<Guid, CatalogBranchStockSnapshot>(products.Count);
        foreach (var product in products)
        {
            if (!accountByProduct.TryGetValue(product.ProductId, out var account)
                || !branchReads.TryGetValue(product.ProductId, out var branchRead))
            {
                continue;
            }

            decimal? sellable = null;
            if (sellableByProduct.TryGetValue(product.ProductId, out var sellableValue))
            {
                sellable = sellableValue;
            }

            var snapshot = CatalogBranchStockEnrichment.BuildSnapshot(
                account.IsTracked,
                branchRead,
                sellable);
            result[product.ProductId] = snapshot;
        }

        return result;
    }

    private async Task<IReadOnlyDictionary<Guid, decimal>> ResolveSellableByProductAsync(
        PosOrganizationId organizationId,
        BranchInventoryContext context,
        IReadOnlyList<CatalogProductId> productIds,
        CancellationToken cancellationToken)
    {
        if (productIds.Count == 0)
        {
            return new Dictionary<Guid, decimal>();
        }

        var branchId = PosBranchId.From(context.BranchId);
        var today = InventoryLot.BusinessDateOf(_clock.UtcNow);
        var result = new Dictionary<Guid, decimal>(productIds.Count);
        foreach (var productId in productIds)
        {
            var lots = await _lots
                .ListOnHandAsync(organizationId, productId, branchId, includeDepleted: false, cancellationToken)
                .ConfigureAwait(false);
            if (context.PrimaryBranchId is not null
                && context.PrimaryBranchId.Value == context.BranchId)
            {
                var legacyLots = await _lots
                    .ListOrgLevelOnHandAsync(organizationId, productId, includeDepleted: false, cancellationToken)
                    .ConfigureAwait(false);
                lots = InventoryLotCompatibility.UnionByLotId(lots, legacyLots).ToList();
            }

            result[productId.Value] = InventoryLotFefo.SellableQuantity(lots, today);
        }

        return result;
    }
}
