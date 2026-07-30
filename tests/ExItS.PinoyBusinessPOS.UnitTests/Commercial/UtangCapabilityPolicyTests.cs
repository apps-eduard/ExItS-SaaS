using ExItS.PinoyBusinessPOS.Application.Commercial;

namespace ExItS.PinoyBusinessPOS.UnitTests.Commercial;

public sealed class UtangCapabilityPolicyTests
{
    private static readonly string[] FullGrants =
    [
        PosFeatureCodes.CustomerCreditView,
        PosFeatureCodes.CustomerCreditRepay,
        PosFeatureCodes.CustomerCreditCreate
    ];

    private static readonly string[] ContinuityGrants =
    [
        PosFeatureCodes.CustomerCreditView,
        PosFeatureCodes.CustomerCreditRepay
    ];

    private static readonly string[] ViewOnly = [PosFeatureCodes.CustomerCreditView];

    [Theory]
    [InlineData(PosSubscriptionStatuses.Trialing)]
    [InlineData(PosSubscriptionStatuses.Active)]
    [InlineData(PosSubscriptionStatuses.GracePeriod)]
    public void Full_states_allow_mutations_when_grants_present(string status)
    {
        Assert.True(UtangCapabilityPolicy.IsAllowed(UtangCapability.CreateCustomer, status, FullGrants));
        Assert.True(UtangCapabilityPolicy.IsAllowed(UtangCapability.EditCustomer, status, FullGrants));
        Assert.True(UtangCapabilityPolicy.IsAllowed(UtangCapability.CreateCredit, status, FullGrants));
        Assert.True(UtangCapabilityPolicy.IsAllowed(UtangCapability.MutateDueDate, status, FullGrants));
        Assert.True(UtangCapabilityPolicy.IsAllowed(UtangCapability.ReverseRepayment, status, FullGrants));
        Assert.True(UtangCapabilityPolicy.IsAllowed(UtangCapability.RecordRepayment, status, FullGrants));
        Assert.True(UtangCapabilityPolicy.IsAllowed(UtangCapability.ReverseCredit, status, FullGrants));
        Assert.True(UtangCapabilityPolicy.IsAllowed(UtangCapability.ViewGenerateStatement, status, FullGrants));
        Assert.True(UtangCapabilityPolicy.IsAllowed(UtangCapability.ViewGenerateReceipt, status, FullGrants));
    }

    [Theory]
    [InlineData(PosSubscriptionStatuses.PastDue)]
    [InlineData(PosSubscriptionStatuses.Cancelled)]
    [InlineData(PosSubscriptionStatuses.Expired)]
    public void Continuity_states_allow_view_repay_credit_reverse_deny_mutations(string status)
    {
        Assert.True(UtangCapabilityPolicy.CanEnter(status, ContinuityGrants));
        Assert.True(UtangCapabilityPolicy.IsAllowed(UtangCapability.ViewCustomersAndHistory, status, ContinuityGrants));
        Assert.True(UtangCapabilityPolicy.IsAllowed(UtangCapability.RecordRepayment, status, ContinuityGrants));
        Assert.True(UtangCapabilityPolicy.IsAllowed(UtangCapability.ReverseCredit, status, ContinuityGrants));
        Assert.True(UtangCapabilityPolicy.IsAllowed(UtangCapability.ViewGenerateStatement, status, ContinuityGrants));
        Assert.True(UtangCapabilityPolicy.IsAllowed(UtangCapability.ViewGenerateReceipt, status, ContinuityGrants));

        // OD-07 / OD-08 / OD-09 repayment reverse / due-date / create credit
        Assert.False(UtangCapabilityPolicy.IsAllowed(UtangCapability.CreateCustomer, status, ContinuityGrants));
        Assert.False(UtangCapabilityPolicy.IsAllowed(UtangCapability.EditCustomer, status, ContinuityGrants));
        Assert.False(UtangCapabilityPolicy.IsAllowed(UtangCapability.CreateCredit, status, ContinuityGrants));
        Assert.False(UtangCapabilityPolicy.IsAllowed(UtangCapability.ReverseRepayment, status, ContinuityGrants));
        Assert.False(UtangCapabilityPolicy.IsAllowed(UtangCapability.MutateDueDate, status, ContinuityGrants));
    }

    [Fact]
    public void Suspended_denies_all_capabilities()
    {
        foreach (UtangCapability capability in Enum.GetValues<UtangCapability>())
        {
            Assert.False(UtangCapabilityPolicy.IsAllowed(capability, PosSubscriptionStatuses.Suspended, FullGrants));
        }
    }

    [Fact]
    public void Missing_or_unknown_status_denies()
    {
        Assert.False(UtangCapabilityPolicy.IsAllowed(UtangCapability.ViewCustomersAndHistory, null, FullGrants));
        Assert.False(UtangCapabilityPolicy.IsAllowed(UtangCapability.ViewCustomersAndHistory, "Unknown", FullGrants));
        Assert.False(UtangCapabilityPolicy.IsAllowed(UtangCapability.EnterPos, "", ViewOnly));
    }

    [Fact]
    public void Continuity_entry_requires_view_or_repay_not_create_alone()
    {
        Assert.False(UtangCapabilityPolicy.CanEnter(
            PosSubscriptionStatuses.Expired,
            [PosFeatureCodes.CustomerCreditCreate]));
        Assert.True(UtangCapabilityPolicy.CanEnter(PosSubscriptionStatuses.Expired, ViewOnly));
    }

    [Fact]
    public void Feature_grants_required_even_in_active()
    {
        Assert.False(UtangCapabilityPolicy.IsAllowed(
            UtangCapability.CreateCredit,
            PosSubscriptionStatuses.Active,
            ViewOnly));
        Assert.False(UtangCapabilityPolicy.IsAllowed(
            UtangCapability.RecordRepayment,
            PosSubscriptionStatuses.Active,
            ViewOnly));
    }
}
