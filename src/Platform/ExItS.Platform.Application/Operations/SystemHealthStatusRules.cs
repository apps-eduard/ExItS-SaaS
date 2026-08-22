namespace ExItS.Platform.Application.Operations;

/// <summary>
/// Truthful aggregation: unknown/unavailable metrics are never treated as Healthy.
/// </summary>
public static class SystemHealthStatusRules
{
    public static string Aggregate(IEnumerable<string> statuses)
    {
        ArgumentNullException.ThrowIfNull(statuses);

        var list = statuses.ToList();
        if (list.Count == 0)
        {
            return SystemHealthStatuses.Unknown;
        }

        if (list.Any(static s => s == SystemHealthStatuses.Unhealthy))
        {
            return SystemHealthStatuses.Unhealthy;
        }

        if (list.Any(static s =>
                s == SystemHealthStatuses.Degraded
                || s == SystemHealthStatuses.Unavailable
                || s == SystemHealthStatuses.Unknown
                || s == SystemHealthStatuses.NotAvailable))
        {
            return SystemHealthStatuses.Degraded;
        }

        if (list.All(static s => s == SystemHealthStatuses.Healthy))
        {
            return SystemHealthStatuses.Healthy;
        }

        return SystemHealthStatuses.Unknown;
    }

    public static string FromAspNetHealthStatus(string? status) =>
        status switch
        {
            "Healthy" => SystemHealthStatuses.Healthy,
            "Degraded" => SystemHealthStatuses.Degraded,
            "Unhealthy" => SystemHealthStatuses.Unhealthy,
            _ => SystemHealthStatuses.Unknown
        };

    public static string FromHealthEndpointBody(string? body, int httpStatus)
    {
        var parsed = FromAspNetHealthStatus(body?.Trim());
        if (parsed != SystemHealthStatuses.Unknown)
        {
            return parsed;
        }

        if (httpStatus == 200)
        {
            return SystemHealthStatuses.Unknown;
        }

        if (httpStatus is 503 or 500)
        {
            return SystemHealthStatuses.Unhealthy;
        }

        return SystemHealthStatuses.Unavailable;
    }
}
