using ExItS.PinoyBusinessPOS.Application.CustomerOrdering;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.CustomerOrdering;

namespace ExItS.PinoyBusinessPOS.UnitTests.CustomerOrdering;

public sealed class RequestedPickupScheduleTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 8, 4, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Empty_schedule_means_as_soon_as_possible()
    {
        var result = RequestedPickupSchedule.Resolve(
            CustomerOrderFulfillmentType.Pickup,
            null,
            null,
            "Asia/Manila",
            Now);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.Local);
        Assert.Null(result.Value.AtUtc);
    }

    [Fact]
    public void Branch_timezone_is_used_instead_of_a_browser_offset()
    {
        var result = RequestedPickupSchedule.Resolve(
            CustomerOrderFulfillmentType.Pickup,
            "2026-10-08",
            "14:30",
            "Asia/Manila",
            Now);

        Assert.True(result.IsSuccess);
        Assert.Equal("2026-10-08 14:30", result.Value.Local);
        Assert.Equal(new DateTimeOffset(2026, 10, 8, 6, 30, 0, TimeSpan.Zero), result.Value.AtUtc);
    }

    [Fact]
    public void Past_branch_local_time_is_rejected()
    {
        var result = RequestedPickupSchedule.Resolve(
            CustomerOrderFulfillmentType.Pickup,
            "2026-10-08",
            "11:00",
            "Asia/Manila",
            Now);

        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrorCodes.InvalidCustomerOrderPickupRequest, result.ErrorCode);
    }

    [Fact]
    public void Time_after_same_day_closing_is_rejected()
    {
        var hours = new[]
        {
            new CustomerOrderBranchHoursDaySnapshot("Thursday", false, false, "08:00", "13:00")
        };
        var late = RequestedPickupSchedule.Resolve(
            CustomerOrderFulfillmentType.Pickup,
            "2026-10-08",
            "14:30",
            "Asia/Manila",
            Now,
            hours);
        Assert.False(late.IsSuccess);

        var during = RequestedPickupSchedule.Resolve(
            CustomerOrderFulfillmentType.Pickup,
            "2026-10-08",
            "12:30",
            "Asia/Manila",
            Now,
            hours);
        Assert.True(during.IsSuccess);
    }
}
