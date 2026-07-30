using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Application.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace ExItS.PinoyBusinessPOS.IntegrationTests;

/// <summary>P9-WP01: Production environment security guards for POS API.</summary>
[Collection(PosPostgreSqlCollection.Name)]
public sealed class PosProductionHardeningApiTests(PosPostgreSqlFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly Guid OrgA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    [Fact]
    public async Task Production_rejects_organization_and_actor_headers()
    {
        await using var factory = new HardeningFactory(fixture.ConnectionString, "Production");
        var client = factory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/pos/customers");
        request.Headers.TryAddWithoutValidation(PosOrganizationHeaders.OrganizationHeaderName, OrgA.ToString("D"));
        request.Headers.TryAddWithoutValidation(PosOrganizationHeaders.ActorHeaderName, Guid.NewGuid().ToString("D"));
        request.Headers.TryAddWithoutValidation(PosCommercialHeaders.SubscriptionStatusHeaderName, "Active");
        request.Headers.TryAddWithoutValidation(
            PosCommercialHeaders.FeatureGrantsHeaderName,
            "store-catalog-view,customer-credit-view");

        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var code = problem.GetProperty("errorCode").GetString();
        Assert.True(
            code is ApplicationErrorCodes.CommercialAccessUnknown
                or ApplicationErrorCodes.DevelopmentHeadersUnavailable,
            $"Unexpected errorCode: {code}");
    }

    [Fact]
    public async Task Production_dev_offline_probe_route_is_not_available()
    {
        await using var factory = new HardeningFactory(fixture.ConnectionString, "Production");
        var client = factory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/pos/dev/offline-probe");
        request.Headers.TryAddWithoutValidation(PosOrganizationHeaders.OrganizationHeaderName, OrgA.ToString("D"));
        request.Content = JsonContent.Create(new
        {
            operationId = Guid.NewGuid(),
            idempotencyKey = "k",
            payloadHash = "h",
            echoToken = "e"
        });

        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Production_health_remains_available_without_dev_headers()
    {
        await using var factory = new HardeningFactory(fixture.ConnectionString, "Production");
        var client = factory.CreateClient();
        using var response = await client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.Contains("X-Content-Type-Options")
                    || response.Headers.TryGetValues("X-Content-Type-Options", out _));
    }

    [Fact]
    public async Task Production_problem_details_omit_stack_traces_on_business_denial()
    {
        await using var factory = new HardeningFactory(fixture.ConnectionString, "Production");
        var client = factory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/pos/customers");
        request.Headers.TryAddWithoutValidation(PosOrganizationHeaders.OrganizationHeaderName, OrgA.ToString("D"));
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("at ExItS.", body, StringComparison.Ordinal);
        Assert.DoesNotContain("stackTrace", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Npgsql", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Password=", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Production_startup_fails_when_allowed_hosts_is_wildcard()
    {
        using var factory = new HardeningFactory(
            fixture.ConnectionString,
            "Production",
            allowedHosts: "*",
            skipDefaultAllowedHostsOverride: true);

        var ex = Assert.ThrowsAny<Exception>(() => factory.CreateClient());
        Assert.Contains("AllowedHosts", ex.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Production_startup_fails_when_connection_string_uses_dev_password()
    {
        using var factory = new HardeningFactory(
            "Host=127.0.0.1;Port=5434;Database=ExItS_PinoyBusinessPOS;Username=postgres;Password=exits_platform_dev_only",
            "Production",
            allowedHosts: "localhost");

        var ex = Assert.ThrowsAny<Exception>(() => factory.CreateClient());
        Assert.Contains("development database password", ex.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Production_startup_fails_when_connection_string_missing()
    {
        using var factory = new HardeningFactory(
            connectionString: " ",
            environmentName: "Production",
            allowedHosts: "localhost");

        var ex = Assert.ThrowsAny<Exception>(() => factory.CreateClient());
        Assert.Contains("PosDatabase", ex.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    private sealed class HardeningFactory(
        string connectionString,
        string environmentName,
        string? allowedHosts = null,
        bool skipDefaultAllowedHostsOverride = false) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment(environmentName);
            builder.UseSetting("Security:EnforceHttps", "false");

            var hosts = allowedHosts
                        ?? (string.Equals(environmentName, "Production", StringComparison.OrdinalIgnoreCase)
                            && !skipDefaultAllowedHostsOverride
                            ? "localhost;test"
                            : null);

            var values = new Dictionary<string, string?>
            {
                ["ConnectionStrings:PosDatabase"] = connectionString,
                ["Security:EnforceHttps"] = "false"
            };

            if (hosts is not null)
            {
                builder.UseSetting("AllowedHosts", hosts);
                values["AllowedHosts"] = hosts;
            }
            else if (skipDefaultAllowedHostsOverride)
            {
                builder.UseSetting("AllowedHosts", "*");
                values["AllowedHosts"] = "*";
            }

            builder.UseSetting("ConnectionStrings:PosDatabase", connectionString);
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(values));
        }
    }
}
