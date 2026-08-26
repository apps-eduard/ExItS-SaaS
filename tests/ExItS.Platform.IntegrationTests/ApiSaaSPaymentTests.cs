using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ExItS.Platform.Application.Payments;
using ExItS.Platform.Domain.Catalog;
using ExItS.Platform.Domain.Subscriptions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace ExItS.Platform.IntegrationTests;

[Collection(PostgreSqlCollection.Name)]
public sealed class ApiSaaSPaymentTests(PostgreSqlFixture fixture) : IAsyncLifetime
{
    private PaymentApiFactory _factory = null!;
    private HttpClient _client = null!;

    public Task InitializeAsync()
    {
        _factory = new PaymentApiFactory(fixture.ConnectionString);
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

    private async Task<Guid> CreateOrganizationAsync(string prefix)
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/platform/organizations",
            new { displayName = $"{prefix} Org", slug = Unique(prefix) });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
    }

    private async Task<string> CreateProductAsync(string prefix)
    {
        var candidate = Unique(prefix);
        var productCode = candidate[..Math.Min(30, candidate.Length)];
        var response = await _client.PostAsJsonAsync(
            "/api/v1/platform/catalog/products",
            new { code = productCode, displayName = "POS" });
        response.EnsureSuccessStatusCode();
        return productCode;
    }

    private async Task<(Guid organizationId, Guid planId, Guid versionId, Guid trialId, string productCode)>
        SeedOrganizationAndTrialEligibleCatalogAsync(string prefix)
    {
        var organizationId = await CreateOrganizationAsync(prefix);
        var productCode = await CreateProductAsync(prefix);

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
            new { code = "utang", displayName = "Utang", monthlyPrice = 500m, annualPrice = 5000m, currencyCode = "PHP" });
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
                grants = new[] { new { featureCode = FeatureCode.CustomerCreditView, enabled = true } }
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

        return (organizationId, planId, versionId, trialId, productCode);
    }

    private async Task<Guid> CreateManualPaymentAsync(
        Guid organizationId,
        string productCode,
        string reference,
        decimal amount = 500m,
        string method = "GCash")
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/platform/payments/manual",
            new
            {
                organizationId,
                productCode,
                amount,
                currencyCode = "PHP",
                method,
                externalReference = reference,
                paidAtUtc = DateTimeOffset.UtcNow
            });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
    }

    [Fact]
    public async Task Create_manual_payment_returns_201_and_can_be_fetched_by_id()
    {
        var organizationId = await CreateOrganizationAsync("api-payment");
        var productCode = await CreateProductAsync("api-payment");

        var response = await _client.PostAsJsonAsync(
            "/api/v1/platform/payments/manual",
            new
            {
                organizationId,
                productCode,
                amount = 1250.75m,
                currencyCode = "PHP",
                method = "GCash",
                externalReference = "GCASH-REF-001",
                paidAtUtc = DateTimeOffset.UtcNow
            });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<JsonElement>();
        var paymentId = created.GetProperty("id").GetGuid();
        Assert.Equal("PendingConfirmation", created.GetProperty("status").GetString());

        var get = await _client.GetAsync($"/api/v1/platform/payments/{paymentId}");
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
        var fetched = await get.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(paymentId, fetched.GetProperty("id").GetGuid());
        Assert.Equal("GCASH-REF-001", fetched.GetProperty("externalReference").GetString());

        var missing = await _client.GetAsync($"/api/v1/platform/payments/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    [Fact]
    public async Task Create_manual_payment_with_duplicate_reference_returns_409()
    {
        var organizationId = await CreateOrganizationAsync("api-dup-payment");
        var productCode = await CreateProductAsync("api-dup-payment");

        await CreateManualPaymentAsync(organizationId, productCode, "DUPLICATE-REF");

        var duplicate = await _client.PostAsJsonAsync(
            "/api/v1/platform/payments/manual",
            new
            {
                organizationId,
                productCode,
                amount = 100m,
                currencyCode = "PHP",
                method = "GCash",
                externalReference = "duplicate-ref",
                paidAtUtc = DateTimeOffset.UtcNow
            });
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
    }

    [Fact]
    public async Task Confirm_reject_and_void_endpoints_transition_payment_status()
    {
        var organizationId = await CreateOrganizationAsync("api-lifecycle-payment");
        var productCode = await CreateProductAsync("api-lifecycle-payment");

        var confirmTargetId = await CreateManualPaymentAsync(organizationId, productCode, "REF-CONFIRM");
        var confirm = await _client.PostAsJsonAsync(
            $"/api/v1/platform/payments/{confirmTargetId}/confirm",
            new { confirmedBy = "staff-1" });
        Assert.Equal(HttpStatusCode.OK, confirm.StatusCode);
        var confirmed = await confirm.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Confirmed", confirmed.GetProperty("status").GetString());

        var voidResponse = await _client.PostAsJsonAsync(
            $"/api/v1/platform/payments/{confirmTargetId}/void",
            new { voidedBy = "staff-1", reason = "refunded" });
        Assert.Equal(HttpStatusCode.OK, voidResponse.StatusCode);
        Assert.Equal(
            "Voided",
            (await voidResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("status").GetString());

        var rejectTargetId = await CreateManualPaymentAsync(organizationId, productCode, "REF-REJECT");
        var reject = await _client.PostAsJsonAsync(
            $"/api/v1/platform/payments/{rejectTargetId}/reject",
            new { rejectedBy = "staff-1", reason = "unverifiable" });
        Assert.Equal(HttpStatusCode.OK, reject.StatusCode);
        Assert.Equal(
            "Rejected",
            (await reject.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("status").GetString());
    }

    [Fact]
    public async Task Activate_subscription_endpoint_succeeds_for_a_confirmed_payment_and_conflicts_on_reuse()
    {
        var (organizationId, planId, versionId, trialId, productCode) =
            await SeedOrganizationAndTrialEligibleCatalogAsync("api-activate-payment");

        var start = await _client.PostAsJsonAsync(
            $"/api/v1/platform/organizations/{organizationId}/subscriptions/trials",
            new { planId, planVersionId = versionId, trialDefinitionId = trialId });
        start.EnsureSuccessStatusCode();
        var subscriptionId = (await start.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var paymentId = await CreateManualPaymentAsync(organizationId, productCode, "REF-API-ACTIVATE");

        var (periodStart, periodEnd) = SubscriptionBillingPeriods.ComputePaidPeriod(
            DateTimeOffset.UtcNow,
            BillingCycle.Monthly);
        var activate = await _client.PostAsJsonAsync(
            $"/api/v1/platform/payments/{paymentId}/activate-subscription",
            new
            {
                confirmedBy = "staff-1",
                subscriptionId,
                periodStartUtc = periodStart,
                periodEndUtc = periodEnd,
                billingCycle = nameof(BillingCycle.Monthly)
            });
        Assert.Equal(HttpStatusCode.OK, activate.StatusCode);
        var activated = await activate.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Confirmed", activated.GetProperty("payment").GetProperty("status").GetString());
        Assert.Equal("Active", activated.GetProperty("subscription").GetProperty("status").GetString());

        var reuse = await _client.PostAsJsonAsync(
            $"/api/v1/platform/payments/{paymentId}/activate-subscription",
            new
            {
                confirmedBy = "staff-1",
                subscriptionId,
                periodStartUtc = periodStart,
                periodEndUtc = periodEnd,
                billingCycle = nameof(BillingCycle.Monthly)
            });
        Assert.Equal(HttpStatusCode.Conflict, reuse.StatusCode);
    }

    [Fact]
    public async Task Activate_subscription_endpoint_allows_confirming_an_unconfirmed_payment_inline()
    {
        var (organizationId, planId, versionId, trialId, productCode) =
            await SeedOrganizationAndTrialEligibleCatalogAsync("api-inline-confirm");

        var start = await _client.PostAsJsonAsync(
            $"/api/v1/platform/organizations/{organizationId}/subscriptions/trials",
            new { planId, planVersionId = versionId, trialDefinitionId = trialId });
        start.EnsureSuccessStatusCode();
        var subscriptionId = (await start.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var paymentId = await CreateManualPaymentAsync(organizationId, productCode, "REF-INLINE-CONFIRM");

        var (periodStart, periodEnd) = SubscriptionBillingPeriods.ComputePaidPeriod(
            DateTimeOffset.UtcNow,
            BillingCycle.Monthly);
        var activate = await _client.PostAsJsonAsync(
            $"/api/v1/platform/payments/{paymentId}/activate-subscription",
            new
            {
                confirmedBy = "staff-1",
                subscriptionId,
                periodStartUtc = periodStart,
                periodEndUtc = periodEnd,
                billingCycle = nameof(BillingCycle.Monthly)
            });
        Assert.Equal(HttpStatusCode.OK, activate.StatusCode);

        var payment = await _client.GetAsync($"/api/v1/platform/payments/{paymentId}");
        var body = await payment.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Confirmed", body.GetProperty("status").GetString());
        Assert.Equal(subscriptionId, body.GetProperty("subscriptionId").GetGuid());
    }

    [Fact]
    public async Task Activate_subscription_endpoint_rejects_a_voided_payment_as_conflict()
    {
        var (organizationId, planId, versionId, trialId, productCode) =
            await SeedOrganizationAndTrialEligibleCatalogAsync("api-voided-activate");

        var start = await _client.PostAsJsonAsync(
            $"/api/v1/platform/organizations/{organizationId}/subscriptions/trials",
            new { planId, planVersionId = versionId, trialDefinitionId = trialId });
        start.EnsureSuccessStatusCode();
        var subscriptionId = (await start.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var paymentId = await CreateManualPaymentAsync(organizationId, productCode, "REF-VOIDED-ACTIVATE");
        var confirm = await _client.PostAsJsonAsync(
            $"/api/v1/platform/payments/{paymentId}/confirm", new { confirmedBy = "staff-1" });
        confirm.EnsureSuccessStatusCode();
        var voidResponse = await _client.PostAsJsonAsync(
            $"/api/v1/platform/payments/{paymentId}/void", new { voidedBy = "staff-1", reason = "refunded" });
        voidResponse.EnsureSuccessStatusCode();

        var now = DateTimeOffset.UtcNow;
        var activate = await _client.PostAsJsonAsync(
            $"/api/v1/platform/payments/{paymentId}/activate-subscription",
            new { confirmedBy = "staff-1", subscriptionId, periodStartUtc = now, periodEndUtc = now.AddMonths(1) });
        Assert.Equal(HttpStatusCode.Conflict, activate.StatusCode);
    }

    [Fact]
    public async Task List_payments_by_organization_and_by_status_return_expected_results()
    {
        var organizationId = await CreateOrganizationAsync("api-list-payment");
        var productCode = await CreateProductAsync("api-list-payment");

        var pendingId = await CreateManualPaymentAsync(organizationId, productCode, "REF-LIST-PENDING");
        var confirmTargetId = await CreateManualPaymentAsync(organizationId, productCode, "REF-LIST-CONFIRM");
        var confirm = await _client.PostAsJsonAsync(
            $"/api/v1/platform/payments/{confirmTargetId}/confirm", new { confirmedBy = "staff-1" });
        confirm.EnsureSuccessStatusCode();

        var byOrg = await _client.GetAsync($"/api/v1/platform/organizations/{organizationId}/payments");
        Assert.Equal(HttpStatusCode.OK, byOrg.StatusCode);
        var byOrgBody = await byOrg.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(2, byOrgBody.GetProperty("totalCount").GetInt32());

        var byOrgPending = await _client.GetAsync(
            $"/api/v1/platform/organizations/{organizationId}/payments?status=PendingConfirmation");
        Assert.Equal(HttpStatusCode.OK, byOrgPending.StatusCode);
        var byOrgPendingBody = await byOrgPending.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, byOrgPendingBody.GetProperty("totalCount").GetInt32());
        var items = byOrgPendingBody.GetProperty("items").EnumerateArray().ToArray();
        Assert.Single(items);
        Assert.Equal(pendingId, items[0].GetProperty("id").GetGuid());

        var byProductStatus = await _client.GetAsync(
            $"/api/v1/platform/payments?productCode={productCode}&status=Confirmed");
        Assert.Equal(HttpStatusCode.OK, byProductStatus.StatusCode);
        var byProductStatusBody = await byProductStatus.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, byProductStatusBody.GetProperty("totalCount").GetInt32());
    }

    private sealed class PaymentApiFactory(string connectionString) : WebApplicationFactory<Program>
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
