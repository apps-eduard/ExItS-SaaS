using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;

namespace ExItS.PinoyBusinessPOS.Application.Catalog;

public sealed class EffectivePriceResolver : IEffectivePriceResolver
{
    private readonly IBranchProductPriceOverrideRepository _overrides;

    public EffectivePriceResolver(IBranchProductPriceOverrideRepository overrides) =>
        _overrides = overrides;

    public async Task<IReadOnlyDictionary<EffectivePriceKey, EffectivePriceResult>> ResolveAsync(
        PosOrganizationId organizationId,
        PosBranchId branchId,
        IReadOnlyList<CatalogProduct> products,
        IReadOnlyDictionary<CatalogProductId, IReadOnlyList<CatalogProductUnit>>? unitsByProduct = null,
        CancellationToken cancellationToken = default)
    {
        if (products.Count == 0)
        {
            return new Dictionary<EffectivePriceKey, EffectivePriceResult>();
        }

        var productIds = products.Select(p => p.Id).Distinct().ToList();
        var overrideRows = await _overrides
            .ListByBranchAndProductIdsAsync(organizationId, branchId, productIds, cancellationToken)
            .ConfigureAwait(false);
        var overridesByKey = overrideRows.ToDictionary(
            o => new EffectivePriceKey(o.ProductId.Value, o.ProductUnitId));

        var resolved = new Dictionary<EffectivePriceKey, EffectivePriceResult>();
        foreach (var product in products)
        {
            var baseKey = EffectivePriceKeys.ForBaseProduct(product.Id.Value);
            resolved[baseKey] = ResolveEntry(
                product.SellingPrice,
                overridesByKey.GetValueOrDefault(baseKey));

            if (unitsByProduct is null
                || !unitsByProduct.TryGetValue(product.Id, out var units)
                || units.Count == 0)
            {
                continue;
            }

            foreach (var unit in units.Where(u => u.IsActive && u.Kind == ProductUnitKind.Sell))
            {
                var unitKey = EffectivePriceKeys.ForSellUnit(product.Id.Value, unit.Id.Value);
                var orgDefault = unit.SellingPrice ?? product.SellingPrice;
                resolved[unitKey] = ResolveEntry(orgDefault, overridesByKey.GetValueOrDefault(unitKey));
            }
        }

        return resolved;
    }

    private static EffectivePriceResult ResolveEntry(
        decimal organizationDefaultPrice,
        BranchProductPriceOverride? overrideRow)
    {
        if (overrideRow is null)
        {
            return new EffectivePriceResult(
                organizationDefaultPrice,
                BranchOverridePrice: null,
                organizationDefaultPrice,
                HasBranchPriceOverride: false);
        }

        return new EffectivePriceResult(
            organizationDefaultPrice,
            overrideRow.SellingPrice,
            overrideRow.SellingPrice,
            HasBranchPriceOverride: true);
    }
}
