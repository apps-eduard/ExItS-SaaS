using ExItS.Platform.Application.Common;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Catalog;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Products;

namespace ExItS.Platform.Application.Catalog;

public sealed class CreateProduct
{
    private readonly IProductRepository _products;
    private readonly IClock _clock;

    public CreateProduct(IProductRepository products, IClock clock)
    {
        _products = products;
        _clock = clock;
    }

    public async Task<ApplicationResult<Product>> ExecuteAsync(
        string productCode,
        string displayName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var code = ProductCode.Create(productCode);
            var existing = await _products.GetByCodeAsync(code, cancellationToken).ConfigureAwait(false);
            if (existing is not null)
            {
                return ApplicationResult<Product>.Failure(
                    ApplicationErrorCodes.DuplicateProductCode,
                    "A product with this ProductCode already exists.");
            }

            var product = Product.Create(code, displayName, _clock.UtcNow);
            await _products.AddAsync(product, cancellationToken).ConfigureAwait(false);
            return ApplicationResult<Product>.Success(product);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<Product>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class CreatePlan
{
    private readonly IProductRepository _products;
    private readonly IPlanRepository _plans;
    private readonly IClock _clock;

    public CreatePlan(IProductRepository products, IPlanRepository plans, IClock clock)
    {
        _products = products;
        _plans = plans;
        _clock = clock;
    }

    public async Task<ApplicationResult<Plan>> ExecuteAsync(
        string productCode,
        string planCode,
        string displayName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var pc = ProductCode.Create(productCode);
            var product = await _products.GetByCodeAsync(pc, cancellationToken).ConfigureAwait(false);
            if (product is null)
            {
                return ApplicationResult<Plan>.Failure(ApplicationErrorCodes.ProductNotFound, "Product was not found.");
            }

            var code = PlanCode.Create(planCode);
            var existing = await _plans.GetByProductAndCodeAsync(pc, code, cancellationToken).ConfigureAwait(false);
            if (existing is not null)
            {
                return ApplicationResult<Plan>.Failure(
                    ApplicationErrorCodes.DuplicatePlanCode,
                    "A plan with this code already exists for the product.");
            }

            var plan = Plan.CreateDraft(pc, code, displayName, _clock.UtcNow);
            await _plans.AddAsync(plan, cancellationToken).ConfigureAwait(false);
            return ApplicationResult<Plan>.Success(plan);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<Plan>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class PublishPlanVersion
{
    private readonly IPlanRepository _plans;
    private readonly IFeatureDefinitionRepository _features;
    private readonly IClock _clock;

    public PublishPlanVersion(IPlanRepository plans, IFeatureDefinitionRepository features, IClock clock)
    {
        _plans = plans;
        _features = features;
        _clock = clock;
    }

    public async Task<ApplicationResult<PlanVersion>> ExecuteAsync(
        PlanId planId,
        int versionNumber,
        BillingPeriod billingPeriod,
        bool trialEligible,
        IReadOnlyList<FeatureGrantSpec> grants,
        CancellationToken cancellationToken = default)
    {
        var plan = await _plans.GetByIdAsync(planId, cancellationToken).ConfigureAwait(false);
        if (plan is null)
        {
            return ApplicationResult<PlanVersion>.Failure(ApplicationErrorCodes.PlanNotFound, "Plan was not found.");
        }

        try
        {
            foreach (var grant in grants)
            {
                var feature = await _features
                    .GetByProductAndCodeAsync(plan.ProductCode, grant.FeatureCode, cancellationToken)
                    .ConfigureAwait(false);
                if (feature is null)
                {
                    return ApplicationResult<PlanVersion>.Failure(
                        ApplicationErrorCodes.FeatureNotFound,
                        $"Feature '{grant.FeatureCode}' was not found for product.");
                }

                feature.EnsureAssignable();
                if (feature.ValueType == FeatureValueType.Boolean && grant.NumericLimit is not null)
                {
                    return ApplicationResult<PlanVersion>.Failure(
                        DomainErrorCodes.InvalidEntitlementLimit,
                        "Boolean features must not carry a numeric limit.");
                }
            }

            var max = await _plans.GetMaxVersionNumberAsync(planId, cancellationToken).ConfigureAwait(false);
            if (versionNumber <= max)
            {
                return ApplicationResult<PlanVersion>.Failure(
                    DomainErrorCodes.InvalidPlanVersionNumber,
                    "Plan version number must increase.");
            }

            var utcNow = _clock.UtcNow;
            var version = PlanVersion.CreateDraft(
                plan,
                versionNumber,
                utcNow,
                billingPeriod,
                trialEligible,
                grants,
                utcNow);
            version.Publish(utcNow);
            await _plans.AddVersionAsync(version, cancellationToken).ConfigureAwait(false);
            return ApplicationResult<PlanVersion>.Success(version);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<PlanVersion>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}
