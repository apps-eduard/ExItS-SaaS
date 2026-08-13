using ExItS.Web.UI;
using Microsoft.AspNetCore.Components;

namespace ExItS.Platform.Admin.Services;

/// <summary>
/// Sends Organization-shell actors to Organization Web (8093) instead of rendering
/// product pages on Platform Admin. Platform operators keep the Admin org tools.
/// </summary>
public static class OrganizationShellHandoff
{
    public static bool TryRedirect(AdminShellContext shell, NavigationManager nav, string returnPath = "/overview")
    {
        if (!shell.Loaded || !shell.IsOrganizationShell)
        {
            return false;
        }

        nav.NavigateTo(Url(shell, returnPath), forceLoad: true);
        return true;
    }

    public static string Url(AdminShellContext shell, string returnPath = "/overview")
    {
        var path = SafeReturnPath.Sanitize(returnPath, "/overview");
        var org = shell.SelectedOrganizationId is Guid id ? $"&organizationId={id:D}" : "";
        return $"/admin/handoff/organization?returnPath={Uri.EscapeDataString(path)}{org}";
    }
}
