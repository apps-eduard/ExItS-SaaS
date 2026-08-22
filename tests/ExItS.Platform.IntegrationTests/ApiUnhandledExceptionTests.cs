using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Testcontainers.PostgreSql;
using Xunit;

namespace ExItS.Platform.IntegrationTests;

public sealed class ApiUnhandledExceptionTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    private UnhandledExceptionApiFactory _factory = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        _factory = new UnhandledExceptionApiFactory(_postgres.GetConnectionString());
        _client = _factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    [Fact]
    public async Task Unhandled_exception_returns_safe_problem_details_with_trace_and_correlation()
    {
        const string correlationId = "11111111-2222-3333-4444-555555555555";
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/platform/__test__/unhandled");
        request.Headers.Add("X-Correlation-Id", correlationId);

        using var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal(correlationId, response.Headers.GetValues("X-Correlation-Id").Single());

        var body = await response.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(body);
        var root = json.RootElement;

        Assert.Equal("platform.unhandled_error", root.GetProperty("errorCode").GetString());
        Assert.Equal(500, root.GetProperty("status").GetInt32());
        Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("traceId").GetString()));
        Assert.Equal(correlationId, root.GetProperty("correlationId").GetString());
        Assert.DoesNotContain("InvalidOperationException", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("SensitiveStackDetail", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("at ", body, StringComparison.Ordinal);
    }
}

internal sealed class UnhandledExceptionApiFactory(string connectionString) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:PlatformDatabase"] = connectionString,
                ["Security:EnforceHttps"] = "false",
            });
        });
    }
}
