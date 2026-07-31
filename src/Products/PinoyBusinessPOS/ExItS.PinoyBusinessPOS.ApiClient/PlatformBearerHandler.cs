using System.Net.Http.Headers;
using ExItS.PinoyBusinessPOS.Application.Abstractions;

namespace ExItS.PinoyBusinessPOS.ApiClient;

/// <summary>
/// Attaches <c>Authorization: Bearer</c> from the restored session access token when present.
/// </summary>
public sealed class PlatformBearerHandler(ICurrentUserContext currentUser) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = currentUser.Session?.AccessToken;
        if (!string.IsNullOrWhiteSpace(token)
            && request.Headers.Authorization is null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
