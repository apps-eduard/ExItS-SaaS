using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.IntegrationTests.Support;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ExItS.Platform.IntegrationTests;

[Collection(PostgreSqlCollection.Name)]
public sealed class ApiPersonalAccountTests(PostgreSqlFixture fixture) : IAsyncLifetime
{
    private SessionApiFactory _factory = null!;
    private HttpClient _admin = null!;
    private HttpClient _client = null!;

    public Task InitializeAsync()
    {
        _factory = new SessionApiFactory(fixture.ConnectionString);
        _admin = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
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

    private async Task<string> LoginPersonalAsync()
    {
        var (_, email, password) = await PlatformIntegrationTestUsers.RegisterPersonalWithPasswordAsync(_client, "pacc");
        var login = await _client.PostAsJsonAsync(
            "/api/v1/platform/auth/login",
            new { usernameOrEmail = email, password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var body = await login.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Personal", body.GetProperty("accountClass").GetString());
        return body.GetProperty("sessionToken").GetString()!;
    }

    private static HttpRequestMessage Authed(HttpMethod method, string url, string token)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Add("X-ExItS-Session-Token", token);
        return request;
    }

    [Fact]
    public async Task Personal_session_can_access_dashboard_profile_and_settings()
    {
        var token = await LoginPersonalAsync();

        using var dashboard = Authed(HttpMethod.Get, "/api/v1/personal/dashboard", token);
        var dashboardResponse = await _client.SendAsync(dashboard);
        Assert.Equal(HttpStatusCode.OK, dashboardResponse.StatusCode);
        var dashboardBody = await dashboardResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Personal", dashboardBody.GetProperty("accountClass").GetString());
        Assert.True(dashboardBody.GetProperty("utangAvailable").GetBoolean());

        using var profile = Authed(HttpMethod.Get, "/api/v1/personal/profile", token);
        var profileResponse = await _client.SendAsync(profile);
        Assert.Equal(HttpStatusCode.OK, profileResponse.StatusCode);

        using var settingsGet = Authed(HttpMethod.Get, "/api/v1/personal/settings", token);
        var settingsResponse = await _client.SendAsync(settingsGet);
        Assert.Equal(HttpStatusCode.OK, settingsResponse.StatusCode);
        var settingsBody = await settingsResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(settingsBody.GetProperty("emailNotificationsEnabled").GetBoolean());

        using var settingsPut = Authed(HttpMethod.Put, "/api/v1/personal/settings", token);
        settingsPut.Content = JsonContent.Create(new
        {
            emailNotificationsEnabled = false,
            pushNotificationsEnabled = true,
            inAppNotificationsEnabled = true,
            reminderNotificationsEnabled = false,
            expectedVersion = settingsBody.GetProperty("version").GetInt32()
        });
        var updated = await _client.SendAsync(settingsPut);
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        var updatedBody = await updated.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(updatedBody.GetProperty("emailNotificationsEnabled").GetBoolean());
    }

    [Fact]
    public async Task Platform_session_cannot_access_personal_dashboard()
    {
        var (userId, username, password) = await PlatformIntegrationTestUsers.CreatePlatformStaffWithPasswordAsync(_admin, "pplat");
        (await _admin.PostAsJsonAsync(
            "/api/v1/platform/authorization/assignments",
            new { platformUserId = userId, role = "PlatformAdministrator" })).EnsureSuccessStatusCode();

        var login = await _client.PostAsJsonAsync(
            "/api/v1/platform/auth/login",
            new { usernameOrEmail = username, password });
        var token = (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("sessionToken").GetString()!;

        using var dashboard = Authed(HttpMethod.Get, "/api/v1/personal/dashboard", token);
        var response = await _client.SendAsync(dashboard);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(ApplicationErrorCodes.AccountScopeDenied, body.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task Personal_profile_update_persists_display_name_for_self_only()
    {
        var token = await LoginPersonalAsync();

        using var getBefore = Authed(HttpMethod.Get, "/api/v1/personal/profile", token);
        var beforeResponse = await _client.SendAsync(getBefore);
        Assert.Equal(HttpStatusCode.OK, beforeResponse.StatusCode);
        var before = await beforeResponse.Content.ReadFromJsonAsync<JsonElement>();
        var originalName = before.GetProperty("displayName").GetString()!;
        var username = before.GetProperty("username").GetString()!;
        var email = before.GetProperty("email").GetString()!;
        var accountClass = before.GetProperty("accountClass").GetString()!;
        var userId = before.GetProperty("userIdentityId").GetGuid();
        Assert.False(string.IsNullOrWhiteSpace(originalName));

        using var blank = Authed(HttpMethod.Put, "/api/v1/personal/profile", token);
        blank.Content = JsonContent.Create(new { displayName = "   " });
        var blankResponse = await _client.SendAsync(blank);
        Assert.Equal(HttpStatusCode.BadRequest, blankResponse.StatusCode);
        var blankBody = await blankResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(DomainErrorCodes.InvalidDisplayName, blankBody.GetProperty("errorCode").GetString());

        using var tooShort = Authed(HttpMethod.Put, "/api/v1/personal/profile", token);
        tooShort.Content = JsonContent.Create(new { displayName = "A" });
        var tooShortResponse = await _client.SendAsync(tooShort);
        Assert.Equal(HttpStatusCode.BadRequest, tooShortResponse.StatusCode);

        var nextName = $"Renamed {Unique("n")}";
        using var update = Authed(HttpMethod.Put, "/api/v1/personal/profile", token);
        update.Content = JsonContent.Create(new { displayName = $"  {nextName}  " });
        var updateResponse = await _client.SendAsync(update);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = await updateResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(nextName, updated.GetProperty("displayName").GetString());
        Assert.Equal(username, updated.GetProperty("username").GetString());
        Assert.Equal(email, updated.GetProperty("email").GetString());
        Assert.Equal(accountClass, updated.GetProperty("accountClass").GetString());
        Assert.Equal(userId, updated.GetProperty("userIdentityId").GetGuid());

        using var getAfter = Authed(HttpMethod.Get, "/api/v1/personal/profile", token);
        var afterResponse = await _client.SendAsync(getAfter);
        Assert.Equal(HttpStatusCode.OK, afterResponse.StatusCode);
        var after = await afterResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(nextName, after.GetProperty("displayName").GetString());

        using var same = Authed(HttpMethod.Put, "/api/v1/personal/profile", token);
        same.Content = JsonContent.Create(new { displayName = nextName });
        var sameResponse = await _client.SendAsync(same);
        Assert.Equal(HttpStatusCode.OK, sameResponse.StatusCode);

        var otherToken = await LoginPersonalAsync();
        using var otherUpdate = Authed(HttpMethod.Put, "/api/v1/personal/profile", otherToken);
        otherUpdate.Content = JsonContent.Create(new { displayName = $"Other {Unique("o")}" });
        Assert.Equal(HttpStatusCode.OK, (await _client.SendAsync(otherUpdate)).StatusCode);

        using var firstAgain = Authed(HttpMethod.Get, "/api/v1/personal/profile", token);
        var firstBody = await (await _client.SendAsync(firstAgain)).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(nextName, firstBody.GetProperty("displayName").GetString());
    }

    [Fact]
    public async Task Unauthenticated_profile_update_is_rejected()
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, "/api/v1/personal/profile")
        {
            Content = JsonContent.Create(new { displayName = "No Session" })
        };
        var response = await _client.SendAsync(request);
        Assert.True(
            response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden,
            $"Expected 401/403, got {(int)response.StatusCode}");
    }
}
