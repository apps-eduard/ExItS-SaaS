using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Catalog;
using ExItS.Platform.Domain.Common;
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

        if (!plan.AcceptsNewSubscriptions)
        {
            return ApplicationResult<Subscription>.Failure(
                ApplicationErrorCodes.SubscriptionIneligible,
                plan.Status == PlanStatus.Retired
                    ? "Retired plans cannot accept new subscriptions."
                    : "Trials can only be started for an active plan.");
        }

        if (!plan.TrialAllowed)
        {
            return ApplicationResult<Subscription>.Failure(
                ApplicationErrorCodes.SubscriptionIneligible,
                "This plan does not allow trials.");
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

public sealed class ActivatePaidSubscription
{
    private readonly IPlatformOrganizationRepository _organizations;
    private readonly IProductRepository _products;
    private readonly IPlanRepository _plans;
    private readonly ISubscriptionRepository _subscriptions;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public ActivatePaidSubscription(
        IPlatformOrganizationRepository organizations,
        IProductRepository products,
        IPlanRepository plans,
        ISubscriptionRepository subscriptions,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _organizations = organizations;
        _products = products;
        _plans = plans;
        _subscriptions = subscriptions;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<Subscription>> ExecuteAsync(
        PlatformOrganizationId organizationId,
        PlanId planId,
        PlanVersionId planVersionId,
        DateTimeOffset periodStartUtc,
        DateTimeOffset periodEndUtc,
        BillingCycle billingCycle,
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
                "Paid subscriptions can only be started for an active Platform Organization.");
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
                "Paid subscriptions can only be started for an active product.");
        }

        if (!plan.AcceptsNewSubscriptions)
        {
            return ApplicationResult<Subscription>.Failure(
                ApplicationErrorCodes.SubscriptionIneligible,
                plan.Status == PlanStatus.Retired
                    ? "Retired plans cannot accept new subscriptions."
                    : "Paid subscriptions can only be started for an active plan.");
        }

        var version = await _plans.GetVersionByIdAsync(planVersionId, cancellationToken).ConfigureAwait(false);
        if (version is null)
        {
            return ApplicationResult<Subscription>.Failure(
                ApplicationErrorCodes.PlanVersionNotFound,
                "Plan version was not found.");
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
            var subscription = Subscription.ActivatePaid(
                organizationId,
                plan,
                version,
                periodStartUtc,
                periodEndUtc,
                billingCycle,
                _clock.UtcNow);
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

    public Task<ApplicationResult<Subscription>> ExecuteAsync(
        SubscriptionId subscriptionId,
        DateTimeOffset periodStartUtc,
        DateTimeOffset periodEndUtc,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(subscriptionId, periodStartUtc, periodEndUtc, expectedVersion: null, cancellationToken);

    public async Task<ApplicationResult<Subscription>> ExecuteAsync(
        SubscriptionId subscriptionId,
        DateTimeOffset periodStartUtc,
        DateTimeOffset periodEndUtc,
        int? expectedVersion,
        CancellationToken cancellationToken)
    {
        var subscription = await _subscriptions.GetByIdAsync(subscriptionId, cancellationToken).ConfigureAwait(false);
        if (subscription is null)
        {
            return ApplicationResult<Subscription>.Failure(
                ApplicationErrorCodes.SubscriptionNotFound,
                "Subscription was not found.");
        }

        if (SubscriptionConcurrency.IsMismatch(subscription.Version, expectedVersion))
        {
            return ApplicationResult<Subscription>.Failure(
                ApplicationErrorCodes.ConcurrencyConflict,
                "The subscription was modified by another request. Refresh and try again.");
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
        CancellationToken cancellationToken = default) =>
        await ExecuteAsync(subscriptionId, gracePeriodEndUtc, expectedVersion: null, cancellationToken)
            .ConfigureAwait(false);

    public async Task<ApplicationResult<Subscription>> ExecuteAsync(
        SubscriptionId subscriptionId,
        DateTimeOffset gracePeriodEndUtc,
        int? expectedVersion,
        CancellationToken cancellationToken)
    {
        var subscription = await _subscriptions.GetByIdAsync(subscriptionId, cancellationToken).ConfigureAwait(false);
        if (subscription is null)
        {
            return ApplicationResult<Subscription>.Failure(
                ApplicationErrorCodes.SubscriptionNotFound,
                "Subscription was not found.");
        }

        if (SubscriptionConcurrency.IsMismatch(subscription.Version, expectedVersion))
        {
            return ApplicationResult<Subscription>.Failure(
                ApplicationErrorCodes.ConcurrencyConflict,
                "The subscription was modified by another request. Refresh and try again.");
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

    public Task<ApplicationResult<Subscription>> ExecuteAsync(
        SubscriptionId subscriptionId,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(subscriptionId, expectedVersion: null, cancellationToken);

    public async Task<ApplicationResult<Subscription>> ExecuteAsync(
        SubscriptionId subscriptionId,
        int? expectedVersion,
        CancellationToken cancellationToken)
    {
        var subscription = await _subscriptions.GetByIdAsync(subscriptionId, cancellationToken).ConfigureAwait(false);
        if (subscription is null)
        {
            return ApplicationResult<Subscription>.Failure(
                ApplicationErrorCodes.SubscriptionNotFound,
                "Subscription was not found.");
        }

        if (SubscriptionConcurrency.IsMismatch(subscription.Version, expectedVersion))
        {
            return ApplicationResult<Subscription>.Failure(
                ApplicationErrorCodes.ConcurrencyConflict,
                "The subscription was modified by another request. Refresh and try again.");
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

    public Task<ApplicationResult<Subscription>> ExecuteAsync(
        SubscriptionId subscriptionId,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(subscriptionId, expectedVersion: null, cancellationToken);

    public async Task<ApplicationResult<Subscription>> ExecuteAsync(
        SubscriptionId subscriptionId,
        int? expectedVersion,
        CancellationToken cancellationToken)
    {
        var subscription = await _subscriptions.GetByIdAsync(subscriptionId, cancellationToken).ConfigureAwait(false);
        if (subscription is null)
        {
            return ApplicationResult<Subscription>.Failure(
                ApplicationErrorCodes.SubscriptionNotFound,
                "Subscription was not found.");
        }

        if (SubscriptionConcurrency.IsMismatch(subscription.Version, expectedVersion))
        {
            return ApplicationResult<Subscription>.Failure(
                ApplicationErrorCodes.ConcurrencyConflict,
                "The subscription was modified by another request. Refresh and try again.");
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

    public Task<ApplicationResult<Subscription>> ExecuteAsync(
        SubscriptionId subscriptionId,
        DateTimeOffset? periodStartUtc = null,
        DateTimeOffset? periodEndUtc = null,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(subscriptionId, periodStartUtc, periodEndUtc, expectedVersion: null, cancellationToken);

    public async Task<ApplicationResult<Subscription>> ExecuteAsync(
        SubscriptionId subscriptionId,
        DateTimeOffset? periodStartUtc,
        DateTimeOffset? periodEndUtc,
        int? expectedVersion,
        CancellationToken cancellationToken)
    {
        var subscription = await _subscriptions.GetByIdAsync(subscriptionId, cancellationToken).ConfigureAwait(false);
        if (subscription is null)
        {
            return ApplicationResult<Subscription>.Failure(
                ApplicationErrorCodes.SubscriptionNotFound,
                "Subscription was not found.");
        }

        if (SubscriptionConcurrency.IsMismatch(subscription.Version, expectedVersion))
        {
            return ApplicationResult<Subscription>.Failure(
                ApplicationErrorCodes.ConcurrencyConflict,
                "The subscription was modified by another request. Refresh and try again.");
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

    public Task<ApplicationResult<Subscription>> ExecuteAsync(
        SubscriptionId subscriptionId,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(subscriptionId, expectedVersion: null, cancellationToken);

    public async Task<ApplicationResult<Subscription>> ExecuteAsync(
        SubscriptionId subscriptionId,
        int? expectedVersion,
        CancellationToken cancellationToken)
    {
        var subscription = await _subscriptions.GetByIdAsync(subscriptionId, cancellationToken).ConfigureAwait(false);
        if (subscription is null)
        {
            return ApplicationResult<Subscription>.Failure(
                ApplicationErrorCodes.SubscriptionNotFound,
                "Subscription was not found.");
        }

        if (SubscriptionConcurrency.IsMismatch(subscription.Version, expectedVersion))
        {
            return ApplicationResult<Subscription>.Failure(
                ApplicationErrorCodes.ConcurrencyConflict,
                "The subscription was modified by another request. Refresh and try again.");
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

    public Task<ApplicationResult<Subscription>> ExecuteAsync(
        SubscriptionId subscriptionId,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(subscriptionId, expectedVersion: null, cancellationToken);

    public async Task<ApplicationResult<Subscription>> ExecuteAsync(
        SubscriptionId subscriptionId,
        int? expectedVersion,
        CancellationToken cancellationToken)
    {
        var subscription = await _subscriptions.GetByIdAsync(subscriptionId, cancellationToken).ConfigureAwait(false);
        if (subscription is null)
        {
            return ApplicationResult<Subscription>.Failure(
                ApplicationErrorCodes.SubscriptionNotFound,
                "Subscription was not found.");
        }

        if (SubscriptionConcurrency.IsMismatch(subscription.Version, expectedVersion))
        {
            return ApplicationResult<Subscription>.Failure(
                ApplicationErrorCodes.ConcurrencyConflict,
                "The subscription was modified by another request. Refresh and try again.");
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

internal static class SubscriptionConcurrency
{
    public static bool IsMismatch(int currentVersion, int? expectedVersion) =>
        expectedVersion is not null && expectedVersion.Value != currentVersion;
}

