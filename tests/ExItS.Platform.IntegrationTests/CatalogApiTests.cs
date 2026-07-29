using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ExItS.Platform.Domain.Catalog;
using ExItS.Platform.Domain.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace ExItS.Platform.IntegrationTests;

[Collection(PostgreSqlCollection.Name)]
public sealed class CatalogApiTests(PostgreSqlFixture fixture) : IAsyncLifetime
{
    private CatalogApiFactory _factory = null!;
    private HttpClient _client = null!;

    public Task InitializeAsync()
    {
        _factory = new CatalogApiFactory(fixture.ConnectionString);
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
    public async Task Create_and_get_product_round_trips()
    {
        var code = $"api-pos-{Guid.NewGuid():N}"[..20];
        var create = await _client.PostAsJsonAsync(
            "/api/v1/platform/catalog/products",
            new { code, displayName = "API POS" });

        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var created = await create.Content.ReadFromJsonAsync<JsonElement>();
        var id = created.GetProperty("id").GetGuid();

        var get = await _client.GetAsync($"/api/v1/platform/catalog/products/{id}");
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
        var product = await get.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(code, product.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Duplicate_product_code_returns_409()
    {
        var code = $"dup-api-{Guid.NewGuid():N}"[..20];
        await _client.PostAsJsonAsync(
            "/api/v1/platform/catalog/products",
            new { code, displayName = "Dup One" });

        var duplicate = await _client.PostAsJsonAsync(
            "/api/v1/platform/catalog/products",
            new { code, displayName = "Dup Two" });

        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
    }

    [Fact]
    public async Task Catalog_flow_create_feature_plan_version_grant_publish_blocks_mutation()
    {
        var productCode = $"api-flow-{Guid.NewGuid():N}"[..24];
        var product = await _client.PostAsJsonAsync(
            "/api/v1/platform/catalog/products",
            new { code = productCode, displayName = "POS" });
        product.EnsureSuccessStatusCode();

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
            new { code = "utang", displayName = "Utang" });
        plan.EnsureSuccessStatusCode();
        var planId = (await plan.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var draft = await _client.PostAsJsonAsync(
            $"/api/v1/platform/catalog/products/{productCode}/plans/{planId}/versions/draft",
            new
            {
                versionNumber = 1,
                billingPeriod = nameof(BillingPeriod.Monthly),
                trialEligible = true,
                grants = new[]
                {
                    new { featureCode = FeatureCode.CustomerCreditView, enabled = true }
                }
            });
        draft.EnsureSuccessStatusCode();

        var grant = await _client.PutAsJsonAsync(
            $"/api/v1/platform/catalog/products/{productCode}/plans/{planId}/versions/1/feature-grants/{FeatureCode.CustomerCreditView}",
            new { featureCode = FeatureCode.CustomerCreditView, enabled = true });
        grant.EnsureSuccessStatusCode();

        var publish = await _client.PostAsync(
            $"/api/v1/platform/catalog/products/{productCode}/plans/{planId}/versions/1/publish",
            null);
        publish.EnsureSuccessStatusCode();

        var blocked = await _client.PutAsJsonAsync(
            $"/api/v1/platform/catalog/products/{productCode}/plans/{planId}/versions/1/feature-grants/{FeatureCode.CustomerCreditView}",
            new { featureCode = FeatureCode.CustomerCreditView, enabled = false });

        Assert.Equal(HttpStatusCode.BadRequest, blocked.StatusCode);
        var body = await blocked.Content.ReadAsStringAsync();
        Assert.Contains(DomainErrorCodes.PlanVersionImmutable, body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Program_and_catalog_routes_exclude_subscription_payment_and_gcash()
    {
        var root = FindRepositoryRoot();
        var program = File.ReadAllText(Path.Combine(root, "src", "Platform", "ExItS.Platform.Api", "Program.cs"));
        var catalog = File.ReadAllText(Path.Combine(root, "src", "Platform", "ExItS.Platform.Api", "Catalog", "CatalogEndpoints.cs"));
        var sources = program + catalog;

        Assert.DoesNotContain("subscriptions", sources, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("payment", sources, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("gcash", sources, StringComparison.OrdinalIgnoreCase);

        var response = await _client.GetAsync("/api/v1/platform/subscriptions");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static string FindRepositoryRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "ExItS.slnx")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root.");
    }

    private sealed class CatalogApiFactory(string connectionString) : WebApplicationFactory<Program>
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
