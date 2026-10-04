namespace ExItS.Platform.Application.Identity;

/// <summary>
/// Sanitizes OAuth completion return URLs to block open redirects while allowing
/// Admin relative callbacks, Dev/Testing localhost absolutes, and the MAUI app callback scheme.
/// </summary>
public static class ExternalAuthReturnUrl
{
    public const string DefaultAdminCallback = "/admin/external-login-callback";
    public const string MauiCallbackScheme = "exitspos";
    public const string MauiCallbackHost = "auth";
    public const string MauiCallbackPath = "/callback";
    public const string MauiCallbackUrl = "exitspos://auth/callback";

    public static string Sanitize(string? returnUrl, bool allowDevLocalhostAbsolute)
    {
        if (string.IsNullOrWhiteSpace(returnUrl))
        {
            return DefaultAdminCallback;
        }

        var candidate = returnUrl.Trim();
        if (IsUnsafeReturn(candidate))
        {
            return DefaultAdminCallback;
        }

        if (Uri.TryCreate(candidate, UriKind.Relative, out _)
            && !candidate.Contains("://", StringComparison.Ordinal))
        {
            return candidate.StartsWith('/') ? candidate : DefaultAdminCallback;
        }

        returnUrl = candidate;

        if (!Uri.TryCreate(returnUrl, UriKind.Absolute, out var absolute))
        {
            return DefaultAdminCallback;
        }

        if (string.Equals(absolute.Scheme, MauiCallbackScheme, StringComparison.OrdinalIgnoreCase)
            && string.Equals(absolute.Host, MauiCallbackHost, StringComparison.OrdinalIgnoreCase)
            && string.Equals(absolute.AbsolutePath, MauiCallbackPath, StringComparison.OrdinalIgnoreCase))
        {
            // Drop query/fragment from the configured callback; Platform appends sessionToken.
            return $"{MauiCallbackScheme}://{MauiCallbackHost}{MauiCallbackPath}";
        }

        if (allowDevLocalhostAbsolute && absolute.Host is "localhost" or "127.0.0.1")
        {
            return absolute.ToString();
        }

        return DefaultAdminCallback;
    }

    private static bool IsUnsafeReturn(string value) =>
        value.Contains('\\')
        || value.Contains('\0')
        || value.StartsWith("//", StringComparison.Ordinal)
        || value.Contains("javascript:", StringComparison.OrdinalIgnoreCase)
        || value.Contains("data:", StringComparison.OrdinalIgnoreCase);
}
