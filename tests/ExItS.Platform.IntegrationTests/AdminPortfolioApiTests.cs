using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace ExItS.Platform.IntegrationTests;

[Collection(PostgreSqlCollection.Name)]
public sealed class AdminPortfolioApiTests(PostgreSqlFixture fixture) : IAsyncLifetime
{
    private AdminApiFactory _factory = null!;
    private HttpClient _client = null!;

    public Task InitializeAsync()
    {
        _factory = new AdminApiFactory(fixture.ConnectionString);
        _client = _factory.CreateClient();
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _client.Dispose();
        _factory.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Portfolio_summary_returns_counts_and_empty_partial_failures()
    {
        var response = await _client.GetAsync("/api/v1/platform/admin/portfolio-summary");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(json.TryGetProperty("activeProductCount", out _));
        Assert.True(json.TryGetProperty("organizationCount", out _));
        Assert.True(json.TryGetProperty("partialFailures", out var failures));
        Assert.Equal(JsonValueKind.Array, failures.ValueKind);
    }

    [Fact]
    public async Task Organizations_list_is_paginated()
    {
        var create = await _client.PostAsJsonAsync(
            "/api/v1/platform/organizations",
            new { displayName = "Admin Org", slug = $"admin-org-{Guid.NewGuid():N}"[..24] });
        create.EnsureSuccessStatusCode();

        var list = await _client.GetAsync("/api/v1/platform/organizations?page=1&pageSize=20");
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        var json = await list.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(json.GetProperty("totalCount").GetInt32() >= 1);
        Assert.Equal(JsonValueKind.Array, json.GetProperty("items").ValueKind);
    }

    [Fact]
    public async Task Product_overview_and_latest_entitlements_endpoints_respond()
    {
        var code = $"adm-{Guid.NewGuid():N}"[..16];
        var create = await _client.PostAsJsonAsync(
            "/api/v1/platform/catalog/products",
            new { code, displayName = "Admin Product" });
        create.EnsureSuccessStatusCode();

        var overview = await _client.GetAsync($"/api/v1/platform/admin/products/{code}/overview");
        Assert.Equal(HttpStatusCode.OK, overview.StatusCode);
        var overviewJson = await overview.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(code, overviewJson.GetProperty("product").GetProperty("code").GetString());

        var latest = await _client.GetAsync("/api/v1/platform/admin/entitlements/latest?page=1&pageSize=10");
        Assert.Equal(HttpStatusCode.OK, latest.StatusCode);
    }

    [Fact]
    public async Task Organization_commercial_summary_returns_404_for_unknown_org()
    {
        var response = await _client.GetAsync($"/api/v1/platform/admin/organizations/{Guid.NewGuid()}/commercial-summary");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private sealed class AdminApiFactory(string connectionString) : WebApplicationFactory<Program>
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
