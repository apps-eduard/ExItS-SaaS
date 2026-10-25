using ExItS.PinoyBusinessPOS.Application.CustomerOrdering;
using ExItS.PinoyBusinessPOS.Domain.CustomerOrdering;

namespace ExItS.PinoyBusinessPOS.UnitTests.CustomerOrdering;

public sealed class CustomerOrderPlaceBranchGateTests
{
    private static CustomerOrderBranchSnapshot Branch(
        bool enabled = true,
        bool paused = false,
        bool operational = true,
        bool pickupEnabled = true,
        bool pickupReady = true,
        bool deliveryEnabled = false,
        bool deliveryReady = false,
        bool acceptScheduled = false,
        bool storeIsOpenNow = true) =>
        new(
            Guid.NewGuid(),
            "Main",
            enabled,
            pickupEnabled,
            deliveryEnabled,
            operational,
            operational && pickupEnabled,
            operational && deliveryEnabled && deliveryReady,
            paused,
            paused ? "Paused" : null,
            null,
            null,
            null,
            true,
            null,
            pickupReady,
            deliveryReady,
            "Asia/Manila",
            null,
            acceptScheduled,
            30,
            14,
            storeIsOpenNow);

    [Fact]
    public void Open_store_allows_immediate_pickup_without_schedule()
    {
        var result = CustomerOrderPlaceBranchGate.Validate(
            Branch(),
            CustomerOrderFulfillmentType.Pickup,
            canCustomerDelivery: false,
            hasRequestedSchedule: false);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Closed_store_with_scheduling_requires_requested_time()
    {
        var closed = Branch(operational: false, acceptScheduled: true, storeIsOpenNow: false);
        var missing = CustomerOrderPlaceBranchGate.Validate(
            closed,
            CustomerOrderFulfillmentType.Pickup,
            canCustomerDelivery: false,
            hasRequestedSchedule: false);
        var withSchedule = CustomerOrderPlaceBranchGate.Validate(
            closed,
            CustomerOrderFulfillmentType.Pickup,
            canCustomerDelivery: false,
            hasRequestedSchedule: true);

        Assert.False(missing.IsSuccess);
        Assert.Contains("closed", missing.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        Assert.True(withSchedule.IsSuccess);
    }

    [Fact]
    public void HasRequestedSchedule_requires_both_date_and_time()
    {
        var dateOnly = new PlaceCustomerOrderRequest(
            "Pickup",
            Guid.NewGuid(),
            "Personal",
            "Ana",
            Guid.NewGuid(),
            null,
            null,
            null,
            [new PlaceCustomerOrderLineRequest(Guid.NewGuid(), 1m)],
            RequestedPickupDate: "2026-08-18",
            RequestedPickupTime: null);
        var both = dateOnly with { RequestedPickupTime = "10:00" };

        Assert.False(CustomerOrderPlaceBranchGate.HasRequestedSchedule(
            CustomerOrderFulfillmentType.Pickup,
            dateOnly));
        Assert.True(CustomerOrderPlaceBranchGate.HasRequestedSchedule(
            CustomerOrderFulfillmentType.Pickup,
            both));
    }

    [Fact]
    public void HasRequestedSchedule_accepts_preferred_day_and_period()
    {
        var preferred = new PlaceCustomerOrderRequest(
            "Pickup",
            Guid.NewGuid(),
            "Personal",
            "Ana",
            Guid.NewGuid(),
            null,
            null,
            null,
            [new PlaceCustomerOrderLineRequest(Guid.NewGuid(), 1m)],
            PreferredFulfillmentDate: "2026-08-18",
            PreferredFulfillmentPeriod: "Morning");

        Assert.True(CustomerOrderPlaceBranchGate.HasRequestedSchedule(
            CustomerOrderFulfillmentType.Pickup,
            preferred));
    }

    [Fact]
    public void Closed_store_allows_schedule_without_accept_scheduled_flag()
    {
        var result = CustomerOrderPlaceBranchGate.Validate(
            Branch(operational: false, acceptScheduled: false, storeIsOpenNow: false),
            CustomerOrderFulfillmentType.Pickup,
            canCustomerDelivery: false,
            hasRequestedSchedule: true);

        Assert.True(result.IsSuccess, result.ErrorMessage);
    }

    [Fact]
    public void Manual_pause_is_not_bypassable_with_schedule()
    {
        var result = CustomerOrderPlaceBranchGate.Validate(
            Branch(paused: true, operational: false, acceptScheduled: true, storeIsOpenNow: false),
            CustomerOrderFulfillmentType.Pickup,
            canCustomerDelivery: false,
            hasRequestedSchedule: true);

        Assert.False(result.IsSuccess);
        Assert.Contains("temporarily", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Closed_delivery_scheduling_requires_capable_branch()
    {
        var closed = Branch(
            operational: false,
            pickupEnabled: false,
            pickupReady: false,
            deliveryEnabled: true,
            deliveryReady: true,
            acceptScheduled: true,
            storeIsOpenNow: false);

        var ok = CustomerOrderPlaceBranchGate.Validate(
            closed,
            CustomerOrderFulfillmentType.Delivery,
            canCustomerDelivery: true,
            hasRequestedSchedule: true);
        var blocked = CustomerOrderPlaceBranchGate.Validate(
            closed,
            CustomerOrderFulfillmentType.Delivery,
            canCustomerDelivery: false,
            hasRequestedSchedule: true);

        Assert.True(ok.IsSuccess);
        Assert.False(blocked.IsSuccess);
    }

    [Fact]
    public void Classify_schedule_available_when_closed_and_scheduling_enabled()
    {
        var branches = new[]
        {
            Branch(operational: false, acceptScheduled: true, storeIsOpenNow: false)
        };

        Assert.Equal(
            CustomerStorefrontFulfillment.ScheduleAvailable,
            CustomerStorefrontFulfillment.Classify(branches, canCustomerDelivery: false));
    }

    [Fact]
    public void Classify_paused_not_store_closed()
    {
        var branches = new[]
        {
            Branch(paused: true, operational: false, acceptScheduled: true, storeIsOpenNow: false)
        };

        Assert.Equal(
            CustomerStorefrontFulfillment.Paused,
            CustomerStorefrontFulfillment.Classify(branches, canCustomerDelivery: false));
    }
}
