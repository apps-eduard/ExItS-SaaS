using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ExItS.Platform.IntegrationTests.Support;
using ExItS.Platform.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace ExItS.Platform.IntegrationTests;

[Collection(PostgreSqlCollection.Name)]
public sealed class ApiCredentialLifecycleTests(PostgreSqlFixture fixture) : IAsyncLifetime
{
    private LifecycleApiFactory _factory = null!;
    private HttpClient _client = null!;

    public Task InitializeAsync()
    {
        _factory = new LifecycleApiFactory(fixture.ConnectionString);
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

    private async Task<(Guid UserId, string Username, string Password)> SeedUserWithPasswordAsync(string password = "Correct-Horse-9!")
    {
        var (userId, username, _) = await PlatformIntegrationTestUsers.CreatePlatformStaffWithPasswordAsync(_client, "life");
        if (!string.Equals(password, "Correct-Horse-9!", StringComparison.Ordinal))
        {
            (await _client.PutAsJsonAsync(
                $"/api/v1/platform/users/{userId}/credentials/password",
                new { password })).EnsureSuccessStatusCode();
        }

        return (userId, username, password);
    }

    [Fact]
    public async Task Forgot_reset_password_and_invalidates_prior_session()
    {
        var (_, username, password) = await SeedUserWithPasswordAsync();

        var login = await _client.PostAsJsonAsync(
            "/api/v1/platform/auth/login",
            new { usernameOrEmail = username, password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var sessionToken = (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("sessionToken").GetString();

        var forgot = await _client.PostAsJsonAsync(
            "/api/v1/platform/auth/forgot-password",
            new { usernameOrEmail = username });
        Assert.Equal(HttpStatusCode.OK, forgot.StatusCode);
        var forgotBody = await forgot.Content.ReadFromJsonAsync<JsonElement>();
        var resetToken = forgotBody.GetProperty("debugToken").GetString();
        Assert.False(string.IsNullOrWhiteSpace(resetToken));

        var reset = await _client.PostAsJsonAsync(
            "/api/v1/platform/auth/reset-password",
            new { token = resetToken, newPassword = "Correct-Horse-10!" });
        Assert.Equal(HttpStatusCode.OK, reset.StatusCode);

        using var me = new HttpRequestMessage(HttpMethod.Get, "/api/v1/platform/auth/me");
        me.Headers.Add("X-ExItS-Session-Token", sessionToken);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.SendAsync(me)).StatusCode);

        var relogin = await _client.PostAsJsonAsync(
            "/api/v1/platform/auth/login",
            new { usernameOrEmail = username, password = "Correct-Horse-10!" });
        Assert.Equal(HttpStatusCode.OK, relogin.StatusCode);
    }

    [Fact]
    public async Task Change_password_requires_session_and_revokes_sessions()
    {
        var (_, username, password) = await SeedUserWithPasswordAsync();
        var login = await _client.PostAsJsonAsync(
            "/api/v1/platform/auth/login",
            new { usernameOrEmail = username, password });
        var sessionToken = (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("sessionToken").GetString();

        using var change = new HttpRequestMessage(HttpMethod.Post, "/api/v1/platform/auth/change-password")
        {
            Content = JsonContent.Create(new { currentPassword = password, newPassword = "Correct-Horse-11!" })
        };
        change.Headers.Add("X-ExItS-Session-Token", sessionToken);
        Assert.Equal(HttpStatusCode.OK, (await _client.SendAsync(change)).StatusCode);

        using var me = new HttpRequestMessage(HttpMethod.Get, "/api/v1/platform/auth/me");
        me.Headers.Add("X-ExItS-Session-Token", sessionToken);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.SendAsync(me)).StatusCode);
    }

    [Fact]
    public async Task Email_verification_token_flow_and_admin_unlock()
    {
        var (userId, username, password) = await SeedUserWithPasswordAsync();
        var login = await _client.PostAsJsonAsync(
            "/api/v1/platform/auth/login",
            new { usernameOrEmail = username, password });
        var sessionToken = (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("sessionToken").GetString();

        using var requestVerify = new HttpRequestMessage(HttpMethod.Post, "/api/v1/platform/auth/email-verification/request");
        requestVerify.Headers.Add("X-ExItS-Session-Token", sessionToken);
        var verifyReq = await _client.SendAsync(requestVerify);
        Assert.Equal(HttpStatusCode.OK, verifyReq.StatusCode);
        var debugToken = (await verifyReq.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("debugToken").GetString();
        Assert.False(string.IsNullOrWhiteSpace(debugToken));

        var confirm = await _client.PostAsJsonAsync(
            "/api/v1/platform/auth/email-verification/confirm",
            new { token = debugToken });
        Assert.Equal(HttpStatusCode.OK, confirm.StatusCode);
        Assert.True((await confirm.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("emailVerified").GetBoolean());

        for (var i = 0; i < 5; i++)
        {
            await _client.PostAsJsonAsync(
                "/api/v1/platform/auth/login",
                new { usernameOrEmail = username, password = "Wrong-Password-9!" });
        }

        var lockedLogin = await _client.PostAsJsonAsync(
            "/api/v1/platform/auth/login",
            new { usernameOrEmail = username, password });
        Assert.Equal(HttpStatusCode.Conflict, lockedLogin.StatusCode);

        var unlockClient = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        var unlock = await unlockClient.PostAsync($"/api/v1/platform/users/{userId}/credentials/unlock", null);
        Assert.Equal(HttpStatusCode.OK, unlock.StatusCode);

        var afterUnlock = await unlockClient.PostAsJsonAsync(
            "/api/v1/platform/auth/login",
            new { usernameOrEmail = username, password });
        Assert.Equal(HttpStatusCode.OK, afterUnlock.StatusCode);
    }
}

[Collection(PostgreSqlCollection.Name)]
public sealed class CredentialTokenMigrationTests(PostgreSqlFixture fixture)
{
    private const string TargetMigration = "AddPlatformCredentialTokens";
    private const string PreviousMigration = "AddPlatformAuthSessions";

    [Fact]
    public async Task Credential_token_migration_applies_rolls_back_and_reapplies()
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

        await AssertTablePresentAsync("platform_credential_tokens");

        await using (var context = new PlatformDbContext(options))
        {
            await context.Database.MigrateAsync(PreviousMigration);
            var applied = await context.Database.GetAppliedMigrationsAsync();
            Assert.DoesNotContain(applied, m => m.Contains(TargetMigration, StringComparison.Ordinal));
        }

        await AssertTableAbsentAsync("platform_credential_tokens");
        await AssertTablePresentAsync("platform_auth_sessions");

        await using (var context = new PlatformDbContext(options))
        {
            await context.Database.MigrateAsync();
        }

        await AssertTablePresentAsync("platform_credential_tokens");
    }

    private async Task AssertTablePresentAsync(string table)
    {
        Assert.Contains(table, await QueryPlatformTablesAsync());
    }

    private async Task AssertTableAbsentAsync(string table)
    {
        Assert.DoesNotContain(table, await QueryPlatformTablesAsync());
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

internal sealed class LifecycleApiFactory(string connectionString) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:PlatformDatabase", connectionString);
        builder.UseSetting("PlatformAuthentication:Lifecycle:ExposeDebugTokens", "true");
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:PlatformDatabase"] = connectionString,
                ["Security:EnforceHttps"] = "false",
                ["PlatformAuthentication:Lifecycle:ExposeDebugTokens"] = "true",
                ["PlatformAuthentication:External:TestingEndpointEnabled"] = "true",
                ["PlatformAuthentication:Lockout:MaxFailedAccessAttempts"] = "5",
                ["PlatformAuthentication:Lockout:LockoutMinutes"] = "15"
            });
        });
    }
}
