using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ExItS.Platform.Domain.Catalog;
using ExItS.Platform.IntegrationTests.Support;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ExItS.Platform.IntegrationTests;

[Collection(PostgreSqlCollection.Name)]
public sealed class ApiSubscriptionEntitlementAdminTests(PostgreSqlFixture fixture) : IAsyncLifetime
{
    private SessionApiFactory _factory = null!;
    private HttpClient _admin = null!;
    private HttpClient _client = null!;

    public Task InitializeAsync()
    {
        _factory = new SessionApiFactory(fixture.ConnectionString);
        _admin = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
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

    private Task<(Guid UserId, string Username, string Password)> SeedUserAsync(string prefix) =>
        PlatformIntegrationTestUsers.CreatePlatformStaffWithPasswordAsync(_admin, prefix);

    private async Task<string> LoginAsync(string username, string password)
    {
        var login = await _client.PostAsJsonAsync(
            "/api/v1/platform/auth/login",
            new { usernameOrEmail = username, password });
        login.EnsureSuccessStatusCode();
        return (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("sessionToken").GetString()!;
    }

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

    private async Task<(Guid OrganizationId, string ProductCode, Guid PlanId, Guid VersionId, Guid TrialId)>
        SeedCatalogAsync(string prefix)
    {
        var org = await _admin.PostAsJsonAsync(
            "/api/v1/platform/organizations",
            new { displayName = $"{prefix} Org", slug = Unique(prefix) });
        org.EnsureSuccessStatusCode();
        var organizationId = (await org.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var productCode = Unique(prefix);
        (await _admin.PostAsJsonAsync(
            "/api/v1/platform/catalog/products",
            new { code = productCode, displayName = "Catalog Product" })).EnsureSuccessStatusCode();

        (await _admin.PostAsJsonAsync(
            $"/api/v1/platform/catalog/products/{productCode}/features",
            new
            {
                featureCode = FeatureCode.CustomerCreditView,
                displayName = "View Credit",
                valueType = nameof(FeatureValueType.Boolean)
            })).EnsureSuccessStatusCode();

        var plan = await _admin.PostAsJsonAsync(
            $"/api/v1/platform/catalog/products/{productCode}/plans",
            new { code = Unique("pln"), displayName = "Starter" });
        plan.EnsureSuccessStatusCode();
        var planId = (await plan.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        (await _admin.PostAsync(
            $"/api/v1/platform/catalog/products/{productCode}/plans/{planId}/activate",
            null)).EnsureSuccessStatusCode();

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
        var trialId = (await trial.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        return (organizationId, productCode, planId, versionId, trialId);
    }

    private async Task<Guid> CreateConfirmedPaymentAsync(Guid organizationId, string productCode, string prefix)
    {
        var now = DateTimeOffset.UtcNow;
        var create = await _admin.PostAsJsonAsync(
            "/api/v1/platform/payments/manual",
            new
            {
                organizationId,
                productCode,
                amount = 100m,
                currencyCode = "PHP",
                method = "GCash",
                externalReference = $"{prefix}-{Guid.NewGuid():N}",
                paidAtUtc = now
            });
        create.EnsureSuccessStatusCode();
        var paymentId = (await create.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        (await _admin.PostAsJsonAsync(
            $"/api/v1/platform/payments/{paymentId}/confirm",
            new { confirmedBy = "test-operator" })).EnsureSuccessStatusCode();
        return paymentId;
    }

    [Fact]
    public async Task Paid_subscription_list_search_lifecycle_concurrency_and_entitlement_snapshot()
    {
        var (organizationId, productCode, planId, versionId, trialId) = await SeedCatalogAsync("sea");
        _ = trialId;

        var now = DateTimeOffset.UtcNow;
        var paymentId = await CreateConfirmedPaymentAsync(organizationId, productCode, "sea-pay");
        var createPaid = await _admin.PostAsJsonAsync(
            $"/api/v1/platform/organizations/{organizationId}/subscriptions",
            new
            {
                planId,
                planVersionId = versionId,
                periodStartUtc = now,
                periodEndUtc = now.AddDays(30),
                paymentId
            });
        Assert.Equal(HttpStatusCode.Created, createPaid.StatusCode);
        var subscription = await createPaid.Content.ReadFromJsonAsync<JsonElement>();
        var subscriptionId = subscription.GetProperty("id").GetGuid();
        Assert.Equal("Active", subscription.GetProperty("status").GetString());
        var version = subscription.GetProperty("version").GetInt32();

        var list = await _admin.GetAsync(
            $"/api/v1/platform/subscriptions?organizationId={organizationId}&productCode={productCode}&search={productCode}&sortBy=ProductCode&isTrial=false");
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        Assert.Contains(
            (await list.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("items").EnumerateArray(),
            i => i.GetProperty("id").GetGuid() == subscriptionId);

        var conflictPaymentId = await CreateConfirmedPaymentAsync(organizationId, productCode, "sea-pay2");
        var conflictPaid = await _admin.PostAsJsonAsync(
            $"/api/v1/platform/organizations/{organizationId}/subscriptions",
            new
            {
                planId,
                planVersionId = versionId,
                periodStartUtc = now,
                periodEndUtc = now.AddDays(30),
                paymentId = conflictPaymentId
            });
        Assert.Equal(HttpStatusCode.Conflict, conflictPaid.StatusCode);

        var staleSuspend = await _admin.PostAsJsonAsync(
            $"/api/v1/platform/subscriptions/{subscriptionId}/suspend",
            new { expectedVersion = version - 1 });
        Assert.Equal(HttpStatusCode.Conflict, staleSuspend.StatusCode);

        var suspend = await _admin.PostAsJsonAsync(
            $"/api/v1/platform/subscriptions/{subscriptionId}/suspend",
            new { expectedVersion = version });
        Assert.Equal(HttpStatusCode.OK, suspend.StatusCode);
        Assert.Equal("Suspended", (await suspend.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("status").GetString());

        var snapshot = await _admin.PostAsJsonAsync(
            $"/api/v1/platform/organizations/{organizationId}/products/{productCode}/entitlements/snapshots",
            new { expectedNextVersion = (int?)null });
        Assert.Equal(HttpStatusCode.Created, snapshot.StatusCode);

        var audit = await _admin.GetAsync(
            "/api/v1/platform/audit?action=platform.subscription.paid_started&page=1&pageSize=20");
        Assert.Equal(HttpStatusCode.OK, audit.StatusCode);
        Assert.True((await audit.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("totalCount").GetInt32() >= 1);
    }

    [Fact]
    public async Task Inactive_plan_and_closed_org_cannot_receive_new_subscription()
    {
        var (organizationId, productCode, planId, versionId, _) = await SeedCatalogAsync("blk");
        (await _admin.PostAsync(
            $"/api/v1/platform/catalog/products/{productCode}/plans/{planId}/retire",
            null)).EnsureSuccessStatusCode();

        var now = DateTimeOffset.UtcNow;
        var retiredPaymentId = await CreateConfirmedPaymentAsync(organizationId, productCode, "blk-pay");
        var retiredPlan = await _admin.PostAsJsonAsync(
            $"/api/v1/platform/organizations/{organizationId}/subscriptions",
            new
            {
                planId,
                planVersionId = versionId,
                periodStartUtc = now,
                periodEndUtc = now.AddDays(30),
                paymentId = retiredPaymentId
            });
        Assert.Equal(HttpStatusCode.Conflict, retiredPlan.StatusCode);

        var (organizationId2, productCode2, planId2, versionId2, _) = await SeedCatalogAsync("clo");
        var closedPaymentId = await CreateConfirmedPaymentAsync(organizationId2, productCode2, "clo-pay");
        (await _admin.PostAsync($"/api/v1/platform/organizations/{organizationId2}/close", null))
            .EnsureSuccessStatusCode();
        var closedOrg = await _admin.PostAsJsonAsync(
            $"/api/v1/platform/organizations/{organizationId2}/subscriptions",
            new
            {
                planId = planId2,
                planVersionId = versionId2,
                periodStartUtc = now,
                periodEndUtc = now.AddDays(30),
                paymentId = closedPaymentId
            });
        Assert.Equal(HttpStatusCode.Conflict, closedOrg.StatusCode);
        _ = productCode2;
    }

    [Fact]
    public async Task Organization_admin_cannot_mutate_subscriptions_but_can_read_own_org()
    {
        var (organizationId, productCode, planId, versionId, trialId) = await SeedCatalogAsync("roa");
        var start = await _admin.PostAsJsonAsync(
            $"/api/v1/platform/organizations/{organizationId}/subscriptions/trials",
            new { planId, planVersionId = versionId, trialDefinitionId = trialId });
        start.EnsureSuccessStatusCode();

        var (userId, username, password) = await SeedUserAsync("roa");
        (await _admin.PostAsJsonAsync(
            $"/api/v1/platform/organizations/{organizationId}/members",
            new { userId, role = "OrganizationMember", reason = "integration-test-link" })).EnsureSuccessStatusCode();

        var token = await LoginAsync(username, password);
        using (var select = Authed(
                   HttpMethod.Put,
                   "/api/v1/platform/auth/organization-context",
                   token,
                   new { organizationId }))
        {
            (await _client.SendAsync(select)).EnsureSuccessStatusCode();
        }

        using (var listAll = Authed(HttpMethod.Get, "/api/v1/platform/subscriptions", token))
        {
            Assert.Equal(HttpStatusCode.Forbidden, (await _client.SendAsync(listAll)).StatusCode);
        }

        using (var listOwn = Authed(
                   HttpMethod.Get,
                   $"/api/v1/platform/organizations/{organizationId}/subscriptions",
                   token))
        {
            Assert.Equal(HttpStatusCode.OK, (await _client.SendAsync(listOwn)).StatusCode);
        }

        using (var mutate = Authed(
                   HttpMethod.Post,
                   $"/api/v1/platform/organizations/{organizationId}/subscriptions/trials",
                   token,
                   new { planId, planVersionId = versionId, trialDefinitionId = trialId }))
        {
            Assert.Equal(HttpStatusCode.Forbidden, (await _client.SendAsync(mutate)).StatusCode);
        }

        using (var snapshot = Authed(
                   HttpMethod.Post,
                   $"/api/v1/platform/organizations/{organizationId}/products/{productCode}/entitlements/snapshots",
                   token,
                   new { }))
        {
            Assert.Equal(HttpStatusCode.Forbidden, (await _client.SendAsync(snapshot)).StatusCode);
        }
    }

    [Fact]
    public async Task Feature_override_duplicate_active_conflict_and_revoke()
    {
        var (organizationId, productCode, _, _, _) = await SeedCatalogAsync("ovd");
        var (userId, _, _) = await SeedUserAsync("ovd");

        var create = await _admin.PostAsJsonAsync(
            $"/api/v1/platform/organizations/{organizationId}/products/{productCode}/feature-overrides",
            new
            {
                featureCode = FeatureCode.CustomerCreditView,
                enabled = false,
                reason = "temp disable",
                createdByUserId = userId
            });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var overrideId = (await create.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var duplicate = await _admin.PostAsJsonAsync(
            $"/api/v1/platform/organizations/{organizationId}/products/{productCode}/feature-overrides",
            new
            {
                featureCode = FeatureCode.CustomerCreditView,
                enabled = true,
                reason = "again",
                createdByUserId = userId
            });
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);

        var revoke = await _admin.PostAsJsonAsync(
            $"/api/v1/platform/feature-overrides/{overrideId}/revoke",
            new { reason = "done", revokedByUserId = userId });
        Assert.Equal(HttpStatusCode.OK, revoke.StatusCode);
    }
}
