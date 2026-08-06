using System.Net;
using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Auth;

namespace ExItS.PinoyBusinessPOS.ApiClient.Tests;

public sealed class PosPlatformSessionForwardingHandlerTests
{
    [Fact]
    public async Task Forwards_session_token_header_without_replacing_bearer()
    {
        HttpRequestMessage? captured = null;
        var inner = new CaptureHandler(request =>
        {
            captured = request;
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        var user = new StubUserContext(new AuthSession(
            Guid.NewGuid(),
            "Peter Paul",
            "peter",
            "peter@gmail.com",
            OrganizationId: Guid.NewGuid(),
            OrganizationDisplayName: "Store",
            IssuedAtUtc: DateTimeOffset.UtcNow,
            ExpiresAtUtc: DateTimeOffset.UtcNow.AddHours(1),
            HasPosAccess: true,
            AccessReasonCode: null,
            AccessToken: "access-token",
            PlatformSessionToken: "session-token-xyz"));

        var handler = new PlatformBearerHandler(user)
        {
            InnerHandler = new PosPlatformSessionForwardingHandler(user) { InnerHandler = inner }
        };
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://pos.test") };

        _ = await client.PostAsync("/api/v1/pos/catalog-imports/template", content: null);

        Assert.NotNull(captured);
        Assert.Equal("Bearer", captured!.Headers.Authorization?.Scheme);
        Assert.Equal("access-token", captured.Headers.Authorization?.Parameter);
        Assert.Contains(
            captured.Headers,
            h => h.Key.Equals(PosPlatformSessionForwardingHandler.HeaderName, StringComparison.OrdinalIgnoreCase)
                 && h.Value.Contains("session-token-xyz"));
    }

    [Fact]
    public async Task Skips_header_when_session_token_missing()
    {
        HttpRequestMessage? captured = null;
        var inner = new CaptureHandler(request =>
        {
            captured = request;
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        var user = new StubUserContext(new AuthSession(
            Guid.NewGuid(),
            "Peter Paul",
            "peter",
            "peter@gmail.com",
            OrganizationId: Guid.NewGuid(),
            OrganizationDisplayName: "Store",
            IssuedAtUtc: DateTimeOffset.UtcNow,
            ExpiresAtUtc: DateTimeOffset.UtcNow.AddHours(1),
            HasPosAccess: true,
            AccessReasonCode: null,
            AccessToken: "access-token",
            PlatformSessionToken: null));

        var handler = new PosPlatformSessionForwardingHandler(user) { InnerHandler = inner };
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://pos.test") };

        _ = await client.GetAsync("/api/v1/pos/catalog/products");

        Assert.NotNull(captured);
        Assert.DoesNotContain(
            captured!.Headers,
            h => h.Key.Equals(PosPlatformSessionForwardingHandler.HeaderName, StringComparison.OrdinalIgnoreCase));
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

    private sealed class CaptureHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(responder(request));
    }
}
