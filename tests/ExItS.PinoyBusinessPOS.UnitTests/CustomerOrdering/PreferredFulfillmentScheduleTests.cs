using ExItS.PinoyBusinessPOS.Application.CustomerOrdering;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.CustomerOrdering;

namespace ExItS.PinoyBusinessPOS.UnitTests.CustomerOrdering;

public sealed class PreferredFulfillmentScheduleTests
{
    // Thursday 2026-10-08 12:00 Asia/Manila = 04:00 UTC
    private static readonly DateTimeOffset NoonManilaUtc = new(2026, 10, 8, 4, 0, 0, TimeSpan.Zero);

    private static readonly IReadOnlyList<CustomerOrderBranchHoursDaySnapshot> DayHours =
    [
        new("Thursday", false, false, "08:00", "17:00"),
        new("Friday", false, false, "08:00", "22:00"),
        new("Saturday", false, false, "18:00", "22:00"),
        new("Sunday", true, false, null, null),
        new("Monday", false, false, "22:00", "02:00"),
        new("Tuesday", false, false, "08:00", "17:00"),
    ];

    [Fact]
    public void Morning_and_afternoon_available_when_store_closes_at_5pm()
    {
        // 09:00 Asia/Manila Thursday
        var morningUtc = new DateTimeOffset(2026, 10, 8, 1, 0, 0, TimeSpan.Zero);
        var morning = PreferredFulfillmentSchedule.Resolve(
            CustomerOrderFulfillmentType.Pickup,
            "2026-10-08",
            "Morning",
            "Asia/Manila",
            morningUtc,
            DayHours);
        var afternoon = PreferredFulfillmentSchedule.Resolve(
            CustomerOrderFulfillmentType.Pickup,
            "2026-10-08",
            "Afternoon",
            "Asia/Manila",
            morningUtc,
            DayHours);
        var evening = PreferredFulfillmentSchedule.Resolve(
            CustomerOrderFulfillmentType.Pickup,
            "2026-10-08",
            "Evening",
            "Asia/Manila",
            morningUtc,
            DayHours);

        Assert.True(morning.IsSuccess);
        Assert.True(afternoon.IsSuccess);
        Assert.False(evening.IsSuccess);
        Assert.Equal(CustomerOrderPreferredFulfillmentPeriod.Morning, morning.Value.Period);
        Assert.True(morning.Value.WindowEndUtc > morning.Value.WindowStartUtc);
    }

    [Fact]
    public void Evening_only_when_store_opens_at_6pm()
    {
        var morning = PreferredFulfillmentSchedule.Resolve(
            CustomerOrderFulfillmentType.Pickup,
            "2026-10-10",
            "Morning",
            "Asia/Manila",
            NoonManilaUtc,
            DayHours);
        var evening = PreferredFulfillmentSchedule.Resolve(
            CustomerOrderFulfillmentType.Pickup,
            "2026-10-10",
            "Evening",
            "Asia/Manila",
            NoonManilaUtc,
            DayHours);

        Assert.False(morning.IsSuccess);
        Assert.True(evening.IsSuccess);
    }

    [Fact]
    public void Past_calendar_day_is_rejected()
    {
        var result = PreferredFulfillmentSchedule.Resolve(
            CustomerOrderFulfillmentType.Pickup,
            "2026-10-07",
            "Morning",
            "Asia/Manila",
            NoonManilaUtc,
            DayHours);

        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrorCodes.InvalidCustomerOrderPickupRequest, result.ErrorCode);
    }

    [Fact]
    public void Closed_day_is_rejected()
    {
        var result = PreferredFulfillmentSchedule.Resolve(
            CustomerOrderFulfillmentType.Pickup,
            "2026-10-11",
            "Morning",
            "Asia/Manila",
            NoonManilaUtc,
            DayHours);

        Assert.False(result.IsSuccess);
        Assert.Contains("closed", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Max_advance_days_is_enforced()
    {
        var result = PreferredFulfillmentSchedule.Resolve(
            CustomerOrderFulfillmentType.Pickup,
            "2026-10-30",
            "Morning",
            "Asia/Manila",
            NoonManilaUtc,
            DayHours,
            minPreparationLeadMinutes: 0,
            maxAdvanceScheduleDays: 7);

        Assert.False(result.IsSuccess);
        Assert.Contains("7", result.ErrorMessage);
    }

    [Fact]
    public void Same_day_morning_is_hidden_after_noon_with_lead_time()
    {
        var result = PreferredFulfillmentSchedule.Resolve(
            CustomerOrderFulfillmentType.Pickup,
            "2026-10-08",
            "Morning",
            "Asia/Manila",
            NoonManilaUtc,
            DayHours,
            minPreparationLeadMinutes: 30);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Overnight_store_offers_evening_on_open_day_and_morning_after_midnight()
    {
        // Monday overnight 22:00–02:00: evening on Monday, morning spill Tuesday.
        var mondayEvening = PreferredFulfillmentSchedule.Resolve(
            CustomerOrderFulfillmentType.Pickup,
            "2026-10-12",
            "Evening",
            "Asia/Manila",
            NoonManilaUtc,
            DayHours);
        var tuesdayMorning = PreferredFulfillmentSchedule.Resolve(
            CustomerOrderFulfillmentType.Pickup,
            "2026-10-13",
            "Morning",
            "Asia/Manila",
            NoonManilaUtc,
            DayHours);

        Assert.True(mondayEvening.IsSuccess);
        Assert.True(tuesdayMorning.IsSuccess);
    }

    [Fact]
    public void Does_not_invent_an_exact_morning_appointment_time()
    {
        var result = PreferredFulfillmentSchedule.Resolve(
            CustomerOrderFulfillmentType.Pickup,
            "2026-10-09",
            "Morning",
            "Asia/Manila",
            NoonManilaUtc,
            DayHours);

        Assert.True(result.IsSuccess);
        Assert.Equal(CustomerOrderPreferredFulfillmentPeriod.Morning, result.Value.Period);
        Assert.Equal(new DateOnly(2026, 10, 9), result.Value.Date);
        // Window start is open∩period (08:00), not a fake 09:00 appointment label.
        Assert.True(result.Value.WindowEndUtc > result.Value.WindowStartUtc);
    }

    [Fact]
    public void AvailablePeriods_lists_open_windows_only()
    {
        var periods = PreferredFulfillmentSchedule.AvailablePeriods(
            new DateOnly(2026, 10, 9),
            "Asia/Manila",
            NoonManilaUtc,
            DayHours);

        Assert.Contains(CustomerOrderPreferredFulfillmentPeriod.Morning, periods);
        Assert.Contains(CustomerOrderPreferredFulfillmentPeriod.Afternoon, periods);
        Assert.Contains(CustomerOrderPreferredFulfillmentPeriod.Evening, periods);
    }
}
