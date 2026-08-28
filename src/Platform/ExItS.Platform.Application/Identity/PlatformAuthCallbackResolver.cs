using System.Globalization;

namespace ExItS.Platform.Application.Identity;

/// <summary>
/// Resolves EmailVerification and PasswordReset callback URLs from a server-selected public surface.
/// Invitation and recovery-email links stay on Admin.
/// Production never derives public origins from request Host/Origin/Referer.
/// Local Validation may align the host with the browser Origin when that origin is in Cors:AllowedOrigins.
/// </summary>
public static class PlatformAuthCallbackResolver
{
    public const string AdminActivatePath = "/admin/activate-account";
    public const string AdminResetPath = "/admin/reset-password";
    public const string ProductActivatePath = "/activate-account";
    public const string ProductResetPath = "/reset-password";

    /// <summary>Alias retained for PLM callers/tests.</summary>
    public const string PinoyLoanManagerActivatePath = ProductActivatePath;

    /// <summary>Alias retained for PLM callers/tests.</summary>
    public const string PinoyLoanManagerResetPath = ProductResetPath;

    public static bool TryCreateLink(
        PlatformAuthOutboundMessage message,
        string adminPublicBaseUrl,
        string? pinoyLoanManagerPublicBaseUrl,
        bool allowHttpLoopbackPublicUrls,
        out string absoluteUrl,
        string? pinoyBusinessPosPublicBaseUrl = null,
        IReadOnlyCollection<string>? allowedHttpOrigins = null)
    {
        absoluteUrl = string.Empty;
        if (message.Kind == PlatformAuthOutboundMessageKinds.EmailVerification)
        {
            return TryCreateVerificationOrResetLink(
                message,
                adminPublicBaseUrl,
                pinoyLoanManagerPublicBaseUrl,
                pinoyBusinessPosPublicBaseUrl,
                allowHttpLoopbackPublicUrls,
                allowedHttpOrigins,
                AdminActivatePath,
                ProductActivatePath,
                out absoluteUrl);
        }

        if (message.Kind == PlatformAuthOutboundMessageKinds.PasswordReset)
        {
            return TryCreateVerificationOrResetLink(
                message,
                adminPublicBaseUrl,
                pinoyLoanManagerPublicBaseUrl,
                pinoyBusinessPosPublicBaseUrl,
                allowHttpLoopbackPublicUrls,
                allowedHttpOrigins,
                AdminResetPath,
                ProductResetPath,
                out absoluteUrl);
        }

        return false;
    }

    /// <summary>
    /// Prefers the browser Origin header; Referer is only used to recover the origin (path stripped).
    /// Never uses the API Host header — that is :8091, not the signup UI.
    /// </summary>
    public static string? ReadBrowserPublicOrigin(string? originHeader, string? refererHeader)
    {
        if (TryParseOrigin(originHeader, out var fromOrigin))
        {
            return fromOrigin;
        }

        if (string.IsNullOrWhiteSpace(refererHeader)
            || !Uri.TryCreate(refererHeader.Trim(), UriKind.Absolute, out var referer))
        {
            return null;
        }

        var authority = referer.GetLeftPart(UriPartial.Authority).TrimEnd('/');
        return TryParseOrigin(authority, out var fromReferer) ? fromReferer : null;
    }

    /// <summary>
    /// Local Validation: if the browser Origin is an allowed CORS origin and uses the same
    /// scheme and port as the configured public base, swap the host so Mailpit links open
    /// on Tailscale when signup/reset happened on Tailscale, or loopback when it happened locally.
    /// Production (<paramref name="allowHttpLoopbackPublicUrls"/> false) never rewrites.
    /// </summary>
    public static string? AlignPublicBaseUrlWithRequestOrigin(
        string? configuredBaseUrl,
        string? requestOrigin,
        IReadOnlyCollection<string>? allowedOrigins,
        bool allowHttpLoopbackPublicUrls)
    {
        if (string.IsNullOrWhiteSpace(configuredBaseUrl))
        {
            return configuredBaseUrl;
        }

        if (!allowHttpLoopbackPublicUrls
            || string.IsNullOrWhiteSpace(requestOrigin)
            || allowedOrigins is null
            || allowedOrigins.Count == 0)
        {
            return configuredBaseUrl;
        }

        if (!TryParseOrigin(requestOrigin, out var origin))
        {
            return configuredBaseUrl;
        }

        if (!ContainsOrigin(allowedOrigins, origin))
        {
            return configuredBaseUrl;
        }

        if (!Uri.TryCreate(configuredBaseUrl, UriKind.Absolute, out var configured)
            || !Uri.TryCreate(origin, UriKind.Absolute, out var originUri))
        {
            return configuredBaseUrl;
        }

        if (!string.Equals(configured.Scheme, originUri.Scheme, StringComparison.OrdinalIgnoreCase)
            || configured.Port != originUri.Port)
        {
            return configuredBaseUrl;
        }

        return IsAllowedPublicBaseUrl(origin, allowHttpLoopbackPublicUrls, allowedOrigins)
            ? origin
            : configuredBaseUrl;
    }

    public static bool IsAllowedPublicBaseUrl(
        string? candidate,
        bool allowHttpLoopbackPublicUrls,
        IReadOnlyCollection<string>? allowedHttpOrigins = null)
    {
        if (!Uri.TryCreate(candidate, UriKind.Absolute, out var uri))
        {
            return false;
        }

        if (!string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment) || !string.IsNullOrEmpty(uri.UserInfo))
        {
            return false;
        }

        if (uri.Scheme == Uri.UriSchemeHttps)
        {
            return true;
        }

        if (!allowHttpLoopbackPublicUrls || uri.Scheme != Uri.UriSchemeHttp)
        {
            return false;
        }

        if (string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase)
            || string.Equals(uri.Host, "127.0.0.1", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var origin = uri.GetLeftPart(UriPartial.Authority).TrimEnd('/');
        return allowedHttpOrigins is not null && ContainsOrigin(allowedHttpOrigins, origin);
    }

    private static bool TryCreateVerificationOrResetLink(
        PlatformAuthOutboundMessage message,
        string adminPublicBaseUrl,
        string? pinoyLoanManagerPublicBaseUrl,
        string? pinoyBusinessPosPublicBaseUrl,
        bool allowHttpLoopbackPublicUrls,
        IReadOnlyCollection<string>? allowedHttpOrigins,
        string adminPath,
        string productPath,
        out string absoluteUrl)
    {
        var encodedToken = Uri.EscapeDataString(message.OpaqueToken ?? string.Empty);
        if (string.Equals(
                message.PublicSurface,
                PlatformAuthPublicSurfaces.PinoyLoanManager,
                StringComparison.Ordinal))
        {
            if (!IsAllowedPublicBaseUrl(pinoyLoanManagerPublicBaseUrl, allowHttpLoopbackPublicUrls, allowedHttpOrigins))
            {
                absoluteUrl = string.Empty;
                return false;
            }

            absoluteUrl = Combine(pinoyLoanManagerPublicBaseUrl!, productPath, encodedToken);
            return true;
        }

        if (string.Equals(
                message.PublicSurface,
                PlatformAuthPublicSurfaces.PinoyBusinessPos,
                StringComparison.Ordinal))
        {
            if (!IsAllowedPublicBaseUrl(pinoyBusinessPosPublicBaseUrl, allowHttpLoopbackPublicUrls, allowedHttpOrigins))
            {
                absoluteUrl = string.Empty;
                return false;
            }

            absoluteUrl = Combine(pinoyBusinessPosPublicBaseUrl!, productPath, encodedToken);
            return true;
        }

        if (string.IsNullOrWhiteSpace(adminPublicBaseUrl))
        {
            absoluteUrl = string.Empty;
            return false;
        }

        absoluteUrl = Combine(adminPublicBaseUrl, adminPath, encodedToken);
        return true;
    }

    private static bool TryParseOrigin(string? value, out string origin)
    {
        origin = string.Empty;
        if (string.IsNullOrWhiteSpace(value) || !Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri))
        {
            return false;
        }

        if (!string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment) || !string.IsNullOrEmpty(uri.UserInfo))
        {
            return false;
        }

        // Origin is scheme://host[:port] only. Reject callback-shaped values with a path.
        if (!string.IsNullOrEmpty(uri.AbsolutePath) && uri.AbsolutePath != "/")
        {
            return false;
        }

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
        {
            return false;
        }

        origin = uri.GetLeftPart(UriPartial.Authority).TrimEnd('/');
        return origin.Length > 0;
    }

    private static bool ContainsOrigin(IReadOnlyCollection<string> allowed, string origin)
    {
        foreach (var item in allowed)
        {
            if (string.IsNullOrWhiteSpace(item))
            {
                continue;
            }

            if (string.Equals(item.Trim().TrimEnd('/'), origin, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static string Combine(string baseUrl, string path, string encodedToken)
    {
        var trimmed = baseUrl.TrimEnd('/');
        return string.Create(
            CultureInfo.InvariantCulture,
            $"{trimmed}{path}?token={encodedToken}");
    }
}
