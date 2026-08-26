using ExItS.Platform.Application.Identity;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.Extensions.Options;

namespace ExItS.Platform.Api.Common;

/// <summary>
/// Requires a valid antiforgery token for state-changing requests that carry the Platform session cookie
/// without an explicit session header (browser callers). Header/token-only callers remain unchanged.
/// </summary>
internal sealed class PlatformBrowserAntiforgeryMiddleware(
    RequestDelegate next,
    IAntiforgery antiforgery,
    IOptions<PlatformSessionOptions> sessionOptions)
{
    private static readonly HashSet<string> ExemptPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/api/v1/platform/auth/login",
        "/api/v1/platform/auth/register",
        // Anonymous credential workflows: Admin React skips antiforgery headers; a leftover
        // Platform session cookie must not block Mailpit activation / password reset.
        "/api/v1/platform/auth/activate-account",
        "/api/v1/platform/auth/forgot-password",
        "/api/v1/platform/auth/reset-password",
        // Token confirmations are anonymous deep-links; leftover session cookies must not require CSRF.
        "/api/v1/platform/auth/email-verification/confirm",
        "/api/v1/platform/auth/recovery-email/confirm",
        "/api/v1/platform/auth/bootstrap",
        // Token+password invite accept: leftover session cookies must not require browser CSRF.
        "/api/v1/platform/invitations/accept",
        "/api/v1/platform/invitations/accept-as-personal",
        PlatformAntiforgeryDefaults.TokenRoute,
        "/api/v1/platform/auth/external/google/callback",
        "/api/v1/platform/auth/external/facebook/callback",
        "/api/v1/platform/auth/external/testing/callback",
    };

    public async Task InvokeAsync(HttpContext context)
    {
        if (IsSafeMethod(context.Request.Method))
        {
            await next(context).ConfigureAwait(false);
            return;
        }

        var path = context.Request.Path.Value ?? string.Empty;
        if (ExemptPaths.Contains(path))
        {
            await next(context).ConfigureAwait(false);
            return;
        }

        var session = sessionOptions.Value;
        var hasSessionCookie = context.Request.Cookies.ContainsKey(session.CookieName);
        var hasSessionHeader = context.Request.Headers.TryGetValue(session.SessionTokenHeaderName, out var headerValues)
            && !string.IsNullOrWhiteSpace(headerValues.ToString());
        var authorization = context.Request.Headers.Authorization.ToString();
        var hasBearer = authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            && authorization.Length > "Bearer ".Length;

        // Cookie+browser callers need CSRF. Header session tokens and Bearer access tokens skip it.
        if (!hasSessionCookie || hasSessionHeader || hasBearer)
        {
            await next(context).ConfigureAwait(false);
            return;
        }

        try
        {
            await antiforgery.ValidateRequestAsync(context).ConfigureAwait(false);
        }
        catch (AntiforgeryValidationException)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsJsonAsync(new
            {
                title = "Antiforgery token validation failed.",
                status = StatusCodes.Status400BadRequest,
                detail = "A valid browser antiforgery token is required for this request.",
                errorCode = PlatformAntiforgeryDefaults.InvalidErrorCode,
                traceId = context.TraceIdentifier
            }).ConfigureAwait(false);
            return;
        }

        await next(context).ConfigureAwait(false);
    }

    private static bool IsSafeMethod(string method) =>
        HttpMethods.IsGet(method)
        || HttpMethods.IsHead(method)
        || HttpMethods.IsOptions(method);
}
