using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Subscriptions;
using ExItS.Platform.Domain.Catalog;
using ExItS.Platform.Domain.Entitlements;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Products;
using ExItS.Platform.Domain.Subscriptions;

namespace ExItS.Platform.UnitTests.Support;

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

    public Task AddAsync(FeatureDefinition feature, CancellationToken cancellationToken = default)
    {
        _items[Key(feature.ProductCode, feature.Code)] = feature;
        AddCount++;
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

    public Task AddAsync(TrialDefinition trial, CancellationToken cancellationToken = default)
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

internal sealed class InMemoryEntitlementSnapshotRepository : IEntitlementSnapshotRepository
{
    private readonly List<EntitlementSnapshot> _items = new();
    public int AddCount { get; private set; }

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

    public Task AddAsync(EntitlementSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        _items.Add(snapshot);
        AddCount++;
        return Task.CompletedTask;
    }
}
