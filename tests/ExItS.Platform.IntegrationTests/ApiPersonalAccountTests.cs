using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ExItS.Platform.Application.Common;
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
}
