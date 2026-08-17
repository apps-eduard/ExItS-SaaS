using ExItS.Web.UI;
using Microsoft.Extensions.Options;

namespace ExItS.Platform.Admin.Services;

/// <summary>
/// After central sign-in, route to the authorized workspace host via one-time handoff.
/// Prefers a concrete destination over the workspace chooser whenever one is safe.
/// </summary>
public sealed class WebPostLoginRouter(
    IHttpClientFactory httpClientFactory,
    IOptions<ExItSWebHostOptions> hosts)
{
    public async Task<string> ResolveAsync(
        HttpContext http,
        string? returnApp,
        string? returnPath,
        Guid? organizationId = null,
        string? sessionToken = null,
        CancellationToken ct = default)
    {
        // Prefer an explicit token from the just-completed SignIn — Request.Cookies / User
        // may still reflect the previous request when called in the same pipeline turn.
        var token = string.IsNullOrWhiteSpace(sessionToken)
            ? PlatformBrowserSessionService.ResolveSessionToken(http)
            : sessionToken.Trim();
        if (string.IsNullOrWhiteSpace(token))
        {
            return "/admin/login";
        }

        var client = httpClientFactory.CreateClient("PlatformApiUnauthenticated");
        using var workspaceRequest = new HttpRequestMessage(HttpMethod.Get, "/api/v1/platform/auth/workspaces");
        workspaceRequest.Headers.TryAddWithoutValidation("X-ExItS-Session-Token", token);
        using var workspaceResponse = await client.SendAsync(workspaceRequest, ct).ConfigureAwait(false);
        if (!workspaceResponse.IsSuccessStatusCode)
        {
            return "/admin";
        }

        var list = await workspaceResponse.Content
            .ReadFromJsonAsync<WebWorkspaceListResponse>(ct)
            .ConfigureAwait(false);
        var workspaces = list?.Workspaces ?? [];
        if (workspaces.Count == 0)
        {
            return "/admin/workspaces";
        }

        var requested = string.IsNullOrWhiteSpace(returnApp) ? null : WebApps.Normalize(returnApp);
        WebWorkspaceItemResponse? target = null;
        if (requested is not null)
        {
            target = workspaces.FirstOrDefault(w =>
                string.Equals(w.App, requested, StringComparison.OrdinalIgnoreCase)
                && (organizationId is null || w.OrganizationId == organizationId));
        }

        if (target is null && workspaces.Count == 1)
        {
            target = workspaces[0];
        }

        // Priority when no explicit returnApp:
        // 1) Platform operator → Platform Admin
        // 2) Qualifying Organization Web membership(s) → Org Web (or chooser if many)
        // 3) Personal-only → Personal Web
        // Cashier / OrganizationMember-only orgs are not listed as Organization workspaces.
        if (target is null)
        {
            target = workspaces.FirstOrDefault(w =>
                string.Equals(w.App, WebApps.Platform, StringComparison.OrdinalIgnoreCase));
        }

        if (target is null && requested is null)
        {
            var orgWorkspaces = workspaces
                .Where(w => string.Equals(w.App, WebApps.Organization, StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (orgWorkspaces.Count == 1)
            {
                target = orgWorkspaces[0];
            }
            else if (orgWorkspaces.Count > 1)
            {
                return "/admin/workspaces";
            }
        }

        if (target is null && requested is null)
        {
            target = workspaces.FirstOrDefault(w =>
                string.Equals(w.App, WebApps.Personal, StringComparison.OrdinalIgnoreCase));
        }

        if (target is null)
        {
            return "/admin/workspaces";
        }

        if (string.Equals(target.App, WebApps.Platform, StringComparison.OrdinalIgnoreCase))
        {
            return ResolveReturnPath(target.App, requested, returnPath);
        }

        var created = await WebHandoffHttp.CreateAsync(
            client,
            token,
            target.App,
            target.OrganizationId,
            ResolveReturnPath(target.App, requested, returnPath),
            ct).ConfigureAwait(false);
        if (created is null)
        {
            return "/admin/workspaces";
        }

        return WebHandoffHttp.EstablishUrl(hosts.Value.GetOrigin(target.App), created.Ticket, created.ReturnPath);
    }

    public static string ResolveReturnPath(string targetApp, string? requestedApp, string? returnPath)
    {
        var requested = string.IsNullOrWhiteSpace(requestedApp) ? null : WebApps.Normalize(requestedApp);
        var target = WebApps.Normalize(targetApp);
        var fallback = WebHandoffAppsDefault(target);

        if (string.Equals(target, WebApps.Platform, StringComparison.OrdinalIgnoreCase))
        {
            if (requested is null || string.Equals(requested, WebApps.Platform, StringComparison.OrdinalIgnoreCase))
            {
                var sanitized = SafeReturnPath.Sanitize(returnPath, "/admin");
                if (sanitized.StartsWith("/admin", StringComparison.OrdinalIgnoreCase))
                {
                    return sanitized;
                }
            }

            return "/admin";
        }

        if (requested is null || string.Equals(requested, target, StringComparison.OrdinalIgnoreCase))
        {
            return SafeReturnPath.Sanitize(returnPath, fallback);
        }

        return fallback;
    }

    private static string WebHandoffAppsDefault(string app) => app switch
    {
        WebApps.Organization => "/overview",
        WebApps.Personal => "/",
        _ => "/admin"
    };
}
