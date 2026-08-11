using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Entitlements;
using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Application.Payments;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Catalog;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Payments;
using ExItS.Platform.Domain.Products;
using ExItS.Platform.Domain.Subscriptions;

namespace ExItS.Platform.Application.Subscriptions;

public sealed class UpgradeOrganizationSubscription
{
    private readonly IPlatformOrganizationRepository _organizations;
    private readonly IProductRepository _products;
    private readonly IPlanRepository _plans;
    private readonly ISubscriptionRepository _subscriptions;
    private readonly IPaymentProvider _paymentProvider;
    private readonly RecordLinkedSuccessfulProviderPayment _recordLinkedPayment;
    private readonly GenerateEntitlementSnapshot _generateSnapshot;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public UpgradeOrganizationSubscription(
        IPlatformOrganizationRepository organizations,
        IProductRepository products,
        IPlanRepository plans,
        ISubscriptionRepository subscriptions,
        IPaymentProvider paymentProvider,
        RecordLinkedSuccessfulProviderPayment recordLinkedPayment,
        GenerateEntitlementSnapshot generateSnapshot,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _organizations = organizations;
        _products = products;
        _plans = plans;
        _subscriptions = subscriptions;
        _paymentProvider = paymentProvider;
        _recordLinkedPayment = recordLinkedPayment;
        _generateSnapshot = generateSnapshot;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<Subscription>> ExecuteAsync(
        PlatformOrganizationId organizationId,
        ProductCode productCode,
        PlanId targetPlanId,
        BillingCycle billingCycle,
        string? idempotencyKey,
        bool skipPaymentWhenTrialing = true,
        CancellationToken cancellationToken = default)
    {
        var organization = await _organizations.GetByIdAsync(organizationId, cancellationToken).ConfigureAwait(false);
        if (organization is null || organization.Status != OrganizationStatus.Active)
        {
            return ApplicationResult<Subscription>.Failure(
                ApplicationErrorCodes.OrganizationNotEligible,
                "Organization must be active.");
        }

        var product = await _products.GetByCodeAsync(productCode, cancellationToken).ConfigureAwait(false);
        if (product is null || product.Status != ProductStatus.Active)
        {
            return ApplicationResult<Subscription>.Failure(
                ApplicationErrorCodes.ProductNotActive,
                "Product must be active.");
        }

        var targetPlan = await _plans.GetByIdAsync(targetPlanId, cancellationToken).ConfigureAwait(false);
        if (targetPlan is null || targetPlan.ProductCode != productCode || !targetPlan.AcceptsNewSubscriptions)
        {
            return ApplicationResult<Subscription>.Failure(
                ApplicationErrorCodes.PlanNotFound,
                "Target plan was not found or is not active for this product.");
        }

        var subscription = await _subscriptions
            .GetCurrentForOrganizationProductAsync(organizationId, productCode, cancellationToken)
            .ConfigureAwait(false);
        if (subscription is null || !Subscription.IsActiveLike(subscription.Status))
        {
            return ApplicationResult<Subscription>.Failure(
                ApplicationErrorCodes.SubscriptionNotFound,
                "No active-like subscription exists for upgrade.");
        }

        if (subscription.PlanId == targetPlanId)
        {
            return ApplicationResult<Subscription>.Failure(
                ApplicationErrorCodes.SubscriptionIneligible,
                "Subscription is already on the requested plan.");
        }

        var version = await RequirePublishedVersionAsync(targetPlan, cancellationToken).ConfigureAwait(false);

        if (subscription.Status == SubscriptionStatus.Trialing && skipPaymentWhenTrialing)
        {
            try
            {
                subscription.ApplyImmediatePlanUpgrade(targetPlan, version, billingCycle, _clock.UtcNow);
                await _subscriptions.UpdateAsync(subscription, cancellationToken).ConfigureAwait(false);
                await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                await _generateSnapshot
                    .ExecuteAsync(organizationId, productCode, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
                return ApplicationResult<Subscription>.Success(subscription);
            }
            catch (DomainException ex)
            {
                return ApplicationResult<Subscription>.Failure(ex.ErrorCode, ex.Message);
            }
        }

        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return ApplicationResult<Subscription>.Failure(
                ApplicationErrorCodes.PaymentReferenceConflict,
                "IdempotencyKey is required for paid plan upgrades.");
        }

        PaymentProviderResult payment;
        try
        {
            payment = await _paymentProvider
                .ChargeAsync(
                    new PaymentChargeRequest(
                        organizationId.Value,
                        subscription.Id.Value,
                        targetPlan.PriceForCycle(billingCycle),
                        targetPlan.CurrencyCode,
                        idempotencyKey,
                        Purpose: "upgrade"),
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch (NotSupportedException ex)
        {
            return ApplicationResult<Subscription>.Failure(
                ApplicationErrorCodes.PaymentNotConfigured,
                ex.Message);
        }
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        if (payment.Status != PaymentProviderResultStatus.Succeeded)
        {
            return ApplicationResult<Subscription>.Failure(
                ApplicationErrorCodes.PaymentNotConfirmed,
                payment.FailureMessage ?? $"Upgrade payment was not successful ({payment.Status}).");
        }

        var linked = await _recordLinkedPayment
            .ExecuteAsync(
                organizationId,
                productCode,
                subscription.Id,
                payment,
                "upgrade",
                cancellationToken)
            .ConfigureAwait(false);
        if (!linked.IsSuccess)
        {
            return ApplicationResult<Subscription>.Failure(
                linked.ErrorCode ?? ApplicationErrorCodes.PaymentNotConfirmed,
                linked.ErrorMessage ?? "Successful upgrade payment could not be linked for administration.");
        }

        try
        {
            subscription.ApplyImmediatePlanUpgrade(targetPlan, version, billingCycle, _clock.UtcNow);
            await _subscriptions.UpdateAsync(subscription, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await _generateSnapshot
                .ExecuteAsync(organizationId, productCode, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            return ApplicationResult<Subscription>.Success(subscription);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<Subscription>.Failure(ex.ErrorCode, ex.Message);
        }
    }

    private async Task<PlanVersion> RequirePublishedVersionAsync(Plan plan, CancellationToken cancellationToken)
    {
        var versions = await _plans.ListVersionsAsync(plan.Id, cancellationToken).ConfigureAwait(false);
        var published = versions.FirstOrDefault(v => v.Status == PlanVersionStatus.Published)
            ?? throw new DomainException(ApplicationErrorCodes.PlanVersionNotFound, "Published plan version is required.");
        return published;
    }
}

public sealed record PlanUsageConflict(
    string Resource,
    int CurrentUsage,
    int TargetLimit,
    string Message);

public sealed record PlanChangeImpactPreview(
    Guid CurrentPlanId,
    string CurrentPlanDisplayName,
    Guid TargetPlanId,
    string TargetPlanDisplayName,
    int ActiveStaffCount,
    int? ActiveBranchCount,
    bool BranchCountAvailable,
    string? BranchCountUnavailableReason,
    IReadOnlyList<PlanUsageConflict> UsageConflicts,
    IReadOnlyList<string> LostFeatures,
    bool HasBlockingUsageConflicts);

/// <summary>Compares current usage against a lower plan's limits without deleting data.</summary>
public static class PlanChangeImpact
{
    public static PlanChangeImpactPreview Evaluate(
        Plan currentPlan,
        Plan targetPlan,
        int activeStaffCount,
        int? activeBranchCount,
        bool branchCountAvailable,
        string? branchCountUnavailableReason = null,
        int? activeBusinessTypeCount = null)
    {
        var conflicts = new List<PlanUsageConflict>();
        if (branchCountAvailable && activeBranchCount.HasValue && activeBranchCount.Value > targetPlan.MaxBranches)
        {
            conflicts.Add(new PlanUsageConflict(
                "Branches",
                activeBranchCount.Value,
                targetPlan.MaxBranches,
                $"Current branches ({activeBranchCount.Value}) exceed the target limit ({targetPlan.MaxBranches}). Existing branches are retained; new branches that would further exceed the limit are blocked."));
        }

        if (activeStaffCount > targetPlan.MaxActiveStaff)
        {
            conflicts.Add(new PlanUsageConflict(
                "ActiveStaff",
                activeStaffCount,
                targetPlan.MaxActiveStaff,
                $"Current active staff ({activeStaffCount}) exceed the target limit ({targetPlan.MaxActiveStaff}). Existing staff are retained; inviting or activating additional staff that would further exceed the limit is blocked."));
        }

        if (activeBusinessTypeCount.HasValue
            && activeBusinessTypeCount.Value > targetPlan.MaxActiveBusinessTypes)
        {
            conflicts.Add(new PlanUsageConflict(
                "ActiveBusinessTypes",
                activeBusinessTypeCount.Value,
                targetPlan.MaxActiveBusinessTypes,
                $"Current effective business types ({activeBusinessTypeCount.Value}) exceed the target limit ({targetPlan.MaxActiveBusinessTypes}). Downgrade is blocked until optional activations are reduced; merchant catalog/history is not deleted."));
        }

        var lost = new List<string>();
        if (currentPlan.CustomerCreditEnabled && !targetPlan.CustomerCreditEnabled)
        {
            lost.Add("Customer credit");
        }

        if (currentPlan.AdvancedReportsEnabled && !targetPlan.AdvancedReportsEnabled)
        {
            lost.Add("Advanced reports");
        }

        if (currentPlan.ExportEnabled && !targetPlan.ExportEnabled)
        {
            lost.Add("Export");
        }

        return new PlanChangeImpactPreview(
            currentPlan.Id.Value,
            currentPlan.DisplayName,
            targetPlan.Id.Value,
            targetPlan.DisplayName,
            activeStaffCount,
            activeBranchCount,
            branchCountAvailable,
            branchCountUnavailableReason,
            conflicts,
            lost,
            conflicts.Count > 0);
    }
}

public sealed class PreviewOrganizationPlanChange
{
    private readonly ISubscriptionRepository _subscriptions;
    private readonly IPlanRepository _plans;
    private readonly IOrganizationProductUsageReader _usageReader;
    private readonly IOrganizationBusinessTypeEntitlementResolver _businessTypeEntitlements;

    public PreviewOrganizationPlanChange(
        ISubscriptionRepository subscriptions,
        IPlanRepository plans,
        IOrganizationProductUsageReader usageReader,
        IOrganizationBusinessTypeEntitlementResolver businessTypeEntitlements)
    {
        _subscriptions = subscriptions;
        _plans = plans;
        _usageReader = usageReader;
        _businessTypeEntitlements = businessTypeEntitlements;
    }

    public async Task<ApplicationResult<PlanChangeImpactPreview>> ExecuteAsync(
        PlatformOrganizationId organizationId,
        ProductCode productCode,
        PlanId targetPlanId,
        int? activeBranchCount = null,
        CancellationToken cancellationToken = default)
    {
        var targetPlan = await _plans.GetByIdAsync(targetPlanId, cancellationToken).ConfigureAwait(false);
        if (targetPlan is null || targetPlan.ProductCode != productCode)
        {
            return ApplicationResult<PlanChangeImpactPreview>.Failure(
                ApplicationErrorCodes.PlanNotFound,
                "Target plan was not found for this product.");
        }

        var subscription = await _subscriptions
            .GetCurrentForOrganizationProductAsync(organizationId, productCode, cancellationToken)
            .ConfigureAwait(false);
        if (subscription is null)
        {
            return ApplicationResult<PlanChangeImpactPreview>.Failure(
                ApplicationErrorCodes.SubscriptionNotFound,
                "No subscription exists for plan-change preview.");
        }

        var currentPlan = await _plans.GetByIdAsync(subscription.PlanId, cancellationToken).ConfigureAwait(false);
        if (currentPlan is null)
        {
            return ApplicationResult<PlanChangeImpactPreview>.Failure(
                ApplicationErrorCodes.PlanNotFound,
                "Current plan was not found.");
        }

        var usage = await _usageReader
            .GetUsageAsync(organizationId, productCode, cancellationToken)
            .ConfigureAwait(false);
        var branchCountAvailable = usage.BranchCountAvailable || activeBranchCount.HasValue;
        var branchCount = activeBranchCount ?? usage.ActiveBranchCount;
        var unavailableReason = branchCountAvailable ? null : usage.BranchCountUnavailableReason;

        int? activeBusinessTypeCount = null;
        var entitlement = await _businessTypeEntitlements
            .ResolveAsync(organizationId, productCode, cancellationToken)
            .ConfigureAwait(false);
        if (entitlement.IsSuccess && entitlement.Value is not null)
        {
            activeBusinessTypeCount = entitlement.Value.EffectiveBusinessTypeIds.Count;
        }

        return ApplicationResult<PlanChangeImpactPreview>.Success(
            PlanChangeImpact.Evaluate(
                currentPlan,
                targetPlan,
                usage.ActiveStaffCount,
                branchCount,
                branchCountAvailable,
                unavailableReason,
                activeBusinessTypeCount));
    }
}

public sealed class ScheduleOrganizationSubscriptionDowngrade
{
    private readonly ISubscriptionRepository _subscriptions;
    private readonly IPlanRepository _plans;
    private readonly IOrganizationBusinessTypeEntitlementResolver _businessTypeEntitlements;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public ScheduleOrganizationSubscriptionDowngrade(
        ISubscriptionRepository subscriptions,
        IPlanRepository plans,
        IOrganizationBusinessTypeEntitlementResolver businessTypeEntitlements,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _subscriptions = subscriptions;
        _plans = plans;
        _businessTypeEntitlements = businessTypeEntitlements;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<Subscription>> ExecuteAsync(
        PlatformOrganizationId organizationId,
        ProductCode productCode,
        PlanId targetPlanId,
        DateTimeOffset effectiveAtUtc,
        CancellationToken cancellationToken = default)
    {
        var targetPlan = await _plans.GetByIdAsync(targetPlanId, cancellationToken).ConfigureAwait(false);
        if (targetPlan is null || targetPlan.ProductCode != productCode || !targetPlan.AcceptsNewSubscriptions)
        {
            return ApplicationResult<Subscription>.Failure(
                ApplicationErrorCodes.PlanNotFound,
                "Target plan was not found or is not active for this product.");
        }

        var subscription = await _subscriptions
            .GetCurrentForOrganizationProductAsync(organizationId, productCode, cancellationToken)
            .ConfigureAwait(false);
        if (subscription is null || !Subscription.IsActiveLike(subscription.Status))
        {
            return ApplicationResult<Subscription>.Failure(
                ApplicationErrorCodes.SubscriptionNotFound,
                "No active-like subscription exists.");
        }

        if (subscription.PlanId == targetPlanId)
        {
            return ApplicationResult<Subscription>.Failure(
                ApplicationErrorCodes.SubscriptionIneligible,
                "Subscription is already on the requested plan.");
        }

        var entitlement = await _businessTypeEntitlements
            .ResolveAsync(organizationId, productCode, cancellationToken)
            .ConfigureAwait(false);
        if (entitlement.IsSuccess
            && entitlement.Value is not null
            && entitlement.Value.EffectiveBusinessTypeIds.Count > targetPlan.MaxActiveBusinessTypes)
        {
            return ApplicationResult<Subscription>.Failure(
                ApplicationErrorCodes.PlanDowngradeBlockedByBusinessTypeCapacity,
                $"Downgrade blocked: effective business types ({entitlement.Value.EffectiveBusinessTypeIds.Count}) exceed target plan capacity ({targetPlan.MaxActiveBusinessTypes}). Deactivate excess types before downgrading; merchant data is not deleted.");
        }

        try
        {
            subscription.SchedulePlanDowngrade(targetPlanId, effectiveAtUtc, _clock.UtcNow);
            await _subscriptions.UpdateAsync(subscription, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<Subscription>.Success(subscription);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<Subscription>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class ApplyDuePendingPlanChanges
{
    private readonly ISubscriptionRepository _subscriptions;
    private readonly IPlanRepository _plans;
    private readonly GenerateEntitlementSnapshot _generateSnapshot;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public ApplyDuePendingPlanChanges(
        ISubscriptionRepository subscriptions,
        IPlanRepository plans,
        GenerateEntitlementSnapshot generateSnapshot,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _subscriptions = subscriptions;
        _plans = plans;
        _generateSnapshot = generateSnapshot;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<int>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var utcNow = _clock.UtcNow;
        var due = await _subscriptions.ListDuePendingPlanChangesAsync(utcNow, cancellationToken).ConfigureAwait(false);
        var applied = 0;

        foreach (var subscription in due)
        {
            if (subscription.PendingPlanId is null)
            {
                continue;
            }

            var plan = await _plans.GetByIdAsync(subscription.PendingPlanId, cancellationToken).ConfigureAwait(false);
            if (plan is null)
            {
                continue;
            }

            var versions = await _plans.ListVersionsAsync(plan.Id, cancellationToken).ConfigureAwait(false);
            var version = versions.FirstOrDefault(v => v.Status == PlanVersionStatus.Published);
            if (version is null)
            {
                continue;
            }

            var cycle = subscription.BillingCycle ?? BillingCycle.Monthly;
            subscription.ApplyPendingPlanChange(plan, version, cycle, utcNow);
            await _subscriptions.UpdateAsync(subscription, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await _generateSnapshot
                .ExecuteAsync(subscription.OrganizationId, subscription.ProductCode, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            applied++;
        }

        return ApplicationResult<int>.Success(applied);
    }
}
