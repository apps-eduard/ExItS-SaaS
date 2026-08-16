using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.UnitTests.Catalog;

public sealed class ConnectedBuyerAvailabilityBulkPricingTests
{
    [Fact]
    public void Set_from_retail_copies_current_selling_price()
    {
        Assert.True(ConnectedBuyerAvailabilityBulkPricing.TryComputeDefaultPoPrice(
            ConnectedBuyerAvailabilityPricingMode.SetFromRetail, 65.13m, null, null, null,
            out var price, out _));
        Assert.Equal(SaleMoney.RoundMoney(65.13m), price);
    }

    [Fact]
    public void Multi_product_match_retail_uses_each_products_own_selling_price()
    {
        Assert.True(ConnectedBuyerAvailabilityBulkPricing.TryComputeDefaultPoPrice(
            ConnectedBuyerAvailabilityPricingMode.SetFromRetail, 200m, null, null, null,
            out var apple, out _));
        Assert.True(ConnectedBuyerAvailabilityBulkPricing.TryComputeDefaultPoPrice(
            ConnectedBuyerAvailabilityPricingMode.SetFromRetail, 80m, null, null, null,
            out var banana, out _));

        Assert.Equal(200m, apple);
        Assert.Equal(80m, banana);
    }

    [Fact]
    public void Discount_from_retail_applies_percent_per_product()
    {
        Assert.True(ConnectedBuyerAvailabilityBulkPricing.TryComputeDefaultPoPrice(
            ConnectedBuyerAvailabilityPricingMode.DiscountFromRetailPercent, 100m, 5m, null, null,
            out var price, out _));
        Assert.Equal(95m, price);
    }

    [Fact]
    public void Multi_product_percent_discount_calculates_independently_from_each_retail()
    {
        Assert.True(ConnectedBuyerAvailabilityBulkPricing.TryComputeDefaultPoPrice(
            ConnectedBuyerAvailabilityPricingMode.DiscountFromRetailPercent, 200m, 10m, null, null,
            out var apple, out _));
        Assert.True(ConnectedBuyerAvailabilityBulkPricing.TryComputeDefaultPoPrice(
            ConnectedBuyerAvailabilityPricingMode.DiscountFromRetailPercent, 80m, 10m, null, null,
            out var banana, out _));

        Assert.Equal(180m, apple);
        Assert.Equal(72m, banana);
    }

    [Fact]
    public void Adjust_from_retail_applies_fixed_amount()
    {
        Assert.True(ConnectedBuyerAvailabilityBulkPricing.TryComputeDefaultPoPrice(
            ConnectedBuyerAvailabilityPricingMode.AdjustFromRetailAmount, 60m, null, -5m, null,
            out var price, out _));
        Assert.Equal(55m, price);
    }

    [Fact]
    public void Multi_product_adjustment_calculates_independently_from_each_retail()
    {
        Assert.True(ConnectedBuyerAvailabilityBulkPricing.TryComputeDefaultPoPrice(
            ConnectedBuyerAvailabilityPricingMode.AdjustFromRetailAmount, 200m, null, -10m, null,
            out var apple, out _));
        Assert.True(ConnectedBuyerAvailabilityBulkPricing.TryComputeDefaultPoPrice(
            ConnectedBuyerAvailabilityPricingMode.AdjustFromRetailAmount, 80m, null, -10m, null,
            out var banana, out _));

        Assert.Equal(190m, apple);
        Assert.Equal(70m, banana);
    }

    [Fact]
    public void Fixed_price_sets_exact_value()
    {
        Assert.True(ConnectedBuyerAvailabilityBulkPricing.TryComputeDefaultPoPrice(
            ConnectedBuyerAvailabilityPricingMode.FixedPrice, 60m, null, null, 42.5m,
            out var price, out _));
        Assert.Equal(42.5m, price);
    }

    [Fact]
    public void Multi_product_one_price_for_all_sets_identical_value()
    {
        Assert.True(ConnectedBuyerAvailabilityBulkPricing.TryComputeDefaultPoPrice(
            ConnectedBuyerAvailabilityPricingMode.FixedPrice, 200m, null, null, 100m,
            out var apple, out _));
        Assert.True(ConnectedBuyerAvailabilityBulkPricing.TryComputeDefaultPoPrice(
            ConnectedBuyerAvailabilityPricingMode.FixedPrice, 80m, null, null, 100m,
            out var banana, out _));

        Assert.Equal(100m, apple);
        Assert.Equal(100m, banana);
    }

    [Fact]
    public void Relative_modes_round_with_sale_money_rules()
    {
        Assert.True(ConnectedBuyerAvailabilityBulkPricing.TryComputeDefaultPoPrice(
            ConnectedBuyerAvailabilityPricingMode.DiscountFromRetailPercent, 33.33m, 10m, null, null,
            out var discounted, out _));
        Assert.Equal(SaleMoney.RoundMoney(33.33m * 0.9m), discounted);

        Assert.True(ConnectedBuyerAvailabilityBulkPricing.TryComputeDefaultPoPrice(
            ConnectedBuyerAvailabilityPricingMode.AdjustFromRetailAmount, 33.335m, null, -1.115m, null,
            out var adjusted, out _));
        Assert.Equal(SaleMoney.RoundMoney(SaleMoney.RoundMoney(33.335m) + (-1.115m)), adjusted);
    }

    [Fact]
    public void Rejects_zero_negative_and_invalid_inputs()
    {
        Assert.False(ConnectedBuyerAvailabilityBulkPricing.TryComputeDefaultPoPrice(
            ConnectedBuyerAvailabilityPricingMode.SetFromRetail, 0m, null, null, null, out _, out _));
        Assert.False(ConnectedBuyerAvailabilityBulkPricing.TryComputeDefaultPoPrice(
            ConnectedBuyerAvailabilityPricingMode.DiscountFromRetailPercent, 10m, 100m, null, null, out _, out _));
        Assert.False(ConnectedBuyerAvailabilityBulkPricing.TryComputeDefaultPoPrice(
            ConnectedBuyerAvailabilityPricingMode.AdjustFromRetailAmount, 10m, null, -10m, null, out _, out _));
        Assert.False(ConnectedBuyerAvailabilityBulkPricing.TryComputeDefaultPoPrice(
            ConnectedBuyerAvailabilityPricingMode.FixedPrice, 10m, null, null, 0m, out _, out _));
        Assert.False(ConnectedBuyerAvailabilityBulkPricing.TryComputeDefaultPoPrice(
            ConnectedBuyerAvailabilityPricingMode.DiscountFromRetailPercent, 10m, 101m, null, null, out _, out _));
        Assert.False(ConnectedBuyerAvailabilityBulkPricing.TryComputeDefaultPoPrice(
            ConnectedBuyerAvailabilityPricingMode.FixedPrice, 10m, null, null, -5m, out _, out _));
    }
}
