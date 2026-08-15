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

        // Never surface development-pipeline jargon to Organization operators.
        if (detail.Contains("Development-stage", StringComparison.OrdinalIgnoreCase)
            || detail.Contains("commercial headers", StringComparison.OrdinalIgnoreCase)
            || detail.Contains("X-Dev-Platform-User-Id", StringComparison.OrdinalIgnoreCase)
            || detail.Contains("X-Pos-Organization-Id", StringComparison.OrdinalIgnoreCase))
        {
            return "This business workspace could not authorize the request. Sign in again, or open Platform Admin if your account is Platform-only.";
        }

        return detail;
    }

    public static string Badge(string? status) =>
        string.IsNullOrWhiteSpace(status) ? "—" : status;
}
