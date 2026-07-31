using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ExItS.Platform.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace ExItS.Platform.IntegrationTests;

[Collection(PostgreSqlCollection.Name)]
public sealed class ApiExternalAuthTests(PostgreSqlFixture fixture) : IAsyncLifetime
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

    [Fact]
    public async Task Testing_external_complete_creates_session_and_me_works()
    {
        var email = $"ext{Guid.NewGuid():N}@example.com";
        var complete = await _client.PostAsJsonAsync(
            "/api/v1/platform/auth/external/testing/complete",
            new
            {
                provider = "google",
                providerSubject = Guid.NewGuid().ToString("N"),
                email,
                emailVerified = true,
                displayName = "External Owner"
            });
        Assert.Equal(HttpStatusCode.OK, complete.StatusCode);
        var body = await complete.Content.ReadFromJsonAsync<JsonElement>();
        var token = body.GetProperty("sessionToken").GetString();
        Assert.False(string.IsNullOrWhiteSpace(token));
        Assert.Equal(0, body.GetProperty("activeOrganizationCount").GetInt32());

        using var meRequest = new HttpRequestMessage(HttpMethod.Get, "/api/v1/platform/auth/me");
        meRequest.Headers.Add("X-ExItS-Session-Token", token);
        var me = await _client.SendAsync(meRequest);
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
        var meBody = await me.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(email, meBody.GetProperty("email").GetString());
    }

    [Fact]
    public async Task Testing_external_rejects_unverified_email()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/platform/auth/external/testing/complete",
            new
            {
                provider = "facebook",
                providerSubject = Guid.NewGuid().ToString("N"),
                email = $"bad{Guid.NewGuid():N}@example.com",
                emailVerified = false,
                displayName = "Bad"
            });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Disabled_provider_challenge_returns_not_found()
    {
        var response = await _client.GetAsync("/api/v1/platform/auth/external/google/challenge");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

[Collection(PostgreSqlCollection.Name)]
public sealed class ExternalLoginMigrationTests(PostgreSqlFixture fixture)
{
    private const string TargetMigration = "AddPlatformExternalLogins";
    private const string PreviousMigration = "AddPlatformAccessTokens";

    [Fact]
    public async Task External_login_migration_applies_rolls_back_and_reapplies()
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

        Assert.True(await TableExistsAsync("platform_external_logins"));

        await using (var context = new PlatformDbContext(options))
        {
            await context.Database.MigrateAsync(PreviousMigration);
            var applied = await context.Database.GetAppliedMigrationsAsync();
            Assert.DoesNotContain(applied, m => m.Contains(TargetMigration, StringComparison.Ordinal));
        }

        Assert.False(await TableExistsAsync("platform_external_logins"));

        await using (var context = new PlatformDbContext(options))
        {
            await context.Database.MigrateAsync();
        }

        Assert.True(await TableExistsAsync("platform_external_logins"));
    }

    private async Task<bool> TableExistsAsync(string table)
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            SELECT 1
            FROM information_schema.tables
            WHERE table_schema = 'platform' AND table_name = @table
            """,
            connection);
        command.Parameters.AddWithValue("table", table);
        return await command.ExecuteScalarAsync() is not null;
    }
}
