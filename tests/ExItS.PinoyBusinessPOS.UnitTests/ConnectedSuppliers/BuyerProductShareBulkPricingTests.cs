using ExItS.PinoyBusinessPOS.Application.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.UnitTests.ConnectedSuppliers;

public sealed class BuyerProductShareBulkPricingTests
{
    [Fact]
    public void UseDefault_clears_buyer_specific_price()
    {
        Assert.True(BuyerProductShareBulkPricing.TryComputeBuyerPrice(
            BulkBuyerPricingMode.UseDefault, 62m, null, null, null, out var price, out var error));
        Assert.Null(price);
        Assert.Null(error);
    }

    [Fact]
    public void DiscountPercent_calculates_from_default_po_not_retail()
    {
        // Default PO 62; retail would be irrelevant even if 200.
        Assert.True(BuyerProductShareBulkPricing.TryComputeBuyerPrice(
            BulkBuyerPricingMode.DiscountPercent, 62m, 5m, null, null, out var price, out _));
        Assert.Equal(SaleMoney.RoundMoney(62m * 0.95m), price);
    }

    [Fact]
    public void AdjustAmount_calculates_from_default_po()
    {
        Assert.True(BuyerProductShareBulkPricing.TryComputeBuyerPrice(
            BulkBuyerPricingMode.AdjustAmount, 62m, null, -5m, null, out var price, out _));
        Assert.Equal(57m, price);
    }

    [Fact]
    public void FixedPrice_rejects_zero_and_negative()
    {
        Assert.False(BuyerProductShareBulkPricing.TryComputeBuyerPrice(
            BulkBuyerPricingMode.FixedPrice, 62m, null, null, 0m, out _, out var error));
        Assert.Contains("greater than zero", error, StringComparison.OrdinalIgnoreCase);

        Assert.False(BuyerProductShareBulkPricing.TryComputeBuyerPrice(
            BulkBuyerPricingMode.FixedPrice, 62m, null, null, -1m, out _, out _));
    }

    [Fact]
    public void Discount_that_reaches_zero_is_rejected()
    {
        Assert.False(BuyerProductShareBulkPricing.TryComputeBuyerPrice(
            BulkBuyerPricingMode.DiscountPercent, 10m, 100m, null, null, out _, out var error));
        Assert.Contains("invalid", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Missing_default_po_is_rejected()
    {
        Assert.False(BuyerProductShareBulkPricing.TryComputeBuyerPrice(
            BulkBuyerPricingMode.DiscountPercent, 0m, 5m, null, null, out _, out var error));
        Assert.Contains("Default PO", error, StringComparison.OrdinalIgnoreCase);
    }
}
