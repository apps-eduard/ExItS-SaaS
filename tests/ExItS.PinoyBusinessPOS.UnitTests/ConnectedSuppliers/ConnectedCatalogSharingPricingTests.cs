using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.UnitTests.ConnectedSuppliers;

public sealed class ConnectedCatalogSharingPricingTests
{
    private static SupplierProductExposure Exposure(decimal orderPrice = 100m) =>
        SupplierProductExposure.Expose(
            PosOrganizationId.From(Guid.NewGuid()),
            CatalogProductId.From(Guid.NewGuid()),
            "Widget",
            "EA",
            orderPrice,
            DateTimeOffset.UtcNow);

    [Fact]
    public void SelectedOnly_requires_explicit_share()
    {
        var exposure = Exposure();
        Assert.False(ConnectedPoPricing.TryResolveEffectivePrice(
            exposure,
            share: null,
            CatalogSharingMode.SelectedOnly,
            null,
            null,
            out _,
            out _));
    }

    [Fact]
    public void AllEligible_inherits_share_without_row()
    {
        var exposure = Exposure(100m);
        Assert.True(ConnectedPoPricing.TryResolveEffectivePrice(
            exposure,
            share: null,
            CatalogSharingMode.AllEligible,
            customerDiscountPercent: 10m,
            sellingPrice: 100m,
            out var price,
            out var source));
        Assert.Equal(90m, price);
        Assert.Equal(ConnectedCustomerPriceSource.CustomerDiscount, source);
    }

    [Fact]
    public void AllEligible_exclusion_hides_product()
    {
        var exposure = Exposure();
        var share = ConnectedBuyerProductShare.Share(
            ConnectedSupplierRelationshipId.New(),
            PosOrganizationId.From(Guid.NewGuid()),
            PosOrganizationId.From(Guid.NewGuid()),
            exposure.ProductId,
            DateTimeOffset.UtcNow);
        share.Unshare(DateTimeOffset.UtcNow);

        Assert.False(ConnectedPoPricing.TryResolveEffectivePrice(
            exposure,
            share,
            CatalogSharingMode.AllEligible,
            null,
            100m,
            out _,
            out _));
    }

    [Fact]
    public void Override_beats_customer_discount()
    {
        var exposure = Exposure(100m);
        var share = ConnectedBuyerProductShare.Share(
            ConnectedSupplierRelationshipId.New(),
            PosOrganizationId.From(Guid.NewGuid()),
            PosOrganizationId.From(Guid.NewGuid()),
            exposure.ProductId,
            DateTimeOffset.UtcNow,
            buyerSpecificPoPrice: 82m);

        Assert.True(ConnectedPoPricing.TryResolveEffectivePrice(
            exposure,
            share,
            CatalogSharingMode.AllEligible,
            customerDiscountPercent: 10m,
            sellingPrice: 100m,
            out var price,
            out var source));
        Assert.Equal(82m, price);
        Assert.Equal(ConnectedCustomerPriceSource.ProductOverride, source);
    }

    [Fact]
    public void Selling_price_change_recalculates_discount_not_override()
    {
        var exposure = Exposure(100m);
        Assert.True(ConnectedPoPricing.TryResolveEffectivePrice(
            exposure,
            null,
            CatalogSharingMode.AllEligible,
            10m,
            sellingPrice: 110m,
            out var discounted,
            out _));
        Assert.Equal(99m, discounted);

        var share = ConnectedBuyerProductShare.Share(
            ConnectedSupplierRelationshipId.New(),
            PosOrganizationId.From(Guid.NewGuid()),
            PosOrganizationId.From(Guid.NewGuid()),
            exposure.ProductId,
            DateTimeOffset.UtcNow,
            buyerSpecificPoPrice: 82m);
        Assert.True(ConnectedPoPricing.TryResolveEffectivePrice(
            exposure,
            share,
            CatalogSharingMode.AllEligible,
            10m,
            sellingPrice: 110m,
            out var overridden,
            out _));
        Assert.Equal(82m, overridden);
    }

    [Fact]
    public void ConfigureCatalogSharing_rejects_invalid_discount()
    {
        var relationship = ConnectedSupplierRelationship.Request(
            PosOrganizationId.From(Guid.NewGuid()),
            PosOrganizationId.From(Guid.NewGuid()),
            DateTimeOffset.UtcNow);
        relationship.Approve(DateTimeOffset.UtcNow);

        Assert.Throws<DomainException>(() =>
            relationship.ConfigureCatalogSharing(CatalogSharingMode.AllEligible, 150m, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Legacy_selected_only_default_on_new_request()
    {
        var relationship = ConnectedSupplierRelationship.Request(
            PosOrganizationId.From(Guid.NewGuid()),
            PosOrganizationId.From(Guid.NewGuid()),
            DateTimeOffset.UtcNow);
        Assert.Equal(CatalogSharingMode.SelectedOnly, relationship.CatalogSharingMode);
        Assert.Null(relationship.CustomerDiscountPercent);
    }
}
