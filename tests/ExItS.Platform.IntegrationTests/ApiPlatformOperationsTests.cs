using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ExItS.Platform.Application.Operations;
using ExItS.Platform.Domain.Authorization;
using ExItS.Platform.Infrastructure.Authorization;
using ExItS.Platform.IntegrationTests.Support;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;

namespace ExItS.Platform.IntegrationTests;

[Collection(PostgreSqlCollection.Name)]
public sealed class ApiPlatformOperationsTests(PostgreSqlFixture fixture)
{
    private const string UsageLimitsRoute = "/api/v1/platform/operations/usage-limits";
    private const string SupportLookupRoute = "/api/v1/platform/operations/support/lookup";
    private const string JobsRoute = "/api/v1/platform/operations/jobs";

    [Fact]
    public async Task Usage_limits_returns_paged_rows_for_authorized_portfolio_reader()
    {
        await using var factory = new Factory(fixture.ConnectionString);
        var client = factory.CreateClient();

        using var response = await client.GetAsync(UsageLimitsRoute);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(json.TryGetProperty("items", out var items));
        Assert.Equal(JsonValueKind.Array, items.ValueKind);
        Assert.True(json.TryGetProperty("totalCount", out _));
    }

    [Fact]
    public async Task Usage_limits_fails_closed_for_unauthorized_actor()
    {
        await using var factory = new Factory(fixture.ConnectionString);
        var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, UsageLimitsRoute);
        request.Headers.Add(DevelopmentPlatformActorAccessor.DevPlatformUserIdHeader, Guid.NewGuid().ToString("D"));
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Support_lookup_requires_platform_administrator()
    {
        await using var factory = new Factory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var (userId, _, _) = await PlatformIntegrationTestUsers.CreatePlatformStaffWithPasswordAsync(
            client,
            "opsup",
            nameof(PlatformSystemRole.PlatformSupport));

        using var request = new HttpRequestMessage(HttpMethod.Post, SupportLookupRoute);
        request.Headers.Add(DevelopmentPlatformActorAccessor.DevPlatformUserIdHeader, userId.ToString("D"));
        request.Content = JsonContent.Create(new { mode = PlatformSupportLookupModes.Organization, query = "missing" });
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Support_lookup_rejects_invalid_mode()
    {
        await using var factory = new SessionApiFactory(fixture.ConnectionString);
        var admin = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        var (_, username, password) = await PlatformIntegrationTestUsers.CreatePlatformStaffWithPasswordAsync(
            admin,
            "opsadm",
            nameof(PlatformSystemRole.PlatformAdministrator));
        var login = await client.PostAsJsonAsync(
            "/api/v1/platform/auth/login",
            new { usernameOrEmail = username, password });
        login.EnsureSuccessStatusCode();
        var token = (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("sessionToken").GetString();
        Assert.False(string.IsNullOrWhiteSpace(token));

        using var request = new HttpRequestMessage(HttpMethod.Post, SupportLookupRoute);
        request.Headers.Add("X-ExItS-Session-Token", token);
        request.Content = JsonContent.Create(new { mode = "invalid-mode", query = "test" });
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Background_jobs_list_requires_platform_administrator_for_support_role()
    {
        await using var factory = new Factory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var (userId, _, _) = await PlatformIntegrationTestUsers.CreatePlatformStaffWithPasswordAsync(
            client,
            "opsjb",
            nameof(PlatformSystemRole.PlatformSupport));

        using var request = new HttpRequestMessage(HttpMethod.Get, JobsRoute);
        request.Headers.Add(DevelopmentPlatformActorAccessor.DevPlatformUserIdHeader, userId.ToString("D"));
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Background_jobs_list_returns_catalog_import_source_rows()
    {
        await using var factory = new Factory(fixture.ConnectionString);
        var client = factory.CreateClient();

        using var response = await client.GetAsync(JobsRoute);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(json.TryGetProperty("items", out var items));
        Assert.Equal(JsonValueKind.Array, items.ValueKind);
        if (items.GetArrayLength() > 0)
        {
            var first = items[0];
            Assert.Equal(PlatformBackgroundJobSources.CatalogImport, first.GetProperty("source").GetString());
        }
    }

    private sealed class Factory : WebApplicationFactory<Program>
    {
        private readonly string _connectionString;

        public Factory(string connectionString) => _connectionString = connectionString;

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("ConnectionStrings:PlatformDatabase", _connectionString);
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:PlatformDatabase"] = _connectionString
                });
            });
        }
    }
}
