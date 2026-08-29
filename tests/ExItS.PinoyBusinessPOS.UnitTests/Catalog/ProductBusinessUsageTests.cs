using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.UnitTests.Catalog;

public sealed class ProductBusinessUsageTests
{
    [Theory]
    [InlineData(ProductBusinessUsage.Resale, true, false, false)]
    [InlineData(ProductBusinessUsage.Ingredient, false, true, false)]
    [InlineData(ProductBusinessUsage.InternalUse, false, false, false)]
    public void ToCapabilities_maps_expected_flags(
        ProductBusinessUsage usage,
        bool canBeSold,
        bool ingredient,
        bool produced)
    {
        var caps = ProductBusinessUsages.ToCapabilities(usage);
        Assert.Equal(canBeSold, caps.CanBeSold);
        Assert.Equal(ingredient, caps.CanBeUsedAsIngredient);
        Assert.Equal(produced, caps.IsProduced);
        Assert.True(caps.CanBePurchased);
        Assert.Equal(usage, ProductBusinessUsages.Classify(caps));
    }

    [Fact]
    public void Legacy_BuyAndSell_classifies_as_Resale()
    {
        Assert.Equal(
            ProductBusinessUsage.Resale,
            ProductBusinessUsages.Classify(ProductUsageCapabilities.BuyAndSell));
    }

    [Fact]
    public void MadeProduct_classifies_as_Resale_for_sell_floor()
    {
        Assert.Equal(
            ProductBusinessUsage.Resale,
            ProductBusinessUsages.Classify(ProductUsageCapabilities.MadeProduct));
    }

    [Fact]
    public void InternalUse_preset_is_valid()
    {
        var caps = ProductUsageCapabilities.FromPreset(ProductUsageCapabilities.InternalUseCode);
        Assert.False(caps.CanBeSold);
        Assert.False(caps.CanBeUsedAsIngredient);
        Assert.True(caps.CanBePurchased);
    }

    [Fact]
    public void Parse_rejects_unknown()
    {
        var ex = Assert.Throws<DomainException>(() => ProductBusinessUsages.ParseRequired("NotAUsage"));
        Assert.Equal(DomainErrorCodes.InvalidProductUsage, ex.ErrorCode);
    }

    [Theory]
    [InlineData("Resale")]
    [InlineData("Ingredient")]
    [InlineData("InternalUse")]
    public void Parse_accepts_stable_codes(string code)
    {
        Assert.True(ProductBusinessUsages.TryParse(code, out var usage));
        Assert.Equal(code, ProductBusinessUsages.ToCode(usage));
    }
}
