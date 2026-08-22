using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Payments;
using ExItS.Platform.Domain.Audit;
using ExItS.Platform.Domain.Catalog;
using ExItS.Platform.Domain.Subscriptions;
using ExItS.Platform.Infrastructure.Authorization;
using ExItS.Platform.IntegrationTests.Support;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace ExItS.Platform.IntegrationTests;

[Collection(PostgreSqlCollection.Name)]
public sealed class ApiEntitlementTests(PostgreSqlFixture fixture) : IAsyncLifetime
{
    private EntitlementApiFactory _factory = null!;
    private HttpClient _client = null!;
    private Guid? _overrideOperatorUserId;

    public Task InitializeAsync()
    {
        _factory = new EntitlementApiFactory(fixture.ConnectionString);
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

    private const decimal SeedPlanMonthlyPrice = 499m;

    private async Task<Guid> GetOverrideOperatorUserIdAsync()
    {
        if (_overrideOperatorUserId is null)
        {
            var (userId, _, _) = await PlatformIntegrationTestUsers.CreatePlatformStaffWithPasswordAsync(
                _client,
                "ent-override-op",
                "PlatformAdministrator");
            _overrideOperatorUserId = userId;
        }

        return _overrideOperatorUserId.Value;
    }

    private async Task<HttpResponseMessage> PostAsOverrideOperatorAsync(string url, object? body = null)
    {
        var operatorId = await GetOverrideOperatorUserIdAsync();
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        request.Headers.Add(
            DevelopmentPlatformActorAccessor.DevPlatformUserIdHeader,
            operatorId.ToString("D"));
        return await _client.SendAsync(request);
    }

    private async Task AssignPlatformRoleAsync(Guid platformUserId, string role, Guid? organizationId = null)
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/platform/authorization/assignments",
            new { platformUserId, role, organizationId });
        response.EnsureSuccessStatusCode();
    }

    private static bool AuditItemMatches(
        JsonElement item,
        string action,
        string outcome,
        Guid? organizationId,
        string? productCode,
        Guid? actorUserId)
    {
        if (!string.Equals(item.GetProperty("actionCode").GetString(), action, StringComparison.Ordinal))
        {
            return false;
        }

        if (!string.Equals(item.GetProperty("outcome").GetString(), outcome, StringComparison.Ordinal))
        {
            return false;
        }

        if (organizationId is not null
            && item.TryGetProperty("organizationId", out var orgProp)
            && orgProp.ValueKind != JsonValueKind.Null
            && orgProp.GetGuid() != organizationId)
        {
            return false;
        }

        if (productCode is not null
            && item.TryGetProperty("productCode", out var productProp)
            && productProp.ValueKind != JsonValueKind.Null
            && !string.Equals(productProp.GetString(), productCode, StringComparison.Ordinal))
        {
            return false;
        }

        if (actorUserId is not null)
        {
            var actor = item.GetProperty("actorIdentifier").GetString();
            if (!string.Equals(actor, $"platform-user:{actorUserId:D}", StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    private async Task<Guid> CreateOrganizationAsync(string prefix)
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/platform/organizations",
            new { displayName = $"{prefix} Org", slug = Unique(prefix) });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
    }

    private async Task<(Guid organizationId, Guid subscriptionId, string productCode)>
        SeedActiveSubscriptionAsync(string prefix)
    {
        var organizationId = await CreateOrganizationAsync(prefix);
        var candidate = Unique(prefix);
        var productCode = candidate[..Math.Min(30, candidate.Length)];

        var product = await _client.PostAsJsonAsync(
            "/api/v1/platform/catalog/products", new { code = productCode, displayName = "POS" });
        product.EnsureSuccessStatusCode();

        foreach (var (code, name) in new[]
                 {
                     (FeatureCode.CustomerCreditView, "View Credit"),
                     (FeatureCode.CustomerCreditCreate, "Create Credit")
                 })
        {
            var feature = await _client.PostAsJsonAsync(
                $"/api/v1/platform/catalog/products/{productCode}/features",
                new { featureCode = code, displayName = name, valueType = nameof(FeatureValueType.Boolean) });
            feature.EnsureSuccessStatusCode();
        }

        var plan = await _client.PostAsJsonAsync(
            $"/api/v1/platform/catalog/products/{productCode}/plans",
            new
            {
                code = "utang",
                displayName = "Utang",
                monthlyPrice = SeedPlanMonthlyPrice,
                currencyCode = "PHP"
            });
        plan.EnsureSuccessStatusCode();
        var planId = (await plan.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var activatePlan = await _client.PostAsync(
            $"/api/v1/platform/catalog/products/{productCode}/plans/{planId}/activate", null);
        activatePlan.EnsureSuccessStatusCode();

        var grants = new[]
        {
            new { featureCode = FeatureCode.CustomerCreditView, enabled = true },
            new { featureCode = FeatureCode.CustomerCreditCreate, enabled = true }
        };

        var draft = await _client.PostAsJsonAsync(
            $"/api/v1/platform/catalog/products/{productCode}/plans/{planId}/versions/draft",
            new { versionNumber = 1, billingPeriod = nameof(BillingPeriod.Monthly), trialEligible = true, grants });
        draft.EnsureSuccessStatusCode();
        var versionId = (await draft.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var publish = await _client.PostAsync(
            $"/api/v1/platform/catalog/products/{productCode}/plans/{planId}/versions/1/publish", null);
        publish.EnsureSuccessStatusCode();

        var trial = await _client.PostAsJsonAsync(
            $"/api/v1/platform/catalog/products/{productCode}/trials",
            new
            {
                displayName = "Trial",
                durationTicks = TimeSpan.FromDays(21).Ticks,
                featureGrants = grants,
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
        var (_, periodEndUtc) = SubscriptionBillingPeriods.ComputePaidPeriod(now, BillingCycle.Monthly);
        var payment = await _client.PostAsJsonAsync(
            "/api/v1/platform/payments/manual",
            new
            {
                organizationId,
                productCode,
                amount = SeedPlanMonthlyPrice,
                currencyCode = "PHP",
                method = "GCash",
                externalReference = $"ent-act-{Guid.NewGuid():N}",
                paidAtUtc = now
            });
        payment.EnsureSuccessStatusCode();
        var paymentId = (await payment.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var activate = await _client.PostAsJsonAsync(
            $"/api/v1/platform/payments/{paymentId}/activate-subscription",
            new
            {
                confirmedBy = "entitlement-operator",
                subscriptionId,
                periodStartUtc = now,
                periodEndUtc
            });
        activate.EnsureSuccessStatusCode();

        return (organizationId, subscriptionId, productCode);
    }

    private async Task<(Guid organizationId, Guid subscriptionId, string productCode)>
        SeedTrialingSubscriptionAsync(string prefix)
    {
        var organizationId = await CreateOrganizationAsync(prefix);
        var candidate = Unique(prefix);
        var productCode = candidate[..Math.Min(30, candidate.Length)];

        var product = await _client.PostAsJsonAsync(
            "/api/v1/platform/catalog/products", new { code = productCode, displayName = "POS" });
        product.EnsureSuccessStatusCode();

        foreach (var (code, name) in new[]
                 {
                     (FeatureCode.CustomerCreditView, "View Credit"),
                     (FeatureCode.CustomerCreditCreate, "Create Credit")
                 })
        {
            var feature = await _client.PostAsJsonAsync(
                $"/api/v1/platform/catalog/products/{productCode}/features",
                new { featureCode = code, displayName = name, valueType = nameof(FeatureValueType.Boolean) });
            feature.EnsureSuccessStatusCode();
        }

        var plan = await _client.PostAsJsonAsync(
            $"/api/v1/platform/catalog/products/{productCode}/plans",
            new
            {
                code = "utang",
                displayName = "Utang",
                monthlyPrice = SeedPlanMonthlyPrice,
                currencyCode = "PHP"
            });
        plan.EnsureSuccessStatusCode();
        var planId = (await plan.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var activatePlan = await _client.PostAsync(
            $"/api/v1/platform/catalog/products/{productCode}/plans/{planId}/activate", null);
        activatePlan.EnsureSuccessStatusCode();

        var grants = new[]
        {
            new { featureCode = FeatureCode.CustomerCreditView, enabled = true },
            new { featureCode = FeatureCode.CustomerCreditCreate, enabled = true }
        };

        var draft = await _client.PostAsJsonAsync(
            $"/api/v1/platform/catalog/products/{productCode}/plans/{planId}/versions/draft",
            new { versionNumber = 1, billingPeriod = nameof(BillingPeriod.Monthly), trialEligible = true, grants });
        draft.EnsureSuccessStatusCode();
        var versionId = (await draft.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var publish = await _client.PostAsync(
            $"/api/v1/platform/catalog/products/{productCode}/plans/{planId}/versions/1/publish", null);
        publish.EnsureSuccessStatusCode();

        var trial = await _client.PostAsJsonAsync(
            $"/api/v1/platform/catalog/products/{productCode}/trials",
            new
            {
                displayName = "Trial",
                durationTicks = TimeSpan.FromDays(21).Ticks,
                featureGrants = grants,
                postExpiryFeatureGrants = Array.Empty<object>()
            });
        trial.EnsureSuccessStatusCode();
        var trialId = (await trial.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var start = await _client.PostAsJsonAsync(
            $"/api/v1/platform/organizations/{organizationId}/subscriptions/trials",
            new { planId, planVersionId = versionId, trialDefinitionId = trialId });
        start.EnsureSuccessStatusCode();
        var subscriptionId = (await start.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        return (organizationId, subscriptionId, productCode);
    }

    [Fact]
    public async Task Create_list_and_revoke_feature_override_via_api()
    {
        var (organizationId, _, productCode) = await SeedActiveSubscriptionAsync("api-override");

        var operatorUserId = await GetOverrideOperatorUserIdAsync();
        var created = await PostAsOverrideOperatorAsync(
            $"/api/v1/platform/organizations/{organizationId}/products/{productCode}/feature-overrides",
            new
            {
                featureCode = FeatureCode.CustomerCreditCreate,
                enabled = false,
                reason = "Compliance hold",
                numericLimit = (int?)null,
                expiresAtUtc = (DateTimeOffset?)null
            });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var createdBody = await created.Content.ReadFromJsonAsync<JsonElement>();
        var overrideId = createdBody.GetProperty("id").GetGuid();
        Assert.Equal("Active", createdBody.GetProperty("status").GetString());
        Assert.Equal(operatorUserId, createdBody.GetProperty("createdByUserId").GetGuid());

        var byId = await _client.GetAsync($"/api/v1/platform/feature-overrides/{overrideId}");
        Assert.Equal(HttpStatusCode.OK, byId.StatusCode);

        var list = await _client.GetAsync(
            $"/api/v1/platform/organizations/{organizationId}/products/{productCode}/feature-overrides");
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        var listBody = await list.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, listBody.GetProperty("totalCount").GetInt32());

        var revoke = await PostAsOverrideOperatorAsync(
            $"/api/v1/platform/feature-overrides/{overrideId}/revoke",
            new { reason = "No longer required" });
        Assert.Equal(HttpStatusCode.OK, revoke.StatusCode);
        var revokedBody = await revoke.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Revoked", revokedBody.GetProperty("status").GetString());
        Assert.Equal(operatorUserId, revokedBody.GetProperty("revokedByUserId").GetGuid());

        var missing = await _client.GetAsync($"/api/v1/platform/feature-overrides/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    [Fact]
    public async Task Creating_a_second_active_override_for_the_same_feature_returns_409()
    {
        var (organizationId, _, productCode) = await SeedActiveSubscriptionAsync("api-override-conflict");

        var body = new
        {
            featureCode = FeatureCode.CustomerCreditCreate,
            enabled = false,
            reason = "Support hold",
            numericLimit = (int?)null,
            expiresAtUtc = (DateTimeOffset?)null
        };
        var first = await PostAsOverrideOperatorAsync(
            $"/api/v1/platform/organizations/{organizationId}/products/{productCode}/feature-overrides", body);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var second = await PostAsOverrideOperatorAsync(
            $"/api/v1/platform/organizations/{organizationId}/products/{productCode}/feature-overrides", body);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Generate_snapshot_get_latest_and_history_via_api()
    {
        var (organizationId, subscriptionId, productCode) = await SeedActiveSubscriptionAsync("api-snapshot");

        var generated = await _client.PostAsJsonAsync(
            $"/api/v1/platform/organizations/{organizationId}/products/{productCode}/entitlements/snapshots",
            new { });
        Assert.Equal(HttpStatusCode.Created, generated.StatusCode);
        var generatedBody = await generated.Content.ReadFromJsonAsync<JsonElement>();
        var snapshotId = generatedBody.GetProperty("id").GetGuid();
        Assert.Equal(2, generatedBody.GetProperty("snapshotVersion").GetInt32());
        Assert.Equal(subscriptionId, generatedBody.GetProperty("subscriptionId").GetGuid());

        var byId = await _client.GetAsync($"/api/v1/platform/entitlements/snapshots/{snapshotId}");
        Assert.Equal(HttpStatusCode.OK, byId.StatusCode);

        var latest = await _client.GetAsync(
            $"/api/v1/platform/organizations/{organizationId}/products/{productCode}/entitlements/snapshots/latest");
        Assert.Equal(HttpStatusCode.OK, latest.StatusCode);
        var latestBody = await latest.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(snapshotId, latestBody.GetProperty("id").GetGuid());

        var secondGeneration = await _client.PostAsJsonAsync(
            $"/api/v1/platform/organizations/{organizationId}/products/{productCode}/entitlements/snapshots",
            new { });
        Assert.Equal(HttpStatusCode.Created, secondGeneration.StatusCode);

        var history = await _client.GetAsync(
            $"/api/v1/platform/organizations/{organizationId}/products/{productCode}/entitlements/snapshots");
        Assert.Equal(HttpStatusCode.OK, history.StatusCode);
        var historyBody = await history.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(3, historyBody.GetProperty("totalCount").GetInt32());

        var missing = await _client.GetAsync($"/api/v1/platform/entitlements/snapshots/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    [Fact]
    public async Task Generating_a_snapshot_with_a_stale_expected_version_returns_409()
    {
        var (organizationId, _, productCode) = await SeedActiveSubscriptionAsync("api-snapshot-conflict");

        var first = await _client.PostAsJsonAsync(
            $"/api/v1/platform/organizations/{organizationId}/products/{productCode}/entitlements/snapshots",
            new { expectedNextVersion = 2 });
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var conflict = await _client.PostAsJsonAsync(
            $"/api/v1/platform/organizations/{organizationId}/products/{productCode}/entitlements/snapshots",
            new { expectedNextVersion = 2 });
        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
    }

    [Fact]
    public async Task Reconcile_creates_a_new_snapshot_version_via_api()
    {
        var (organizationId, _, productCode) = await SeedActiveSubscriptionAsync("api-reconcile");

        var initial = await _client.PostAsJsonAsync(
            $"/api/v1/platform/organizations/{organizationId}/products/{productCode}/entitlements/snapshots",
            new { });
        Assert.Equal(HttpStatusCode.Created, initial.StatusCode);

        var reconciled = await _client.PostAsJsonAsync(
            $"/api/v1/platform/organizations/{organizationId}/products/{productCode}/entitlements/reconcile",
            new { reason = "manual correction" });
        Assert.Equal(HttpStatusCode.Created, reconciled.StatusCode);
        var reconciledBody = await reconciled.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(3, reconciledBody.GetProperty("snapshotVersion").GetInt32());
    }

    [Fact]
    public async Task PastDue_subscription_snapshot_disables_customer_credit_create_but_keeps_view()
    {
        var (organizationId, subscriptionId, productCode) = await SeedActiveSubscriptionAsync("api-past-due-snapshot");

        var gracePeriod = await _client.PostAsJsonAsync(
            $"/api/v1/platform/subscriptions/{subscriptionId}/grace-period",
            new { gracePeriodEndUtc = DateTimeOffset.UtcNow.AddDays(37) });
        gracePeriod.EnsureSuccessStatusCode();

        var pastDue = await _client.PostAsync($"/api/v1/platform/subscriptions/{subscriptionId}/past-due", null);
        pastDue.EnsureSuccessStatusCode();

        var generated = await _client.PostAsJsonAsync(
            $"/api/v1/platform/organizations/{organizationId}/products/{productCode}/entitlements/snapshots",
            new { });
        Assert.Equal(HttpStatusCode.Created, generated.StatusCode);
        var body = await generated.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("PastDue", body.GetProperty("subscriptionStatus").GetString());

        var grants = body.GetProperty("grants").EnumerateArray().ToArray();
        var create = grants.Single(g => g.GetProperty("featureCode").GetString() == FeatureCode.CustomerCreditCreate);
        var view = grants.Single(g => g.GetProperty("featureCode").GetString() == FeatureCode.CustomerCreditView);
        Assert.False(create.GetProperty("enabled").GetBoolean());
        Assert.True(view.GetProperty("enabled").GetBoolean());
    }

    [Fact]
    public async Task Listing_feature_overrides_by_status_filters_correctly()
    {
        var (organizationId, _, productCode) = await SeedActiveSubscriptionAsync("api-override-status-filter");

        var created = await PostAsOverrideOperatorAsync(
            $"/api/v1/platform/organizations/{organizationId}/products/{productCode}/feature-overrides",
            new
            {
                featureCode = FeatureCode.CustomerCreditCreate,
                enabled = false,
                reason = "Compliance hold",
                numericLimit = (int?)null,
                expiresAtUtc = (DateTimeOffset?)null
            });
        created.EnsureSuccessStatusCode();
        var overrideId = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var activeList = await _client.GetAsync(
            $"/api/v1/platform/organizations/{organizationId}/products/{productCode}/feature-overrides?status=Active");
        var activeBody = await activeList.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, activeBody.GetProperty("totalCount").GetInt32());

        var revokedListBeforeRevoke = await _client.GetAsync(
            $"/api/v1/platform/organizations/{organizationId}/products/{productCode}/feature-overrides?status=Revoked");
        var revokedBodyBefore = await revokedListBeforeRevoke.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(0, revokedBodyBefore.GetProperty("totalCount").GetInt32());

        var revoke = await PostAsOverrideOperatorAsync(
            $"/api/v1/platform/feature-overrides/{overrideId}/revoke",
            new { reason = "No longer needed" });
        revoke.EnsureSuccessStatusCode();

        var revokedListAfterRevoke = await _client.GetAsync(
            $"/api/v1/platform/organizations/{organizationId}/products/{productCode}/feature-overrides?status=Revoked");
        var revokedBodyAfter = await revokedListAfterRevoke.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, revokedBodyAfter.GetProperty("totalCount").GetInt32());
    }

    [Fact]
    public async Task Revoking_an_unknown_feature_override_returns_404()
    {
        var revoke = await PostAsOverrideOperatorAsync(
            $"/api/v1/platform/feature-overrides/{Guid.NewGuid()}/revoke",
            new { reason = "does not matter" });
        Assert.Equal(HttpStatusCode.NotFound, revoke.StatusCode);
    }

    [Fact]
    public async Task Getting_latest_snapshot_before_any_snapshot_exists_returns_404()
    {
        var (organizationId, _, productCode) = await SeedTrialingSubscriptionAsync("api-no-snapshot-yet");

        var latest = await _client.GetAsync(
            $"/api/v1/platform/organizations/{organizationId}/products/{productCode}/entitlements/snapshots/latest");
        Assert.Equal(HttpStatusCode.NotFound, latest.StatusCode);
    }

    [Fact]
    public async Task Snapshot_history_supports_pagination()
    {
        var (organizationId, _, productCode) = await SeedActiveSubscriptionAsync("api-snapshot-pagination");

        for (var i = 0; i < 3; i++)
        {
            var generated = await _client.PostAsJsonAsync(
                $"/api/v1/platform/organizations/{organizationId}/products/{productCode}/entitlements/snapshots",
                new { });
            generated.EnsureSuccessStatusCode();
        }

        var page1 = await _client.GetAsync(
            $"/api/v1/platform/organizations/{organizationId}/products/{productCode}/entitlements/snapshots?page=1&pageSize=2");
        var page1Body = await page1.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(4, page1Body.GetProperty("totalCount").GetInt32());
        Assert.Equal(2, page1Body.GetProperty("items").GetArrayLength());

        var page2 = await _client.GetAsync(
            $"/api/v1/platform/organizations/{organizationId}/products/{productCode}/entitlements/snapshots?page=2&pageSize=2");
        var page2Body = await page2.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(2, page2Body.GetProperty("items").GetArrayLength());
    }

    [Fact]
    public async Task No_delivery_or_broker_routes_are_exposed_by_entitlement_endpoints()
    {
        var (organizationId, _, productCode) = await SeedActiveSubscriptionAsync("api-no-delivery");

        foreach (var path in new[]
                 {
                     $"/api/v1/platform/organizations/{organizationId}/products/{productCode}/entitlements/deliver",
                     $"/api/v1/platform/organizations/{organizationId}/products/{productCode}/entitlements/publish",
                     "/api/v1/platform/entitlements/broker",
                     "/api/v1/other-product/entitlements"
                 })
        {
            var response = await _client.GetAsync(path);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }

    [Fact]
    public async Task Feature_override_create_and_revoke_record_succeeded_audit_with_operator_actor()
    {
        var (organizationId, _, productCode) = await SeedActiveSubscriptionAsync("audit-override");
        var operatorUserId = await GetOverrideOperatorUserIdAsync();

        var created = await PostAsOverrideOperatorAsync(
            $"/api/v1/platform/organizations/{organizationId}/products/{productCode}/feature-overrides",
            new
            {
                featureCode = FeatureCode.CustomerCreditCreate,
                enabled = false,
                reason = "Audit create hold",
            });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var overrideId = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var createAudit = await _client.GetAsync(
            $"/api/v1/platform/audit?action={PlatformAuditActions.FeatureOverrideCreated}&outcome=Succeeded&organizationId={organizationId:D}&pageSize=20");
        createAudit.EnsureSuccessStatusCode();
        var createItems = (await createAudit.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("items");
        Assert.Contains(
            createItems.EnumerateArray(),
            item => AuditItemMatches(
                item,
                PlatformAuditActions.FeatureOverrideCreated,
                "Succeeded",
                organizationId,
                productCode,
                operatorUserId));

        var revoke = await PostAsOverrideOperatorAsync(
            $"/api/v1/platform/feature-overrides/{overrideId}/revoke",
            new { reason = "Audit revoke done" });
        Assert.Equal(HttpStatusCode.OK, revoke.StatusCode);

        var revokeAudit = await _client.GetAsync(
            $"/api/v1/platform/audit?action={PlatformAuditActions.FeatureOverrideRevoked}&outcome=Succeeded&organizationId={organizationId:D}&pageSize=20");
        revokeAudit.EnsureSuccessStatusCode();
        var revokeItems = (await revokeAudit.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("items");
        Assert.Contains(
            revokeItems.EnumerateArray(),
            item => AuditItemMatches(
                item,
                PlatformAuditActions.FeatureOverrideRevoked,
                "Succeeded",
                organizationId,
                productCode,
                operatorUserId));

        var persisted = await _client.GetAsync($"/api/v1/platform/feature-overrides/{overrideId}");
        persisted.EnsureSuccessStatusCode();
        var persistedBody = await persisted.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Revoked", persistedBody.GetProperty("status").GetString());
        Assert.Equal("Audit revoke done", persistedBody.GetProperty("revocationReason").GetString());
    }

    [Fact]
    public async Task Feature_override_create_without_manage_entitlement_permission_is_denied_and_audited()
    {
        var (organizationId, _, productCode) = await SeedActiveSubscriptionAsync("deny-override");
        var (billingUserId, _, _) = await PlatformIntegrationTestUsers.CreatePlatformStaffWithPasswordAsync(
            _client,
            "bill-override",
            "BillingAdministrator");

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/v1/platform/organizations/{organizationId}/products/{productCode}/feature-overrides")
        {
            Content = JsonContent.Create(new
            {
                featureCode = FeatureCode.CustomerCreditCreate,
                enabled = false,
                reason = "Should be denied",
            })
        };
        request.Headers.Add(
            DevelopmentPlatformActorAccessor.DevPlatformUserIdHeader,
            billingUserId.ToString("D"));
        var denied = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);

        var deniedAudit = await _client.GetAsync(
            $"/api/v1/platform/audit?actor={Uri.EscapeDataString($"platform-user:{billingUserId:D}")}&action={PlatformAuditActions.FeatureOverrideCreated}&outcome=Denied&pageSize=20");
        deniedAudit.EnsureSuccessStatusCode();
        Assert.True((await deniedAudit.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("totalCount").GetInt32() >= 1);

        var successAudit = await _client.GetAsync(
            $"/api/v1/platform/audit?actor={Uri.EscapeDataString($"platform-user:{billingUserId:D}")}&action={PlatformAuditActions.FeatureOverrideCreated}&outcome=Succeeded&pageSize=20");
        successAudit.EnsureSuccessStatusCode();
        Assert.Equal(0, (await successAudit.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("totalCount").GetInt32());
    }

    [Fact]
    public async Task Feature_override_rejects_feature_not_defined_on_selected_product()
    {
        var (organizationId, _, productCode) = await SeedTrialingSubscriptionAsync("cross-product-feature");
        var operatorUserId = await GetOverrideOperatorUserIdAsync();

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/v1/platform/organizations/{organizationId}/products/{productCode}/feature-overrides")
        {
            Content = JsonContent.Create(new
            {
                featureCode = FeatureCode.StoreCatalogView,
                enabled = false,
                reason = "Feature not on product catalog",
            })
        };
        request.Headers.Add(
            DevelopmentPlatformActorAccessor.DevPlatformUserIdHeader,
            operatorUserId.ToString("D"));
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(ApplicationErrorCodes.FeatureNotFound, body.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task Feature_override_create_on_foreign_organization_is_denied_for_org_scoped_operator()
    {
        var (organizationId1, _, productCode1) = await SeedTrialingSubscriptionAsync("scoped-org1");
        var (organizationId2, _, productCode2) = await SeedTrialingSubscriptionAsync("scoped-org2");
        var (adminUserId, _, _) = await PlatformIntegrationTestUsers.CreatePlatformStaffWithPasswordAsync(
            _client,
            "scoped-override",
            "PlatformSupport");
        await AssignPlatformRoleAsync(adminUserId, "PlatformAdministrator", organizationId1);

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/v1/platform/organizations/{organizationId2}/products/{productCode2}/feature-overrides")
        {
            Content = JsonContent.Create(new
            {
                featureCode = FeatureCode.CustomerCreditCreate,
                enabled = false,
                reason = "Cross org attempt",
            })
        };
        request.Headers.Add(
            DevelopmentPlatformActorAccessor.DevPlatformUserIdHeader,
            adminUserId.ToString("D"));
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        _ = organizationId1;
        _ = productCode1;
    }

    [Fact]
    public async Task Entitlement_snapshot_generate_and_reconcile_record_succeeded_audit()
    {
        var (organizationId, _, productCode) = await SeedTrialingSubscriptionAsync("audit-snapshot");

        var generated = await _client.PostAsJsonAsync(
            $"/api/v1/platform/organizations/{organizationId}/products/{productCode}/entitlements/snapshots",
            new { });
        Assert.Equal(HttpStatusCode.Created, generated.StatusCode);

        var generateAudit = await _client.GetAsync(
            $"/api/v1/platform/audit?action={PlatformAuditActions.EntitlementSnapshotGenerated}&outcome=Succeeded&organizationId={organizationId:D}&pageSize=20");
        generateAudit.EnsureSuccessStatusCode();
        Assert.True((await generateAudit.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("totalCount").GetInt32() >= 1);

        var reconciled = await _client.PostAsJsonAsync(
            $"/api/v1/platform/organizations/{organizationId}/products/{productCode}/entitlements/reconcile",
            new { reason = "manual correction" });
        Assert.Equal(HttpStatusCode.Created, reconciled.StatusCode);

        var reconcileAudit = await _client.GetAsync(
            $"/api/v1/platform/audit?action={PlatformAuditActions.EntitlementSnapshotReconciled}&outcome=Succeeded&organizationId={organizationId:D}&pageSize=20");
        reconcileAudit.EnsureSuccessStatusCode();
        var reconcileItems = (await reconcileAudit.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("items");
        Assert.Contains(
            reconcileItems.EnumerateArray(),
            item => item.TryGetProperty("reason", out var reasonProp)
                    && string.Equals(reasonProp.GetString(), "manual correction", StringComparison.Ordinal));
    }

    private sealed class EntitlementApiFactory(string connectionString) : WebApplicationFactory<Program>
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
