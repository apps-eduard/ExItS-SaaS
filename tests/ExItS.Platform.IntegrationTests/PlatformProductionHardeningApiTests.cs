using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace ExItS.Platform.IntegrationTests;

/// <summary>P9-WP01: Production environment security guards for Platform API.</summary>
[Collection(PostgreSqlCollection.Name)]
public sealed class PlatformProductionHardeningApiTests(PostgreSqlFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task Production_ignores_dev_platform_user_header_and_denies_mutations()
    {
        await using var factory = new HardeningFactory(fixture.ConnectionString, "Production");
        var client = factory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/platform/organizations");
        request.Headers.TryAddWithoutValidation(
            "X-Dev-Platform-User-Id",
            Guid.NewGuid().ToString("D"));
        request.Content = JsonContent.Create(new { displayName = "Should Fail", slug = "should-fail" });

        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("stackTrace", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Password=", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Production_health_and_root_remain_available()
    {
        await using var factory = new HardeningFactory(fixture.ConnectionString, "Production");
        var client = factory.CreateClient();

        using var health = await client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, health.StatusCode);

        using var root = await client.GetAsync("/");
        Assert.Equal(HttpStatusCode.OK, root.StatusCode);
        var json = await root.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.Equal("P10-WP08-phase-10-closeout", json.GetProperty("phase").GetString());
    }

    [Fact]
    public void Production_startup_fails_when_allowed_hosts_is_wildcard()
    {
        using var factory = new HardeningFactory(
            fixture.ConnectionString,
            "Production",
            allowedHosts: "*");

        var ex = Assert.ThrowsAny<Exception>(() => factory.CreateClient());
        Assert.Contains("AllowedHosts", ex.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Production_startup_fails_when_dev_password_present()
    {
        using var factory = new HardeningFactory(
            "Host=127.0.0.1;Port=5434;Database=ExItS_Platform;Username=postgres;Password=exits_platform_dev_only",
            "Production",
            allowedHosts: "localhost");

        var ex = Assert.ThrowsAny<Exception>(() => factory.CreateClient());
        Assert.Contains("development database password", ex.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    private sealed class HardeningFactory(
        string connectionString,
        string environmentName,
        string? allowedHosts = null) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment(environmentName);
            builder.UseSetting("Security:EnforceHttps", "false");

            var hosts = allowedHosts
                        ?? (string.Equals(environmentName, "Production", StringComparison.OrdinalIgnoreCase)
                            ? "localhost;test"
                            : null);

            var values = new Dictionary<string, string?>
            {
                ["ConnectionStrings:PlatformDatabase"] = connectionString,
                ["Security:EnforceHttps"] = "false"
            };

            if (hosts is not null)
            {
                builder.UseSetting("AllowedHosts", hosts);
                values["AllowedHosts"] = hosts;
            }

            builder.UseSetting("ConnectionStrings:PlatformDatabase", connectionString);
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(values));
        }
    }
}
