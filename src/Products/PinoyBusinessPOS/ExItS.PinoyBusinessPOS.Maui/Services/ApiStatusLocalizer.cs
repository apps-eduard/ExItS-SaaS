using ExItS.DesignSystem.Localization;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Maui.Localization;
using Microsoft.Extensions.Localization;

namespace ExItS.PinoyBusinessPOS.Maui.Services;

/// <summary>
/// Maps API call outcomes to localized user-facing title/message pairs without exposing
/// stack traces, raw ProblemDetails, or infrastructure details. Optional diagnostic codes
/// are returned separately for developer surfaces.
/// </summary>
public sealed class ApiStatusLocalizer(
    IStringLocalizer<PosResources> pos,
    IStringLocalizer<ErrorResources> errors,
    IStringLocalizer<DesignSystemResources> design)
{
    public (string Title, string Message, string? DiagnosticCode) Describe(ApiCallStatus status, ApiError? error = null)
    {
        var diagnostic = string.IsNullOrWhiteSpace(error?.ErrorCode) ? null : error.ErrorCode;

        if (!string.IsNullOrWhiteSpace(error?.ErrorCode)
            && error.ErrorCode.StartsWith("pos.catalog_import.", StringComparison.OrdinalIgnoreCase)
            && status is not ApiCallStatus.Success)
        {
            return (
                PreferText(error.Title, errors["Unexpected_Title"]),
                PreferText(error.Detail, design["Error_DefaultMessage"]),
                diagnostic);
        }

        return status switch
        {
            ApiCallStatus.Success => (pos["Api_Available"], pos["Home_ApiHealthy"], diagnostic),
            ApiCallStatus.Offline => (errors["Offline_Title"], pos["Api_Offline"], diagnostic),
            ApiCallStatus.Timeout => (errors["Timeout_Title"], errors["Timeout_Message"], diagnostic),
            ApiCallStatus.Unavailable => (errors["Unavailable_Title"], errors["Unavailable_Message"], diagnostic),
            ApiCallStatus.Unauthorized => (errors["Unauthorized_Title"], errors["Unauthorized_Message"], diagnostic),
            ApiCallStatus.Forbidden => (errors["Forbidden_Title"], errors["Forbidden_Message"], diagnostic),
            ApiCallStatus.Validation => (
                PreferText(error?.Title, errors["Validation_Title"]),
                PreferText(error?.Detail, errors["Validation_Message"]),
                diagnostic),
            ApiCallStatus.NotFound => (pos["NotFound_Title"], pos["NotFound_Message"], diagnostic),
            ApiCallStatus.Cancelled => (errors["Unexpected_Title"], design["Error_DefaultMessage"], diagnostic),
            ApiCallStatus.Conflict => DescribeConflict(error, diagnostic),
            _ => (errors["Unexpected_Title"], errors["Unexpected_Message"], diagnostic),
        };
    }

    private (string Title, string Message, string? DiagnosticCode) DescribeConflict(
        ApiError? error,
        string? diagnostic)
    {
        if (string.Equals(error?.ErrorCode, ApplicationErrorCodes.InsufficientStock, StringComparison.Ordinal)
            || string.Equals(error?.ErrorCode, "pos.inventory.insufficient_stock", StringComparison.Ordinal))
        {
            return (pos["Inventory_InsufficientStockTitle"], pos["Inventory_InsufficientStockMessage"], diagnostic);
        }

        return (errors["Unexpected_Title"], design["Error_DefaultMessage"], diagnostic);
    }

    private static string PreferText(string? preferred, LocalizedString fallback) =>
        string.IsNullOrWhiteSpace(preferred) ? fallback : preferred.Trim();

    public string PreferenceSaveFailed => pos["Preference_SaveFailed"];
}
