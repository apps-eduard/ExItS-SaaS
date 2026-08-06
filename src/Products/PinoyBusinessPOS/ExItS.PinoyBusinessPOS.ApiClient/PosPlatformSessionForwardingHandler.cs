using ExItS.PinoyBusinessPOS.Application.Abstractions;

namespace ExItS.PinoyBusinessPOS.ApiClient;

/// <summary>
/// Forwards the Platform browser/session token to POS business APIs so server-side
/// Platform merchant catalog calls can authenticate as <c>PlatformSession</c>.
/// Does not replace Authorization Bearer (POS introspection still needs the product access token).
/// </summary>
public sealed class PosPlatformSessionForwardingHandler(ICurrentUserContext currentUser) : DelegatingHandler
{
    public const string HeaderName = "X-ExItS-Session-Token";

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var sessionToken = currentUser.Session?.PlatformSessionToken;
        if (!string.IsNullOrWhiteSpace(sessionToken) && !request.Headers.Contains(HeaderName))
        {
            request.Headers.TryAddWithoutValidation(HeaderName, sessionToken);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
