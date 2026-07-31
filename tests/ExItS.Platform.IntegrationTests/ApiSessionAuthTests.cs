using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ExItS.Platform.Application.Identity;
using ExItS.Platform.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace ExItS.Platform.IntegrationTests;

[Collection(PostgreSqlCollection.Name)]
public sealed class ApiSessionAuthTests(PostgreSqlFixture fixture) : IAsyncLifetime
{
    private SessionApiFactory _factory = null!;
    private HttpClient _client = null!;

    public Task InitializeAsync()
    {
        _factory = new SessionApiFactory(fixture.ConnectionString);
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _client.Dispose();
        _factory.Dispose();
        return Task.CompletedTask;
    }

    private static string UniqueToken(string prefix) =>
        $"{prefix}{Guid.NewGuid():N}"[..Math.Min(24, prefix.Length + 32)].ToLowerInvariant();

    private async Task<(Guid UserId, string Username, string Password)> SeedUserWithPasswordAsync()
    {
        var username = UniqueToken("sess");
        var password = "Correct-Horse-9!";
        var create = await _client.PostAsJsonAsync(
            "/api/v1/platform/users",
            new { username, displayName = "Session User", email = $"{username}@example.com" });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var userId = (await create.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var set = await _client.PutAsJsonAsync(
            $"/api/v1/platform/users/{userId}/credentials/password",
            new { password });
        Assert.Equal(HttpStatusCode.OK, set.StatusCode);
        return (userId, username, password);
    }

    [Fact]
    public async Task Login_me_and_logout_with_session_token_header()
    {
        var (_, username, password) = await SeedUserWithPasswordAsync();

        var login = await _client.PostAsJsonAsync(
            "/api/v1/platform/auth/login",
            new { usernameOrEmail = username, password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var body = await login.Content.ReadFromJsonAsync<JsonElement>();
        var token = body.GetProperty("sessionToken").GetString();
        Assert.False(string.IsNullOrWhiteSpace(token));
        Assert.False(body.TryGetProperty("password", out _));
        var loginMfa = body.GetProperty("mfa");
        Assert.False(loginMfa.GetProperty("mfaEnabled").GetBoolean());
        Assert.False(loginMfa.GetProperty("challengeRequired").GetBoolean());
        Assert.Equal("NotEnrolled", loginMfa.GetProperty("readinessState").GetString());

        using var meRequest = new HttpRequestMessage(HttpMethod.Get, "/api/v1/platform/auth/me");
        meRequest.Headers.Add("X-ExItS-Session-Token", token);
        var me = await _client.SendAsync(meRequest);
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
        var meBody = await me.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(username, meBody.GetProperty("username").GetString());
        Assert.False(meBody.GetProperty("mfa").GetProperty("challengeRequired").GetBoolean());

        using var logoutRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/platform/auth/logout");
        logoutRequest.Headers.Add("X-ExItS-Session-Token", token);
        var logout = await _client.SendAsync(logoutRequest);
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);

        using var meAfter = new HttpRequestMessage(HttpMethod.Get, "/api/v1/platform/auth/me");
        meAfter.Headers.Add("X-ExItS-Session-Token", token);
        var denied = await _client.SendAsync(meAfter);
        Assert.Equal(HttpStatusCode.Unauthorized, denied.StatusCode);
    }

    [Fact]
    public async Task Login_rejects_wrong_password_with_generic_failure()
    {
        var (_, username, _) = await SeedUserWithPasswordAsync();
        var login = await _client.PostAsJsonAsync(
            "/api/v1/platform/auth/login",
            new { usernameOrEmail = username, password = "Wrong-Password-9!" });
        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
    }

    [Fact]
    public async Task Suspended_user_cannot_login()
    {
        var (userId, username, password) = await SeedUserWithPasswordAsync();
        var suspend = await _client.PostAsJsonAsync($"/api/v1/platform/users/{userId}/suspend", new { reason = "test" });
        Assert.Equal(HttpStatusCode.OK, suspend.StatusCode);

        var login = await _client.PostAsJsonAsync(
            "/api/v1/platform/auth/login",
            new { usernameOrEmail = username, password });
        Assert.Equal(HttpStatusCode.Forbidden, login.StatusCode);
    }
}

[Collection(PostgreSqlCollection.Name)]
public sealed class AuthSessionMigrationTests(PostgreSqlFixture fixture)
{
    private const string TargetMigration = "AddPlatformAuthSessions";
    private const string PreviousMigration = "AddPlatformUserCredentials";

    [Fact]
    public async Task Auth_session_migration_applies_rolls_back_and_reapplies()
    {
        var options = new DbContextOptionsBuilder<PlatformDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .Options;

        await using (var context = new PlatformDbContext(options))
        {
            await context.Database.MigrateAsync();
            var applied = await context.Database.GetAppliedMigrationsAsync();
            Assert.Contains(applied, m => m.Contains(TargetMigration, StringComparison.Ordinal));
        }

        await AssertTablePresentAsync("platform_auth_sessions");

        await using (var context = new PlatformDbContext(options))
        {
            await context.Database.MigrateAsync(PreviousMigration);
            var applied = await context.Database.GetAppliedMigrationsAsync();
            Assert.DoesNotContain(applied, m => m.Contains(TargetMigration, StringComparison.Ordinal));
        }

        await AssertTableAbsentAsync("platform_auth_sessions");
        await AssertTablePresentAsync("platform_user_credentials");

        await using (var context = new PlatformDbContext(options))
        {
            await context.Database.MigrateAsync();
        }

        await AssertTablePresentAsync("platform_auth_sessions");
    }

    private async Task AssertTablePresentAsync(string table)
    {
        var tables = await QueryPlatformTablesAsync();
        Assert.Contains(table, tables);
    }

    private async Task AssertTableAbsentAsync(string table)
    {
        var tables = await QueryPlatformTablesAsync();
        Assert.DoesNotContain(table, tables);
    }

    private async Task<HashSet<string>> QueryPlatformTablesAsync()
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            SELECT table_name
            FROM information_schema.tables
            WHERE table_schema = 'platform'
              AND table_type = 'BASE TABLE'
            """,
            connection);

        var tables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            tables.Add(reader.GetString(0));
        }

        return tables;
    }
}

internal sealed class SessionApiFactory(string connectionString) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:PlatformDatabase", connectionString);
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:PlatformDatabase"] = connectionString,
                ["Security:EnforceHttps"] = "false",
                ["PlatformAuthentication:External:TestingEndpointEnabled"] = "true"
            });
        });
    }
}
