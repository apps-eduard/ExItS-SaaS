using System.Net.Http.Headers;
using ExItS.PinoyBusinessPOS.Application.Abstractions;

namespace ExItS.PinoyBusinessPOS.ApiClient;

/// <summary>
/// Adds Development/Testing <c>X-Dev-Platform-User-Id</c> from the restored session when no Bearer
/// token is present. Preserves an existing Authorization Bearer header.
/// </summary>
public sealed class DevPlatformUserHeaderHandler(
    ICurrentUserContext currentUser,
    IAppInfoService appInfo) : DelegatingHandler
{
    public const string HeaderName = "X-Dev-Platform-User-Id";

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var hasBearer = request.Headers.Authorization?.Scheme.Equals("Bearer", StringComparison.OrdinalIgnoreCase) == true
                        && !string.IsNullOrWhiteSpace(request.Headers.Authorization.Parameter);

        if (!hasBearer && !string.IsNullOrWhiteSpace(currentUser.Session?.AccessToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", currentUser.Session.AccessToken);
            hasBearer = true;
        }

        if (hasBearer)
        {
            // Do not clear Authorization and do not forge Dev identity when Bearer is set.
            return base.SendAsync(request, cancellationToken);
        }

        var isDevLike = string.Equals(appInfo.EnvironmentName, "Development", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(appInfo.EnvironmentName, "Testing", StringComparison.OrdinalIgnoreCase);

        if (isDevLike && currentUser.Session is { UserId: var userId })
        {
            request.Headers.Remove(HeaderName);
            request.Headers.TryAddWithoutValidation(HeaderName, userId.ToString("D"));
        }

        // No Bearer: ensure we do not leak a stale Authorization header on Dev-header path.
        request.Headers.Authorization = null;

        return base.SendAsync(request, cancellationToken);
    }
}
