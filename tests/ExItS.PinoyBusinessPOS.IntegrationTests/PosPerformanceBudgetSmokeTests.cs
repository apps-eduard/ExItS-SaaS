using System.Diagnostics;
using System.Net.Http.Json;
using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Customers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace ExItS.PinoyBusinessPOS.IntegrationTests;

/// <summary>
/// P9-WP02: repeatable warm-path latency smoke against Testcontainers.
/// Volumes are intentionally scaled for CI; results are provisional engineering budgets, not SLAs.
/// </summary>
[Collection(PosPostgreSqlCollection.Name)]
public sealed class PosPerformanceBudgetSmokeTests(PosPostgreSqlFixture fixture)
{
    private static readonly Guid Org = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid Actor = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    /// <summary>Provisional p95 budgets (ms) for this constrained environment.</summary>
    public static class ProvisionalBudgetsMs
    {
        public const int CommonRead = 500;
        public const int SearchList = 750;
        public const int Dashboard = 1500;
    }

    [Fact]
    public async Task Warm_catalog_customer_and_dashboard_reads_stay_within_provisional_budgets()
    {
        await using var factory = new Factory(fixture.ConnectionString);
        var client = factory.CreateClient();

        // Seed a modest catalog + customers (scaled CI volume — not full MVP 5k/10k).
        for (var i = 0; i < 25; i++)
        {
            using var productReq = Scoped(HttpMethod.Post, "/api/v1/pos/catalog/products");
            productReq.Content = JsonContent.Create(new CreatePosCatalogProductRequest(
                $"Perf Product {i}",
                "Piece",
                10m + i,
                Sku: $"perf-{i:D4}"));
            using var productResp = await client.SendAsync(productReq);
            productResp.EnsureSuccessStatusCode();
        }

        for (var i = 0; i < 25; i++)
        {
            using var customerReq = Scoped(HttpMethod.Post, "/api/v1/pos/customers");
            customerReq.Content = JsonContent.Create(new CreatePosCustomerRequest(
                $"Perf Customer {i}",
                $"+63917{i:D7}",
                null,
                null));
            using var customerResp = await client.SendAsync(customerReq);
            customerResp.EnsureSuccessStatusCode();
        }

        // Warm-up
        await MeasureAsync(client, HttpMethod.Get, "/api/v1/pos/catalog/products?page=1&pageSize=20");
        await MeasureAsync(client, HttpMethod.Get, "/api/v1/pos/customers?page=1&pageSize=20");
        await MeasureAsync(client, HttpMethod.Get, "/api/v1/pos/dashboard");

        var catalog = await MeasureManyAsync(client, HttpMethod.Get, "/api/v1/pos/catalog/products?page=1&pageSize=20", 12);
        var search = await MeasureManyAsync(client, HttpMethod.Get, "/api/v1/pos/catalog/products?search=Perf&page=1&pageSize=20", 12);
        var customers = await MeasureManyAsync(client, HttpMethod.Get, "/api/v1/pos/customers?search=Perf&page=1&pageSize=20", 12);
        var dashboard = await MeasureManyAsync(client, HttpMethod.Get, "/api/v1/pos/dashboard", 8);

        Assert.True(catalog.P95Ms <= ProvisionalBudgetsMs.CommonRead,
            $"Catalog list p95={catalog.P95Ms}ms exceeded provisional {ProvisionalBudgetsMs.CommonRead}ms (env-limited).");
        Assert.True(search.P95Ms <= ProvisionalBudgetsMs.SearchList,
            $"Catalog search p95={search.P95Ms}ms exceeded provisional {ProvisionalBudgetsMs.SearchList}ms.");
        Assert.True(customers.P95Ms <= ProvisionalBudgetsMs.SearchList,
            $"Customer search p95={customers.P95Ms}ms exceeded provisional {ProvisionalBudgetsMs.SearchList}ms.");
        Assert.True(dashboard.P95Ms <= ProvisionalBudgetsMs.Dashboard,
            $"Dashboard p95={dashboard.P95Ms}ms exceeded provisional {ProvisionalBudgetsMs.Dashboard}ms.");
        Assert.Equal(0, catalog.Errors + search.Errors + customers.Errors + dashboard.Errors);
    }

    private static async Task<LatencySample> MeasureManyAsync(
        HttpClient client,
        HttpMethod method,
        string path,
        int iterations)
    {
        var samples = new List<long>(iterations);
        var errors = 0;
        for (var i = 0; i < iterations; i++)
        {
            var (ms, ok) = await MeasureAsync(client, method, path);
            samples.Add(ms);
            if (!ok)
            {
                errors++;
            }
        }

        samples.Sort();
        var p95Index = Math.Clamp((int)Math.Ceiling(samples.Count * 0.95) - 1, 0, samples.Count - 1);
        return new LatencySample(samples[samples.Count / 2], samples[p95Index], errors);
    }

    private static async Task<(long ElapsedMs, bool Ok)> MeasureAsync(
        HttpClient client,
        HttpMethod method,
        string path)
    {
        using var request = Scoped(method, path);
        var sw = Stopwatch.StartNew();
        using var response = await client.SendAsync(request);
        sw.Stop();
        return (sw.ElapsedMilliseconds, response.IsSuccessStatusCode);
    }

    private static HttpRequestMessage Scoped(HttpMethod method, string path)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.TryAddWithoutValidation(PosOrganizationHeaders.OrganizationHeaderName, Org.ToString("D"));
        request.Headers.TryAddWithoutValidation(PosOrganizationHeaders.ActorHeaderName, Actor.ToString("D"));
        return request;
    }

    private sealed record LatencySample(long MedianMs, long P95Ms, int Errors);

    private sealed class Factory(string connectionString) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("ConnectionStrings:PosDatabase", connectionString);
            builder.ConfigureAppConfiguration((_, config) =>
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:PosDatabase"] = connectionString
                }));
        }
    }
}
