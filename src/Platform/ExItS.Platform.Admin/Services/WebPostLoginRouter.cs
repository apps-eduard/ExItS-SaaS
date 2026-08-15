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

        // Baseline Platform Administrators also have a Personal profile. Do not force the
        // "Choose a workspace" page after every login — prefer Platform, then Personal.
        // If returnApp=organization was requested but this identity has no org membership
        // (common after Local Validation reset), fall back to Platform instead of Org Web
        // with an empty organization context.
        if (target is null)
        {
            target = workspaces.FirstOrDefault(w =>
                string.Equals(w.App, WebApps.Platform, StringComparison.OrdinalIgnoreCase));
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
            return SafeReturnPath.Sanitize(returnPath, "/admin");
        }

        var created = await WebHandoffHttp.CreateAsync(
            client,
            token,
            target.App,
            target.OrganizationId,
            SafeReturnPath.Sanitize(returnPath, WebHandoffAppsDefault(target.App)),
            ct).ConfigureAwait(false);
        if (created is null)
        {
            return "/admin/workspaces";
        }

        return WebHandoffHttp.EstablishUrl(hosts.Value.GetOrigin(target.App), created.Ticket, created.ReturnPath);
    }

    private static string WebHandoffAppsDefault(string app) => app switch
    {
        WebApps.Organization => "/overview",
        WebApps.Personal => "/",
        _ => "/admin"
    };
}
