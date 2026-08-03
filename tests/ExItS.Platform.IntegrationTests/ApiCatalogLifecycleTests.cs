using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ExItS.Platform.IntegrationTests.Support;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ExItS.Platform.IntegrationTests;

[Collection(PostgreSqlCollection.Name)]
public sealed class ApiCatalogLifecycleTests(PostgreSqlFixture fixture) : IAsyncLifetime
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

    [Fact]
    public async Task Product_and_plan_crud_search_lifecycle_and_concurrency()
    {
        var productCode = Unique("catp");
        var createProduct = await _admin.PostAsJsonAsync(
            "/api/v1/platform/catalog/products",
            new { code = productCode, displayName = "Catalog Product Alpha" });
        Assert.Equal(HttpStatusCode.Created, createProduct.StatusCode);
        var product = await createProduct.Content.ReadFromJsonAsync<JsonElement>();
        var productId = product.GetProperty("id").GetGuid();

        var list = await _admin.GetAsync(
            $"/api/v1/platform/catalog/products?search=Catalog%20Product%20Alpha&sortBy=Code&page=1&pageSize=20");
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        Assert.Contains(
            (await list.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("items").EnumerateArray(),
            i => i.GetProperty("code").GetString() == productCode);

        var rename = await _admin.PatchAsJsonAsync(
            $"/api/v1/platform/catalog/products/{productId}/rename",
            new
            {
                displayName = "Catalog Product Renamed",
                expectedUpdatedAtUtc = product.GetProperty("updatedAtUtc").GetDateTimeOffset()
            });
        Assert.Equal(HttpStatusCode.OK, rename.StatusCode);
        var renamed = await rename.Content.ReadFromJsonAsync<JsonElement>();

        var stale = await _admin.PatchAsJsonAsync(
            $"/api/v1/platform/catalog/products/{productId}/rename",
            new
            {
                displayName = "Stale",
                expectedUpdatedAtUtc = product.GetProperty("updatedAtUtc").GetDateTimeOffset()
            });
        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);

        var planCode = Unique("plan");
        var createPlan = await _admin.PostAsJsonAsync(
            $"/api/v1/platform/catalog/products/{productCode}/plans",
            new { code = planCode, displayName = "Starter Plan" });
        Assert.Equal(HttpStatusCode.Created, createPlan.StatusCode);
        var plan = await createPlan.Content.ReadFromJsonAsync<JsonElement>();
        var planId = plan.GetProperty("id").GetGuid();

        var plans = await _admin.GetAsync(
            $"/api/v1/platform/catalog/plans?productCode={productCode}&search=Starter&status=Draft");
        Assert.Equal(HttpStatusCode.OK, plans.StatusCode);
        Assert.True((await plans.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("totalCount").GetInt32() >= 1);

        var getPlan = await _admin.GetAsync($"/api/v1/platform/catalog/plans/{planId}");
        Assert.Equal(HttpStatusCode.OK, getPlan.StatusCode);

        var activatePlan = await _admin.PostAsync(
            $"/api/v1/platform/catalog/products/{productCode}/plans/{planId}/activate",
            null);
        Assert.Equal(HttpStatusCode.OK, activatePlan.StatusCode);
        Assert.Equal("Active", (await activatePlan.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("status").GetString());

        var deactivateProduct = await _admin.PostAsync(
            $"/api/v1/platform/catalog/products/{productId}/deactivate",
            null);
        Assert.Equal(HttpStatusCode.OK, deactivateProduct.StatusCode);
        Assert.Equal("Inactive", (await deactivateProduct.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("status").GetString());

        var activateProduct = await _admin.PostAsync(
            $"/api/v1/platform/catalog/products/{productId}/activate",
            null);
        Assert.Equal(HttpStatusCode.OK, activateProduct.StatusCode);

        var retirePlan = await _admin.PostAsync(
            $"/api/v1/platform/catalog/products/{productCode}/plans/{planId}/retire",
            null);
        Assert.Equal(HttpStatusCode.OK, retirePlan.StatusCode);
        Assert.Equal("Retired", (await retirePlan.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("status").GetString());

        var audit = await _admin.GetAsync(
            $"/api/v1/platform/audit?action=platform.catalog.product.created&page=1&pageSize=20");
        Assert.Equal(HttpStatusCode.OK, audit.StatusCode);
        Assert.True((await audit.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("totalCount").GetInt32() >= 1);

        _ = renamed;
    }

    [Fact]
    public async Task Organization_admin_cannot_mutate_catalog()
    {
        var productCode = Unique("deny");
        (await _admin.PostAsJsonAsync(
            "/api/v1/platform/catalog/products",
            new { code = productCode, displayName = "Deny Product" })).EnsureSuccessStatusCode();

        var org = await _admin.PostAsJsonAsync(
            "/api/v1/platform/organizations",
            new { displayName = "Catalog Deny Org", slug = Unique("cdo") });
        org.EnsureSuccessStatusCode();
        var organizationId = (await org.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var (userId, username, password) = await SeedUserAsync("cata");
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

        using (var create = Authed(
                   HttpMethod.Post,
                   "/api/v1/platform/catalog/products",
                   token,
                   new { code = Unique("hack"), displayName = "Hack" }))
        {
            var response = await _client.SendAsync(create);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        using (var list = Authed(HttpMethod.Get, "/api/v1/platform/catalog/products", token))
        {
            var response = await _client.SendAsync(list);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }
}
