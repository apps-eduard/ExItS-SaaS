namespace ExItS.PinoyBusinessPOS.Maui.Services;

/// <summary>
/// Captures a safe internal MAUI route before SessionExpired sign-in so the user can return
/// after successful authentication. External URLs and auth pages are rejected.
/// </summary>
public sealed class PostSignInReturnRoute
{
    private string? _route;
    private readonly object _gate = new();

    public void Capture(string? relativePath)
    {
        if (!IsSafeInternalRoute(relativePath))
        {
            return;
        }

        lock (_gate)
        {
            _route = Normalize(relativePath!);
        }
    }

    public string? Take()
    {
        lock (_gate)
        {
            var route = _route;
            _route = null;
            return route;
        }
    }

    public static bool IsSafeInternalRoute(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return false;
        }

        var path = relativePath.Trim();
        if (!path.StartsWith("/", StringComparison.Ordinal)
            || path.StartsWith("//", StringComparison.Ordinal)
            || path.Contains("://", StringComparison.Ordinal)
            || path.Contains('\\'))
        {
            return false;
        }

        var lower = path.ToLowerInvariant();
        if (lower.StartsWith("/signin", StringComparison.Ordinal)
            || lower.StartsWith("/register", StringComparison.Ordinal)
            || lower.StartsWith("/forgot-password", StringComparison.Ordinal)
            || lower.StartsWith("/welcome", StringComparison.Ordinal)
            || lower.StartsWith("/onboarding", StringComparison.Ordinal))
        {
            return false;
        }

        return lower.StartsWith("/personal", StringComparison.Ordinal)
               || lower.StartsWith("/org", StringComparison.Ordinal)
               || lower.StartsWith("/organization", StringComparison.Ordinal)
               || lower.StartsWith("/subscription", StringComparison.Ordinal)
               || lower.StartsWith("/settings", StringComparison.Ordinal)
               || lower.StartsWith("/more", StringComparison.Ordinal)
               || lower.StartsWith("/home", StringComparison.Ordinal)
               || lower == "/";
    }

    private static string Normalize(string relativePath)
    {
        var path = relativePath.Trim();
        var hash = path.IndexOf('#', StringComparison.Ordinal);
        if (hash >= 0)
        {
            path = path[..hash];
        }

        return path;
    }
}
