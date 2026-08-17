using ExItS.PinoyBusinessPOS.Application.CustomerOrdering;

namespace ExItS.PinoyBusinessPOS.UnitTests.CustomerOrdering;

public sealed class PersonalMerchantCheckoutUiTests
{
    private static readonly CustomerStorefrontBranchDto PickupOnly =
        new(Guid.Parse("11111111-1111-1111-1111-111111111111"), "Main Branch", true, false);
    private static readonly CustomerStorefrontBranchDto BothModes =
        new(Guid.Parse("22222222-2222-2222-2222-222222222222"), "Both Branch", true, true);
    private static readonly CustomerStorefrontBranchDto DeliveryOnly =
        new(Guid.Parse("33333333-3333-3333-3333-333333333333"), "Delivery Branch", false, true);
    private static readonly CustomerStorefrontBranchDto SecondPickup =
        new(Guid.Parse("44444444-4444-4444-4444-444444444444"), "Second Branch", true, false);

    [Fact]
    public void One_pickup_branch_auto_selects_and_hides_selector()
    {
        var selection = PersonalMerchantCheckoutUi.Resolve([PickupOnly], canCustomerDelivery: false, "Pickup", null);
        Assert.Equal(PersonalMerchantCheckoutUi.Pickup, selection.FulfillmentType);
        Assert.Equal(PickupOnly.BranchId, selection.BranchId);
        Assert.Equal("Main Branch", selection.BranchName);
        Assert.False(selection.ShowBranchSelector);
        Assert.False(selection.ShowFulfillmentToggle);
        Assert.True(selection.CanPlace);
    }

    [Fact]
    public void One_branch_supporting_pickup_and_delivery_shows_fulfillment_toggle()
    {
        var selection = PersonalMerchantCheckoutUi.Resolve([BothModes], canCustomerDelivery: true, "Pickup", null);
        Assert.Equal(PersonalMerchantCheckoutUi.Pickup, selection.FulfillmentType);
        Assert.Equal(BothModes.BranchId, selection.BranchId);
        Assert.False(selection.ShowBranchSelector);
        Assert.True(selection.ShowFulfillmentToggle);
        Assert.True(selection.CanPlace);
    }

    [Fact]
    public void Multiple_pickup_branches_preselect_first_and_show_selector()
    {
        var selection = PersonalMerchantCheckoutUi.Resolve(
            [PickupOnly, SecondPickup],
            canCustomerDelivery: false,
            "Pickup",
            null);
        Assert.Equal(PickupOnly.BranchId, selection.BranchId);
        Assert.True(selection.ShowBranchSelector);
        Assert.True(selection.CanPlace);
    }

    [Fact]
    public void Changing_fulfillment_keeps_valid_branch()
    {
        var kept = PersonalMerchantCheckoutUi.Resolve(
            [BothModes, DeliveryOnly],
            canCustomerDelivery: true,
            PersonalMerchantCheckoutUi.Delivery,
            BothModes.BranchId);
        Assert.Equal(BothModes.BranchId, kept.BranchId);
        Assert.Equal(PersonalMerchantCheckoutUi.Delivery, kept.FulfillmentType);

        var switched = PersonalMerchantCheckoutUi.Resolve(
            [PickupOnly, DeliveryOnly],
            canCustomerDelivery: true,
            PersonalMerchantCheckoutUi.Delivery,
            PickupOnly.BranchId);
        Assert.Equal(DeliveryOnly.BranchId, switched.BranchId);
        Assert.False(switched.ShowBranchSelector);
    }

    [Fact]
    public void No_eligible_branch_blocks_place()
    {
        var noPickupWhenDeliveryLocked = PersonalMerchantCheckoutUi.Resolve(
            [DeliveryOnly],
            canCustomerDelivery: false,
            PersonalMerchantCheckoutUi.Pickup,
            null);
        Assert.False(noPickupWhenDeliveryLocked.CanPlace);
        Assert.Null(noPickupWhenDeliveryLocked.BranchId);
        Assert.False(noPickupWhenDeliveryLocked.ShowBranchSelector);

        var none = PersonalMerchantCheckoutUi.Resolve(
            [],
            canCustomerDelivery: true,
            PersonalMerchantCheckoutUi.Pickup,
            null);
        Assert.False(none.CanPlace);
    }
}
