using ExItS.PinoyBuyNowPayLater.Domain.Access;

namespace ExItS.PinoyBuyNowPayLater.UnitTests.Access;

public sealed class BnplCapabilityCatalogTests
{
    [Fact]
    public void Known_capability_catalog_is_stable()
    {
        Assert.Equal(11, BnplCapabilityCodes.All.Count);
        Assert.True(BnplCapabilityCodes.IsKnown(BnplCapabilityCodes.CustomerRead));
        Assert.True(BnplCapabilityCodes.IsKnown(BnplCapabilityCodes.CustomerManage));
        Assert.True(BnplCapabilityCodes.IsKnown(BnplCapabilityCodes.ApplicationApprove));
        Assert.False(BnplCapabilityCodes.IsKnown("bnpl.fake"));
        Assert.False(BnplCapabilityCodes.IsKnown(null));
    }

    [Fact]
    public void Presets_are_capability_bundles_not_authorization_keys()
    {
        Assert.Contains(BnplCapabilityCodes.CustomerManage, BnplCapabilityPresets.SalesCapabilities);
        Assert.Contains(BnplCapabilityCodes.CustomerRead, BnplCapabilityPresets.ReportingCapabilities);
        Assert.Contains(BnplCapabilityCodes.ApplicationApprove, BnplCapabilityPresets.ApproverCapabilities);
        Assert.DoesNotContain(BnplCapabilityCodes.SettlementManage, BnplCapabilityPresets.SalesCapabilities);
        Assert.DoesNotContain(BnplCapabilityCodes.ApplicationApprove, BnplCapabilityPresets.SalesCapabilities);
        Assert.Equal(
            BnplCapabilityPresets.OwnerCapabilities,
            BnplCapabilityPresets.CapabilitiesFor(BnplCapabilityPresets.Owner));
        Assert.Empty(BnplCapabilityPresets.CapabilitiesFor("NotARealPreset"));
    }

    [Fact]
    public void Product_identity_rejects_pos_and_plm_codes()
    {
        Assert.False(BnplProductIdentity.IsPinoyBuyNowPayLater("pinoy-business-pos"));
        Assert.False(BnplProductIdentity.IsPinoyBuyNowPayLater("pinoy-loan-manager"));
        Assert.True(BnplProductIdentity.IsPinoyBuyNowPayLater(BnplProductIdentity.ProductCode));
    }
}
