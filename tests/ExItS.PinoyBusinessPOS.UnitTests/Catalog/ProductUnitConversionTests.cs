using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.UnitTests.Catalog;

public sealed class ProductUnitConversionTests
{
    private static readonly PosOrganizationId Org =
        PosOrganizationId.From(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));

    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-08-14T10:00:00Z");

    [Fact]
    public void ToBaseQuantity_converts_ten_bags_of_fifty()
    {
        Assert.Equal(500m, ProductUnitConversion.ToBaseQuantity(10m, 50m));
    }

    [Fact]
    public void ToBaseUnitCost_divides_and_rounds_money_away_from_zero()
    {
        Assert.Equal(45.00m, ProductUnitConversion.ToBaseUnitCost(2250m, 50m));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-0.01)]
    public void Invalid_multiplier_is_rejected(decimal multiplier)
    {
        var qtyEx = Assert.Throws<DomainException>(() =>
            ProductUnitConversion.ToBaseQuantity(1m, multiplier));
        Assert.Equal(DomainErrorCodes.InvalidProductUnitMultiplier, qtyEx.ErrorCode);

        var costEx = Assert.Throws<DomainException>(() =>
            ProductUnitConversion.ToBaseUnitCost(100m, multiplier));
        Assert.Equal(DomainErrorCodes.InvalidProductUnitMultiplier, costEx.ErrorCode);

        var normalizeEx = Assert.Throws<DomainException>(() =>
            CatalogProductUnit.NormalizeMultiplier(multiplier));
        Assert.Equal(DomainErrorCodes.InvalidProductUnitMultiplier, normalizeEx.ErrorCode);
    }

    [Fact]
    public void Sale_line_pack_prices_entered_qty_and_stores_base_quantity()
    {
        var sellingUnitId = ProductUnitId.New();
        var line = SaleLine.Create(
            SaleId.New(),
            Org,
            1,
            new SaleLineDraft(
                CatalogProductId.New(),
                "Rice 5kg pack",
                "RICE-5",
                null,
                UnitOfMeasure.Kilogram,
                UnitPrice: 290m,
                Quantity: 5m,
                SellingModeSnapshot: SellingMode.PerItem,
                SellingUnitId: sellingUnitId,
                SellingUnitNameSnapshot: "5 kg pack",
                EnteredQuantity: 1m,
                MultiplierToBaseSnapshot: 5m));

        Assert.Equal(290m, line.LineTotal);
        Assert.Equal(5m, line.Quantity);
        Assert.Equal(1m, line.EnteredQuantity);
        Assert.Equal(5m, line.MultiplierToBaseSnapshot);
        Assert.Equal(sellingUnitId, line.SellingUnitId);
    }

    [Fact]
    public void Sale_line_custom_meat_quantity_uses_base_when_multiplier_is_one()
    {
        var sellingUnitId = ProductUnitId.New();
        var line = SaleLine.Create(
            SaleId.New(),
            Org,
            1,
            new SaleLineDraft(
                CatalogProductId.New(),
                "Pork belly",
                null,
                null,
                UnitOfMeasure.Kilogram,
                UnitPrice: 380m,
                Quantity: 0.75m,
                SellingModeSnapshot: SellingMode.ByWeight,
                SellingUnitId: sellingUnitId,
                SellingUnitNameSnapshot: "kg",
                EnteredQuantity: 0.75m,
                MultiplierToBaseSnapshot: 1m));

        Assert.Equal(0.75m, line.Quantity);
        Assert.Equal(0.75m, line.EnteredQuantity);
        Assert.Equal(285.00m, line.LineTotal);
    }

    [Fact]
    public void Ingredient_can_also_be_sellable()
    {
        var usage = ProductUsageCapabilities.IngredientAndSellable;
        Assert.True(usage.CanBePurchased);
        Assert.True(usage.CanBeSold);
        Assert.True(usage.CanBeUsedAsIngredient);
        Assert.False(usage.IsProduced);

        var product = CatalogProduct.Create(
            Org,
            "Sugar",
            UnitOfMeasure.Kilogram,
            55m,
            Now,
            usage: usage);

        Assert.True(product.CanBeSold);
        Assert.True(product.CanBeUsedAsIngredient);
        Assert.Equal(ProductUsageCapabilities.IngredientAndSellableCode, product.UsagePreset);
    }

    [Fact]
    public void Simple_one_to_one_product_sale_still_works_without_conversion()
    {
        var product = CatalogProduct.Create(Org, "Sardinas", UnitOfMeasure.Can, 25m, Now);
        Assert.True(product.CanBePurchased);
        Assert.True(product.CanBeSold);
        Assert.False(product.CanBeUsedAsIngredient);
        Assert.False(product.IsProduced);
        Assert.Equal(ProductUsageCapabilities.BuyAndSellCode, product.UsagePreset);

        var line = SaleLine.Create(
            SaleId.New(),
            Org,
            1,
            new SaleLineDraft(
                product.Id,
                product.Name,
                product.Sku,
                product.Barcode,
                product.UnitOfMeasure,
                product.SellingPrice,
                Quantity: 2m));

        Assert.Equal(2m, line.Quantity);
        Assert.Equal(50m, line.LineTotal);
        Assert.Null(line.EnteredQuantity);
        Assert.Null(line.MultiplierToBaseSnapshot);
        Assert.Null(line.SellingUnitId);
    }

    [Fact]
    public void Usage_requires_at_least_one_consume_or_produce_flag()
    {
        var ex = Assert.Throws<DomainException>(() =>
            ProductUsageCapabilities.Create(
                canBePurchased: true,
                canBeSold: false,
                canBeUsedAsIngredient: false,
                isProduced: false));
        Assert.Equal(DomainErrorCodes.InvalidProductUsage, ex.ErrorCode);
    }
}
