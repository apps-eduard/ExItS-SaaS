using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Entitlements;
using ExItS.Platform.Application.Subscriptions;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Catalog;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Products;

namespace ExItS.Platform.Application.LocalValidation;

/// <summary>
/// Idempotent Local Validation-only catalog and commercial fixture for Pinoy Pawn Manager.
/// Not production commercial policy. Plan name, zero price, trial duration, and empty grants
/// are test-only schema values required by existing Platform primitives — they do not close
/// PPM-D-00-04/08/18/20, pricing, pawnshop licensing, or grant-identifier decisions.
/// </summary>
public sealed class EnsurePpmLocalValidationCatalog
{
    public const string ProductDisplayName = "Pinoy Pawn Manager";

    private readonly CreateProduct _createProduct;
    private readonly IProductRepository _products;
    private readonly CreatePlan _createPlan;
    private readonly ActivatePlan _activatePlan;
    private readonly PublishPlanVersion _publishPlanVersion;
    private readonly CreateTrialDefinition _createTrial;
    private readonly StartTrialSubscription _startTrial;
    private readonly GenerateEntitlementSnapshot _generateSnapshot;
    private readonly IPlanRepository _plans;
    private readonly ITrialDefinitionRepository _trials;
    private readonly ISubscriptionRepository _subscriptions;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public EnsurePpmLocalValidationCatalog(
        CreateProduct createProduct,
        IProductRepository products,
        CreatePlan createPlan,
        ActivatePlan activatePlan,
        PublishPlanVersion publishPlanVersion,
        CreateTrialDefinition createTrial,
        StartTrialSubscription startTrial,
        GenerateEntitlementSnapshot generateSnapshot,
        IPlanRepository plans,
        ITrialDefinitionRepository trials,
        ISubscriptionRepository subscriptions,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _createProduct = createProduct;
        _products = products;
        _createPlan = createPlan;
        _activatePlan = activatePlan;
        _publishPlanVersion = publishPlanVersion;
        _createTrial = createTrial;
        _startTrial = startTrial;
        _generateSnapshot = generateSnapshot;
        _plans = plans;
        _trials = trials;
        _subscriptions = subscriptions;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task EnsureReferenceAsync(CancellationToken cancellationToken = default)
    {
        var code = ProductCode.Create(ProductCode.PinoyPawnManager);
        var product = await _products.GetByCodeAsync(code, cancellationToken).ConfigureAwait(false);
        if (product is null)
        {
            var created = await _createProduct
                .ExecuteAsync(ProductCode.PinoyPawnManager, ProductDisplayName, cancellationToken)
                .ConfigureAwait(false);
            if (!created.IsSuccess)
            {
                product = await _products.GetByCodeAsync(code, cancellationToken).ConfigureAwait(false);
                if (product is null)
                {
                    throw new InvalidOperationException(
                        $"Local validation PPM product create failed: {created.ErrorCode} {created.ErrorMessage}");
                }
            }
            else
            {
                product = created.Value;
            }
        }

        if (product is not null
            && !string.Equals(product.DisplayName, ProductDisplayName, StringComparison.Ordinal))
        {
            product.Rename(ProductDisplayName, _clock.UtcNow);
            await _products.UpdateAsync(product, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        var planCode = PlanCode.Create(LocalValidationOptions.PpmLocalValidationPlanCode);
        var plan = await _plans
            .GetByProductAndCodeAsync(code, planCode, cancellationToken)
            .ConfigureAwait(false);
        if (plan is null)
        {
            var createdPlan = await _createPlan
                .ExecuteAsync(
                    ProductCode.PinoyPawnManager,
                    LocalValidationOptions.PpmLocalValidationPlanCode,
                    LocalValidationOptions.PpmLocalValidationPlanDisplayName,
                    cancellationToken)
                .ConfigureAwait(false);
            if (!createdPlan.IsSuccess || createdPlan.Value is null)
            {
                plan = await _plans
                    .GetByProductAndCodeAsync(code, planCode, cancellationToken)
                    .ConfigureAwait(false);
                if (plan is null)
                {
                    throw new InvalidOperationException(
                        $"Local validation PPM plan create failed: {createdPlan.ErrorCode} {createdPlan.ErrorMessage}");
                }
            }
            else
            {
                plan = createdPlan.Value;
            }
        }

        if (plan.Status == PlanStatus.Draft)
        {
            var activated = await _activatePlan.ExecuteAsync(plan.Id, cancellationToken).ConfigureAwait(false);
            if (!activated.IsSuccess || activated.Value is null)
            {
                throw new InvalidOperationException(
                    $"Local validation PPM plan activate failed: {activated.ErrorCode} {activated.ErrorMessage}");
            }

            plan = activated.Value;
        }

        var versions = await _plans.ListVersionsAsync(plan.Id, cancellationToken).ConfigureAwait(false);
        var published = versions
            .Where(v => v.Status == PlanVersionStatus.Published)
            .OrderByDescending(v => v.VersionNumber)
            .FirstOrDefault();
        if (published is null)
        {
            var nextVersion = await _plans.GetMaxVersionNumberAsync(plan.Id, cancellationToken).ConfigureAwait(false) + 1;
            var publishedResult = await _publishPlanVersion
                .ExecuteAsync(
                    plan.Id,
                    nextVersion,
                    BillingPeriod.None,
                    trialEligible: true,
                    Array.Empty<FeatureGrantSpec>(),
                    cancellationToken)
                .ConfigureAwait(false);
            if (!publishedResult.IsSuccess)
            {
                throw new InvalidOperationException(
                    $"Local validation PPM plan version publish failed: {publishedResult.ErrorCode} {publishedResult.ErrorMessage}");
            }
        }

        var trials = await _trials.ListByProductAsync(code, cancellationToken).ConfigureAwait(false);
        var trial = trials.FirstOrDefault(t =>
            string.Equals(
                t.DisplayName,
                LocalValidationOptions.PpmLocalValidationTrialDisplayName,
                StringComparison.Ordinal));
        if (trial is null)
        {
            var createdTrial = await _createTrial
                .ExecuteAsync(
                    ProductCode.PinoyPawnManager,
                    LocalValidationOptions.PpmLocalValidationTrialDisplayName,
                    TimeSpan.FromDays(14),
                    Array.Empty<FeatureGrantSpec>(),
                    Array.Empty<FeatureGrantSpec>(),
                    planId: null,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            if (!createdTrial.IsSuccess || createdTrial.Value is null)
            {
                throw new InvalidOperationException(
                    $"Local validation PPM trial create failed: {createdTrial.ErrorCode} {createdTrial.ErrorMessage}");
            }
        }
    }

    /// <summary>
    /// Starts an independent PPM trial subscription and entitlement snapshot for one organization.
    /// Does not reuse POS, PLM, or BNPL subscription, snapshot, or product-access assignment.
    /// </summary>
    public async Task EnsureCommercialAsync(
        PlatformOrganizationId organizationId,
        CancellationToken cancellationToken = default)
    {
        await EnsureReferenceAsync(cancellationToken).ConfigureAwait(false);

        var code = ProductCode.Create(ProductCode.PinoyPawnManager);
        var plan = await _plans
            .GetByProductAndCodeAsync(
                code,
                PlanCode.Create(LocalValidationOptions.PpmLocalValidationPlanCode),
                cancellationToken)
            .ConfigureAwait(false);
        if (plan is null || plan.Status != PlanStatus.Active)
        {
            throw new InvalidOperationException(
                $"Local validation PPM plan '{LocalValidationOptions.PpmLocalValidationPlanCode}' was not available.");
        }

        var versions = await _plans.ListVersionsAsync(plan.Id, cancellationToken).ConfigureAwait(false);
        var version = versions
            .Where(v => v.Status == PlanVersionStatus.Published)
            .OrderByDescending(v => v.VersionNumber)
            .FirstOrDefault();
        if (version is null)
        {
            throw new InvalidOperationException("Published Local Validation PPM plan version was not available.");
        }

        var trials = await _trials.ListByProductAsync(code, cancellationToken).ConfigureAwait(false);
        var trial = trials.FirstOrDefault(t =>
            string.Equals(
                t.DisplayName,
                LocalValidationOptions.PpmLocalValidationTrialDisplayName,
                StringComparison.Ordinal));
        if (trial is null)
        {
            throw new InvalidOperationException("Local validation PPM trial definition was not available.");
        }

        var subscription = await _subscriptions
            .GetCurrentForOrganizationProductAsync(organizationId, code, cancellationToken)
            .ConfigureAwait(false);
        if (subscription is null)
        {
            var started = await _startTrial
                .ExecuteAsync(organizationId, plan.Id, version.Id, trial.Id, cancellationToken)
                .ConfigureAwait(false);
            if (!started.IsSuccess || started.Value is null)
            {
                throw new InvalidOperationException(
                    $"Local validation PPM trial subscription failed: {started.ErrorCode} {started.ErrorMessage}");
            }
        }

        var snapshot = await _generateSnapshot
            .ExecuteAsync(organizationId, code, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        if (!snapshot.IsSuccess)
        {
            throw new InvalidOperationException(
                $"Local validation PPM entitlement snapshot failed: {snapshot.ErrorCode} {snapshot.ErrorMessage}");
        }
    }
}
