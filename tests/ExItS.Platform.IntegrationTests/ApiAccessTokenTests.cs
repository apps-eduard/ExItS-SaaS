using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ExItS.Platform.Infrastructure.Persistence;
using ExItS.Platform.IntegrationTests.Support;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace ExItS.Platform.IntegrationTests;

[Collection(PostgreSqlCollection.Name)]
public sealed class ApiAccessTokenTests(PostgreSqlFixture fixture) : IAsyncLifetime
{
    private SessionApiFactory _factory = null!;
    private HttpClient _client = null!;
    private HttpClient _admin = null!;

    public Task InitializeAsync()
    {
        _factory = new SessionApiFactory(fixture.ConnectionString);
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
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

    private Task<(Guid UserId, string Username, string Password)> SeedUserWithPasswordAsync() =>
        PlatformIntegrationTestUsers.CreatePlatformStaffWithPasswordAsync(_admin, "atok");

    [Fact]
    public async Task Password_grant_issues_bearer_and_introspect_reports_active()
    {
        var (_, username, password) = await SeedUserWithPasswordAsync();

        var issue = await _client.PostAsJsonAsync(
            "/api/v1/platform/auth/token",
            new { grantType = "password", usernameOrEmail = username, password });
        Assert.Equal(HttpStatusCode.OK, issue.StatusCode);
        var body = await issue.Content.ReadFromJsonAsync<JsonElement>();
        var accessToken = body.GetProperty("accessToken").GetString();
        Assert.False(string.IsNullOrWhiteSpace(accessToken));
        Assert.Equal("Bearer", body.GetProperty("tokenType").GetString());
        Assert.False(body.TryGetProperty("password", out _));
        Assert.False(body.GetProperty("mfa").GetProperty("challengeRequired").GetBoolean());

        var introspect = await _client.PostAsJsonAsync(
            "/api/v1/platform/auth/introspect",
            new { token = accessToken });
        Assert.Equal(HttpStatusCode.OK, introspect.StatusCode);
        var info = await introspect.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(info.GetProperty("active").GetBoolean());
        Assert.Equal(username, info.GetProperty("username").GetString());
        Assert.Equal("NotEnrolled", info.GetProperty("mfa").GetProperty("readinessState").GetString());
    }

    [Fact]
    public async Task Suspend_user_revokes_active_access_token()
    {
        var (userId, username, password) = await SeedUserWithPasswordAsync();
        var issue = await _client.PostAsJsonAsync(
            "/api/v1/platform/auth/token",
            new { grantType = "password", usernameOrEmail = username, password });
        var accessToken = (await issue.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString();

        Assert.Equal(HttpStatusCode.OK, (await _admin.PostAsJsonAsync(
            $"/api/v1/platform/users/{userId}/suspend",
            new { reason = "hardening" })).StatusCode);

        var introspect = await _client.PostAsJsonAsync(
            "/api/v1/platform/auth/introspect",
            new { token = accessToken });
        Assert.False((await introspect.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("active").GetBoolean());
    }

    [Fact]
    public async Task Owner_session_grant_issues_management_authority_without_selling_role()
    {
        // Mica-like: Organization Owner, no product-local POS checkout role.
        var (userId, username, password) = await SeedUserWithPasswordAsync();
        var org = await _admin.PostAsJsonAsync(
            "/api/v1/platform/organizations",
            new { displayName = "Mica Store Test", slug = UniqueToken("mica") });
        org.EnsureSuccessStatusCode();
        var organizationId = (await org.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        (await _admin.PostAsJsonAsync(
            $"/api/v1/platform/organizations/{organizationId}/members",
            new { userId, role = "OrganizationOwner", reason = "integration-test-owner" })).EnsureSuccessStatusCode();

        var login = await _client.PostAsJsonAsync(
            "/api/v1/platform/auth/login",
            new { usernameOrEmail = username, password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var sessionToken = (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("sessionToken").GetString();

        using var select = new HttpRequestMessage(HttpMethod.Put, "/api/v1/platform/auth/organization-context");
        select.Headers.Add("X-ExItS-Session-Token", sessionToken);
        select.Content = JsonContent.Create(new { organizationId });
        (await _client.SendAsync(select)).EnsureSuccessStatusCode();

        using var sessionGrant = new HttpRequestMessage(HttpMethod.Post, "/api/v1/platform/auth/token");
        sessionGrant.Headers.Add("X-ExItS-Session-Token", sessionToken);
        sessionGrant.Content = JsonContent.Create(new
        {
            grantType = "session",
            organizationId,
            productCode = "pinoy-business-pos"
        });
        var issued = await _client.SendAsync(sessionGrant);
        Assert.Equal(HttpStatusCode.OK, issued.StatusCode);
        var body = await issued.Content.ReadFromJsonAsync<JsonElement>();
        var accessToken = body.GetProperty("accessToken").GetString();
        Assert.False(string.IsNullOrWhiteSpace(accessToken));
        Assert.True(body.GetProperty("organizationManagementAuthority").GetBoolean());
        Assert.True(body.GetProperty("productAccessAllowed").GetBoolean());
        Assert.Equal("organization_management_authority", body.GetProperty("productAccessReasonCode").GetString());
        Assert.True(
            !body.TryGetProperty("productLocalRoleCode", out var roleEl)
            || roleEl.ValueKind is JsonValueKind.Null
            || string.IsNullOrWhiteSpace(roleEl.GetString()));
        Assert.True(
            !body.TryGetProperty("mappedPosRoleCode", out var mappedEl)
            || mappedEl.ValueKind is JsonValueKind.Null
            || string.IsNullOrWhiteSpace(mappedEl.GetString()));

        var introspect = await _client.PostAsJsonAsync(
            "/api/v1/platform/auth/introspect",
            new { token = accessToken });
        Assert.Equal(HttpStatusCode.OK, introspect.StatusCode);
        var info = await introspect.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(info.GetProperty("active").GetBoolean());
        Assert.True(info.GetProperty("organizationManagementAuthority").GetBoolean());
        Assert.True(info.GetProperty("productAccessAllowed").GetBoolean());
        Assert.Equal(organizationId, info.GetProperty("organizationId").GetGuid());

        // Owner may list own org branches via PlatformSession (membership), not view_portfolio.
        using var branches = new HttpRequestMessage(
            HttpMethod.Get,
            $"/api/v1/platform/organizations/{organizationId}/branches");
        branches.Headers.Add("X-ExItS-Session-Token", sessionToken);
        var branchesResponse = await _client.SendAsync(branches);
        Assert.Equal(HttpStatusCode.OK, branchesResponse.StatusCode);
    }

    [Fact]
    public async Task Session_grant_and_bind_requires_active_membership_and_product_access()
    {
        // OrganizationMember must be an org-scoped staff identity (invite accept), not Platform staff.
        var (userId, _, staffLogin, password, organizationId) = await PlatformIntegrationTestUsers
            .SeedOrgMemberViaInvitationAsync(_admin, _client, "atokm");

        var login = await _client.PostAsJsonAsync(
            "/api/v1/platform/auth/login",
            new { usernameOrEmail = staffLogin, password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var sessionToken = (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("sessionToken").GetString();

        using var sessionGrant = new HttpRequestMessage(HttpMethod.Post, "/api/v1/platform/auth/token");
        sessionGrant.Headers.Add("X-ExItS-Session-Token", sessionToken);
        sessionGrant.Content = JsonContent.Create(new
        {
            grantType = "session",
            organizationId,
            productCode = "pinoy-business-pos"
        });
        var denied = await _client.SendAsync(sessionGrant);
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);

        using var identityGrant = new HttpRequestMessage(HttpMethod.Post, "/api/v1/platform/auth/token");
        identityGrant.Headers.Add("X-ExItS-Session-Token", sessionToken);
        identityGrant.Content = JsonContent.Create(new { grantType = "session" });
        var issued = await _client.SendAsync(identityGrant);
        Assert.Equal(HttpStatusCode.OK, issued.StatusCode);
        var accessToken = (await issued.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString();

        using var bind = new HttpRequestMessage(HttpMethod.Post, "/api/v1/platform/auth/token/bind");
        bind.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        bind.Content = JsonContent.Create(new { organizationId, productCode = "pinoy-business-pos" });
        var bindDenied = await _client.SendAsync(bind);
        Assert.Equal(HttpStatusCode.Forbidden, bindDenied.StatusCode);
        _ = userId;
    }

    [Fact]
    public async Task Introspect_inactive_for_revoked_token()
    {
        var (_, username, password) = await SeedUserWithPasswordAsync();
        var issue = await _client.PostAsJsonAsync(
            "/api/v1/platform/auth/token",
            new { grantType = "password", usernameOrEmail = username, password });
        var accessToken = (await issue.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString();

        using var revoke = new HttpRequestMessage(HttpMethod.Post, "/api/v1/platform/auth/token/revoke");
        revoke.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        Assert.Equal(HttpStatusCode.NoContent, (await _client.SendAsync(revoke)).StatusCode);

        var introspect = await _client.PostAsJsonAsync(
            "/api/v1/platform/auth/introspect",
            new { token = accessToken });
        Assert.False((await introspect.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("active").GetBoolean());
    }
}

[Collection(PostgreSqlCollection.Name)]
public sealed class AccessTokenMigrationTests(PostgreSqlFixture fixture)
{
    private const string TargetMigration = "AddPlatformAccessTokens";
    private const string PreviousMigration = "AddSelectedOrganizationToAuthSessions";

    [Fact]
    public async Task Access_token_migration_applies_rolls_back_and_reapplies()
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

        Assert.True(await TableExistsAsync("platform_access_tokens"));

        await using (var context = new PlatformDbContext(options))
        {
            await context.Database.MigrateAsync(PreviousMigration);
            var applied = await context.Database.GetAppliedMigrationsAsync();
            Assert.DoesNotContain(applied, m => m.Contains(TargetMigration, StringComparison.Ordinal));
        }

        Assert.False(await TableExistsAsync("platform_access_tokens"));

        await using (var context = new PlatformDbContext(options))
        {
            await context.Database.MigrateAsync();
        }

        Assert.True(await TableExistsAsync("platform_access_tokens"));
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
}
