using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Domain.Authorization;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ExItS.Platform.IntegrationTests;

[Collection(PostgreSqlCollection.Name)]
public sealed class ApiAccountScopeIsolationTests(PostgreSqlFixture fixture) : IAsyncLifetime
{
    private SessionApiFactory _factory = null!;
    private HttpClient _admin = null!;
    private HttpClient _client = null!;

    public Task InitializeAsync()
    {
        _factory = new SessionApiFactory(fixture.ConnectionString);
        _admin = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _client.Dispose();
        _admin.Dispose();
        _factory.Dispose();
        return Task.CompletedTask;
    }

    private static string Unique(string prefix) =>
        $"{prefix}{Guid.NewGuid():N}"[..Math.Min(20, prefix.Length + 32)].ToLowerInvariant();

    private async Task<(Guid UserId, string Username, string Password)> SeedUserAsync(string prefix)
    {
        var username = Unique(prefix);
        var password = "Correct-Horse-9!";
        var create = await _admin.PostAsJsonAsync(
            "/api/v1/platform/users",
            new { username, displayName = "Scope User", email = $"{username}@example.com" });
        create.EnsureSuccessStatusCode();
        var userId = (await create.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        (await _admin.PutAsJsonAsync(
            $"/api/v1/platform/users/{userId}/credentials/password",
            new { password })).EnsureSuccessStatusCode();
        return (userId, username, password);
    }

    private async Task<JsonElement> LoginAsync(string username, string password)
    {
        var login = await _client.PostAsJsonAsync(
            "/api/v1/platform/auth/login",
            new { usernameOrEmail = username, password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        return await login.Content.ReadFromJsonAsync<JsonElement>();
    }

    private static HttpRequestMessage Authed(HttpMethod method, string url, string token)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Add("X-ExItS-Session-Token", token);
        return request;
    }

    private static async Task AssertScopeDeniedAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(ApplicationErrorCodes.AccountScopeDenied, body.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task Personal_session_cannot_call_platform_admin_apis()
    {
        var (_, username, password) = await SeedUserAsync("pers");
        var login = await LoginAsync(username, password);
        Assert.Equal("Personal", login.GetProperty("accountClass").GetString());
        var token = login.GetProperty("sessionToken").GetString()!;

        using var users = Authed(HttpMethod.Get, "/api/v1/platform/users?page=1&pageSize=10", token);
        await AssertScopeDeniedAsync(await _client.SendAsync(users));

        using var personal = Authed(HttpMethod.Get, "/api/v1/personal/me", token);
        var allowed = await _client.SendAsync(personal);
        Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
    }

    [Fact]
    public async Task Platform_session_cannot_call_personal_apis()
    {
        var (userId, username, password) = await SeedUserAsync("plat");
        var assign = await _admin.PostAsJsonAsync(
            "/api/v1/platform/authorization/assignments",
            new
            {
                platformUserId = userId,
                role = nameof(PlatformSystemRole.PlatformAdministrator)
            });
        Assert.Equal(HttpStatusCode.Created, assign.StatusCode);

        var login = await LoginAsync(username, password);
        Assert.Equal("Platform", login.GetProperty("accountClass").GetString());
        var token = login.GetProperty("sessionToken").GetString()!;

        using var personal = Authed(HttpMethod.Get, "/api/v1/personal/me", token);
        await AssertScopeDeniedAsync(await _client.SendAsync(personal));

        using var users = Authed(HttpMethod.Get, "/api/v1/platform/users?page=1&pageSize=10", token);
        var allowed = await _client.SendAsync(users);
        Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
    }

    [Fact]
    public async Task Organization_session_cannot_call_platform_or_personal_apis()
    {
        var (userId, username, password) = await SeedUserAsync("orgs");
        var org = await _admin.PostAsJsonAsync(
            "/api/v1/platform/organizations",
            new { displayName = "Scope Org", slug = Unique("scorg") });
        org.EnsureSuccessStatusCode();
        var organizationId = (await org.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        (await _admin.PostAsJsonAsync(
            $"/api/v1/platform/organizations/{organizationId}/members",
            new { userId, role = "OrganizationMember" })).EnsureSuccessStatusCode();

        var login = await LoginAsync(username, password);
        Assert.Equal("Organization", login.GetProperty("accountClass").GetString());
        var token = login.GetProperty("sessionToken").GetString()!;

        using var users = Authed(HttpMethod.Get, "/api/v1/platform/users?page=1&pageSize=10", token);
        await AssertScopeDeniedAsync(await _client.SendAsync(users));

        using var personal = Authed(HttpMethod.Get, "/api/v1/personal/me", token);
        await AssertScopeDeniedAsync(await _client.SendAsync(personal));
    }

    [Fact]
    public async Task Client_cannot_forge_account_class_via_profile_select_of_other_user()
    {
        var (userAId, usernameA, passwordA) = await SeedUserAsync("fora");
        var (userBId, usernameB, passwordB) = await SeedUserAsync("forb");
        _ = userAId;
        _ = userBId;
        _ = passwordB;

        var loginB = await LoginAsync(usernameB, passwordB);
        var foreignProfileId = loginB.GetProperty("accountProfileId").GetGuid();

        var loginA = await LoginAsync(usernameA, passwordA);
        var tokenA = loginA.GetProperty("sessionToken").GetString()!;

        using var select = Authed(HttpMethod.Post, "/api/v1/platform/auth/account-profiles/select", tokenA);
        select.Content = JsonContent.Create(new { accountProfileId = foreignProfileId });
        var denied = await _client.SendAsync(select);
        Assert.Equal(HttpStatusCode.BadRequest, denied.StatusCode);
        var body = await denied.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(ApplicationErrorCodes.AccountProfileNotAvailable, body.GetProperty("errorCode").GetString());
    }
}
