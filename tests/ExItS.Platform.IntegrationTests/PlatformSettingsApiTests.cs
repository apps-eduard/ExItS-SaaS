using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ExItS.Platform.IntegrationTests.Support;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ExItS.Platform.IntegrationTests;

[Collection(PostgreSqlCollection.Name)]
public sealed class PlatformSettingsApiTests(PostgreSqlFixture fixture) : IAsyncLifetime
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

    private async Task<string> LoginAsPlatformAdministratorAsync()
    {
        var (_, username, password) = await PlatformIntegrationTestUsers.CreatePlatformStaffWithPasswordAsync(
            _admin,
            "settings",
            platformRole: "PlatformAdministrator");
        var login = await _client.PostAsJsonAsync(
            "/api/v1/platform/auth/login",
            new { usernameOrEmail = username, password });
        login.EnsureSuccessStatusCode();
        return (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("sessionToken").GetString()!;
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
    public async Task Platform_administrator_can_read_and_update_general_settings_without_exposing_secrets()
    {
        var token = await LoginAsPlatformAdministratorAsync();

        var get = await _client.SendAsync(Authed(HttpMethod.Get, "/api/v1/platform/settings/general", token));
        get.EnsureSuccessStatusCode();
        var initial = await get.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("ExItS", initial.GetProperty("platformDisplayName").GetString());

        var put = await _client.SendAsync(
            Authed(
                HttpMethod.Put,
                "/api/v1/platform/settings/general",
                token,
                new
                {
                    platformDisplayName = "ExItS Platform",
                    supportEmail = "support@example.test",
                    expectedVersion = initial.GetProperty("version").GetInt32(),
                }));
        put.EnsureSuccessStatusCode();
        var updated = await put.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("ExItS Platform", updated.GetProperty("platformDisplayName").GetString());
        Assert.Equal("support@example.test", updated.GetProperty("supportEmail").GetString());
    }

    [Fact]
    public async Task Email_settings_never_return_smtp_password_and_support_replace_flag()
    {
        var token = await LoginAsPlatformAdministratorAsync();

        var get = await _client.SendAsync(Authed(HttpMethod.Get, "/api/v1/platform/settings/email", token));
        get.EnsureSuccessStatusCode();
        var json = await get.Content.ReadAsStringAsync();
        Assert.DoesNotContain("smtpPassword", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("protected", json, StringComparison.OrdinalIgnoreCase);

        var initial = JsonDocument.Parse(json).RootElement;
        var put = await _client.SendAsync(
            Authed(
                HttpMethod.Put,
                "/api/v1/platform/settings/email",
                token,
                new
                {
                    providerMode = "Smtp",
                    smtpHost = "mailpit",
                    smtpPort = 1025,
                    smtpUsername = "smtp-user",
                    replacePassword = true,
                    smtpPassword = "secret-value",
                    fromDisplayName = "ExItS",
                    fromAddress = "noreply@example.test",
                    securityMode = "None",
                    adminPublicBaseUrl = "http://localhost:8095",
                    expectedVersion = initial.GetProperty("version").GetInt32(),
                }));
        put.EnsureSuccessStatusCode();
        var updated = await put.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(updated.GetProperty("passwordConfigured").GetBoolean());
        Assert.False(updated.TryGetProperty("smtpPassword", out _));
    }

    [Fact]
    public async Task Non_platform_administrator_is_forbidden()
    {
        var (_, username, password) = await PlatformIntegrationTestUsers.CreatePlatformStaffWithPasswordAsync(
            _admin,
            "settings-support",
            platformRole: "PlatformSupport");
        var login = await _client.PostAsJsonAsync(
            "/api/v1/platform/auth/login",
            new { usernameOrEmail = username, password });
        login.EnsureSuccessStatusCode();
        var token = (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("sessionToken").GetString()!;

        var response = await _client.SendAsync(Authed(HttpMethod.Get, "/api/v1/platform/settings/general", token));
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
