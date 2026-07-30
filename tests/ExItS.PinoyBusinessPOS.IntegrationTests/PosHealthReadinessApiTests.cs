using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace ExItS.PinoyBusinessPOS.IntegrationTests;

/// <summary>P9-WP02: liveness vs readiness health endpoints.</summary>
[Collection(PosPostgreSqlCollection.Name)]
public sealed class PosHealthReadinessApiTests(PosPostgreSqlFixture fixture)
{
    [Fact]
    public async Task Liveness_is_ok_without_dev_headers()
    {
        await using var factory = new Factory(fixture.ConnectionString);
        var client = factory.CreateClient();
        using var response = await client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("Password=", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Host=", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Readiness_is_ok_when_database_is_reachable()
    {
        await using var factory = new Factory(fixture.ConnectionString);
        var client = factory.CreateClient();
        using var response = await client.GetAsync("/health/ready");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("Password=", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(fixture.ConnectionString, body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Readiness_fails_when_database_is_unreachable()
    {
        await using var factory = new Factory(
            "Host=127.0.0.1;Port=1;Database=ExItS_Missing;Username=postgres;Password=not_a_real_secret");
        var client = factory.CreateClient();
        using var response = await client.GetAsync("/health/ready");
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("not_a_real_secret", body, StringComparison.Ordinal);
        Assert.DoesNotContain("Password=", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Liveness_stays_ok_when_database_is_unreachable()
    {
        await using var factory = new Factory(
            "Host=127.0.0.1;Port=1;Database=ExItS_Missing;Username=postgres;Password=not_a_real_secret");
        var client = factory.CreateClient();
        using var response = await client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

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
