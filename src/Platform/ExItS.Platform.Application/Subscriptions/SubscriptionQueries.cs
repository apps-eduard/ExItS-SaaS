using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Domain.Catalog;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Products;
using ExItS.Platform.Domain.Subscriptions;

namespace ExItS.Platform.Application.Subscriptions;

public sealed record SubscriptionDto(
    Guid Id,
    Guid OrganizationId,
    string ProductCode,
    Guid PlanId,
    Guid PlanVersionId,
    Guid? TrialDefinitionId,
    string Status,
    DateTimeOffset? TrialStartUtc,
    DateTimeOffset? TrialEndUtc,
    DateTimeOffset? PaidPeriodStartUtc,
    DateTimeOffset? PaidPeriodEndUtc,
    DateTimeOffset? GracePeriodEndUtc,
    DateTimeOffset? SuspendedAtUtc,
    DateTimeOffset? PastDueAtUtc,
    DateTimeOffset? CancelledAtUtc,
    DateTimeOffset? ExpiredAtUtc,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    int Version,
    string? BillingCycle = null,
    decimal? AgreedPrice = null,
    string? CurrencyCode = null,
    DateTimeOffset? PriceEffectiveFromUtc = null,
    Guid? PendingPlanId = null,
    string? PendingPlanKey = null,
    string? PendingPlanDisplayName = null,
    DateTimeOffset? PendingPlanEffectiveAtUtc = null,
    DateTimeOffset? CurrentPeriodStartUtc = null,
    DateTimeOffset? CurrentPeriodEndUtc = null,
    string? OrganizationDisplayName = null,
    string? ProductDisplayName = null,
    string? PlanDisplayName = null,
    string? PlanKey = null,
    DateTimeOffset? RenewalDateUtc = null);

public sealed class SubscriptionQueryService
{
    private readonly ISubscriptionRepository _subscriptions;
    private readonly IPlatformOrganizationRepository _organizations;
    private readonly IProductRepository _products;
    private readonly IPlanRepository _plans;

    public SubscriptionQueryService(
        ISubscriptionRepository subscriptions,
        IPlatformOrganizationRepository organizations,
        IProductRepository products,
        IPlanRepository plans)
    {
        _subscriptions = subscriptions;
        _organizations = organizations;
        _products = products;
        _plans = plans;
    }

    public async Task<SubscriptionDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var subscription = await _subscriptions
            .GetByIdAsync(SubscriptionId.From(id), cancellationToken)
            .ConfigureAwait(false);

        return subscription is null ? null : await MapAsync(subscription, cancellationToken).ConfigureAwait(false);
    }

    public async Task<SubscriptionDto?> GetCurrentAsync(
        Guid organizationId,
        string productCode,
        CancellationToken cancellationToken = default)
    {
        var subscription = await _subscriptions
            .GetCurrentForOrganizationProductAsync(
                PlatformOrganizationId.From(organizationId),
                ProductCode.Create(productCode),
                cancellationToken)
            .ConfigureAwait(false);

        return subscription is null ? null : await MapAsync(subscription, cancellationToken).ConfigureAwait(false);
    }

    public async Task<PagedResult<SubscriptionDto>> ListByOrganizationAsync(
        Guid organizationId,
        SubscriptionStatus? status,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken = default)
    {
        var (skip, take) = CatalogPagination.Normalize(page, pageSize);
        var (items, totalCount) = await _subscriptions
            .ListByOrganizationAsync(PlatformOrganizationId.From(organizationId), status, skip, take, cancellationToken)
            .ConfigureAwait(false);

        return await ToPagedResultAsync(items, totalCount, page, take, cancellationToken).ConfigureAwait(false);
    }

    public async Task<PagedResult<SubscriptionDto>> ListByProductAsync(
        string productCode,
        SubscriptionStatus? status,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken = default)
    {
        var (skip, take) = CatalogPagination.Normalize(page, pageSize);
        var (items, totalCount) = await _subscriptions
            .ListByProductAsync(ProductCode.Create(productCode), status, skip, take, cancellationToken)
            .ConfigureAwait(false);

        return await ToPagedResultAsync(items, totalCount, page, take, cancellationToken).ConfigureAwait(false);
    }

    public async Task<PagedResult<SubscriptionDto>> ListExpiringTrialsAsync(
        DateTimeOffset asOfUtc,
        DateTimeOffset throughUtc,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken = default)
    {
        var (skip, take) = CatalogPagination.Normalize(page, pageSize);
        var (items, totalCount) = await _subscriptions
            .ListExpiringTrialsAsync(asOfUtc, throughUtc, skip, take, cancellationToken)
            .ConfigureAwait(false);

        return await ToPagedResultAsync(items, totalCount, page, take, cancellationToken).ConfigureAwait(false);
    }

    public Task<PagedResult<SubscriptionDto>> ListGracePeriodAsync(
        int? page,
        int? pageSize,
        CancellationToken cancellationToken = default) =>
        ListByStatusAsync(SubscriptionStatus.GracePeriod, page, pageSize, cancellationToken);

    public Task<PagedResult<SubscriptionDto>> ListPastDueAsync(
        int? page,
        int? pageSize,
        CancellationToken cancellationToken = default) =>
        ListByStatusAsync(SubscriptionStatus.PastDue, page, pageSize, cancellationToken);

    public async Task<PagedResult<SubscriptionDto>> ListByStatusAsync(
        SubscriptionStatus status,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken = default)
    {
        var (skip, take) = CatalogPagination.Normalize(page, pageSize);
        var (items, totalCount) = await _subscriptions
            .ListByStatusAsync(status, skip, take, cancellationToken)
            .ConfigureAwait(false);

        return await ToPagedResultAsync(items, totalCount, page, take, cancellationToken).ConfigureAwait(false);
    }

    public async Task<PagedResult<SubscriptionDto>> ListAsync(
        Guid? organizationId,
        string? productCode,
        SubscriptionStatus? status,
        string? search,
        bool? isTrial,
        Guid? planId,
        SubscriptionListSortBy sortBy,
        bool sortDescending,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken = default)
    {
        var (skip, take) = CatalogPagination.Normalize(page, pageSize);
        PlatformOrganizationId? orgId = organizationId is null
            ? null
            : PlatformOrganizationId.From(organizationId.Value);
        ProductCode? code = string.IsNullOrWhiteSpace(productCode)
            ? null
            : ProductCode.Create(productCode);

        var (items, totalCount) = await _subscriptions
            .ListAsync(
                orgId,
                code,
                status,
                search,
                isTrial,
                planId,
                sortBy,
                sortDescending,
                skip,
                take,
                cancellationToken)
            .ConfigureAwait(false);

        return await ToPagedResultAsync(items, totalCount, page, take, cancellationToken).ConfigureAwait(false);
    }

    private async Task<PagedResult<SubscriptionDto>> ToPagedResultAsync(
        IReadOnlyList<Subscription> items,
        int totalCount,
        int? page,
        int take,
        CancellationToken cancellationToken)
    {
        var pageNumber = Math.Max(page ?? 1, 1);
        var mapped = new List<SubscriptionDto>(items.Count);
        foreach (var item in items)
        {
            try
            {
                mapped.Add(await MapAsync(item, cancellationToken).ConfigureAwait(false));
            }
            catch
            {
                mapped.Add(MapMinimal(item));
            }
        }

        return new PagedResult<SubscriptionDto>(mapped, totalCount, pageNumber, take);
    }

    private static SubscriptionDto MapMinimal(Subscription subscription) =>
        new(
            subscription.Id.Value,
            subscription.OrganizationId.Value,
            subscription.ProductCode.Value,
            subscription.PlanId.Value,
            subscription.PlanVersionId.Value,
            subscription.TrialDefinitionId?.Value,
            subscription.Status.ToString(),
            subscription.TrialStartUtc,
            subscription.TrialEndUtc,
            subscription.PaidPeriodStartUtc,
            subscription.PaidPeriodEndUtc,
            subscription.GracePeriodEndUtc,
            subscription.SuspendedAtUtc,
            subscription.PastDueAtUtc,
            subscription.CancelledAtUtc,
            subscription.ExpiredAtUtc,
            subscription.CreatedAtUtc,
            subscription.UpdatedAtUtc,
            subscription.Version,
            BillingCycle: subscription.BillingCycle?.ToString(),
            AgreedPrice: subscription.AgreedPrice,
            CurrencyCode: subscription.CurrencyCode,
            PriceEffectiveFromUtc: subscription.PriceEffectiveFromUtc,
            PendingPlanId: subscription.PendingPlanId?.Value,
            PendingPlanEffectiveAtUtc: subscription.PendingPlanEffectiveAtUtc,
            CurrentPeriodStartUtc: subscription.PaidPeriodStartUtc,
            CurrentPeriodEndUtc: subscription.PaidPeriodEndUtc,
            RenewalDateUtc: subscription.PaidPeriodEndUtc);

    private async Task<SubscriptionDto> MapAsync(Subscription subscription, CancellationToken cancellationToken)
    {
        var org = await _organizations.GetByIdAsync(subscription.OrganizationId, cancellationToken).ConfigureAwait(false);
        var product = await _products.GetByCodeAsync(subscription.ProductCode, cancellationToken).ConfigureAwait(false);
        var plan = await _plans.GetByIdAsync(subscription.PlanId, cancellationToken).ConfigureAwait(false);
        Plan? pendingPlan = null;
        if (subscription.PendingPlanId is not null)
        {
            pendingPlan = await _plans.GetByIdAsync(subscription.PendingPlanId, cancellationToken).ConfigureAwait(false);
        }

        return new SubscriptionDto(
            subscription.Id.Value,
            subscription.OrganizationId.Value,
            subscription.ProductCode.Value,
            subscription.PlanId.Value,
            subscription.PlanVersionId.Value,
            subscription.TrialDefinitionId?.Value,
            subscription.Status.ToString(),
            subscription.TrialStartUtc,
            subscription.TrialEndUtc,
            subscription.PaidPeriodStartUtc,
            subscription.PaidPeriodEndUtc,
            subscription.GracePeriodEndUtc,
            subscription.SuspendedAtUtc,
            subscription.PastDueAtUtc,
            subscription.CancelledAtUtc,
            subscription.ExpiredAtUtc,
            subscription.CreatedAtUtc,
            subscription.UpdatedAtUtc,
            subscription.Version,
            BillingCycle: subscription.BillingCycle?.ToString(),
            AgreedPrice: subscription.AgreedPrice,
            CurrencyCode: subscription.CurrencyCode,
            PriceEffectiveFromUtc: subscription.PriceEffectiveFromUtc,
            PendingPlanId: subscription.PendingPlanId?.Value,
            PendingPlanKey: pendingPlan?.PlanKey,
            PendingPlanDisplayName: pendingPlan?.DisplayName,
            PendingPlanEffectiveAtUtc: subscription.PendingPlanEffectiveAtUtc,
            CurrentPeriodStartUtc: subscription.PaidPeriodStartUtc,
            CurrentPeriodEndUtc: subscription.PaidPeriodEndUtc,
            OrganizationDisplayName: org?.DisplayName,
            ProductDisplayName: product?.DisplayName,
            PlanDisplayName: plan?.DisplayName,
            PlanKey: plan?.PlanKey,
            RenewalDateUtc: subscription.PaidPeriodEndUtc);
    }
}
