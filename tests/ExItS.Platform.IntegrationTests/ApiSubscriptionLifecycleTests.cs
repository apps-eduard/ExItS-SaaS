using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ExItS.Platform.Domain.Catalog;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace ExItS.Platform.IntegrationTests;

[Collection(PostgreSqlCollection.Name)]
public sealed class ApiSubscriptionLifecycleTests(PostgreSqlFixture fixture) : IAsyncLifetime
{
    private SubscriptionApiFactory _factory = null!;
    private HttpClient _client = null!;

    public Task InitializeAsync()
    {
        _factory = new SubscriptionApiFactory(fixture.ConnectionString);
        _client = _factory.CreateClient();
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _client.Dispose();
        _factory.Dispose();
        return Task.CompletedTask;
    }

    private static string Unique(string prefix) => $"{prefix}-{Guid.NewGuid():N}";

    private async Task<(Guid organizationId, string productCode, Guid planId, Guid versionId, Guid trialId)>
        SeedOrganizationAndTrialEligibleCatalogAsync(string prefix)
    {
        var orgResponse = await _client.PostAsJsonAsync(
            "/api/v1/platform/organizations",
            new { displayName = $"{prefix} Org", slug = Unique(prefix) });
        orgResponse.EnsureSuccessStatusCode();
        var organizationId = (await orgResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var candidate = Unique(prefix);
        var productCode = candidate[..Math.Min(30, candidate.Length)];
        var product = await _client.PostAsJsonAsync(
            "/api/v1/platform/catalog/products",
            new { code = productCode, displayName = "POS" });
        product.EnsureSuccessStatusCode();

        var feature = await _client.PostAsJsonAsync(
            $"/api/v1/platform/catalog/products/{productCode}/features",
            new
            {
                featureCode = FeatureCode.CustomerCreditView,
                displayName = "View Credit",
                valueType = nameof(FeatureValueType.Boolean)
            });
        feature.EnsureSuccessStatusCode();

        var plan = await _client.PostAsJsonAsync(
            $"/api/v1/platform/catalog/products/{productCode}/plans",
            new { code = "utang", displayName = "Utang" });
        plan.EnsureSuccessStatusCode();
        var planId = (await plan.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var activatePlan = await _client.PostAsync(
            $"/api/v1/platform/catalog/products/{productCode}/plans/{planId}/activate",
            null);
        activatePlan.EnsureSuccessStatusCode();

        var draft = await _client.PostAsJsonAsync(
            $"/api/v1/platform/catalog/products/{productCode}/plans/{planId}/versions/draft",
            new
            {
                versionNumber = 1,
                billingPeriod = nameof(BillingPeriod.Monthly),
                trialEligible = true,
                grants = new[]
                {
                    new { featureCode = FeatureCode.CustomerCreditView, enabled = true }
                }
            });
        draft.EnsureSuccessStatusCode();
        var versionId = (await draft.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var publish = await _client.PostAsync(
            $"/api/v1/platform/catalog/products/{productCode}/plans/{planId}/versions/1/publish",
            null);
        publish.EnsureSuccessStatusCode();

        var trial = await _client.PostAsJsonAsync(
            $"/api/v1/platform/catalog/products/{productCode}/trials",
            new
            {
                displayName = "Trial",
                durationTicks = TimeSpan.FromDays(21).Ticks,
                featureGrants = new[] { new { featureCode = FeatureCode.CustomerCreditView, enabled = true } },
                postExpiryFeatureGrants = Array.Empty<object>()
            });
        trial.EnsureSuccessStatusCode();
        var trialId = (await trial.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        return (organizationId, productCode, planId, versionId, trialId);
    }

    [Fact]
    public async Task Create_organization_get_by_id_and_reject_duplicate_slug()
    {
        var slug = Unique("api-org");
        var create = await _client.PostAsJsonAsync(
            "/api/v1/platform/organizations",
            new { displayName = "API Org", slug });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var id = (await create.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var get = await _client.GetAsync($"/api/v1/platform/organizations/{id}");
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);

        var duplicate = await _client.PostAsJsonAsync(
            "/api/v1/platform/organizations",
            new { displayName = "API Org Two", slug });
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);

        var missing = await _client.GetAsync($"/api/v1/platform/organizations/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    [Fact]
    public async Task Start_trial_get_current_and_reject_second_active_like_subscription()
    {
        var (organizationId, productCode, planId, versionId, trialId) =
            await SeedOrganizationAndTrialEligibleCatalogAsync("api-trial");

        var start = await _client.PostAsJsonAsync(
            $"/api/v1/platform/organizations/{organizationId}/subscriptions/trials",
            new { planId, planVersionId = versionId, trialDefinitionId = trialId });
        Assert.Equal(HttpStatusCode.Created, start.StatusCode);
        var subscription = await start.Content.ReadFromJsonAsync<JsonElement>();
        var subscriptionId = subscription.GetProperty("id").GetGuid();
        Assert.Equal("Trialing", subscription.GetProperty("status").GetString());

        var current = await _client.GetAsync(
            $"/api/v1/platform/organizations/{organizationId}/subscriptions/current?productCode={productCode}");
        Assert.Equal(HttpStatusCode.OK, current.StatusCode);
        var currentBody = await current.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(subscriptionId, currentBody.GetProperty("id").GetGuid());

        var getById = await _client.GetAsync($"/api/v1/platform/subscriptions/{subscriptionId}");
        Assert.Equal(HttpStatusCode.OK, getById.StatusCode);

        var conflict = await _client.PostAsJsonAsync(
            $"/api/v1/platform/organizations/{organizationId}/subscriptions/trials",
            new { planId, planVersionId = versionId, trialDefinitionId = trialId });
        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);

        var listByOrg = await _client.GetAsync($"/api/v1/platform/organizations/{organizationId}/subscriptions");
        Assert.Equal(HttpStatusCode.OK, listByOrg.StatusCode);
        var page = await listByOrg.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, page.GetProperty("totalCount").GetInt32());
    }

    [Fact]
    public async Task Subscription_lifecycle_endpoints_activate_grace_pastdue_suspend_reactivate_cancel()
    {
        var (organizationId, _, planId, versionId, trialId) =
            await SeedOrganizationAndTrialEligibleCatalogAsync("api-lifecycle");

        var start = await _client.PostAsJsonAsync(
            $"/api/v1/platform/organizations/{organizationId}/subscriptions/trials",
            new { planId, planVersionId = versionId, trialDefinitionId = trialId });
        start.EnsureSuccessStatusCode();
        var subscriptionId = (await start.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var now = DateTimeOffset.UtcNow;
        var activate = await _client.PostAsJsonAsync(
            $"/api/v1/platform/subscriptions/{subscriptionId}/activate",
            new { periodStartUtc = now, periodEndUtc = now.AddDays(30) });
        Assert.Equal(HttpStatusCode.OK, activate.StatusCode);
        var activated = await activate.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Active", activated.GetProperty("status").GetString());

        var grace = await _client.PostAsJsonAsync(
            $"/api/v1/platform/subscriptions/{subscriptionId}/grace-period",
            new { gracePeriodEndUtc = now.AddDays(37) });
        Assert.Equal(HttpStatusCode.OK, grace.StatusCode);
        Assert.Equal(
            "GracePeriod",
            (await grace.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("status").GetString());

        var pastDue = await _client.PostAsync($"/api/v1/platform/subscriptions/{subscriptionId}/past-due", null);
        Assert.Equal(HttpStatusCode.OK, pastDue.StatusCode);
        Assert.Equal(
            "PastDue",
            (await pastDue.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("status").GetString());

        var suspend = await _client.PostAsync($"/api/v1/platform/subscriptions/{subscriptionId}/suspend", null);
        Assert.Equal(HttpStatusCode.OK, suspend.StatusCode);
        Assert.Equal(
            "Suspended",
            (await suspend.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("status").GetString());

        var reactivate = await _client.PostAsync($"/api/v1/platform/subscriptions/{subscriptionId}/reactivate", null);
        Assert.Equal(HttpStatusCode.OK, reactivate.StatusCode);
        Assert.Equal(
            "Active",
            (await reactivate.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("status").GetString());

        var cancel = await _client.PostAsync($"/api/v1/platform/subscriptions/{subscriptionId}/cancel", null);
        Assert.Equal(HttpStatusCode.OK, cancel.StatusCode);
        Assert.Equal(
            "Cancelled",
            (await cancel.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("status").GetString());

        // Terminal state: further lifecycle transitions are rejected as conflicts.
        var reactivateTerminal = await _client.PostAsync(
            $"/api/v1/platform/subscriptions/{subscriptionId}/reactivate", null);
        Assert.Equal(HttpStatusCode.Conflict, reactivateTerminal.StatusCode);
    }

    [Fact]
    public async Task Expire_endpoint_marks_subscription_terminal()
    {
        var (organizationId, _, planId, versionId, trialId) =
            await SeedOrganizationAndTrialEligibleCatalogAsync("api-expire");

        var start = await _client.PostAsJsonAsync(
            $"/api/v1/platform/organizations/{organizationId}/subscriptions/trials",
            new { planId, planVersionId = versionId, trialDefinitionId = trialId });
        start.EnsureSuccessStatusCode();
        var subscriptionId = (await start.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var expire = await _client.PostAsync($"/api/v1/platform/subscriptions/{subscriptionId}/expire", null);
        Assert.Equal(HttpStatusCode.OK, expire.StatusCode);
        Assert.Equal(
            "Expired",
            (await expire.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("status").GetString());
    }

    [Fact]
    public async Task Suspend_organization_endpoint_transitions_status()
    {
        var orgResponse = await _client.PostAsJsonAsync(
            "/api/v1/platform/organizations",
            new { displayName = "Suspend Org", slug = Unique("suspend-org") });
        orgResponse.EnsureSuccessStatusCode();
        var organizationId = (await orgResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var suspend = await _client.PostAsync($"/api/v1/platform/organizations/{organizationId}/suspend", null);
        Assert.Equal(HttpStatusCode.OK, suspend.StatusCode);
        Assert.Equal(
            "Suspended",
            (await suspend.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("status").GetString());
    }

    private sealed class SubscriptionApiFactory(string connectionString) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("ConnectionStrings:PlatformDatabase", connectionString);
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:PlatformDatabase"] = connectionString
                });
            });
        }
    }
}
