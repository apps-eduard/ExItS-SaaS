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
using Microsoft.Extensions.DependencyInjection;

namespace ExItS.Platform.IntegrationTests;

[Collection(PostgreSqlCollection.Name)]
public sealed class ApiSystemHealthTests(PostgreSqlFixture fixture)
{
    private const string Route = "/api/v1/platform/operations/system-health";

    [Fact]
    public async Task Authorized_request_returns_structured_health()
    {
        await using var factory = new Factory(fixture.ConnectionString);
        var client = factory.CreateClient();

        using var response = await client.GetAsync(Route);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(json.TryGetProperty("overallStatus", out var overall));
        Assert.False(string.IsNullOrWhiteSpace(overall.GetString()));

        var services = json.GetProperty("services").EnumerateArray().ToDictionary(
            s => s.GetProperty("name").GetString()!,
            s => s,
            StringComparer.Ordinal);
        Assert.Contains(SystemHealthServiceNames.PlatformApi, services.Keys);
        Assert.Contains(SystemHealthServiceNames.PosApi, services.Keys);
        Assert.Contains(SystemHealthServiceNames.PlatformDatabase, services.Keys);
        Assert.Contains(SystemHealthServiceNames.PosDatabase, services.Keys);

        Assert.Equal(SystemHealthStatuses.Healthy, services[SystemHealthServiceNames.PlatformApi].GetProperty("status").GetString());
        Assert.Equal(SystemHealthStatuses.Healthy, services[SystemHealthServiceNames.PlatformDatabase].GetProperty("status").GetString());
        Assert.Equal(SystemHealthStatuses.Unavailable, services[SystemHealthServiceNames.PosApi].GetProperty("status").GetString());
        Assert.Equal(SystemHealthStatuses.Unavailable, services[SystemHealthServiceNames.PosDatabase].GetProperty("status").GetString());
        Assert.Equal(SystemHealthStatuses.Degraded, overall.GetString());

        var host = json.GetProperty("host");
        Assert.True(host.TryGetProperty("cpuPercent", out _));
        Assert.True(host.TryGetProperty("memoryUsedBytes", out _));
        Assert.True(host.TryGetProperty("memoryTotalBytes", out _));
        Assert.True(host.TryGetProperty("storageUsedBytes", out _));
        Assert.True(host.TryGetProperty("storageFreeBytes", out _));
        Assert.True(host.TryGetProperty("storageTotalBytes", out _));
        Assert.True(host.TryGetProperty("uptimeSeconds", out _));

        var build = json.GetProperty("build");
        Assert.False(string.IsNullOrWhiteSpace(build.GetProperty("environment").GetString()));

        var backup = json.GetProperty("backup");
        Assert.Equal(SystemHealthStatuses.NotAvailable, backup.GetProperty("status").GetString());
        Assert.Equal(JsonValueKind.Null, backup.GetProperty("lastSuccessfulAtUtc").ValueKind);
        Assert.Equal(JsonValueKind.Null, backup.GetProperty("ageSeconds").ValueKind);
    }

    [Fact]
    public async Task Unauthorized_request_fails_closed()
    {
        await using var factory = new Factory(fixture.ConnectionString);
        var client = factory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Get, Route);
        request.Headers.Add(
            DevelopmentPlatformActorAccessor.DevPlatformUserIdHeader,
            Guid.NewGuid().ToString("D"));
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Support_role_with_view_portfolio_is_authorized()
    {
        await using var factory = new Factory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var (userId, _, _) = await PlatformIntegrationTestUsers.CreatePlatformStaffWithPasswordAsync(
            client,
            "syshs",
            nameof(PlatformSystemRole.PlatformSupport));

        using var request = new HttpRequestMessage(HttpMethod.Get, Route);
        request.Headers.Add(DevelopmentPlatformActorAccessor.DevPlatformUserIdHeader, userId.ToString("D"));
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Response_redacts_secrets_and_does_not_leak_environment_or_docker()
    {
        await using var factory = new Factory(
            fixture.ConnectionString,
            posBaseUrl: "http://127.0.0.1:1");
        var client = factory.CreateClient();
        using var response = await client.GetAsync(Route);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();

        Assert.DoesNotContain("Password=", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(fixture.ConnectionString, body, StringComparison.Ordinal);
        Assert.DoesNotContain("exits_platform_dev_only", body, StringComparison.Ordinal);
        Assert.DoesNotContain("docker.sock", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DOCKER", body, StringComparison.Ordinal);
        Assert.DoesNotContain("ConnectionStrings", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("SupportApiKey", body, StringComparison.Ordinal);
        Assert.DoesNotContain("dev-platform-support-key", body, StringComparison.Ordinal);
        Assert.DoesNotContain("BEGIN PRIVATE KEY", body, StringComparison.Ordinal);
        Assert.DoesNotContain("stackTrace", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Platform_database_unhealthy_is_reflected_truthfully()
    {
        await using var factory = new Factory(
            "Host=127.0.0.1;Port=1;Database=ExItS_Missing;Username=postgres;Password=platform_secret_value");
        var client = factory.CreateClient();
        using var response = await client.GetAsync(Route);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var services = json.GetProperty("services").EnumerateArray()
            .ToDictionary(s => s.GetProperty("name").GetString()!, s => s.GetProperty("status").GetString()!);
        Assert.Equal(SystemHealthStatuses.Healthy, services[SystemHealthServiceNames.PlatformApi]);
        Assert.Equal(SystemHealthStatuses.Unhealthy, services[SystemHealthServiceNames.PlatformDatabase]);
        Assert.Equal(SystemHealthStatuses.Unhealthy, json.GetProperty("overallStatus").GetString());

        var body = json.GetRawText();
        Assert.DoesNotContain("platform_secret_value", body, StringComparison.Ordinal);
        Assert.DoesNotContain("Password=", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Host_metric_failures_do_not_crash_endpoint()
    {
        await using var factory = new Factory(fixture.ConnectionString, throwHostMetrics: true);
        var client = factory.CreateClient();
        using var response = await client.GetAsync(Route);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var host = json.GetProperty("host");
        Assert.Equal(JsonValueKind.Null, host.GetProperty("cpuPercent").ValueKind);
        Assert.Equal(JsonValueKind.Null, host.GetProperty("memoryUsedBytes").ValueKind);
        Assert.Equal(SystemHealthStatuses.Healthy, FindService(json, SystemHealthServiceNames.PlatformApi));
    }

    [Fact]
    public async Task Degraded_pos_api_is_reflected_without_inferring_database()
    {
        await using var factory = new Factory(fixture.ConnectionString, posProbe: new StubPosProbe(
            live: SystemHealthStatuses.Degraded,
            ready: SystemHealthStatuses.Healthy));
        var client = factory.CreateClient();
        using var response = await client.GetAsync(Route);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(SystemHealthStatuses.Degraded, FindService(json, SystemHealthServiceNames.PosApi));
        Assert.Equal(SystemHealthStatuses.Healthy, FindService(json, SystemHealthServiceNames.PosDatabase));
        Assert.Equal(SystemHealthStatuses.Degraded, json.GetProperty("overallStatus").GetString());
    }

    private static string? FindService(JsonElement json, string name) =>
        json.GetProperty("services").EnumerateArray()
            .First(s => s.GetProperty("name").GetString() == name)
            .GetProperty("status")
            .GetString();

    private sealed class Factory : WebApplicationFactory<Program>
    {
        private readonly string _connectionString;
        private readonly string? _posBaseUrl;
        private readonly bool _throwHostMetrics;
        private readonly IPosHealthProbe? _posProbe;

        public Factory(
            string connectionString,
            string? posBaseUrl = null,
            bool throwHostMetrics = false,
            IPosHealthProbe? posProbe = null)
        {
            _connectionString = connectionString;
            _posBaseUrl = posBaseUrl;
            _throwHostMetrics = throwHostMetrics;
            _posProbe = posProbe;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("ConnectionStrings:PlatformDatabase", _connectionString);
            builder.ConfigureAppConfiguration((_, config) =>
            {
                var values = new Dictionary<string, string?>
                {
                    ["ConnectionStrings:PlatformDatabase"] = _connectionString
                };
                if (_posBaseUrl is not null)
                {
                    values["PosProductApi:BaseUrl"] = _posBaseUrl;
                    values["PosProductApi:SupportApiKey"] = "should-never-appear";
                }

                config.AddInMemoryCollection(values);
            });
            builder.ConfigureTestServices(services =>
            {
                if (_throwHostMetrics)
                {
                    services.AddSingleton<IHostResourceMetrics, ThrowingHostMetrics>();
                }

                if (_posProbe is not null)
                {
                    services.AddSingleton<IPosHealthProbe>(_posProbe);
                }
            });
        }
    }

    private sealed class ThrowingHostMetrics : IHostResourceMetrics
    {
        public HostResourceSnapshot Capture() =>
            throw new InvalidOperationException("Password=should-not-leak /etc/secrets");
    }

    private sealed class StubPosProbe(string live, string ready) : IPosHealthProbe
    {
        public Task<ProbedDependencyHealth> ProbeLivenessAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new ProbedDependencyHealth(live, 12, DateTimeOffset.UtcNow));

        public Task<ProbedDependencyHealth> ProbeReadinessAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new ProbedDependencyHealth(ready, 18, DateTimeOffset.UtcNow));
    }
}
