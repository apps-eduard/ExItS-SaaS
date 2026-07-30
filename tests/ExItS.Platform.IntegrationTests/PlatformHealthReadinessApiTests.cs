using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace ExItS.Platform.IntegrationTests;

/// <summary>P9-WP02: Platform liveness vs readiness.</summary>
[Collection(PostgreSqlCollection.Name)]
public sealed class PlatformHealthReadinessApiTests(PostgreSqlFixture fixture)
{
    [Fact]
    public async Task Liveness_and_readiness_succeed_when_database_is_up()
    {
        await using var factory = new Factory(fixture.ConnectionString);
        var client = factory.CreateClient();

        using var live = await client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, live.StatusCode);

        using var ready = await client.GetAsync("/health/ready");
        Assert.Equal(HttpStatusCode.OK, ready.StatusCode);
        var body = await ready.Content.ReadAsStringAsync();
        Assert.DoesNotContain("Password=", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(fixture.ConnectionString, body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Readiness_fails_without_leaking_secrets_when_database_is_down()
    {
        await using var factory = new Factory(
            "Host=127.0.0.1;Port=1;Database=ExItS_Missing;Username=postgres;Password=platform_secret_value");
        var client = factory.CreateClient();

        using var ready = await client.GetAsync("/health/ready");
        Assert.Equal(HttpStatusCode.ServiceUnavailable, ready.StatusCode);
        var body = await ready.Content.ReadAsStringAsync();
        Assert.DoesNotContain("platform_secret_value", body, StringComparison.Ordinal);

        using var live = await client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, live.StatusCode);
    }

    private sealed class Factory(string connectionString) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("ConnectionStrings:PlatformDatabase", connectionString);
            builder.ConfigureAppConfiguration((_, config) =>
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:PlatformDatabase"] = connectionString
                }));
        }
    }
}
