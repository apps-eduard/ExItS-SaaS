using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Entitlements;
using ExItS.Platform.Application.Payments;
using ExItS.Platform.Application.Subscriptions;
using ExItS.Platform.Domain.Catalog;
using ExItS.Platform.Domain.Entitlements;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Payments;
using ExItS.Platform.Domain.Products;
using ExItS.Platform.Domain.Subscriptions;

namespace ExItS.Platform.UnitTests.Support;

internal sealed class NoOpUnitOfWork : IPlatformUnitOfWork
{
    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}

internal sealed class InMemoryProductRepository : IProductRepository
{
    private readonly Dictionary<Guid, Product> _byId = new();
    private readonly Dictionary<string, Guid> _byCode = new(StringComparer.Ordinal);
    public int AddCount { get; private set; }

    public Task<Product?> GetByIdAsync(ProductId id, CancellationToken cancellationToken = default)
    {
        _byId.TryGetValue(id.Value, out var p);
        return Task.FromResult(p);
    }

    public Task<Product?> GetByCodeAsync(ProductCode code, CancellationToken cancellationToken = default)
    {
        if (_byCode.TryGetValue(code.Value, out var id) && _byId.TryGetValue(id, out var p))
            return Task.FromResult<Product?>(p);
        return Task.FromResult<Product?>(null);
    }

    public Task<(IReadOnlyList<Product> Items, int TotalCount)> ListAsync(
        ProductStatus? status,
        int skip,
        int take,
        CancellationToken cancellationToken = default) =>
        ListAsync(status, null, CatalogListSortBy.Code, false, skip, take, cancellationToken);

    public Task<(IReadOnlyList<Product> Items, int TotalCount)> ListAsync(
        ProductStatus? status,
        string? search,
        CatalogListSortBy sortBy,
        bool sortDescending,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _byId.Values.AsEnumerable();
        if (status is not null)
            query = query.Where(p => p.Status == status);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(p =>
                p.Code.Value.Contains(term, StringComparison.OrdinalIgnoreCase)
                || p.DisplayName.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        query = (sortBy, sortDescending) switch
        {
            (CatalogListSortBy.DisplayName, false) => query.OrderBy(p => p.DisplayName).ThenBy(p => p.Code.Value),
            (CatalogListSortBy.DisplayName, true) => query.OrderByDescending(p => p.DisplayName).ThenBy(p => p.Code.Value),
            (CatalogListSortBy.Status, false) => query.OrderBy(p => p.Status).ThenBy(p => p.Code.Value),
            (CatalogListSortBy.Status, true) => query.OrderByDescending(p => p.Status).ThenBy(p => p.Code.Value),
            (_, true) => query.OrderByDescending(p => p.Code.Value),
            _ => query.OrderBy(p => p.Code.Value)
        };

        var ordered = query.ToList();
        var page = ordered.Skip(skip).Take(take).ToList();
        return Task.FromResult<(IReadOnlyList<Product>, int)>((page, ordered.Count));
    }

    public Task AddAsync(Product product, CancellationToken cancellationToken = default)
    {
        _byId[product.Id.Value] = product;
        _byCode[product.Code.Value] = product.Id.Value;
        AddCount++;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Product product, CancellationToken cancellationToken = default)
    {
        _byId[product.Id.Value] = product;
        return Task.CompletedTask;
    }
}

internal sealed class InMemoryFeatureDefinitionRepository : IFeatureDefinitionRepository
{
    private readonly Dictionary<string, FeatureDefinition> _items = new(StringComparer.Ordinal);
    public int AddCount { get; private set; }

    private static string Key(ProductCode p, FeatureCode f) => p.Value + "|" + f.Value;

    public Task<FeatureDefinition?> GetByProductAndCodeAsync(
        ProductCode productCode,
        FeatureCode featureCode,
        CancellationToken cancellationToken = default)
    {
        _items.TryGetValue(Key(productCode, featureCode), out var f);
        return Task.FromResult(f);
    }

    public Task<IReadOnlyList<FeatureDefinition>> ListByProductAsync(
        ProductCode productCode,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<FeatureDefinition> list = _items.Values
            .Where(f => f.ProductCode == productCode)
            .OrderBy(f => f.Code.Value, StringComparer.Ordinal)
            .ToList();
        return Task.FromResult(list);
    }

    public Task AddAsync(FeatureDefinition feature, CancellationToken cancellationToken = default)
    {
        _items[Key(feature.ProductCode, feature.Code)] = feature;
        AddCount++;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(FeatureDefinition feature, CancellationToken cancellationToken = default)
    {
        _items[Key(feature.ProductCode, feature.Code)] = feature;
        return Task.CompletedTask;
    }
}

internal sealed class InMemoryPlanRepository : IPlanRepository
{
    private readonly Dictionary<Guid, Plan> _plans = new();
    private readonly Dictionary<string, Guid> _planCodes = new(StringComparer.Ordinal);
    private readonly Dictionary<Guid, PlanVersion> _versions = new();
    public int AddPlanCount { get; private set; }
    public int AddVersionCount { get; private set; }

    public Task<Plan?> GetByIdAsync(PlanId id, CancellationToken cancellationToken = default)
    {
        _plans.TryGetValue(id.Value, out var p);
        return Task.FromResult(p);
    }

    public Task<Plan?> GetByProductAndCodeAsync(ProductCode productCode, PlanCode planCode, CancellationToken cancellationToken = default)
    {
        var key = productCode.Value + "|" + planCode.Value;
        if (_planCodes.TryGetValue(key, out var id) && _plans.TryGetValue(id, out var p))
            return Task.FromResult<Plan?>(p);
        return Task.FromResult<Plan?>(null);
    }

    public Task<IReadOnlyList<Plan>> ListByProductAsync(ProductCode productCode, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Plan> list = _plans.Values
            .Where(p => p.ProductCode == productCode)
            .OrderBy(p => p.Code.Value, StringComparer.Ordinal)
            .ToList();
        return Task.FromResult(list);
    }

    public Task<(IReadOnlyList<Plan> Items, int TotalCount)> ListAsync(
        ProductCode? productCode,
        PlanStatus? status,
        string? search,
        CatalogListSortBy sortBy,
        bool sortDescending,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _plans.Values.AsEnumerable();
        if (productCode is not null)
            query = query.Where(p => p.ProductCode == productCode);
        if (status is not null)
            query = query.Where(p => p.Status == status);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(p =>
                p.Code.Value.Contains(term, StringComparison.OrdinalIgnoreCase)
                || p.DisplayName.Contains(term, StringComparison.OrdinalIgnoreCase)
                || p.ProductCode.Value.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        query = (sortBy, sortDescending) switch
        {
            (CatalogListSortBy.DisplayName, false) => query.OrderBy(p => p.DisplayName).ThenBy(p => p.Code.Value),
            (CatalogListSortBy.DisplayName, true) => query.OrderByDescending(p => p.DisplayName).ThenBy(p => p.Code.Value),
            (CatalogListSortBy.ProductCode, false) => query.OrderBy(p => p.ProductCode.Value).ThenBy(p => p.Code.Value),
            (CatalogListSortBy.ProductCode, true) => query.OrderByDescending(p => p.ProductCode.Value).ThenBy(p => p.Code.Value),
            (CatalogListSortBy.SortOrder, false) => query.OrderBy(p => p.SortOrder).ThenBy(p => p.Code.Value),
            (CatalogListSortBy.SortOrder, true) => query.OrderByDescending(p => p.SortOrder).ThenBy(p => p.Code.Value),
            (CatalogListSortBy.MonthlyPrice, false) => query.OrderBy(p => p.MonthlyPrice).ThenBy(p => p.Code.Value),
            (CatalogListSortBy.MonthlyPrice, true) => query.OrderByDescending(p => p.MonthlyPrice).ThenBy(p => p.Code.Value),
            (CatalogListSortBy.AnnualPrice, false) => query.OrderBy(p => p.AnnualPrice).ThenBy(p => p.Code.Value),
            (CatalogListSortBy.AnnualPrice, true) => query.OrderByDescending(p => p.AnnualPrice).ThenBy(p => p.Code.Value),
            (CatalogListSortBy.CurrencyCode, false) => query.OrderBy(p => p.CurrencyCode, StringComparer.OrdinalIgnoreCase).ThenBy(p => p.Code.Value),
            (CatalogListSortBy.CurrencyCode, true) => query.OrderByDescending(p => p.CurrencyCode, StringComparer.OrdinalIgnoreCase).ThenBy(p => p.Code.Value),
            (_, true) => query.OrderByDescending(p => p.Code.Value),
            _ => query.OrderBy(p => p.Code.Value)
        };

        var ordered = query.ToList();
        return Task.FromResult<(IReadOnlyList<Plan>, int)>((ordered.Skip(skip).Take(take).ToList(), ordered.Count));
    }

    public Task AddAsync(Plan plan, CancellationToken cancellationToken = default)
    {
        _plans[plan.Id.Value] = plan;
        _planCodes[plan.ProductCode.Value + "|" + plan.Code.Value] = plan.Id.Value;
        AddPlanCount++;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Plan plan, CancellationToken cancellationToken = default)
    {
        _plans[plan.Id.Value] = plan;
        return Task.CompletedTask;
    }

    public Task<PlanVersion?> GetVersionByIdAsync(PlanVersionId id, CancellationToken cancellationToken = default)
    {
        _versions.TryGetValue(id.Value, out var v);
        return Task.FromResult(v);
    }

    public Task<PlanVersion?> GetVersionByPlanAndNumberAsync(PlanId planId, int versionNumber, CancellationToken cancellationToken = default)
    {
        var version = _versions.Values.FirstOrDefault(v => v.PlanId == planId && v.VersionNumber == versionNumber);
        return Task.FromResult(version);
    }

    public Task<IReadOnlyList<PlanVersion>> ListVersionsAsync(PlanId planId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<PlanVersion> list = _versions.Values
            .Where(v => v.PlanId == planId)
            .OrderBy(v => v.VersionNumber)
            .ToList();
        return Task.FromResult(list);
    }

    public Task<PlanVersion?> GetLatestPublishedVersionAsync(PlanId planId, CancellationToken cancellationToken = default)
    {
        var version = _versions.Values
            .Where(v => v.PlanId == planId && v.Status == PlanVersionStatus.Published)
            .OrderByDescending(v => v.VersionNumber)
            .FirstOrDefault();
        return Task.FromResult(version);
    }

    public Task<int> GetMaxVersionNumberAsync(PlanId planId, CancellationToken cancellationToken = default)
    {
        var max = _versions.Values.Where(v => v.PlanId == planId).Select(v => v.VersionNumber).DefaultIfEmpty(0).Max();
        return Task.FromResult(max);
    }

    public Task AddVersionAsync(PlanVersion version, CancellationToken cancellationToken = default)
    {
        _versions[version.Id.Value] = version;
        AddVersionCount++;
        return Task.CompletedTask;
    }

    public Task UpdateVersionAsync(PlanVersion version, CancellationToken cancellationToken = default)
    {
        _versions[version.Id.Value] = version;
        return Task.CompletedTask;
    }
}

internal sealed class InMemoryTrialDefinitionRepository : ITrialDefinitionRepository
{
    private readonly Dictionary<Guid, TrialDefinition> _items = new();

    public Task<TrialDefinition?> GetByIdAsync(TrialDefinitionId id, CancellationToken cancellationToken = default)
    {
        _items.TryGetValue(id.Value, out var t);
        return Task.FromResult(t);
    }

    public Task<IReadOnlyList<TrialDefinition>> ListByProductAsync(ProductCode productCode, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<TrialDefinition> list = _items.Values
            .Where(t => t.ProductCode == productCode)
            .OrderBy(t => t.DisplayName, StringComparer.Ordinal)
            .ToList();
        return Task.FromResult(list);
    }

    public Task AddAsync(TrialDefinition trial, CancellationToken cancellationToken = default)
    {
        _items[trial.Id.Value] = trial;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(TrialDefinition trial, CancellationToken cancellationToken = default)
    {
        _items[trial.Id.Value] = trial;
        return Task.CompletedTask;
    }
}

internal sealed class InMemorySubscriptionRepository : ISubscriptionRepository
{
    private readonly Dictionary<Guid, Subscription> _items = new();
    public int AddCount { get; private set; }
    public int UpdateCount { get; private set; }

    public Task<Subscription?> GetByIdAsync(SubscriptionId id, CancellationToken cancellationToken = default)
    {
        _items.TryGetValue(id.Value, out var s);
        return Task.FromResult(s);
    }

    public Task<Subscription?> GetCurrentForOrganizationProductAsync(
        PlatformOrganizationId organizationId,
        ProductCode productCode,
        CancellationToken cancellationToken = default)
    {
        var current = _items.Values
            .Where(s => s.OrganizationId == organizationId && s.ProductCode == productCode)
            .OrderByDescending(s => Subscription.IsActiveLike(s.Status))
            .ThenByDescending(s => s.UpdatedAtUtc)
            .FirstOrDefault();

        return Task.FromResult(current);
    }

    public Task<(IReadOnlyList<Subscription> Items, int TotalCount)> ListByOrganizationAsync(
        PlatformOrganizationId organizationId,
        SubscriptionStatus? status,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _items.Values.Where(s => s.OrganizationId == organizationId);
        if (status is not null)
        {
            query = query.Where(s => s.Status == status.Value);
        }

        var ordered = query.OrderByDescending(s => s.CreatedAtUtc).ToList();
        IReadOnlyList<Subscription> page = ordered.Skip(skip).Take(take).ToList();
        return Task.FromResult((page, ordered.Count));
    }

    public Task<(IReadOnlyList<Subscription> Items, int TotalCount)> ListByProductAsync(
        ProductCode productCode,
        SubscriptionStatus? status,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _items.Values.Where(s => s.ProductCode == productCode);
        if (status is not null)
        {
            query = query.Where(s => s.Status == status.Value);
        }

        var ordered = query.OrderByDescending(s => s.CreatedAtUtc).ToList();
        IReadOnlyList<Subscription> page = ordered.Skip(skip).Take(take).ToList();
        return Task.FromResult((page, ordered.Count));
    }

    public Task<(IReadOnlyList<Subscription> Items, int TotalCount)> ListExpiringTrialsAsync(
        DateTimeOffset asOfUtc,
        DateTimeOffset throughUtc,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var ordered = _items.Values
            .Where(s => s.Status == SubscriptionStatus.Trialing
                        && s.TrialEndUtc is not null
                        && s.TrialEndUtc.Value >= asOfUtc
                        && s.TrialEndUtc.Value <= throughUtc)
            .OrderBy(s => s.TrialEndUtc)
            .ToList();
        IReadOnlyList<Subscription> page = ordered.Skip(skip).Take(take).ToList();
        return Task.FromResult((page, ordered.Count));
    }

    public Task<IReadOnlyList<Subscription>> ListDuePendingPlanChangesAsync(
        DateTimeOffset asOfUtc,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Subscription> due = _items.Values
            .Where(s => s.PendingPlanId is not null
                        && s.PendingPlanEffectiveAtUtc is not null
                        && s.PendingPlanEffectiveAtUtc.Value <= asOfUtc)
            .OrderBy(s => s.PendingPlanEffectiveAtUtc)
            .ToList();
        return Task.FromResult(due);
    }

    public Task<(IReadOnlyList<Subscription> Items, int TotalCount)> ListByStatusAsync(
        SubscriptionStatus status,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var ordered = _items.Values.Where(s => s.Status == status).OrderByDescending(s => s.UpdatedAtUtc).ToList();
        IReadOnlyList<Subscription> page = ordered.Skip(skip).Take(take).ToList();
        return Task.FromResult((page, ordered.Count));
    }

    public Task<(IReadOnlyList<Subscription> Items, int TotalCount)> ListAsync(
        PlatformOrganizationId? organizationId,
        ProductCode? productCode,
        SubscriptionStatus? status,
        string? search,
        bool? isTrial,
        Guid? planId,
        SubscriptionListSortBy sortBy,
        bool sortDescending,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        IEnumerable<Subscription> query = _items.Values;
        if (organizationId is not null)
        {
            query = query.Where(s => s.OrganizationId == organizationId);
        }

        if (productCode is not null)
        {
            query = query.Where(s => s.ProductCode == productCode);
        }

        if (status is not null)
        {
            query = query.Where(s => s.Status == status.Value);
        }

        if (planId is not null)
        {
            query = query.Where(s => s.PlanId.Value == planId.Value);
        }

        if (isTrial is true)
        {
            query = query.Where(s => s.TrialDefinitionId is not null || s.Status == SubscriptionStatus.Trialing);
        }
        else if (isTrial is false)
        {
            query = query.Where(s => s.TrialDefinitionId is null && s.Status != SubscriptionStatus.Trialing);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(s =>
                s.ProductCode.Value.Contains(term, StringComparison.OrdinalIgnoreCase)
                || s.Status.ToString().Contains(term, StringComparison.OrdinalIgnoreCase)
                || s.OrganizationId.Value.ToString().Contains(term, StringComparison.OrdinalIgnoreCase)
                || s.PlanId.Value.ToString().Contains(term, StringComparison.OrdinalIgnoreCase)
                || s.Id.Value.ToString().Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        query = (sortBy, sortDescending) switch
        {
            (SubscriptionListSortBy.Status, false) => query.OrderBy(s => s.Status).ThenByDescending(s => s.UpdatedAtUtc),
            (SubscriptionListSortBy.Status, true) => query.OrderByDescending(s => s.Status).ThenByDescending(s => s.UpdatedAtUtc),
            (SubscriptionListSortBy.ProductCode, false) => query.OrderBy(s => s.ProductCode.Value).ThenByDescending(s => s.UpdatedAtUtc),
            (SubscriptionListSortBy.ProductCode, true) => query.OrderByDescending(s => s.ProductCode.Value).ThenByDescending(s => s.UpdatedAtUtc),
            (SubscriptionListSortBy.TrialEndUtc, false) => query.OrderBy(s => s.TrialEndUtc).ThenByDescending(s => s.UpdatedAtUtc),
            (SubscriptionListSortBy.TrialEndUtc, true) => query.OrderByDescending(s => s.TrialEndUtc).ThenByDescending(s => s.UpdatedAtUtc),
            (SubscriptionListSortBy.PaidPeriodEndUtc, false) => query.OrderBy(s => s.PaidPeriodEndUtc).ThenByDescending(s => s.UpdatedAtUtc),
            (SubscriptionListSortBy.PaidPeriodEndUtc, true) => query.OrderByDescending(s => s.PaidPeriodEndUtc).ThenByDescending(s => s.UpdatedAtUtc),
            (SubscriptionListSortBy.CreatedAtUtc, false) => query.OrderBy(s => s.CreatedAtUtc),
            (SubscriptionListSortBy.CreatedAtUtc, true) => query.OrderByDescending(s => s.CreatedAtUtc),
            (SubscriptionListSortBy.UpdatedAtUtc, false) => query.OrderBy(s => s.UpdatedAtUtc),
            _ => query.OrderByDescending(s => s.UpdatedAtUtc)
        };

        var ordered = query.ToList();
        IReadOnlyList<Subscription> page = ordered.Skip(skip).Take(take).ToList();
        return Task.FromResult((page, ordered.Count));
    }

    public Task<bool> ExistsActiveLikeAsync(
        PlatformOrganizationId organizationId,
        ProductCode productCode,
        CancellationToken cancellationToken = default)
    {
        var exists = _items.Values.Any(s =>
            s.OrganizationId == organizationId && s.ProductCode == productCode && Subscription.IsActiveLike(s.Status));
        return Task.FromResult(exists);
    }

    public Task<bool> HasConsumedTrialAsync(
        PlatformOrganizationId organizationId,
        ProductCode productCode,
        CancellationToken cancellationToken = default)
    {
        var consumed = _items.Values.Any(s =>
            s.OrganizationId == organizationId
            && s.ProductCode == productCode
            && (s.TrialStartUtc is not null || s.TrialDefinitionId is not null));
        return Task.FromResult(consumed);
    }

    public Task AddAsync(Subscription subscription, CancellationToken cancellationToken = default)
    {
        _items[subscription.Id.Value] = subscription;
        AddCount++;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Subscription subscription, CancellationToken cancellationToken = default)
    {
        _items[subscription.Id.Value] = subscription;
        UpdateCount++;
        return Task.CompletedTask;
    }
}

internal sealed class InMemoryFeatureOverrideRepository : IFeatureOverrideRepository
{
    private readonly Dictionary<Guid, FeatureOverride> _items = new();
    public int AddCount { get; private set; }

    public Task<FeatureOverride?> GetByIdAsync(FeatureOverrideId id, CancellationToken cancellationToken = default)
    {
        _items.TryGetValue(id.Value, out var o);
        return Task.FromResult(o);
    }

    public Task<IReadOnlyList<FeatureOverride>> ListActiveForOrganizationProductAsync(
        PlatformOrganizationId organizationId,
        ProductCode productCode,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<FeatureOverride> list = _items.Values
            .Where(o => o.OrganizationId == organizationId && o.ProductCode == productCode && o.IsActiveAt(utcNow))
            .ToList();
        return Task.FromResult(list);
    }

    public Task<(IReadOnlyList<FeatureOverride> Items, int TotalCount)> ListByOrganizationProductAsync(
        PlatformOrganizationId organizationId,
        ProductCode productCode,
        FeatureOverrideStatus? status,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _items.Values
            .Where(o => o.OrganizationId == organizationId && o.ProductCode == productCode);
        if (status is not null)
        {
            query = query.Where(o => o.Status == status.Value);
        }

        var ordered = query.OrderByDescending(o => o.CreatedAtUtc).ToList();
        IReadOnlyList<FeatureOverride> page = ordered.Skip(skip).Take(take).ToList();
        return Task.FromResult((page, ordered.Count));
    }

    public Task AddAsync(FeatureOverride featureOverride, CancellationToken cancellationToken = default)
    {
        _items[featureOverride.Id.Value] = featureOverride;
        AddCount++;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(FeatureOverride featureOverride, CancellationToken cancellationToken = default)
    {
        _items[featureOverride.Id.Value] = featureOverride;
        return Task.CompletedTask;
    }
}

internal sealed class InMemorySaaSPaymentRepository : ISaaSPaymentRepository
{
    private readonly Dictionary<Guid, SaaSPayment> _items = new();
    public int AddCount { get; private set; }
    public int UpdateCount { get; private set; }

    public Task<SaaSPayment?> GetByIdAsync(SaaSPaymentId id, CancellationToken cancellationToken = default)
    {
        _items.TryGetValue(id.Value, out var p);
        return Task.FromResult(p);
    }

    public Task<bool> ExistsByNormalizedReferenceAsync(
        SaaSPaymentMethod method,
        string normalizedReference,
        PlatformOrganizationId orgId,
        CancellationToken cancellationToken = default)
    {
        var exists = _items.Values.Any(p =>
            p.Method == method
            && p.NormalizedReference == normalizedReference
            && p.OrganizationId == orgId
            && p.Status is not (SaaSPaymentStatus.Rejected or SaaSPaymentStatus.Voided));
        return Task.FromResult(exists);
    }

    public Task<SaaSPayment?> GetByNormalizedReferenceAsync(
        SaaSPaymentMethod method,
        string normalizedReference,
        PlatformOrganizationId orgId,
        CancellationToken cancellationToken = default)
    {
        var found = _items.Values.FirstOrDefault(p =>
            p.Method == method && p.NormalizedReference == normalizedReference && p.OrganizationId == orgId);
        return Task.FromResult(found);
    }

    public Task<(IReadOnlyList<SaaSPayment> Items, int TotalCount)> ListByOrganizationAsync(
        PlatformOrganizationId orgId,
        SaaSPaymentStatus? status,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _items.Values.Where(p => p.OrganizationId == orgId);
        if (status is not null)
        {
            query = query.Where(p => p.Status == status.Value);
        }

        var ordered = query.OrderByDescending(p => p.CreatedAtUtc).ToList();
        IReadOnlyList<SaaSPayment> page = ordered.Skip(skip).Take(take).ToList();
        return Task.FromResult((page, ordered.Count));
    }

    public Task<(IReadOnlyList<SaaSPayment> Items, int TotalCount)> ListByProductAsync(
        ProductCode productCode,
        SaaSPaymentStatus? status,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _items.Values.Where(p => p.ProductCode == productCode);
        if (status is not null)
        {
            query = query.Where(p => p.Status == status.Value);
        }

        var ordered = query.OrderByDescending(p => p.CreatedAtUtc).ToList();
        IReadOnlyList<SaaSPayment> page = ordered.Skip(skip).Take(take).ToList();
        return Task.FromResult((page, ordered.Count));
    }

    public Task<(IReadOnlyList<SaaSPayment> Items, int TotalCount)> ListByStatusAsync(
        SaaSPaymentStatus status,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var ordered = _items.Values.Where(p => p.Status == status).OrderByDescending(p => p.CreatedAtUtc).ToList();
        IReadOnlyList<SaaSPayment> page = ordered.Skip(skip).Take(take).ToList();
        return Task.FromResult((page, ordered.Count));
    }

    public Task<(IReadOnlyList<SaaSPayment> Items, int TotalCount)> ListBySubscriptionAsync(
        SubscriptionId subscriptionId,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var ordered = _items.Values
            .Where(p => p.SubscriptionId == subscriptionId)
            .OrderByDescending(p => p.CreatedAtUtc)
            .ToList();
        IReadOnlyList<SaaSPayment> page = ordered.Skip(skip).Take(take).ToList();
        return Task.FromResult((page, ordered.Count));
    }

    public Task AddAsync(SaaSPayment payment, CancellationToken cancellationToken = default)
    {
        _items[payment.Id.Value] = payment;
        AddCount++;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(SaaSPayment payment, CancellationToken cancellationToken = default)
    {
        _items[payment.Id.Value] = payment;
        UpdateCount++;
        return Task.CompletedTask;
    }
}

internal sealed class InMemoryEntitlementSnapshotRepository : IEntitlementSnapshotRepository
{
    private readonly List<EntitlementSnapshot> _items = new();
    public int AddCount { get; private set; }

    public Task<EntitlementSnapshot?> GetByIdAsync(
        EntitlementSnapshotId id,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_items.FirstOrDefault(s => s.Id == id));

    public Task<EntitlementSnapshot?> GetLatestForOrganizationProductAsync(
        PlatformOrganizationId organizationId,
        ProductCode productCode,
        CancellationToken cancellationToken = default)
    {
        var latest = _items
            .Where(s => s.OrganizationId == organizationId && s.ProductCode == productCode)
            .OrderByDescending(s => s.SnapshotVersion)
            .FirstOrDefault();
        return Task.FromResult(latest);
    }

    public Task<EntitlementSnapshot?> GetByVersionAsync(
        PlatformOrganizationId organizationId,
        ProductCode productCode,
        int snapshotVersion,
        CancellationToken cancellationToken = default)
    {
        var found = _items.FirstOrDefault(s =>
            s.OrganizationId == organizationId
            && s.ProductCode == productCode
            && s.SnapshotVersion == snapshotVersion);
        return Task.FromResult(found);
    }

    public Task<int?> GetLatestSnapshotVersionAsync(
        PlatformOrganizationId organizationId,
        ProductCode productCode,
        CancellationToken cancellationToken = default)
    {
        var max = _items
            .Where(s => s.OrganizationId == organizationId && s.ProductCode == productCode)
            .Select(s => (int?)s.SnapshotVersion)
            .DefaultIfEmpty(null)
            .Max();
        return Task.FromResult(max);
    }

    public Task<(IReadOnlyList<EntitlementSnapshot> Items, int TotalCount)> ListHistoryAsync(
        PlatformOrganizationId organizationId,
        ProductCode productCode,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var ordered = _items
            .Where(s => s.OrganizationId == organizationId && s.ProductCode == productCode)
            .OrderByDescending(s => s.SnapshotVersion)
            .ToList();
        IReadOnlyList<EntitlementSnapshot> page = ordered.Skip(skip).Take(take).ToList();
        return Task.FromResult((page, ordered.Count));
    }

    public Task AddAsync(EntitlementSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        _items.Add(snapshot);
        AddCount++;
        return Task.CompletedTask;
    }
}

internal sealed class InMemoryProviderPaymentRepository : IProviderPaymentRepository
{
    private readonly Dictionary<string, ProviderPayment> _byIdempotency = new(StringComparer.Ordinal);
    private int _sequence;

    public int Count => _byIdempotency.Count;

    public Task<ProviderPayment?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default)
    {
        _byIdempotency.TryGetValue(idempotencyKey, out var payment);
        return Task.FromResult(payment);
    }

    public Task AddAsync(ProviderPayment payment, CancellationToken cancellationToken = default)
    {
        _byIdempotency[payment.IdempotencyKey] = payment;
        return Task.CompletedTask;
    }

    public Task<int> GetNextSequenceAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(Interlocked.Increment(ref _sequence));
}
