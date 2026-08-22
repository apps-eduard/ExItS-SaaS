using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ExItS.Platform.Api.Common;
using ExItS.Platform.IntegrationTests.Support;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ExItS.Platform.IntegrationTests;

[Collection(PostgreSqlCollection.Name)]
public sealed class ApiBrowserAntiforgeryTests(PostgreSqlFixture fixture) : IAsyncLifetime
{
    private SessionApiFactory _factory = null!;
    private HttpClient _browser = null!;
    private HttpClient _headerClient = null!;

    public Task InitializeAsync()
    {
        _factory = new SessionApiFactory(fixture.ConnectionString);
        _browser = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        _headerClient = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _browser.Dispose();
        _headerClient.Dispose();
        _factory.Dispose();
        return Task.CompletedTask;
    }

    private async Task<(string Username, string Password)> SeedPlatformStaffAsync()
    {
        var (_, username, password) = await PlatformIntegrationTestUsers.CreatePlatformStaffWithPasswordAsync(
            _headerClient,
            "csrf");
        return (username, password);
    }

    private async Task LoginWithCookieAsync(string username, string password)
    {
        var login = await _browser.PostAsJsonAsync(
            "/api/v1/platform/auth/login",
            new { usernameOrEmail = username, password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
    }

    private async Task<string> FetchAntiforgeryTokenAsync(HttpClient client)
    {
        var response = await client.GetAsync(PlatformAntiforgeryDefaults.TokenRoute);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var token = body.GetProperty("token").GetString();
        Assert.False(string.IsNullOrWhiteSpace(token));
        Assert.Equal(PlatformAntiforgeryDefaults.HeaderName, body.GetProperty("headerName").GetString());
        return token!;
    }

    [Fact]
    public async Task Cookie_session_mutation_without_antiforgery_is_rejected()
    {
        var (username, password) = await SeedPlatformStaffAsync();
        await LoginWithCookieAsync(username, password);

        var logout = await _browser.PostAsync("/api/v1/platform/auth/logout", content: null);
        Assert.Equal(HttpStatusCode.BadRequest, logout.StatusCode);
        var problem = await logout.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(PlatformAntiforgeryDefaults.InvalidErrorCode, problem.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task Cookie_session_mutation_with_invalid_antiforgery_is_rejected()
    {
        var (username, password) = await SeedPlatformStaffAsync();
        await LoginWithCookieAsync(username, password);
        _ = await FetchAntiforgeryTokenAsync(_browser);

        using var logout = new HttpRequestMessage(HttpMethod.Post, "/api/v1/platform/auth/logout");
        logout.Headers.Add(PlatformAntiforgeryDefaults.HeaderName, "invalid-token");
        var response = await _browser.SendAsync(logout);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Cookie_session_mutation_with_valid_antiforgery_reaches_endpoint()
    {
        var (username, password) = await SeedPlatformStaffAsync();
        await LoginWithCookieAsync(username, password);
        var token = await FetchAntiforgeryTokenAsync(_browser);

        using var logout = new HttpRequestMessage(HttpMethod.Post, "/api/v1/platform/auth/logout");
        logout.Headers.Add(PlatformAntiforgeryDefaults.HeaderName, token);
        var response = await _browser.SendAsync(logout);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Header_session_mutation_does_not_require_antiforgery()
    {
        var (username, password) = await SeedPlatformStaffAsync();
        var login = await _headerClient.PostAsJsonAsync(
            "/api/v1/platform/auth/login",
            new { usernameOrEmail = username, password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var body = await login.Content.ReadFromJsonAsync<JsonElement>();
        var sessionToken = body.GetProperty("sessionToken").GetString();
        Assert.False(string.IsNullOrWhiteSpace(sessionToken));

        using var logout = new HttpRequestMessage(HttpMethod.Post, "/api/v1/platform/auth/logout");
        logout.Headers.Add("X-ExItS-Session-Token", sessionToken);
        var response = await _headerClient.SendAsync(logout);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Get_requests_remain_unaffected_with_cookie_session()
    {
        var (username, password) = await SeedPlatformStaffAsync();
        await LoginWithCookieAsync(username, password);

        var me = await _browser.GetAsync("/api/v1/platform/auth/me");
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
    }

    [Fact]
    public async Task Antiforgery_token_without_session_does_not_authenticate_protected_mutation()
    {
        var token = await FetchAntiforgeryTokenAsync(_browser);

        using var request = new HttpRequestMessage(
            HttpMethod.Put,
            "/api/v1/platform/auth/organization-context");
        request.Headers.Add(PlatformAntiforgeryDefaults.HeaderName, token);
        request.Content = JsonContent.Create(new { organizationId = Guid.NewGuid() });
        var response = await _browser.SendAsync(request);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Antiforgery_bootstrap_does_not_persist_in_response_body_for_get_only_contract()
    {
        var response = await _browser.GetAsync(PlatformAntiforgeryDefaults.TokenRoute);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.TryGetProperty("token", out var token));
        Assert.False(string.IsNullOrWhiteSpace(token.GetString()));
    }
}
