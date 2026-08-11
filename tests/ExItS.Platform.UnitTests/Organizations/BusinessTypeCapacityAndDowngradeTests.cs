using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.GlobalCatalog;
using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Application.Subscriptions;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Catalog;
using ExItS.Platform.Domain.GlobalCatalog;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Products;
using ExItS.Platform.Domain.Subscriptions;

namespace ExItS.Platform.UnitTests.Organizations;

public sealed class BusinessTypeCapacityAndDowngradeTests
{
    private static readonly DateTimeOffset T0 = new(2026, 8, 11, 22, 0, 0, TimeSpan.Zero);
    private static readonly ProductCode Pos = ProductCode.Create(ProductCode.PinoyBusinessPos);

    [Fact]
    public async Task Activate_blocks_when_effective_count_meets_plan_capacity()
    {
        var h = Harness.Create(maxActiveBusinessTypes: 1);
        var primary = h.AddBusinessType("SariSari");
        var bakery = h.AddBusinessType("Bakery");
        var org = h.AddOrganization(primary);
        h.SetPlanGrants(org.Id, [primary, bakery]);

        var result = await h.Activate.ExecuteAsync(org.Id.Value, bakery.Value, Pos.Value);
        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.BusinessTypeActivationCapacityExceeded, result.ErrorCode);
    }

    [Fact]
    public async Task Activate_is_idempotent_when_type_already_effective()
    {
        var h = Harness.Create(maxActiveBusinessTypes: 1);
        var primary = h.AddBusinessType("SariSari");
        var org = h.AddOrganization(primary);
        h.SetPlanGrants(org.Id, [primary]);

        var first = await h.Activate.ExecuteAsync(org.Id.Value, primary.Value, Pos.Value);
        var second = await h.Activate.ExecuteAsync(org.Id.Value, primary.Value, Pos.Value);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.Empty(await h.Activations.ListByOrganizationAsync(org.Id));
    }

    [Fact]
    public void Plan_change_impact_blocks_when_effective_business_types_exceed_target()
    {
        var current = Plan.CreateDraft(Pos, PlanCode.Create("pro-cap"), "Pro", T0, maxActiveBusinessTypes: 6);
        var target = Plan.CreateDraft(Pos, PlanCode.Create("starter-cap"), "Starter", T0, maxActiveBusinessTypes: 1);

        var preview = PlanChangeImpact.Evaluate(
            current,
            target,
            activeStaffCount: 1,
            activeBranchCount: 1,
            branchCountAvailable: true,
            activeBusinessTypeCount: 3);

        Assert.True(preview.HasBlockingUsageConflicts);
        Assert.Contains(preview.UsageConflicts, c => c.Resource == "ActiveBusinessTypes");
    }

    [Fact]
    public async Task Downgrade_fails_when_effective_business_types_exceed_target_capacity()
    {
        var h = Harness.Create(maxActiveBusinessTypes: 3);
        var primary = h.AddBusinessType("SariSari");
        var bakery = h.AddBusinessType("Bakery");
        var pharmacy = h.AddBusinessType("Pharmacy");
        var org = h.AddOrganization(primary);
        h.SetPlanGrants(org.Id, [primary, bakery, pharmacy]);
        h.ActivateRow(org.Id, bakery);
        h.ActivateRow(org.Id, pharmacy);

        var starter = Plan.CreateDraft(
            Pos,
            PlanCode.Create(MvpPosPlanCodes.Starter),
            "Starter",
            T0,
            maxActiveBusinessTypes: 1);
        starter.Activate(T0);
        await h.Plans.AddAsync(starter);

        var result = await h.Downgrade.ExecuteAsync(org.Id, Pos, starter.Id, T0.AddMonths(1));
        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.PlanDowngradeBlockedByBusinessTypeCapacity, result.ErrorCode);
    }

    private sealed class Harness
    {
        private readonly Dictionary<Guid, PlanVersionId> _versionByOrg = new();
        private readonly FixedClock _clock = new(T0);

        public required OrgRepo Organizations { get; init; }
        public required ActivationRepo Activations { get; init; }
        public required BtRepo BusinessTypes { get; init; }
        public required PlanRepo Plans { get; init; }
        public required SubRepo Subscriptions { get; init; }
        public required ActivateOrganizationBusinessType Activate { get; init; }
        public required ScheduleOrganizationSubscriptionDowngrade Downgrade { get; init; }
        public int MaxActiveBusinessTypes { get; private init; }

        public static Harness Create(int maxActiveBusinessTypes = 1)
        {
            var orgs = new OrgRepo();
            var subs = new SubRepo();
            var plans = new PlanRepo();
            var activations = new ActivationRepo();
            var businessTypes = new BtRepo();
            var clock = new FixedClock(T0);
            var resolver = new OrganizationBusinessTypeEntitlementResolver(orgs, subs, plans, activations, businessTypes);
            return new Harness
            {
                Organizations = orgs,
                Activations = activations,
                BusinessTypes = businessTypes,
                Plans = plans,
                Subscriptions = subs,
                MaxActiveBusinessTypes = maxActiveBusinessTypes,
                Activate = new ActivateOrganizationBusinessType(
                    orgs, resolver, activations, businessTypes, plans, new NoOpUnitOfWork(), clock),
                Downgrade = new ScheduleOrganizationSubscriptionDowngrade(
                    subs, plans, resolver, new NoOpUnitOfWork(), clock)
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

            var plan = Plan.CreateDraft(
                Pos,
                PlanCode.Create($"p{Guid.NewGuid():N}"[..8]),
                "Plan",
                T0,
                maxActiveBusinessTypes: MaxActiveBusinessTypes);
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

        public void SetPlanGrants(PlatformOrganizationId orgId, IReadOnlyList<BusinessTypeId> grants) =>
            Plans.ReplaceBusinessTypeGrants(_versionByOrg[orgId.Value], grants);

        public void ActivateRow(PlatformOrganizationId orgId, BusinessTypeId btId)
        {
            var org = Organizations.GetByIdAsync(orgId).GetAwaiter().GetResult()!;
            Activations.AddAsync(
                    OrganizationBusinessTypeActivation.Activate(orgId, btId, T0, org.PrimaryBusinessTypeId))
                .GetAwaiter().GetResult();
        }
    }

    private sealed class FixedClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
    }

    private sealed class NoOpUnitOfWork : IPlatformUnitOfWork
    {
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
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
        public Task<Plan?> GetByProductAndCodeAsync(ProductCode productCode, PlanCode planCode, CancellationToken cancellationToken = default) => Task.FromResult(_plans.Values.FirstOrDefault(p => p.ProductCode == productCode && p.Code == planCode));
        public Task<int> GetMaxVersionNumberAsync(PlanId planId, CancellationToken cancellationToken = default) => Task.FromResult(_versions.Values.Where(v => v.PlanId == planId).Select(v => v.VersionNumber).DefaultIfEmpty(0).Max());
        public Task<PlanVersion?> GetLatestPublishedVersionAsync(PlanId planId, CancellationToken cancellationToken = default) => Task.FromResult(_versions.Values.Where(v => v.PlanId == planId && v.Status == PlanVersionStatus.Published).OrderByDescending(v => v.VersionNumber).FirstOrDefault());
        public Task<PlanVersion?> GetVersionByIdAsync(PlanVersionId id, CancellationToken cancellationToken = default) => Task.FromResult(_versions.GetValueOrDefault(id.Value));
        public Task<PlanVersion?> GetVersionByPlanAndNumberAsync(PlanId planId, int versionNumber, CancellationToken cancellationToken = default) => Task.FromResult(_versions.Values.FirstOrDefault(v => v.PlanId == planId && v.VersionNumber == versionNumber));
        public Task<(IReadOnlyList<Plan> Items, int TotalCount)> ListAsync(ProductCode? productCode, PlanStatus? status, string? search, CatalogListSortBy sortBy, bool sortDescending, int skip, int take, CancellationToken cancellationToken = default) => Task.FromResult<(IReadOnlyList<Plan>, int)>((_plans.Values.ToList(), _plans.Count));
        public Task<IReadOnlyList<Plan>> ListByProductAsync(ProductCode productCode, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Plan>>(_plans.Values.ToList());
        public Task<IReadOnlyList<PlanVersion>> ListVersionsAsync(PlanId planId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<PlanVersion>>(_versions.Values.Where(v => v.PlanId == planId).ToList());
        public Task UpdateAsync(Plan plan, CancellationToken cancellationToken = default) { _plans[plan.Id.Value] = plan; return Task.CompletedTask; }
        public Task UpdateVersionAsync(PlanVersion version, CancellationToken cancellationToken = default) { _versions[version.Id.Value] = version; return Task.CompletedTask; }
    }
}
