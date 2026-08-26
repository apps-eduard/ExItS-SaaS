using System.Reflection;
using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Entitlements;
using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Application.Subscriptions;
using ExItS.Platform.Domain.Catalog;
using ExItS.Platform.Domain.Entitlements;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Products;
using ExItS.Platform.Domain.Subscriptions;
using ExItS.Platform.UnitTests.Support;

using ExItS.Platform.UnitTests.TestSupport;
namespace ExItS.Platform.UnitTests.Subscriptions;

public sealed class SubscriptionDisplayAndFilterTests
{
    private static readonly DateTimeOffset T0 = new(2026, 7, 29, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task SubscriptionQueryService_enriches_display_names_not_product_keys()
    {
        var harness = await QueryHarness.CreateSingleSubscriptionAsync();

        var dto = await harness.Queries.GetByIdAsync(harness.Subscription.Id.Value);
        Assert.NotNull(dto);
        Assert.Equal("Acme Retail Group", dto!.OrganizationDisplayName);
        Assert.Equal("Pinoy Business POS", dto.ProductDisplayName);
        Assert.Equal("Growth", dto.PlanDisplayName);
        Assert.Equal(MvpPosPlanCodes.Growth, dto.PlanKey);

        Assert.NotEqual(dto.ProductCode, dto.ProductDisplayName);
        Assert.NotEqual(dto.PlanKey, dto.PlanDisplayName);
        Assert.False(string.IsNullOrWhiteSpace(dto.OrganizationDisplayName));
    }

    [Fact]
    public async Task SubscriptionQueryService_list_filters_by_product_plan_and_status()
    {
        var harness = await QueryHarness.CreateMultiSubscriptionAsync();

        var trialingOnly = await harness.Queries.ListAsync(
            organizationId: null,
            productCode: ProductCode.PinoyBusinessPos,
            status: SubscriptionStatus.Trialing,
            search: null,
            isTrial: null,
            planId: null,
            SubscriptionListSortBy.CreatedAtUtc,
            sortDescending: false,
            page: 1,
            pageSize: 20);
        Assert.Equal(2, trialingOnly.TotalCount);
        Assert.All(trialingOnly.Items, i => Assert.Equal(SubscriptionStatus.Trialing.ToString(), i.Status));

        var businessPlanOnly = await harness.Queries.ListAsync(
            organizationId: null,
            productCode: null,
            status: null,
            search: null,
            isTrial: null,
            planId: harness.BusinessPlan.Id.Value,
            SubscriptionListSortBy.CreatedAtUtc,
            sortDescending: false,
            page: 1,
            pageSize: 20);
        Assert.Equal(2, businessPlanOnly.TotalCount);
        Assert.All(businessPlanOnly.Items, i => Assert.Equal(MvpPosPlanCodes.Growth, i.PlanKey));

        var orgScoped = await harness.Queries.ListAsync(
            organizationId: harness.OrgA.Id.Value,
            productCode: null,
            status: null,
            search: null,
            isTrial: null,
            planId: null,
            SubscriptionListSortBy.CreatedAtUtc,
            sortDescending: false,
            page: 1,
            pageSize: 20);
        Assert.Equal(2, orgScoped.TotalCount);
        Assert.All(orgScoped.Items, i => Assert.Equal(harness.OrgA.Id.Value, i.OrganizationId));
        Assert.DoesNotContain(orgScoped.Items, i => i.OrganizationId == harness.OrgB.Id.Value);
    }

    [Fact]
    public async Task SubscriptionQueryService_sorts_before_pagination()
    {
        var harness = await QueryHarness.CreateMultiSubscriptionAsync();

        var firstPage = await harness.Queries.ListAsync(
            organizationId: null,
            productCode: ProductCode.PinoyBusinessPos,
            status: null,
            search: null,
            isTrial: null,
            planId: null,
            SubscriptionListSortBy.CreatedAtUtc,
            sortDescending: false,
            page: 1,
            pageSize: 1);

        Assert.Equal(3, firstPage.TotalCount);
        Assert.Single(firstPage.Items);
        Assert.Equal(harness.EarliestSubscriptionId, firstPage.Items[0].Id);
    }

    [Fact]
    public async Task Trialing_subscription_enables_entitlement_snapshot()
    {
        var harness = await QueryHarness.CreateSingleSubscriptionAsync();
        var refreshPolicy = new ProvisionalEntitlementRefreshPolicy();
        var snapshots = new InMemoryEntitlementSnapshotRepository();

        var snapshot = await new GenerateEntitlementSnapshot(
                harness.Subscriptions,
                harness.Plans,
                harness.Trials,
                new InMemoryFeatureOverrideRepository(),
                snapshots,
                refreshPolicy,
                harness.UnitOfWork,
                harness.Clock)
            .ExecuteAsync(harness.Subscription.Id, expectedNextVersion: 1);

        Assert.True(snapshot.IsSuccess);
        Assert.Equal(SubscriptionStatus.Trialing, snapshot.Value!.SubscriptionStatus);
        Assert.Contains(snapshot.Value.Grants, g => g.Enabled);
    }

    [Fact]
    public async Task Expired_subscription_blocks_create_entitlement_without_removing_view_grants()
    {
        var harness = await QueryHarness.CreateSingleSubscriptionAsync();
        harness.Subscription.Expire(T0.AddDays(15));
        await harness.Subscriptions.UpdateAsync(harness.Subscription);

        var plan = (await harness.Plans.GetByIdAsync(harness.BusinessPlan.Id))!;
        var version = (await harness.Plans.GetLatestPublishedVersionAsync(plan.Id))!;
        var trial = (await harness.Trials.GetByIdAsync(harness.Trial.Id))!;

        var snapshot = new EntitlementSnapshotComposer().Compose(
            harness.Subscription,
            plan,
            version,
            trial,
            Array.Empty<FeatureOverride>(),
            nextSnapshotVersion: 1,
            utcNow: T0.AddDays(16));

        Assert.Equal(SubscriptionStatus.Expired, snapshot.SubscriptionStatus);
        Assert.Contains(snapshot.Grants, g => g.FeatureCode.Value == FeatureCode.CustomerCreditView && g.Enabled);
        Assert.Contains(snapshot.Grants, g => g.FeatureCode.Value == FeatureCode.CustomerCreditCreate && !g.Enabled);
    }

    [Fact]
    public void StartTrialSubscription_does_not_assign_product_local_roles()
    {
        var ctor = typeof(StartTrialSubscription).GetConstructors(BindingFlags.Public | BindingFlags.Instance).Single();
        var parameterTypes = ctor.GetParameters().Select(p => p.ParameterType).ToArray();

        Assert.DoesNotContain(parameterTypes, t => t.Name.Contains("ProductLocalRole", StringComparison.Ordinal));
        Assert.DoesNotContain(parameterTypes, t => t.Name.Contains("RoleGrant", StringComparison.Ordinal));
    }

    [Fact]
    public void ActivatePaidSubscription_does_not_assign_product_local_roles()
    {
        var ctor = typeof(ActivatePaidSubscription).GetConstructors(BindingFlags.Public | BindingFlags.Instance).Single();
        var parameterTypes = ctor.GetParameters().Select(p => p.ParameterType).ToArray();

        Assert.DoesNotContain(parameterTypes, t => t.Name.Contains("ProductLocalRole", StringComparison.Ordinal));
        Assert.DoesNotContain(parameterTypes, t => t.Name.Contains("RoleGrant", StringComparison.Ordinal));
    }

    private sealed class QueryHarness
    {
        public FixedClock Clock { get; private init; } = null!;
        public NoOpUnitOfWork UnitOfWork { get; private init; } = null!;
        public InMemoryPlatformOrganizationRepository Organizations { get; private init; } = null!;
        public InMemoryProductRepository Products { get; private init; } = null!;
        public InMemoryPlanRepository Plans { get; private init; } = null!;
        public InMemoryTrialDefinitionRepository Trials { get; private init; } = null!;
        public InMemorySubscriptionRepository Subscriptions { get; private init; } = null!;
        public SubscriptionQueryService Queries { get; private init; } = null!;
        public PlatformOrganization OrgA { get; private init; } = null!;
        public PlatformOrganization OrgB { get; private init; } = null!;
        public Plan BusinessPlan { get; private init; } = null!;
        public Plan StarterPlan { get; private init; } = null!;
        public TrialDefinition Trial { get; private init; } = null!;
        public Subscription Subscription { get; set; } = null!;
        public Guid EarliestSubscriptionId { get; set; }

        public static async Task<QueryHarness> CreateSingleSubscriptionAsync()
        {
            var harness = await CreateCatalogAsync();
            var version = await PublishBusinessVersionAsync(harness);

            var subscription = Subscription.StartTrial(
                harness.OrgA.Id,
                harness.BusinessPlan,
                version,
                harness.Trial,
                harness.Clock.UtcNow);
            await harness.Subscriptions.AddAsync(subscription);

            harness.Subscription = subscription;
            harness.EarliestSubscriptionId = subscription.Id.Value;
            return harness;
        }

        public static async Task<QueryHarness> CreateMultiSubscriptionAsync()
        {
            var harness = await CreateCatalogAsync();
            var businessVersion = await PublishBusinessVersionAsync(harness);
            var starterVersion = await PublishStarterVersionAsync(harness);

            harness.Clock.UtcNow = T0;
            var earliest = Subscription.StartTrial(
                harness.OrgA.Id,
                harness.BusinessPlan,
                businessVersion,
                harness.Trial,
                harness.Clock.UtcNow);
            await harness.Subscriptions.AddAsync(earliest);

            harness.Clock.UtcNow = T0.AddHours(1);
            var middle = Subscription.StartTrial(
                harness.OrgB.Id,
                harness.BusinessPlan,
                businessVersion,
                harness.Trial,
                harness.Clock.UtcNow);
            await harness.Subscriptions.AddAsync(middle);

            harness.Clock.UtcNow = T0.AddHours(2);
            var latest = Subscription.StartTrial(
                harness.OrgA.Id,
                harness.StarterPlan,
                starterVersion,
                harness.Trial,
                harness.Clock.UtcNow);
            latest.ActivateFromTrial(T0.AddHours(2), T0.AddDays(30), T0.AddHours(3));
            await harness.Subscriptions.AddAsync(latest);

            harness.Subscription = earliest;
            harness.EarliestSubscriptionId = earliest.Id.Value;
            return harness;
        }

        private static async Task<QueryHarness> CreateCatalogAsync()
        {
            var clock = new FixedClock(T0);
            var uow = new NoOpUnitOfWork();
            var orgs = new InMemoryPlatformOrganizationRepository();
            var products = new InMemoryProductRepository();
            var plans = new InMemoryPlanRepository();
            var trials = new InMemoryTrialDefinitionRepository();
            var subscriptions = new InMemorySubscriptionRepository();

            var orgA = (await new CreatePlatformOrganization(orgs, new FakePublicOrganizationIdGenerator(), uow, clock)
                .ExecuteAsync("Acme Retail Group", "acme-retail")).Value!;
            var orgB = (await new CreatePlatformOrganization(orgs, new FakePublicOrganizationIdGenerator(), uow, clock)
                .ExecuteAsync("Beta Shop", "beta-shop")).Value!;

            await products.AddAsync(Product.Create(
                ProductCode.Create(ProductCode.PinoyBusinessPos),
                "Pinoy Business POS",
                T0));

            var businessPlan = Plan.CreateDraft(
                ProductCode.Create(ProductCode.PinoyBusinessPos),
                PlanCode.Create(MvpPosPlanCodes.Growth),
                "Growth",
                T0);
            businessPlan.Activate(T0);
            await plans.AddAsync(businessPlan);

            var starterPlan = Plan.CreateDraft(
                ProductCode.Create(ProductCode.PinoyBusinessPos),
                PlanCode.Create(MvpPosPlanCodes.Starter),
                "Starter",
                T0);
            starterPlan.Activate(T0);
            await plans.AddAsync(starterPlan);

            var trial = UtangTrialTestFactory.CreateConfigured(T0, TimeSpan.FromDays(14), planId: null);
            await trials.AddAsync(trial);

            var queries = new SubscriptionQueryService(subscriptions, orgs, products, plans);

            return new QueryHarness
            {
                Clock = clock,
                UnitOfWork = uow,
                Organizations = orgs,
                Products = products,
                Plans = plans,
                Trials = trials,
                Subscriptions = subscriptions,
                Queries = queries,
                OrgA = orgA,
                OrgB = orgB,
                BusinessPlan = businessPlan,
                StarterPlan = starterPlan,
                Trial = trial,
                Subscription = null!,
                EarliestSubscriptionId = Guid.Empty
            };
        }

        private static async Task<PlanVersion> PublishBusinessVersionAsync(QueryHarness harness)
        {
            var grants = new[]
            {
                FeatureGrantSpec.Boolean(FeatureCode.Create(FeatureCode.CustomerCreditView), true),
                FeatureGrantSpec.Boolean(FeatureCode.Create(FeatureCode.CustomerCreditCreate), true)
            };
            var version = PlanVersion.CreateDraft(
                harness.BusinessPlan,
                1,
                T0,
                BillingPeriod.Monthly,
                trialEligible: true,
                grants,
                T0);
            version.Publish(T0);
            await harness.Plans.AddVersionAsync(version);
            return version;
        }

        private static async Task<PlanVersion> PublishStarterVersionAsync(QueryHarness harness)
        {
            var grants = new[]
            {
                FeatureGrantSpec.Boolean(FeatureCode.Create(FeatureCode.CustomerCreditView), true)
            };
            var version = PlanVersion.CreateDraft(
                harness.StarterPlan,
                1,
                T0,
                BillingPeriod.Monthly,
                trialEligible: true,
                grants,
                T0);
            version.Publish(T0);
            await harness.Plans.AddVersionAsync(version);
            return version;
        }
    }
}
