using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ExItS.Platform.Domain.Catalog;
using ExItS.Platform.Domain.Products;
using ExItS.Platform.IntegrationTests.Support;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ExItS.Platform.IntegrationTests;

[Collection(PostgreSqlCollection.Name)]
public sealed class ApiWp11CommercialIntegrationTests(PostgreSqlFixture fixture) : IAsyncLifetime
{
    private SessionApiFactory _factory = null!;
    private HttpClient _admin = null!;
    private HttpClient _client = null!;

    public Task InitializeAsync()
    {
        _factory = new SessionApiFactory(fixture.ConnectionString);
        _admin = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _client.Dispose();
        _admin.Dispose();
        _factory.Dispose();
        return Task.CompletedTask;
    }

    private static string Unique(string prefix) =>
        $"{prefix}{Guid.NewGuid():N}"[..Math.Min(20, prefix.Length + 32)].ToLowerInvariant();

    private static HttpRequestMessage Authed(HttpMethod method, string url, string token, object? body = null)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Add("X-ExItS-Session-Token", token);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        return request;
    }

    [Fact]
    public async Task Start_business_creates_org_subscription_with_commercial_flow()
    {
        var (token, _, _, _) = await SeedPersonalUserAsync("wp11");
        var slug = Unique("wp11");
        using var request = Authed(
            HttpMethod.Post,
            "/api/v1/personal/start-business",
            token,
            new
            {
                displayName = "Ana Sari-Sari",
                slug,
                activatePosEntitlement = true,
                activateProductAccess = true,
                assignPosOwnerRole = true
            });
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.NotEqual(Guid.Empty, body.GetProperty("organizationId").GetGuid());
        Assert.NotEqual(Guid.Empty, body.GetProperty("subscriptionId").GetGuid());
        Assert.Equal("Organization", body.GetProperty("accountClass").GetString());
    }

    [Fact]
    public async Task Subscription_upgrade_and_downgrade_endpoints_are_org_scoped()
    {
        var seededA = await SeedCatalogAsync("upa");
        var orgB = await CreateOrganizationAsync("upb");
        var subscriptionId = await StartTrialAsync(seededA);

        using var crossOrgUpgrade = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/v1/platform/organizations/{orgB}/subscriptions/{subscriptionId}/upgrade")
        {
            Content = JsonContent.Create(new
            {
                planKey = seededA.UpgradePlanKey,
                billingCycle = "Monthly",
                idempotencyKey = Guid.NewGuid().ToString("N")
            })
        };
        Assert.Equal(HttpStatusCode.NotFound, (await _admin.SendAsync(crossOrgUpgrade)).StatusCode);

        var upgraded = await _admin.PostAsJsonAsync(
            $"/api/v1/platform/organizations/{seededA.OrganizationId}/subscriptions/{subscriptionId}/upgrade",
            new { planKey = seededA.UpgradePlanKey, billingCycle = "Monthly", idempotencyKey = Guid.NewGuid().ToString("N") });
        upgraded.EnsureSuccessStatusCode();
        var upgradedBody = await upgraded.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(seededA.BusinessPlanId, upgradedBody.GetProperty("planId").GetGuid());

        var scheduled = await _admin.PostAsJsonAsync(
            $"/api/v1/platform/organizations/{seededA.OrganizationId}/subscriptions/{subscriptionId}/downgrade",
            new
            {
                planKey = seededA.StarterPlanKey,
                effectiveAtUtc = DateTimeOffset.UtcNow.AddMonths(1),
                idempotencyKey = Guid.NewGuid().ToString("N")
            });
        scheduled.EnsureSuccessStatusCode();
        var downBody = await scheduled.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(seededA.StarterPlanId, downBody.GetProperty("pendingPlanId").GetGuid());
    }

    [Fact]
    public async Task Plans_list_sorts_by_display_order_before_pagination()
    {
        var seeded = await SeedCatalogAsync("srt");
        var response = await _admin.GetAsync(
            $"/api/v1/platform/catalog/plans?productCode={seeded.ProductCode}&status=Active&sortBy=SortOrder&sortDesc=false&page=1&pageSize=2");
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var items = body.GetProperty("items").EnumerateArray().ToList();
        Assert.True(items.Count >= 2);
        Assert.True(items[0].GetProperty("sortOrder").GetInt32() <= items[1].GetProperty("sortOrder").GetInt32());
    }

    [Fact]
    public async Task Local_validation_enabled_endpoint_is_false_in_testing_environment_by_default()
    {
        var response = await _admin.GetAsync("/api/v1/platform/local-validation/enabled");
        response.EnsureSuccessStatusCode();
        var enabled = await response.Content.ReadFromJsonAsync<bool>();
        Assert.False(enabled);
    }

    private async Task<(string Token, Guid UserId, string Email, string Password)> SeedPersonalUserAsync(string prefix)
    {
        var (userId, email, password) = await PlatformIntegrationTestUsers.RegisterPersonalWithPasswordAsync(_client, prefix);
        var login = await _client.PostAsJsonAsync(
            "/api/v1/platform/auth/login",
            new { usernameOrEmail = email, password });
        login.EnsureSuccessStatusCode();
        var token = (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("sessionToken").GetString()!;
        return (token, userId, email, password);
    }

    private async Task<Guid> CreateOrganizationAsync(string prefix)
    {
        var response = await _admin.PostAsJsonAsync(
            "/api/v1/platform/organizations",
            new { displayName = $"{prefix} Org", slug = Unique(prefix) });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
    }

    private async Task<SeededCatalog> SeedCatalogAsync(string prefix)
    {
        var organizationId = await CreateOrganizationAsync(prefix);
        var productCode = Unique(prefix);
        (await _admin.PostAsJsonAsync(
            "/api/v1/platform/catalog/products",
            new { code = productCode, displayName = "WP11 Product" })).EnsureSuccessStatusCode();
        (await _admin.PostAsJsonAsync(
            $"/api/v1/platform/catalog/products/{productCode}/features",
            new
            {
                featureCode = FeatureCode.CustomerCreditView,
                displayName = "View Credit",
                valueType = nameof(FeatureValueType.Boolean)
            })).EnsureSuccessStatusCode();

        var starterKey = Unique("st");
        var businessKey = Unique("bz");
        var starterPlanId = await CreateActivePlanAsync(productCode, starterKey, "Starter", sortOrder: 10);
        var businessPlanId = await CreateActivePlanAsync(productCode, businessKey, "Business", sortOrder: 20);
        var starterVersionId = await PublishPlanVersionAsync(productCode, starterPlanId);
        await PublishPlanVersionAsync(productCode, businessPlanId);
        var trialId = await CreateTrialAsync(productCode, starterPlanId);

        return new SeededCatalog(
            organizationId,
            productCode,
            starterKey,
            businessKey,
            starterPlanId,
            businessPlanId,
            starterVersionId,
            trialId);
    }

    private async Task<Guid> CreateActivePlanAsync(string productCode, string planCode, string displayName, int sortOrder)
    {
        var plan = await _admin.PostAsJsonAsync(
            $"/api/v1/platform/catalog/products/{productCode}/plans",
            new { code = planCode, displayName, sortOrder, monthlyPrice = 499m, annualPrice = 4990m, currencyCode = "PHP" });
        plan.EnsureSuccessStatusCode();
        var planId = (await plan.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        (await _admin.PostAsync(
            $"/api/v1/platform/catalog/products/{productCode}/plans/{planId}/activate",
            null)).EnsureSuccessStatusCode();
        return planId;
    }

    private async Task<Guid> PublishPlanVersionAsync(string productCode, Guid planId)
    {
        var draft = await _admin.PostAsJsonAsync(
            $"/api/v1/platform/catalog/products/{productCode}/plans/{planId}/versions/draft",
            new
            {
                versionNumber = 1,
                billingPeriod = nameof(BillingPeriod.Monthly),
                trialEligible = true,
                grants = new[] { new { featureCode = FeatureCode.CustomerCreditView, enabled = true } }
            });
        draft.EnsureSuccessStatusCode();
        var versionId = (await draft.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        (await _admin.PostAsync(
            $"/api/v1/platform/catalog/products/{productCode}/plans/{planId}/versions/1/publish",
            null)).EnsureSuccessStatusCode();
        return versionId;
    }

    private async Task<Guid> CreateTrialAsync(string productCode, Guid planId)
    {
        var trial = await _admin.PostAsJsonAsync(
            $"/api/v1/platform/catalog/products/{productCode}/trials",
            new
            {
                displayName = "Trial",
                durationTicks = TimeSpan.FromDays(14).Ticks,
                planId,
                featureGrants = new[] { new { featureCode = FeatureCode.CustomerCreditView, enabled = true } },
                postExpiryFeatureGrants = Array.Empty<object>()
            });
        trial.EnsureSuccessStatusCode();
        return (await trial.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
    }

    private async Task<Guid> StartTrialAsync(SeededCatalog catalog)
    {
        var startResponse = await _admin.PostAsJsonAsync(
            $"/api/v1/platform/organizations/{catalog.OrganizationId}/subscriptions/trials",
            new
            {
                planId = catalog.StarterPlanId,
                planVersionId = catalog.StarterVersionId,
                trialDefinitionId = catalog.TrialId
            });
        startResponse.EnsureSuccessStatusCode();
        return (await startResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
    }

    private sealed record SeededCatalog(
        Guid OrganizationId,
        string ProductCode,
        string StarterPlanKey,
        string UpgradePlanKey,
        Guid StarterPlanId,
        Guid BusinessPlanId,
        Guid StarterVersionId,
        Guid TrialId);
}
