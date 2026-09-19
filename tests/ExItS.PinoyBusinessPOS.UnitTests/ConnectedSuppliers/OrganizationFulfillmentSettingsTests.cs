using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.UnitTests.ConnectedSuppliers;

public sealed class OrganizationFulfillmentSettingsTests
{
    private static readonly PosOrganizationId OrgId =
        PosOrganizationId.From(Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"));

    private static readonly DateTimeOffset Now =
        new(2026, 9, 19, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public void CreateDefault_preserves_historical_create_behavior_all_channels_off()
    {
        var settings = OrganizationFulfillmentSettings.CreateDefault(OrgId, Now);

        Assert.False(settings.OfferDelivery);
        Assert.False(settings.DefaultPickupEnabled);
        Assert.False(settings.DefaultDeliveryEnabled);
        Assert.False(settings.DefaultOnlineOrdersEnabled);
    }

    [Fact]
    public void SetBranchFulfillmentDefaults_updates_defaults_only()
    {
        var settings = OrganizationFulfillmentSettings.CreateDefault(OrgId, Now);
        settings.SetOfferDelivery(true, Now);

        settings.SetBranchFulfillmentDefaults(
            defaultPickupEnabled: true,
            defaultDeliveryEnabled: false,
            defaultOnlineOrdersEnabled: true,
            Now.AddMinutes(1));

        Assert.True(settings.OfferDelivery);
        Assert.True(settings.DefaultPickupEnabled);
        Assert.False(settings.DefaultDeliveryEnabled);
        Assert.True(settings.DefaultOnlineOrdersEnabled);
    }

    [Fact]
    public void SetOfferDelivery_does_not_change_branch_defaults()
    {
        var settings = OrganizationFulfillmentSettings.CreateDefault(OrgId, Now);
        settings.SetBranchFulfillmentDefaults(true, true, true, Now);

        settings.SetOfferDelivery(false, Now.AddMinutes(1));

        Assert.False(settings.OfferDelivery);
        Assert.True(settings.DefaultPickupEnabled);
        Assert.True(settings.DefaultDeliveryEnabled);
        Assert.True(settings.DefaultOnlineOrdersEnabled);
    }
}
