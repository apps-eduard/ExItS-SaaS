namespace ExItS.PinoyBusinessPOS.Maui.Tests;

public sealed class MoreHubUiGuardTests
{
    [Fact]
    public void MoreHub_uses_compact_action_grids_and_preserves_capability_gates()
    {
        var more = File.ReadAllText(Path.Combine(MauiProject(), "Components", "Pages", "MoreHub.razor"));
        var css = File.ReadAllText(Path.Combine(MauiProject(), "wwwroot", "app.css"));
        var switcher = File.ReadAllText(Path.Combine(MauiProject(), "Components", "Shared", "AccountContextSwitcher.razor"));
        var en = File.ReadAllText(Path.Combine(MauiProject(), "Localization", "PosResources.resx"));

        Assert.Contains("@page \"/more\"", more, StringComparison.Ordinal);
        Assert.Contains("pos-action-grid", more, StringComparison.Ordinal);
        Assert.Contains("pos-action-tile", more, StringComparison.Ordinal);
        Assert.Contains("More_WorkspaceSection", more, StringComparison.Ordinal);
        Assert.Contains("More_ToolsSection", more, StringComparison.Ordinal);
        Assert.Contains("GoRoleHome", more, StringComparison.Ordinal);
        Assert.Contains("GoOrg", more, StringComparison.Ordinal);
        Assert.Contains("AccountContextSwitcher", more, StringComparison.Ordinal);
        Assert.Contains("UtangCapability.ViewInventory", more, StringComparison.Ordinal);
        Assert.Contains("UtangCapability.ViewExpenses", more, StringComparison.Ordinal);
        Assert.Contains("UtangCapability.ViewSuppliers", more, StringComparison.Ordinal);
        Assert.Contains("UtangCapability.ViewPurchasing", more, StringComparison.Ordinal);
        Assert.Contains("UtangCapability.ViewRegisters", more, StringComparison.Ordinal);
        Assert.Contains("UtangCapability.ManageOperationalSetup", more, StringComparison.Ordinal);
        Assert.Contains("UtangCapability.ViewShifts", more, StringComparison.Ordinal);
        Assert.Contains("UtangCapability.ViewPermissions", more, StringComparison.Ordinal);
        Assert.Contains("UtangCapability.ViewReports", more, StringComparison.Ordinal);
        Assert.Contains("/overdue", more, StringComparison.Ordinal);
        Assert.Contains("/inventory", more, StringComparison.Ordinal);
        Assert.Contains("Auth_Logout", more, StringComparison.Ordinal);

        Assert.Contains(".pos-action-grid", css, StringComparison.Ordinal);
        Assert.Contains(".pos-dash .exds-page-header", css, StringComparison.Ordinal);
        Assert.Contains("border-bottom: none", css, StringComparison.Ordinal);

        Assert.Contains("Context_RoleOwner", switcher, StringComparison.Ordinal);
        Assert.DoesNotContain("@org.MembershipRole", switcher, StringComparison.Ordinal);
        Assert.Contains("RoleLabel(org.MembershipRole)", switcher, StringComparison.Ordinal);
        Assert.Contains("// Never surface raw Platform membership role codes", switcher, StringComparison.Ordinal);
        Assert.Contains("pos-context-row", switcher, StringComparison.Ordinal);
        Assert.Contains("SwitchToPersonalAsync", switcher, StringComparison.Ordinal);
        Assert.Contains("SelectOrganizationAsync", switcher, StringComparison.Ordinal);

        Assert.Contains("name=\"More_ToolsSection\"", en, StringComparison.Ordinal);
        Assert.Contains("<value>Tools and organization</value>", en, StringComparison.Ordinal);
        Assert.Contains("name=\"Context_RoleOwner\"", en, StringComparison.Ordinal);
        Assert.Contains("<value>Owner</value>", en, StringComparison.Ordinal);
    }

    private static string MauiProject() => Path.Combine(
        FindRepoRoot(),
        "src",
        "Products",
        "PinoyBusinessPOS",
        "ExItS.PinoyBusinessPOS.Maui");

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
