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
    public void Discount_from_retail_applies_percent_per_product()
    {
        Assert.True(ConnectedBuyerAvailabilityBulkPricing.TryComputeDefaultPoPrice(
            ConnectedBuyerAvailabilityPricingMode.DiscountFromRetailPercent, 100m, 5m, null, null,
            out var price, out _));
        Assert.Equal(95m, price);
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
    public void Fixed_price_sets_exact_value()
    {
        Assert.True(ConnectedBuyerAvailabilityBulkPricing.TryComputeDefaultPoPrice(
            ConnectedBuyerAvailabilityPricingMode.FixedPrice, 60m, null, null, 42.5m,
            out var price, out _));
        Assert.Equal(42.5m, price);
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
    }
}
