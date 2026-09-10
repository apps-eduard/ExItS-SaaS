using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Credit;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.UnitTests.Credit;

public sealed class BusinessCustomerCreditPolicyDomainTests
{
    private static readonly PosOrganizationId Seller = PosOrganizationId.From(Guid.Parse("11111111-1111-1111-1111-111111111111"));
    private static readonly PosOrganizationId Buyer = PosOrganizationId.From(Guid.Parse("22222222-2222-2222-2222-222222222222"));
    private static readonly Guid ConnectionId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid ActorA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid ActorB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-07-30T10:00:00Z");

    [Fact]
    public void Configure_then_approve_moves_to_approved_and_permits_utang()
    {
        var (policy, configureChange) = BusinessCustomerCreditPolicy.Configure(
            Seller,
            Buyer,
            ConnectionId,
            creditLimit: 5_000m,
            defaultTermDays: 30,
            ActorA,
            reason: null,
            Now);

        Assert.Equal(CustomerCreditPolicyStatus.PendingApproval, policy.Status);
        Assert.False(policy.PermitsNewUtang);
        Assert.Equal(CustomerCreditPolicyChangeAction.Configured, configureChange.Action);
        Assert.Equal(BusinessCustomerCreditPolicy.InitialConfigureReason, configureChange.Reason);
        Assert.Equal(ConnectionId, policy.ConnectionId);

        var approveChange = policy.Approve(ActorA, "Approved for credit.", Now.AddSeconds(1));
        Assert.Equal(CustomerCreditPolicyStatus.Approved, policy.Status);
        Assert.True(policy.PermitsNewUtang);
        Assert.Equal(ActorA, policy.ApprovedByUserId);
        Assert.Equal(CustomerCreditPolicyChangeAction.Approved, approveChange.Action);
    }

    [Fact]
    public void Same_actor_may_configure_and_approve()
    {
        var (policy, _) = BusinessCustomerCreditPolicy.Configure(
            Seller,
            Buyer,
            ConnectionId,
            1_000m,
            14,
            ActorA,
            "Initial",
            Now);

        var approve = policy.Approve(ActorA, "Same actor approve", Now.AddMinutes(1));
        Assert.Equal(CustomerCreditPolicyStatus.Approved, policy.Status);
        Assert.Equal(ActorA, policy.ConfiguredByUserId);
        Assert.Equal(ActorA, policy.ApprovedByUserId);
        Assert.Equal(CustomerCreditPolicyChangeAction.Approved, approve.Action);
    }

    [Fact]
    public void Limit_change_on_approved_policy_returns_to_pending()
    {
        var (policy, _) = BusinessCustomerCreditPolicy.Configure(
            Seller, Buyer, ConnectionId, 1_000m, 30, ActorA, null, Now);
        policy.Approve(ActorB, "Approve", Now.AddSeconds(1));

        var change = policy.UpdateTerms(2_000m, 30, ActorA, "Raise limit", Now.AddMinutes(1));
        Assert.Equal(CustomerCreditPolicyStatus.PendingApproval, policy.Status);
        Assert.False(policy.PermitsNewUtang);
        Assert.Null(policy.ApprovedByUserId);
        Assert.Null(policy.ApprovedAtUtc);
        Assert.Equal(2_000m, policy.CreditLimit);
        Assert.Equal(CustomerCreditPolicyChangeAction.CreditLimitChanged, change.Action);
        Assert.Equal(CustomerCreditPolicyStatus.Approved, change.PreviousStatus);
    }

    [Fact]
    public void Disable_from_approved_blocks_new_utang()
    {
        var (policy, _) = BusinessCustomerCreditPolicy.Configure(
            Seller, Buyer, ConnectionId, 500m, 7, ActorA, null, Now);
        policy.Approve(ActorB, "Approve", Now.AddSeconds(1));

        var disable = policy.Disable(ActorA, "Stop credit", Now.AddMinutes(1));
        Assert.Equal(CustomerCreditPolicyStatus.Disabled, policy.Status);
        Assert.False(policy.PermitsNewUtang);
        Assert.Equal(CustomerCreditPolicyChangeAction.Disabled, disable.Action);
        Assert.Equal(0m, BusinessCustomerCreditPolicy.AvailableCredit(policy.Status, policy.CreditLimit));
    }

    [Fact]
    public void AvailableCredit_uses_zero_outstanding_for_b2b()
    {
        Assert.Equal(0m, BusinessCustomerCreditPolicy.AvailableCredit(CustomerCreditPolicyStatus.PendingApproval, 100m));
        Assert.Equal(100m, BusinessCustomerCreditPolicy.AvailableCredit(CustomerCreditPolicyStatus.Approved, 100m));
        Assert.Equal(40m, BusinessCustomerCreditPolicy.AvailableCredit(CustomerCreditPolicyStatus.Approved, 100m, outstanding: 60m));
    }
}
