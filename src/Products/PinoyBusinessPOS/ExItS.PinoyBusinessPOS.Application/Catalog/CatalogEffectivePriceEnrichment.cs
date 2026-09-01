namespace ExItS.PinoyBusinessPOS.Application.Catalog;

public static class CatalogEffectivePriceEnrichment
{
    public static PosCatalogProductDto Apply(
        PosCatalogProductDto product,
        IReadOnlyDictionary<EffectivePriceKey, EffectivePriceResult> resolved)
    {
        var baseKey = EffectivePriceKeys.ForBaseProduct(product.ProductId);
        if (!resolved.TryGetValue(baseKey, out var basePrice))
        {
            return product;
        }

        IReadOnlyList<PosCatalogProductUnitDto>? units = product.Units;
        if (product.Units is { Count: > 0 })
        {
            units = product.Units
                .Select(u =>
                {
                    var key = EffectivePriceKeys.ForSellUnit(product.ProductId, u.UnitId);
                    if (!resolved.TryGetValue(key, out var unitPrice))
                    {
                        return u;
                    }

                    return u with { EffectiveSellingPrice = unitPrice.EffectivePrice };
                })
                .ToList();
        }

        return product with
        {
            EffectiveSellingPrice = basePrice.EffectivePrice,
            HasBranchPriceOverride = basePrice.HasBranchPriceOverride,
            Units = units
        };
    }

    public static IReadOnlyList<PosCatalogProductDto> ApplyMany(
        IReadOnlyList<PosCatalogProductDto> products,
        IReadOnlyDictionary<EffectivePriceKey, EffectivePriceResult> resolved) =>
        products.Select(p => Apply(p, resolved)).ToList();
}
