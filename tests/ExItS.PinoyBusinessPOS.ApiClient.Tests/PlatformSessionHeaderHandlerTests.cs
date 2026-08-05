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
