using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Routing;

namespace ExItS.Web.UI;

public static class ExitsCultureEndpoints
{
    public static IEndpointRouteBuilder MapExitsCultureSet(this IEndpointRouteBuilder app)
    {
        app.MapGet("/culture/set", (HttpContext http, string? culture, string? redirectUri) =>
        {
            var normalized = string.Equals(culture, "fil-PH", StringComparison.OrdinalIgnoreCase)
                ? "fil-PH"
                : "en";
            http.Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(normalized, normalized)),
                new CookieOptions
                {
                    HttpOnly = false,
                    IsEssential = true,
                    SameSite = SameSiteMode.Lax,
                    Path = "/",
                    MaxAge = TimeSpan.FromDays(365)
                });

            return Results.Redirect(SafeReturnPath.Sanitize(redirectUri, "/"));
        }).AllowAnonymous();

        return app;
    }
}
