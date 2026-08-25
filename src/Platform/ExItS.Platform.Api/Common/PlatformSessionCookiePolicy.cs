namespace ExItS.Platform.Api.Common;

/// <summary>
/// Browser session cookie Secure flag — mirrors antiforgery Local Validation HTTP support.
/// Production and non-LocalValidation Staging remain Secure on all requests.
/// </summary>
internal static class PlatformSessionCookiePolicy
{
    internal static bool IsSecureSessionCookie(
        IHostEnvironment environment,
        IConfiguration configuration,
        HttpRequest request)
    {
        if (environment.IsDevelopment() || environment.IsEnvironment("Testing"))
        {
            return request.IsHttps;
        }

        if (configuration.GetValue<bool>("LocalValidation:Enabled"))
        {
            return request.IsHttps;
        }

        return true;
    }
}
