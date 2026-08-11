using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.GlobalCatalog;
using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Application.Subscriptions;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Catalog;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.GlobalCatalog;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Products;
using ExItS.Platform.Domain.Subscriptions;

namespace ExItS.Platform.UnitTests.Organizations;

public sealed class OrganizationBusinessTypeEntitlementResolverTests
{
    private static readonly DateTimeOffset T0 = new(2026, 8, 11, 18, 0, 0, TimeSpan.Zero);
    private static readonly ProductCode Pos = ProductCode.Create(ProductCode.PinoyBusinessPos);

    [Fact]
    public async Task Legacy_org_with_primary_only_resolves_primary()
    {
        var h = Harness.Create();
        var primary = h.AddBusinessType("SariSari");
        var org = h.AddOrganization(primary);

        var result = await h.Resolver.ResolveAsync(org.Id);
        Assert.True(result.IsSuccess);
        Assert.Equal([primary], result.Value!.EffectiveBusinessTypeIds);
        Assert.Empty(result.Value.GrantedBusinessTypeIds);
    }

    [Fact]
    public async Task Primary_remains_included_with_additional_activation()
    {
        var h = Harness.Create();
        var primary = h.AddBusinessType("SariSari");
        var veg = h.AddBusinessType("VegetableVendor");
        var org = h.AddOrganization(primary);
        h.SetPlanGrants(org.Id, [primary, veg]);
        h.Activate(org.Id, veg);

        var result = await h.Resolver.ResolveAsync(org.Id);
        Assert.True(result.IsSuccess);
        Assert.Contains(primary, result.Value!.EffectiveBusinessTypeIds);
        Assert.Contains(veg, result.Value.EffectiveBusinessTypeIds);
    }

    [Fact]
    public async Task Granted_but_not_activated_additional_bt_is_excluded()
    {
        var h = Harness.Create();
        var primary = h.AddBusinessType("SariSari");
        var veg = h.AddBusinessType("VegetableVendor");
        var org = h.AddOrganization(primary);
        h.SetPlanGrants(org.Id, [primary, veg]);

        var result = await h.Resolver.ResolveAsync(org.Id);
        Assert.True(result.IsSuccess);
        Assert.Equal([primary], result.Value!.EffectiveBusinessTypeIds);
    }

    [Fact]
    public async Task Activated_but_no_longer_granted_bt_is_excluded()
    {
        var h = Harness.Create();
        var primary = h.AddBusinessType("SariSari");
        var veg = h.AddBusinessType("VegetableVendor");
        var org = h.AddOrganization(primary);
        h.SetPlanGrants(org.Id, [primary]);
        h.Activate(org.Id, veg);

        var result = await h.Resolver.ResolveAsync(org.Id);
        Assert.True(result.IsSuccess);
        Assert.DoesNotContain(veg, result.Value!.EffectiveBusinessTypeIds);
        Assert.Contains(veg, result.Value.ActivatedBusinessTypeIds);
    }

    [Fact]
    public async Task Inactive_additional_activation_is_excluded()
    {
        var h = Harness.Create();
        var primary = h.AddBusinessType("SariSari");
        var bakery = h.AddBusinessType("Bakery");
        var org = h.AddOrganization(primary);
        h.BusinessTypes.SetStatus(bakery, BusinessTypeStatus.Inactive);
        h.SetPlanGrants(org.Id, [primary, bakery]);
        h.Activate(org.Id, bakery);

        var result = await h.Resolver.ResolveAsync(org.Id);
        Assert.True(result.IsSuccess);
        Assert.DoesNotContain(bakery, result.Value!.EffectiveBusinessTypeIds);
    }

    [Fact]
    public async Task EnsureEntitled_rejects_pharmacy_for_sarisari_org()
    {
        var h = Harness.Create();
        var primary = h.AddBusinessType("SariSari");
        var pharmacy = h.AddBusinessType("Pharmacy");
        var org = h.AddOrganization(primary);
        h.SetPlanGrants(org.Id, [primary]);

        var denied = await h.Resolver.EnsureEntitledAsync(org.Id, pharmacy);
        Assert.False(denied.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.BusinessTypeNotEntitled, denied.ErrorCode);
    }

    [Fact]
    public async Task Activate_rejects_ungranted_type()
    {
        var h = Harness.Create();
        var primary = h.AddBusinessType("SariSari");
        var pharmacy = h.AddBusinessType("Pharmacy");
        var org = h.AddOrganization(primary);
        h.SetPlanGrants(org.Id, [primary]);

        var useCase = new ActivateOrganizationBusinessType(
            h.Organizations,
            h.Resolver,
            h.Activations,
            h.BusinessTypes,
            new NoopUnitOfWork(),
            new FixedClock(T0));

        var result = await useCase.ExecuteAsync(org.Id.Value, pharmacy.Value);
        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.BusinessTypeNotEntitled, result.ErrorCode);
    }

    [Fact]
    public async Task Gate_forged_business_type_filter_cannot_widen_access()
    {
        var allowed = Guid.NewGuid();
        var scope = new MerchantCatalogEntitlementGate.DiscoveryScope(
            Unrestricted: false,
            OrganizationId: PlatformOrganizationId.New(),
            AllowedBusinessTypeIds: [allowed],
            Entitlement: null);

        var gate = new MerchantCatalogEntitlementGate(
            null!,
            null!,
            null!,
            new EmptyBusinessTypeRepo());

        var filter = await gate.ResolveListFilterAsync(scope, Guid.NewGuid(), null);
        Assert.False(filter.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.BusinessTypeNotEntitled, filter.ErrorCode);
    }

    [Fact]
    public async Task Gate_omitted_filter_returns_full_allowed_set_not_unrestricted()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var scope = new MerchantCatalogEntitlementGate.DiscoveryScope(
            Unrestricted: false,
            OrganizationId: PlatformOrganizationId.New(),
            AllowedBusinessTypeIds: [a, b],
            Entitlement: null);

        var gate = new MerchantCatalogEntitlementGate(null!, null!, null!, new EmptyBusinessTypeRepo());
        var filter = await gate.ResolveListFilterAsync(scope, null, null);
        Assert.True(filter.IsSuccess);
        Assert.Null(filter.Value.SingleBusinessTypeId);
        Assert.Equal(2, filter.Value.AllowedBusinessTypeIds!.Count);
    }

    private sealed class EmptyBusinessTypeRepo : IBusinessTypeRepository
    {
        public Task AddAsync(BusinessType businessType, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<bool> ExistsWithCodeAsync(string code, BusinessTypeId? excludingId = null, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<bool> ExistsWithNameAsync(string name, BusinessTypeId? excludingId = null, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<BusinessType?> FindByNormalizedNameAsync(string normalizedName, CancellationToken cancellationToken = default) => Task.FromResult<BusinessType?>(null);
        public Task<BusinessType?> GetByCodeAsync(string code, CancellationToken cancellationToken = default) => Task.FromResult<BusinessType?>(null);
        public Task<BusinessType?> GetByIdAsync(BusinessTypeId id, CancellationToken cancellationToken = default) => Task.FromResult<BusinessType?>(null);
        public Task<IReadOnlyList<BusinessType>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<BusinessType>>([]);
        public Task<bool> IsReferencedAsync(BusinessTypeId id, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<(IReadOnlyList<BusinessType> Items, int TotalCount)> ListAsync(BusinessTypeStatus? status, string? search, int skip, int take, CancellationToken cancellationToken = default, BusinessTypeListSortBy sortBy = BusinessTypeListSortBy.SortOrder, bool sortDescending = false) => Task.FromResult<(IReadOnlyList<BusinessType>, int)>(([], 0));
        public Task UpdateAsync(BusinessType businessType, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FixedClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow => utcNow;
    }

    private sealed class NoopUnitOfWork : IPlatformUnitOfWork
    {
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class Harness
    {
        public required OrganizationBusinessTypeEntitlementResolver Resolver { get; init; }
        public required OrgRepo Organizations { get; init; }
        public required ActivationRepo Activations { get; init; }
        public required BtRepo BusinessTypes { get; init; }
        private SubRepo Subscriptions { get; init; } = null!;
        private PlanRepo Plans { get; init; } = null!;
        private readonly Dictionary<Guid, PlanVersionId> _versionByOrg = new();

        public static Harness Create()
        {
            var orgs = new OrgRepo();
            var subs = new SubRepo();
            var plans = new PlanRepo();
            var activations = new ActivationRepo();
            var businessTypes = new BtRepo();
            return new Harness
            {
                Organizations = orgs,
                Activations = activations,
                BusinessTypes = businessTypes,
                Subscriptions = subs,
                Plans = plans,
                Resolver = new OrganizationBusinessTypeEntitlementResolver(orgs, subs, plans, activations, businessTypes)
            };
        }

        public BusinessTypeId AddBusinessType(string code)
        {
            var bt = BusinessType.Create(code, code, T0);
            BusinessTypes.Store(bt);
            return bt.Id;
        }

        public PlatformOrganization AddOrganization(BusinessTypeId primary)
        {
            var org = PlatformOrganization.Create("Org", $"o{Guid.NewGuid():N}"[..12], T0);
            org.AssignPrimaryBusinessType(primary, T0);
            Organizations.Store(org);

            var plan = Plan.CreateDraft(Pos, PlanCode.Create($"p{Guid.NewGuid():N}"[..8]), "Plan", T0);
            plan.Activate(T0);
            var version = PlanVersion.CreateDraft(plan, 1, T0, BillingPeriod.Monthly, true, [], T0);
            version.Publish(T0);
            Plans.Store(plan, version);

            var trial = TrialDefinition.Create(
                Pos,
                "Trial",
                TimeSpan.FromDays(14),
                Array.Empty<FeatureGrantSpec>(),
                Array.Empty<FeatureGrantSpec>(),
                T0);
            var sub = Subscription.StartTrial(org.Id, plan, version, trial, T0);
            Subscriptions.Store(sub);
            _versionByOrg[org.Id.Value] = version.Id;
            return org;
        }

        public void SetPlanGrants(PlatformOrganizationId orgId, IReadOnlyList<BusinessTypeId> grants)
        {
            Plans.ReplaceBusinessTypeGrants(_versionByOrg[orgId.Value], grants);
        }

        public void Activate(PlatformOrganizationId orgId, BusinessTypeId btId)
        {
            var org = Organizations.GetByIdAsync(orgId).GetAwaiter().GetResult()!;
            Activations.AddAsync(
                    OrganizationBusinessTypeActivation.Activate(orgId, btId, T0, org.PrimaryBusinessTypeId))
                .GetAwaiter().GetResult();
        }
    }

    private sealed class OrgRepo : IPlatformOrganizationRepository
    {
        private readonly Dictionary<Guid, PlatformOrganization> _items = new();
        public void Store(PlatformOrganization org) => _items[org.Id.Value] = org;
        public Task AddAsync(PlatformOrganization organization, CancellationToken cancellationToken = default) { Store(organization); return Task.CompletedTask; }
        public Task<PlatformOrganization?> GetByIdAsync(PlatformOrganizationId id, CancellationToken cancellationToken = default) => Task.FromResult(_items.GetValueOrDefault(id.Value));
        public Task<PlatformOrganization?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default) => Task.FromResult(_items.Values.FirstOrDefault(o => o.Slug == slug));
        public Task<(IReadOnlyList<PlatformOrganization> Items, int TotalCount)> ListAsync(int skip, int take, CancellationToken cancellationToken = default) => Task.FromResult<(IReadOnlyList<PlatformOrganization>, int)>((_items.Values.Skip(skip).Take(take).ToList(), _items.Count));
        public Task<(IReadOnlyList<PlatformOrganization> Items, int TotalCount)> ListAsync(OrganizationStatus? status, string? search, OrganizationListSortBy sortBy, bool sortDescending, int skip, int take, CancellationToken cancellationToken = default) => ListAsync(skip, take, cancellationToken);
        public Task UpdateAsync(PlatformOrganization organization, CancellationToken cancellationToken = default) { Store(organization); return Task.CompletedTask; }
    }

    private sealed class ActivationRepo : IOrganizationBusinessTypeActivationRepository
    {
        private readonly List<OrganizationBusinessTypeActivation> _items = [];
        public Task AddAsync(OrganizationBusinessTypeActivation activation, CancellationToken cancellationToken = default) { _items.Add(activation); return Task.CompletedTask; }
        public Task<OrganizationBusinessTypeActivation?> GetAsync(PlatformOrganizationId organizationId, BusinessTypeId businessTypeId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(a => a.OrganizationId == organizationId && a.BusinessTypeId == businessTypeId));
        public Task<IReadOnlyList<OrganizationBusinessTypeActivation>> ListByOrganizationAsync(PlatformOrganizationId organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<OrganizationBusinessTypeActivation>>(_items.Where(a => a.OrganizationId == organizationId).ToList());
        public Task RemoveAsync(PlatformOrganizationId organizationId, BusinessTypeId businessTypeId, CancellationToken cancellationToken = default)
        {
            _items.RemoveAll(a => a.OrganizationId == organizationId && a.BusinessTypeId == businessTypeId);
            return Task.CompletedTask;
        }
    }

    private sealed class BtRepo : IBusinessTypeRepository
    {
        private readonly Dictionary<Guid, BusinessType> _items = new();
        public void Store(BusinessType bt) => _items[bt.Id.Value] = bt;
        public void SetStatus(BusinessTypeId id, BusinessTypeStatus status)
        {
            var bt = _items[id.Value];
            bt.SetStatus(status, T0.AddMinutes(1));
            _items[id.Value] = bt;
        }
        public Task AddAsync(BusinessType businessType, CancellationToken cancellationToken = default) { Store(businessType); return Task.CompletedTask; }
        public Task<bool> ExistsWithCodeAsync(string code, BusinessTypeId? excludingId = null, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<bool> ExistsWithNameAsync(string name, BusinessTypeId? excludingId = null, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<BusinessType?> FindByNormalizedNameAsync(string normalizedName, CancellationToken cancellationToken = default) => Task.FromResult<BusinessType?>(null);
        public Task<BusinessType?> GetByCodeAsync(string code, CancellationToken cancellationToken = default) => Task.FromResult(_items.Values.FirstOrDefault(b => b.Code.Equals(code, StringComparison.OrdinalIgnoreCase)));
        public Task<BusinessType?> GetByIdAsync(BusinessTypeId id, CancellationToken cancellationToken = default) => Task.FromResult(_items.GetValueOrDefault(id.Value));
        public Task<IReadOnlyList<BusinessType>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<BusinessType>>(_items.Values.Where(b => ids.Contains(b.Id.Value)).ToList());
        public Task<bool> IsReferencedAsync(BusinessTypeId id, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<(IReadOnlyList<BusinessType> Items, int TotalCount)> ListAsync(BusinessTypeStatus? status, string? search, int skip, int take, CancellationToken cancellationToken = default, BusinessTypeListSortBy sortBy = BusinessTypeListSortBy.SortOrder, bool sortDescending = false) => Task.FromResult<(IReadOnlyList<BusinessType>, int)>((_items.Values.ToList(), _items.Count));
        public Task UpdateAsync(BusinessType businessType, CancellationToken cancellationToken = default) { Store(businessType); return Task.CompletedTask; }
    }

    private sealed class SubRepo : ISubscriptionRepository
    {
        private readonly List<Subscription> _items = [];
        public void Store(Subscription sub) => _items.Add(sub);
        public Task AddAsync(Subscription subscription, CancellationToken cancellationToken = default) { Store(subscription); return Task.CompletedTask; }
        public Task<bool> ExistsActiveLikeAsync(PlatformOrganizationId organizationId, ProductCode productCode, CancellationToken cancellationToken = default) => Task.FromResult(true);
        public Task<Subscription?> GetByIdAsync(SubscriptionId id, CancellationToken cancellationToken = default) => Task.FromResult(_items.FirstOrDefault(s => s.Id == id));
        public Task<Subscription?> GetCurrentForOrganizationProductAsync(PlatformOrganizationId organizationId, ProductCode productCode, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(s => s.OrganizationId == organizationId && s.ProductCode == productCode && Subscription.IsActiveLike(s.Status)));
        public Task<bool> HasConsumedTrialAsync(PlatformOrganizationId organizationId, ProductCode productCode, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<(IReadOnlyList<Subscription> Items, int TotalCount)> ListAsync(PlatformOrganizationId? organizationId, ProductCode? productCode, SubscriptionStatus? status, string? search, bool? isTrial, Guid? planId, SubscriptionListSortBy sortBy, bool sortDescending, int skip, int take, CancellationToken cancellationToken = default) => Task.FromResult<(IReadOnlyList<Subscription>, int)>((_items, _items.Count));
        public Task<(IReadOnlyList<Subscription> Items, int TotalCount)> ListByOrganizationAsync(PlatformOrganizationId organizationId, SubscriptionStatus? status, int skip, int take, CancellationToken cancellationToken = default) => Task.FromResult<(IReadOnlyList<Subscription>, int)>((_items, _items.Count));
        public Task<(IReadOnlyList<Subscription> Items, int TotalCount)> ListByProductAsync(ProductCode productCode, SubscriptionStatus? status, int skip, int take, CancellationToken cancellationToken = default) => Task.FromResult<(IReadOnlyList<Subscription>, int)>((_items, _items.Count));
        public Task<(IReadOnlyList<Subscription> Items, int TotalCount)> ListByStatusAsync(SubscriptionStatus status, int skip, int take, CancellationToken cancellationToken = default) => Task.FromResult<(IReadOnlyList<Subscription>, int)>((_items, _items.Count));
        public Task<IReadOnlyList<Subscription>> ListDuePendingPlanChangesAsync(DateTimeOffset asOfUtc, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Subscription>>([]);
        public Task<(IReadOnlyList<Subscription> Items, int TotalCount)> ListExpiringTrialsAsync(DateTimeOffset asOfUtc, DateTimeOffset throughUtc, int skip, int take, CancellationToken cancellationToken = default) => Task.FromResult<(IReadOnlyList<Subscription>, int)>(([], 0));
        public Task UpdateAsync(Subscription subscription, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class PlanRepo : IPlanRepository
    {
        private readonly Dictionary<Guid, Plan> _plans = new();
        private readonly Dictionary<Guid, PlanVersion> _versions = new();
        public void Store(Plan plan, PlanVersion version)
        {
            _plans[plan.Id.Value] = plan;
            _versions[version.Id.Value] = version;
        }

        public void ReplaceBusinessTypeGrants(PlanVersionId id, IReadOnlyList<BusinessTypeId> grants)
        {
            var existing = _versions[id.Value];
            var plan = _plans[existing.PlanId.Value];
            var draft = PlanVersion.CreateDraft(
                plan,
                existing.VersionNumber,
                T0,
                existing.BillingPeriod,
                existing.TrialEligible,
                existing.Grants.ToList(),
                T0,
                id: id,
                businessTypeGrants: grants);
            draft.Publish(T0);
            _versions[id.Value] = draft;
        }

        public Task AddAsync(Plan plan, CancellationToken cancellationToken = default) { _plans[plan.Id.Value] = plan; return Task.CompletedTask; }
        public Task AddVersionAsync(PlanVersion version, CancellationToken cancellationToken = default) { _versions[version.Id.Value] = version; return Task.CompletedTask; }
        public Task<Plan?> GetByIdAsync(PlanId id, CancellationToken cancellationToken = default) => Task.FromResult(_plans.GetValueOrDefault(id.Value));
        public Task<Plan?> GetByProductAndCodeAsync(ProductCode productCode, PlanCode planCode, CancellationToken cancellationToken = default) => Task.FromResult<Plan?>(null);
        public Task<int> GetMaxVersionNumberAsync(PlanId planId, CancellationToken cancellationToken = default) => Task.FromResult(1);
        public Task<PlanVersion?> GetLatestPublishedVersionAsync(PlanId planId, CancellationToken cancellationToken = default) => Task.FromResult(_versions.Values.FirstOrDefault(v => v.PlanId == planId));
        public Task<PlanVersion?> GetVersionByIdAsync(PlanVersionId id, CancellationToken cancellationToken = default) => Task.FromResult(_versions.GetValueOrDefault(id.Value));
        public Task<PlanVersion?> GetVersionByPlanAndNumberAsync(PlanId planId, int versionNumber, CancellationToken cancellationToken = default) => Task.FromResult<PlanVersion?>(null);
        public Task<(IReadOnlyList<Plan> Items, int TotalCount)> ListAsync(ProductCode? productCode, PlanStatus? status, string? search, CatalogListSortBy sortBy, bool sortDescending, int skip, int take, CancellationToken cancellationToken = default) => Task.FromResult<(IReadOnlyList<Plan>, int)>((_plans.Values.ToList(), _plans.Count));
        public Task<IReadOnlyList<Plan>> ListByProductAsync(ProductCode productCode, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Plan>>(_plans.Values.ToList());
        public Task<IReadOnlyList<PlanVersion>> ListVersionsAsync(PlanId planId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<PlanVersion>>(_versions.Values.Where(v => v.PlanId == planId).ToList());
        public Task UpdateAsync(Plan plan, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task UpdateVersionAsync(PlanVersion version, CancellationToken cancellationToken = default) { _versions[version.Id.Value] = version; return Task.CompletedTask; }
    }
}
