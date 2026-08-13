namespace ExItS.Web.UI;

/// <summary>
/// Public browser host URLs. Local Validation uses distinct ports; production uses HTTPS :443 hostnames.
/// </summary>
public sealed class ExItSWebHostOptions
{
    public const string SectionName = "ExItSWebHosts";

    /// <summary>Platform Admin origin, e.g. http://localhost:8090 or https://platform.example.com.</summary>
    public string PlatformAdmin { get; set; } = "http://localhost:8090";

    /// <summary>Organization Web origin, e.g. http://localhost:8093 or https://org.example.com.</summary>
    public string OrganizationWeb { get; set; } = "http://localhost:8093";

    /// <summary>Personal Web origin, e.g. http://localhost:8094 or https://personal.example.com.</summary>
    public string PersonalWeb { get; set; } = "http://localhost:8094";

    public string CanonicalLoginPath { get; set; } = "/admin/login";

    public string GetOrigin(string app) => app.Trim().ToLowerInvariant() switch
    {
        WebApps.Organization => TrimSlash(OrganizationWeb),
        WebApps.Personal => TrimSlash(PersonalWeb),
        _ => TrimSlash(PlatformAdmin)
    };

    public string CanonicalLoginUrl(string? returnApp = null, string? returnPath = null)
    {
        var url = TrimSlash(PlatformAdmin) + CanonicalLoginPath;
        var query = new List<string>();
        if (!string.IsNullOrWhiteSpace(returnApp))
        {
            query.Add("returnApp=" + Uri.EscapeDataString(returnApp.Trim()));
        }

        if (!string.IsNullOrWhiteSpace(returnPath))
        {
            query.Add("returnPath=" + Uri.EscapeDataString(returnPath.Trim()));
        }

        return query.Count == 0 ? url : url + "?" + string.Join("&", query);
    }

    private static string TrimSlash(string value) => (value ?? string.Empty).TrimEnd('/');
}

public static class WebApps
{
    public const string Platform = "platform";
    public const string Organization = "organization";
    public const string Personal = "personal";

    public static bool IsKnown(string? app) =>
        string.Equals(app, Platform, StringComparison.OrdinalIgnoreCase)
        || string.Equals(app, Organization, StringComparison.OrdinalIgnoreCase)
        || string.Equals(app, Personal, StringComparison.OrdinalIgnoreCase);

    public static string Normalize(string? app) => app?.Trim().ToLowerInvariant() switch
    {
        Organization => Organization,
        Personal => Personal,
        _ => Platform
    };
}

/// <summary>Rejects open redirects. Only same-app relative paths are allowed.</summary>
public static class SafeReturnPath
{
    public static string Sanitize(string? path, string fallback = "/")
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return fallback;
        }

        var trimmed = path.Trim();
        if (trimmed.Length > 1024
            || !trimmed.StartsWith('/')
            || trimmed.StartsWith("//", StringComparison.Ordinal)
            || trimmed.StartsWith("/\\", StringComparison.Ordinal)
            || trimmed.Contains("://", StringComparison.Ordinal)
            || trimmed.Contains('\\'))
        {
            return fallback;
        }

        return trimmed;
    }
}
