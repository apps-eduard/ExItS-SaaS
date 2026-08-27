using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using ExItS.Platform.Application.Common;
using ExItS.Platform.IntegrationTests.Support;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ExItS.Platform.IntegrationTests;

[Collection(PostgreSqlCollection.Name)]
public sealed class ApiAuthPublicSurfaceTests(PostgreSqlFixture fixture) : IAsyncLifetime
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
    public async Task Unknown_public_surface_is_rejected_and_callback_url_is_ignored()
    {
        var unknown = await _client.PostAsJsonAsync(
            "/api/v1/platform/auth/register",
            new
            {
                displayName = "Surface User",
                email = $"surface.{Guid.NewGuid():N}@example.com",
                publicSurface = "https://evil.example/callback"
            });
        Assert.Equal(HttpStatusCode.BadRequest, unknown.StatusCode);
        var problem = await unknown.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(
            ApplicationErrorCodes.AuthPublicSurfaceInvalid,
            problem.GetProperty("errorCode").GetString());

        using var ignoredCallback = new StringContent(
            """
            {"displayName":"Ignored Url","email":"ignored-callback@example.com","callbackUrl":"https://evil.example/hijack","redirectUrl":"https://evil.example","returnUrl":"https://evil.example","origin":"https://evil.example"}
            """,
            Encoding.UTF8,
            "application/json");
        var register = await _client.PostAsync("/api/v1/platform/auth/register", ignoredCallback);
        Assert.Equal(HttpStatusCode.OK, register.StatusCode);

        var forgotUnknown = await _client.PostAsJsonAsync(
            "/api/v1/platform/auth/forgot-password",
            new { usernameOrEmail = "nobody@example.com", publicSurface = "unknown-surface" });
        Assert.Equal(HttpStatusCode.BadRequest, forgotUnknown.StatusCode);
    }

    [Fact]
    public async Task Known_plm_surface_is_accepted_without_changing_admin_forgot_ack()
    {
        var email = $"plm.surface.{Guid.NewGuid():N}@example.com";
        var register = await _client.PostAsJsonAsync(
            "/api/v1/platform/auth/register",
            new
            {
                displayName = "PLM Surface",
                email,
                publicSurface = "pinoy-loan-manager"
            });
        Assert.Equal(HttpStatusCode.OK, register.StatusCode);

        var forgot = await _client.PostAsJsonAsync(
            "/api/v1/platform/auth/forgot-password",
            new { usernameOrEmail = email, publicSurface = "pinoy-loan-manager" });
        Assert.Equal(HttpStatusCode.OK, forgot.StatusCode);
        var forgotBody = await forgot.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains("eligible account", forgotBody.GetProperty("message").GetString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Known_pos_surface_is_accepted_without_changing_forgot_ack()
    {
        var email = $"pos.surface.{Guid.NewGuid():N}@example.com";
        var register = await _client.PostAsJsonAsync(
            "/api/v1/platform/auth/register",
            new
            {
                displayName = "POS Surface",
                email,
                publicSurface = "pinoy-business-pos"
            });
        Assert.Equal(HttpStatusCode.OK, register.StatusCode);

        var forgot = await _client.PostAsJsonAsync(
            "/api/v1/platform/auth/forgot-password",
            new { usernameOrEmail = email, publicSurface = "pinoy-business-pos" });
        Assert.Equal(HttpStatusCode.OK, forgot.StatusCode);
        var forgotBody = await forgot.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains("eligible account", forgotBody.GetProperty("message").GetString(), StringComparison.OrdinalIgnoreCase);
    }
}
