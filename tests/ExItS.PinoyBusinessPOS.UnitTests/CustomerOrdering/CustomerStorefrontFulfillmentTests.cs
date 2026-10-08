using ExItS.PinoyBusinessPOS.Application.CustomerOrdering;

namespace ExItS.PinoyBusinessPOS.UnitTests.CustomerOrdering;

public sealed class CustomerStorefrontFulfillmentTests
{
    [Fact]
    public void Zero_eligible_branches_has_no_placeable_fulfillment()
    {
        var branches = new[] { Branch(pickup: false, delivery: false, ordering: false) };
        Assert.False(CustomerStorefrontFulfillment.HasPlaceableFulfillment(branches, true));
        Assert.Equal(CustomerStorefrontFulfillment.Paused, CustomerStorefrontFulfillment.Classify(branches, true));
    }

    [Fact]
    public void Pickup_and_delivery_disabled_is_no_method()
    {
        var branches = new[] { Branch(pickup: false, delivery: false) };
        Assert.Equal(CustomerStorefrontFulfillment.NoMethod, CustomerStorefrontFulfillment.Classify(branches, true));
    }

    [Fact]
    public void Pickup_temporarily_unavailable_is_not_placeable()
    {
        var branches = new[] { Branch(pickupOperational: false) };
        Assert.Equal(
            CustomerStorefrontFulfillment.PickupUnavailable,
            CustomerStorefrontFulfillment.Classify(branches, false));
    }

    [Fact]
    public void Online_orders_paused_is_paused()
    {
        var branches = new[] { Branch(paused: true, pickupOperational: false, orderingOperational: false) };
        Assert.Equal(CustomerStorefrontFulfillment.Paused, CustomerStorefrontFulfillment.Classify(branches, true));
        Assert.True(CustomerStorefrontFulfillment.AllEnabledBranchesPaused(branches));
    }

    [Fact]
    public void Pickup_only_branch_is_ready()
    {
        var branches = new[] { Branch(delivery: false) };
        Assert.Equal(CustomerStorefrontFulfillment.Ready, CustomerStorefrontFulfillment.Classify(branches, true));
    }

    [Fact]
    public void Delivery_only_branch_is_ready_when_entitled()
    {
        var branches = new[] { Branch(pickup: false, delivery: true) };
        Assert.Equal(CustomerStorefrontFulfillment.Ready, CustomerStorefrontFulfillment.Classify(branches, true));
        Assert.Equal(
            CustomerStorefrontFulfillment.NoMethod,
            CustomerStorefrontFulfillment.Classify(branches, canCustomerDelivery: false));
    }

    [Fact]
    public void Multiple_branches_are_ready_when_one_can_place()
    {
        var branches = new[]
        {
            Branch(pickupOperational: false),
            Branch(delivery: true, pickup: true),
        };
        Assert.Equal(CustomerStorefrontFulfillment.Ready, CustomerStorefrontFulfillment.Classify(branches, true));
    }

    private static CustomerOrderBranchSnapshot Branch(
        bool pickup = true,
        bool delivery = false,
        bool ordering = true,
        bool paused = false,
        bool pickupOperational = true,
        bool deliveryOperational = true,
        bool orderingOperational = true) =>
        new(
            Guid.NewGuid(),
            "Main",
            ordering,
            pickup,
            delivery,
            orderingOperational,
            pickupOperational,
            delivery && deliveryOperational,
            paused,
            null,
            null,
            null,
            null);
}
