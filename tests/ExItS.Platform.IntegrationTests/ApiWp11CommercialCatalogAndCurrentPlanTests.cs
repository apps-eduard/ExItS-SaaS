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
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ExItS.Platform.IntegrationTests;

[Collection(PostgreSqlCollection.Name)]
public sealed class ApiWp11CommercialCatalogAndCurrentPlanTests(PostgreSqlFixture fixture) : IAsyncLifetime
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
    public async Task Authenticated_personal_user_can_get_commercial_plans_with_mvp_packages()
    {
        await EnsureMvpCatalogAsync();
        var (_, _, email, password) = await SeedPersonalUserAsync("cmpl");
        var token = await EnsurePersonalSessionTokenAsync(email, password);

        using var request = Authed(
            HttpMethod.Get,
            $"/api/v1/commercial/plans?productCode={ProductCode.PinoyBusinessPos}",
            token);
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var plans = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(JsonValueKind.Array, plans.ValueKind);
        var planKeys = plans.EnumerateArray()
            .Select(p => p.GetProperty("planKey").GetString())
            .Where(k => k is not null)
            .OrderBy(k => k, StringComparer.Ordinal)
            .ToList();
        Assert.Equal(MvpPosPlanCodes.All.OrderBy(x => x, StringComparer.Ordinal), planKeys);
        Assert.All(plans.EnumerateArray(), p => Assert.Equal("PHP", p.GetProperty("currencyCode").GetString()));
    }

    [Fact]
    public async Task Unauthenticated_commercial_plans_returns_401()
    {
        var response = await _client.GetAsync(
            $"/api/v1/commercial/plans?productCode={ProductCode.PinoyBusinessPos}");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task EnsureMvpPosPlans_remains_idempotent_via_commercial_plans_endpoint()
    {
        await EnsureMvpCatalogAsync();
        var (_, _, email, password) = await SeedPersonalUserAsync("idmp");
        var token = await EnsurePersonalSessionTokenAsync(email, password);

        using var first = Authed(
            HttpMethod.Get,
            $"/api/v1/commercial/plans?productCode={ProductCode.PinoyBusinessPos}",
            token);
        using var second = Authed(
            HttpMethod.Get,
            $"/api/v1/commercial/plans?productCode={ProductCode.PinoyBusinessPos}",
            token);
        var firstResponse = await _client.SendAsync(first);
        var secondResponse = await _client.SendAsync(second);
        firstResponse.EnsureSuccessStatusCode();
        secondResponse.EnsureSuccessStatusCode();

        var firstBody = await firstResponse.Content.ReadAsStringAsync();
        var secondBody = await secondResponse.Content.ReadAsStringAsync();
        Assert.Equal(firstBody, secondBody);
    }

    [Fact]
    public async Task Org_without_subscription_current_plan_returns_empty_state()
    {
        await EnsureMvpCatalogAsync();
        var orgId = await CreateOrganizationAsync("empty");

        var response = await _admin.GetAsync(
            $"/api/v1/platform/organizations/{orgId}/current-plan?productCode={ProductCode.PinoyBusinessPos}");
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(orgId, body.GetProperty("organizationId").GetGuid());
        Assert.True(body.GetProperty("currentSubscription").ValueKind is JsonValueKind.Null);
        Assert.True(body.GetProperty("currentPlan").ValueKind is JsonValueKind.Null);
        Assert.True(body.GetProperty("pendingPlanChange").ValueKind is JsonValueKind.Null);
        Assert.True(body.GetProperty("availablePlans").GetArrayLength() >= 3);
    }

    [Fact]
    public async Task Org_commercial_summary_without_subscription_returns_200()
    {
        await EnsureMvpCatalogAsync();
        var orgId = await CreateOrganizationAsync("csem");

        var response = await _admin.GetAsync(
            $"/api/v1/platform/admin/organizations/{orgId}/commercial-summary");
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(orgId, body.GetProperty("organization").GetProperty("id").GetGuid());
        Assert.Empty(body.GetProperty("subscriptions").EnumerateArray());
        Assert.Empty(body.GetProperty("payments").EnumerateArray());
    }

    [Fact]
    public async Task Trialing_subscription_current_plan_maps_status_and_pending_fields()
    {
        await EnsureMvpCatalogAsync();
        var orgId = await CreateOrganizationAsync("trl");
        var subscriptionId = await StartBusinessTrialAsync(orgId);

        var response = await _admin.GetAsync(
            $"/api/v1/platform/organizations/{orgId}/current-plan?productCode={ProductCode.PinoyBusinessPos}");
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Trialing", body.GetProperty("currentSubscription").GetProperty("status").GetString());
        Assert.NotEqual(Guid.Empty, body.GetProperty("currentSubscription").GetProperty("id").GetGuid());
        Assert.Equal(subscriptionId, body.GetProperty("currentSubscription").GetProperty("id").GetGuid());
        Assert.True(body.GetProperty("pendingPlanChange").ValueKind is JsonValueKind.Null);
    }

    [Fact]
    public async Task Cross_org_current_plan_is_denied_for_org_member()
    {
        await EnsureMvpCatalogAsync();
        var orgA = await CreateOrganizationAsync("orga");
        var orgB = await CreateOrganizationAsync("orgb");
        var (_, _, email, password) = await SeedPersonalUserAsync("xorg");
        var token = await EnsurePersonalSessionTokenAsync(email, password);

        using var request = Authed(
            HttpMethod.Get,
            $"/api/v1/platform/organizations/{orgB}/current-plan?productCode={ProductCode.PinoyBusinessPos}",
            token);
        var response = await _client.SendAsync(request);
        Assert.True(
            response.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.NotFound or HttpStatusCode.Unauthorized,
            $"Expected denial, got {response.StatusCode}");
        _ = orgA;
    }

    [Fact]
    public async Task Admin_deserializes_commercial_summary_and_current_plan_dtos()
    {
        await EnsureMvpCatalogAsync();
        var orgId = await CreateOrganizationAsync("dto");
        await StartBusinessTrialAsync(orgId);

        var summaryJson = await (await _admin.GetAsync(
            $"/api/v1/platform/admin/organizations/{orgId}/commercial-summary")).Content.ReadAsStringAsync();
        var currentPlanJson = await (await _admin.GetAsync(
            $"/api/v1/platform/organizations/{orgId}/current-plan?productCode={ProductCode.PinoyBusinessPos}")).Content.ReadAsStringAsync();

        var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var summary = JsonSerializer.Deserialize<TestOrganizationCommercialSummaryDto>(summaryJson, jsonOptions);
        var currentPlan = JsonSerializer.Deserialize<TestOrganizationCurrentPlanDto>(currentPlanJson, jsonOptions);

        Assert.NotNull(summary);
        Assert.Equal(orgId, summary!.Organization.Id);
        Assert.NotEmpty(summary.Subscriptions);
        Assert.NotNull(currentPlan);
        Assert.Equal("Trialing", currentPlan!.CurrentSubscription?.Status);
        Assert.NotEmpty(currentPlan.AvailablePlans);
    }

    [Fact]
    public async Task From_catalog_starts_business_trial_for_empty_organization()
    {
        await EnsureMvpCatalogAsync();
        var orgId = await CreateOrganizationAsync("fctl");

        var response = await _admin.PostAsJsonAsync(
            $"/api/v1/platform/organizations/{orgId}/subscriptions/from-catalog",
            new
            {
                productCode = ProductCode.PinoyBusinessPos,
                planKey = MvpPosPlanCodes.Growth,
                billingCycle = "Monthly",
                startAsTrial = true,
                payNow = false,
                idempotencyKey = Guid.NewGuid().ToString("N")
            });
        if (!response.IsSuccessStatusCode)
        {
            Assert.Fail($"from-catalog failed ({response.StatusCode}): {await response.Content.ReadAsStringAsync()}");
        }

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Trialing", body.GetProperty("status").GetString());
        Assert.Equal(orgId, body.GetProperty("organizationId").GetGuid());
    }

    [Fact]
    public async Task From_catalog_cross_org_is_denied_for_unrelated_user()
    {
        await EnsureMvpCatalogAsync();
        var orgId = await CreateOrganizationAsync("fcxo");
        var (_, _, email, password) = await SeedPersonalUserAsync("fcxo");
        var token = await EnsurePersonalSessionTokenAsync(email, password);

        using var request = Authed(
            HttpMethod.Post,
            $"/api/v1/platform/organizations/{orgId}/subscriptions/from-catalog",
            token,
            new
            {
                productCode = ProductCode.PinoyBusinessPos,
                planKey = MvpPosPlanCodes.Growth,
                billingCycle = "Monthly",
                startAsTrial = true,
                payNow = false
            });
        var response = await _client.SendAsync(request);
        Assert.True(
            response.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.NotFound or HttpStatusCode.Unauthorized,
            $"Expected denial, got {response.StatusCode}");
    }

    private sealed record TestOrganizationDto(Guid Id, string DisplayName, string Slug, string Status);

    private sealed record TestSubscriptionDto(Guid Id, string Status);

    private sealed record TestPlanDto(Guid Id, string DisplayName);

    private sealed record TestOrganizationCommercialSummaryDto(
        TestOrganizationDto Organization,
        IReadOnlyList<TestSubscriptionDto> Subscriptions,
        IReadOnlyList<TestPaymentDto> Payments,
        IReadOnlyList<TestEntitlementLatestSummaryDto> LatestEntitlements);

    private sealed record TestPaymentDto(Guid Id);

    private sealed record TestEntitlementLatestSummaryDto(Guid Id);

    private sealed record TestOrganizationCurrentPlanDto(
        Guid OrganizationId,
        string ProductCode,
        TestSubscriptionDto? CurrentSubscription,
        TestPlanDto? CurrentPlan,
        IReadOnlyList<TestPlanDto> AvailablePlans);

    private async Task<Guid> StartBusinessTrialAsync(Guid organizationId)
    {
        var plans = await _admin.GetAsync(
            $"/api/v1/platform/catalog/plans?productCode={ProductCode.PinoyBusinessPos}&status=Active&pageSize=20");
        plans.EnsureSuccessStatusCode();
        var businessPlan = (await plans.Content.ReadFromJsonAsync<JsonElement>())!
            .GetProperty("items")
            .EnumerateArray()
            .First(p => string.Equals(p.GetProperty("planKey").GetString(), MvpPosPlanCodes.Growth, StringComparison.Ordinal));

        var trial = await _admin.PostAsJsonAsync(
            $"/api/v1/platform/organizations/{organizationId}/subscriptions/trials",
            new
            {
                planId = businessPlan.GetProperty("id").GetGuid(),
                planVersionId = (await GetPublishedVersionIdAsync(businessPlan.GetProperty("id").GetGuid())),
                trialDefinitionId = (await EnsureTrialDefinitionAsync())
            });
        trial.EnsureSuccessStatusCode();
        return (await trial.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
    }

    private async Task<Guid> GetPublishedVersionIdAsync(Guid planId)
    {
        var versions = await _admin.GetAsync(
            $"/api/v1/platform/catalog/products/{ProductCode.PinoyBusinessPos}/plans/{planId}/versions");
        versions.EnsureSuccessStatusCode();
        return (await versions.Content.ReadFromJsonAsync<JsonElement>())!
            .EnumerateArray()
            .First(v => string.Equals(v.GetProperty("status").GetString(), nameof(PlanVersionStatus.Published), StringComparison.Ordinal))
            .GetProperty("id")
            .GetGuid();
    }

    private async Task<Guid> EnsureTrialDefinitionAsync()
    {
        var trials = await _admin.GetAsync(
            $"/api/v1/platform/catalog/products/{ProductCode.PinoyBusinessPos}/trials");
        trials.EnsureSuccessStatusCode();
        var items = (await trials.Content.ReadFromJsonAsync<JsonElement>())!.EnumerateArray().ToList();
        if (items.Count > 0)
        {
            return items[0].GetProperty("id").GetGuid();
        }

        var createFeature = await _admin.PostAsJsonAsync(
            $"/api/v1/platform/catalog/products/{ProductCode.PinoyBusinessPos}/features",
            new
            {
                featureCode = FeatureCode.CustomerCreditView,
                displayName = "View Credit",
                valueType = nameof(FeatureValueType.Boolean)
            });
        if (createFeature.StatusCode != HttpStatusCode.Created
            && createFeature.StatusCode != HttpStatusCode.BadRequest)
        {
            createFeature.EnsureSuccessStatusCode();
        }

        var createTrial = await _admin.PostAsJsonAsync(
            $"/api/v1/platform/catalog/products/{ProductCode.PinoyBusinessPos}/trials",
            new
            {
                displayName = "MVP Trial",
                durationIso = "P14D",
                featureGrants = new[] { new { featureCode = FeatureCode.CustomerCreditView, enabled = true } }
            });
        if (createTrial.IsSuccessStatusCode)
        {
            return (await createTrial.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        }

        var retryList = await _admin.GetAsync(
            $"/api/v1/platform/catalog/products/{ProductCode.PinoyBusinessPos}/trials");
        retryList.EnsureSuccessStatusCode();
        var retryItems = (await retryList.Content.ReadFromJsonAsync<JsonElement>())!.EnumerateArray().ToList();
        if (retryItems.Count > 0)
        {
            return retryItems[0].GetProperty("id").GetGuid();
        }

        createTrial.EnsureSuccessStatusCode();
        return (await createTrial.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
    }

    private async Task<Guid> CreateOrganizationAsync(string prefix)
    {
        var response = await _admin.PostAsJsonAsync(
            "/api/v1/platform/organizations",
            new { displayName = $"{prefix} Org", slug = Unique(prefix) });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
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

        await provider.GetRequiredService<EnsureMvpPosPlans>().ExecuteAsync();
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

    private async Task<string> EnsurePersonalSessionTokenAsync(string email, string password)
    {
        var login = await _client.PostAsJsonAsync(
            "/api/v1/platform/auth/login",
            new { usernameOrEmail = email, password });
        login.EnsureSuccessStatusCode();
        var token = (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("sessionToken").GetString()!;

        using var profilesRequest = Authed(HttpMethod.Get, "/api/v1/platform/auth/account-profiles", token);
        var profilesResponse = await _client.SendAsync(profilesRequest);
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
        var selectResponse = await _client.SendAsync(selectRequest);
        selectResponse.EnsureSuccessStatusCode();
        return (await selectResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("sessionToken").GetString()!;
    }
}
