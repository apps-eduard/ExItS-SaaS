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

        return status switch
        {
            ApiCallStatus.Success => (pos["Api_Available"], pos["Home_ApiHealthy"], diagnostic),
            ApiCallStatus.Offline => (errors["Offline_Title"], pos["Api_Offline"], diagnostic),
            ApiCallStatus.Timeout => (errors["Timeout_Title"], errors["Timeout_Message"], diagnostic),
            ApiCallStatus.Unavailable => (errors["Unavailable_Title"], errors["Unavailable_Message"], diagnostic),
            ApiCallStatus.Unauthorized => (errors["Unauthorized_Title"], errors["Unauthorized_Message"], diagnostic),
            ApiCallStatus.Forbidden => (errors["Forbidden_Title"], errors["Forbidden_Message"], diagnostic),
            ApiCallStatus.Validation => (errors["Validation_Title"], errors["Validation_Message"], diagnostic),
            ApiCallStatus.NotFound => (pos["NotFound_Title"], pos["NotFound_Message"], diagnostic),
            ApiCallStatus.Cancelled => (errors["Unexpected_Title"], design["Error_DefaultMessage"], diagnostic),
            ApiCallStatus.Conflict => (errors["Unexpected_Title"], design["Error_DefaultMessage"], diagnostic),
            _ => (errors["Unexpected_Title"], errors["Unexpected_Message"], diagnostic),
        };
    }

    public string PreferenceSaveFailed => pos["Preference_SaveFailed"];
}
