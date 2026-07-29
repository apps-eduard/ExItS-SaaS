using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ExItS.Platform.Domain.Catalog;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace ExItS.Platform.IntegrationTests;

[Collection(PostgreSqlCollection.Name)]
public sealed class ApiIdentityAccessTests(PostgreSqlFixture fixture) : IAsyncLifetime
{
    private IdentityAccessApiFactory _factory = null!;
    private HttpClient _client = null!;

    public Task InitializeAsync()
    {
        _factory = new IdentityAccessApiFactory(fixture.ConnectionString);
        _client = _factory.CreateClient();
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _client.Dispose();
        _factory.Dispose();
        return Task.CompletedTask;
    }

    private static string UniqueToken(string prefix) =>
        $"{prefix}{Guid.NewGuid():N}"[..Math.Min(24, prefix.Length + 32)].ToLowerInvariant();

    [Fact]
    public async Task User_create_duplicate_conflicts_and_lifecycle_work()
    {
        var username = UniqueToken("user");
        var email = $"{username}@example.com";

        var create = await _client.PostAsJsonAsync(
            "/api/v1/platform/users",
            new { username, displayName = "Ada Lovelace", email });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var user = await create.Content.ReadFromJsonAsync<JsonElement>();
        var userId = user.GetProperty("id").GetGuid();
        Assert.Equal("Active", user.GetProperty("status").GetString());

        var emailConflict = await _client.PostAsJsonAsync(
            "/api/v1/platform/users",
            new { username = UniqueToken("user2"), displayName = "Ada Two", email = email.ToUpperInvariant() });
        Assert.Equal(HttpStatusCode.Conflict, emailConflict.StatusCode);

        var usernameConflict = await _client.PostAsJsonAsync(
            "/api/v1/platform/users",
            new { username = username.ToUpperInvariant(), displayName = "Ada Three", email = $"{UniqueToken("u3")}@example.com" });
        Assert.Equal(HttpStatusCode.Conflict, usernameConflict.StatusCode);

        var list = await _client.GetAsync($"/api/v1/platform/users?search={username}&page=1&pageSize=10");
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        Assert.True((await list.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("totalCount").GetInt32() >= 1);

        var suspend = await _client.PostAsJsonAsync($"/api/v1/platform/users/{userId}/suspend", new { reason = "hold" });
        Assert.Equal(HttpStatusCode.OK, suspend.StatusCode);
        Assert.Equal("Suspended", (await suspend.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("status").GetString());

        var reactivate = await _client.PostAsync($"/api/v1/platform/users/{userId}/reactivate", null);
        Assert.Equal(HttpStatusCode.OK, reactivate.StatusCode);

        var disable = await _client.PostAsync($"/api/v1/platform/users/{userId}/disable", null);
        Assert.Equal(HttpStatusCode.OK, disable.StatusCode);
        Assert.Equal("Deactivated", (await disable.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("status").GetString());
    }

    [Fact]
    public async Task Membership_and_product_access_flow_with_effective_evaluation()
    {
        var seeded = await SeedCommercialContextAsync("access");
        var username = UniqueToken("mem");
        var createUser = await _client.PostAsJsonAsync(
            "/api/v1/platform/users",
            new { username, displayName = "Member User", email = $"{username}@example.com" });
        createUser.EnsureSuccessStatusCode();
        var userId = (await createUser.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var add = await _client.PostAsJsonAsync(
            $"/api/v1/platform/organizations/{seeded.OrganizationId}/members",
            new { userId, role = "OrganizationMember" });
        Assert.Equal(HttpStatusCode.Created, add.StatusCode);
        var membershipId = (await add.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var duplicateMembership = await _client.PostAsJsonAsync(
            $"/api/v1/platform/organizations/{seeded.OrganizationId}/members",
            new { userId, role = "OrganizationOwner" });
        Assert.Equal(HttpStatusCode.Conflict, duplicateMembership.StatusCode);

        var grant = await _client.PostAsJsonAsync(
            $"/api/v1/platform/organizations/{seeded.OrganizationId}/product-access",
            new { userId, productCode = seeded.ProductCode, grantedByActor = "dev-admin", reason = "test" });
        Assert.Equal(HttpStatusCode.Created, grant.StatusCode);
        var assignmentId = (await grant.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var evaluate = await _client.GetAsync(
            $"/api/v1/platform/access/evaluate?userId={userId}&organizationId={seeded.OrganizationId}&productCode={seeded.ProductCode}");
        Assert.Equal(HttpStatusCode.OK, evaluate.StatusCode);
        var allowed = await evaluate.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(allowed.GetProperty("allowed").GetBoolean());
        Assert.Equal("allowed", allowed.GetProperty("reasonCode").GetString());

        var suspendMembership = await _client.PostAsJsonAsync(
            $"/api/v1/platform/memberships/{membershipId}/suspend",
            new { reason = "temp", actorReference = "dev-admin" });
        Assert.Equal(HttpStatusCode.OK, suspendMembership.StatusCode);

        var denied = await _client.GetAsync(
            $"/api/v1/platform/access/evaluate?userId={userId}&organizationId={seeded.OrganizationId}&productCode={seeded.ProductCode}");
        var deniedBody = await denied.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(deniedBody.GetProperty("allowed").GetBoolean());
        Assert.Equal("membership_inactive", deniedBody.GetProperty("reasonCode").GetString());

        var reactivateMembership = await _client.PostAsJsonAsync(
            $"/api/v1/platform/memberships/{membershipId}/reactivate",
            new { actorReference = "dev-admin" });
        Assert.Equal(HttpStatusCode.OK, reactivateMembership.StatusCode);

        var reallowed = await _client.GetAsync(
            $"/api/v1/platform/access/evaluate?userId={userId}&organizationId={seeded.OrganizationId}&productCode={seeded.ProductCode}");
        Assert.True((await reallowed.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("allowed").GetBoolean());

        var otherOrg = await _client.PostAsJsonAsync(
            "/api/v1/platform/organizations",
            new { displayName = "Other Org", slug = UniqueToken("other") });
        otherOrg.EnsureSuccessStatusCode();
        var otherOrgId = (await otherOrg.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var crossGrant = await _client.PostAsJsonAsync(
            $"/api/v1/platform/organizations/{otherOrgId}/product-access",
            new { userId, productCode = seeded.ProductCode, grantedByActor = "dev-admin" });
        Assert.True(crossGrant.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Conflict);

        var revoke = await _client.PostAsJsonAsync(
            $"/api/v1/platform/product-access/{assignmentId}/revoke",
            new { revokedByActor = "dev-admin", reason = "done" });
        Assert.Equal(HttpStatusCode.OK, revoke.StatusCode);
        Assert.Equal("Revoked", (await revoke.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("status").GetString());

        var history = await _client.GetAsync(
            $"/api/v1/platform/organizations/{seeded.OrganizationId}/product-access?status=Revoked");
        Assert.Equal(HttpStatusCode.OK, history.StatusCode);
        Assert.True((await history.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("totalCount").GetInt32() >= 1);
    }

    private async Task<(Guid OrganizationId, string ProductCode)> SeedCommercialContextAsync(string prefix)
    {
        var orgResponse = await _client.PostAsJsonAsync(
            "/api/v1/platform/organizations",
            new { displayName = $"{prefix} Org", slug = UniqueToken(prefix) });
        orgResponse.EnsureSuccessStatusCode();
        var organizationId = (await orgResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var productCode = $"p{Guid.NewGuid():N}"[..16];

        (await _client.PostAsJsonAsync("/api/v1/platform/catalog/products", new { code = productCode, displayName = "POS" }))
            .EnsureSuccessStatusCode();
        (await _client.PostAsJsonAsync(
            $"/api/v1/platform/catalog/products/{productCode}/features",
            new { featureCode = FeatureCode.CustomerCreditView, displayName = "View", valueType = nameof(FeatureValueType.Boolean) }))
            .EnsureSuccessStatusCode();

        var plan = await _client.PostAsJsonAsync(
            $"/api/v1/platform/catalog/products/{productCode}/plans",
            new { code = "utang", displayName = "Utang" });
        plan.EnsureSuccessStatusCode();
        var planId = (await plan.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        (await _client.PostAsync($"/api/v1/platform/catalog/products/{productCode}/plans/{planId}/activate", null))
            .EnsureSuccessStatusCode();

        var draft = await _client.PostAsJsonAsync(
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
        (await _client.PostAsync($"/api/v1/platform/catalog/products/{productCode}/plans/{planId}/versions/1/publish", null))
            .EnsureSuccessStatusCode();

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

        var start = await _client.PostAsJsonAsync(
            $"/api/v1/platform/organizations/{organizationId}/subscriptions/trials",
            new { planId, planVersionId = versionId, trialDefinitionId = trialId });
        start.EnsureSuccessStatusCode();
        var subscriptionId = (await start.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var now = DateTimeOffset.UtcNow;
        (await _client.PostAsJsonAsync(
            $"/api/v1/platform/subscriptions/{subscriptionId}/activate",
            new { periodStartUtc = now, periodEndUtc = now.AddDays(30) })).EnsureSuccessStatusCode();

        var snapshot = await _client.PostAsJsonAsync(
            $"/api/v1/platform/organizations/{organizationId}/products/{productCode}/entitlements/snapshots",
            new { });
        snapshot.EnsureSuccessStatusCode();

        return (organizationId, productCode);
    }

    private sealed class IdentityAccessApiFactory(string connectionString) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
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
