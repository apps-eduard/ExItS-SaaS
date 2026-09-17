using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Credit;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.UnitTests.Credit;

public sealed class CustomerCreditPolicyDomainTests
{
    private static readonly PosOrganizationId Org = PosOrganizationId.From(Guid.Parse("11111111-1111-1111-1111-111111111111"));
    private static readonly POSCustomerId Customer = POSCustomerId.From(Guid.Parse("22222222-2222-2222-2222-222222222222"));
    private static readonly Guid ActorA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid ActorB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-07-30T10:00:00Z");

    [Fact]
    public void Configure_then_approve_moves_to_approved_and_permits_utang()
    {
        var (policy, configureChange) = CustomerCreditPolicy.Configure(
            Org,
            Customer,
            creditLimit: 5_000m,
            defaultTermDays: 30,
            ActorA,
            reason: null,
            Now);

        Assert.Equal(CustomerCreditPolicyStatus.PendingApproval, policy.Status);
        Assert.False(policy.PermitsNewUtang);
        Assert.Equal(CustomerCreditPolicyChangeAction.Configured, configureChange.Action);
        Assert.Equal(CustomerCreditPolicy.InitialConfigureReason, configureChange.Reason);

        var approveChange = policy.Approve(ActorA, "Approved for credit.", Now.AddSeconds(1));
        Assert.Equal(CustomerCreditPolicyStatus.Approved, policy.Status);
        Assert.True(policy.PermitsNewUtang);
        Assert.Equal(ActorA, policy.ApprovedByUserId);
        Assert.Equal(CustomerCreditPolicyChangeAction.Approved, approveChange.Action);
    }

    [Fact]
    public void Same_actor_may_configure_and_approve()
    {
        var (policy, _) = CustomerCreditPolicy.Configure(
            Org,
            Customer,
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
        var (policy, _) = CustomerCreditPolicy.Configure(Org, Customer, 1_000m, 30, ActorA, null, Now);
        policy.Approve(ActorB, "Approve", Now.AddSeconds(1));

        var change = policy.UpdateTerms(2_000m, 30, ActorA, "Raise limit", Now.AddMinutes(1));
        Assert.Equal(CustomerCreditPolicyStatus.PendingApproval, policy.Status);
        Assert.False(policy.PermitsNewUtang);
        Assert.Null(policy.ApprovedByUserId);
        Assert.Null(policy.ApprovedAtUtc);
        Assert.Equal(2_000m, policy.CreditLimit);
        Assert.Equal(CustomerCreditPolicyChangeAction.CreditLimitChanged, change.Action);
        Assert.Equal(CustomerCreditPolicyStatus.Approved, change.PreviousStatus);
        Assert.Equal(1_000m, change.PreviousCreditLimit);
        Assert.Equal(2_000m, change.NewCreditLimit);
    }

    [Fact]
    public void Disable_from_approved_blocks_new_utang()
    {
        var (policy, _) = CustomerCreditPolicy.Configure(Org, Customer, 500m, 7, ActorA, null, Now);
        policy.Approve(ActorB, "Approve", Now.AddSeconds(1));

        var disable = policy.Disable(ActorA, "Stop credit", Now.AddMinutes(1));
        Assert.Equal(CustomerCreditPolicyStatus.Disabled, policy.Status);
        Assert.False(policy.PermitsNewUtang);
        Assert.Equal(CustomerCreditPolicyChangeAction.Disabled, disable.Action);
        Assert.Equal(0m, CustomerCreditPolicy.AvailableCredit(policy.Status, policy.CreditLimit, outstanding: 0m));
    }

    [Fact]
    public void AvailableCredit_is_zero_when_not_approved_or_exhausted()
    {
        Assert.Equal(0m, CustomerCreditPolicy.AvailableCredit(CustomerCreditPolicyStatus.PendingApproval, 100m, 0m));
        Assert.Equal(0m, CustomerCreditPolicy.AvailableCredit(CustomerCreditPolicyStatus.Disabled, 100m, 0m));
        Assert.Equal(0m, CustomerCreditPolicy.AvailableCredit(CustomerCreditPolicyStatus.NotConfigured, 100m, 0m));
        Assert.Equal(0m, CustomerCreditPolicy.AvailableCredit(CustomerCreditPolicyStatus.Approved, 100m, 100m));
        Assert.Equal(0m, CustomerCreditPolicy.AvailableCredit(CustomerCreditPolicyStatus.Approved, 100m, 150m));
    }

    [Fact]
    public void AvailableCredit_limit_exceeded_formula_is_limit_minus_outstanding()
    {
        Assert.Equal(40m, CustomerCreditPolicy.AvailableCredit(CustomerCreditPolicyStatus.Approved, 100m, 60m));
        Assert.Equal(100m, CustomerCreditPolicy.AvailableCredit(CustomerCreditPolicyStatus.Approved, 100m, 0m));

        // Projected outstanding > limit when requested > available (authorization formula).
        const decimal limit = 100m;
        const decimal outstanding = 70m;
        const decimal requested = 40m;
        var available = CustomerCreditPolicy.AvailableCredit(CustomerCreditPolicyStatus.Approved, limit, outstanding);
        Assert.Equal(30m, available);
        Assert.True(outstanding + requested > limit);
    }

    [Fact]
    public void Term_days_must_be_within_bounds()
    {
        Assert.Equal(
            DomainErrorCodes.InvalidCustomerCreditTermDays,
            Assert.Throws<DomainException>(() =>
                CustomerCreditPolicy.Configure(Org, Customer, 10m, 0, ActorA, null, Now)).ErrorCode);
        Assert.Equal(
            DomainErrorCodes.InvalidCustomerCreditTermDays,
            Assert.Throws<DomainException>(() =>
                CustomerCreditPolicy.Configure(Org, Customer, 10m, 366, ActorA, null, Now)).ErrorCode);
        Assert.Equal(1, CustomerCreditPolicy.NormalizeTermDays(1));
        Assert.Equal(365, CustomerCreditPolicy.NormalizeTermDays(365));
    }

    [Fact]
    public void ComputeDefaultDueDate_adds_normalized_term_days()
    {
        var saleDate = new DateOnly(2026, 7, 30);
        Assert.Equal(new DateOnly(2026, 8, 29), CustomerCreditPolicy.ComputeDefaultDueDate(saleDate, 30));
    }
}
