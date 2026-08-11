using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.GlobalCatalog;
using ExItS.Platform.Application.LocalValidation;
using ExItS.Platform.Application.Subscriptions;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Catalog;
using ExItS.Platform.Domain.GlobalCatalog;
using ExItS.Platform.Domain.Products;
using ExItS.Platform.Domain.Subscriptions;
using Microsoft.Extensions.Logging;

namespace ExItS.Platform.Application.Catalog;

/// <summary>
/// Idempotently seeds MVP Pinoy Business POS commercial plans (Starter / Growth / Pro),
/// remaps legacy Local Validation / Start-Business / Business provisional subscriptions onto Growth,
/// and retires unused legacy plans when no active-like subscriptions remain.
/// </summary>
public sealed class EnsureMvpPosPlans
{
    private const string LegacyLocalValidationPlanCode = LocalValidationOptions.ProductPlanCode;
    private const string LegacyStartBusinessPlanCode = "start-business-pos";

    private static readonly (string Code, FeatureValueType ValueType)[] RequiredFeatures =
    [
        (FeatureCode.PlanMaxBranches, FeatureValueType.QuantityLimit),
        (FeatureCode.PlanMaxActiveStaff, FeatureValueType.QuantityLimit),
        (FeatureCode.PlanMaxActivePosDevices, FeatureValueType.QuantityLimit),
        (FeatureCode.PlanMaxActiveBusinessTypes, FeatureValueType.QuantityLimit),
        (FeatureCode.StoreAdvancedReports, FeatureValueType.Boolean),
        (FeatureCode.StoreExport, FeatureValueType.Boolean),
        (FeatureCode.CustomerCreditCreate, FeatureValueType.Boolean),
        (FeatureCode.CustomerCreditView, FeatureValueType.Boolean),
        (FeatureCode.CustomerCreditRepay, FeatureValueType.Boolean),
        (FeatureCode.StoreCatalogView, FeatureValueType.Boolean),
        (FeatureCode.StoreCatalogManage, FeatureValueType.Boolean),
        (FeatureCode.StoreSalesView, FeatureValueType.Boolean),
        (FeatureCode.StoreSalesCreate, FeatureValueType.Boolean),
        (FeatureCode.StoreDashboardView, FeatureValueType.Boolean),
        (FeatureCode.StoreReportsView, FeatureValueType.Boolean),
        (FeatureCode.StorePermissionsView, FeatureValueType.Boolean),
        (FeatureCode.StorePermissionsManage, FeatureValueType.Boolean),
        (FeatureCode.StoreSalesVoid, FeatureValueType.Boolean),
        (FeatureCode.StoreInventoryView, FeatureValueType.Boolean),
        (FeatureCode.StoreInventoryManage, FeatureValueType.Boolean),
        (FeatureCode.StoreExpensesView, FeatureValueType.Boolean),
        (FeatureCode.StoreExpensesManage, FeatureValueType.Boolean),
        (FeatureCode.StoreSuppliersView, FeatureValueType.Boolean),
        (FeatureCode.StoreSuppliersManage, FeatureValueType.Boolean),
        (FeatureCode.StoreShiftsView, FeatureValueType.Boolean),
        (FeatureCode.StoreShiftsManage, FeatureValueType.Boolean),
        (FeatureCode.StoreReturnsView, FeatureValueType.Boolean),
        (FeatureCode.StoreReturnsManage, FeatureValueType.Boolean),
        (FeatureCode.StoreRegistersView, FeatureValueType.Boolean),
        (FeatureCode.StoreRegistersManage, FeatureValueType.Boolean)
    ];

    private static readonly string[] BasicStoreFeatureCodes =
    [
        FeatureCode.StoreCatalogView,
        FeatureCode.StoreCatalogManage,
        FeatureCode.StoreSalesView,
        FeatureCode.StoreSalesCreate,
        FeatureCode.StoreSalesVoid,
        FeatureCode.StoreDashboardView,
        FeatureCode.StoreReportsView,
        FeatureCode.StorePermissionsView,
        FeatureCode.StorePermissionsManage,
        FeatureCode.StoreInventoryView,
        FeatureCode.StoreInventoryManage,
        FeatureCode.StoreExpensesView,
        FeatureCode.StoreExpensesManage,
        FeatureCode.StoreSuppliersView,
        FeatureCode.StoreSuppliersManage,
        FeatureCode.StoreShiftsView,
        FeatureCode.StoreShiftsManage,
        FeatureCode.StoreReturnsView,
        FeatureCode.StoreReturnsManage,
        FeatureCode.StoreRegistersView,
        FeatureCode.StoreRegistersManage
    ];

    private readonly IProductRepository _products;
    private readonly IPlanRepository _plans;
    private readonly IFeatureDefinitionRepository _features;
    private readonly IBusinessTypeRepository _businessTypes;
    private readonly CreatePlan _createPlan;
    private readonly ActivatePlan _activatePlan;
    private readonly UpdatePlanCommercialPackage _updateCommercialPackage;
    private readonly CreateDraftPlanVersion _createDraftVersion;
    private readonly PublishExistingPlanVersion _publishVersion;
    private readonly CreateFeatureDefinition _createFeature;
    private readonly RetirePlan _retirePlan;
    private readonly ISubscriptionRepository _subscriptions;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly ILogger<EnsureMvpPosPlans> _logger;

    public EnsureMvpPosPlans(
        IProductRepository products,
        IPlanRepository plans,
        IFeatureDefinitionRepository features,
        IBusinessTypeRepository businessTypes,
        CreatePlan createPlan,
        ActivatePlan activatePlan,
        UpdatePlanCommercialPackage updateCommercialPackage,
        CreateDraftPlanVersion createDraftVersion,
        PublishExistingPlanVersion publishVersion,
        CreateFeatureDefinition createFeature,
        RetirePlan retirePlan,
        ISubscriptionRepository subscriptions,
        IPlatformUnitOfWork unitOfWork,
        IClock clock,
        ILogger<EnsureMvpPosPlans> logger)
    {
        _products = products;
        _plans = plans;
        _features = features;
        _businessTypes = businessTypes;
        _createPlan = createPlan;
        _activatePlan = activatePlan;
        _updateCommercialPackage = updateCommercialPackage;
        _createDraftVersion = createDraftVersion;
        _publishVersion = publishVersion;
        _createFeature = createFeature;
        _retirePlan = retirePlan;
        _subscriptions = subscriptions;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var productCode = ProductCode.Create(ProductCode.PinoyBusinessPos);
        var product = await _products.GetByCodeAsync(productCode, cancellationToken).ConfigureAwait(false);
        if (product is null)
        {
            throw new InvalidOperationException(
                $"Product '{ProductCode.PinoyBusinessPos}' must exist before EnsureMvpPosPlans runs (Local Validation product seed creates it).");
        }

        await EnsureFeaturesAsync(productCode.Value, cancellationToken).ConfigureAwait(false);
        var businessTypeGrants = await EnsurePhilippineBusinessTypesAsync(cancellationToken).ConfigureAwait(false);

        Plan? growthPlan = null;
        PlanVersion? growthVersion = null;

        foreach (var spec in MvpPosPlanCatalog.Plans)
        {
            var plan = await EnsurePlanAsync(productCode.Value, spec, cancellationToken).ConfigureAwait(false);
            var version = await EnsurePublishedVersionAsync(plan, spec, businessTypeGrants, cancellationToken)
                .ConfigureAwait(false);

            if (string.Equals(spec.PlanKey, MvpPosPlanCodes.Growth, StringComparison.Ordinal))
            {
                growthPlan = plan;
                growthVersion = version;
            }
        }

        if (growthPlan is null || growthVersion is null)
        {
            throw new InvalidOperationException("MVP Growth plan/version was not available after EnsureMvpPosPlans.");
        }

        await RemapLegacySubscriptionsAsync(productCode, growthPlan, growthVersion, cancellationToken)
            .ConfigureAwait(false);

        await RetireLegacyPlanIfUnusedAsync(productCode, LegacyLocalValidationPlanCode, cancellationToken)
            .ConfigureAwait(false);
        await RetireLegacyPlanIfUnusedAsync(productCode, LegacyStartBusinessPlanCode, cancellationToken)
            .ConfigureAwait(false);
        await RetireLegacyPlanIfUnusedAsync(productCode, MvpPosPlanCodes.LegacyBusiness, cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task EnsureFeaturesAsync(string productCode, CancellationToken cancellationToken)
    {
        foreach (var (code, valueType) in RequiredFeatures)
        {
            var existing = await _features
                .GetByProductAndCodeAsync(ProductCode.Create(productCode), FeatureCode.Create(code), cancellationToken)
                .ConfigureAwait(false);
            if (existing is not null)
            {
                continue;
            }

            var created = await _createFeature
                .ExecuteAsync(productCode, code, code, valueType, cancellationToken)
                .ConfigureAwait(false);
            if (!created.IsSuccess && created.ErrorCode != ApplicationErrorCodes.DuplicateFeatureCode)
            {
                throw new InvalidOperationException(
                    $"MVP POS feature '{code}' failed: {created.ErrorCode} {created.ErrorMessage}");
            }
        }
    }

    private async Task<IReadOnlyList<BusinessTypeId>> EnsurePhilippineBusinessTypesAsync(
        CancellationToken cancellationToken)
    {
        var grants = new List<BusinessTypeId>(PhilippineBusinessTypeSeeds.All.Count);
        var added = false;
        foreach (var seed in PhilippineBusinessTypeSeeds.All)
        {
            var existing = await _businessTypes.GetByCodeAsync(seed.Code, cancellationToken).ConfigureAwait(false);
            if (existing is not null)
            {
                grants.Add(existing.Id);
                continue;
            }

            var byId = await _businessTypes
                .GetByIdAsync(BusinessTypeId.From(seed.Id), cancellationToken)
                .ConfigureAwait(false);
            if (byId is not null)
            {
                grants.Add(byId.Id);
                continue;
            }

            var created = BusinessType.Create(
                seed.Code,
                seed.Name,
                _clock.UtcNow,
                description: $"Philippine Pinoy Business POS default type ({seed.Code}).",
                sortOrder: seed.SortOrder,
                id: BusinessTypeId.From(seed.Id));
            await _businessTypes.AddAsync(created, cancellationToken).ConfigureAwait(false);
            grants.Add(created.Id);
            added = true;
        }

        if (added)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return grants;
    }

    private async Task<Plan> EnsurePlanAsync(
        string productCode,
        MvpPosPlanCatalog.Spec spec,
        CancellationToken cancellationToken)
    {
        var pc = ProductCode.Create(productCode);
        var planCode = PlanCode.Create(spec.PlanKey);
        var plan = await _plans.GetByProductAndCodeAsync(pc, planCode, cancellationToken).ConfigureAwait(false);

        if (plan is null)
        {
            var created = await _createPlan
                .ExecuteAsync(
                    productCode,
                    spec.PlanKey,
                    spec.DisplayName,
                    spec.Description,
                    spec.MaxBranches,
                    spec.MaxActiveStaff,
                    spec.MaxActivePosDevices,
                    spec.MaxActiveBusinessTypes,
                    spec.CustomerCreditEnabled,
                    spec.AdvancedReportsEnabled,
                    spec.ExportEnabled,
                    spec.TrialAllowed,
                    spec.DefaultTrialDays,
                    spec.SortOrder,
                    spec.MonthlyPrice,
                    spec.AnnualPrice,
                    spec.CurrencyCode,
                    cancellationToken)
                .ConfigureAwait(false);

            if (!created.IsSuccess || created.Value is null)
            {
                plan = await _plans.GetByProductAndCodeAsync(pc, planCode, cancellationToken).ConfigureAwait(false);
                if (plan is null)
                {
                    throw new InvalidOperationException(
                        $"MVP POS plan '{spec.PlanKey}' create failed: {created.ErrorCode} {created.ErrorMessage}");
                }
            }
            else
            {
                plan = created.Value;
            }
        }
        else if (CommercialPackageDiffers(plan, spec))
        {
            var updated = await _updateCommercialPackage
                .ExecuteAsync(
                    plan.Id,
                    spec.DisplayName,
                    spec.Description,
                    spec.MaxBranches,
                    spec.MaxActiveStaff,
                    spec.MaxActivePosDevices,
                    spec.MaxActiveBusinessTypes,
                    spec.CustomerCreditEnabled,
                    spec.AdvancedReportsEnabled,
                    spec.ExportEnabled,
                    spec.TrialAllowed,
                    spec.DefaultTrialDays,
                    spec.SortOrder,
                    spec.MonthlyPrice,
                    spec.AnnualPrice,
                    spec.CurrencyCode,
                    expectedUpdatedAtUtc: null,
                    cancellationToken)
                .ConfigureAwait(false);

            if (!updated.IsSuccess || updated.Value is null)
            {
                throw new InvalidOperationException(
                    $"MVP POS plan '{spec.PlanKey}' commercial update failed: {updated.ErrorCode} {updated.ErrorMessage}");
            }

            plan = updated.Value;
        }

        if (plan.Status != PlanStatus.Active)
        {
            var activate = await _activatePlan.ExecuteAsync(plan.Id, cancellationToken).ConfigureAwait(false);
            if (!activate.IsSuccess)
            {
                var reloaded = await _plans.GetByIdAsync(plan.Id, cancellationToken).ConfigureAwait(false);
                if (reloaded?.Status != PlanStatus.Active)
                {
                    throw new InvalidOperationException(
                        $"MVP POS plan '{spec.PlanKey}' activate failed: {activate.ErrorCode} {activate.ErrorMessage}");
                }

                plan = reloaded;
            }
            else if (activate.Value is not null)
            {
                plan = activate.Value;
            }
        }

        return plan;
    }

    private async Task<PlanVersion> EnsurePublishedVersionAsync(
        Plan plan,
        MvpPosPlanCatalog.Spec spec,
        IReadOnlyList<BusinessTypeId> businessTypeGrants,
        CancellationToken cancellationToken)
    {
        var versions = await _plans.ListVersionsAsync(plan.Id, cancellationToken).ConfigureAwait(false);
        var published = versions
            .Where(v => v.Status == PlanVersionStatus.Published)
            .OrderByDescending(v => v.VersionNumber)
            .FirstOrDefault();
        var grants = BuildGrants(spec);

        if (published is null)
        {
            var nextNumber = Math.Max(1, await _plans.GetMaxVersionNumberAsync(plan.Id, cancellationToken).ConfigureAwait(false) + 1);
            return await CreateAndPublishVersionAsync(plan, spec, nextNumber, grants, businessTypeGrants, cancellationToken)
                .ConfigureAwait(false);
        }

        if (!VersionNeedsRefresh(published, grants, businessTypeGrants))
        {
            return published;
        }

        var refreshNumber = await _plans.GetMaxVersionNumberAsync(plan.Id, cancellationToken).ConfigureAwait(false) + 1;
        var refreshed = await CreateAndPublishVersionAsync(
                plan,
                spec,
                refreshNumber,
                grants,
                businessTypeGrants,
                cancellationToken)
            .ConfigureAwait(false);

        await RemapPlanSubscriptionsToVersionAsync(plan, refreshed, cancellationToken).ConfigureAwait(false);
        return refreshed;
    }

    private static bool VersionNeedsRefresh(
        PlanVersion version,
        IReadOnlyList<FeatureGrantSpec> expectedGrants,
        IReadOnlyList<BusinessTypeId> expectedBusinessTypes)
    {
        if (version.BusinessTypeGrants.Count != expectedBusinessTypes.Count)
        {
            return true;
        }

        var granted = version.BusinessTypeGrants.Select(id => id.Value).ToHashSet();
        if (expectedBusinessTypes.Any(id => !granted.Contains(id.Value)))
        {
            return true;
        }

        if (version.Grants.Count != expectedGrants.Count)
        {
            return true;
        }

        foreach (var expected in expectedGrants)
        {
            var match = version.Grants.FirstOrDefault(g =>
                string.Equals(g.FeatureCode.Value, expected.FeatureCode.Value, StringComparison.Ordinal));
            if (match is null
                || match.Enabled != expected.Enabled
                || match.NumericLimit != expected.NumericLimit)
            {
                return true;
            }
        }

        return false;
    }

    private async Task<PlanVersion> CreateAndPublishVersionAsync(
        Plan plan,
        MvpPosPlanCatalog.Spec spec,
        int versionNumber,
        IReadOnlyList<FeatureGrantSpec> grants,
        IReadOnlyList<BusinessTypeId> businessTypeGrants,
        CancellationToken cancellationToken)
    {
        var draft = await _createDraftVersion
            .ExecuteAsync(
                plan.Id,
                versionNumber,
                BillingPeriod.Monthly,
                trialEligible: true,
                grants,
                businessTypeGrants: businessTypeGrants,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        if (!draft.IsSuccess || draft.Value is null)
        {
            throw new InvalidOperationException(
                $"MVP POS plan '{spec.PlanKey}' version draft failed: {draft.ErrorCode} {draft.ErrorMessage}");
        }

        var published = await _publishVersion
            .ExecuteAsync(plan.Id, versionNumber, cancellationToken)
            .ConfigureAwait(false);
        if (!published.IsSuccess || published.Value is null)
        {
            throw new InvalidOperationException(
                $"MVP POS plan '{spec.PlanKey}' version publish failed: {published.ErrorCode} {published.ErrorMessage}");
        }

        return published.Value;
    }

    private async Task RemapPlanSubscriptionsToVersionAsync(
        Plan plan,
        PlanVersion version,
        CancellationToken cancellationToken)
    {
        var skip = 0;
        const int take = 200;
        while (true)
        {
            var (items, total) = await _subscriptions
                .ListAsync(
                    organizationId: null,
                    plan.ProductCode,
                    status: null,
                    search: null,
                    isTrial: null,
                    planId: plan.Id.Value,
                    SubscriptionListSortBy.CreatedAtUtc,
                    sortDescending: false,
                    skip,
                    take,
                    cancellationToken)
                .ConfigureAwait(false);

            foreach (var subscription in items)
            {
                if (!Subscription.IsActiveLike(subscription.Status))
                {
                    continue;
                }

                if (subscription.PlanVersionId == version.Id)
                {
                    continue;
                }

                subscription.RebindCommercialPackage(plan, version, _clock.UtcNow);
                await _subscriptions.UpdateAsync(subscription, cancellationToken).ConfigureAwait(false);
                await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

                _logger.LogInformation(
                    "Remapped subscription {SubscriptionId} on plan '{PlanCode}' to published version {VersionNumber}.",
                    subscription.Id.Value,
                    plan.Code.Value,
                    version.VersionNumber);
            }

            skip += items.Count;
            if (skip >= total || items.Count == 0)
            {
                break;
            }
        }
    }

    private async Task RemapLegacySubscriptionsAsync(
        ProductCode productCode,
        Plan growthPlan,
        PlanVersion growthVersion,
        CancellationToken cancellationToken)
    {
        foreach (var legacyCode in new[]
                 {
                     LegacyLocalValidationPlanCode,
                     LegacyStartBusinessPlanCode,
                     MvpPosPlanCodes.LegacyBusiness
                 })
        {
            var legacyPlan = await _plans
                .GetByProductAndCodeAsync(productCode, PlanCode.Create(legacyCode), cancellationToken)
                .ConfigureAwait(false);
            if (legacyPlan is null)
            {
                continue;
            }

            var skip = 0;
            const int take = 200;
            while (true)
            {
                var (items, total) = await _subscriptions
                    .ListAsync(
                        organizationId: null,
                        productCode,
                        status: null,
                        search: null,
                        isTrial: null,
                        planId: legacyPlan.Id.Value,
                        SubscriptionListSortBy.CreatedAtUtc,
                        sortDescending: false,
                        skip,
                        take,
                        cancellationToken)
                    .ConfigureAwait(false);

                foreach (var subscription in items)
                {
                    if (!Subscription.IsActiveLike(subscription.Status))
                    {
                        continue;
                    }

                    if (subscription.PlanId == growthPlan.Id
                        && subscription.PlanVersionId == growthVersion.Id)
                    {
                        continue;
                    }

                    subscription.RebindCommercialPackage(growthPlan, growthVersion, _clock.UtcNow);
                    await _subscriptions.UpdateAsync(subscription, cancellationToken).ConfigureAwait(false);
                    await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

                    _logger.LogInformation(
                        "Remapped subscription {SubscriptionId} from plan '{LegacyPlanCode}' to Growth plan '{GrowthPlanCode}'.",
                        subscription.Id.Value,
                        legacyCode,
                        growthPlan.Code.Value);
                }

                skip += items.Count;
                if (skip >= total || items.Count == 0)
                {
                    break;
                }
            }
        }
    }

    private async Task RetireLegacyPlanIfUnusedAsync(
        ProductCode productCode,
        string legacyPlanCode,
        CancellationToken cancellationToken)
    {
        var legacyPlan = await _plans
            .GetByProductAndCodeAsync(productCode, PlanCode.Create(legacyPlanCode), cancellationToken)
            .ConfigureAwait(false);
        if (legacyPlan is null || legacyPlan.Status == PlanStatus.Retired)
        {
            return;
        }

        var (items, _) = await _subscriptions
            .ListAsync(
                organizationId: null,
                productCode,
                status: null,
                search: null,
                isTrial: null,
                planId: legacyPlan.Id.Value,
                SubscriptionListSortBy.CreatedAtUtc,
                sortDescending: false,
                skip: 0,
                take: 50,
                cancellationToken)
            .ConfigureAwait(false);

        if (items.Any(s => Subscription.IsActiveLike(s.Status)))
        {
            _logger.LogInformation(
                "Leaving legacy plan '{PlanCode}' in place; active-like subscriptions remain.",
                legacyPlanCode);
            return;
        }

        var retired = await _retirePlan.ExecuteAsync(legacyPlan.Id, cancellationToken).ConfigureAwait(false);
        if (!retired.IsSuccess)
        {
            _logger.LogWarning(
                "Could not retire legacy plan '{PlanCode}': {ErrorCode} {ErrorMessage}",
                legacyPlanCode,
                retired.ErrorCode,
                retired.ErrorMessage);
            return;
        }

        _logger.LogInformation("Retired unused legacy plan '{PlanCode}'.", legacyPlanCode);
    }

    private static bool CommercialPackageDiffers(Plan plan, MvpPosPlanCatalog.Spec spec) =>
        !string.Equals(plan.DisplayName, spec.DisplayName, StringComparison.Ordinal)
        || !string.Equals(plan.Description ?? string.Empty, spec.Description ?? string.Empty, StringComparison.Ordinal)
        || plan.MaxBranches != spec.MaxBranches
        || plan.MaxActiveStaff != spec.MaxActiveStaff
        || plan.MaxActivePosDevices != spec.MaxActivePosDevices
        || plan.MaxActiveBusinessTypes != spec.MaxActiveBusinessTypes
        || plan.CustomerCreditEnabled != spec.CustomerCreditEnabled
        || plan.AdvancedReportsEnabled != spec.AdvancedReportsEnabled
        || plan.ExportEnabled != spec.ExportEnabled
        || plan.TrialAllowed != spec.TrialAllowed
        || plan.DefaultTrialDays != spec.DefaultTrialDays
        || plan.SortOrder != spec.SortOrder
        || plan.MonthlyPrice != spec.MonthlyPrice
        || plan.AnnualPrice != spec.AnnualPrice
        || !string.Equals(plan.CurrencyCode, spec.CurrencyCode, StringComparison.Ordinal);

    public static FeatureGrantSpec[] BuildGrants(MvpPosPlanCatalog.Spec spec)
    {
        var grants = new List<FeatureGrantSpec>
        {
            FeatureGrantSpec.Limit(FeatureCode.Create(FeatureCode.PlanMaxBranches), spec.MaxBranches),
            FeatureGrantSpec.Limit(FeatureCode.Create(FeatureCode.PlanMaxActiveStaff), spec.MaxActiveStaff),
            FeatureGrantSpec.Limit(FeatureCode.Create(FeatureCode.PlanMaxActivePosDevices), spec.MaxActivePosDevices),
            FeatureGrantSpec.Limit(
                FeatureCode.Create(FeatureCode.PlanMaxActiveBusinessTypes),
                spec.MaxActiveBusinessTypes),
            FeatureGrantSpec.Boolean(FeatureCode.Create(FeatureCode.CustomerCreditCreate), spec.CustomerCreditEnabled),
            FeatureGrantSpec.Boolean(FeatureCode.Create(FeatureCode.CustomerCreditView), spec.CustomerCreditEnabled),
            FeatureGrantSpec.Boolean(FeatureCode.Create(FeatureCode.CustomerCreditRepay), spec.CustomerCreditEnabled),
            FeatureGrantSpec.Boolean(FeatureCode.Create(FeatureCode.StoreAdvancedReports), spec.AdvancedReportsEnabled),
            FeatureGrantSpec.Boolean(FeatureCode.Create(FeatureCode.StoreExport), spec.ExportEnabled)
        };

        foreach (var code in BasicStoreFeatureCodes)
        {
            grants.Add(FeatureGrantSpec.Boolean(FeatureCode.Create(code), enabled: true));
        }

        return grants.ToArray();
    }
}
