using System.Net.Http.Headers;
using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Auth;

namespace ExItS.PinoyBusinessPOS.ApiClient;

/// <summary>
/// Adds Development/Testing <c>X-Dev-Platform-User-Id</c> from the restored session.
/// Disabled when no authenticated user is present. Never attaches passwords or Bearer tokens
/// (production JWT is not implemented).
/// </summary>
public sealed class DevPlatformUserHeaderHandler(ICurrentUserContext currentUser) : DelegatingHandler
{
    public const string HeaderName = "X-Dev-Platform-User-Id";

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (currentUser.Session is { UserId: var userId })
        {
            request.Headers.Remove(HeaderName);
            request.Headers.TryAddWithoutValidation(HeaderName, userId.ToString("D"));
        }

        // Never leak Authorization for this development-stage client.
        request.Headers.Authorization = null;

        return base.SendAsync(request, cancellationToken);
    }
}
