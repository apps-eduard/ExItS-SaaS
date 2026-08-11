using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.GlobalCatalog;

namespace ExItS.Platform.UnitTests.GlobalCatalog;

public sealed class ProductSellingModeTests
{
    private const string ValidEan13 = "4800010000016";
    private static readonly DateTimeOffset T0 = new(2026, 8, 5, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void GlobalProduct_Create_defaults_SellingMode_PerItem()
    {
        var category = GlobalCategory.Create("Beverages", T0);
        var product = GlobalProduct.Create(
            "Coke",
            ProductUnit.Bottle,
            "SKU1",
            ValidEan13,
            "Brand",
            category.Id,
            T0,
            1m,
            2m);

        Assert.Equal(ProductSellingMode.PerItem, product.SellingMode);
    }

    [Fact]
    public void GlobalProduct_Create_PerItem_with_Bottle_is_valid()
    {
        var category = GlobalCategory.Create("Beverages", T0);
        var product = GlobalProduct.Create(
            "Coke",
            ProductUnit.Bottle,
            "SKU1",
            ValidEan13,
            "Brand",
            category.Id,
            T0,
            1m,
            2m,
            sellingMode: ProductSellingMode.PerItem);

        Assert.Equal(ProductSellingMode.PerItem, product.SellingMode);
        Assert.Equal(ProductUnit.Bottle, product.Unit);
    }

    [Fact]
    public void GlobalProduct_Create_ByWeight_with_Kilogram_allows_blank_barcode()
    {
        var category = GlobalCategory.Create("Produce", T0);
        var product = GlobalProduct.Create(
            "Tomato",
            ProductUnit.Kilogram,
            "VEG-TOMATO",
            null,
            "Fresh",
            category.Id,
            T0,
            80m,
            120m,
            sellingMode: ProductSellingMode.ByWeight);

        Assert.Equal(ProductSellingMode.ByWeight, product.SellingMode);
        Assert.Equal(ProductUnit.Kilogram, product.Unit);
        Assert.Null(product.Barcode);
    }

    [Fact]
    public void GlobalProduct_Create_ByWeight_with_Bottle_throws_InvalidGlobalProductSellingModeUnit()
    {
        var category = GlobalCategory.Create("Produce", T0);
        var ex = Assert.Throws<DomainException>(() =>
            GlobalProduct.Create(
                "Tomato",
                ProductUnit.Bottle,
                "VEG-TOMATO",
                null,
                "Fresh",
                category.Id,
                T0,
                80m,
                120m,
                sellingMode: ProductSellingMode.ByWeight));

        Assert.Equal(DomainErrorCodes.InvalidGlobalProductSellingModeUnit, ex.ErrorCode);
    }

    [Fact]
    public void GlobalProduct_Update_to_ByWeight_Kilogram_works_and_incompatible_unit_fails()
    {
        var category = GlobalCategory.Create("Produce", T0);
        var product = GlobalProduct.Create(
            "Tomato",
            ProductUnit.Piece,
            "VEG-TOMATO",
            ValidEan13,
            "Fresh",
            category.Id,
            T0,
            80m,
            120m);

        product.Update(
            "Tomato",
            ProductUnit.Kilogram,
            "VEG-TOMATO",
            null,
            "Fresh",
            category.Id,
            T0.AddMinutes(1),
            80m,
            120m,
            sellingMode: ProductSellingMode.ByWeight);

        Assert.Equal(ProductSellingMode.ByWeight, product.SellingMode);
        Assert.Equal(ProductUnit.Kilogram, product.Unit);
        Assert.Null(product.Barcode);

        var ex = Assert.Throws<DomainException>(() =>
            product.Update(
                "Tomato",
                ProductUnit.Bottle,
                "VEG-TOMATO",
                null,
                "Fresh",
                category.Id,
                T0.AddMinutes(2),
                80m,
                120m,
                sellingMode: ProductSellingMode.ByWeight));

        Assert.Equal(DomainErrorCodes.InvalidGlobalProductSellingModeUnit, ex.ErrorCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ProductSellingModes_Parse_blank_defaults_to_PerItem(string? text)
    {
        Assert.Equal(ProductSellingMode.PerItem, ProductSellingModes.Parse(text));
    }

    [Theory]
    [InlineData("NotAMode")]
    [InlineData("Weight")]
    [InlineData("per-item")]
    public void ProductSellingModes_Parse_invalid_throws_InvalidGlobalProductSellingMode(string text)
    {
        var ex = Assert.Throws<DomainException>(() => ProductSellingModes.Parse(text));
        Assert.Equal(DomainErrorCodes.InvalidGlobalProductSellingMode, ex.ErrorCode);
    }
}
