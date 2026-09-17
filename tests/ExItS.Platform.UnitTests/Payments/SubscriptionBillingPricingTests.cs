using ExItS.Platform.Domain.Payments;
using ExItS.Platform.Domain.Subscriptions;

namespace ExItS.Platform.UnitTests.Payments;

public sealed class SubscriptionBillingPricingTests
{
    [Fact]
    public void Quote_example_monthly_base_1000_matches_task_math()
    {
        var monthly = SubscriptionBillingPricing.Quote("demo", 1000m, 0m, "PHP", BillingCycle.Monthly);
        Assert.Equal(1000m, monthly.FinalAmount);
        Assert.Equal(0m, monthly.DiscountAmount);

        var quarterly = SubscriptionBillingPricing.Quote("demo", 1000m, 0m, "PHP", BillingCycle.Quarterly);
        Assert.Equal(3000m, quarterly.BaseAmount);
        Assert.Equal(5m, quarterly.DiscountPercent);
        Assert.Equal(150m, quarterly.DiscountAmount);
        Assert.Equal(2850m, quarterly.FinalAmount);
        Assert.Equal(950m, quarterly.EquivalentMonthlyAmount);

        var six = SubscriptionBillingPricing.Quote("demo", 1000m, 0m, "PHP", BillingCycle.SixMonths);
        Assert.Equal(6000m, six.BaseAmount);
        Assert.Equal(10m, six.DiscountPercent);
        Assert.Equal(600m, six.DiscountAmount);
        Assert.Equal(5400m, six.FinalAmount);
        Assert.Equal(900m, six.EquivalentMonthlyAmount);

        var annualFallback = SubscriptionBillingPricing.Quote("demo", 1000m, 0m, "PHP", BillingCycle.Annual);
        Assert.Equal(12000m, annualFallback.BaseAmount);
        Assert.Equal(15m, annualFallback.DiscountPercent);
        Assert.Equal(1800m, annualFallback.DiscountAmount);
        Assert.Equal(10200m, annualFallback.FinalAmount);
        Assert.Equal(850m, annualFallback.EquivalentMonthlyAmount);
    }

    [Fact]
    public void Quote_preserves_canonical_annual_list_price_when_set()
    {
        // Starter catalog: 299 monthly / 2990 annual
        var quote = SubscriptionBillingPricing.Quote("starter", 299m, 2990m, "PHP", BillingCycle.Annual);
        Assert.Equal(2990m, quote.FinalAmount);
        Assert.Equal(3588m, quote.BaseAmount);
        Assert.Equal(598m, quote.DiscountAmount);
        Assert.True(quote.DiscountPercent > 0m);
    }

    [Theory]
    [InlineData(BillingCycle.Monthly, 1)]
    [InlineData(BillingCycle.Quarterly, 3)]
    [InlineData(BillingCycle.SixMonths, 6)]
    [InlineData(BillingCycle.Annual, 12)]
    public void PeriodMonths_are_calendar_aligned(BillingCycle cycle, int months)
    {
        Assert.Equal(months, SubscriptionBillingPricing.PeriodMonths(cycle));
    }
}
