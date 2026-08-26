using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ExItS.Platform.Domain.Authorization;
using ExItS.Platform.Infrastructure.Authorization;
using ExItS.Platform.IntegrationTests.Support;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;

namespace ExItS.Platform.IntegrationTests;

[Collection(PostgreSqlCollection.Name)]
public sealed class ApiBillingActionCenterTests(PostgreSqlFixture fixture)
{
    private const string BillingSummaryRoute = "/api/v1/platform/admin/billing/summary";
    private const string BillingIssuesRoute = "/api/v1/platform/admin/billing/issues";
    private const string ActionCenterRoute = "/api/v1/platform/admin/action-center";

    [Fact]
    public async Task Billing_summary_requires_manage_manual_payments()
    {
        await using var factory = new Factory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var (userId, _, _) = await PlatformIntegrationTestUsers.CreatePlatformStaffWithPasswordAsync(
            client,
            "billv",
            nameof(PlatformSystemRole.PlatformAuditor));

        using var request = new HttpRequestMessage(HttpMethod.Get, BillingSummaryRoute);
        request.Headers.Add(DevelopmentPlatformActorAccessor.DevPlatformUserIdHeader, userId.ToString("D"));
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Billing_summary_returns_authoritative_counts_for_development_operator()
    {
        await using var factory = new Factory(fixture.ConnectionString);
        var client = factory.CreateClient();

        using var response = await client.GetAsync(BillingSummaryRoute);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(json.TryGetProperty("pendingPaymentCount", out _));
        Assert.True(json.TryGetProperty("pastDueSubscriptionCount", out _));
    }

    [Fact]
    public async Task Billing_issues_returns_paged_rows()
    {
        await using var factory = new Factory(fixture.ConnectionString);
        var client = factory.CreateClient();

        using var response = await client.GetAsync(BillingIssuesRoute);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(json.TryGetProperty("items", out _));
        Assert.True(json.TryGetProperty("totalCount", out _));
    }

    [Fact]
    public async Task Action_center_returns_items_for_portfolio_reader()
    {
        await using var factory = new Factory(fixture.ConnectionString);
        var client = factory.CreateClient();

        using var response = await client.GetAsync(ActionCenterRoute);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(json.TryGetProperty("items", out var items));
        Assert.Equal(JsonValueKind.Array, items.ValueKind);
    }

    [Fact]
    public async Task Action_center_fails_closed_for_unauthorized_actor()
    {
        await using var factory = new Factory(fixture.ConnectionString);
        var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, ActionCenterRoute);
        request.Headers.Add(DevelopmentPlatformActorAccessor.DevPlatformUserIdHeader, Guid.NewGuid().ToString("D"));
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
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
