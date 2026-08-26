using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.GlobalCatalog;
using ExItS.Platform.Infrastructure;
using ExItS.Platform.IntegrationTests.Support;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ExItS.Platform.IntegrationTests;

[Collection(PostgreSqlCollection.Name)]
public sealed class ApiProductAccessNavigationTests(PostgreSqlFixture fixture) : IAsyncLifetime
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
        $"{prefix}-{Guid.NewGuid():N}"[..Math.Min(32, prefix.Length + 1 + 32)].ToLowerInvariant();

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

    private async Task EnsurePhilippineStarterCatalogAsync()
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
        services.AddScoped<EnsurePhilippinePosStarterCatalog>();
        await using var provider = services.BuildServiceProvider();
        await provider.GetRequiredService<EnsurePhilippinePosStarterCatalog>().ExecuteAsync();
    }

    private async Task<Guid> ResolvePrimaryBusinessTypeIdAsync(string personalToken)
    {
        using var businessTypesRequest = Authed(
            HttpMethod.Get,
            "/api/v1/personal/onboarding/business-types",
            personalToken);
        var businessTypesResponse = await _client.SendAsync(businessTypesRequest);
        businessTypesResponse.EnsureSuccessStatusCode();
        var businessTypes = await businessTypesResponse.Content.ReadFromJsonAsync<JsonElement>();
        return businessTypes.EnumerateArray().First().GetProperty("id").GetGuid();
    }

    private async Task<(string Token, Guid UserId, Guid OrgId)> StartBusinessAsync()
    {
        await EnsurePhilippineStarterCatalogAsync();
        var (userId, email, password) = await PlatformIntegrationTestUsers.RegisterPersonalWithPasswordAsync(_client, "nav");

        var login = await _client.PostAsJsonAsync(
            "/api/v1/platform/auth/login",
            new { usernameOrEmail = email, password });
        login.EnsureSuccessStatusCode();
        var personalToken = (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("sessionToken").GetString()!;
        var primaryBusinessTypeId = await ResolvePrimaryBusinessTypeIdAsync(personalToken);

        using var start = Authed(
            HttpMethod.Post,
            "/api/v1/personal/start-business",
            personalToken,
            new
            {
                displayName = "Nav Store",
                slug = Unique("navbiz"),
                primaryBusinessTypeId,
                activatePosEntitlement = true,
                activateProductAccess = true,
                assignPosOwnerRole = true
            });
        var response = await _client.SendAsync(start);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return (
            body.GetProperty("sessionToken").GetString()!,
            userId,
            body.GetProperty("organizationId").GetGuid());
    }

    [Fact]
    public async Task Enabled_products_discovery_and_launch_for_organization_owner()
    {
        var (token, _, orgId) = await StartBusinessAsync();

        using var discover = Authed(HttpMethod.Get, $"/api/v1/organizations/{orgId}/enabled-products", token);
        var discovery = await _client.SendAsync(discover);
        Assert.Equal(HttpStatusCode.OK, discovery.StatusCode);
        var products = await discovery.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(JsonValueKind.Array, products.ValueKind);
        Assert.True(products.GetArrayLength() >= 1);
        var first = products[0];
        Assert.Equal("pinoy-business-pos", first.GetProperty("productCode").GetString());
        Assert.True(first.GetProperty("canLaunch").GetBoolean());
        Assert.Equal("Owner", first.GetProperty("productLocalRoleCode").GetString());

        using var launch = Authed(HttpMethod.Post, $"/api/v1/organizations/{orgId}/products/pinoy-business-pos/launch", token);
        var launched = await _client.SendAsync(launch);
        Assert.Equal(HttpStatusCode.OK, launched.StatusCode);
        var launchBody = await launched.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(launchBody.GetProperty("canOperate").GetBoolean());
        Assert.Contains("product-entry", launchBody.GetProperty("launchPath").GetString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Authorization_separates_entitlement_from_role_and_denies_without_role()
    {
        var (token, ownerId, orgId) = await StartBusinessAsync();

        // Staff via invite accept (org-scoped identity), commercial access, no product-local role.
        var (staffId, _, _, _, _) =
            await PlatformIntegrationTestUsers.SeedStaffViaInvitationToOrgAsync(
                _admin, _client, orgId, "staff");
        (await _admin.PostAsJsonAsync(
            $"/api/v1/platform/organizations/{orgId}/product-access",
            new { userId = staffId, productCode = "pinoy-business-pos", grantedByActor = "dev-admin", reason = "staff access" }))
            .EnsureSuccessStatusCode();

        using var authz = Authed(
            HttpMethod.Get,
            $"/api/v1/organizations/{orgId}/product-authorization?productCode=pinoy-business-pos&userId={staffId:D}",
            token);
        var response = await _client.SendAsync(authz);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.GetProperty("entitlementAllowed").GetBoolean());
        Assert.True(body.GetProperty("productAccessAssigned").GetBoolean());
        Assert.False(body.GetProperty("productLocalRoleGranted").GetBoolean());
        Assert.False(body.GetProperty("canOperate").GetBoolean());
        Assert.Equal("product_local_role_missing", body.GetProperty("reasonCode").GetString());

        using var assign = Authed(
            HttpMethod.Post,
            $"/api/v1/organizations/{orgId}/product-local-roles",
            token,
            new { userIdentityId = staffId, productCode = "pinoy-business-pos", roleCode = "Cashier", reason = "register" });
        var assigned = await _client.SendAsync(assign);
        Assert.Equal(HttpStatusCode.Created, assigned.StatusCode);

        using var authz2 = Authed(
            HttpMethod.Get,
            $"/api/v1/organizations/{orgId}/product-authorization?productCode=pinoy-business-pos&userId={staffId:D}",
            token);
        var allowed = await (await _client.SendAsync(authz2)).Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(allowed.GetProperty("canOperate").GetBoolean());
        Assert.Equal("Cashier", allowed.GetProperty("productLocalRoleCode").GetString());
        Assert.Equal("Cashier", allowed.GetProperty("mappedPosRoleCode").GetString());

        _ = ownerId;
    }

    [Fact]
    public async Task Personal_session_cannot_discover_organization_products()
    {
        var (_, email, password) = await PlatformIntegrationTestUsers.RegisterPersonalWithPasswordAsync(_client, "pers");
        var login = await _client.PostAsJsonAsync(
            "/api/v1/platform/auth/login",
            new { usernameOrEmail = email, password });
        login.EnsureSuccessStatusCode();
        var token = (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("sessionToken").GetString()!;

        using var discover = Authed(
            HttpMethod.Get,
            $"/api/v1/organizations/{Guid.NewGuid():D}/enabled-products",
            token);
        var response = await _client.SendAsync(discover);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(ApplicationErrorCodes.AccountScopeDenied, problem.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task Revoking_product_local_role_blocks_launch()
    {
        var (token, userId, orgId) = await StartBusinessAsync();

        using var list = Authed(HttpMethod.Get, $"/api/v1/organizations/{orgId}/product-local-roles?status=Active", token);
        var listed = await _client.SendAsync(list);
        listed.EnsureSuccessStatusCode();
        var grants = await listed.Content.ReadFromJsonAsync<JsonElement>();
        var grantId = grants[0].GetProperty("id").GetGuid();

        using var revoke = Authed(
            HttpMethod.Post,
            $"/api/v1/organizations/{orgId}/product-local-roles/{grantId}/revoke",
            token,
            new { reason = "test revoke" });
        (await _client.SendAsync(revoke)).EnsureSuccessStatusCode();

        using var launch = Authed(HttpMethod.Post, $"/api/v1/organizations/{orgId}/products/pinoy-business-pos/launch", token);
        var denied = await _client.SendAsync(launch);
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        var problem = await denied.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(ApplicationErrorCodes.ProductLocalRoleMissing, problem.GetProperty("errorCode").GetString());

        using var authz = Authed(
            HttpMethod.Get,
            $"/api/v1/organizations/{orgId}/product-authorization?productCode=pinoy-business-pos&userId={userId:D}",
            token);
        var body = await (await _client.SendAsync(authz)).Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.GetProperty("entitlementAllowed").GetBoolean());
        Assert.False(body.GetProperty("canOperate").GetBoolean());
    }
}
