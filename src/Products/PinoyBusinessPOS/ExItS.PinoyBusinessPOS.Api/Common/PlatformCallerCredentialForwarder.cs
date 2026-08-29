using Microsoft.AspNetCore.Http;

namespace ExItS.PinoyBusinessPOS.Api.Common;

/// <summary>
/// Copies caller credentials onto POS → Platform HttpClient requests.
/// Platform organization APIs authenticate via cookie / PlatformSession / session header —
/// not product Bearer access tokens. Forwarding Bearer causes false 403/empty branch lookups.
/// </summary>
internal static class PlatformCallerCredentialForwarder
{
    /// <summary>
    /// Default Platform browser session cookie name (see PlatformAuthOptions.CookieName).
    /// React POS keeps the session here (HttpOnly) and sends product Bearer to POS only.
    /// </summary>
    public const string PlatformAuthCookieName = ".ExItS.Platform.Auth";

    public static void CopyTo(HttpRequest? source, HttpRequestMessage platformRequest)
    {
        if (source is null)
        {
            return;
        }

        // Prefer reconstructing Cookie from the parsed cookie collection — some hosts
        // do not expose the raw Cookie header on HttpRequest.Headers.
        if (source.Cookies.Count > 0)
        {
            var cookieHeader = string.Join(
                "; ",
                source.Cookies.Select(static cookie => $"{cookie.Key}={cookie.Value}"));
            if (!string.IsNullOrWhiteSpace(cookieHeader))
            {
                platformRequest.Headers.TryAddWithoutValidation("Cookie", cookieHeader);
            }
        }
        else if (source.Headers.TryGetValue("Cookie", out var cookies))
        {
            platformRequest.Headers.TryAddWithoutValidation("Cookie", cookies.ToArray());
        }

        if (source.Headers.TryGetValue("X-ExItS-Session-Token", out var sessionHeader))
        {
            platformRequest.Headers.TryAddWithoutValidation("X-ExItS-Session-Token", sessionHeader.ToArray());
        }

        if (source.Headers.TryGetValue("Authorization", out var authorizationValues))
        {
            var authorization = authorizationValues.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(authorization)
                && authorization.StartsWith("PlatformSession ", StringComparison.OrdinalIgnoreCase))
            {
                platformRequest.Headers.TryAddWithoutValidation("Authorization", authorization);
            }
        }

        if (source.Headers.TryGetValue("X-Dev-Platform-User-Id", out var devUser))
        {
            platformRequest.Headers.TryAddWithoutValidation("X-Dev-Platform-User-Id", devUser.ToArray());
        }
    }

    /// <summary>
    /// Resolves a Platform session token for POS → Platform calls that must use
    /// <c>Authorization: PlatformSession</c> (avoids cookie+CSRF requirements on Platform POSTs).
    /// Order: <c>X-ExItS-Session-Token</c>, <c>PlatformSession</c> Authorization, then auth cookie.
    /// </summary>
    public static string? ResolvePlatformSessionToken(HttpRequest? source)
    {
        if (source is null)
        {
            return null;
        }

        var header = source.Headers["X-ExItS-Session-Token"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(header))
        {
            return header.Trim();
        }

        var auth = source.Headers.Authorization.FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(auth)
            && auth.StartsWith("PlatformSession ", StringComparison.OrdinalIgnoreCase))
        {
            var token = auth["PlatformSession ".Length..].Trim();
            if (!string.IsNullOrWhiteSpace(token))
            {
                return token;
            }
        }

        if (source.Cookies.TryGetValue(PlatformAuthCookieName, out var cookieToken)
            && !string.IsNullOrWhiteSpace(cookieToken))
        {
            return cookieToken.Trim();
        }

        return null;
    }
}
