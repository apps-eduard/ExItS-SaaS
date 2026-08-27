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
/// Idempotent Local Validation-only catalog and commercial fixture for Pinoy Buy Now Pay Later.
/// Not production commercial policy. Plan name, zero price, trial duration, and empty grants
/// are test-only schema values required by existing Platform primitives — they do not close
/// BNPL-D-00-04/08/14–18/20/25–26 pricing, settlement, or grant-identifier decisions.
/// </summary>
public sealed class EnsureBnplLocalValidationCatalog
{
    public const string ProductDisplayName = "Pinoy Buy Now Pay Later";

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

    public EnsureBnplLocalValidationCatalog(
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
        var code = ProductCode.Create(ProductCode.PinoyBuyNowPayLater);
        var product = await _products.GetByCodeAsync(code, cancellationToken).ConfigureAwait(false);
        if (product is null)
        {
            var created = await _createProduct
                .ExecuteAsync(ProductCode.PinoyBuyNowPayLater, ProductDisplayName, cancellationToken)
                .ConfigureAwait(false);
            if (!created.IsSuccess)
            {
                product = await _products.GetByCodeAsync(code, cancellationToken).ConfigureAwait(false);
                if (product is null)
                {
                    throw new InvalidOperationException(
                        $"Local validation BNPL product create failed: {created.ErrorCode} {created.ErrorMessage}");
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

        var planCode = PlanCode.Create(LocalValidationOptions.BnplLocalValidationPlanCode);
        var plan = await _plans
            .GetByProductAndCodeAsync(code, planCode, cancellationToken)
            .ConfigureAwait(false);
        if (plan is null)
        {
            var createdPlan = await _createPlan
                .ExecuteAsync(
                    ProductCode.PinoyBuyNowPayLater,
                    LocalValidationOptions.BnplLocalValidationPlanCode,
                    LocalValidationOptions.BnplLocalValidationPlanDisplayName,
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
                        $"Local validation BNPL plan create failed: {createdPlan.ErrorCode} {createdPlan.ErrorMessage}");
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
                    $"Local validation BNPL plan activate failed: {activated.ErrorCode} {activated.ErrorMessage}");
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
                    $"Local validation BNPL plan version publish failed: {publishedResult.ErrorCode} {publishedResult.ErrorMessage}");
            }
        }

        var trials = await _trials.ListByProductAsync(code, cancellationToken).ConfigureAwait(false);
        var trial = trials.FirstOrDefault(t =>
            string.Equals(
                t.DisplayName,
                LocalValidationOptions.BnplLocalValidationTrialDisplayName,
                StringComparison.Ordinal));
        if (trial is null)
        {
            var createdTrial = await _createTrial
                .ExecuteAsync(
                    ProductCode.PinoyBuyNowPayLater,
                    LocalValidationOptions.BnplLocalValidationTrialDisplayName,
                    TimeSpan.FromDays(14),
                    Array.Empty<FeatureGrantSpec>(),
                    Array.Empty<FeatureGrantSpec>(),
                    planId: null,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            if (!createdTrial.IsSuccess || createdTrial.Value is null)
            {
                throw new InvalidOperationException(
                    $"Local validation BNPL trial create failed: {createdTrial.ErrorCode} {createdTrial.ErrorMessage}");
            }
        }
    }

    /// <summary>
    /// Starts an independent BNPL trial subscription and entitlement snapshot for one organization.
    /// Does not reuse POS or PLM subscription, snapshot, or product-access assignment.
    /// </summary>
    public async Task EnsureCommercialAsync(
        PlatformOrganizationId organizationId,
        CancellationToken cancellationToken = default)
    {
        await EnsureReferenceAsync(cancellationToken).ConfigureAwait(false);

        var code = ProductCode.Create(ProductCode.PinoyBuyNowPayLater);
        var plan = await _plans
            .GetByProductAndCodeAsync(
                code,
                PlanCode.Create(LocalValidationOptions.BnplLocalValidationPlanCode),
                cancellationToken)
            .ConfigureAwait(false);
        if (plan is null || plan.Status != PlanStatus.Active)
        {
            throw new InvalidOperationException(
                $"Local validation BNPL plan '{LocalValidationOptions.BnplLocalValidationPlanCode}' was not available.");
        }

        var versions = await _plans.ListVersionsAsync(plan.Id, cancellationToken).ConfigureAwait(false);
        var version = versions
            .Where(v => v.Status == PlanVersionStatus.Published)
            .OrderByDescending(v => v.VersionNumber)
            .FirstOrDefault();
        if (version is null)
        {
            throw new InvalidOperationException("Published Local Validation BNPL plan version was not available.");
        }

        var trials = await _trials.ListByProductAsync(code, cancellationToken).ConfigureAwait(false);
        var trial = trials.FirstOrDefault(t =>
            string.Equals(
                t.DisplayName,
                LocalValidationOptions.BnplLocalValidationTrialDisplayName,
                StringComparison.Ordinal));
        if (trial is null)
        {
            throw new InvalidOperationException("Local validation BNPL trial definition was not available.");
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
                    $"Local validation BNPL trial subscription failed: {started.ErrorCode} {started.ErrorMessage}");
            }
        }

        var snapshot = await _generateSnapshot
            .ExecuteAsync(organizationId, code, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        if (!snapshot.IsSuccess)
        {
            throw new InvalidOperationException(
                $"Local validation BNPL entitlement snapshot failed: {snapshot.ErrorCode} {snapshot.ErrorMessage}");
        }
    }
}
