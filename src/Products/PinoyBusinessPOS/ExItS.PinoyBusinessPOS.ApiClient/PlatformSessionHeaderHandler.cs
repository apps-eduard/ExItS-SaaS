using System.Net.Http.Headers;
using ExItS.PinoyBusinessPOS.Application.Abstractions;

namespace ExItS.PinoyBusinessPOS.ApiClient;

/// <summary>
/// Attaches Platform session auth for Personal/Org Owner Platform routes.
/// Bearer POS token routes under introspect remain Bearer-first.
/// <c>/auth/token</c> attaches PlatformSession when a session exists so GrantType=session works
/// (password grant ignores the session header).
/// </summary>
public sealed class PlatformSessionHeaderHandler(ICurrentUserContext currentUser) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var sessionToken = currentUser.Session?.PlatformSessionToken;
        if (!string.IsNullOrWhiteSpace(sessionToken) && RequiresPlatformSession(request.RequestUri))
        {
            request.Headers.Remove("Authorization");
            request.Headers.Authorization = new AuthenticationHeaderValue("PlatformSession", sessionToken);
            if (!request.Headers.Contains("X-ExItS-Session-Token"))
            {
                request.Headers.TryAddWithoutValidation("X-ExItS-Session-Token", sessionToken);
            }
        }

        return base.SendAsync(request, cancellationToken);
    }

    private static bool RequiresPlatformSession(Uri? uri)
    {
        if (uri is null)
        {
            return false;
        }

        var path = uri.IsAbsoluteUri ? uri.AbsolutePath : uri.OriginalString;
        if (path.StartsWith("/api/v1/personal", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/api/v1/organizations", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/api/v1/commercial", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (!path.StartsWith("/api/v1/platform", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // Introspect must keep Authorization: Bearer.
        if (path.Equals("/api/v1/platform/auth/introspect", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // Token issue: attach session when present (needed for GrantType=session).
        if (path.StartsWith("/api/v1/platform/auth/token", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Session-scoped auth helpers (eligible orgs + org context) require Platform session.
        if (path.Equals("/api/v1/platform/auth/organizations", StringComparison.OrdinalIgnoreCase)
            || path.Equals("/api/v1/platform/auth/organization-context", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Anonymous credential bootstrap endpoints — never attach a stale session.
        if (path.Equals("/api/v1/platform/auth/login", StringComparison.OrdinalIgnoreCase)
            || path.Equals("/api/v1/platform/auth/register", StringComparison.OrdinalIgnoreCase)
            || path.Equals("/api/v1/platform/auth/activate-account", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }
}
