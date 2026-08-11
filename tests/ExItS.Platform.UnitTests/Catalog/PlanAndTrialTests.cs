using System.Reflection;
using ExItS.Platform.Domain.Catalog;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Products;
using ExItS.Platform.UnitTests.Support;

namespace ExItS.Platform.UnitTests.Catalog;

public sealed class PlanAndTrialTests
{
    private static readonly DateTimeOffset T0 = new(2026, 7, 29, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Plan_draft_activate_retire()
    {
        var plan = Plan.CreateDraft(
            ProductCode.Create(ProductCode.PinoyBusinessPos),
            PlanCode.Create("utang-trial"),
            "Utang Trial",
            T0);
        Assert.Equal(PlanStatus.Draft, plan.Status);
        plan.Activate(T0.AddMinutes(1));
        plan.Retire(T0.AddMinutes(2));
        Assert.Throws<DomainException>(() => plan.Activate(T0.AddMinutes(3)));
    }

    [Fact]
    public void PlanVersion_publish_is_immutable()
    {
        var plan = Plan.CreateDraft(
            ProductCode.Create(ProductCode.PinoyBusinessPos),
            PlanCode.Create("utang"),
            "Utang",
            T0);
        var grants = new[]
        {
            FeatureGrantSpec.Boolean(FeatureCode.Create(FeatureCode.CustomerCreditView), true)
        };
        var version = PlanVersion.CreateDraft(plan, 1, T0, BillingPeriod.Monthly, true, grants, T0);
        version.Publish(T0.AddMinutes(1));
        Assert.Equal(PlanVersionStatus.Published, version.Status);
        var ex = Assert.Throws<DomainException>(() =>
            version.ReplaceDraftGrants(grants, T0.AddMinutes(2)));
        Assert.Equal(DomainErrorCodes.PlanVersionImmutable, ex.ErrorCode);
    }

    [Fact]
    public void PlanVersion_rejects_duplicate_features_and_non_positive_version()
    {
        var plan = Plan.CreateDraft(
            ProductCode.Create("other-product"),
            PlanCode.Create("basic"),
            "Basic",
            T0);
        var code = FeatureCode.Create("max-users");
        var dup = new[] { FeatureGrantSpec.Limit(code, 5), FeatureGrantSpec.Limit(code, 10) };
        Assert.Throws<DomainException>(() =>
            PlanVersion.CreateDraft(plan, 1, T0, BillingPeriod.Monthly, false, dup, T0));
        Assert.Throws<DomainException>(() =>
            PlanVersion.CreateDraft(plan, 0, T0, BillingPeriod.Monthly, false, Array.Empty<FeatureGrantSpec>(), T0));
    }

    [Fact]
    public void TrialDefinition_accepts_configured_positive_duration_and_rejects_non_positive()
    {
        var zero = Assert.Throws<DomainException>(() =>
            TrialDefinition.Create(
                ProductCode.Create(ProductCode.PinoyBusinessPos),
                "Trial",
                TimeSpan.Zero,
                Array.Empty<FeatureGrantSpec>(),
                Array.Empty<FeatureGrantSpec>(),
                T0));
        Assert.Equal(DomainErrorCodes.InvalidTrialDuration, zero.ErrorCode);

        var negative = Assert.Throws<DomainException>(() =>
            TrialDefinition.Create(
                ProductCode.Create(ProductCode.PinoyBusinessPos),
                "Trial",
                TimeSpan.FromDays(-1),
                Array.Empty<FeatureGrantSpec>(),
                Array.Empty<FeatureGrantSpec>(),
                T0));
        Assert.Equal(DomainErrorCodes.InvalidTrialDuration, negative.ErrorCode);

        var fourteenDays = UtangTrialTestFactory.CreateConfigured(T0, TimeSpan.FromDays(14));
        Assert.Equal(TimeSpan.FromDays(14), fourteenDays.Duration);

        var thirtyDays = UtangTrialTestFactory.CreateConfigured(T0, TimeSpan.FromDays(30));
        Assert.Equal(TimeSpan.FromDays(30), thirtyDays.Duration);
        Assert.NotEqual(fourteenDays.Duration, thirtyDays.Duration);
    }

    [Fact]
    public void TrialDefinition_post_expiry_utang_feature_grants_do_not_require_fixed_duration()
    {
        var trial = UtangTrialTestFactory.CreateConfigured(T0, TimeSpan.FromDays(7));
        Assert.Contains(trial.FeatureGrants, g =>
            g.FeatureCode.Value == FeatureCode.CustomerCreditCreate && g.Enabled);
        Assert.Contains(trial.PostExpiryFeatureGrants, g =>
            g.FeatureCode.Value == FeatureCode.CustomerCreditCreate && !g.Enabled);
        Assert.Contains(trial.PostExpiryFeatureGrants, g =>
            g.FeatureCode.Value == FeatureCode.CustomerCreditView && g.Enabled);
        Assert.Contains(trial.PostExpiryFeatureGrants, g =>
            g.FeatureCode.Value == FeatureCode.CustomerCreditRepay && g.Enabled);
    }

    [Fact]
    public void Domain_TrialDefinition_has_no_pinoy_business_pos_duration_helper()
    {
        var helpers = typeof(TrialDefinition).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => m.Name.Contains("Pinoy", StringComparison.OrdinalIgnoreCase)
                        || m.Name.Contains("Utang", StringComparison.OrdinalIgnoreCase)
                        || m.Name.Contains("ThreeMonth", StringComparison.OrdinalIgnoreCase)
                        || m.Name.Contains("90", StringComparison.Ordinal))
            .Select(m => m.Name)
            .ToArray();

        Assert.Empty(helpers);
    }
}
