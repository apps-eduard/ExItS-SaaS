namespace ExItS.Platform.Api.Common;

/// <summary>Browser CSRF token contract for cookie-authenticated Platform API mutations.</summary>
public static class PlatformAntiforgeryDefaults
{
    public const string HeaderName = "X-XSRF-TOKEN";
    public const string CookieName = ".ExItS.Platform.Antiforgery";
    public const string TokenRoute = "/api/v1/platform/antiforgery/token";
    public const string InvalidErrorCode = "platform.antiforgery.invalid";
}
