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
public sealed class ApiOrganizationContextTests(PostgreSqlFixture fixture) : IAsyncLifetime
{
    private SessionApiFactory _factory = null!;
    private HttpClient _client = null!;
    private HttpClient _admin = null!;

    public Task InitializeAsync()
    {
        _factory = new SessionApiFactory(fixture.ConnectionString);
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        _admin = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _client.Dispose();
        _admin.Dispose();
        _factory.Dispose();
        return Task.CompletedTask;
    }

    private static string UniqueToken(string prefix) =>
        $"{prefix}{Guid.NewGuid():N}"[..Math.Min(24, prefix.Length + 32)].ToLowerInvariant();

    private async Task<(Guid UserId, string Username, string Password)> SeedUserWithPasswordAsync()
    {
        var username = UniqueToken("orgctx");
        var password = "Correct-Horse-9!";
        var create = await _admin.PostAsJsonAsync(
            "/api/v1/platform/users",
            new { username, displayName = "Org Ctx User", email = $"{username}@example.com" });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var userId = (await create.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var set = await _admin.PutAsJsonAsync(
            $"/api/v1/platform/users/{userId}/credentials/password",
            new { password });
        Assert.Equal(HttpStatusCode.OK, set.StatusCode);
        return (userId, username, password);
    }

    private async Task<Guid> CreateOrganizationAsync(string prefix)
    {
        var create = await _admin.PostAsJsonAsync(
            "/api/v1/platform/organizations",
            new { displayName = $"{prefix} Org", slug = UniqueToken(prefix) });
        create.EnsureSuccessStatusCode();
        return (await create.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
    }

    private async Task<string> LoginAsync(string username, string password)
    {
        var login = await _client.PostAsJsonAsync(
            "/api/v1/platform/auth/login",
            new { usernameOrEmail = username, password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var body = await login.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("sessionToken").GetString()!;
    }

    private static HttpRequestMessage Authed(HttpMethod method, string url, string token, object? body = null)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Add("X-ExItS-Session-Token", token);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        return request;
    }

    [Fact]
    public async Task Login_with_no_membership_has_none_organization_context()
    {
        var (_, username, password) = await SeedUserWithPasswordAsync();
        var login = await _client.PostAsJsonAsync(
            "/api/v1/platform/auth/login",
            new { usernameOrEmail = username, password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var body = await login.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(JsonValueKind.Null, body.GetProperty("selectedOrganizationId").ValueKind);
        Assert.Equal(OrganizationSelectionStates.None, body.GetProperty("organizationSelectionState").GetString());
        Assert.Equal(0, body.GetProperty("activeOrganizationCount").GetInt32());
    }

    [Fact]
    public async Task Login_with_one_active_membership_auto_selects_organization()
    {
        var (userId, username, password) = await SeedUserWithPasswordAsync();
        var organizationId = await CreateOrganizationAsync("one");
        var add = await _admin.PostAsJsonAsync(
            $"/api/v1/platform/organizations/{organizationId}/members",
            new { userId, role = "OrganizationMember" });
        Assert.Equal(HttpStatusCode.Created, add.StatusCode);

        var login = await _client.PostAsJsonAsync(
            "/api/v1/platform/auth/login",
            new { usernameOrEmail = username, password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var body = await login.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(organizationId, body.GetProperty("selectedOrganizationId").GetGuid());
        Assert.Equal(OrganizationSelectionStates.Selected, body.GetProperty("organizationSelectionState").GetString());
        Assert.Equal(1, body.GetProperty("activeOrganizationCount").GetInt32());
    }

    [Fact]
    public async Task Multiple_memberships_require_selection_and_support_switch()
    {
        var (userId, username, password) = await SeedUserWithPasswordAsync();
        var orgA = await CreateOrganizationAsync("a");
        var orgB = await CreateOrganizationAsync("b");
        (await _admin.PostAsJsonAsync(
            $"/api/v1/platform/organizations/{orgA}/members",
            new { userId, role = "OrganizationMember" })).EnsureSuccessStatusCode();
        (await _admin.PostAsJsonAsync(
            $"/api/v1/platform/organizations/{orgB}/members",
            new { userId, role = "OrganizationOwner" })).EnsureSuccessStatusCode();

        var login = await _client.PostAsJsonAsync(
            "/api/v1/platform/auth/login",
            new { usernameOrEmail = username, password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var loginBody = await login.Content.ReadFromJsonAsync<JsonElement>();
        var token = loginBody.GetProperty("sessionToken").GetString()!;
        Assert.Equal(JsonValueKind.Null, loginBody.GetProperty("selectedOrganizationId").ValueKind);
        Assert.Equal(OrganizationSelectionStates.SelectionRequired, loginBody.GetProperty("organizationSelectionState").GetString());
        Assert.Equal(2, loginBody.GetProperty("activeOrganizationCount").GetInt32());

        using var listRequest = Authed(HttpMethod.Get, "/api/v1/platform/auth/organizations", token);
        var list = await _client.SendAsync(listRequest);
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        var orgs = await list.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(2, orgs.GetArrayLength());

        using var selectA = Authed(HttpMethod.Put, "/api/v1/platform/auth/organization-context", token, new { organizationId = orgA });
        var selectedA = await _client.SendAsync(selectA);
        Assert.Equal(HttpStatusCode.OK, selectedA.StatusCode);
        Assert.Equal(orgA, (await selectedA.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("selectedOrganizationId").GetGuid());

        using var selectB = Authed(HttpMethod.Put, "/api/v1/platform/auth/organization-context", token, new { organizationId = orgB });
        var selectedB = await _client.SendAsync(selectB);
        Assert.Equal(HttpStatusCode.OK, selectedB.StatusCode);
        Assert.Equal(orgB, (await selectedB.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("selectedOrganizationId").GetGuid());

        using var meRequest = Authed(HttpMethod.Get, "/api/v1/platform/auth/me", token);
        var me = await _client.SendAsync(meRequest);
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
        Assert.Equal(orgB, (await me.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("selectedOrganizationId").GetGuid());
    }

    [Fact]
    public async Task Rejects_organization_without_active_membership()
    {
        var (_, username, password) = await SeedUserWithPasswordAsync();
        var foreignOrg = await CreateOrganizationAsync("fx");
        var token = await LoginAsync(username, password);

        using var select = Authed(
            HttpMethod.Put,
            "/api/v1/platform/auth/organization-context",
            token,
            new { organizationId = foreignOrg });
        var response = await _client.SendAsync(select);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Suspend_membership_clears_stale_organization_context()
    {
        var (userId, username, password) = await SeedUserWithPasswordAsync();
        var organizationId = await CreateOrganizationAsync("sus");
        var add = await _admin.PostAsJsonAsync(
            $"/api/v1/platform/organizations/{organizationId}/members",
            new { userId, role = "OrganizationMember" });
        add.EnsureSuccessStatusCode();
        var membershipId = (await add.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var token = await LoginAsync(username, password);
        using var meBefore = Authed(HttpMethod.Get, "/api/v1/platform/auth/me", token);
        var before = await _client.SendAsync(meBefore);
        Assert.Equal(organizationId, (await before.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("selectedOrganizationId").GetGuid());

        var suspend = await _admin.PostAsJsonAsync(
            $"/api/v1/platform/memberships/{membershipId}/suspend",
            new { reason = "hold", actorReference = "dev-admin" });
        Assert.Equal(HttpStatusCode.OK, suspend.StatusCode);

        using var meAfter = Authed(HttpMethod.Get, "/api/v1/platform/auth/me", token);
        var after = await _client.SendAsync(meAfter);
        Assert.Equal(HttpStatusCode.OK, after.StatusCode);
        var afterBody = await after.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(JsonValueKind.Null, afterBody.GetProperty("selectedOrganizationId").ValueKind);
        Assert.Equal(0, afterBody.GetProperty("activeOrganizationCount").GetInt32());
    }
}

[Collection(PostgreSqlCollection.Name)]
public sealed class SelectedOrganizationMigrationTests(PostgreSqlFixture fixture)
{
    private const string TargetMigration = "AddSelectedOrganizationToAuthSessions";
    private const string PreviousMigration = "AddPlatformCredentialTokens";

    [Fact]
    public async Task Selected_organization_migration_applies_rolls_back_and_reapplies()
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

        Assert.True(await ColumnExistsAsync("platform_auth_sessions", "selected_organization_id"));

        await using (var context = new PlatformDbContext(options))
        {
            await context.Database.MigrateAsync(PreviousMigration);
            var applied = await context.Database.GetAppliedMigrationsAsync();
            Assert.DoesNotContain(applied, m => m.Contains(TargetMigration, StringComparison.Ordinal));
        }

        Assert.False(await ColumnExistsAsync("platform_auth_sessions", "selected_organization_id"));
        Assert.True(await TableExistsAsync("platform_credential_tokens"));

        await using (var context = new PlatformDbContext(options))
        {
            await context.Database.MigrateAsync();
        }

        Assert.True(await ColumnExistsAsync("platform_auth_sessions", "selected_organization_id"));
    }

    private async Task<bool> TableExistsAsync(string table)
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            SELECT 1
            FROM information_schema.tables
            WHERE table_schema = 'platform'
              AND table_name = @table
            """,
            connection);
        command.Parameters.AddWithValue("table", table);
        return await command.ExecuteScalarAsync() is not null;
    }

    private async Task<bool> ColumnExistsAsync(string table, string column)
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            SELECT 1
            FROM information_schema.columns
            WHERE table_schema = 'platform'
              AND table_name = @table
              AND column_name = @column
            """,
            connection);
        command.Parameters.AddWithValue("table", table);
        command.Parameters.AddWithValue("column", column);
        return await command.ExecuteScalarAsync() is not null;
    }
}
