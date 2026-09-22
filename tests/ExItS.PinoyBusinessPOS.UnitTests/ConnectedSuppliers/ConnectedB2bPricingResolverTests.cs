using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.UnitTests.ConnectedSuppliers;

public sealed class ConnectedB2bPricingResolverTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 18, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Product_override_has_highest_precedence()
    {
        var exposure = CreateExposure(100m);
        var share = ConnectedBuyerProductShare.Share(
            ConnectedSupplierRelationshipId.New(),
            PosOrganizationId.From(Guid.NewGuid()),
            PosOrganizationId.From(Guid.NewGuid()),
            exposure.ProductId,
            Now,
            buyerSpecificPoPrice: 77m);
        var policy = new ConnectedB2bPricingPolicy(10m, 15m, 20m, 25m);

        var resolved = ConnectedB2bPricingResolver.TryResolve(
            exposure,
            share,
            CatalogSharingMode.AllEligible,
            policy,
            out var price,
            out var source);

        Assert.True(resolved);
        Assert.Equal(77m, price);
        Assert.Equal(ConnectedCustomerPriceSource.ProductOverride, source);
    }

    [Fact]
    public void Discount_precedence_customer_category_then_customer_default_then_org_category_then_org_default()
    {
        var exposure = CreateExposure(200m);
        Assert.True(ConnectedB2bPricingResolver.TryResolve(
            exposure,
            share: null,
            CatalogSharingMode.AllEligible,
            new ConnectedB2bPricingPolicy(5m, 10m, 20m, 30m),
            out var customerCategoryPrice,
            out var customerCategorySource));
        Assert.Equal(140m, customerCategoryPrice);
        Assert.Equal(ConnectedCustomerPriceSource.CustomerCategory, customerCategorySource);

        Assert.True(ConnectedB2bPricingResolver.TryResolve(
            exposure,
            share: null,
            CatalogSharingMode.AllEligible,
            new ConnectedB2bPricingPolicy(5m, 10m, 20m, null),
            out var customerDefaultPrice,
            out var customerDefaultSource));
        Assert.Equal(160m, customerDefaultPrice);
        Assert.Equal(ConnectedCustomerPriceSource.CustomerDiscount, customerDefaultSource);

        Assert.True(ConnectedB2bPricingResolver.TryResolve(
            exposure,
            share: null,
            CatalogSharingMode.AllEligible,
            new ConnectedB2bPricingPolicy(5m, 10m, null, null),
            out var orgCategoryPrice,
            out var orgCategorySource));
        Assert.Equal(180m, orgCategoryPrice);
        Assert.Equal(ConnectedCustomerPriceSource.OrganizationCategory, orgCategorySource);

        Assert.True(ConnectedB2bPricingResolver.TryResolve(
            exposure,
            share: null,
            CatalogSharingMode.AllEligible,
            new ConnectedB2bPricingPolicy(5m, null, null, null),
            out var orgDefaultPrice,
            out var orgDefaultSource));
        Assert.Equal(190m, orgDefaultPrice);
        Assert.Equal(ConnectedCustomerPriceSource.OrganizationDefault, orgDefaultSource);
    }

    [Fact]
    public void Payment_timing_effective_policy_is_intersection_when_customer_override_enabled()
    {
        var supplier = PosOrganizationId.From(Guid.NewGuid());
        var settings = OrganizationConnectedCommerceSettings.CreateDefault(supplier, Now);
        settings.ConfigurePaymentTiming(
            allowPayBeforeFulfillment: true,
            allowPayOnDeliveryOrReceipt: false,
            allowSupplierCredit: true,
            ConnectedPoPaymentTiming.PayBeforeFulfillment,
            Now);
        var relationship = ConnectedSupplierRelationship.Request(
            PosOrganizationId.From(Guid.NewGuid()),
            supplier,
            Now);
        relationship.Approve(Now);
        relationship.ConfigurePaymentTimingOverrides(
            useOrganizationDefaults: false,
            allowPayBeforeFulfillment: true,
            allowPayOnDeliveryOrReceipt: false,
            allowSupplierCredit: false,
            customerDefaultPaymentTiming: ConnectedPoPaymentTiming.PayBeforeFulfillment,
            settings,
            Now);

        var effective = ConnectedPoPaymentTimingResolver.Resolve(settings, relationship);

        Assert.True(effective.AllowPayBeforeFulfillment);
        Assert.False(effective.AllowPayOnDeliveryOrReceipt);
        Assert.False(effective.AllowSupplierCredit);
        Assert.Equal(ConnectedPoPaymentTiming.PayBeforeFulfillment, effective.DefaultPaymentTiming);
    }

    private static SupplierProductExposure CreateExposure(decimal orderPrice) =>
        SupplierProductExposure.Expose(
            PosOrganizationId.From(Guid.NewGuid()),
            CatalogProductId.From(Guid.NewGuid()),
            "Product",
            "EA",
            orderPrice,
            Now);
}
