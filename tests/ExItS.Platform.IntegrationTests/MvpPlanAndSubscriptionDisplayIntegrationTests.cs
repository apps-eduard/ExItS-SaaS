using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Application.Subscriptions;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Catalog;
using ExItS.Platform.Domain.Products;
using ExItS.Platform.Domain.Subscriptions;
using ExItS.Platform.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ExItS.Platform.IntegrationTests;

[Collection(PostgreSqlCollection.Name)]
public sealed class MvpPlanAndSubscriptionDisplayIntegrationTests(PostgreSqlFixture fixture)
{
    private static readonly DateTimeOffset T0 = new(2026, 7, 29, 12, 0, 0, TimeSpan.Zero);

    private static async Task SeedPosProductAsync(IServiceProvider provider)
    {
        var createProduct = provider.GetRequiredService<CreateProduct>();
        var result = await createProduct.ExecuteAsync(ProductCode.PinoyBusinessPos, "Pinoy Business POS");
        if (!result.IsSuccess && result.ErrorCode != ApplicationErrorCodes.DuplicateProductCode)
        {
            throw new InvalidOperationException($"POS product seed failed: {result.ErrorCode} {result.ErrorMessage}");
        }
    }

    private static async Task<TrialDefinitionId> SeedPosTrialAsync(IServiceProvider provider)
    {
        var createFeature = provider.GetRequiredService<CreateFeatureDefinition>();
        var createTrial = provider.GetRequiredService<CreateTrialDefinition>();
        var trials = provider.GetRequiredService<ITrialDefinitionRepository>();

        var productCode = ProductCode.PinoyBusinessPos;
        await createFeature.ExecuteAsync(
            productCode,
            FeatureCode.CustomerCreditView,
            "View Credit",
            FeatureValueType.Boolean);

        var existing = await trials.ListByProductAsync(ProductCode.Create(productCode));
        if (existing.Count > 0)
        {
            return existing[0].Id;
        }

        var grants = new[] { FeatureGrantSpec.Boolean(FeatureCode.Create(FeatureCode.CustomerCreditView), true) };
        var created = await createTrial.ExecuteAsync(
            productCode,
            "MVP Trial",
            TimeSpan.FromDays(14),
            grants,
            Array.Empty<FeatureGrantSpec>());
        if (!created.IsSuccess || created.Value is null)
        {
            throw new InvalidOperationException($"Trial seed failed: {created.ErrorCode} {created.ErrorMessage}");
        }

        return created.Value.Id;
    }

    [Fact]
    public async Task EnsureMvpPosPlans_seeds_starter_business_pro_idempotently()
    {
        await using var provider = BuildMvpPlanServices(fixture.ConnectionString);
        await SeedPosProductAsync(provider);
        var ensure = provider.GetRequiredService<EnsureMvpPosPlans>();
        var plans = provider.GetRequiredService<IPlanRepository>();

        await ensure.ExecuteAsync();
        await ensure.ExecuteAsync();

        var productCode = ProductCode.Create(ProductCode.PinoyBusinessPos);
        var seeded = await plans.ListByProductAsync(productCode);
        var mvpPlans = seeded
            .Where(p => MvpPosPlanCodes.All.Contains(p.PlanKey, StringComparer.Ordinal))
            .ToList();

        Assert.Equal(3, mvpPlans.Count);
        Assert.Equal(MvpPosPlanCodes.All.OrderBy(x => x), mvpPlans.Select(p => p.PlanKey).OrderBy(x => x));
        Assert.All(mvpPlans, p => Assert.Equal(PlanStatus.Active, p.Status));

        foreach (var expected in MvpPosPlanCatalog.Plans)
        {
            var plan = mvpPlans.Single(p => p.PlanKey == expected.PlanKey);
            Assert.Equal(expected.DisplayName, plan.DisplayName);
            Assert.Equal(expected.MaxBranches, plan.MaxBranches);
            Assert.Equal(expected.MaxActiveStaff, plan.MaxActiveStaff);
        }
    }

    [Fact]
    public async Task SubscriptionQueryService_persists_display_enrichment_filters_and_sort()
    {
        await using var provider = BuildMvpPlanServices(fixture.ConnectionString, T0);
        await SeedPosProductAsync(provider);
        var ensure = provider.GetRequiredService<EnsureMvpPosPlans>();
        var createOrg = provider.GetRequiredService<CreatePlatformOrganization>();
        var startTrial = provider.GetRequiredService<StartTrialSubscription>();
        var plans = provider.GetRequiredService<IPlanRepository>();
        var queries = provider.GetRequiredService<SubscriptionQueryService>();

        await ensure.ExecuteAsync();
        var trialId = await SeedPosTrialAsync(provider);

        var productCode = ProductCode.Create(ProductCode.PinoyBusinessPos);
        var businessPlan = (await plans.GetByProductAndCodeAsync(
            productCode,
            PlanCode.Create(MvpPosPlanCodes.Business)))!;
        var starterPlan = (await plans.GetByProductAndCodeAsync(
            productCode,
            PlanCode.Create(MvpPosPlanCodes.Starter)))!;
        var businessVersion = (await plans.GetLatestPublishedVersionAsync(businessPlan.Id))!;
        var starterVersion = (await plans.GetLatestPublishedVersionAsync(starterPlan.Id))!;

        var orgA = (await createOrg.ExecuteAsync("Alpha Stores", $"alpha-{Guid.NewGuid():N}"[..20])).Value!;
        var orgB = (await createOrg.ExecuteAsync("Beta Market", $"beta-{Guid.NewGuid():N}"[..20])).Value!;
        var orgC = (await createOrg.ExecuteAsync("Gamma Shop", $"gamma-{Guid.NewGuid():N}"[..20])).Value!;

        Assert.True((await startTrial.ExecuteAsync(orgA.Id, businessPlan.Id, businessVersion.Id, trialId)).IsSuccess);
        Assert.True((await startTrial.ExecuteAsync(orgB.Id, businessPlan.Id, businessVersion.Id, trialId)).IsSuccess);
        Assert.True((await startTrial.ExecuteAsync(orgC.Id, starterPlan.Id, starterVersion.Id, trialId)).IsSuccess);

        var dto = await queries.GetByIdAsync(
            (await provider.GetRequiredService<ISubscriptionRepository>()
                .GetCurrentForOrganizationProductAsync(orgC.Id, productCode))!.Id.Value);
        Assert.NotNull(dto);
        Assert.Equal("Gamma Shop", dto!.OrganizationDisplayName);
        Assert.Equal("Pinoy Business POS", dto.ProductDisplayName);
        Assert.Equal("Starter", dto.PlanDisplayName);
        Assert.NotEqual(dto.ProductCode, dto.ProductDisplayName);

        var trialing = await queries.ListAsync(
            organizationId: null,
            productCode: ProductCode.PinoyBusinessPos,
            status: SubscriptionStatus.Trialing,
            search: null,
            isTrial: null,
            planId: null,
            SubscriptionListSortBy.OrganizationName,
            sortDescending: false,
            page: 1,
            pageSize: 10);
        Assert.Equal(3, trialing.TotalCount);
        Assert.All(trialing.Items, i => Assert.Equal(SubscriptionStatus.Trialing.ToString(), i.Status));

        var businessOnly = await queries.ListAsync(
            organizationId: null,
            productCode: null,
            status: null,
            search: null,
            isTrial: null,
            planId: businessPlan.Id.Value,
            SubscriptionListSortBy.CreatedAtUtc,
            sortDescending: false,
            page: 1,
            pageSize: 10);
        Assert.Equal(2, businessOnly.TotalCount);
        Assert.All(businessOnly.Items, i => Assert.Equal(MvpPosPlanCodes.Business, i.PlanKey));

        var firstPage = await queries.ListAsync(
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
    }

    [Fact]
    public async Task RetirePlan_preserves_subscription_history()
    {
        await using var provider = BuildMvpPlanServices(fixture.ConnectionString, T0);
        await SeedPosProductAsync(provider);
        var ensure = provider.GetRequiredService<EnsureMvpPosPlans>();
        var createOrg = provider.GetRequiredService<CreatePlatformOrganization>();
        var startTrial = provider.GetRequiredService<StartTrialSubscription>();
        var createPlan = provider.GetRequiredService<CreatePlan>();
        var activatePlan = provider.GetRequiredService<ActivatePlan>();
        var retirePlan = provider.GetRequiredService<RetirePlan>();
        var plans = provider.GetRequiredService<IPlanRepository>();
        var subscriptions = provider.GetRequiredService<ISubscriptionRepository>();

        await ensure.ExecuteAsync();
        var trialId = await SeedPosTrialAsync(provider);

        var productCode = ProductCode.Create(ProductCode.PinoyBusinessPos);
        var disposablePlanCode = $"retire-{Guid.NewGuid():N}"[..20];
        var disposablePlan = (await createPlan.ExecuteAsync(
            productCode.Value,
            disposablePlanCode,
            "Disposable Retire Plan")).Value!;
        await activatePlan.ExecuteAsync(disposablePlan.Id);
        var disposableVersion = (await plans.GetLatestPublishedVersionAsync(disposablePlan.Id));
        if (disposableVersion is null)
        {
            var createVersion = provider.GetRequiredService<CreateDraftPlanVersion>();
            var publish = provider.GetRequiredService<PublishExistingPlanVersion>();
            var grants = new[] { FeatureGrantSpec.Boolean(FeatureCode.Create(FeatureCode.CustomerCreditView), true) };
            await createVersion.ExecuteAsync(disposablePlan.Id, 1, BillingPeriod.Monthly, true, grants, T0);
            await publish.ExecuteAsync(disposablePlan.Id, 1);
            disposableVersion = (await plans.GetLatestPublishedVersionAsync(disposablePlan.Id))!;
        }

        var org = (await createOrg.ExecuteAsync("History Org", $"hist-{Guid.NewGuid():N}"[..20])).Value!;

        var started = await startTrial.ExecuteAsync(org.Id, disposablePlan.Id, disposableVersion.Id, trialId);
        Assert.True(started.IsSuccess);

        var retired = await retirePlan.ExecuteAsync(disposablePlan.Id);
        Assert.True(retired.IsSuccess);
        Assert.Equal(PlanStatus.Retired, retired.Value!.Status);

        var reloadedSub = await subscriptions.GetByIdAsync(started.Value!.Id);
        Assert.NotNull(reloadedSub);
        Assert.Equal(disposablePlan.Id, reloadedSub!.PlanId);

        var otherOrg = (await createOrg.ExecuteAsync("Blocked Org", $"blk-{Guid.NewGuid():N}"[..20])).Value!;
        var blocked = await startTrial.ExecuteAsync(otherOrg.Id, disposablePlan.Id, disposableVersion.Id, trialId);
        Assert.False(blocked.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.SubscriptionIneligible, blocked.ErrorCode);
    }

    private static ServiceProvider BuildMvpPlanServices(string connectionString, DateTimeOffset? utcNow = null)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:PlatformDatabase"] = connectionString
            })
            .Build();

        var services = new ServiceCollection();
        services.AddPlatformPersistence(configuration);
        services.AddLogging();

        services.AddScoped<CreateProduct>();
        services.AddScoped<CreateFeatureDefinition>();
        services.AddScoped<CreatePlan>();
        services.AddScoped<ActivatePlan>();
        services.AddScoped<UpdatePlanCommercialPackage>();
        services.AddScoped<CreateDraftPlanVersion>();
        services.AddScoped<PublishExistingPlanVersion>();
        services.AddScoped<CreateTrialDefinition>();
        services.AddScoped<RetirePlan>();
        services.AddScoped<EnsureMvpPosPlans>();

        services.AddScoped<CreatePlatformOrganization>();
        services.AddScoped<StartTrialSubscription>();
        services.AddScoped<SubscriptionQueryService>();

        services.AddSingleton<IClock>(new FixedUtcClock(utcNow ?? T0));

        return services.BuildServiceProvider();
    }

    private sealed class FixedUtcClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow { get; set; } = utcNow;
    }
}
