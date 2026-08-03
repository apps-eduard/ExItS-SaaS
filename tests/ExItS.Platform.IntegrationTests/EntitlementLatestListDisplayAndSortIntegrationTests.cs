using ExItS.Platform.Application.Admin;
using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Entitlements;
using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Application.Subscriptions;
using ExItS.Platform.Domain.Catalog;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Products;
using ExItS.Platform.Domain.Subscriptions;
using ExItS.Platform.IntegrationTests.Support;
using Microsoft.Extensions.DependencyInjection;

namespace ExItS.Platform.IntegrationTests;

[Collection(PostgreSqlCollection.Name)]
public sealed class EntitlementLatestListDisplayAndSortIntegrationTests(PostgreSqlFixture fixture)
{
    private static readonly DateTimeOffset SeedBase = new(2099, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private sealed record SeededEntitlementRow(
        string OrganizationDisplayName,
        string ProductDisplayName,
        string ProductCode,
        string Status,
        int Revision,
        DateTimeOffset GeneratedAtUtc);

    private sealed record SeededPortfolio(
        IReadOnlyList<SeededEntitlementRow> Rows,
        HashSet<string> ProductCodes);

    private static string Unique(string prefix) => $"{prefix}-{Guid.NewGuid():N}";

    private static async Task<(PlatformOrganizationId OrganizationId, SubscriptionId SubscriptionId, ProductCode ProductCode)>
        SeedTrialEligibleOrganizationAsync(
            IServiceProvider provider,
            string orgDisplayName,
            string productDisplayName,
            string prefix)
    {
        var createOrg = provider.GetRequiredService<CreatePlatformOrganization>();
        var org = (await createOrg.ExecuteAsync(orgDisplayName, Unique(prefix)).ConfigureAwait(false)).Value!;

        var candidate = Unique(prefix);
        var productCodeValue = candidate[..Math.Min(30, candidate.Length)];
        var createProduct = provider.GetRequiredService<CreateProduct>();
        var createFeature = provider.GetRequiredService<CreateFeatureDefinition>();
        var createPlan = provider.GetRequiredService<CreatePlan>();
        var activatePlan = provider.GetRequiredService<ActivatePlan>();
        var createVersion = provider.GetRequiredService<CreateDraftPlanVersion>();
        var publish = provider.GetRequiredService<PublishExistingPlanVersion>();
        var createTrial = provider.GetRequiredService<CreateTrialDefinition>();

        await createProduct.ExecuteAsync(productCodeValue, productDisplayName).ConfigureAwait(false);
        await createFeature.ExecuteAsync(
            productCodeValue, FeatureCode.CustomerCreditView, "View", FeatureValueType.Boolean).ConfigureAwait(false);

        var plan = (await createPlan.ExecuteAsync(productCodeValue, "utang", "Utang").ConfigureAwait(false)).Value!;
        await activatePlan.ExecuteAsync(plan.Id).ConfigureAwait(false);

        var grants = new[]
        {
            FeatureGrantSpec.Boolean(FeatureCode.Create(FeatureCode.CustomerCreditView), true)
        };
        var version = (await createVersion
            .ExecuteAsync(plan.Id, 1, BillingPeriod.Monthly, true, grants, SeedBase)
            .ConfigureAwait(false)).Value!;
        await publish.ExecuteAsync(plan.Id, 1).ConfigureAwait(false);

        var trial = (await createTrial
            .ExecuteAsync(productCodeValue, "Trial", TimeSpan.FromDays(21), grants, Array.Empty<FeatureGrantSpec>())
            .ConfigureAwait(false)).Value!;

        var startTrial = provider.GetRequiredService<StartTrialSubscription>();
        var subscription = (await startTrial.ExecuteAsync(org.Id, plan.Id, version.Id, trial.Id)
            .ConfigureAwait(false)).Value!;

        return (org.Id, subscription.Id, ProductCode.Create(productCodeValue));
    }

    private static async Task<SeededPortfolio> SeedPortfolioAsync(
        ServiceProvider provider,
        CommercialTestServices.TestUtcClock clock)
    {
        var runId = Guid.NewGuid().ToString("N")[..8];
        clock.UtcNow = SeedBase;
        var generate = provider.GetRequiredService<GenerateEntitlementSnapshot>();
        var activate = provider.GetRequiredService<ActivateSubscription>();
        var suspend = provider.GetRequiredService<SuspendSubscription>();

        var alphaOrgName = $"000 Entlist Alpha {runId}";
        var gammaOrgName = $"222 Entlist Gamma {runId}";
        var betaOrgName = $"111 Entlist Beta {runId}";
        var alphaProductName = $"000 Entlist Alpha POS {runId}";
        var gammaProductName = $"222 Entlist Gamma POS {runId}";
        var betaProductName = $"111 Entlist Beta POS {runId}";

        var alpha = await SeedTrialEligibleOrganizationAsync(
            provider, alphaOrgName, alphaProductName, $"ent-alpha-{runId}");
        await generate.ExecuteAsync(alpha.OrganizationId, alpha.ProductCode);
        var alphaGenerated = clock.UtcNow;

        clock.Advance(TimeSpan.FromHours(1));
        var gamma = await SeedTrialEligibleOrganizationAsync(
            provider, gammaOrgName, gammaProductName, $"ent-gamma-{runId}");
        await suspend.ExecuteAsync(gamma.SubscriptionId);
        await generate.ExecuteAsync(gamma.OrganizationId, gamma.ProductCode);
        await generate.ExecuteAsync(gamma.OrganizationId, gamma.ProductCode);
        var gammaGenerated = clock.UtcNow;

        clock.Advance(TimeSpan.FromHours(1));
        var beta = await SeedTrialEligibleOrganizationAsync(
            provider, betaOrgName, betaProductName, $"ent-beta-{runId}");
        await activate.ExecuteAsync(
            beta.SubscriptionId,
            clock.UtcNow,
            clock.UtcNow.AddMonths(1));
        await generate.ExecuteAsync(beta.OrganizationId, beta.ProductCode);
        await generate.ExecuteAsync(beta.OrganizationId, beta.ProductCode);
        await generate.ExecuteAsync(beta.OrganizationId, beta.ProductCode);
        var betaGenerated = clock.UtcNow;

        var rows = new[]
        {
            new SeededEntitlementRow(
                alphaOrgName,
                alphaProductName,
                alpha.ProductCode.Value,
                SubscriptionStatus.Trialing.ToString(),
                1,
                alphaGenerated),
            new SeededEntitlementRow(
                gammaOrgName,
                gammaProductName,
                gamma.ProductCode.Value,
                SubscriptionStatus.Suspended.ToString(),
                2,
                gammaGenerated),
            new SeededEntitlementRow(
                betaOrgName,
                betaProductName,
                beta.ProductCode.Value,
                SubscriptionStatus.Active.ToString(),
                3,
                betaGenerated)
        };

        return new SeededPortfolio(rows, rows.Select(r => r.ProductCode).ToHashSet(StringComparer.Ordinal));
    }

    private static AdminPortfolioQueryService GetQueries(ServiceProvider provider) =>
        provider.GetRequiredService<AdminPortfolioQueryService>();

    private static IReadOnlyList<EntitlementLatestSummaryDto> FilterSeeded(
        IEnumerable<EntitlementLatestSummaryDto> items,
        SeededPortfolio seeded) =>
        items.Where(i => seeded.ProductCodes.Contains(i.ProductCode)).ToList();

    [Fact]
    public async Task ListLatestEntitlements_returns_organization_and_product_display_names()
    {
        await using var provider = CommercialTestServices.Build(fixture.ConnectionString, SeedBase);
        var clock = provider.GetRequiredService<CommercialTestServices.TestUtcClock>();
        var seeded = await SeedPortfolioAsync(provider, clock);
        var queries = GetQueries(provider);

        var result = FilterSeeded(
            (await queries.ListLatestEntitlementsAsync(page: 1, pageSize: 200)).Items,
            seeded);

        Assert.Equal(3, result.Count);

        foreach (var item in result)
        {
            var expected = seeded.Rows.Single(s => s.ProductCode == item.ProductCode);

            Assert.Equal(expected.OrganizationDisplayName, item.OrganizationDisplayName);
            Assert.Equal(expected.ProductDisplayName, item.ProductDisplayName);
            Assert.NotEqual(item.ProductCode, item.ProductDisplayName);
            Assert.DoesNotContain(item.OrganizationId.ToString("D"), item.OrganizationDisplayName!);
            Assert.DoesNotContain(item.ProductCode, item.ProductDisplayName!, StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task ListLatestEntitlements_returns_latest_snapshot_revision_per_org_product_pair()
    {
        await using var provider = CommercialTestServices.Build(fixture.ConnectionString, SeedBase);
        var clock = provider.GetRequiredService<CommercialTestServices.TestUtcClock>();
        var seeded = await SeedPortfolioAsync(provider, clock);
        var queries = GetQueries(provider);

        var result = FilterSeeded(
            (await queries.ListLatestEntitlementsAsync(page: 1, pageSize: 200)).Items,
            seeded);

        Assert.Equal(3, result.Count);
        foreach (var item in result)
        {
            var expected = seeded.Rows.Single(s => s.ProductCode == item.ProductCode);
            Assert.Equal(expected.Revision, item.SnapshotVersion);
        }
    }

    [Theory]
    [InlineData(EntitlementListSortBy.Revision, false)]
    [InlineData(EntitlementListSortBy.Revision, true)]
    [InlineData(EntitlementListSortBy.OrganizationDisplayName, false)]
    [InlineData(EntitlementListSortBy.OrganizationDisplayName, true)]
    [InlineData(EntitlementListSortBy.ProductDisplayName, false)]
    [InlineData(EntitlementListSortBy.ProductDisplayName, true)]
    [InlineData(EntitlementListSortBy.Status, false)]
    [InlineData(EntitlementListSortBy.Status, true)]
    [InlineData(EntitlementListSortBy.GeneratedAtUtc, false)]
    [InlineData(EntitlementListSortBy.GeneratedAtUtc, true)]
    public async Task ListLatestEntitlements_page_size_one_applies_sort_before_pagination(
        EntitlementListSortBy sortBy,
        bool sortDescending)
    {
        await using var provider = CommercialTestServices.Build(fixture.ConnectionString, SeedBase);
        var clock = provider.GetRequiredService<CommercialTestServices.TestUtcClock>();
        var seeded = await SeedPortfolioAsync(provider, clock);
        var queries = GetQueries(provider);

        var sortedSeeded = await queries.ListLatestEntitlementsAsync(
            page: 1,
            pageSize: 500,
            sortBy: sortBy,
            sortDescending: sortDescending);
        var seededInSortOrder = FilterSeeded(sortedSeeded.Items, seeded);
        Assert.Equal(3, seededInSortOrder.Count);

        var target = seededInSortOrder[0];
        var globalIndex = sortedSeeded.Items.ToList().FindIndex(i => i.Id == target.Id);
        Assert.True(globalIndex >= 0);
        var expectedPage = globalIndex + 1;

        var page = await queries.ListLatestEntitlementsAsync(
            page: expectedPage,
            pageSize: 1,
            sortBy: sortBy,
            sortDescending: sortDescending);

        Assert.Single(page.Items);
        Assert.Equal(target.Id, page.Items[0].Id);
    }

    [Fact]
    public async Task ListLatestEntitlements_default_sort_is_generated_descending_then_organization_ascending()
    {
        await using var provider = CommercialTestServices.Build(fixture.ConnectionString, SeedBase);
        var clock = provider.GetRequiredService<CommercialTestServices.TestUtcClock>();
        var seeded = await SeedPortfolioAsync(provider, clock);
        var queries = GetQueries(provider);

        var defaultSort = FilterSeeded(
            (await queries.ListLatestEntitlementsAsync(page: 1, pageSize: 200)).Items,
            seeded);
        Assert.Equal(
            seeded.Rows.OrderByDescending(r => r.GeneratedAtUtc).ThenBy(r => r.OrganizationDisplayName)
                .Select(r => r.OrganizationDisplayName)
                .ToList(),
            defaultSort.Select(i => i.OrganizationDisplayName!).ToList());

        var explicitDefault = FilterSeeded(
            (await queries.ListLatestEntitlementsAsync(
                page: 1,
                pageSize: 200,
                sortBy: EntitlementListSortBy.GeneratedAtUtc,
                sortDescending: true)).Items,
            seeded);
        Assert.Equal(
            defaultSort.Select(i => i.Id),
            explicitDefault.Select(i => i.Id));

        var runId = Guid.NewGuid().ToString("N")[..8];
        clock.UtcNow = SeedBase.AddHours(4);
        var generate = provider.GetRequiredService<GenerateEntitlementSnapshot>();
        var createOrg = provider.GetRequiredService<CreatePlatformOrganization>();
        var createProduct = provider.GetRequiredService<CreateProduct>();
        var createFeature = provider.GetRequiredService<CreateFeatureDefinition>();
        var createPlan = provider.GetRequiredService<CreatePlan>();
        var activatePlan = provider.GetRequiredService<ActivatePlan>();
        var createVersion = provider.GetRequiredService<CreateDraftPlanVersion>();
        var publish = provider.GetRequiredService<PublishExistingPlanVersion>();
        var createTrial = provider.GetRequiredService<CreateTrialDefinition>();
        var startTrial = provider.GetRequiredService<StartTrialSubscription>();

        async Task<(string OrgName, string ProductCode)> SeedSameTimePair(string orgName, string productName, string prefix)
        {
            var org = (await createOrg.ExecuteAsync(orgName, Unique(prefix)).ConfigureAwait(false)).Value!;
            var unique = Unique(prefix);
            var code = unique[..Math.Min(30, unique.Length)];
            await createProduct.ExecuteAsync(code, productName).ConfigureAwait(false);
            await createFeature.ExecuteAsync(code, FeatureCode.CustomerCreditView, "View", FeatureValueType.Boolean)
                .ConfigureAwait(false);
            var plan = (await createPlan.ExecuteAsync(code, "utang", "Utang").ConfigureAwait(false)).Value!;
            await activatePlan.ExecuteAsync(plan.Id).ConfigureAwait(false);
            var grants = new[] { FeatureGrantSpec.Boolean(FeatureCode.Create(FeatureCode.CustomerCreditView), true) };
            var version = (await createVersion.ExecuteAsync(plan.Id, 1, BillingPeriod.Monthly, true, grants, clock.UtcNow)
                .ConfigureAwait(false)).Value!;
            await publish.ExecuteAsync(plan.Id, 1).ConfigureAwait(false);
            var trial = (await createTrial.ExecuteAsync(code, "Trial", TimeSpan.FromDays(21), grants, Array.Empty<FeatureGrantSpec>())
                .ConfigureAwait(false)).Value!;
            var sub = (await startTrial.ExecuteAsync(org.Id, plan.Id, version.Id, trial.Id).ConfigureAwait(false)).Value!;
            await generate.ExecuteAsync(org.Id, ProductCode.Create(code)).ConfigureAwait(false);
            Assert.NotNull(sub);
            return (orgName, code);
        }

        var mangoOrg = $"333 Entlist Mango {runId}";
        var appleOrg = $"111 Entlist Apple {runId}";
        var mango = await SeedSameTimePair(mangoOrg, $"333 Entlist Mango POS {runId}", $"tie-mango-{runId}");
        var apple = await SeedSameTimePair(appleOrg, $"111 Entlist Apple POS {runId}", $"tie-apple-{runId}");
        var tieProductCodes = new HashSet<string>(StringComparer.Ordinal) { mango.ProductCode, apple.ProductCode };

        var tieBreak = FilterSeeded(
            (await queries.ListLatestEntitlementsAsync(page: 1, pageSize: 200)).Items,
            new SeededPortfolio([], tieProductCodes));
        Assert.Equal(2, tieBreak.Count);
        Assert.Equal(appleOrg, tieBreak[0].OrganizationDisplayName);
        Assert.Equal(mangoOrg, tieBreak[1].OrganizationDisplayName);
    }
}
