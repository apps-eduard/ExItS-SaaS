using System.Net;
using ExItS.Platform.Admin.Services;

namespace ExItS.Platform.Admin.Forms;

public enum AdminDialogKind
{
    Confirm,
    Destructive,
    Conflict,
    UnsavedChanges
}

/// <summary>
/// Maps Platform API call results to page/field validation messages without inventing business rules.
/// </summary>
public static class AdminFormErrorMapper
{
    public static string? MapDetail(PlatformApiException? error) =>
        error?.Detail ?? error?.Title;

    public static bool IsConflict(PlatformApiException? error) =>
        error?.StatusCode == HttpStatusCode.Conflict;

    public static string? MapPageError<T>(ApiCallResult<T> result)
    {
        if (result.IsSuccess)
        {
            return null;
        }

        return MapDetail(result.Error);
    }

    public static bool TryBeginSubmit(ref bool busy)
    {
        if (busy)
        {
            return false;
        }

        busy = true;
        return true;
    }
}
