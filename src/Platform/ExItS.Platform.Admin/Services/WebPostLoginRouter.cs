using ExItS.Web.UI;
using Microsoft.Extensions.Options;

namespace ExItS.Platform.Admin.Services;

/// <summary>
/// After central sign-in, route to the authorized workspace host via one-time handoff.
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
        CancellationToken ct = default)
    {
        var token = PlatformBrowserSessionService.ResolveSessionToken(http);
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
