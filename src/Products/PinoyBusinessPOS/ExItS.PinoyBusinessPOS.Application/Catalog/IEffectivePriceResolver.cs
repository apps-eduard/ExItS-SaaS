using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;

namespace ExItS.PinoyBusinessPOS.Application.Catalog;

public sealed record EffectivePriceKey(Guid ProductId, Guid ProductUnitId);

public sealed record EffectivePriceResult(
    decimal OrganizationDefaultPrice,
    decimal? BranchOverridePrice,
    decimal EffectivePrice,
    bool HasBranchPriceOverride);

/// <summary>
/// Central effective-price authority: BranchOverride ?? OrganizationDefaultPrice (MB2-03).
/// </summary>
public interface IEffectivePriceResolver
{
    Task<IReadOnlyDictionary<EffectivePriceKey, EffectivePriceResult>> ResolveAsync(
        PosOrganizationId organizationId,
        PosBranchId branchId,
        IReadOnlyList<CatalogProduct> products,
        IReadOnlyDictionary<CatalogProductId, IReadOnlyList<CatalogProductUnit>>? unitsByProduct = null,
        CancellationToken cancellationToken = default);
}

public static class EffectivePriceKeys
{
    public static EffectivePriceKey ForBaseProduct(Guid productId) =>
        new(productId, BranchProductPriceOverride.BaseProductUnitKey);

    public static EffectivePriceKey ForSellUnit(Guid productId, Guid unitId) =>
        new(productId, unitId);
}
