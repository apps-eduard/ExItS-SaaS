namespace ExItS.Platform.Application.Identity;

/// <summary>
/// Builds the browser-facing Google OAuth callback on the React origin.
/// The React nginx proxy keeps the /platform-api prefix, and the API strips it with PathBase.
/// </summary>
public static class ExternalAuthCallbackUri
{
    public const string ReactPathBase = "/platform-api";
    public const string GoogleCallbackPath = "/api/v1/platform/auth/external/google/callback";

    public static bool TryCreate(string? publicBrowserOrigin, out string redirectUri)
    {
        redirectUri = string.Empty;
        if (string.IsNullOrWhiteSpace(publicBrowserOrigin)
            || !Uri.TryCreate(publicBrowserOrigin.Trim(), UriKind.Absolute, out var origin))
        {
            return false;
        }

        if (!string.Equals(origin.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
            || !string.IsNullOrEmpty(origin.UserInfo)
            || !string.IsNullOrEmpty(origin.Query)
            || !string.IsNullOrEmpty(origin.Fragment)
            || origin.AbsolutePath is not ("/" or ""))
        {
            return false;
        }

        redirectUri = $"{origin.GetLeftPart(UriPartial.Authority)}{ReactPathBase}{GoogleCallbackPath}";
        return true;
    }

    /// <summary>
    /// Browser path for the post-Google completion request.
    /// The public React proxy only forwards URLs that keep the /platform-api prefix.
    /// </summary>
    public static string BrowserCompletePath(string provider, bool includeReactPathBase)
    {
        var path = $"/api/v1/platform/auth/external/{provider}/complete";
        return includeReactPathBase ? ReactPathBase + path : path;
    }
}
