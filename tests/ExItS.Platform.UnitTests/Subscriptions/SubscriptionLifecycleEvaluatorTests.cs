using ExItS.Platform.Domain.Catalog;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Products;
using ExItS.Platform.Domain.Subscriptions;
using ExItS.Platform.UnitTests.Support;

namespace ExItS.Platform.UnitTests.Subscriptions;

public sealed class SubscriptionLifecycleEvaluatorTests
{
    private static readonly DateTimeOffset T0 = new(2026, 7, 29, 12, 0, 0, TimeSpan.Zero);

    private static (Plan plan, PlanVersion version, TrialDefinition trial) CreatePosCatalog(TimeSpan trialDuration)
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
        var trial = UtangTrialTestFactory.CreateConfigured(T0, trialDuration, plan.Id);
        return (plan, version, trial);
    }

    [Fact]
    public void Trialing_subscription_past_trial_end_suggests_ExpireTrial()
    {
        var (plan, version, trial) = CreatePosCatalog(TimeSpan.FromDays(14));
        var sub = Subscription.StartTrial(PlatformOrganizationId.New(), plan, version, trial, T0);

        var beforeEnd = SubscriptionLifecycleEvaluator.Evaluate(sub, T0.AddDays(13));
        Assert.Equal(SubscriptionLifecycleAction.None, beforeEnd);

        var afterEnd = SubscriptionLifecycleEvaluator.Evaluate(sub, T0.AddDays(15));
        Assert.Equal(SubscriptionLifecycleAction.ExpireTrial, afterEnd);
    }

    [Fact]
    public void Active_subscription_with_lapsed_paid_period_is_not_auto_expired()
    {
        var (plan, version, trial) = CreatePosCatalog(TimeSpan.FromDays(14));
        var sub = Subscription.StartTrial(PlatformOrganizationId.New(), plan, version, trial, T0);
        sub.ActivateFromTrial(T0, T0.AddDays(30), T0.AddMinutes(1));

        var result = SubscriptionLifecycleEvaluator.Evaluate(sub, T0.AddDays(45));
        Assert.Equal(SubscriptionLifecycleAction.None, result);
    }

    [Fact]
    public void GracePeriod_subscription_past_grace_end_suggests_PastDue()
    {
        var (plan, version, trial) = CreatePosCatalog(TimeSpan.FromDays(14));
        var sub = Subscription.StartTrial(PlatformOrganizationId.New(), plan, version, trial, T0);
        sub.ActivateFromTrial(T0, T0.AddDays(30), T0.AddMinutes(1));
        sub.EnterGracePeriod(T0.AddDays(37), T0.AddMinutes(2));

        var withinGrace = SubscriptionLifecycleEvaluator.Evaluate(sub, T0.AddDays(35));
        Assert.Equal(SubscriptionLifecycleAction.None, withinGrace);

        var pastGrace = SubscriptionLifecycleEvaluator.Evaluate(sub, T0.AddDays(38));
        Assert.Equal(SubscriptionLifecycleAction.SuggestPastDue, pastGrace);
    }

    [Theory]
    [InlineData(SubscriptionStatus.PastDue)]
    [InlineData(SubscriptionStatus.Suspended)]
    [InlineData(SubscriptionStatus.Cancelled)]
    [InlineData(SubscriptionStatus.Expired)]
    public void Other_statuses_yield_None(SubscriptionStatus status)
    {
        var (plan, version, trial) = CreatePosCatalog(TimeSpan.FromDays(14));
        var sub = Subscription.StartTrial(PlatformOrganizationId.New(), plan, version, trial, T0);
        sub.ActivateFromTrial(T0, T0.AddDays(30), T0.AddMinutes(1));

        switch (status)
        {
            case SubscriptionStatus.PastDue:
                sub.MarkPastDue(T0.AddMinutes(2));
                break;
            case SubscriptionStatus.Suspended:
                sub.Suspend(T0.AddMinutes(2));
                break;
            case SubscriptionStatus.Cancelled:
                sub.Cancel(T0.AddMinutes(2));
                break;
            case SubscriptionStatus.Expired:
                sub.Expire(T0.AddMinutes(2));
                break;
        }

        var result = SubscriptionLifecycleEvaluator.Evaluate(sub, T0.AddYears(1));
        Assert.Equal(SubscriptionLifecycleAction.None, result);
    }

    [Fact]
    public void Evaluate_is_pure_and_does_not_mutate_the_subscription()
    {
        var (plan, version, trial) = CreatePosCatalog(TimeSpan.FromDays(14));
        var sub = Subscription.StartTrial(PlatformOrganizationId.New(), plan, version, trial, T0);
        var versionBefore = sub.Version;
        var statusBefore = sub.Status;

        SubscriptionLifecycleEvaluator.Evaluate(sub, T0.AddDays(30));

        Assert.Equal(versionBefore, sub.Version);
        Assert.Equal(statusBefore, sub.Status);
    }
}
