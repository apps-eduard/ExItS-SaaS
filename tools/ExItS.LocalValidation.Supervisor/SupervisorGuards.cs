namespace ExItS.LocalValidation.Supervisor;

/// <summary>Pure allowlist/production guards shared by host and tests.</summary>
public static class SupervisorGuards
{
    private static readonly HashSet<string> RestartableKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "platform-admin",
        "platform-api",
        "pos-api",
        "org-web",
        "personal-web",
        "react-admin",
        "react-pos",
        "mailpit",
    };

    private static readonly HashSet<string> HealthOnlyKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "platform-db",
        "pos-db",
    };

    public static bool IsProductionEnvironmentName(string? name) =>
        string.Equals(name, "Production", StringComparison.OrdinalIgnoreCase);

    public static bool IsAllowedServiceKey(string? serviceKey)
    {
        if (string.IsNullOrWhiteSpace(serviceKey))
        {
            return false;
        }

        return RestartableKeys.Contains(serviceKey) || HealthOnlyKeys.Contains(serviceKey);
    }

    public static bool IsRestartableServiceKey(string? serviceKey) =>
        !string.IsNullOrWhiteSpace(serviceKey) && RestartableKeys.Contains(serviceKey);

    public static bool IsAllowedRoute(string method, string path)
    {
        method = method.Trim().ToUpperInvariant();
        path = path.Trim().ToLowerInvariant();
        if (method == "GET" && (path is "/health" or "/health/services" or "/operation"))
        {
            return true;
        }

        if (method == "POST" && path is "/services/restart-all" or "/reset")
        {
            return true;
        }

        if (method == "POST" && path.StartsWith("/services/", StringComparison.Ordinal) && path.EndsWith("/restart", StringComparison.Ordinal))
        {
            var key = path["/services/".Length..^"/restart".Length];
            return IsRestartableServiceKey(key);
        }

        return false;
    }
}
