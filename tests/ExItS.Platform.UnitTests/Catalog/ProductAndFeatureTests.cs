using ExItS.Platform.Domain.Catalog;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Products;

namespace ExItS.Platform.UnitTests.Catalog;

public sealed class ProductAndFeatureTests
{
    private static readonly DateTimeOffset T0 = new(2026, 7, 29, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Product_create_activate_deactivate_retire()
    {
        var product = Product.Create(ProductCode.Create("other-product"), "Other Product", T0);
        Assert.Equal(ProductStatus.Active, product.Status);
        product.Deactivate(T0.AddMinutes(1));
        Assert.Equal(ProductStatus.Inactive, product.Status);
        product.Activate(T0.AddMinutes(2));
        product.Retire(T0.AddMinutes(3));
        Assert.Equal(ProductStatus.Retired, product.Status);
        Assert.Throws<DomainException>(() => product.Activate(T0.AddMinutes(4)));
    }

    [Fact]
    public void Product_rejects_blank_name()
    {
        var ex = Assert.Throws<DomainException>(() =>
            Product.Create(ProductCode.Create("other-product"), " ", T0));
        Assert.Equal(DomainErrorCodes.InvalidDisplayName, ex.ErrorCode);
    }

    [Fact]
    public void FeatureDefinition_retire_blocks_assign()
    {
        var feature = FeatureDefinition.Create(
            ProductCode.Create(ProductCode.PinoyBusinessPos),
            FeatureCode.Create(FeatureCode.CustomerCreditCreate),
            "Create Credit",
            FeatureValueType.Boolean,
            T0);
        feature.Retire(T0.AddMinutes(1));
        Assert.Throws<DomainException>(() => feature.EnsureAssignable());
    }

    [Fact]
    public void FeatureCode_rejects_invalid()
    {
        Assert.Throws<DomainException>(() => FeatureCode.Create(""));
        Assert.Throws<DomainException>(() => FeatureCode.Create("Bad_Code"));
    }

    [Fact]
    public void FeatureGrantSpec_rejects_negative_limit()
    {
        var ex = Assert.Throws<DomainException>(() =>
            FeatureGrantSpec.Limit(FeatureCode.Create("max-users"), -1));
        Assert.Equal(DomainErrorCodes.InvalidEntitlementLimit, ex.ErrorCode);
    }
}
