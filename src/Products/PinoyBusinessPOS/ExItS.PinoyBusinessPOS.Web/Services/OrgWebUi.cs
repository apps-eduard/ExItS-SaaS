using ExItS.PinoyBusinessPOS.Application.Common;

namespace ExItS.PinoyBusinessPOS.Web.Services;

public static class OrgWebUi
{
    public static string Money(decimal value) => value.ToString("N2");

    public static string Error(ApiError? error, string fallback = "Request failed.")
    {
        var detail = error?.Detail ?? error?.Title;
        if (string.IsNullOrWhiteSpace(detail))
        {
            return fallback;
        }

        // Never surface development-pipeline jargon, actor ids, or permission codes.
        if (detail.Contains("Development-stage", StringComparison.OrdinalIgnoreCase)
            || detail.Contains("commercial headers", StringComparison.OrdinalIgnoreCase)
            || detail.Contains("X-Dev-Platform-User-Id", StringComparison.OrdinalIgnoreCase)
            || detail.Contains("X-Pos-Organization-Id", StringComparison.OrdinalIgnoreCase)
            || detail.Contains("development-operator", StringComparison.OrdinalIgnoreCase)
            || detail.Contains("does not hold permission", StringComparison.OrdinalIgnoreCase)
            || detail.Contains("platform.permission.", StringComparison.OrdinalIgnoreCase)
            || detail.Contains("Actor '", StringComparison.OrdinalIgnoreCase))
        {
            if (detail.Contains("does not hold permission", StringComparison.OrdinalIgnoreCase)
                || detail.Contains("platform.permission.", StringComparison.OrdinalIgnoreCase))
            {
                return "You don't have permission to view this section.";
            }

            return "We couldn't verify your access to this business. Sign out and sign in again. If the problem continues, contact support.";
        }

        if (detail.Contains("session", StringComparison.OrdinalIgnoreCase)
            && (detail.Contains("expired", StringComparison.OrdinalIgnoreCase)
                || detail.Contains("invalid", StringComparison.OrdinalIgnoreCase)))
        {
            return "Your session has expired. Please sign in again.";
        }

        return detail;
    }

    public static string Badge(string? status) =>
        string.IsNullOrWhiteSpace(status) ? "—" : status;
}
