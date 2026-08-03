using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Domain.Catalog;
using ExItS.Platform.Domain.Products;
using ExItS.Platform.Domain.Subscriptions;
using ExItS.Platform.Infrastructure;
using ExItS.Platform.IntegrationTests.Support;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ExItS.Platform.IntegrationTests;

[Collection(PostgreSqlCollection.Name)]
public sealed class ApiWp11StartBusinessCommercialTests(PostgreSqlFixture fixture) : IAsyncLifetime
{
    private Wp11LocalValidationApiFactory _factory = null!;
    private HttpClient _admin = null!;
    private HttpClient _client = null!;

    public Task InitializeAsync()
    {
        _factory = new Wp11LocalValidationApiFactory(fixture.ConnectionString);
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
    public async Task Start_business_business_monthly_gets_14_day_trial_with_commercial_snapshot()
    {
        await EnsureMvpCatalogAsync();
        var (token, userId, email, password) = await SeedPersonalUserAsync("sb14");
        var slug = Unique("sb14");
        var startBody = new
        {
            displayName = "Ana Sari-Sari",
            slug,
            productCode = ProductCode.PinoyBusinessPos,
            planKey = MvpPosPlanCodes.Business,
            billingCycle = BillingCycle.Monthly,
            startAsTrial = true,
            payNow = false,
            activatePosEntitlement = true,
            activateProductAccess = true,
            assignPosOwnerRole = false
        };

        using var start = Authed(HttpMethod.Post, "/api/v1/personal/start-business", token, startBody);
        var startResponse = await _client.SendAsync(start);
        if (startResponse.StatusCode != HttpStatusCode.Created)
        {
            var errorBody = await startResponse.Content.ReadAsStringAsync();
            Assert.Fail($"Start business failed ({startResponse.StatusCode}): {errorBody}");
        }
        var started = await startResponse.Content.ReadFromJsonAsync<JsonElement>();
        var organizationId = started.GetProperty("organizationId").GetGuid();
        var subscriptionId = started.GetProperty("subscriptionId").GetGuid();
        Assert.False(started.GetProperty("posOwnerRoleGranted").GetBoolean());
        Assert.True(started.GetProperty("organizationOwnerGranted").GetBoolean());

        var subscription = await _admin.GetAsync(
            $"/api/v1/platform/subscriptions/{subscriptionId}");
        subscription.EnsureSuccessStatusCode();
        var subBody = await subscription.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Trialing", subBody.GetProperty("status").GetString());
        Assert.Equal(organizationId, subBody.GetProperty("organizationId").GetGuid());
        Assert.Equal(nameof(BillingCycle.Monthly), subBody.GetProperty("billingCycle").GetString());
        Assert.Equal(699m, subBody.GetProperty("agreedPrice").GetDecimal());
        Assert.Equal("PHP", subBody.GetProperty("currencyCode").GetString());

        var trialStart = subBody.GetProperty("trialStartUtc").GetDateTimeOffset();
        var trialEnd = subBody.GetProperty("trialEndUtc").GetDateTimeOffset();
        Assert.InRange((trialEnd - trialStart).TotalDays, 13.9, 14.1);

        var entitlement = await _admin.GetAsync(
            $"/api/v1/platform/organizations/{organizationId}/products/{ProductCode.PinoyBusinessPos}/entitlements/snapshots/latest");
        entitlement.EnsureSuccessStatusCode();
        var entBody = await entitlement.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Trialing", entBody.GetProperty("subscriptionStatus").GetString());
        Assert.True(entBody.GetProperty("snapshotVersion").GetInt32() >= 1);

        var relogin = await _client.PostAsJsonAsync(
            "/api/v1/platform/auth/login",
            new { usernameOrEmail = email, password });
        relogin.EnsureSuccessStatusCode();
        var personalToken = (await relogin.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("sessionToken").GetString()!;

        using var profiles = Authed(HttpMethod.Get, "/api/v1/platform/auth/account-profiles", personalToken);
        var profilesResponse = await _client.SendAsync(profiles);
        profilesResponse.EnsureSuccessStatusCode();
        var profileItems = (await profilesResponse.Content.ReadFromJsonAsync<JsonElement>())
            .EnumerateArray()
            .ToList();
        Assert.Contains(profileItems, p =>
            string.Equals(p.GetProperty("accountClass").GetString(), "Personal", StringComparison.Ordinal)
            && string.Equals(p.GetProperty("status").GetString(), "Active", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(profileItems, p => p.GetProperty("userIdentityId").GetGuid() == userId);
    }

    [Fact]
    public async Task Start_business_accepts_string_billing_cycle_monthly_from_admin_ui()
    {
        await EnsureMvpCatalogAsync();
        var (token, _, _, _) = await SeedPersonalUserAsync("sbstr");
        var slug = Unique("sbstr");
        using var start = Authed(
            HttpMethod.Post,
            "/api/v1/personal/start-business",
            token,
            new
            {
                displayName = "String Cycle Store",
                slug,
                productCode = ProductCode.PinoyBusinessPos,
                planKey = MvpPosPlanCodes.Business,
                billingCycle = "Monthly",
                startAsTrial = true,
                payNow = false
            });
        var response = await _client.SendAsync(start);
        if (response.StatusCode != HttpStatusCode.Created)
        {
            Assert.Fail($"Start business string billingCycle failed ({response.StatusCode}): {await response.Content.ReadAsStringAsync()}");
        }

        var started = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.NotEqual(Guid.Empty, started.GetProperty("organizationId").GetGuid());
        Assert.NotEqual(Guid.Empty, started.GetProperty("subscriptionId").GetGuid());
    }

    [Fact]
    public async Task Start_business_rejects_invalid_plan_key()
    {
        await EnsureMvpCatalogAsync();
        var (token, _, _, _) = await SeedPersonalUserAsync("sbinv");
        using var request = Authed(
            HttpMethod.Post,
            "/api/v1/personal/start-business",
            token,
            new
            {
                displayName = "Invalid Plan Store",
                slug = Unique("sbinv"),
                planKey = "does-not-exist",
                productCode = ProductCode.PinoyBusinessPos
            });
        var response = await _client.SendAsync(request);
        Assert.True(
            response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.BadRequest,
            $"Unexpected status: {response.StatusCode}");
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("plan", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Start_business_starter_trial_creates_trialing_subscription_without_payment()
    {
        await EnsureMvpCatalogAsync();
        var (token, _, _, _) = await SeedPersonalUserAsync("sbstr");
        var slug = Unique("sbstr");
        using var request = Authed(
            HttpMethod.Post,
            "/api/v1/personal/start-business",
            token,
            new
            {
                displayName = "Starter Trial Store",
                slug,
                productCode = ProductCode.PinoyBusinessPos,
                planKey = MvpPosPlanCodes.Starter,
                billingCycle = "Monthly",
                startAsTrial = true,
                payNow = false,
                activatePosEntitlement = true,
                assignPosOwnerRole = false
            });
        var response = await _client.SendAsync(request);
        if (response.StatusCode != HttpStatusCode.Created)
        {
            Assert.Fail($"Starter trial failed ({response.StatusCode}): {await response.Content.ReadAsStringAsync()}");
        }

        var started = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(started.GetProperty("posOwnerRoleGranted").GetBoolean());
        Assert.True(started.GetProperty("organizationOwnerGranted").GetBoolean());
        Assert.Equal("Organization", started.GetProperty("accountClass").GetString());
        Assert.False(string.IsNullOrWhiteSpace(started.GetProperty("sessionToken").GetString()));

        var subscriptionId = started.GetProperty("subscriptionId").GetGuid();
        var subscription = await _admin.GetAsync($"/api/v1/platform/subscriptions/{subscriptionId}");
        subscription.EnsureSuccessStatusCode();
        var subBody = await subscription.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Trialing", subBody.GetProperty("status").GetString());
        Assert.Equal("Starter", subBody.GetProperty("planDisplayName").GetString());

        var payments = await _admin.GetAsync(
            $"/api/v1/platform/organizations/{started.GetProperty("organizationId").GetGuid()}/payments?pageSize=20");
        if (payments.IsSuccessStatusCode)
        {
            var payBody = await payments.Content.ReadFromJsonAsync<JsonElement>();
            if (payBody.TryGetProperty("items", out var items))
            {
                Assert.Equal(0, items.GetArrayLength());
            }
        }
    }

    [Fact]
    public async Task Start_business_starter_subscribe_paynow_activates_paid_subscription()
    {
        await EnsureMvpCatalogAsync();
        var (token, _, _, _) = await SeedPersonalUserAsync("sbpay");
        var slug = Unique("sbpay");
        using var request = Authed(
            HttpMethod.Post,
            "/api/v1/personal/start-business",
            token,
            new
            {
                displayName = "Starter Paid Store",
                slug,
                productCode = ProductCode.PinoyBusinessPos,
                planKey = MvpPosPlanCodes.Starter,
                billingCycle = "Monthly",
                startAsTrial = false,
                payNow = true,
                activatePosEntitlement = true,
                assignPosOwnerRole = false
            });
        var response = await _client.SendAsync(request);
        if (response.StatusCode != HttpStatusCode.Created)
        {
            Assert.Fail($"Starter subscribe failed ({response.StatusCode}): {await response.Content.ReadAsStringAsync()}");
        }

        var started = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(started.GetProperty("organizationOwnerGranted").GetBoolean());
        Assert.False(started.GetProperty("posOwnerRoleGranted").GetBoolean());
        var subscriptionId = started.GetProperty("subscriptionId").GetGuid();
        var subscription = await _admin.GetAsync($"/api/v1/platform/subscriptions/{subscriptionId}");
        subscription.EnsureSuccessStatusCode();
        var subBody = await subscription.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Active", subBody.GetProperty("status").GetString());
        Assert.Equal("Starter", subBody.GetProperty("planDisplayName").GetString());
    }

    [Fact]
    public async Task Start_business_pro_trial_is_rejected()
    {
        await EnsureMvpCatalogAsync();
        var (token, _, _, _) = await SeedPersonalUserAsync("sbpro");
        using var request = Authed(
            HttpMethod.Post,
            "/api/v1/personal/start-business",
            token,
            new
            {
                displayName = "Pro Trial Store",
                slug = Unique("sbpro"),
                productCode = ProductCode.PinoyBusinessPos,
                planKey = MvpPosPlanCodes.Pro,
                startAsTrial = true,
                payNow = false,
                activatePosEntitlement = true
            });
        var response = await _client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(
            response.StatusCode is HttpStatusCode.Conflict or HttpStatusCode.BadRequest,
            $"Unexpected status {response.StatusCode}: {body}");
        Assert.True(
            body.Contains(ApplicationErrorCodes.TrialNotAllowed, StringComparison.Ordinal)
            || body.Contains("trial", StringComparison.OrdinalIgnoreCase),
            body);
    }

    [Fact]
    public async Task Start_business_same_slug_is_idempotent_for_owner()
    {
        await EnsureMvpCatalogAsync();
        var (token, _, email, password) = await SeedPersonalUserAsync("sbid");
        var slug = Unique("sbid");
        var payload = new
        {
            displayName = "Idempotent Store",
            slug,
            productCode = ProductCode.PinoyBusinessPos,
            planKey = MvpPosPlanCodes.Business,
            billingCycle = BillingCycle.Monthly,
            startAsTrial = true,
            activatePosEntitlement = true
        };

        using var first = Authed(HttpMethod.Post, "/api/v1/personal/start-business", token, payload);
        var firstResponse = await _client.SendAsync(first);
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);
        var firstBody = await firstResponse.Content.ReadFromJsonAsync<JsonElement>();
        var firstOrgId = firstBody.GetProperty("organizationId").GetGuid();

        var personalToken = await EnsurePersonalSessionTokenAsync(email, password);

        using var second = Authed(HttpMethod.Post, "/api/v1/personal/start-business", personalToken, payload);
        var secondResponse = await _client.SendAsync(second);
        Assert.Equal(HttpStatusCode.Created, secondResponse.StatusCode);
        var secondBody = await secondResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(firstOrgId, secondBody.GetProperty("organizationId").GetGuid());
    }

    [Fact]
    public async Task Second_trial_for_same_org_product_is_rejected()
    {
        await EnsureMvpCatalogAsync();
        var (token, _, _, _) = await SeedPersonalUserAsync("sb2t");
        var slug = Unique("sb2t");
        using var start = Authed(
            HttpMethod.Post,
            "/api/v1/personal/start-business",
            token,
            new
            {
                displayName = "Trial Once Store",
                slug,
                productCode = ProductCode.PinoyBusinessPos,
                planKey = MvpPosPlanCodes.Business,
                startAsTrial = true,
                activatePosEntitlement = true
            });
        var startResponse = await _client.SendAsync(start);
        startResponse.EnsureSuccessStatusCode();
        var started = await startResponse.Content.ReadFromJsonAsync<JsonElement>();
        var organizationId = started.GetProperty("organizationId").GetGuid();

        var plans = await _admin.GetAsync(
            $"/api/v1/platform/catalog/plans?productCode={ProductCode.PinoyBusinessPos}&status=Active&page=1&pageSize=10");
        plans.EnsureSuccessStatusCode();
        var businessPlan = (await plans.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("items")
            .EnumerateArray()
            .First(p => p.GetProperty("code").GetString() == MvpPosPlanCodes.Business);

        var planId = businessPlan.GetProperty("id").GetGuid();
        var versionResponse = await _admin.GetAsync(
            $"/api/v1/platform/catalog/products/{ProductCode.PinoyBusinessPos}/plans/{planId}/versions");
        versionResponse.EnsureSuccessStatusCode();
        var versionId = (await versionResponse.Content.ReadFromJsonAsync<JsonElement>())
            .EnumerateArray()
            .First(v => v.GetProperty("status").GetString() == "Published")
            .GetProperty("id")
            .GetGuid();

        var trials = await _admin.GetAsync(
            $"/api/v1/platform/catalog/products/{ProductCode.PinoyBusinessPos}/trials");
        trials.EnsureSuccessStatusCode();
        var trialId = (await trials.Content.ReadFromJsonAsync<JsonElement>())
            .EnumerateArray()
            .First()
            .GetProperty("id")
            .GetGuid();

        var retry = await _admin.PostAsJsonAsync(
            $"/api/v1/platform/organizations/{organizationId}/subscriptions/trials",
            new { planId, planVersionId = versionId, trialDefinitionId = trialId });
        Assert.Equal(HttpStatusCode.Conflict, retry.StatusCode);
        var retryBody = await retry.Content.ReadAsStringAsync();
        Assert.True(
            retryBody.Contains(ApplicationErrorCodes.ActiveSubscriptionConflict, StringComparison.Ordinal)
            || retryBody.Contains(ApplicationErrorCodes.TrialAlreadyConsumed, StringComparison.Ordinal),
            retryBody);
    }

    [Fact]
    public async Task Trial_conversion_via_local_validation_payment_simulation()
    {
        await using var lvFactory = new Wp11LocalValidationApiFactory(fixture.ConnectionString);
        using var lvClient = lvFactory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

        await EnsureMvpCatalogAsync();
        var (token, _, _, _) = await SeedPersonalUserAsync("sbpay", lvClient);
        var slug = Unique("sbpay");
        using var start = Authed(
            HttpMethod.Post,
            "/api/v1/personal/start-business",
            token,
            new
            {
                displayName = "Pay Convert Store",
                slug,
                productCode = ProductCode.PinoyBusinessPos,
                planKey = MvpPosPlanCodes.Business,
                billingCycle = BillingCycle.Monthly,
                startAsTrial = true,
                activatePosEntitlement = true
            });
        var startResponse = await lvClient.SendAsync(start);
        startResponse.EnsureSuccessStatusCode();
        var started = await startResponse.Content.ReadFromJsonAsync<JsonElement>();
        var organizationId = started.GetProperty("organizationId").GetGuid();
        var subscriptionId = started.GetProperty("subscriptionId").GetGuid();

        var entitlementBefore = await lvClient.GetAsync(
            $"/api/v1/platform/organizations/{organizationId}/products/{ProductCode.PinoyBusinessPos}/entitlements/snapshots/latest");
        entitlementBefore.EnsureSuccessStatusCode();
        var versionBefore = (await entitlementBefore.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("snapshotVersion")
            .GetInt32();

        var idempotencyKey = Guid.NewGuid().ToString("N");
        var simulate = await lvClient.PostAsJsonAsync(
            "/api/v1/platform/local-validation/payments/simulate",
            new
            {
                simulation = "Succeeded",
                organizationId,
                subscriptionId,
                amount = 699m,
                currencyCode = "PHP",
                idempotencyKey,
                purpose = "initial",
                billingCycle = "Monthly"
            });
        if (!simulate.IsSuccessStatusCode)
        {
            var simulateError = await simulate.Content.ReadAsStringAsync();
            Assert.Fail($"Simulate payment failed ({simulate.StatusCode}): {simulateError}");
        }

        var subscription = await lvClient.GetAsync(
            $"/api/v1/platform/subscriptions/{subscriptionId}");
        subscription.EnsureSuccessStatusCode();
        var subBody = await subscription.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Active", subBody.GetProperty("status").GetString());
        Assert.Equal(699m, subBody.GetProperty("agreedPrice").GetDecimal());
        Assert.NotNull(subBody.GetProperty("paidPeriodEndUtc").GetString());

        var entitlementAfter = await lvClient.GetAsync(
            $"/api/v1/platform/organizations/{organizationId}/products/{ProductCode.PinoyBusinessPos}/entitlements/snapshots/latest");
        entitlementAfter.EnsureSuccessStatusCode();
        var versionAfter = (await entitlementAfter.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("snapshotVersion")
            .GetInt32();
        Assert.True(versionAfter > versionBefore);

        var duplicate = await lvClient.PostAsJsonAsync(
            "/api/v1/platform/local-validation/payments/simulate",
            new
            {
                simulation = "Succeeded",
                organizationId,
                subscriptionId,
                amount = 699m,
                currencyCode = "PHP",
                idempotencyKey,
                purpose = "initial",
                billingCycle = "Monthly"
            });
        duplicate.EnsureSuccessStatusCode();
        var subscriptionAgain = await lvClient.GetAsync(
            $"/api/v1/platform/subscriptions/{subscriptionId}");
        subscriptionAgain.EnsureSuccessStatusCode();
        var subAgain = await subscriptionAgain.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Active", subAgain.GetProperty("status").GetString());

        var declinedKey = Guid.NewGuid().ToString("N");
        await using var declinedFactory = new Wp11LocalValidationApiFactory(fixture.ConnectionString);
        using var declinedClient = declinedFactory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        await EnsureMvpCatalogAsync();
        var (declinedToken, _, _, _) = await SeedPersonalUserAsync("sbdec", declinedClient);
        var declinedSlug = Unique("sbdec");
        using var declinedStart = Authed(
            HttpMethod.Post,
            "/api/v1/personal/start-business",
            declinedToken,
            new
            {
                displayName = "Declined Store",
                slug = declinedSlug,
                productCode = ProductCode.PinoyBusinessPos,
                planKey = MvpPosPlanCodes.Business,
                startAsTrial = true,
                activatePosEntitlement = true
            });
        var declinedStartResponse = await declinedClient.SendAsync(declinedStart);
        declinedStartResponse.EnsureSuccessStatusCode();
        var declinedStarted = await declinedStartResponse.Content.ReadFromJsonAsync<JsonElement>();
        var declinedOrgId = declinedStarted.GetProperty("organizationId").GetGuid();
        var declinedSubId = declinedStarted.GetProperty("subscriptionId").GetGuid();

        var declined = await declinedClient.PostAsJsonAsync(
            "/api/v1/platform/local-validation/payments/simulate",
            new
            {
                simulation = "Declined",
                organizationId = declinedOrgId,
                subscriptionId = declinedSubId,
                amount = 699m,
                currencyCode = "PHP",
                idempotencyKey = declinedKey,
                purpose = "initial",
                billingCycle = "Monthly"
            });
        declined.EnsureSuccessStatusCode();

        var declinedSub = await declinedClient.GetAsync(
            $"/api/v1/platform/subscriptions/{declinedSubId}");
        declinedSub.EnsureSuccessStatusCode();
        var declinedBody = await declinedSub.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Trialing", declinedBody.GetProperty("status").GetString());
    }

    private async Task<string> EnsurePersonalSessionTokenAsync(string email, string password, HttpClient? client = null)
    {
        client ??= _client;
        var login = await client.PostAsJsonAsync(
            "/api/v1/platform/auth/login",
            new { usernameOrEmail = email, password });
        login.EnsureSuccessStatusCode();
        var loginBody = await login.Content.ReadFromJsonAsync<JsonElement>();
        var token = loginBody.GetProperty("sessionToken").GetString()!;

        using var profilesRequest = Authed(HttpMethod.Get, "/api/v1/platform/auth/account-profiles", token);
        var profilesResponse = await client.SendAsync(profilesRequest);
        profilesResponse.EnsureSuccessStatusCode();
        var personalProfileId = (await profilesResponse.Content.ReadFromJsonAsync<JsonElement>())
            .EnumerateArray()
            .First(p => string.Equals(p.GetProperty("accountClass").GetString(), "Personal", StringComparison.Ordinal))
            .GetProperty("id")
            .GetGuid();

        using var selectRequest = Authed(
            HttpMethod.Post,
            "/api/v1/platform/auth/account-profiles/select",
            token,
            new { accountProfileId = personalProfileId });
        var selectResponse = await client.SendAsync(selectRequest);
        selectResponse.EnsureSuccessStatusCode();
        return (await selectResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("sessionToken").GetString()!;
    }

    private async Task EnsureMvpCatalogAsync()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:PlatformDatabase"] = fixture.ConnectionString
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

        await using var provider = services.BuildServiceProvider();
        var createProduct = provider.GetRequiredService<CreateProduct>();
        var productResult = await createProduct.ExecuteAsync(ProductCode.PinoyBusinessPos, "Pinoy Business POS");
        if (!productResult.IsSuccess && productResult.ErrorCode != ApplicationErrorCodes.DuplicateProductCode)
        {
            throw new InvalidOperationException(
                $"POS product seed failed: {productResult.ErrorCode} {productResult.ErrorMessage}");
        }

        var ensure = provider.GetRequiredService<EnsureMvpPosPlans>();
        await ensure.ExecuteAsync();
    }

    private async Task<(string Token, Guid UserId, string Email, string Password)> SeedPersonalUserAsync(
        string prefix,
        HttpClient? client = null)
    {
        client ??= _client;
        var (userId, email, password) = await PlatformIntegrationTestUsers.RegisterPersonalWithPasswordAsync(client, prefix);
        var login = await client.PostAsJsonAsync(
            "/api/v1/platform/auth/login",
            new { usernameOrEmail = email, password });
        login.EnsureSuccessStatusCode();
        var token = (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("sessionToken").GetString()!;
        return (token, userId, email, password);
    }
}

internal sealed class Wp11LocalValidationApiFactory(string connectionString) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:PlatformDatabase", connectionString);
        builder.UseSetting("LocalValidation:Enabled", "true");
        builder.UseSetting("LocalValidation:RunHostedSeed", "false");
        builder.UseSetting("LocalValidation:SharedPassword", "LocalValidationTestPass123");
        builder.UseSetting("Payments:Provider", "LocalValidation");
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:PlatformDatabase"] = connectionString,
                ["Security:EnforceHttps"] = "false",
                ["PlatformAuthentication:External:TestingEndpointEnabled"] = "true",
                ["PlatformAuthentication:Lifecycle:ExposeDebugTokens"] = "true",
                ["LocalValidation:Enabled"] = "true",
                ["LocalValidation:RunHostedSeed"] = "false",
                ["LocalValidation:SharedPassword"] = "LocalValidationTestPass123",
                ["Payments:Provider"] = "LocalValidation"
            });
        });
    }
}
