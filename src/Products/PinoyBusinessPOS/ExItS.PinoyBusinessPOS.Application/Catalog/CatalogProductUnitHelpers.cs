using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.Application.Catalog;

internal static class CatalogProductUnitHelpers
{
    public static PosCatalogProductUnitDto MapUnit(CatalogProductUnit unit) =>
        new(
            unit.Id.Value,
            unit.ProductId.Value,
            unit.Kind.ToString(),
            unit.DisplayName,
            unit.ShortLabel,
            unit.MultiplierToBase,
            unit.SellingPrice,
            unit.AllowsCustomQuantity,
            unit.IsActive,
            unit.SortOrder);

    public static ProductUsageCapabilities ResolveUsage(
        bool? canBePurchased,
        bool? canBeSold,
        bool? canBeUsedAsIngredient,
        bool? isProduced,
        string? usagePreset,
        string? businessUsage = null)
    {
        // Explicit business-usage classification wins over raw flags/presets.
        if (!string.IsNullOrWhiteSpace(businessUsage))
        {
            return ProductBusinessUsages.ToCapabilities(
                ProductBusinessUsages.ParseRequired(businessUsage));
        }

        if (!string.IsNullOrWhiteSpace(usagePreset)
            && canBePurchased is null
            && canBeSold is null
            && canBeUsedAsIngredient is null
            && isProduced is null)
        {
            return ProductUsageCapabilities.FromPreset(usagePreset);
        }

        if (canBePurchased is null
            && canBeSold is null
            && canBeUsedAsIngredient is null
            && isProduced is null
            && string.IsNullOrWhiteSpace(usagePreset))
        {
            return ProductUsageCapabilities.BuyAndSell;
        }

        var fromPreset = string.IsNullOrWhiteSpace(usagePreset)
            ? null
            : ProductUsageCapabilities.FromPreset(usagePreset);

        return ProductUsageCapabilities.Create(
            canBePurchased ?? fromPreset?.CanBePurchased ?? true,
            canBeSold ?? fromPreset?.CanBeSold ?? true,
            canBeUsedAsIngredient ?? fromPreset?.CanBeUsedAsIngredient ?? false,
            isProduced ?? fromPreset?.IsProduced ?? false,
            usagePreset ?? fromPreset?.PresetCode);
    }

    public static IReadOnlyList<CatalogProductUnit> CreateDefaultOneToOneUnits(
        PosOrganizationId organizationId,
        CatalogProduct product,
        DateTimeOffset utcNow)
    {
        var label = UnitOfMeasures.ToCode(product.UnitOfMeasure);
        var shortLabel = label.Length <= CatalogProductUnit.ShortLabelMaxLength
            ? label
            : label[..CatalogProductUnit.ShortLabelMaxLength];

        var purchase = CatalogProductUnit.Create(
            organizationId,
            product.Id,
            ProductUnitKind.Purchase,
            label,
            shortLabel,
            multiplierToBase: 1m,
            utcNow,
            sellingPrice: null,
            allowsCustomQuantity: false,
            sortOrder: 0);

        var sell = CatalogProductUnit.Create(
            organizationId,
            product.Id,
            ProductUnitKind.Sell,
            label,
            shortLabel,
            multiplierToBase: 1m,
            utcNow,
            sellingPrice: product.SellingPrice,
            allowsCustomQuantity: product.SellingMode == SellingMode.ByWeight,
            sortOrder: 0);

        return [purchase, sell];
    }

    public static CatalogProductUnit CreateFromInput(
        PosOrganizationId organizationId,
        CatalogProductId productId,
        PosCatalogProductUnitInput input,
        DateTimeOffset utcNow)
    {
        if (!Enum.TryParse<ProductUnitKind>(input.Kind, ignoreCase: true, out var kind))
        {
            throw new Domain.Common.DomainException(
                Domain.Common.DomainErrorCodes.InvalidProductUnitKind,
                $"Unknown product unit kind '{input.Kind}'.");
        }

        return CatalogProductUnit.Create(
            organizationId,
            productId,
            kind,
            input.DisplayName,
            input.ShortLabel,
            input.MultiplierToBase,
            utcNow,
            input.SellingPrice,
            input.AllowsCustomQuantity,
            input.SortOrder,
            input.UnitId is null ? null : ProductUnitId.From(input.UnitId.Value));
    }

    public static decimal? PrimarySellUnitPrice(IEnumerable<CatalogProductUnit> units) =>
        units
            .Where(u => u.IsActive && u.Kind == ProductUnitKind.Sell)
            .OrderBy(u => u.SortOrder)
            .ThenBy(u => u.DisplayName)
            .Select(u => u.SellingPrice)
            .FirstOrDefault();
}
