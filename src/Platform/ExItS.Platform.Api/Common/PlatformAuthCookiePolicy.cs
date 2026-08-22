using Microsoft.AspNetCore.Http;

namespace ExItS.Platform.Api.Common;

/// <summary>
/// Local Validation Docker/host apps run Staging over HTTP on localhost and Tailscale/LAN IPs.
/// Browsers keep Secure cookies on http://localhost but drop them on http://100.x, which
/// breaks React Admin cookie sessions. Production keeps Always-secure cookies.
/// </summary>
internal static class PlatformAuthCookiePolicy
{
    public static bool AllowHttpAuthCookies(IHostEnvironment environment, IConfiguration configuration) =>
        environment.IsDevelopment()
        || environment.IsEnvironment("Testing")
        || (configuration.GetValue("LocalValidation:Enabled", false) && !environment.IsProduction());

    public static CookieSecurePolicy SecurePolicy(IHostEnvironment environment, IConfiguration configuration) =>
        AllowHttpAuthCookies(environment, configuration)
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;

    public static bool SessionCookieSecure(
        HttpRequest request,
        IHostEnvironment environment,
        IConfiguration configuration) =>
        AllowHttpAuthCookies(environment, configuration) ? request.IsHttps : true;
}
