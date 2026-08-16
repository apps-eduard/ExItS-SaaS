using ExItS.PinoyBusinessPOS.Application.Common;

namespace ExItS.PinoyBusinessPOS.Web.Services;

public static class OrgWebUi
{
    public static string Money(decimal value) => value.ToString("N2");

    public static string Error(ApiError? error, string fallback = "Request failed.")
    {
        if (error is null)
        {
            return fallback;
        }

        // Prefer HTTP semantics over raw Platform/POS detail jargon.
        if (error.StatusCode is 401)
        {
            return "Your session has expired. Please sign in again.";
        }

        if (error.StatusCode is 403)
        {
            var detail403 = error.Detail ?? error.Title ?? string.Empty;
            if (detail403.Contains("subscription", StringComparison.OrdinalIgnoreCase)
                || detail403.Contains("entitlement", StringComparison.OrdinalIgnoreCase)
                || detail403.Contains("plan", StringComparison.OrdinalIgnoreCase)
                || string.Equals(error.ErrorCode, "commercial_denied", StringComparison.OrdinalIgnoreCase))
            {
                return "This feature requires an active plan. Organization management remains available.";
            }

            // Org Web Owners never use view_portfolio. When Platform falls through to that
            // permission after a missing session actor, surface a session recovery message.
            if (detail403.Contains("platform.permission.view_portfolio", StringComparison.OrdinalIgnoreCase)
                || detail403.Contains("Development-stage", StringComparison.OrdinalIgnoreCase)
                || detail403.Contains("commercial headers", StringComparison.OrdinalIgnoreCase)
                || detail403.Contains("X-Dev-Platform-User-Id", StringComparison.OrdinalIgnoreCase)
                || detail403.Contains("X-Pos-Organization-Id", StringComparison.OrdinalIgnoreCase)
                || detail403.Contains("development-operator", StringComparison.OrdinalIgnoreCase))
            {
                return "We couldn't verify your access to this business. Sign out and sign in again. If the problem continues, contact support.";
            }

            if (detail403.Contains("does not hold permission", StringComparison.OrdinalIgnoreCase)
                || detail403.Contains("platform.permission.", StringComparison.OrdinalIgnoreCase))
            {
                return "You don't have permission to view this section.";
            }

            return "You don't have permission to view this section.";
        }

        var detail = error.Detail ?? error.Title;
        if (string.IsNullOrWhiteSpace(detail))
        {
            return fallback;
        }

        // Never dump ProblemDetails / raw JSON bodies to end users.
        var trimmed = detail.TrimStart();
        if (trimmed.StartsWith('{')
            || trimmed.StartsWith('[')
            || detail.Contains("\"traceId\"", StringComparison.Ordinal)
            || detail.Contains("\"errorCode\"", StringComparison.Ordinal)
            || detail.Contains("\"status\":", StringComparison.Ordinal))
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
