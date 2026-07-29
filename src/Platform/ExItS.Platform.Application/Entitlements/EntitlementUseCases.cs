using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Application.Subscriptions;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Catalog;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Entitlements;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Products;
using ExItS.Platform.Domain.Subscriptions;

namespace ExItS.Platform.Application.Entitlements;

public sealed class CreateFeatureOverride
{
    private readonly IPlatformOrganizationRepository _organizations;
    private readonly IFeatureDefinitionRepository _features;
    private readonly IFeatureOverrideRepository _overrides;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public CreateFeatureOverride(
        IPlatformOrganizationRepository organizations,
        IFeatureDefinitionRepository features,
        IFeatureOverrideRepository overrides,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _organizations = organizations;
        _features = features;
        _overrides = overrides;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<FeatureOverride>> ExecuteAsync(
        PlatformOrganizationId organizationId,
        ProductCode productCode,
        FeatureCode featureCode,
        bool enabled,
        string reason,
        PlatformUserId createdByUserId,
        int? numericLimit = null,
        DateTimeOffset? expiresAtUtc = null,
        CancellationToken cancellationToken = default)
    {
        var organization = await _organizations.GetByIdAsync(organizationId, cancellationToken).ConfigureAwait(false);
        if (organization is null)
        {
            return ApplicationResult<FeatureOverride>.Failure(
                ApplicationErrorCodes.OrganizationNotFound,
                "Platform Organization was not found.");
        }

        var feature = await _features
            .GetByProductAndCodeAsync(productCode, featureCode, cancellationToken)
            .ConfigureAwait(false);
        if (feature is null)
        {
            return ApplicationResult<FeatureOverride>.Failure(
                ApplicationErrorCodes.FeatureNotFound,
                "Feature was not found.");
        }

        var utcNow = _clock.UtcNow;
        var existingActive = await _overrides
            .ListActiveForOrganizationProductAsync(organizationId, productCode, utcNow, cancellationToken)
            .ConfigureAwait(false);
        if (existingActive.Any(o => o.FeatureCode == featureCode))
        {
            return ApplicationResult<FeatureOverride>.Failure(
                ApplicationErrorCodes.FeatureOverrideConflict,
                "An active override already exists for this feature; revoke it before creating a new one.");
        }

        try
        {
            var featureOverride = FeatureOverride.Create(
                organizationId,
                productCode,
                feature,
                enabled,
                reason,
                createdByUserId,
                utcNow,
                numericLimit,
                expiresAtUtc);
            await _overrides.AddAsync(featureOverride, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<FeatureOverride>.Success(featureOverride);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<FeatureOverride>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<FeatureOverride>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class RevokeFeatureOverride
{
    private readonly IFeatureOverrideRepository _overrides;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public RevokeFeatureOverride(
        IFeatureOverrideRepository overrides,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _overrides = overrides;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<FeatureOverride>> ExecuteAsync(
        FeatureOverrideId overrideId,
        string reason,
        PlatformUserId revokedByUserId,
        CancellationToken cancellationToken = default)
    {
        var featureOverride = await _overrides.GetByIdAsync(overrideId, cancellationToken).ConfigureAwait(false);
        if (featureOverride is null)
        {
            return ApplicationResult<FeatureOverride>.Failure(
                ApplicationErrorCodes.FeatureOverrideNotFound,
                "Feature override was not found.");
        }

        try
        {
            featureOverride.Revoke(reason, revokedByUserId, _clock.UtcNow);
            await _overrides.UpdateAsync(featureOverride, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<FeatureOverride>.Success(featureOverride);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<FeatureOverride>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<FeatureOverride>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

/// <summary>
/// Shared implementation for generating the next authoritative entitlement snapshot for an
/// organization/product. Used directly by <see cref="GenerateEntitlementSnapshot"/> and by
/// <see cref="ReconcileEntitlementSnapshot"/>, which is the same generation flow issued on-demand
/// for out-of-band reconciliation (no outbox/broker involved at this phase).
/// </summary>
internal static class EntitlementSnapshotGenerator
{
    public static async Task<ApplicationResult<EntitlementSnapshot>> GenerateAsync(
        ISubscriptionRepository subscriptions,
        IPlanRepository plans,
        ITrialDefinitionRepository trials,
        IFeatureOverrideRepository overrides,
        IEntitlementSnapshotRepository snapshots,
        IEntitlementRefreshPolicy refreshPolicy,
        IPlatformUnitOfWork unitOfWork,
        PlatformOrganizationId organizationId,
        ProductCode productCode,
        DateTimeOffset utcNow,
        int? expectedNextVersion,
        CancellationToken cancellationToken)
    {
        var subscription = await subscriptions
            .GetCurrentForOrganizationProductAsync(organizationId, productCode, cancellationToken)
            .ConfigureAwait(false);
        if (subscription is null)
        {
            return ApplicationResult<EntitlementSnapshot>.Failure(
                ApplicationErrorCodes.SubscriptionNotFound,
                "No subscription was found for this organization and product.");
        }

        return await GenerateForSubscriptionAsync(
            plans,
            trials,
            overrides,
            snapshots,
            refreshPolicy,
            unitOfWork,
            subscription,
            utcNow,
            expectedNextVersion,
            cancellationToken).ConfigureAwait(false);
    }

    public static async Task<ApplicationResult<EntitlementSnapshot>> GenerateAsync(
        ISubscriptionRepository subscriptions,
        IPlanRepository plans,
        ITrialDefinitionRepository trials,
        IFeatureOverrideRepository overrides,
        IEntitlementSnapshotRepository snapshots,
        IEntitlementRefreshPolicy refreshPolicy,
        IPlatformUnitOfWork unitOfWork,
        SubscriptionId subscriptionId,
        DateTimeOffset utcNow,
        int? expectedNextVersion,
        CancellationToken cancellationToken)
    {
        var subscription = await subscriptions.GetByIdAsync(subscriptionId, cancellationToken).ConfigureAwait(false);
        if (subscription is null)
        {
            return ApplicationResult<EntitlementSnapshot>.Failure(
                ApplicationErrorCodes.SubscriptionNotFound,
                "Subscription was not found.");
        }

        return await GenerateForSubscriptionAsync(
            plans,
            trials,
            overrides,
            snapshots,
            refreshPolicy,
            unitOfWork,
            subscription,
            utcNow,
            expectedNextVersion,
            cancellationToken).ConfigureAwait(false);
    }

    private static async Task<ApplicationResult<EntitlementSnapshot>> GenerateForSubscriptionAsync(
        IPlanRepository plans,
        ITrialDefinitionRepository trials,
        IFeatureOverrideRepository overrides,
        IEntitlementSnapshotRepository snapshots,
        IEntitlementRefreshPolicy refreshPolicy,
        IPlatformUnitOfWork unitOfWork,
        Subscription subscription,
        DateTimeOffset utcNow,
        int? expectedNextVersion,
        CancellationToken cancellationToken)
    {
        var plan = await plans.GetByIdAsync(subscription.PlanId, cancellationToken).ConfigureAwait(false);
        if (plan is null)
        {
            return ApplicationResult<EntitlementSnapshot>.Failure(
                ApplicationErrorCodes.PlanNotFound,
                "Plan was not found.");
        }

        var version = await plans.GetVersionByIdAsync(subscription.PlanVersionId, cancellationToken)
            .ConfigureAwait(false);
        if (version is null)
        {
            return ApplicationResult<EntitlementSnapshot>.Failure(
                ApplicationErrorCodes.PlanVersionNotFound,
                "Plan version was not found.");
        }

        TrialDefinition? trial = null;
        if (subscription.TrialDefinitionId is not null)
        {
            trial = await trials.GetByIdAsync(subscription.TrialDefinitionId, cancellationToken)
                .ConfigureAwait(false);
        }

        var activeOverrides = await overrides
            .ListActiveForOrganizationProductAsync(
                subscription.OrganizationId,
                subscription.ProductCode,
                utcNow,
                cancellationToken)
            .ConfigureAwait(false);

        var latest = await snapshots
            .GetLatestSnapshotVersionAsync(subscription.OrganizationId, subscription.ProductCode, cancellationToken)
            .ConfigureAwait(false);
        var next = (latest ?? 0) + 1;
        if (expectedNextVersion is not null && expectedNextVersion.Value != next)
        {
            return ApplicationResult<EntitlementSnapshot>.Failure(
                ApplicationErrorCodes.SnapshotVersionConflict,
                "Entitlement snapshot version conflict.");
        }

        try
        {
            var refreshWindow = refreshPolicy.GetRefreshWindow(subscription.Status);
            var expiresAtUtc = refreshPolicy.GetOptionalExpiryUtc(subscription.Status, utcNow);
            var snapshot = new EntitlementSnapshotComposer().Compose(
                subscription,
                plan,
                version,
                trial,
                activeOverrides,
                next,
                utcNow,
                refreshWindow);
            if (expiresAtUtc is not null)
            {
                snapshot = EntitlementSnapshot.Create(
                    snapshot.OrganizationId,
                    snapshot.ProductCode,
                    snapshot.SubscriptionId,
                    snapshot.PlanCode,
                    snapshot.PlanVersionNumber,
                    snapshot.SnapshotVersion,
                    snapshot.SubscriptionStatus,
                    snapshot.InGracePeriod,
                    snapshot.GeneratedAtUtc,
                    snapshot.EffectiveAtUtc,
                    snapshot.RefreshByUtc,
                    snapshot.SourceAggregateVersion,
                    snapshot.Grants,
                    id: snapshot.Id,
                    expiresAtUtc: expiresAtUtc);
            }

            await snapshots.AddAsync(snapshot, cancellationToken).ConfigureAwait(false);
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<EntitlementSnapshot>.Success(snapshot);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<EntitlementSnapshot>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<EntitlementSnapshot>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

/// <summary>
/// Generates the next authoritative entitlement snapshot version for an organization/product (or
/// directly for a known subscription). No broker, outbox, or product-local projection is involved
/// at this phase; downstream delivery is out of scope.
/// </summary>
public sealed class GenerateEntitlementSnapshot
{
    private readonly ISubscriptionRepository _subscriptions;
    private readonly IPlanRepository _plans;
    private readonly ITrialDefinitionRepository _trials;
    private readonly IFeatureOverrideRepository _overrides;
    private readonly IEntitlementSnapshotRepository _snapshots;
    private readonly IEntitlementRefreshPolicy _refreshPolicy;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public GenerateEntitlementSnapshot(
        ISubscriptionRepository subscriptions,
        IPlanRepository plans,
        ITrialDefinitionRepository trials,
        IFeatureOverrideRepository overrides,
        IEntitlementSnapshotRepository snapshots,
        IEntitlementRefreshPolicy refreshPolicy,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _subscriptions = subscriptions;
        _plans = plans;
        _trials = trials;
        _overrides = overrides;
        _snapshots = snapshots;
        _refreshPolicy = refreshPolicy;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    /// <summary>Preferred entry point: resolves the organization's current subscription for the product.</summary>
    public Task<ApplicationResult<EntitlementSnapshot>> ExecuteAsync(
        PlatformOrganizationId organizationId,
        ProductCode productCode,
        int? expectedNextVersion = null,
        CancellationToken cancellationToken = default) =>
        EntitlementSnapshotGenerator.GenerateAsync(
            _subscriptions,
            _plans,
            _trials,
            _overrides,
            _snapshots,
            _refreshPolicy,
            _unitOfWork,
            organizationId,
            productCode,
            _clock.UtcNow,
            expectedNextVersion,
            cancellationToken);

    /// <summary>Generates directly from a known subscription id.</summary>
    public Task<ApplicationResult<EntitlementSnapshot>> ExecuteAsync(
        SubscriptionId subscriptionId,
        int? expectedNextVersion = null,
        CancellationToken cancellationToken = default) =>
        EntitlementSnapshotGenerator.GenerateAsync(
            _subscriptions,
            _plans,
            _trials,
            _overrides,
            _snapshots,
            _refreshPolicy,
            _unitOfWork,
            subscriptionId,
            _clock.UtcNow,
            expectedNextVersion,
            cancellationToken);
}

/// <summary>
/// Issues a fresh entitlement snapshot version on demand (e.g. after an out-of-band correction).
/// Reconciliation always creates a brand-new snapshot version; historical snapshots are never
/// mutated. The reconciliation reason is accepted for audit/response purposes only — it is not
/// persisted as a separate outbox/event record at this phase.
/// </summary>
public sealed class ReconcileEntitlementSnapshot
{
    private readonly ISubscriptionRepository _subscriptions;
    private readonly IPlanRepository _plans;
    private readonly ITrialDefinitionRepository _trials;
    private readonly IFeatureOverrideRepository _overrides;
    private readonly IEntitlementSnapshotRepository _snapshots;
    private readonly IEntitlementRefreshPolicy _refreshPolicy;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public ReconcileEntitlementSnapshot(
        ISubscriptionRepository subscriptions,
        IPlanRepository plans,
        ITrialDefinitionRepository trials,
        IFeatureOverrideRepository overrides,
        IEntitlementSnapshotRepository snapshots,
        IEntitlementRefreshPolicy refreshPolicy,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _subscriptions = subscriptions;
        _plans = plans;
        _trials = trials;
        _overrides = overrides;
        _snapshots = snapshots;
        _refreshPolicy = refreshPolicy;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public Task<ApplicationResult<EntitlementSnapshot>> ExecuteAsync(
        PlatformOrganizationId organizationId,
        ProductCode productCode,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        _ = reason; // Audit-only for this phase; no outbox/event persistence.
        return EntitlementSnapshotGenerator.GenerateAsync(
            _subscriptions,
            _plans,
            _trials,
            _overrides,
            _snapshots,
            _refreshPolicy,
            _unitOfWork,
            organizationId,
            productCode,
            _clock.UtcNow,
            expectedNextVersion: null,
            cancellationToken);
    }
}
