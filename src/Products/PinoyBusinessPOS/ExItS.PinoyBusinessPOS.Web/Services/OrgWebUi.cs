using ExItS.PinoyBusinessPOS.Application.Common;

namespace ExItS.PinoyBusinessPOS.Web.Services;

internal static class OrgWebUi
{
    public static string Money(decimal value) => value.ToString("N2");

    public static string Error(ApiError? error, string fallback = "Request failed.") =>
        error?.Detail ?? error?.Title ?? fallback;

    public static string Badge(string? status) =>
        string.IsNullOrWhiteSpace(status) ? "—" : status;
}
