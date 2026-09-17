using ExItS.Platform.Application.Payments;
using ExItS.Platform.Domain.Subscriptions;

namespace ExItS.Platform.UnitTests.Payments;

public sealed class SubscriptionBillingPeriodsTests
{
    [Fact]
    public void ComputePaidPeriod_uses_calendar_months_and_years()
    {
        var start = new DateTimeOffset(2026, 1, 31, 12, 0, 0, TimeSpan.Zero);

        var monthly = SubscriptionBillingPeriods.ComputePaidPeriod(start, BillingCycle.Monthly);
        Assert.Equal(new DateTimeOffset(2026, 2, 28, 12, 0, 0, TimeSpan.Zero), monthly.End);

        var quarterly = SubscriptionBillingPeriods.ComputePaidPeriod(start, BillingCycle.Quarterly);
        Assert.Equal(new DateTimeOffset(2026, 4, 30, 12, 0, 0, TimeSpan.Zero), quarterly.End);

        var six = SubscriptionBillingPeriods.ComputePaidPeriod(start, BillingCycle.SixMonths);
        Assert.Equal(new DateTimeOffset(2026, 7, 31, 12, 0, 0, TimeSpan.Zero), six.End);

        var annual = SubscriptionBillingPeriods.ComputePaidPeriod(start, BillingCycle.Annual);
        Assert.Equal(new DateTimeOffset(2027, 1, 31, 12, 0, 0, TimeSpan.Zero), annual.End);
    }
}
