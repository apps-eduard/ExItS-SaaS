using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;

namespace ExItS.PinoyBusinessPOS.UnitTests.ConnectedSuppliers;

public sealed class BranchDeliveryEnablementTests
{
    [Theory]
    [InlineData(true, false, true, false)] // global ON, branch OFF → ON allowed
    [InlineData(true, true, false, false)] // global ON, branch ON → OFF allowed
    [InlineData(false, false, true, true)] // global OFF, branch OFF → ON blocked
    [InlineData(false, true, false, false)] // global OFF, branch ON → OFF allowed
    [InlineData(false, true, true, false)] // global OFF, already ON → re-assert ON allowed
    [InlineData(false, false, false, false)] // global OFF, stay OFF
    public void IsEnableBlockedByOrgOffer_matches_global_off_guard(
        bool orgOfferDelivery,
        bool currentlyDeliveryEnabled,
        bool requestedDeliveryEnabled,
        bool expectedBlocked)
    {
        var blocked = BranchDeliveryEnablement.IsEnableBlockedByOrgOffer(
            orgOfferDelivery,
            currentlyDeliveryEnabled,
            requestedDeliveryEnabled);

        Assert.Equal(expectedBlocked, blocked);
    }

    [Fact]
    public void OrganizationDeliveryNotOfferedMessage_is_actionable()
    {
        Assert.Equal(
            "Turn on Offer Delivery before enabling Delivery for this branch.",
            BranchDeliveryEnablement.OrganizationDeliveryNotOfferedMessage);
    }
}
