using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ExItS.Platform.Infrastructure.Persistence;
using ExItS.Platform.IntegrationTests.Support;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ExItS.Platform.IntegrationTests;

[Collection(PostgreSqlCollection.Name)]
public sealed class ApiRecoveryEmailTests(PostgreSqlFixture fixture) : IAsyncLifetime
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

    [Fact]
    public async Task External_user_can_skip_or_verify_recovery_email_without_privilege_grants()
    {
        var email = $"rec{Guid.NewGuid():N}@gmail.com";
        var complete = await _client.PostAsJsonAsync(
            "/api/v1/platform/auth/external/testing/complete",
            new
            {
                provider = "google",
                providerSubject = Guid.NewGuid().ToString("N"),
                email,
                emailVerified = true,
                displayName = "Recovery Owner"
            });
        Assert.Equal(HttpStatusCode.OK, complete.StatusCode);
        var body = await complete.Content.ReadFromJsonAsync<JsonElement>();
        var sessionToken = body.GetProperty("sessionToken").GetString();
        Assert.Equal(0, body.GetProperty("activeOrganizationCount").GetInt32());

        using (var statusRequest = new HttpRequestMessage(HttpMethod.Get, "/api/v1/platform/auth/credentials"))
        {
            statusRequest.Headers.Add("X-ExItS-Session-Token", sessionToken);
            var status = await _client.SendAsync(statusRequest);
            Assert.Equal(HttpStatusCode.OK, status.StatusCode);
            var statusBody = await status.Content.ReadFromJsonAsync<JsonElement>();
            Assert.True(statusBody.GetProperty("needsRecoveryEmailPrompt").GetBoolean());
            Assert.False(statusBody.GetProperty("hasPassword").GetBoolean());
        }

        using (var skipRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/platform/auth/recovery-email/skip"))
        {
            skipRequest.Headers.Add("X-ExItS-Session-Token", sessionToken);
            var skip = await _client.SendAsync(skipRequest);
            Assert.Equal(HttpStatusCode.OK, skip.StatusCode);
            Assert.False((await skip.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("needsRecoveryEmailPrompt").GetBoolean());
        }

        // Session still valid after skip — login is not blocked.
        using (var meRequest = new HttpRequestMessage(HttpMethod.Get, "/api/v1/platform/auth/me"))
        {
            meRequest.Headers.Add("X-ExItS-Session-Token", sessionToken);
            Assert.Equal(HttpStatusCode.OK, (await _client.SendAsync(meRequest)).StatusCode);
        }

        using (var requestRecovery = new HttpRequestMessage(HttpMethod.Post, "/api/v1/platform/auth/recovery-email/request")
        {
            Content = JsonContent.Create(new { recoveryEmail = $"backup{Guid.NewGuid():N}@example.com" })
        })
        {
            requestRecovery.Headers.Add("X-ExItS-Session-Token", sessionToken);
            var requested = await _client.SendAsync(requestRecovery);
            Assert.Equal(HttpStatusCode.OK, requested.StatusCode);
            var debugToken = (await requested.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("debugToken").GetString();
            Assert.False(string.IsNullOrWhiteSpace(debugToken));

            var confirm = await _client.PostAsJsonAsync(
                "/api/v1/platform/auth/recovery-email/confirm",
                new { token = debugToken });
            Assert.Equal(HttpStatusCode.OK, confirm.StatusCode);
            var confirmed = await confirm.Content.ReadFromJsonAsync<JsonElement>();
            Assert.True(confirmed.GetProperty("recoveryEmailVerified").GetBoolean());
        }

        // Still no org membership / entitlement side effects from recovery email.
        using (var meAfter = new HttpRequestMessage(HttpMethod.Get, "/api/v1/platform/auth/me"))
        {
            meAfter.Headers.Add("X-ExItS-Session-Token", sessionToken);
            var meBody = await (await _client.SendAsync(meAfter)).Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(0, meBody.GetProperty("activeOrganizationCount").GetInt32());
            Assert.Equal("None", meBody.GetProperty("organizationSelectionState").GetString());
        }
    }

    [Fact]
    public async Task Forgot_password_uses_verified_recovery_email_when_identifier_matches()
    {
        var (_, username, _) = await PlatformIntegrationTestUsers.CreatePlatformStaffWithPasswordAsync(_client, "recpwd");

        var login = await _client.PostAsJsonAsync(
            "/api/v1/platform/auth/login",
            new { usernameOrEmail = username, password = "Correct-Horse-9!" });
        var sessionToken = (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("sessionToken").GetString();

        var recoveryEmail = $"alt{Guid.NewGuid():N}@example.com";
        using (var requestRecovery = new HttpRequestMessage(HttpMethod.Post, "/api/v1/platform/auth/recovery-email/request")
        {
            Content = JsonContent.Create(new { recoveryEmail })
        })
        {
            requestRecovery.Headers.Add("X-ExItS-Session-Token", sessionToken);
            var requested = await _client.SendAsync(requestRecovery);
            Assert.Equal(HttpStatusCode.OK, requested.StatusCode);
            var debugToken = (await requested.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("debugToken").GetString();
            var confirm = await _client.PostAsJsonAsync(
                "/api/v1/platform/auth/recovery-email/confirm",
                new { token = debugToken });
            Assert.Equal(HttpStatusCode.OK, confirm.StatusCode);
        }

        var forgot = await _client.PostAsJsonAsync(
            "/api/v1/platform/auth/forgot-password",
            new { usernameOrEmail = recoveryEmail });
        Assert.Equal(HttpStatusCode.OK, forgot.StatusCode);
        Assert.False(string.IsNullOrWhiteSpace(
            (await forgot.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("debugToken").GetString()));
    }
}

[Collection(PostgreSqlCollection.Name)]
public sealed class RecoveryEmailMigrationTests(PostgreSqlFixture fixture)
{
    private const string TargetMigration = "AddPlatformRecoveryEmail";
    private const string PreviousMigration = "AddPlatformExternalLogins";

    [Fact]
    public async Task Recovery_email_migration_applies_rolls_back_and_reapplies()
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

        await AssertColumnPresentAsync();

        await using (var context = new PlatformDbContext(options))
        {
            await context.Database.MigrateAsync(PreviousMigration);
            var applied = await context.Database.GetAppliedMigrationsAsync();
            Assert.DoesNotContain(applied, m => m.Contains(TargetMigration, StringComparison.Ordinal));
        }

        await AssertColumnAbsentAsync();

        await using (var context = new PlatformDbContext(options))
        {
            await context.Database.MigrateAsync();
        }

        await AssertColumnPresentAsync();
    }

    private async Task AssertColumnPresentAsync()
    {
        Assert.Contains("recovery_normalized_email", await QueryCredentialColumnsAsync());
    }

    private async Task AssertColumnAbsentAsync()
    {
        Assert.DoesNotContain("recovery_normalized_email", await QueryCredentialColumnsAsync());
    }

    private async Task<HashSet<string>> QueryCredentialColumnsAsync()
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            SELECT column_name
            FROM information_schema.columns
            WHERE table_schema = 'platform'
              AND table_name = 'platform_user_credentials'
            """,
            connection);

        var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            columns.Add(reader.GetString(0));
        }

        return columns;
    }
}
