using System.Reflection;
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

        sub.EnterGracePeriod(T0.AddDays(37), T0.AddMinutes(2));
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

        var otherPlan = Plan.CreateDraft(ProductCode.Create("other-product"), PlanCode.Create("op-basic"), "Other", T0);
        Assert.Throws<DomainException>(() =>
            Subscription.StartTrial(PlatformOrganizationId.New(), otherPlan, version, trial, T0));
    }

    [Fact]
    public void StartTrial_rejects_trial_definition_bound_to_a_different_plan()
    {
        var (plan, version, _) = CreatePosCatalog();
        var otherPlan = Plan.CreateDraft(
            ProductCode.Create(ProductCode.PinoyBusinessPos),
            PlanCode.Create("growth-other"),
            "Growth Other",
            T0);
        otherPlan.Activate(T0);
        var otherTrial = UtangTrialTestFactory.CreateConfigured(T0, ConfiguredTrialDuration, otherPlan.Id);

        var ex = Assert.Throws<DomainException>(() =>
            Subscription.StartTrial(PlatformOrganizationId.New(), plan, version, otherTrial, T0));
        Assert.Equal(DomainErrorCodes.ProductMismatch, ex.ErrorCode);
        Assert.Contains("belong", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void StartTrial_accepts_matching_plan_specific_trial()
    {
        var (plan, version, trial) = CreatePosCatalog();
        var sub = Subscription.StartTrial(PlatformOrganizationId.New(), plan, version, trial, T0);
        Assert.Equal(SubscriptionStatus.Trialing, sub.Status);
        Assert.Equal(plan.Id, sub.PlanId);
        Assert.Equal(trial.Id, sub.TrialDefinitionId);
        Assert.Equal(version.Id, sub.PlanVersionId);
    }

    [Fact]
    public void StartTrial_accepts_product_wide_trial_with_null_plan_id()
    {
        var (plan, version, _) = CreatePosCatalog();
        var productWide = UtangTrialTestFactory.CreateConfigured(T0, ConfiguredTrialDuration, planId: null);
        var sub = Subscription.StartTrial(PlatformOrganizationId.New(), plan, version, productWide, T0);
        Assert.Equal(SubscriptionStatus.Trialing, sub.Status);
        Assert.Equal(productWide.Id, sub.TrialDefinitionId);
        Assert.Null(productWide.PlanId);
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

        Assert.Throws<DomainException>(() => ov.Revoke(" ", PlatformUserId.New(), T0.AddHours(2)));

        var revokedBy = PlatformUserId.New();
        ov.Revoke("No longer required", revokedBy, T0.AddHours(2));
        Assert.False(ov.IsActiveAt(T0.AddHours(3)));
        Assert.Equal(FeatureOverrideStatus.Revoked, ov.Status);
        Assert.Equal(T0.AddHours(2), ov.RevokedAtUtc);
        Assert.Equal(revokedBy, ov.RevokedByUserId);
        Assert.Equal("No longer required", ov.RevocationReason);

        // Idempotent: re-revoking is a same-state no-op that keeps the original metadata.
        ov.Revoke("Different reason", PlatformUserId.New(), T0.AddHours(4));
        Assert.Equal(T0.AddHours(2), ov.RevokedAtUtc);
        Assert.Equal(revokedBy, ov.RevokedByUserId);
        Assert.Equal("No longer required", ov.RevocationReason);
    }

    [Fact]
    public void FeatureOverride_rehydrate_reconstructs_full_persisted_state()
    {
        var product = ProductCode.Create(ProductCode.PinoyBusinessPos);
        var id = FeatureOverrideId.New();
        var createdBy = PlatformUserId.New();
        var revokedBy = PlatformUserId.New();

        var rehydrated = FeatureOverride.Rehydrate(
            id,
            PlatformOrganizationId.New(),
            product,
            FeatureCode.Create(FeatureCode.CustomerCreditCreate),
            enabled: false,
            numericLimit: null,
            reason: "Persisted reason",
            effectiveFromUtc: T0,
            expiresAtUtc: T0.AddDays(5),
            status: FeatureOverrideStatus.Revoked,
            createdAtUtc: T0,
            createdByUserId: createdBy,
            updatedAtUtc: T0.AddHours(1),
            revokedAtUtc: T0.AddHours(1),
            revokedByUserId: revokedBy,
            revocationReason: "Persisted revocation");

        Assert.Equal(id, rehydrated.Id);
        Assert.Equal(FeatureOverrideStatus.Revoked, rehydrated.Status);
        Assert.Equal(createdBy, rehydrated.CreatedByUserId);
        Assert.Equal(revokedBy, rehydrated.RevokedByUserId);
        Assert.Equal("Persisted revocation", rehydrated.RevocationReason);
        Assert.Equal(T0.AddHours(1), rehydrated.RevokedAtUtc);
    }

    [Theory]
    [InlineData(FeatureValueType.Boolean, 5)]
    public void FeatureOverride_rejects_numeric_limit_on_boolean_features(FeatureValueType valueType, int limit)
    {
        var product = ProductCode.Create(ProductCode.PinoyBusinessPos);
        var feature = FeatureDefinition.Create(
            product, FeatureCode.Create(FeatureCode.CustomerCreditCreate), "Create Credit", valueType, T0);

        Assert.Throws<DomainException>(() =>
            FeatureOverride.Create(
                PlatformOrganizationId.New(), product, feature, true, "reason", PlatformUserId.New(), T0, limit));
    }

    [Fact]
    public void FeatureOverride_rejects_negative_numeric_limit()
    {
        var product = ProductCode.Create(ProductCode.PinoyBusinessPos);
        var feature = FeatureDefinition.Create(
            product, FeatureCode.Create("max-credit"), "Max Credit", FeatureValueType.NumericLimit, T0);

        Assert.Throws<DomainException>(() =>
            FeatureOverride.Create(
                PlatformOrganizationId.New(), product, feature, true, "reason", PlatformUserId.New(), T0, -1));
    }

    [Fact]
    public void FeatureOverride_numeric_limit_feature_requires_a_limit()
    {
        var product = ProductCode.Create(ProductCode.PinoyBusinessPos);
        var feature = FeatureDefinition.Create(
            product, FeatureCode.Create("max-credit"), "Max Credit", FeatureValueType.NumericLimit, T0);

        Assert.Throws<DomainException>(() =>
            FeatureOverride.Create(
                PlatformOrganizationId.New(), product, feature, true, "reason", PlatformUserId.New(), T0));

        var withLimit = FeatureOverride.Create(
            PlatformOrganizationId.New(), product, feature, true, "reason", PlatformUserId.New(), T0, 10);
        Assert.Equal(10, withLimit.NumericLimit);
    }

    [Fact]
    public void EntitlementSnapshot_rehydrate_reconstructs_grants_and_metadata()
    {
        var grant = new EntitlementGrant(
            FeatureCode.Create(FeatureCode.CustomerCreditView), true, EntitlementGrantSource.Plan, T0, 5);
        var id = EntitlementSnapshotId.New();

        var rehydrated = EntitlementSnapshot.Rehydrate(
            id,
            PlatformOrganizationId.New(),
            ProductCode.Create(ProductCode.PinoyBusinessPos),
            SubscriptionId.New(),
            PlanCode.Create("utang"),
            planVersionNumber: 1,
            snapshotVersion: 3,
            schemaVersion: 1,
            subscriptionStatus: SubscriptionStatus.Active,
            inGracePeriod: false,
            generatedAtUtc: T0,
            effectiveAtUtc: T0,
            refreshByUtc: T0.AddHours(24),
            expiresAtUtc: null,
            sourceAggregateVersion: 2,
            grants: new[] { grant });

        Assert.Equal(id, rehydrated.Id);
        Assert.Equal(3, rehydrated.SnapshotVersion);
        Assert.Single(rehydrated.Grants);
        Assert.Equal(FeatureCode.CustomerCreditView, rehydrated.Grants[0].FeatureCode.Value);
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
                ProductCode.Create("other-product"),
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
                ProductCode.Create("other-product"),
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

    [Fact]
    public void EntitlementSnapshot_pastdue_subscription_disables_create_but_keeps_view_and_repay()
    {
        var (plan, version, trial) = CreatePosCatalog();
        var sub = Subscription.StartTrial(PlatformOrganizationId.New(), plan, version, trial, T0);
        sub.ActivateFromTrial(T0, T0.AddDays(30), T0.AddMinutes(1));
        sub.EnterGracePeriod(T0.AddDays(37), T0.AddMinutes(2));
        sub.MarkPastDue(T0.AddMinutes(3));

        var snapshot = new EntitlementSnapshotComposer().Compose(
            sub, plan, version, trial, Array.Empty<FeatureOverride>(), nextSnapshotVersion: 1, utcNow: T0.AddMinutes(4));

        Assert.Equal(SubscriptionStatus.PastDue, snapshot.SubscriptionStatus);
        Assert.Contains(snapshot.Grants, g => g.FeatureCode.Value == FeatureCode.CustomerCreditCreate && !g.Enabled);
        Assert.Contains(snapshot.Grants, g => g.FeatureCode.Value == FeatureCode.CustomerCreditRepay && g.Enabled);
        Assert.Contains(snapshot.Grants, g => g.FeatureCode.Value == FeatureCode.CustomerCreditView && g.Enabled);
    }

    [Fact]
    public void EntitlementSnapshot_grace_period_keeps_base_plan_grants_unrestricted()
    {
        var (plan, version, trial) = CreatePosCatalog();
        var sub = Subscription.StartTrial(PlatformOrganizationId.New(), plan, version, trial, T0);
        sub.ActivateFromTrial(T0, T0.AddDays(30), T0.AddMinutes(1));
        sub.EnterGracePeriod(T0.AddDays(37), T0.AddMinutes(2));

        var snapshot = new EntitlementSnapshotComposer().Compose(
            sub, plan, version, trial, Array.Empty<FeatureOverride>(), nextSnapshotVersion: 1, utcNow: T0.AddMinutes(3));

        Assert.Equal(SubscriptionStatus.GracePeriod, snapshot.SubscriptionStatus);
        Assert.True(snapshot.InGracePeriod);
        // Grace period leaves base (plan) grants untouched: only PastDue/Suspended/Cancelled restrict create.
        Assert.Contains(snapshot.Grants, g => g.FeatureCode.Value == FeatureCode.CustomerCreditCreate && g.Enabled);
        Assert.Contains(snapshot.Grants, g => g.FeatureCode.Value == FeatureCode.CustomerCreditView && g.Enabled);
    }

    [Fact]
    public void EntitlementSnapshot_cancelled_subscription_disables_all_but_view_and_repay()
    {
        var (plan, version, trial) = CreatePosCatalog();
        var sub = Subscription.StartTrial(PlatformOrganizationId.New(), plan, version, trial, T0);
        sub.ActivateFromTrial(T0, T0.AddDays(30), T0.AddMinutes(1));
        sub.Cancel(T0.AddMinutes(2));

        var snapshot = new EntitlementSnapshotComposer().Compose(
            sub, plan, version, trial, Array.Empty<FeatureOverride>(), nextSnapshotVersion: 1, utcNow: T0.AddMinutes(3));

        Assert.Equal(SubscriptionStatus.Cancelled, snapshot.SubscriptionStatus);
        Assert.Contains(snapshot.Grants, g => g.FeatureCode.Value == FeatureCode.CustomerCreditCreate && !g.Enabled);
        Assert.Contains(snapshot.Grants, g => g.FeatureCode.Value == FeatureCode.CustomerCreditView && g.Enabled);
        Assert.Contains(snapshot.Grants, g => g.FeatureCode.Value == FeatureCode.CustomerCreditRepay && g.Enabled);
    }

    [Fact]
    public void EntitlementSnapshot_never_hardcodes_a_ninety_day_refresh_window()
    {
        var (plan, version, trial) = CreatePosCatalog();
        var sub = Subscription.StartTrial(PlatformOrganizationId.New(), plan, version, trial, T0);

        var snapshot = new EntitlementSnapshotComposer().Compose(
            sub, plan, version, trial, Array.Empty<FeatureOverride>(), nextSnapshotVersion: 1, utcNow: T0,
            refreshWindow: TimeSpan.FromHours(6));

        Assert.Equal(T0.AddHours(6), snapshot.RefreshByUtc);
        Assert.NotEqual(T0.AddDays(90), snapshot.RefreshByUtc);
    }

    [Fact]
    public void EntitlementSnapshotComposer_fails_closed_for_an_unsupported_subscription_status()
    {
        var (plan, version, trial) = CreatePosCatalog();
        var sub = Subscription.StartTrial(PlatformOrganizationId.New(), plan, version, trial, T0);
        sub.ActivateFromTrial(T0, T0.AddDays(30), T0.AddMinutes(1));

        // Force an out-of-range status value: the composer must fail closed rather than silently
        // granting full access for a status it does not recognize.
        var invalidStatusField = typeof(Subscription).GetField(
            "<Status>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(invalidStatusField);
        invalidStatusField!.SetValue(sub, (SubscriptionStatus)999);

        var ex = Assert.Throws<DomainException>(() => new EntitlementSnapshotComposer().Compose(
            sub, plan, version, trial, Array.Empty<FeatureOverride>(), 1, T0.AddMinutes(2)));
        Assert.Equal(DomainErrorCodes.UnsupportedSubscriptionStatus, ex.ErrorCode);
    }

    [Fact]
    public void FeatureOverride_IsActiveAt_boundary_conditions()
    {
        var product = ProductCode.Create(ProductCode.PinoyBusinessPos);
        var feature = FeatureDefinition.Create(
            product, FeatureCode.Create(FeatureCode.CustomerCreditCreate), "Create Credit",
            FeatureValueType.Boolean, T0);

        var ov = FeatureOverride.Create(
            PlatformOrganizationId.New(), product, feature, enabled: false, reason: "hold",
            createdByUserId: PlatformUserId.New(), utcNow: T0, expiresAtUtc: T0.AddDays(1));

        Assert.False(ov.IsActiveAt(T0.AddMinutes(-1)));
        Assert.True(ov.IsActiveAt(T0));
        Assert.True(ov.IsActiveAt(T0.AddDays(1).AddMinutes(-1)));
        Assert.False(ov.IsActiveAt(T0.AddDays(1)));
    }

    [Fact]
    public void EntitlementSnapshot_rejects_non_positive_snapshot_version()
    {
        var grant = new EntitlementGrant(
            FeatureCode.Create(FeatureCode.CustomerCreditView), true, EntitlementGrantSource.Plan, T0);

        var ex = Assert.Throws<DomainException>(() => EntitlementSnapshot.Create(
            PlatformOrganizationId.New(), ProductCode.Create(ProductCode.PinoyBusinessPos), SubscriptionId.New(),
            PlanCode.Create("utang"), 1, snapshotVersion: 0, SubscriptionStatus.Active, false, T0, T0,
            T0.AddHours(24), 1, new[] { grant }));

        Assert.Equal(DomainErrorCodes.InvalidSnapshotVersion, ex.ErrorCode);
    }

    [Fact]
    public void EntitlementSnapshot_rejects_duplicate_feature_codes_in_grants()
    {
        var grants = new[]
        {
            new EntitlementGrant(FeatureCode.Create(FeatureCode.CustomerCreditView), true, EntitlementGrantSource.Plan, T0),
            new EntitlementGrant(FeatureCode.Create(FeatureCode.CustomerCreditView), false, EntitlementGrantSource.Override, T0)
        };

        var ex = Assert.Throws<DomainException>(() => EntitlementSnapshot.Create(
            PlatformOrganizationId.New(), ProductCode.Create(ProductCode.PinoyBusinessPos), SubscriptionId.New(),
            PlanCode.Create("utang"), 1, 1, SubscriptionStatus.Active, false, T0, T0, T0.AddHours(24), 1, grants));

        Assert.Equal(DomainErrorCodes.DuplicateFeatureCode, ex.ErrorCode);
    }

    [Fact]
    public void EntitlementSnapshot_expires_at_utc_must_not_precede_effective_time()
    {
        var grant = new EntitlementGrant(
            FeatureCode.Create("max-users"), true, EntitlementGrantSource.Plan, T0, 5);

        Assert.Throws<DomainException>(() =>
            EntitlementSnapshot.Create(
                PlatformOrganizationId.New(),
                ProductCode.Create("other-product"),
                SubscriptionId.New(),
                PlanCode.Create("basic"),
                1,
                1,
                SubscriptionStatus.Active,
                false,
                T0, T0, T0.AddHours(1),
                1,
                new[] { grant },
                expiresAtUtc: T0.AddHours(-1)));

        var withExpiry = EntitlementSnapshot.Create(
            PlatformOrganizationId.New(),
            ProductCode.Create("other-product"),
            SubscriptionId.New(),
            PlanCode.Create("basic"),
            1,
            1,
            SubscriptionStatus.Active,
            false,
            T0, T0, T0.AddHours(1),
            1,
            new[] { grant },
            expiresAtUtc: T0.AddDays(1));
        Assert.Equal(T0.AddDays(1), withExpiry.ExpiresAtUtc);
    }
}
