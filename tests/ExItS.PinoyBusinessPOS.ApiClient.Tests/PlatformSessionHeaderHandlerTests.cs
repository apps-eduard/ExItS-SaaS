using System.Net;
using System.Net.Http.Headers;
using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Auth;

namespace ExItS.PinoyBusinessPOS.ApiClient.Tests;

public sealed class PlatformSessionHeaderHandlerTests
{
    [Theory]
    [InlineData("/api/v1/me/public-identity")]
    [InlineData("/api/v1/users/resolve-public-id")]
    [InlineData("/api/v1/qr/resolve")]
    [InlineData("/api/v1/personal/dashboard")]
    public async Task Public_identity_and_personal_routes_use_platform_session_not_bearer(string path)
    {
        HttpRequestMessage? captured = null;
        var inner = new CaptureHandler(request =>
        {
            captured = request;
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        var user = new StubUserContext(new AuthSession(
            Guid.NewGuid(),
            "Mica Uy",
            "mica.uy",
            "mica@example.com",
            OrganizationId: null,
            OrganizationDisplayName: null,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddHours(1),
            HasPosAccess: false,
            AccessReasonCode: null,
            AccessToken: "bearer-should-not-win",
            PlatformSessionToken: "session-token-abc"));

        var handler = new PlatformSessionHeaderHandler(user) { InnerHandler = inner };
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://platform.test") };

        _ = await client.GetAsync(path);

        Assert.NotNull(captured);
        Assert.Equal("PlatformSession", captured!.Headers.Authorization?.Scheme);
        Assert.Equal("session-token-abc", captured.Headers.Authorization?.Parameter);
        Assert.Contains(
            captured.Headers,
            h => h.Key.Equals("X-ExItS-Session-Token", StringComparison.OrdinalIgnoreCase)
                 && h.Value.Contains("session-token-abc"));
    }

    [Theory]
    [InlineData("/api/v1/catalog")]
    [InlineData("/api/v1/catalog/products/search")]
    [InlineData("/api/v1/catalog/products/search?q=tuna&page=1&pageSize=20")]
    [InlineData("/api/v1/catalog/categories?page=1&pageSize=100")]
    [InlineData("/api/v1/catalog/templates")]
    public async Task Merchant_catalog_routes_use_platform_session(string path)
    {
        HttpRequestMessage? captured = null;
        var inner = new CaptureHandler(request =>
        {
            captured = request;
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        var user = new StubUserContext(Session(
            accessToken: "bearer-should-not-win",
            platformSessionToken: "session-token-abc"));

        var handler = new PlatformSessionHeaderHandler(user) { InnerHandler = inner };
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://platform.test") };

        _ = await client.GetAsync(path);

        Assert.NotNull(captured);
        Assert.Equal("PlatformSession", captured!.Headers.Authorization?.Scheme);
        Assert.Equal("session-token-abc", captured.Headers.Authorization?.Parameter);
        Assert.Contains(
            captured.Headers,
            h => h.Key.Equals("X-ExItS-Session-Token", StringComparison.OrdinalIgnoreCase)
                 && h.Value.Contains("session-token-abc"));
    }

    [Theory]
    [InlineData("/api/v1/pos/catalog/products")]
    [InlineData("/api/v1/pos/sales")]
    [InlineData("/api/v1/pos/catalog-imports")]
    public async Task Unrelated_pos_business_routes_do_not_receive_platform_session(string path)
    {
        HttpRequestMessage? captured = null;
        var inner = new CaptureHandler(request =>
        {
            captured = request;
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        var user = new StubUserContext(Session(
            accessToken: "bearer-token",
            platformSessionToken: "session-token-abc"));

        var handler = new PlatformSessionHeaderHandler(user) { InnerHandler = inner };
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://platform.test") };

        _ = await client.GetAsync(path);

        Assert.NotNull(captured);
        // POS business routes keep their own auth (Bearer/dev headers added by other handlers);
        // this handler must never inject a PlatformSession credential onto them.
        Assert.NotEqual("PlatformSession", captured!.Headers.Authorization?.Scheme);
        Assert.DoesNotContain(
            captured.Headers,
            h => h.Key.Equals("X-ExItS-Session-Token", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Missing_session_token_does_not_attach_platform_session_to_catalog()
    {
        HttpRequestMessage? captured = null;
        var inner = new CaptureHandler(request =>
        {
            captured = request;
            return new HttpResponseMessage(HttpStatusCode.Unauthorized);
        });

        var user = new StubUserContext(Session(
            accessToken: "bearer-token",
            platformSessionToken: null));

        var handler = new PlatformSessionHeaderHandler(user) { InnerHandler = inner };
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://platform.test") };

        var response = await client.GetAsync("/api/v1/catalog/products/search");

        Assert.NotNull(captured);
        // No session -> no PlatformSession header; the server-side RequireAuthorization still 401s.
        Assert.NotEqual("PlatformSession", captured!.Headers.Authorization?.Scheme);
        Assert.DoesNotContain(
            captured.Headers,
            h => h.Key.Equals("X-ExItS-Session-Token", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Introspect_keeps_bearer_and_does_not_force_platform_session()
    {
        HttpRequestMessage? captured = null;
        var inner = new CaptureHandler(request =>
        {
            captured = request;
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        var user = new StubUserContext(new AuthSession(
            Guid.NewGuid(),
            "Mica Uy",
            "mica.uy",
            "mica@example.com",
            OrganizationId: null,
            OrganizationDisplayName: null,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddHours(1),
            HasPosAccess: false,
            AccessReasonCode: null,
            AccessToken: "bearer-token",
            PlatformSessionToken: "session-token-abc"));

        var sessionOuter = new PlatformSessionHeaderHandler(user)
        {
            InnerHandler = new PlatformBearerHandler(user) { InnerHandler = inner }
        };
        using var client = new HttpClient(sessionOuter) { BaseAddress = new Uri("http://platform.test") };

        _ = await client.GetAsync("/api/v1/platform/auth/introspect");

        Assert.NotNull(captured);
        // Introspect must not force PlatformSession; Bearer handler attaches access token.
        Assert.Equal("Bearer", captured!.Headers.Authorization?.Scheme);
        Assert.Equal("bearer-token", captured.Headers.Authorization?.Parameter);
    }

    [Fact]
    public async Task Token_bind_keeps_bearer_and_does_not_force_platform_session()
    {
        HttpRequestMessage? captured = null;
        var inner = new CaptureHandler(request =>
        {
            captured = request;
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        var user = new StubUserContext(new AuthSession(
            Guid.NewGuid(),
            "Mica Uy",
            "mica.uy",
            "mica@example.com",
            OrganizationId: null,
            OrganizationDisplayName: null,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddHours(1),
            HasPosAccess: false,
            AccessReasonCode: null,
            AccessToken: "bearer-token",
            PlatformSessionToken: "session-token-abc"));

        var sessionOuter = new PlatformSessionHeaderHandler(user)
        {
            InnerHandler = new PlatformBearerHandler(user) { InnerHandler = inner }
        };
        using var client = new HttpClient(sessionOuter) { BaseAddress = new Uri("http://platform.test") };

        _ = await client.PostAsync("/api/v1/platform/auth/token/bind", null);

        Assert.NotNull(captured);
        Assert.Equal("Bearer", captured!.Headers.Authorization?.Scheme);
        Assert.Equal("bearer-token", captured.Headers.Authorization?.Parameter);
    }

    private static AuthSession Session(string? accessToken, string? platformSessionToken) =>
        new(
            Guid.NewGuid(),
            "Mica Uy",
            "mica.uy",
            "mica@example.com",
            OrganizationId: null,
            OrganizationDisplayName: null,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddHours(1),
            HasPosAccess: false,
            AccessReasonCode: null,
            AccessToken: accessToken,
            PlatformSessionToken: platformSessionToken);

    private sealed class StubUserContext(AuthSession? session) : ICurrentUserContext
    {
        public AuthSession? Session { get; private set; } = session;
        public bool IsAuthenticated => Session is not null;
        public bool HasPosAccess => Session?.HasPosAccess == true;
        public event Func<Task>? Changed;
        public void Set(AuthSession? next) => Session = next;
        public void Clear() => Session = null;
    }

    private sealed class CaptureHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(respond(request));
    }
}
