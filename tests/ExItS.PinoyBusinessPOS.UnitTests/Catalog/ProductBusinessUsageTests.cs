using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.UnitTests.Catalog;

public sealed class ProductBusinessUsageTests
{
    [Theory]
    [InlineData(ProductBusinessUsage.Resale, true, false, false)]
    [InlineData(ProductBusinessUsage.Ingredient, false, true, false)]
    [InlineData(ProductBusinessUsage.InternalUse, false, false, false)]
    [InlineData(ProductBusinessUsage.ProducedItem, true, false, true)]
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
        Assert.Equal(usage == ProductBusinessUsage.ProducedItem ? false : true, caps.CanBePurchased);
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
    public void MadeProduct_classifies_as_ProducedItem()
    {
        Assert.Equal(
            ProductBusinessUsage.ProducedItem,
            ProductBusinessUsages.Classify(ProductUsageCapabilities.MadeProduct));
    }

    [Fact]
    public void ProducedItem_maps_to_MadeProduct_capabilities()
    {
        var caps = ProductBusinessUsages.ToCapabilities(ProductBusinessUsage.ProducedItem);
        Assert.True(caps.IsProduced);
        Assert.True(caps.CanBeSold);
        Assert.False(caps.CanBePurchased);
        Assert.Equal(ProductUsageCapabilities.MadeProductCode, caps.PresetCode);
    }

    [Fact]
    public void FromPreset_accepts_ProducedItem_alias()
    {
        var caps = ProductUsageCapabilities.FromPreset(ProductBusinessUsages.ProducedItem);
        Assert.True(caps.IsProduced);
        Assert.Equal(ProductUsageCapabilities.MadeProductCode, caps.PresetCode);
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
    [InlineData("ProducedItem")]
    [InlineData("MadeProduct")]
    public void Parse_accepts_stable_codes(string code)
    {
        Assert.True(ProductBusinessUsages.TryParse(code, out var usage));
        if (code is "ProducedItem" or "MadeProduct")
        {
            Assert.Equal(ProductBusinessUsage.ProducedItem, usage);
            Assert.Equal(ProductBusinessUsages.ProducedItem, ProductBusinessUsages.ToCode(usage));
        }
        else
        {
            Assert.Equal(code, ProductBusinessUsages.ToCode(usage));
        }
    }
}
