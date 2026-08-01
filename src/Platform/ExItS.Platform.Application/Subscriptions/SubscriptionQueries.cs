using ExItS.Platform.Application.Catalog;
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
    int Version);

public sealed class SubscriptionQueryService
{
    private readonly ISubscriptionRepository _subscriptions;

    public SubscriptionQueryService(ISubscriptionRepository subscriptions)
    {
        _subscriptions = subscriptions;
    }

    public async Task<SubscriptionDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var subscription = await _subscriptions
            .GetByIdAsync(SubscriptionId.From(id), cancellationToken)
            .ConfigureAwait(false);

        return subscription is null ? null : Map(subscription);
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

        return subscription is null ? null : Map(subscription);
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

        return ToPagedResult(items, totalCount, page, take);
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

        return ToPagedResult(items, totalCount, page, take);
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

        return ToPagedResult(items, totalCount, page, take);
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

        return ToPagedResult(items, totalCount, page, take);
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

        return ToPagedResult(items, totalCount, page, take);
    }

    private static PagedResult<SubscriptionDto> ToPagedResult(
        IReadOnlyList<Subscription> items,
        int totalCount,
        int? page,
        int take)
    {
        var pageNumber = Math.Max(page ?? 1, 1);
        return new PagedResult<SubscriptionDto>(items.Select(Map).ToList(), totalCount, pageNumber, take);
    }

    private static SubscriptionDto Map(Subscription subscription) =>
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
            subscription.Version);
}
