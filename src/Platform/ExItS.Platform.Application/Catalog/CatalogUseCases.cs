using ExItS.Platform.Application.Common;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Catalog;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.GlobalCatalog;
using ExItS.Platform.Domain.Products;

namespace ExItS.Platform.Application.Catalog;

public sealed class CreateProduct
{
    private readonly IProductRepository _products;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public CreateProduct(IProductRepository products, IPlatformUnitOfWork unitOfWork, IClock clock)
    {
        _products = products;
        _unitOfWork = unitOfWork;
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
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<Product>.Success(product);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<Product>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<Product>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class RenameProduct
{
    private readonly IProductRepository _products;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public RenameProduct(IProductRepository products, IPlatformUnitOfWork unitOfWork, IClock clock)
    {
        _products = products;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public Task<ApplicationResult<Product>> ExecuteAsync(
        ProductId id,
        string displayName,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(id, displayName, expectedUpdatedAtUtc: null, cancellationToken);

    public async Task<ApplicationResult<Product>> ExecuteAsync(
        ProductId id,
        string displayName,
        DateTimeOffset? expectedUpdatedAtUtc,
        CancellationToken cancellationToken)
    {
        var product = await _products.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (product is null)
        {
            return ApplicationResult<Product>.Failure(ApplicationErrorCodes.ProductNotFound, "Product was not found.");
        }

        if (IsConcurrencyMismatch(product.UpdatedAtUtc, expectedUpdatedAtUtc))
        {
            return ApplicationResult<Product>.Failure(
                ApplicationErrorCodes.ConcurrencyConflict,
                "The product was modified by another request. Refresh and try again.");
        }

        try
        {
            product.Rename(displayName, _clock.UtcNow);
            await _products.UpdateAsync(product, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<Product>.Success(product);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<Product>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<Product>.Failure(ex.ErrorCode, ex.Message);
        }
    }

    internal static bool IsConcurrencyMismatch(DateTimeOffset current, DateTimeOffset? expected) =>
        expected is not null
        && current.ToUnixTimeMilliseconds() != expected.Value.ToUnixTimeMilliseconds();
}

public sealed class ActivateProduct
{
    private readonly IProductRepository _products;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public ActivateProduct(IProductRepository products, IPlatformUnitOfWork unitOfWork, IClock clock)
    {
        _products = products;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<Product>> ExecuteAsync(ProductId id, CancellationToken cancellationToken = default)
    {
        var product = await _products.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (product is null)
        {
            return ApplicationResult<Product>.Failure(ApplicationErrorCodes.ProductNotFound, "Product was not found.");
        }

        try
        {
            product.Activate(_clock.UtcNow);
            await _products.UpdateAsync(product, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<Product>.Success(product);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<Product>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class DeactivateProduct
{
    private readonly IProductRepository _products;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public DeactivateProduct(IProductRepository products, IPlatformUnitOfWork unitOfWork, IClock clock)
    {
        _products = products;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<Product>> ExecuteAsync(ProductId id, CancellationToken cancellationToken = default)
    {
        var product = await _products.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (product is null)
        {
            return ApplicationResult<Product>.Failure(ApplicationErrorCodes.ProductNotFound, "Product was not found.");
        }

        try
        {
            product.Deactivate(_clock.UtcNow);
            await _products.UpdateAsync(product, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<Product>.Success(product);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<Product>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class RetireProduct
{
    private readonly IProductRepository _products;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public RetireProduct(IProductRepository products, IPlatformUnitOfWork unitOfWork, IClock clock)
    {
        _products = products;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<Product>> ExecuteAsync(ProductId id, CancellationToken cancellationToken = default)
    {
        var product = await _products.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (product is null)
        {
            return ApplicationResult<Product>.Failure(ApplicationErrorCodes.ProductNotFound, "Product was not found.");
        }

        try
        {
            product.Retire(_clock.UtcNow);
            await _products.UpdateAsync(product, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<Product>.Success(product);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<Product>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class CreateFeatureDefinition
{
    private readonly IProductRepository _products;
    private readonly IFeatureDefinitionRepository _features;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public CreateFeatureDefinition(
        IProductRepository products,
        IFeatureDefinitionRepository features,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _products = products;
        _features = features;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<FeatureDefinition>> ExecuteAsync(
        string productCode,
        string featureCode,
        string displayName,
        FeatureValueType valueType,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var pc = ProductCode.Create(productCode);
            var product = await _products.GetByCodeAsync(pc, cancellationToken).ConfigureAwait(false);
            if (product is null)
            {
                return ApplicationResult<FeatureDefinition>.Failure(
                    ApplicationErrorCodes.ProductNotFound,
                    "Product was not found.");
            }

            var code = FeatureCode.Create(featureCode);
            var existing = await _features.GetByProductAndCodeAsync(pc, code, cancellationToken).ConfigureAwait(false);
            if (existing is not null)
            {
                return ApplicationResult<FeatureDefinition>.Failure(
                    ApplicationErrorCodes.DuplicateFeatureCode,
                    "A feature with this code already exists for the product.");
            }

            var feature = FeatureDefinition.Create(pc, code, displayName, valueType, _clock.UtcNow);
            await _features.AddAsync(feature, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<FeatureDefinition>.Success(feature);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<FeatureDefinition>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<FeatureDefinition>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class RetireFeatureDefinition
{
    private readonly IFeatureDefinitionRepository _features;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public RetireFeatureDefinition(IFeatureDefinitionRepository features, IPlatformUnitOfWork unitOfWork, IClock clock)
    {
        _features = features;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<FeatureDefinition>> ExecuteAsync(
        string productCode,
        string featureCode,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var pc = ProductCode.Create(productCode);
            var code = FeatureCode.Create(featureCode);
            var feature = await _features.GetByProductAndCodeAsync(pc, code, cancellationToken).ConfigureAwait(false);
            if (feature is null)
            {
                return ApplicationResult<FeatureDefinition>.Failure(
                    ApplicationErrorCodes.FeatureNotFound,
                    "Feature was not found.");
            }

            feature.Retire(_clock.UtcNow);
            await _features.UpdateAsync(feature, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<FeatureDefinition>.Success(feature);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<FeatureDefinition>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class CreatePlan
{
    private readonly IProductRepository _products;
    private readonly IPlanRepository _plans;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public CreatePlan(
        IProductRepository products,
        IPlanRepository plans,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _products = products;
        _plans = plans;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public Task<ApplicationResult<Plan>> ExecuteAsync(
        string productCode,
        string planCode,
        string displayName,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            productCode,
            planCode,
            displayName,
            description: null,
            maxBranches: 1,
            maxActiveStaff: 3,
            maxActivePosDevices: 1,
            maxActiveBusinessTypes: 1,
            customerCreditEnabled: false,
            advancedReportsEnabled: false,
            exportEnabled: false,
            trialAllowed: true,
            defaultTrialDays: 14,
            sortOrder: 100,
            monthlyPrice: 0m,
            annualPrice: 0m,
            currencyCode: "PHP",
            cancellationToken);

    public async Task<ApplicationResult<Plan>> ExecuteAsync(
        string productCode,
        string planCode,
        string displayName,
        string? description,
        int maxBranches,
        int maxActiveStaff,
        int maxActivePosDevices,
        int maxActiveBusinessTypes,
        bool customerCreditEnabled,
        bool advancedReportsEnabled,
        bool exportEnabled,
        bool trialAllowed,
        int defaultTrialDays,
        int sortOrder,
        decimal monthlyPrice,
        decimal annualPrice,
        string currencyCode,
        CancellationToken cancellationToken = default,
        int? maxAreas = null)
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

            var plan = Plan.CreateDraft(
                pc,
                code,
                displayName,
                _clock.UtcNow,
                description: description,
                maxBranches: maxBranches,
                maxActiveStaff: maxActiveStaff,
                maxActivePosDevices: maxActivePosDevices,
                maxActiveBusinessTypes: maxActiveBusinessTypes,
                customerCreditEnabled: customerCreditEnabled,
                advancedReportsEnabled: advancedReportsEnabled,
                exportEnabled: exportEnabled,
                trialAllowed: trialAllowed,
                defaultTrialDays: defaultTrialDays,
                sortOrder: sortOrder,
                monthlyPrice: monthlyPrice,
                annualPrice: annualPrice,
                currencyCode: currencyCode,
                maxAreas: maxAreas ?? 1);
            await _plans.AddAsync(plan, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<Plan>.Success(plan);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<Plan>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<Plan>.Failure(ex.ErrorCode, ex.Message);
        }
    }

    public Task<ApplicationResult<Plan>> ExecuteAsync(
        string productCode,
        string planCode,
        string displayName,
        string? description,
        int maxBranches,
        int maxActiveStaff,
        int maxActivePosDevices,
        bool customerCreditEnabled,
        bool advancedReportsEnabled,
        bool exportEnabled,
        bool trialAllowed,
        int defaultTrialDays,
        int sortOrder,
        decimal monthlyPrice,
        decimal annualPrice,
        string currencyCode,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            productCode,
            planCode,
            displayName,
            description,
            maxBranches,
            maxActiveStaff,
            maxActivePosDevices,
            maxActiveBusinessTypes: 1,
            customerCreditEnabled,
            advancedReportsEnabled,
            exportEnabled,
            trialAllowed,
            defaultTrialDays,
            sortOrder,
            monthlyPrice,
            annualPrice,
            currencyCode,
            cancellationToken);

    public Task<ApplicationResult<Plan>> ExecuteAsync(
        string productCode,
        string planCode,
        string displayName,
        string? description,
        int maxBranches,
        int maxActiveStaff,
        bool customerCreditEnabled,
        bool advancedReportsEnabled,
        bool exportEnabled,
        bool trialAllowed,
        int defaultTrialDays,
        int sortOrder,
        decimal monthlyPrice,
        decimal annualPrice,
        string currencyCode,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            productCode,
            planCode,
            displayName,
            description,
            maxBranches,
            maxActiveStaff,
            maxActivePosDevices: 1,
            maxActiveBusinessTypes: 1,
            customerCreditEnabled,
            advancedReportsEnabled,
            exportEnabled,
            trialAllowed,
            defaultTrialDays,
            sortOrder,
            monthlyPrice,
            annualPrice,
            currencyCode,
            cancellationToken);
}

public sealed class RenamePlan
{
    private readonly IPlanRepository _plans;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public RenamePlan(IPlanRepository plans, IPlatformUnitOfWork unitOfWork, IClock clock)
    {
        _plans = plans;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public Task<ApplicationResult<Plan>> ExecuteAsync(
        PlanId id,
        string displayName,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(id, displayName, expectedUpdatedAtUtc: null, cancellationToken);

    public async Task<ApplicationResult<Plan>> ExecuteAsync(
        PlanId id,
        string displayName,
        DateTimeOffset? expectedUpdatedAtUtc,
        CancellationToken cancellationToken)
    {
        var plan = await _plans.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (plan is null)
        {
            return ApplicationResult<Plan>.Failure(ApplicationErrorCodes.PlanNotFound, "Plan was not found.");
        }

        if (RenameProduct.IsConcurrencyMismatch(plan.UpdatedAtUtc, expectedUpdatedAtUtc))
        {
            return ApplicationResult<Plan>.Failure(
                ApplicationErrorCodes.ConcurrencyConflict,
                "The plan was modified by another request. Refresh and try again.");
        }

        try
        {
            plan.Rename(displayName, _clock.UtcNow);
            await _plans.UpdateAsync(plan, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<Plan>.Success(plan);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<Plan>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<Plan>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class ActivatePlan
{
    private readonly IPlanRepository _plans;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public ActivatePlan(IPlanRepository plans, IPlatformUnitOfWork unitOfWork, IClock clock)
    {
        _plans = plans;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<Plan>> ExecuteAsync(PlanId id, CancellationToken cancellationToken = default)
    {
        var plan = await _plans.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (plan is null)
        {
            return ApplicationResult<Plan>.Failure(ApplicationErrorCodes.PlanNotFound, "Plan was not found.");
        }

        try
        {
            plan.Activate(_clock.UtcNow);
            await _plans.UpdateAsync(plan, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<Plan>.Success(plan);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<Plan>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class DeactivatePlan
{
    private readonly IPlanRepository _plans;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public DeactivatePlan(IPlanRepository plans, IPlatformUnitOfWork unitOfWork, IClock clock)
    {
        _plans = plans;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<Plan>> ExecuteAsync(PlanId id, CancellationToken cancellationToken = default)
    {
        var plan = await _plans.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (plan is null)
        {
            return ApplicationResult<Plan>.Failure(ApplicationErrorCodes.PlanNotFound, "Plan was not found.");
        }

        try
        {
            plan.Deactivate(_clock.UtcNow);
            await _plans.UpdateAsync(plan, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<Plan>.Success(plan);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<Plan>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class UpdatePlanCommercialPackage
{
    private readonly IPlanRepository _plans;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public UpdatePlanCommercialPackage(IPlanRepository plans, IPlatformUnitOfWork unitOfWork, IClock clock)
    {
        _plans = plans;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<Plan>> ExecuteAsync(
        PlanId id,
        string displayName,
        string? description,
        int maxBranches,
        int maxActiveStaff,
        int maxActivePosDevices,
        int maxActiveBusinessTypes,
        bool customerCreditEnabled,
        bool advancedReportsEnabled,
        bool exportEnabled,
        bool trialAllowed,
        int defaultTrialDays,
        int sortOrder,
        decimal monthlyPrice,
        decimal annualPrice,
        string currencyCode,
        DateTimeOffset? expectedUpdatedAtUtc = null,
        CancellationToken cancellationToken = default,
        int? maxAreas = null)
    {
        var plan = await _plans.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (plan is null)
        {
            return ApplicationResult<Plan>.Failure(ApplicationErrorCodes.PlanNotFound, "Plan was not found.");
        }

        if (RenameProduct.IsConcurrencyMismatch(plan.UpdatedAtUtc, expectedUpdatedAtUtc))
        {
            return ApplicationResult<Plan>.Failure(
                ApplicationErrorCodes.ConcurrencyConflict,
                "The plan was modified by another request. Refresh and try again.");
        }

        try
        {
            plan.Rename(displayName, _clock.UtcNow);
            plan.UpdateCommercialPackage(
                description,
                maxBranches,
                maxActiveStaff,
                maxActivePosDevices,
                maxActiveBusinessTypes,
                customerCreditEnabled,
                advancedReportsEnabled,
                exportEnabled,
                trialAllowed,
                defaultTrialDays,
                sortOrder,
                monthlyPrice,
                annualPrice,
                currencyCode,
                _clock.UtcNow,
                maxAreas ?? plan.MaxAreas);
            await _plans.UpdateAsync(plan, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<Plan>.Success(plan);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<Plan>.Failure(ex.ErrorCode, ex.Message);
        }
    }

    public Task<ApplicationResult<Plan>> ExecuteAsync(
        PlanId id,
        string displayName,
        string? description,
        int maxBranches,
        int maxActiveStaff,
        int maxActivePosDevices,
        bool customerCreditEnabled,
        bool advancedReportsEnabled,
        bool exportEnabled,
        bool trialAllowed,
        int defaultTrialDays,
        int sortOrder,
        decimal monthlyPrice,
        decimal annualPrice,
        string currencyCode,
        DateTimeOffset? expectedUpdatedAtUtc = null,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            id,
            displayName,
            description,
            maxBranches,
            maxActiveStaff,
            maxActivePosDevices,
            maxActiveBusinessTypes: 1,
            customerCreditEnabled,
            advancedReportsEnabled,
            exportEnabled,
            trialAllowed,
            defaultTrialDays,
            sortOrder,
            monthlyPrice,
            annualPrice,
            currencyCode,
            expectedUpdatedAtUtc,
            cancellationToken);

    public Task<ApplicationResult<Plan>> ExecuteAsync(
        PlanId id,
        string displayName,
        string? description,
        int maxBranches,
        int maxActiveStaff,
        bool customerCreditEnabled,
        bool advancedReportsEnabled,
        bool exportEnabled,
        bool trialAllowed,
        int defaultTrialDays,
        int sortOrder,
        decimal monthlyPrice,
        decimal annualPrice,
        string currencyCode,
        DateTimeOffset? expectedUpdatedAtUtc = null,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            id,
            displayName,
            description,
            maxBranches,
            maxActiveStaff,
            maxActivePosDevices: 1,
            maxActiveBusinessTypes: 1,
            customerCreditEnabled,
            advancedReportsEnabled,
            exportEnabled,
            trialAllowed,
            defaultTrialDays,
            sortOrder,
            monthlyPrice,
            annualPrice,
            currencyCode,
            expectedUpdatedAtUtc,
            cancellationToken);
}

public sealed class RetirePlan
{
    private readonly IPlanRepository _plans;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public RetirePlan(IPlanRepository plans, IPlatformUnitOfWork unitOfWork, IClock clock)
    {
        _plans = plans;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<Plan>> ExecuteAsync(PlanId id, CancellationToken cancellationToken = default)
    {
        var plan = await _plans.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (plan is null)
        {
            return ApplicationResult<Plan>.Failure(ApplicationErrorCodes.PlanNotFound, "Plan was not found.");
        }

        try
        {
            plan.Retire(_clock.UtcNow);
            await _plans.UpdateAsync(plan, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<Plan>.Success(plan);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<Plan>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class CreateDraftPlanVersion
{
    private readonly IPlanRepository _plans;
    private readonly IFeatureDefinitionRepository _features;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public CreateDraftPlanVersion(
        IPlanRepository plans,
        IFeatureDefinitionRepository features,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _plans = plans;
        _features = features;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<PlanVersion>> ExecuteAsync(
        PlanId planId,
        int versionNumber,
        BillingPeriod billingPeriod,
        bool trialEligible,
        IReadOnlyList<FeatureGrantSpec> grants,
        DateTimeOffset? effectiveFromUtc = null,
        DateTimeOffset? effectiveToUtc = null,
        IReadOnlyList<BusinessTypeId>? businessTypeGrants = null,
        CancellationToken cancellationToken = default)
    {
        var plan = await _plans.GetByIdAsync(planId, cancellationToken).ConfigureAwait(false);
        if (plan is null)
        {
            return ApplicationResult<PlanVersion>.Failure(ApplicationErrorCodes.PlanNotFound, "Plan was not found.");
        }

        var validation = await ValidateGrantsAsync(plan.ProductCode, grants, cancellationToken).ConfigureAwait(false);
        if (!validation.IsSuccess)
        {
            return ApplicationResult<PlanVersion>.Failure(validation.ErrorCode!, validation.ErrorMessage!);
        }

        try
        {
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
                effectiveFromUtc ?? utcNow,
                billingPeriod,
                trialEligible,
                grants,
                utcNow,
                effectiveToUtc,
                businessTypeGrants: businessTypeGrants);

            await _plans.AddVersionAsync(version, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<PlanVersion>.Success(version);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<PlanVersion>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<PlanVersion>.Failure(ex.ErrorCode, ex.Message);
        }
    }

    internal async Task<ApplicationResult> ValidateGrantsAsync(
        ProductCode productCode,
        IReadOnlyList<FeatureGrantSpec> grants,
        CancellationToken cancellationToken)
    {
        foreach (var grant in grants)
        {
            var feature = await _features
                .GetByProductAndCodeAsync(productCode, grant.FeatureCode, cancellationToken)
                .ConfigureAwait(false);
            if (feature is null)
            {
                return ApplicationResult.Failure(
                    ApplicationErrorCodes.FeatureNotFound,
                    $"Feature '{grant.FeatureCode}' was not found for product.");
            }

            try
            {
                feature.EnsureAssignable();
                if (feature.ValueType == FeatureValueType.Boolean && grant.NumericLimit is not null)
                {
                    return ApplicationResult.Failure(
                        DomainErrorCodes.InvalidEntitlementLimit,
                        "Boolean features must not carry a numeric limit.");
                }
            }
            catch (DomainException ex)
            {
                return ApplicationResult.Failure(ex.ErrorCode, ex.Message);
            }
        }

        return ApplicationResult.Success();
    }
}

public sealed class ReplaceDraftPlanVersionGrants
{
    private readonly IPlanRepository _plans;
    private readonly IFeatureDefinitionRepository _features;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public ReplaceDraftPlanVersionGrants(
        IPlanRepository plans,
        IFeatureDefinitionRepository features,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _plans = plans;
        _features = features;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<PlanVersion>> ExecuteAsync(
        PlanId planId,
        int versionNumber,
        IReadOnlyList<FeatureGrantSpec> grants,
        CancellationToken cancellationToken = default)
    {
        var version = await _plans
            .GetVersionByPlanAndNumberAsync(planId, versionNumber, cancellationToken)
            .ConfigureAwait(false);

        if (version is null)
        {
            return ApplicationResult<PlanVersion>.Failure(
                ApplicationErrorCodes.PlanVersionNotFound,
                "Plan version was not found.");
        }

        var validation = await ValidateGrantsAsync(version.ProductCode, grants, cancellationToken).ConfigureAwait(false);
        if (!validation.IsSuccess)
        {
            return ApplicationResult<PlanVersion>.Failure(validation.ErrorCode!, validation.ErrorMessage!);
        }

        try
        {
            version.ReplaceDraftGrants(grants, _clock.UtcNow);
            await _plans.UpdateVersionAsync(version, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<PlanVersion>.Success(version);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<PlanVersion>.Failure(ex.ErrorCode, ex.Message);
        }
    }

    private async Task<ApplicationResult> ValidateGrantsAsync(
        ProductCode productCode,
        IReadOnlyList<FeatureGrantSpec> grants,
        CancellationToken cancellationToken)
    {
        foreach (var grant in grants)
        {
            var feature = await _features
                .GetByProductAndCodeAsync(productCode, grant.FeatureCode, cancellationToken)
                .ConfigureAwait(false);
            if (feature is null)
            {
                return ApplicationResult.Failure(
                    ApplicationErrorCodes.FeatureNotFound,
                    $"Feature '{grant.FeatureCode}' was not found for product.");
            }

            try
            {
                feature.EnsureAssignable();
                if (feature.ValueType == FeatureValueType.Boolean && grant.NumericLimit is not null)
                {
                    return ApplicationResult.Failure(
                        DomainErrorCodes.InvalidEntitlementLimit,
                        "Boolean features must not carry a numeric limit.");
                }
            }
            catch (DomainException ex)
            {
                return ApplicationResult.Failure(ex.ErrorCode, ex.Message);
            }
        }

        return ApplicationResult.Success();
    }
}

public sealed class UpsertDraftFeatureGrant
{
    private readonly IPlanRepository _plans;
    private readonly IFeatureDefinitionRepository _features;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public UpsertDraftFeatureGrant(
        IPlanRepository plans,
        IFeatureDefinitionRepository features,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _plans = plans;
        _features = features;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<PlanVersion>> ExecuteAsync(
        PlanId planId,
        int versionNumber,
        FeatureGrantSpec grant,
        CancellationToken cancellationToken = default)
    {
        var version = await _plans
            .GetVersionByPlanAndNumberAsync(planId, versionNumber, cancellationToken)
            .ConfigureAwait(false);

        if (version is null)
        {
            return ApplicationResult<PlanVersion>.Failure(
                ApplicationErrorCodes.PlanVersionNotFound,
                "Plan version was not found.");
        }

        var feature = await _features
            .GetByProductAndCodeAsync(version.ProductCode, grant.FeatureCode, cancellationToken)
            .ConfigureAwait(false);

        if (feature is null)
        {
            return ApplicationResult<PlanVersion>.Failure(
                ApplicationErrorCodes.FeatureNotFound,
                $"Feature '{grant.FeatureCode}' was not found for product.");
        }

        try
        {
            feature.EnsureAssignable();
            if (feature.ValueType == FeatureValueType.Boolean && grant.NumericLimit is not null)
            {
                return ApplicationResult<PlanVersion>.Failure(
                    DomainErrorCodes.InvalidEntitlementLimit,
                    "Boolean features must not carry a numeric limit.");
            }

            var grants = version.Grants
                .Where(g => g.FeatureCode != grant.FeatureCode)
                .Append(grant)
                .ToList();

            version.ReplaceDraftGrants(grants, _clock.UtcNow);
            await _plans.UpdateVersionAsync(version, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<PlanVersion>.Success(version);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<PlanVersion>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class PublishExistingPlanVersion
{
    private readonly IPlanRepository _plans;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public PublishExistingPlanVersion(IPlanRepository plans, IPlatformUnitOfWork unitOfWork, IClock clock)
    {
        _plans = plans;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<PlanVersion>> ExecuteAsync(
        PlanId planId,
        int versionNumber,
        CancellationToken cancellationToken = default)
    {
        var version = await _plans
            .GetVersionByPlanAndNumberAsync(planId, versionNumber, cancellationToken)
            .ConfigureAwait(false);

        if (version is null)
        {
            return ApplicationResult<PlanVersion>.Failure(
                ApplicationErrorCodes.PlanVersionNotFound,
                "Plan version was not found.");
        }

        try
        {
            version.Publish(_clock.UtcNow);
            await _plans.UpdateVersionAsync(version, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<PlanVersion>.Success(version);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<PlanVersion>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class PublishPlanVersion
{
    private readonly IPlanRepository _plans;
    private readonly IFeatureDefinitionRepository _features;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public PublishPlanVersion(
        IPlanRepository plans,
        IFeatureDefinitionRepository features,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _plans = plans;
        _features = features;
        _unitOfWork = unitOfWork;
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
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<PlanVersion>.Success(version);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<PlanVersion>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<PlanVersion>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class CreateTrialDefinition
{
    private readonly IProductRepository _products;
    private readonly IPlanRepository _plans;
    private readonly ITrialDefinitionRepository _trials;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public CreateTrialDefinition(
        IProductRepository products,
        IPlanRepository plans,
        ITrialDefinitionRepository trials,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _products = products;
        _plans = plans;
        _trials = trials;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<TrialDefinition>> ExecuteAsync(
        string productCode,
        string displayName,
        TimeSpan duration,
        IReadOnlyList<FeatureGrantSpec> featureGrants,
        IReadOnlyList<FeatureGrantSpec> postExpiryFeatureGrants,
        Guid? planId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var pc = ProductCode.Create(productCode);
            var product = await _products.GetByCodeAsync(pc, cancellationToken).ConfigureAwait(false);
            if (product is null)
            {
                return ApplicationResult<TrialDefinition>.Failure(
                    ApplicationErrorCodes.ProductNotFound,
                    "Product was not found.");
            }

            PlanId? linkedPlanId = null;
            if (planId is not null)
            {
                var plan = await _plans.GetByIdAsync(PlanId.From(planId.Value), cancellationToken).ConfigureAwait(false);
                if (plan is null)
                {
                    return ApplicationResult<TrialDefinition>.Failure(
                        ApplicationErrorCodes.PlanNotFound,
                        "Plan was not found.");
                }

                linkedPlanId = plan.Id;
            }

            var trial = TrialDefinition.Create(
                pc,
                displayName,
                duration,
                featureGrants,
                postExpiryFeatureGrants,
                _clock.UtcNow,
                linkedPlanId);

            await _trials.AddAsync(trial, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<TrialDefinition>.Success(trial);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<TrialDefinition>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<TrialDefinition>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class RetireTrialDefinition
{
    private readonly ITrialDefinitionRepository _trials;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public RetireTrialDefinition(ITrialDefinitionRepository trials, IPlatformUnitOfWork unitOfWork, IClock clock)
    {
        _trials = trials;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<TrialDefinition>> ExecuteAsync(
        TrialDefinitionId id,
        CancellationToken cancellationToken = default)
    {
        var trial = await _trials.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (trial is null)
        {
            return ApplicationResult<TrialDefinition>.Failure(
                ApplicationErrorCodes.TrialNotFound,
                "Trial definition was not found.");
        }

        try
        {
            trial.Retire(_clock.UtcNow);
            await _trials.UpdateAsync(trial, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<TrialDefinition>.Success(trial);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<TrialDefinition>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}
