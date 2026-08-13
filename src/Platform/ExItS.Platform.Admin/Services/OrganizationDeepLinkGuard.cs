namespace ExItS.Platform.Admin.Services;

/// <summary>
/// UI convenience guard for organization deep links. Never replaces server-side authorization.
/// </summary>
public sealed class OrganizationDeepLinkGuard(AdminShellContext shell)
{
    /// <summary>
    /// Returns true when Platform operators may open any org URL, or Organization-shell actors
    /// may open only the currently selected organization.
    /// </summary>
    public bool CanOpenOrganization(Guid organizationId)
    {
        if (!shell.Loaded)
        {
            return false;
        }

        if (shell.IsPlatformShell)
        {
            return true;
        }

        return shell.IsOrganizationShell
               && shell.SelectedOrganizationId == organizationId;
    }

    public string DeniedRedirectUrl(Guid requestedOrganizationId) =>
        shell.IsOrganizationShell
            ? OrganizationShellHandoff.Url(shell, "/overview")
            : shell.SelectedOrganizationId is Guid selected
                ? $"/admin/organizations/{selected:D}"
                : "/admin";
}
