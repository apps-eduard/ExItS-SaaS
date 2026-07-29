using ExItS.Platform.Domain.Catalog;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Products;
using ExItS.Platform.Domain.Subscriptions;
using ExItS.Platform.UnitTests.Support;

namespace ExItS.Platform.UnitTests.Subscriptions;

public sealed class SubscriptionLifecycleTests
{
    private static readonly DateTimeOffset T0 = new(2026, 7, 29, 12, 0, 0, TimeSpan.Zero);

    private static (Plan plan, PlanVersion version, TrialDefinition trial) CreatePosCatalog(
        TimeSpan? trialDuration = null)
    {
        var plan = Plan.CreateDraft(
            ProductCode.Create(ProductCode.PinoyBusinessPos),
            PlanCode.Create("utang-trial"),
            "Utang Trial",
            T0);
        plan.Activate(T0);
        var grants = UtangTrialTestFactory.ActiveGrants();
        var version = PlanVersion.CreateDraft(plan, 1, T0, BillingPeriod.None, true, grants, T0);
        version.Publish(T0);
        var trial = UtangTrialTestFactory.CreateConfigured(T0, trialDuration ?? TimeSpan.FromDays(14), plan.Id);
        return (plan, version, trial);
    }

    private static Subscription StartAndActivate(DateTimeOffset paidPeriodEndUtc)
    {
        var (plan, version, trial) = CreatePosCatalog();
        var sub = Subscription.StartTrial(PlatformOrganizationId.New(), plan, version, trial, T0);
        sub.ActivateFromTrial(T0, paidPeriodEndUtc, T0.AddMinutes(1));
        return sub;
    }

    [Theory]
    [InlineData(SubscriptionStatus.Cancelled)]
    [InlineData(SubscriptionStatus.Expired)]
    public void Cancelled_and_Expired_are_terminal_and_reject_reactivation(SubscriptionStatus terminalStatus)
    {
        var sub = StartAndActivate(T0.AddDays(30));
        if (terminalStatus == SubscriptionStatus.Cancelled)
        {
            sub.Cancel(T0.AddMinutes(2));
        }
        else
        {
            sub.Expire(T0.AddMinutes(2));
        }

        Assert.Equal(terminalStatus, sub.Status);
        var ex = Assert.Throws<DomainException>(() => sub.Reactivate(T0.AddMinutes(3)));
        Assert.Equal(DomainErrorCodes.InvalidSubscriptionTransition, ex.ErrorCode);

        // Terminal states reject every other lifecycle command too.
        Assert.Throws<DomainException>(() => sub.Suspend(T0.AddMinutes(4)));
        Assert.Throws<DomainException>(() => sub.MarkPastDue(T0.AddMinutes(4)));
        Assert.Throws<DomainException>(() => sub.EnterGracePeriod(T0.AddDays(60), T0.AddMinutes(4)));
    }

    [Fact]
    public void EnterGracePeriod_rejects_end_before_current_paid_period_end()
    {
        var sub = StartAndActivate(T0.AddDays(30));
        var ex = Assert.Throws<DomainException>(() => sub.EnterGracePeriod(T0.AddDays(10), T0.AddMinutes(2)));
        Assert.Equal(DomainErrorCodes.InvalidEffectiveRange, ex.ErrorCode);
    }

    [Fact]
    public void EnterGracePeriod_accepts_end_at_or_after_paid_period_end()
    {
        var sub = StartAndActivate(T0.AddDays(30));
        sub.EnterGracePeriod(T0.AddDays(30), T0.AddMinutes(2));
        Assert.Equal(SubscriptionStatus.GracePeriod, sub.Status);
        Assert.Equal(T0.AddDays(30), sub.GracePeriodEndUtc);
    }

    [Fact]
    public void Reactivate_from_GracePeriod_requires_new_paid_period_range()
    {
        var sub = StartAndActivate(T0.AddDays(30));
        sub.EnterGracePeriod(T0.AddDays(37), T0.AddMinutes(2));

        var missingPeriod = Assert.Throws<DomainException>(() => sub.Reactivate(T0.AddMinutes(3)));
        Assert.Equal(DomainErrorCodes.InvalidEffectiveRange, missingPeriod.ErrorCode);

        var invalidRange = Assert.Throws<DomainException>(() =>
            sub.Reactivate(T0.AddMinutes(3), T0.AddDays(31), T0.AddDays(31)));
        Assert.Equal(DomainErrorCodes.InvalidEffectiveRange, invalidRange.ErrorCode);

        sub.Reactivate(T0.AddMinutes(4), T0.AddDays(31), T0.AddDays(61));
        Assert.Equal(SubscriptionStatus.Active, sub.Status);
        Assert.Equal(T0.AddDays(31), sub.PaidPeriodStartUtc);
        Assert.Equal(T0.AddDays(61), sub.PaidPeriodEndUtc);
        Assert.Null(sub.GracePeriodEndUtc);
        Assert.Null(sub.PastDueAtUtc);
        Assert.Null(sub.SuspendedAtUtc);
    }

    [Fact]
    public void Reactivate_from_PastDue_requires_new_paid_period_range()
    {
        var sub = StartAndActivate(T0.AddDays(30));
        sub.MarkPastDue(T0.AddMinutes(2));
        Assert.Equal(T0.AddMinutes(2), sub.PastDueAtUtc);

        Assert.Throws<DomainException>(() => sub.Reactivate(T0.AddMinutes(3)));

        sub.Reactivate(T0.AddMinutes(4), T0.AddDays(31), T0.AddDays(61));
        Assert.Equal(SubscriptionStatus.Active, sub.Status);
        Assert.Null(sub.PastDueAtUtc);
    }

    [Fact]
    public void Reactivate_from_Suspended_allows_optional_period()
    {
        var sub = StartAndActivate(T0.AddDays(30));
        sub.Suspend(T0.AddMinutes(2));
        Assert.NotNull(sub.SuspendedAtUtc);

        sub.Reactivate(T0.AddMinutes(3));
        Assert.Equal(SubscriptionStatus.Active, sub.Status);
        Assert.Null(sub.SuspendedAtUtc);
        Assert.Equal(T0.AddDays(30), sub.PaidPeriodEndUtc);
    }

    [Fact]
    public void Reactivate_from_Suspended_with_period_updates_paid_period()
    {
        var sub = StartAndActivate(T0.AddDays(30));
        sub.Suspend(T0.AddMinutes(2));

        sub.Reactivate(T0.AddMinutes(3), T0.AddDays(31), T0.AddDays(61));
        Assert.Equal(T0.AddDays(31), sub.PaidPeriodStartUtc);
        Assert.Equal(T0.AddDays(61), sub.PaidPeriodEndUtc);
    }

    [Fact]
    public void MarkPastDue_and_Expire_record_timestamps()
    {
        var sub = StartAndActivate(T0.AddDays(30));
        sub.MarkPastDue(T0.AddMinutes(2));
        Assert.Equal(T0.AddMinutes(2), sub.PastDueAtUtc);

        sub.Reactivate(T0.AddMinutes(3), T0.AddDays(31), T0.AddDays(61));
        Assert.Null(sub.PastDueAtUtc);

        sub.Expire(T0.AddMinutes(4));
        Assert.Equal(SubscriptionStatus.Expired, sub.Status);
        Assert.Equal(T0.AddMinutes(4), sub.ExpiredAtUtc);
    }

    [Theory]
    [InlineData(SubscriptionStatus.Trialing, true)]
    [InlineData(SubscriptionStatus.Active, true)]
    [InlineData(SubscriptionStatus.GracePeriod, true)]
    [InlineData(SubscriptionStatus.PastDue, true)]
    [InlineData(SubscriptionStatus.Suspended, true)]
    [InlineData(SubscriptionStatus.Cancelled, false)]
    [InlineData(SubscriptionStatus.Expired, false)]
    public void IsActiveLike_matches_expected_statuses(SubscriptionStatus status, bool expected)
    {
        Assert.Equal(expected, Subscription.IsActiveLike(status));
    }

    [Fact]
    public void StartTrial_never_uses_a_hardcoded_ninety_day_duration()
    {
        var configuredDuration = TimeSpan.FromDays(45);
        var (plan, version, trial) = CreatePosCatalog(configuredDuration);
        var sub = Subscription.StartTrial(PlatformOrganizationId.New(), plan, version, trial, T0);
        Assert.Equal(T0.Add(configuredDuration), sub.TrialEndUtc);
        Assert.NotEqual(T0.Add(TimeSpan.FromDays(90)), sub.TrialEndUtc);
    }
}
