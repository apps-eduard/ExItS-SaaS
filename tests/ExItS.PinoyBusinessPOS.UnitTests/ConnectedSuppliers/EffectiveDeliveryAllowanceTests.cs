using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;

namespace ExItS.PinoyBusinessPOS.UnitTests.ConnectedSuppliers;

public sealed class EffectiveDeliveryAllowanceTests
{
    [Theory]
    [InlineData(false, true, CustomerDeliveryOverride.Inherit, false)]
    [InlineData(true, false, CustomerDeliveryOverride.Inherit, false)]
    [InlineData(true, true, CustomerDeliveryOverride.Block, false)]
    [InlineData(true, true, CustomerDeliveryOverride.Inherit, true)]
    [InlineData(true, true, CustomerDeliveryOverride.Allow, true)]
    [InlineData(false, true, CustomerDeliveryOverride.Allow, false)]
    public void Effective_rule_matches_org_branch_and_override(
        bool orgOffer,
        bool readyBranch,
        CustomerDeliveryOverride overrideValue,
        bool expected)
    {
        Assert.Equal(
            expected,
            EffectiveDeliveryAllowance.IsAllowed(orgOffer, readyBranch, overrideValue));
    }

    [Fact]
    public void Org_off_overrides_all_customers_including_explicit_allow()
    {
        Assert.False(EffectiveDeliveryAllowance.IsAllowed(
            orgOfferDelivery: false,
            readyDeliveryBranchExists: true,
            CustomerDeliveryOverride.Allow));
    }

    [Fact]
    public void Reenable_org_restores_inherit_but_keeps_block()
    {
        Assert.True(EffectiveDeliveryAllowance.IsAllowed(
            true, true, CustomerDeliveryOverride.Inherit));
        Assert.False(EffectiveDeliveryAllowance.IsAllowed(
            true, true, CustomerDeliveryOverride.Block));
    }

    [Theory]
    [InlineData(null, CustomerDeliveryOverride.Inherit)]
    [InlineData("", CustomerDeliveryOverride.Inherit)]
    [InlineData("allow", CustomerDeliveryOverride.Allow)]
    [InlineData("block", CustomerDeliveryOverride.Block)]
    public void Parse_override_round_trips(string? raw, CustomerDeliveryOverride expected)
    {
        Assert.Equal(expected, EffectiveDeliveryAllowance.ParseOverride(raw));
    }
}
