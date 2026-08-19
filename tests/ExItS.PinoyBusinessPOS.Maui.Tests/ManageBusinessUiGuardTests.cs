namespace ExItS.PinoyBusinessPOS.Maui.Tests;

public sealed class ManageBusinessUiGuardTests
{
    [Fact]
    public void Primary_owner_manage_business_entry_and_hub_are_gated()
    {
        var maui = MauiProject();
        var menu = File.ReadAllText(Path.Combine(maui, "Components", "Shared", "ShellAccountMenu.razor"));
        var manage = File.ReadAllText(Path.Combine(maui, "Components", "Pages", "Organization", "ManageBusiness.razor"));
        var gate = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "src",
            "Products",
            "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Application",
            "Support",
            "IWorkspaceGovernanceGate.cs"));

        Assert.Contains("IWorkspaceGovernanceGate Governance", menu, StringComparison.Ordinal);
        Assert.Contains("ManageBusiness_Menu", menu, StringComparison.Ordinal);
        Assert.Contains("CanAccessManageBusinessAsync", menu, StringComparison.Ordinal);
        Assert.Contains("_showManageBusiness", menu, StringComparison.Ordinal);
        Assert.Contains("GoManageBusinessAsync", menu, StringComparison.Ordinal);
        Assert.Contains("/manage-business", menu, StringComparison.Ordinal);

        Assert.Contains("@page \"/manage-business\"", manage, StringComparison.Ordinal);
        Assert.Contains("CanAccessManageBusinessAsync", manage, StringComparison.Ordinal);
        Assert.Contains("ManageBusiness_PrimaryRequired", manage, StringComparison.Ordinal);
        Assert.Contains("pos-manage-business__row", manage, StringComparison.Ordinal);
        Assert.Contains("/organization/branches", manage, StringComparison.Ordinal);
        Assert.Contains("/org/staff", manage, StringComparison.Ordinal);

        Assert.Contains("IsPrimaryWorkspaceAsync", gate, StringComparison.Ordinal);
        Assert.Contains("IsOwnerAsync", gate, StringComparison.Ordinal);
        Assert.Contains("b.IsPrimary", gate, StringComparison.Ordinal);
    }

    [Fact]
    public void Operational_nav_removes_global_branch_management_and_org_governance_clutter()
    {
        var maui = MauiProject();
        var more = File.ReadAllText(Path.Combine(maui, "Components", "Pages", "MoreHub.razor"));
        var org = File.ReadAllText(Path.Combine(maui, "Components", "Pages", "Organization", "OrgSummary.razor"));
        var owner = File.ReadAllText(Path.Combine(maui, "Components", "Pages", "Dashboards", "OwnerDashboard.razor"));
        var branches = File.ReadAllText(Path.Combine(maui, "Components", "Pages", "Organization", "Branches.razor"));

        Assert.DoesNotContain("More_BusinessSection", more, StringComparison.Ordinal);
        Assert.DoesNotContain("GoBranches", more, StringComparison.Ordinal);
        Assert.DoesNotContain("/organization/branches", more, StringComparison.Ordinal);
        Assert.Contains("GoBranchSettings", more, StringComparison.Ordinal);
        Assert.Contains("/branch-settings", more, StringComparison.Ordinal);

        Assert.DoesNotContain("/org/staff", org, StringComparison.Ordinal);
        Assert.DoesNotContain("/org/profile", org, StringComparison.Ordinal);
        Assert.DoesNotContain("Org_EssentialsSection", org, StringComparison.Ordinal);
        Assert.Contains("BranchSettings_Title", org, StringComparison.Ordinal);
        Assert.Contains("GoBranchSettings", org, StringComparison.Ordinal);

        Assert.DoesNotContain("GoStaff", owner, StringComparison.Ordinal);
        Assert.DoesNotContain("/org/staff", owner, StringComparison.Ordinal);

        Assert.Contains("Href=\"/manage-business\"", branches, StringComparison.Ordinal);
        Assert.DoesNotContain("Href=\"/more\"", branches, StringComparison.Ordinal);
    }

    [Fact]
    public void Branch_settings_stays_local_and_branch_edit_honors_return_path()
    {
        var maui = MauiProject();
        var settings = File.ReadAllText(Path.Combine(maui, "Components", "Pages", "Organization", "BranchSettings.razor"));
        var edit = File.ReadAllText(Path.Combine(maui, "Components", "Pages", "Organization", "BranchEdit.razor"));

        Assert.Contains("@page \"/branch-settings\"", settings, StringComparison.Ordinal);
        Assert.Contains("BranchSettings_LocalOnlyHint", settings, StringComparison.Ordinal);
        Assert.Contains("return=branch-settings", settings, StringComparison.Ordinal);
        Assert.DoesNotContain("/org/staff", settings, StringComparison.Ordinal);
        Assert.DoesNotContain("/organization/branches\"", settings, StringComparison.OrdinalIgnoreCase);

        Assert.Contains("SupplyParameterFromQuery", edit, StringComparison.Ordinal);
        Assert.Contains("branch-settings", edit, StringComparison.Ordinal);
        Assert.Contains("BackHref", edit, StringComparison.Ordinal);
        Assert.DoesNotContain("/org/staff", edit, StringComparison.Ordinal);
    }

    [Fact]
    public void Workspace_switch_and_display_only_topbar_preserved()
    {
        var maui = MauiProject();
        var menu = File.ReadAllText(Path.Combine(maui, "Components", "Shared", "ShellAccountMenu.razor"));
        var identity = File.ReadAllText(Path.Combine(maui, "Components", "Shared", "ShellOrganizationIdentity.razor"));

        Assert.Contains("WorkspaceSelect_SwitchMenu", menu, StringComparison.Ordinal);
        Assert.Contains("/workspace-select", menu, StringComparison.Ordinal);
        Assert.DoesNotContain("ShellBranchSwitcher", identity, StringComparison.Ordinal);
        Assert.DoesNotContain("pos-topbar__brand--switch", identity, StringComparison.Ordinal);
    }

    [Fact]
    public void Web_governance_navigation_unaffected()
    {
        var web = Path.Combine(
            FindRepoRoot(),
            "src",
            "Products",
            "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Web");
        var layout = File.ReadAllText(Path.Combine(web, "Components", "Layout", "MainLayout.razor"));
        var branches = File.ReadAllText(Path.Combine(web, "Components", "Pages", "Organization", "Branches.razor"));

        Assert.Contains("Nav_Branches", layout, StringComparison.Ordinal);
        Assert.DoesNotContain("@page \"/manage-business\"", File.ReadAllText(Path.Combine(web, "Components", "Routes.razor")), StringComparison.Ordinal);
        Assert.DoesNotContain("manage-business", branches, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Manage_business_and_branch_settings_localization_and_density_css_exist()
    {
        var maui = MauiProject();
        var css = File.ReadAllText(Path.Combine(maui, "wwwroot", "app.css"));
        var en = File.ReadAllText(Path.Combine(maui, "Localization", "PosResources.resx"));
        var fil = File.ReadAllText(Path.Combine(maui, "Localization", "PosResources.fil-PH.resx"));

        Assert.Contains(".pos-manage-business__row", css, StringComparison.Ordinal);
        Assert.Contains("min-height: 2.75rem", css, StringComparison.Ordinal);
        Assert.Contains(".pos-branch-settings__row", css, StringComparison.Ordinal);

        foreach (var key in new[]
                 {
                     "ManageBusiness_Menu",
                     "ManageBusiness_Title",
                     "ManageBusiness_PrimaryRequired",
                     "BranchSettings_Title",
                     "BranchSettings_Configure",
                     "BranchSettings_LocalOnlyHint"
                 })
        {
            Assert.Contains($"name=\"{key}\"", en, StringComparison.Ordinal);
            Assert.Contains($"name=\"{key}\"", fil, StringComparison.Ordinal);
        }
    }

    private static string MauiProject() =>
        Path.Combine(FindRepoRoot(), "src", "Products", "PinoyBusinessPOS", "ExItS.PinoyBusinessPOS.Maui");

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "ExItS.slnx")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Repository root not found.");
    }
}
