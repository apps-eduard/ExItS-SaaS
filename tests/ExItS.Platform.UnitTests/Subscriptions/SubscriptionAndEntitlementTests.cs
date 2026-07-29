using ExItS.Platform.Domain.Catalog;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Entitlements;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Products;
using ExItS.Platform.Domain.Subscriptions;
using ExItS.Platform.UnitTests.Support;

namespace ExItS.Platform.UnitTests.Subscriptions;

public sealed class SubscriptionAndEntitlementTests
{
    private static readonly DateTimeOffset T0 = new(2026, 7, 29, 12, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan ConfiguredTrialDuration = TimeSpan.FromDays(14);

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
        var trial = UtangTrialTestFactory.CreateConfigured(
            T0,
            trialDuration ?? ConfiguredTrialDuration,
            plan.Id);
        return (plan, version, trial);
    }

    [Fact]
    public void Subscription_lifecycle_transitions()
    {
        var (plan, version, trial) = CreatePosCatalog();
        var org = PlatformOrganizationId.New();
        var sub = Subscription.StartTrial(org, plan, version, trial, T0);
        Assert.Equal(SubscriptionStatus.Trialing, sub.Status);
        Assert.Equal(T0, sub.TrialStartUtc);
        Assert.Equal(T0.Add(ConfiguredTrialDuration), sub.TrialEndUtc);

        sub.ActivateFromTrial(T0, T0.AddDays(30), T0.AddMinutes(1));
        Assert.Equal(SubscriptionStatus.Active, sub.Status);

        sub.EnterGracePeriod(T0.AddDays(7), T0.AddMinutes(2));
        sub.MarkPastDue(T0.AddMinutes(3));
        sub.Suspend(T0.AddMinutes(4));
        sub.Reactivate(T0.AddMinutes(5));
        Assert.Equal(SubscriptionStatus.Active, sub.Status);
        sub.Cancel(T0.AddMinutes(6));
        Assert.Throws<DomainException>(() => sub.Reactivate(T0.AddMinutes(7)));
    }

    [Fact]
    public void Subscription_trial_end_follows_configured_duration_not_fixed_ninety_days()
    {
        var duration = TimeSpan.FromDays(21);
        var (plan, version, trial) = CreatePosCatalog(duration);
        var sub = Subscription.StartTrial(PlatformOrganizationId.New(), plan, version, trial, T0);
        Assert.Equal(duration, trial.Duration);
        Assert.Equal(T0.Add(duration), sub.TrialEndUtc);
        Assert.NotEqual(T0.AddDays(90), sub.TrialEndUtc);
    }

    [Fact]
    public void Subscription_expire_is_terminal_and_rejects_product_mismatch()
    {
        var (plan, version, trial) = CreatePosCatalog();
        var sub = Subscription.StartTrial(PlatformOrganizationId.New(), plan, version, trial, T0);
        sub.Expire(T0.AddMinutes(1));
        Assert.Equal(SubscriptionStatus.Expired, sub.Status);
        Assert.Throws<DomainException>(() => sub.ActivateFromTrial(T0, T0.AddDays(1), T0.AddMinutes(2)));

        var otherPlan = Plan.CreateDraft(ProductCode.Create("healthcare"), PlanCode.Create("hc-basic"), "HC", T0);
        Assert.Throws<DomainException>(() =>
            Subscription.StartTrial(PlatformOrganizationId.New(), otherPlan, version, trial, T0));
    }

    [Fact]
    public void FeatureOverride_requires_reason_and_creator_and_can_revoke()
    {
        var product = ProductCode.Create(ProductCode.PinoyBusinessPos);
        var feature = FeatureDefinition.Create(
            product,
            FeatureCode.Create(FeatureCode.CustomerCreditCreate),
            "Create Credit",
            FeatureValueType.Boolean,
            T0);

        Assert.Throws<DomainException>(() =>
            FeatureOverride.Create(
                PlatformOrganizationId.New(),
                product,
                feature,
                true,
                " ",
                PlatformUserId.New(),
                T0));

        var ov = FeatureOverride.Create(
            PlatformOrganizationId.New(),
            product,
            feature,
            enabled: false,
            reason: "Compliance hold",
            createdByUserId: PlatformUserId.New(),
            utcNow: T0,
            expiresAtUtc: T0.AddDays(1));
        Assert.True(ov.IsActiveAt(T0.AddHours(1)));
        Assert.False(ov.IsActiveAt(T0.AddDays(2)));
        ov.Revoke(T0.AddHours(2));
        Assert.False(ov.IsActiveAt(T0.AddHours(3)));
    }

    [Fact]
    public void EntitlementSnapshot_trial_expiry_blocks_new_credit_keeps_view_and_repay()
    {
        var (plan, version, trial) = CreatePosCatalog(TimeSpan.FromDays(10));
        var sub = Subscription.StartTrial(PlatformOrganizationId.New(), plan, version, trial, T0);
        Assert.Equal(T0.AddDays(10), sub.TrialEndUtc);
        sub.Expire(T0.AddMinutes(1));

        var snapshot = new EntitlementSnapshotComposer().Compose(
            sub, plan, version, trial, Array.Empty<FeatureOverride>(), nextSnapshotVersion: 1, utcNow: T0.AddMinutes(2));

        Assert.Equal(SubscriptionStatus.Expired, snapshot.SubscriptionStatus);
        Assert.Contains(snapshot.Grants, g => g.FeatureCode.Value == FeatureCode.CustomerCreditView && g.Enabled);
        Assert.Contains(snapshot.Grants, g => g.FeatureCode.Value == FeatureCode.CustomerCreditRepay && g.Enabled);
        Assert.Contains(snapshot.Grants, g => g.FeatureCode.Value == FeatureCode.CustomerCreditCreate && !g.Enabled);
    }

    [Fact]
    public void EntitlementSnapshot_override_precedence_and_expired_override_ignored()
    {
        var (plan, version, trial) = CreatePosCatalog();
        var org = PlatformOrganizationId.New();
        var sub = Subscription.StartTrial(org, plan, version, trial, T0);
        var product = ProductCode.Create(ProductCode.PinoyBusinessPos);
        var feature = FeatureDefinition.Create(
            product,
            FeatureCode.Create(FeatureCode.CustomerCreditCreate),
            "Create Credit",
            FeatureValueType.Boolean,
            T0);

        var activeOverride = FeatureOverride.Create(
            org, product, feature, enabled: false, reason: "Temp block",
            createdByUserId: PlatformUserId.New(), utcNow: T0, expiresAtUtc: T0.AddDays(1));

        var expiredOverride = FeatureOverride.Create(
            org, product, feature, enabled: false, reason: "Old block",
            createdByUserId: PlatformUserId.New(), utcNow: T0.AddDays(-2), expiresAtUtc: T0.AddDays(-1));

        // expiredOverride.IsActiveAt(T0) is false — composer ignores it
        var snapshot = new EntitlementSnapshotComposer().Compose(
            sub, plan, version, trial, new[] { activeOverride, expiredOverride }, 1, T0);

        var create = Assert.Single(snapshot.Grants, g => g.FeatureCode.Value == FeatureCode.CustomerCreditCreate);
        Assert.False(create.Enabled);
        Assert.Equal(EntitlementGrantSource.Override, create.Source);
    }

    [Fact]
    public void EntitlementSnapshot_rejects_duplicate_features_and_non_positive_version()
    {
        var grant = new EntitlementGrant(
            FeatureCode.Create("max-users"),
            true,
            EntitlementGrantSource.Plan,
            T0,
            5);
        Assert.Throws<DomainException>(() =>
            EntitlementSnapshot.Create(
                PlatformOrganizationId.New(),
                ProductCode.Create("healthcare"),
                SubscriptionId.New(),
                PlanCode.Create("basic"),
                1,
                0,
                SubscriptionStatus.Active,
                false,
                T0, T0, T0.AddHours(1),
                1,
                new[] { grant }));

        Assert.Throws<DomainException>(() =>
            EntitlementSnapshot.Create(
                PlatformOrganizationId.New(),
                ProductCode.Create("healthcare"),
                SubscriptionId.New(),
                PlanCode.Create("basic"),
                1,
                1,
                SubscriptionStatus.Active,
                false,
                T0, T0, T0.AddHours(1),
                1,
                new[] { grant, grant }));
    }
}
