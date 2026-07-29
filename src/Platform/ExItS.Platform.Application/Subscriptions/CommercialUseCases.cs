using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Catalog;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Entitlements;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Products;
using ExItS.Platform.Domain.Subscriptions;

namespace ExItS.Platform.Application.Subscriptions;

public sealed class StartTrialSubscription
{
    private readonly IPlatformOrganizationRepository _organizations;
    private readonly IProductRepository _products;
    private readonly IPlanRepository _plans;
    private readonly ITrialDefinitionRepository _trials;
    private readonly ISubscriptionRepository _subscriptions;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public StartTrialSubscription(
        IPlatformOrganizationRepository organizations,
        IProductRepository products,
        IPlanRepository plans,
        ITrialDefinitionRepository trials,
        ISubscriptionRepository subscriptions,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _organizations = organizations;
        _products = products;
        _plans = plans;
        _trials = trials;
        _subscriptions = subscriptions;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<Subscription>> ExecuteAsync(
        PlatformOrganizationId organizationId,
        PlanId planId,
        PlanVersionId planVersionId,
        TrialDefinitionId trialDefinitionId,
        CancellationToken cancellationToken = default)
    {
        var organization = await _organizations.GetByIdAsync(organizationId, cancellationToken).ConfigureAwait(false);
        if (organization is null)
        {
            return ApplicationResult<Subscription>.Failure(
                ApplicationErrorCodes.OrganizationNotFound,
                "Platform Organization was not found.");
        }

        if (organization.Status != OrganizationStatus.Active)
        {
            return ApplicationResult<Subscription>.Failure(
                ApplicationErrorCodes.OrganizationNotEligible,
                "Trials can only be started for an active Platform Organization.");
        }

        var plan = await _plans.GetByIdAsync(planId, cancellationToken).ConfigureAwait(false);
        if (plan is null)
        {
            return ApplicationResult<Subscription>.Failure(ApplicationErrorCodes.PlanNotFound, "Plan was not found.");
        }

        var product = await _products.GetByCodeAsync(plan.ProductCode, cancellationToken).ConfigureAwait(false);
        if (product is null)
        {
            return ApplicationResult<Subscription>.Failure(
                ApplicationErrorCodes.ProductNotFound,
                "Product was not found.");
        }

        if (product.Status != ProductStatus.Active)
        {
            return ApplicationResult<Subscription>.Failure(
                ApplicationErrorCodes.ProductNotActive,
                "Trials can only be started for an active product.");
        }

        var version = await _plans.GetVersionByIdAsync(planVersionId, cancellationToken).ConfigureAwait(false);
        if (version is null)
        {
            return ApplicationResult<Subscription>.Failure(
                ApplicationErrorCodes.PlanVersionNotFound,
                "Plan version was not found.");
        }

        var trial = await _trials.GetByIdAsync(trialDefinitionId, cancellationToken).ConfigureAwait(false);
        if (trial is null)
        {
            return ApplicationResult<Subscription>.Failure(
                ApplicationErrorCodes.TrialNotFound,
                "Trial definition was not found.");
        }

        var hasActiveLike = await _subscriptions
            .ExistsActiveLikeAsync(organizationId, plan.ProductCode, cancellationToken)
            .ConfigureAwait(false);
        if (hasActiveLike)
        {
            return ApplicationResult<Subscription>.Failure(
                ApplicationErrorCodes.ActiveSubscriptionConflict,
                "An active-like subscription already exists for this organization and product.");
        }

        try
        {
            var subscription = Subscription.StartTrial(organizationId, plan, version, trial, _clock.UtcNow);
            await _subscriptions.AddAsync(subscription, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<Subscription>.Success(subscription);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<Subscription>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<Subscription>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class ActivateSubscription
{
    private readonly ISubscriptionRepository _subscriptions;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public ActivateSubscription(ISubscriptionRepository subscriptions, IPlatformUnitOfWork unitOfWork, IClock clock)
    {
        _subscriptions = subscriptions;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<Subscription>> ExecuteAsync(
        SubscriptionId subscriptionId,
        DateTimeOffset periodStartUtc,
        DateTimeOffset periodEndUtc,
        CancellationToken cancellationToken = default)
    {
        var subscription = await _subscriptions.GetByIdAsync(subscriptionId, cancellationToken).ConfigureAwait(false);
        if (subscription is null)
        {
            return ApplicationResult<Subscription>.Failure(
                ApplicationErrorCodes.SubscriptionNotFound,
                "Subscription was not found.");
        }

        try
        {
            subscription.ActivateFromTrial(periodStartUtc, periodEndUtc, _clock.UtcNow);
            await _subscriptions.UpdateAsync(subscription, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<Subscription>.Success(subscription);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<Subscription>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<Subscription>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class EnterSubscriptionGracePeriod
{
    private readonly ISubscriptionRepository _subscriptions;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public EnterSubscriptionGracePeriod(
        ISubscriptionRepository subscriptions,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _subscriptions = subscriptions;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<Subscription>> ExecuteAsync(
        SubscriptionId subscriptionId,
        DateTimeOffset gracePeriodEndUtc,
        CancellationToken cancellationToken = default)
    {
        var subscription = await _subscriptions.GetByIdAsync(subscriptionId, cancellationToken).ConfigureAwait(false);
        if (subscription is null)
        {
            return ApplicationResult<Subscription>.Failure(
                ApplicationErrorCodes.SubscriptionNotFound,
                "Subscription was not found.");
        }

        try
        {
            subscription.EnterGracePeriod(gracePeriodEndUtc, _clock.UtcNow);
            await _subscriptions.UpdateAsync(subscription, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<Subscription>.Success(subscription);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<Subscription>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<Subscription>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class MarkSubscriptionPastDue
{
    private readonly ISubscriptionRepository _subscriptions;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public MarkSubscriptionPastDue(ISubscriptionRepository subscriptions, IPlatformUnitOfWork unitOfWork, IClock clock)
    {
        _subscriptions = subscriptions;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<Subscription>> ExecuteAsync(
        SubscriptionId subscriptionId,
        CancellationToken cancellationToken = default)
    {
        var subscription = await _subscriptions.GetByIdAsync(subscriptionId, cancellationToken).ConfigureAwait(false);
        if (subscription is null)
        {
            return ApplicationResult<Subscription>.Failure(
                ApplicationErrorCodes.SubscriptionNotFound,
                "Subscription was not found.");
        }

        try
        {
            subscription.MarkPastDue(_clock.UtcNow);
            await _subscriptions.UpdateAsync(subscription, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<Subscription>.Success(subscription);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<Subscription>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<Subscription>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class SuspendSubscription
{
    private readonly ISubscriptionRepository _subscriptions;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public SuspendSubscription(ISubscriptionRepository subscriptions, IPlatformUnitOfWork unitOfWork, IClock clock)
    {
        _subscriptions = subscriptions;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<Subscription>> ExecuteAsync(
        SubscriptionId subscriptionId,
        CancellationToken cancellationToken = default)
    {
        var subscription = await _subscriptions.GetByIdAsync(subscriptionId, cancellationToken).ConfigureAwait(false);
        if (subscription is null)
        {
            return ApplicationResult<Subscription>.Failure(
                ApplicationErrorCodes.SubscriptionNotFound,
                "Subscription was not found.");
        }

        try
        {
            subscription.Suspend(_clock.UtcNow);
            await _subscriptions.UpdateAsync(subscription, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<Subscription>.Success(subscription);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<Subscription>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<Subscription>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class ReactivateSubscription
{
    private readonly ISubscriptionRepository _subscriptions;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public ReactivateSubscription(ISubscriptionRepository subscriptions, IPlatformUnitOfWork unitOfWork, IClock clock)
    {
        _subscriptions = subscriptions;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<Subscription>> ExecuteAsync(
        SubscriptionId subscriptionId,
        DateTimeOffset? periodStartUtc = null,
        DateTimeOffset? periodEndUtc = null,
        CancellationToken cancellationToken = default)
    {
        var subscription = await _subscriptions.GetByIdAsync(subscriptionId, cancellationToken).ConfigureAwait(false);
        if (subscription is null)
        {
            return ApplicationResult<Subscription>.Failure(
                ApplicationErrorCodes.SubscriptionNotFound,
                "Subscription was not found.");
        }

        try
        {
            subscription.Reactivate(_clock.UtcNow, periodStartUtc, periodEndUtc);
            await _subscriptions.UpdateAsync(subscription, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<Subscription>.Success(subscription);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<Subscription>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<Subscription>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class CancelSubscription
{
    private readonly ISubscriptionRepository _subscriptions;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public CancelSubscription(ISubscriptionRepository subscriptions, IPlatformUnitOfWork unitOfWork, IClock clock)
    {
        _subscriptions = subscriptions;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<Subscription>> ExecuteAsync(
        SubscriptionId subscriptionId,
        CancellationToken cancellationToken = default)
    {
        var subscription = await _subscriptions.GetByIdAsync(subscriptionId, cancellationToken).ConfigureAwait(false);
        if (subscription is null)
        {
            return ApplicationResult<Subscription>.Failure(
                ApplicationErrorCodes.SubscriptionNotFound,
                "Subscription was not found.");
        }

        try
        {
            subscription.Cancel(_clock.UtcNow);
            await _subscriptions.UpdateAsync(subscription, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<Subscription>.Success(subscription);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<Subscription>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<Subscription>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class ExpireSubscription
{
    private readonly ISubscriptionRepository _subscriptions;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public ExpireSubscription(ISubscriptionRepository subscriptions, IPlatformUnitOfWork unitOfWork, IClock clock)
    {
        _subscriptions = subscriptions;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<Subscription>> ExecuteAsync(
        SubscriptionId subscriptionId,
        CancellationToken cancellationToken = default)
    {
        var subscription = await _subscriptions.GetByIdAsync(subscriptionId, cancellationToken).ConfigureAwait(false);
        if (subscription is null)
        {
            return ApplicationResult<Subscription>.Failure(
                ApplicationErrorCodes.SubscriptionNotFound,
                "Subscription was not found.");
        }

        try
        {
            subscription.Expire(_clock.UtcNow);
            await _subscriptions.UpdateAsync(subscription, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<Subscription>.Success(subscription);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<Subscription>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<Subscription>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class CreateFeatureOverride
{
    private readonly IFeatureDefinitionRepository _features;
    private readonly IFeatureOverrideRepository _overrides;
    private readonly IClock _clock;

    public CreateFeatureOverride(
        IFeatureDefinitionRepository features,
        IFeatureOverrideRepository overrides,
        IClock clock)
    {
        _features = features;
        _overrides = overrides;
        _clock = clock;
    }

    public async Task<ApplicationResult<FeatureOverride>> ExecuteAsync(
        PlatformOrganizationId organizationId,
        Domain.Products.ProductCode productCode,
        FeatureCode featureCode,
        bool enabled,
        string reason,
        PlatformUserId createdByUserId,
        int? numericLimit = null,
        DateTimeOffset? expiresAtUtc = null,
        CancellationToken cancellationToken = default)
    {
        var feature = await _features
            .GetByProductAndCodeAsync(productCode, featureCode, cancellationToken)
            .ConfigureAwait(false);
        if (feature is null)
        {
            return ApplicationResult<FeatureOverride>.Failure(
                ApplicationErrorCodes.FeatureNotFound,
                "Feature was not found.");
        }

        try
        {
            var ov = FeatureOverride.Create(
                organizationId,
                productCode,
                feature,
                enabled,
                reason,
                createdByUserId,
                _clock.UtcNow,
                numericLimit,
                expiresAtUtc);
            await _overrides.AddAsync(ov, cancellationToken).ConfigureAwait(false);
            return ApplicationResult<FeatureOverride>.Success(ov);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<FeatureOverride>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class RevokeFeatureOverride
{
    private readonly IFeatureOverrideRepository _overrides;
    private readonly IClock _clock;

    public RevokeFeatureOverride(IFeatureOverrideRepository overrides, IClock clock)
    {
        _overrides = overrides;
        _clock = clock;
    }

    public async Task<ApplicationResult<FeatureOverride>> ExecuteAsync(
        FeatureOverrideId overrideId,
        CancellationToken cancellationToken = default)
    {
        var ov = await _overrides.GetByIdAsync(overrideId, cancellationToken).ConfigureAwait(false);
        if (ov is null)
        {
            return ApplicationResult<FeatureOverride>.Failure(
                ApplicationErrorCodes.FeatureOverrideNotFound,
                "Feature override was not found.");
        }

        try
        {
            ov.Revoke(_clock.UtcNow);
            await _overrides.UpdateAsync(ov, cancellationToken).ConfigureAwait(false);
            return ApplicationResult<FeatureOverride>.Success(ov);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<FeatureOverride>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class GenerateEntitlementSnapshot
{
    private readonly ISubscriptionRepository _subscriptions;
    private readonly IPlanRepository _plans;
    private readonly ITrialDefinitionRepository _trials;
    private readonly IFeatureOverrideRepository _overrides;
    private readonly IEntitlementSnapshotRepository _snapshots;
    private readonly IClock _clock;

    public GenerateEntitlementSnapshot(
        ISubscriptionRepository subscriptions,
        IPlanRepository plans,
        ITrialDefinitionRepository trials,
        IFeatureOverrideRepository overrides,
        IEntitlementSnapshotRepository snapshots,
        IClock clock)
    {
        _subscriptions = subscriptions;
        _plans = plans;
        _trials = trials;
        _overrides = overrides;
        _snapshots = snapshots;
        _clock = clock;
    }

    public async Task<ApplicationResult<EntitlementSnapshot>> ExecuteAsync(
        SubscriptionId subscriptionId,
        int? expectedNextVersion = null,
        CancellationToken cancellationToken = default)
    {
        var subscription = await _subscriptions.GetByIdAsync(subscriptionId, cancellationToken).ConfigureAwait(false);
        if (subscription is null)
        {
            return ApplicationResult<EntitlementSnapshot>.Failure(
                ApplicationErrorCodes.SubscriptionNotFound,
                "Subscription was not found.");
        }

        var plan = await _plans.GetByIdAsync(subscription.PlanId, cancellationToken).ConfigureAwait(false);
        if (plan is null)
        {
            return ApplicationResult<EntitlementSnapshot>.Failure(
                ApplicationErrorCodes.PlanNotFound,
                "Plan was not found.");
        }

        var version = await _plans.GetVersionByIdAsync(subscription.PlanVersionId, cancellationToken)
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
            trial = await _trials.GetByIdAsync(subscription.TrialDefinitionId, cancellationToken)
                .ConfigureAwait(false);
        }

        var utcNow = _clock.UtcNow;
        var activeOverrides = await _overrides
            .ListActiveForOrganizationProductAsync(
                subscription.OrganizationId,
                subscription.ProductCode,
                utcNow,
                cancellationToken)
            .ConfigureAwait(false);

        var latest = await _snapshots
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
            var snapshot = new EntitlementSnapshotComposer().Compose(
                subscription,
                plan,
                version,
                trial,
                activeOverrides,
                next,
                utcNow);
            await _snapshots.AddAsync(snapshot, cancellationToken).ConfigureAwait(false);
            return ApplicationResult<EntitlementSnapshot>.Success(snapshot);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<EntitlementSnapshot>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}
