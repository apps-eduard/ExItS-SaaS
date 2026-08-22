using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ExItS.Platform.Application.Payments;
using ExItS.Platform.Domain.Catalog;
using ExItS.Platform.Domain.Subscriptions;
using ExItS.Platform.Infrastructure.Authorization;
using ExItS.Platform.IntegrationTests.Support;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace ExItS.Platform.IntegrationTests;

/// <summary>
/// P3-WP05 closeout: deterministic end-to-end commercial lifecycle across catalog,
/// trial/subscription, manual SaaS payment, overrides, and entitlement snapshots.
/// </summary>
[Collection(PostgreSqlCollection.Name)]
public sealed class Phase3CommercialCloseoutTests(PostgreSqlFixture fixture) : IAsyncLifetime
{
    private CloseoutApiFactory _factory = null!;
    private HttpClient _client = null!;

    public Task InitializeAsync()
    {
        _factory = new CloseoutApiFactory(fixture.ConnectionString);
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

    private const decimal CloseoutPlanMonthlyPrice = 499m;

    [Fact]
    public async Task Full_phase3_commercial_lifecycle_scenario()
    {
        var candidate = Unique("p3close");
        var productCode = candidate[..Math.Min(28, candidate.Length)];
        var slug = Unique("close-org");
        if (slug.Length > 40)
        {
            slug = slug[..40];
        }

        // 1–3. Catalog: product, features, plan, published version, trial
        var product = await _client.PostAsJsonAsync(
            "/api/v1/platform/catalog/products",
            new { code = productCode, displayName = "Closeout POS" });
        if (!product.IsSuccessStatusCode)
        {
            var detail = await product.Content.ReadAsStringAsync();
            Assert.Fail($"Product create failed ({(int)product.StatusCode}): {detail}");
        }
        var productId = (await product.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        (await _client.PostAsync($"/api/v1/platform/catalog/products/{productId}/activate", null))
            .EnsureSuccessStatusCode();

        foreach (var (code, name) in new[]
                 {
                     (FeatureCode.CustomerCreditView, "View"),
                     (FeatureCode.CustomerCreditRepay, "Repay"),
                     (FeatureCode.CustomerCreditCreate, "Create")
                 })
        {
            (await _client.PostAsJsonAsync(
                $"/api/v1/platform/catalog/products/{productCode}/features",
                new { featureCode = code, displayName = name, valueType = nameof(FeatureValueType.Boolean) }))
                .EnsureSuccessStatusCode();
        }

        var plan = await _client.PostAsJsonAsync(
            $"/api/v1/platform/catalog/products/{productCode}/plans",
            new
            {
                code = "utang",
                displayName = "Utang",
                monthlyPrice = CloseoutPlanMonthlyPrice,
                currencyCode = "PHP"
            });
        plan.EnsureSuccessStatusCode();
        var planId = (await plan.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        (await _client.PostAsync(
            $"/api/v1/platform/catalog/products/{productCode}/plans/{planId}/activate", null))
            .EnsureSuccessStatusCode();

        var grants = new[]
        {
            new { featureCode = FeatureCode.CustomerCreditView, enabled = true },
            new { featureCode = FeatureCode.CustomerCreditRepay, enabled = true },
            new { featureCode = FeatureCode.CustomerCreditCreate, enabled = true }
        };
        var postExpiry = new[]
        {
            new { featureCode = FeatureCode.CustomerCreditView, enabled = true },
            new { featureCode = FeatureCode.CustomerCreditRepay, enabled = true },
            new { featureCode = FeatureCode.CustomerCreditCreate, enabled = false }
        };

        (await _client.PostAsJsonAsync(
            $"/api/v1/platform/catalog/products/{productCode}/plans/{planId}/versions/draft",
            new
            {
                versionNumber = 1,
                billingPeriod = nameof(BillingPeriod.Monthly),
                trialEligible = true,
                grants
            })).EnsureSuccessStatusCode();
        (await _client.PostAsync(
            $"/api/v1/platform/catalog/products/{productCode}/plans/{planId}/versions/1/publish", null))
            .EnsureSuccessStatusCode();
        var versionList = await _client.GetAsync(
            $"/api/v1/platform/catalog/products/{productCode}/plans/{planId}/versions");
        versionList.EnsureSuccessStatusCode();
        var versions = await versionList.Content.ReadFromJsonAsync<JsonElement>();
        var planVersionId = versions.EnumerateArray().First().GetProperty("id").GetGuid();

        var trial = await _client.PostAsJsonAsync(
            $"/api/v1/platform/catalog/products/{productCode}/trials",
            new
            {
                displayName = "Utang Trial",
                durationTicks = TimeSpan.FromDays(21).Ticks,
                planId,
                featureGrants = grants,
                postExpiryFeatureGrants = postExpiry
            });
        trial.EnsureSuccessStatusCode();
        var trialId = (await trial.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        // 4–5. Organization + trial subscription
        var org = await _client.PostAsJsonAsync(
            "/api/v1/platform/organizations",
            new { displayName = "Closeout Org", slug });
        org.EnsureSuccessStatusCode();
        var organizationId = (await org.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var startTrial = await _client.PostAsJsonAsync(
            $"/api/v1/platform/organizations/{organizationId}/subscriptions/trials",
            new { planId, planVersionId, trialDefinitionId = trialId });
        startTrial.EnsureSuccessStatusCode();
        var startTrialBody = await startTrial.Content.ReadFromJsonAsync<JsonElement>();
        var subscriptionId = startTrialBody.GetProperty("id").GetGuid();
        Assert.Equal("Trialing", startTrialBody.GetProperty("status").GetString());

        // 6. Trial entitlement snapshot
        var trialSnap = await _client.PostAsJsonAsync(
            $"/api/v1/platform/organizations/{organizationId}/products/{productCode}/entitlements/snapshots",
            new { });
        Assert.Equal(HttpStatusCode.Created, trialSnap.StatusCode);
        var trialSnapBody = await trialSnap.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, trialSnapBody.GetProperty("snapshotVersion").GetInt32());
        Assert.Equal("Trialing", trialSnapBody.GetProperty("subscriptionStatus").GetString());

        // 7–8. Manual SaaS payment + confirm/activate
        var paidAt = DateTimeOffset.UtcNow;
        var payment = await _client.PostAsJsonAsync(
            "/api/v1/platform/payments/manual",
            new
            {
                organizationId,
                productCode,
                amount = CloseoutPlanMonthlyPrice,
                currencyCode = "PHP",
                method = "GCash",
                externalReference = $"CLOSEOUT-{Guid.NewGuid():N}",
                paidAtUtc = paidAt
            });
        Assert.Equal(HttpStatusCode.Created, payment.StatusCode);
        var paymentBody = await payment.Content.ReadFromJsonAsync<JsonElement>();
        var paymentId = paymentBody.GetProperty("id").GetGuid();
        Assert.Equal("PendingConfirmation", paymentBody.GetProperty("status").GetString());

        var periodStart = DateTimeOffset.UtcNow;
        var (_, periodEnd) = SubscriptionBillingPeriods.ComputePaidPeriod(periodStart, BillingCycle.Monthly);
        var activate = await _client.PostAsJsonAsync(
            $"/api/v1/platform/payments/{paymentId}/activate-subscription",
            new
            {
                confirmedBy = "closeout-operator",
                subscriptionId,
                periodStartUtc = periodStart,
                periodEndUtc = periodEnd
            });
        activate.EnsureSuccessStatusCode();
        var activated = await activate.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Confirmed", activated.GetProperty("payment").GetProperty("status").GetString());
        Assert.Equal("Active", activated.GetProperty("subscription").GetProperty("status").GetString());

        // 19. Payment reuse blocked
        var reuse = await _client.PostAsJsonAsync(
            $"/api/v1/platform/payments/{paymentId}/activate-subscription",
            new
            {
                confirmedBy = "closeout-operator",
                subscriptionId,
                periodStartUtc = periodEnd,
                periodEndUtc = SubscriptionBillingPeriods.ComputePaidPeriod(periodEnd, BillingCycle.Monthly).End
            });
        Assert.Equal(HttpStatusCode.Conflict, reuse.StatusCode);

        // 9. Active entitlement snapshot
        var activeSnap = await _client.PostAsJsonAsync(
            $"/api/v1/platform/organizations/{organizationId}/products/{productCode}/entitlements/snapshots",
            new { });
        Assert.Equal(HttpStatusCode.Created, activeSnap.StatusCode);
        var activeSnapBody = await activeSnap.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(3, activeSnapBody.GetProperty("snapshotVersion").GetInt32());
        Assert.Equal("Active", activeSnapBody.GetProperty("subscriptionStatus").GetString());

        // 10–11. Override + next snapshot precedence
        var (overrideOperatorUserId, _, _) = await PlatformIntegrationTestUsers.CreatePlatformStaffWithPasswordAsync(
            _client,
            "closeout-override",
            "PlatformAdministrator");
        using var overrideRequest = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/v1/platform/organizations/{organizationId}/products/{productCode}/feature-overrides")
        {
            Content = JsonContent.Create(new
            {
                featureCode = FeatureCode.CustomerCreditCreate,
                enabled = false,
                reason = "Closeout hold",
            })
        };
        overrideRequest.Headers.Add(
            DevelopmentPlatformActorAccessor.DevPlatformUserIdHeader,
            overrideOperatorUserId.ToString("D"));
        var overrideResponse = await _client.SendAsync(overrideRequest);
        Assert.Equal(HttpStatusCode.Created, overrideResponse.StatusCode);
        var overrideId = (await overrideResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var overrideSnap = await _client.PostAsJsonAsync(
            $"/api/v1/platform/organizations/{organizationId}/products/{productCode}/entitlements/snapshots",
            new { });
        overrideSnap.EnsureSuccessStatusCode();
        var overrideSnapBody = await overrideSnap.Content.ReadFromJsonAsync<JsonElement>();
        var createGrant = overrideSnapBody.GetProperty("grants").EnumerateArray()
            .Single(g => g.GetProperty("featureCode").GetString() == FeatureCode.CustomerCreditCreate);
        Assert.False(createGrant.GetProperty("enabled").GetBoolean());
        Assert.Equal("Override", createGrant.GetProperty("source").GetString());

        // 12–13. GracePeriod snapshot
        (await _client.PostAsJsonAsync(
            $"/api/v1/platform/subscriptions/{subscriptionId}/grace-period",
            new { gracePeriodEndUtc = periodEnd.AddDays(7) })).EnsureSuccessStatusCode();

        var graceSnap = await _client.PostAsJsonAsync(
            $"/api/v1/platform/organizations/{organizationId}/products/{productCode}/entitlements/snapshots",
            new { });
        graceSnap.EnsureSuccessStatusCode();
        var graceBody = await graceSnap.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("GracePeriod", graceBody.GetProperty("subscriptionStatus").GetString());
        Assert.True(graceBody.GetProperty("inGracePeriod").GetBoolean());

        // 14–15. PastDue restricted grants
        (await _client.PostAsJsonAsync(
            $"/api/v1/platform/subscriptions/{subscriptionId}/past-due",
            new { })).EnsureSuccessStatusCode();

        var pastDueSnap = await _client.PostAsJsonAsync(
            $"/api/v1/platform/organizations/{organizationId}/products/{productCode}/entitlements/snapshots",
            new { });
        pastDueSnap.EnsureSuccessStatusCode();
        var pastDueBody = await pastDueSnap.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("PastDue", pastDueBody.GetProperty("subscriptionStatus").GetString());
        var pastDueCreate = pastDueBody.GetProperty("grants").EnumerateArray()
            .Single(g => g.GetProperty("featureCode").GetString() == FeatureCode.CustomerCreditCreate);
        Assert.False(pastDueCreate.GetProperty("enabled").GetBoolean());

        // 16–17. Cancel terminal + expire path on second org for Utang post-expiry
        (await _client.PostAsJsonAsync(
            $"/api/v1/platform/subscriptions/{subscriptionId}/cancel",
            new { })).EnsureSuccessStatusCode();
        var terminalReactivate = await _client.PostAsJsonAsync(
            $"/api/v1/platform/subscriptions/{subscriptionId}/reactivate",
            new { periodStartUtc = periodStart, periodEndUtc = periodEnd });
        Assert.Equal(HttpStatusCode.Conflict, terminalReactivate.StatusCode);

        var expireSlug = Unique("expire");
        if (expireSlug.Length > 40)
        {
            expireSlug = expireSlug[..40];
        }

        var org2 = await _client.PostAsJsonAsync(
            "/api/v1/platform/organizations",
            new { displayName = "Expire Org", slug = expireSlug });
        org2.EnsureSuccessStatusCode();
        var organizationId2 = (await org2.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var trial2 = await _client.PostAsJsonAsync(
            $"/api/v1/platform/organizations/{organizationId2}/subscriptions/trials",
            new { planId, planVersionId, trialDefinitionId = trialId });
        trial2.EnsureSuccessStatusCode();
        var subscriptionId2 = (await trial2.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        (await _client.PostAsJsonAsync(
            $"/api/v1/platform/subscriptions/{subscriptionId2}/expire",
            new { })).EnsureSuccessStatusCode();

        var expiredSnap = await _client.PostAsJsonAsync(
            $"/api/v1/platform/organizations/{organizationId2}/products/{productCode}/entitlements/snapshots",
            new { });
        expiredSnap.EnsureSuccessStatusCode();
        var expiredBody = await expiredSnap.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Expired", expiredBody.GetProperty("subscriptionStatus").GetString());
        Assert.True(GrantEnabled(expiredBody, FeatureCode.CustomerCreditView));
        Assert.True(GrantEnabled(expiredBody, FeatureCode.CustomerCreditRepay));
        Assert.False(GrantEnabled(expiredBody, FeatureCode.CustomerCreditCreate));

        // 18. Historical records remain queryable
        (await _client.GetAsync($"/api/v1/platform/payments/{paymentId}")).EnsureSuccessStatusCode();
        (await _client.GetAsync($"/api/v1/platform/subscriptions/{subscriptionId}")).EnsureSuccessStatusCode();
        (await _client.GetAsync($"/api/v1/platform/feature-overrides/{overrideId}")).EnsureSuccessStatusCode();
        var history = await _client.GetAsync(
            $"/api/v1/platform/organizations/{organizationId}/products/{productCode}/entitlements/snapshots");
        history.EnsureSuccessStatusCode();
        var historyBody = await history.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(historyBody.GetProperty("totalCount").GetInt32() >= 5);

        // 20. No delivery routes
        Assert.Equal(HttpStatusCode.NotFound,
            (await _client.GetAsync("/api/v1/platform/entitlements/deliver")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await _client.PostAsync("/api/v1/other-product/entitlements", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await _client.PostAsync("/api/v1/pos/entitlements", null)).StatusCode);
    }

    private static bool GrantEnabled(JsonElement snapshot, string featureCode) =>
        snapshot.GetProperty("grants").EnumerateArray()
            .Single(g => g.GetProperty("featureCode").GetString() == featureCode)
            .GetProperty("enabled").GetBoolean();

    private sealed class CloseoutApiFactory(string connectionString) : WebApplicationFactory<Program>
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
