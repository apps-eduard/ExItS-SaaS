using ExItS.Platform.Domain.Catalog;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Subscriptions;

namespace ExItS.Platform.Domain.Entitlements;

/// <summary>
/// Deterministic entitlement composition. No database access. No clock access.
/// Override precedence: active override replaces plan/trial grant for the same feature.
/// </summary>
public sealed class EntitlementSnapshotComposer
{
    public static readonly TimeSpan DefaultRefreshWindow = TimeSpan.FromHours(24);

    public EntitlementSnapshot Compose(
        Subscription subscription,
        Plan plan,
        PlanVersion planVersion,
        TrialDefinition? trialDefinition,
        IReadOnlyList<FeatureOverride> overrides,
        int nextSnapshotVersion,
        DateTimeOffset utcNow,
        TimeSpan? refreshWindow = null)
    {
        ArgumentNullException.ThrowIfNull(subscription);
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(planVersion);
        ArgumentNullException.ThrowIfNull(overrides);
        DomainTime.EnsureUtc(utcNow);

        if (subscription.ProductCode != plan.ProductCode
            || subscription.ProductCode != planVersion.ProductCode
            || plan.Id != planVersion.PlanId
            || subscription.PlanId != plan.Id
            || subscription.PlanVersionId != planVersion.Id)
        {
            throw new DomainException(
                DomainErrorCodes.ProductMismatch,
                "Subscription, plan, and plan version must be consistent.");
        }

        if (nextSnapshotVersion < 1)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidSnapshotVersion,
                "Snapshot version must be positive.");
        }

        var baseSpecs = ResolveBaseGrants(subscription, planVersion, trialDefinition);
        var map = new Dictionary<string, EntitlementGrant>(StringComparer.Ordinal);

        foreach (var spec in baseSpecs)
        {
            map[spec.FeatureCode.Value] = new EntitlementGrant(
                spec.FeatureCode,
                spec.Enabled,
                subscription.Status == SubscriptionStatus.Trialing
                    ? EntitlementGrantSource.Trial
                    : EntitlementGrantSource.Plan,
                utcNow,
                spec.NumericLimit);
        }

        ApplySubscriptionStatusAdjustments(subscription, map, utcNow);

        foreach (var ov in overrides)
        {
            if (ov.OrganizationId != subscription.OrganizationId
                || ov.ProductCode != subscription.ProductCode)
            {
                continue;
            }

            if (!ov.IsActiveAt(utcNow))
            {
                continue;
            }

            map[ov.FeatureCode.Value] = new EntitlementGrant(
                ov.FeatureCode,
                ov.Enabled,
                EntitlementGrantSource.Override,
                utcNow,
                ov.NumericLimit,
                ov.ExpiresAtUtc);
        }

        var refreshBy = utcNow.Add(refreshWindow ?? DefaultRefreshWindow);
        return EntitlementSnapshot.Create(
            subscription.OrganizationId,
            subscription.ProductCode,
            subscription.Id,
            plan.Code,
            planVersion.VersionNumber,
            nextSnapshotVersion,
            subscription.Status,
            inGracePeriod: subscription.Status == SubscriptionStatus.GracePeriod,
            utcNow,
            utcNow,
            refreshBy,
            subscription.Version,
            map.Values.OrderBy(g => g.FeatureCode.Value, StringComparer.Ordinal).ToList());
    }

    private static IReadOnlyList<FeatureGrantSpec> ResolveBaseGrants(
        Subscription subscription,
        PlanVersion planVersion,
        TrialDefinition? trialDefinition)
    {
        if (subscription.Status == SubscriptionStatus.Trialing)
        {
            if (trialDefinition is null)
            {
                throw new DomainException(
                    DomainErrorCodes.InvalidSubscriptionTransition,
                    "Trialing subscription requires a trial definition for composition.");
            }

            if (trialDefinition.ProductCode != subscription.ProductCode)
            {
                throw new DomainException(DomainErrorCodes.ProductMismatch, "Trial product mismatch.");
            }

            return trialDefinition.FeatureGrants;
        }

        if (subscription.Status == SubscriptionStatus.Expired && trialDefinition is not null)
        {
            return trialDefinition.PostExpiryFeatureGrants;
        }

        return planVersion.Grants;
    }

    private static void ApplySubscriptionStatusAdjustments(
        Subscription subscription,
        Dictionary<string, EntitlementGrant> map,
        DateTimeOffset utcNow)
    {
        if (!Enum.IsDefined(subscription.Status))
        {
            // Fail closed: an unrecognized status must never silently fall through to full access.
            throw new DomainException(
                DomainErrorCodes.UnsupportedSubscriptionStatus,
                $"Subscription status '{subscription.Status}' is not supported for entitlement composition.");
        }

        // GracePeriod keeps whatever the base grants (plan/trial) already provide — per the
        // authorization matrix, grace-period entitlements are "per grace entitlements" and are
        // not further restricted here. Trialing/Active/Expired are likewise handled entirely by
        // ResolveBaseGrants and require no further adjustment.
        if (subscription.Status is not (SubscriptionStatus.PastDue
                or SubscriptionStatus.Suspended
                or SubscriptionStatus.Cancelled))
        {
            return;
        }

        // PastDue/Suspended/Cancelled fail closed on new credit; view/repay may remain for continuity.
        // Create-customer-credit is always disabled once a subscription is no longer current.
        if (map.TryGetValue(FeatureCode.CustomerCreditCreate, out var create))
        {
            map[FeatureCode.CustomerCreditCreate] = new EntitlementGrant(
                create.FeatureCode,
                enabled: false,
                create.Source,
                utcNow,
                create.NumericLimit,
                create.ExpiresAtUtc);
        }

        if (subscription.Status == SubscriptionStatus.Cancelled)
        {
            // Cancelled: disable all enabled grants except explicit view/repay continuity features.
            foreach (var key in map.Keys.ToList())
            {
                if (key is FeatureCode.CustomerCreditView or FeatureCode.CustomerCreditRepay)
                {
                    continue;
                }

                var g = map[key];
                if (g.Enabled)
                {
                    map[key] = new EntitlementGrant(
                        g.FeatureCode,
                        enabled: false,
                        g.Source,
                        utcNow,
                        g.NumericLimit,
                        g.ExpiresAtUtc);
                }
            }
        }
    }
}
