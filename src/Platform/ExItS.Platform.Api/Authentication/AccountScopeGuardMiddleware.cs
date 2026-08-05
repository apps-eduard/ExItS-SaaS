using System.Security.Claims;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Identity;
using ExItS.Platform.Domain.Identity;

namespace ExItS.Platform.Api.Authentication;

/// <summary>
/// Enforces ADR-017 scope-bound sessions for authenticated Platform browser sessions.
/// DevelopmentOperator / unauthenticated actors are not scope-classified (legacy test path).
/// </summary>
public sealed class AccountScopeGuardMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        if (!path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase))
        {
            await next(context).ConfigureAwait(false);
            return;
        }

        if (IsExempt(path))
        {
            await next(context).ConfigureAwait(false);
            return;
        }

        var user = context.User;
        if (user.Identity?.IsAuthenticated != true
            || !string.Equals(
                user.Identity.AuthenticationType,
                PlatformSessionClaimTypes.AuthenticationScheme,
                StringComparison.Ordinal))
        {
            // Unauthenticated / DevelopmentOperator paths keep existing permission checks.
            await next(context).ConfigureAwait(false);
            return;
        }

        var accountClassRaw = user.FindFirstValue(PlatformSessionClaimTypes.AccountClass);
        if (string.IsNullOrWhiteSpace(accountClassRaw)
            || !Enum.TryParse<AccountClass>(accountClassRaw, ignoreCase: true, out var accountClass))
        {
            await WriteDeniedAsync(context, "Session is missing a bound account class.").ConfigureAwait(false);
            return;
        }

        if (!IsPathAllowed(path, accountClass))
        {
            await WriteDeniedAsync(
                context,
                $"Account class '{accountClass}' is not allowed to call '{path}'.").ConfigureAwait(false);
            return;
        }

        await next(context).ConfigureAwait(false);
    }

    private static bool IsExempt(string path)
    {
        if (path.StartsWith("/api/v1/platform/auth/", StringComparison.OrdinalIgnoreCase)
            || path.Equals("/api/v1/platform/auth/login", StringComparison.OrdinalIgnoreCase)
            || path.Equals("/api/v1/platform/auth/logout", StringComparison.OrdinalIgnoreCase)
            || path.Equals("/api/v1/platform/auth/me", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Shell permission discovery for any authenticated class. Returns empty Platform
        // permissions for Organization/Personal (see ResolveCurrentPermissions). Other
        // /authorization/* routes remain Platform-only.
        if (path.Equals("/api/v1/platform/authorization/me", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (path.StartsWith("/api/v1/platform/local-validation", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Authenticated commercial catalog (any account class); endpoint enforces session auth.
        if (path.StartsWith("/api/v1/commercial", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Authenticated merchant discovery (global catalog templates/products/categories) —
        // cross-scope, any authenticated account class. Endpoint enforces session auth
        // (RequireAuthorization + EnsureAuthenticated) and returns published/Active data only;
        // entitlement-aware filtering is deferred to POS import (WP06). Mirrors /api/v1/commercial.
        if (path.StartsWith("/api/v1/catalog", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Public ExItS ID — any authenticated account class (Personal/Organization/Platform).
        if (path.Equals("/api/v1/me/public-identity", StringComparison.OrdinalIgnoreCase)
            || path.Equals("/api/v1/users/resolve-public-id", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (path.StartsWith("/health", StringComparison.OrdinalIgnoreCase)
            || path.Equals("/", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // External auth callback/login routes
        if (path.StartsWith("/api/v1/platform/auth/external", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/signin-", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Invitation accept is an identity-bound action available before Organization profile selection (WP06 remaps).
        if (path.Equals("/api/v1/platform/invitations/accept", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Customer link accept/decline is identity-bound and must not create staff membership (WP07).
        if (path.Equals("/api/v1/organizations/customer-link-requests/accept", StringComparison.OrdinalIgnoreCase)
            || path.Equals("/api/v1/organizations/customer-link-requests/decline", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    private static bool IsPathAllowed(string path, AccountClass accountClass)
    {
        if (path.Equals("/api/v1/me/public-identity", StringComparison.OrdinalIgnoreCase)
            || path.Equals("/api/v1/users/resolve-public-id", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (path.StartsWith("/api/v1/personal", StringComparison.OrdinalIgnoreCase))
        {
            return accountClass is AccountClass.Personal;
        }

        // Target Organization product family (WP03+). Deny for Personal/Platform.
        if (path.StartsWith("/api/v1/organizations", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/api/v1/products", StringComparison.OrdinalIgnoreCase))
        {
            return accountClass is AccountClass.Organization;
        }

        if (path.StartsWith("/api/v1/platform", StringComparison.OrdinalIgnoreCase))
        {
            // Until WP03 remaps Organization APIs off /platform, Organization sessions may call
            // organization-scoped routes that still live under /api/v1/platform/organizations/*.
            // Platform administration, catalog, global subscriptions, RBAC, and users stay Platform-only.
            if (accountClass is AccountClass.Organization)
            {
                return path.StartsWith("/api/v1/platform/organizations", StringComparison.OrdinalIgnoreCase);
            }

            return accountClass is AccountClass.Platform;
        }

        return false;
    }

    private static async Task WriteDeniedAsync(HttpContext context, string detail)
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(new
        {
            type = "https://tools.ietf.org/html/rfc9110#section-15.5.4",
            title = "Forbidden",
            status = 403,
            detail,
            errorCode = ApplicationErrorCodes.AccountScopeDenied,
            traceId = context.TraceIdentifier
        }).ConfigureAwait(false);
    }
}
