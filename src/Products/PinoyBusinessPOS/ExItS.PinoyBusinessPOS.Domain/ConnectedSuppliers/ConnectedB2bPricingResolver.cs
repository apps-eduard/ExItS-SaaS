namespace ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;

/// <summary>Percentage-discount policy inputs for B2B price resolution (no stacking).</summary>
public sealed record ConnectedB2bPricingPolicy(
    decimal OrganizationDefaultDiscountPercent,
    decimal? OrganizationCategoryDiscountPercent,
    decimal? CustomerDefaultDiscountPercent,
    decimal? CustomerCategoryDiscountPercent);

/// <summary>
/// Canonical B2B percentage discount resolver. Does not stack discounts.
/// Precedence (percentage model only): Customer Category > Customer Default >
/// Organization Category > Organization Default > Base.
/// <para>
/// Legacy relationship product fixed price (<see cref="ConnectedBuyerProductShare.BuyerSpecificPoPrice"/>)
/// remains a separate shared-catalog override and is evaluated first when present.
/// It is NOT part of Connected Commerce org/customer percentage settings and must not be
/// exposed as a fixed-price rule on the Connected Commerce Settings page.
/// </para>
/// </summary>
public static class ConnectedB2bPricingResolver
{
    public readonly record struct PricingInputs(
        decimal? ProductOverridePrice,
        Guid? ProductCategoryId,
        decimal? CustomerCategoryDiscountPercent,
        decimal? CustomerDefaultDiscountPercent,
        decimal? OrganizationCategoryDiscountPercent,
        decimal OrganizationDefaultDiscountPercent,
        decimal? SellingPrice,
        decimal ExposureSupplierOrderPrice);

    public readonly record struct PricingResult(
        decimal UnitPrice,
        decimal AppliedDiscountPercent,
        ConnectedCustomerPriceSource Source);

    public static bool TryResolve(
        SupplierProductExposure exposure,
        ConnectedBuyerProductShare? share,
        CatalogSharingMode mode,
        ConnectedB2bPricingPolicy policy,
        out decimal price,
        out ConnectedCustomerPriceSource source,
        decimal? sellingPrice = null)
    {
        price = 0m;
        source = ConnectedCustomerPriceSource.DefaultPoPrice;
        if (!exposure.IsExposed || !exposure.IsOrderable || !ConnectedPoPricing.IsProductShared(mode, share))
        {
            return false;
        }

        var resolved = Resolve(new PricingInputs(
            ProductOverridePrice: share?.BuyerSpecificPoPrice,
            ProductCategoryId: null,
            CustomerCategoryDiscountPercent: policy.CustomerCategoryDiscountPercent,
            CustomerDefaultDiscountPercent: policy.CustomerDefaultDiscountPercent,
            OrganizationCategoryDiscountPercent: policy.OrganizationCategoryDiscountPercent,
            OrganizationDefaultDiscountPercent: policy.OrganizationDefaultDiscountPercent,
            SellingPrice: sellingPrice,
            ExposureSupplierOrderPrice: exposure.SupplierOrderPrice));

        price = resolved.UnitPrice;
        source = resolved.Source;
        return true;
    }

    /// <summary>
    /// Resolve effective unit price and pricing source.
    /// Null customer/org category/default means inherit next level; org default defaults to 0.
    /// </summary>
    public static PricingResult Resolve(PricingInputs input)
    {
        if (input.ProductOverridePrice is decimal overridePrice)
        {
            return new PricingResult(
                ConnectedPoPricing.RoundMoney(overridePrice),
                AppliedDiscountPercent: 0m,
                ConnectedCustomerPriceSource.ProductOverride);
        }

        var baseline = input.SellingPrice is > 0m
            ? input.SellingPrice.Value
            : input.ExposureSupplierOrderPrice;
        var baselineSource = input.SellingPrice is > 0m
            ? ConnectedCustomerPriceSource.SellingPrice
            : ConnectedCustomerPriceSource.DefaultPoPrice;

        if (input.CustomerCategoryDiscountPercent is decimal customerCategory)
        {
            return ApplyDiscount(baseline, customerCategory, ConnectedCustomerPriceSource.CustomerCategory);
        }

        if (input.CustomerDefaultDiscountPercent is decimal customerDefault)
        {
            return ApplyDiscount(baseline, customerDefault, ConnectedCustomerPriceSource.CustomerDiscount);
        }

        if (input.OrganizationCategoryDiscountPercent is decimal orgCategory)
        {
            return ApplyDiscount(baseline, orgCategory, ConnectedCustomerPriceSource.OrganizationCategory);
        }

        var orgDefault = OrganizationConnectedCommerceSettings.NormalizeDiscount(
            input.OrganizationDefaultDiscountPercent);
        if (orgDefault > 0m)
        {
            return ApplyDiscount(baseline, orgDefault, ConnectedCustomerPriceSource.OrganizationDefault);
        }

        return new PricingResult(
            ConnectedPoPricing.RoundMoney(baseline),
            AppliedDiscountPercent: 0m,
            baselineSource);
    }

    private static PricingResult ApplyDiscount(
        decimal baseline,
        decimal discountPercent,
        ConnectedCustomerPriceSource source)
    {
        var normalized = OrganizationConnectedCommerceSettings.NormalizeDiscount(discountPercent);
        var price = ConnectedPoPricing.RoundMoney(baseline * (1m - (normalized / 100m)));
        return new PricingResult(price, normalized, source);
    }
}
