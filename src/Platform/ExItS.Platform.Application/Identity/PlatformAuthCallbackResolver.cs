using System.Globalization;

namespace ExItS.Platform.Application.Identity;

/// <summary>
/// Resolves EmailVerification and PasswordReset callback URLs from a server-selected public surface.
/// Invitation and recovery-email links stay on Admin.
/// </summary>
public static class PlatformAuthCallbackResolver
{
    public const string AdminActivatePath = "/admin/activate-account";
    public const string AdminResetPath = "/admin/reset-password";
    public const string PinoyLoanManagerActivatePath = "/activate-account";
    public const string PinoyLoanManagerResetPath = "/reset-password";

    public static bool TryCreateLink(
        PlatformAuthOutboundMessage message,
        string adminPublicBaseUrl,
        string? pinoyLoanManagerPublicBaseUrl,
        bool allowHttpLoopbackPublicUrls,
        out string absoluteUrl)
    {
        absoluteUrl = string.Empty;
        if (message.Kind == PlatformAuthOutboundMessageKinds.EmailVerification)
        {
            return TryCreateVerificationOrResetLink(
                message,
                adminPublicBaseUrl,
                pinoyLoanManagerPublicBaseUrl,
                allowHttpLoopbackPublicUrls,
                AdminActivatePath,
                PinoyLoanManagerActivatePath,
                out absoluteUrl);
        }

        if (message.Kind == PlatformAuthOutboundMessageKinds.PasswordReset)
        {
            return TryCreateVerificationOrResetLink(
                message,
                adminPublicBaseUrl,
                pinoyLoanManagerPublicBaseUrl,
                allowHttpLoopbackPublicUrls,
                AdminResetPath,
                PinoyLoanManagerResetPath,
                out absoluteUrl);
        }

        return false;
    }

    public static bool IsAllowedPublicBaseUrl(string? candidate, bool allowHttpLoopbackPublicUrls)
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

        if (allowHttpLoopbackPublicUrls
            && uri.Scheme == Uri.UriSchemeHttp
            && (string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase)
                || string.Equals(uri.Host, "127.0.0.1", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        return false;
    }

    private static bool TryCreateVerificationOrResetLink(
        PlatformAuthOutboundMessage message,
        string adminPublicBaseUrl,
        string? pinoyLoanManagerPublicBaseUrl,
        bool allowHttpLoopbackPublicUrls,
        string adminPath,
        string plmPath,
        out string absoluteUrl)
    {
        var encodedToken = Uri.EscapeDataString(message.OpaqueToken ?? string.Empty);
        if (string.Equals(
                message.PublicSurface,
                PlatformAuthPublicSurfaces.PinoyLoanManager,
                StringComparison.Ordinal))
        {
            if (!IsAllowedPublicBaseUrl(pinoyLoanManagerPublicBaseUrl, allowHttpLoopbackPublicUrls))
            {
                absoluteUrl = string.Empty;
                return false;
            }

            absoluteUrl = Combine(pinoyLoanManagerPublicBaseUrl!, plmPath, encodedToken);
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

    private static string Combine(string baseUrl, string path, string encodedToken)
    {
        var trimmed = baseUrl.TrimEnd('/');
        return string.Create(
            CultureInfo.InvariantCulture,
            $"{trimmed}{path}?token={encodedToken}");
    }
}
