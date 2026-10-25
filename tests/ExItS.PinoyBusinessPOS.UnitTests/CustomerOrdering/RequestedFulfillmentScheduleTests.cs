using ExItS.PinoyBusinessPOS.Application.CustomerOrdering;
using ExItS.PinoyBusinessPOS.Domain.CustomerOrdering;

namespace ExItS.PinoyBusinessPOS.UnitTests.CustomerOrdering;

public sealed class RequestedFulfillmentScheduleTests
{
    private static readonly IReadOnlyList<CustomerOrderBranchHoursDaySnapshot> WeekdayNineToFive =
    [
        new("Monday", false, false, "09:00", "17:00"),
        new("Tuesday", false, false, "09:00", "17:00"),
        new("Wednesday", false, false, "09:00", "17:00"),
        new("Thursday", false, false, "09:00", "17:00"),
        new("Friday", false, false, "09:00", "17:00"),
        new("Saturday", true, false, null, null),
        new("Sunday", true, false, null, null),
    ];

    private static readonly IReadOnlyList<CustomerOrderBranchHoursDaySnapshot> Overnight =
    [
        new("Friday", false, false, "22:00", "02:00"),
        new("Saturday", false, false, "22:00", "02:00"),
        new("Sunday", true, false, null, null),
        new("Monday", true, false, null, null),
        new("Tuesday", true, false, null, null),
        new("Wednesday", true, false, null, null),
        new("Thursday", true, false, null, null),
    ];

    [Fact]
    public void Rejects_time_before_opening()
    {
        var result = RequestedFulfillmentSchedule.Resolve(
            CustomerOrderFulfillmentType.Pickup,
            "2026-10-26",
            "08:30",
            "UTC",
            new DateTimeOffset(2026, 10, 25, 12, 0, 0, TimeSpan.Zero),
            WeekdayNineToFive);

        Assert.False(result.IsSuccess);
        Assert.Contains("after this store opens", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Rejects_time_after_closing()
    {
        var result = RequestedFulfillmentSchedule.Resolve(
            CustomerOrderFulfillmentType.Pickup,
            "2026-10-26",
            "17:30",
            "UTC",
            new DateTimeOffset(2026, 10, 25, 12, 0, 0, TimeSpan.Zero),
            WeekdayNineToFive);

        Assert.False(result.IsSuccess);
        Assert.Contains("before this store closes", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Accepts_time_inside_hours()
    {
        var result = RequestedFulfillmentSchedule.Resolve(
            CustomerOrderFulfillmentType.Pickup,
            "2026-10-26",
            "10:15",
            "UTC",
            new DateTimeOffset(2026, 10, 25, 12, 0, 0, TimeSpan.Zero),
            WeekdayNineToFive);

        Assert.True(result.IsSuccess);
        Assert.Equal("2026-10-26 10:15", result.Value.Local);
    }

    [Fact]
    public void Accepts_overnight_window_late_and_early()
    {
        var late = RequestedFulfillmentSchedule.Resolve(
            CustomerOrderFulfillmentType.Delivery,
            "2026-10-30",
            "23:00",
            "UTC",
            new DateTimeOffset(2026, 10, 29, 12, 0, 0, TimeSpan.Zero),
            Overnight);
        var early = RequestedFulfillmentSchedule.Resolve(
            CustomerOrderFulfillmentType.Delivery,
            "2026-10-31",
            "01:00",
            "UTC",
            new DateTimeOffset(2026, 10, 29, 12, 0, 0, TimeSpan.Zero),
            Overnight);

        Assert.True(late.IsSuccess);
        Assert.True(early.IsSuccess);
    }

    [Fact]
    public void Rejects_overnight_gap()
    {
        var result = RequestedFulfillmentSchedule.Resolve(
            CustomerOrderFulfillmentType.Pickup,
            "2026-10-30",
            "12:00",
            "UTC",
            new DateTimeOffset(2026, 10, 29, 12, 0, 0, TimeSpan.Zero),
            Overnight);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Enforces_minimum_lead_time()
    {
        var now = new DateTimeOffset(2026, 10, 26, 9, 0, 0, TimeSpan.Zero);
        var result = RequestedFulfillmentSchedule.Resolve(
            CustomerOrderFulfillmentType.Pickup,
            "2026-10-26",
            "09:30",
            "UTC",
            now,
            WeekdayNineToFive,
            minPreparationLeadMinutes: 60);

        Assert.False(result.IsSuccess);
        Assert.Contains("60 minutes", result.ErrorMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void Enforces_maximum_advance_days()
    {
        var now = new DateTimeOffset(2026, 10, 26, 9, 0, 0, TimeSpan.Zero);
        var result = RequestedFulfillmentSchedule.Resolve(
            CustomerOrderFulfillmentType.Delivery,
            "2026-11-30",
            "10:00",
            "UTC",
            now,
            WeekdayNineToFive,
            maxAdvanceScheduleDays: 7);

        Assert.False(result.IsSuccess);
        Assert.Contains("7 days", result.ErrorMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void Rejects_scheduled_time_when_operating_hours_missing()
    {
        var result = RequestedFulfillmentSchedule.Resolve(
            CustomerOrderFulfillmentType.Pickup,
            "2026-10-26",
            "10:00",
            "UTC",
            new DateTimeOffset(2026, 10, 25, 12, 0, 0, TimeSpan.Zero),
            operatingHours: null);

        Assert.False(result.IsSuccess);
        Assert.Contains("operating hours", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }
}
