namespace ExItS.PinoyBusinessPOS.Maui.Tests;

public sealed class OwnerDashboardUiGuardTests
{
    [Fact]
    public void Owner_dashboard_uses_compact_action_grids_and_preserves_routes()
    {
        var owner = File.ReadAllText(Path.Combine(MauiProject(), "Components", "Pages", "Dashboards", "OwnerDashboard.razor"));
        var more = File.ReadAllText(Path.Combine(MauiProject(), "Components", "Pages", "MoreHub.razor"));
        var css = File.ReadAllText(Path.Combine(MauiProject(), "wwwroot", "app.css"));
        var en = File.ReadAllText(Path.Combine(MauiProject(), "Localization", "PosResources.resx"));

        Assert.Contains("@page \"/owner\"", owner, StringComparison.Ordinal);
        Assert.Contains("pos-action-grid", owner, StringComparison.Ordinal);
        Assert.Contains("pos-action-tile--primary", owner, StringComparison.Ordinal);
        Assert.Contains("Owner_QuickActionsSection", owner, StringComparison.Ordinal);
        Assert.Contains("Owner_InsightsSection", owner, StringComparison.Ordinal);
        Assert.Contains("StartSelling", owner, StringComparison.Ordinal);
        Assert.Contains("/sales/new", owner, StringComparison.Ordinal);
        Assert.Contains("GoOrg", owner, StringComparison.Ordinal);
        Assert.Contains("GoSetup", owner, StringComparison.Ordinal);
        Assert.Contains("GoCatalog", owner, StringComparison.Ordinal);
        Assert.Contains("GoCategories", owner, StringComparison.Ordinal);
        Assert.Contains("GoInventory", owner, StringComparison.Ordinal);
        Assert.Contains("GoStaff", owner, StringComparison.Ordinal);
        Assert.Contains("GoRegisters", owner, StringComparison.Ordinal);
        Assert.Contains("GoShifts", owner, StringComparison.Ordinal);
        Assert.Contains("GoSales", owner, StringComparison.Ordinal);
        Assert.Contains("GoPermissions", owner, StringComparison.Ordinal);
        Assert.Contains("GoReports", owner, StringComparison.Ordinal);
        Assert.Contains("GoSettings", owner, StringComparison.Ordinal);
        Assert.Contains("Owner_SetupStatusComplete", owner, StringComparison.Ordinal);
        Assert.DoesNotContain("InlineMessage", owner, StringComparison.Ordinal);
        Assert.DoesNotContain("OrganizationDisplayName", owner, StringComparison.Ordinal);

        Assert.Contains("pos-action-grid", more, StringComparison.Ordinal);
        Assert.Contains("More_ToolsSection", more, StringComparison.Ordinal);
        Assert.Contains("UtangCapability.ViewInventory", more, StringComparison.Ordinal);
        Assert.Contains("AccountContextSwitcher", more, StringComparison.Ordinal);

        Assert.Contains(".pos-action-grid", css, StringComparison.Ordinal);
        Assert.Contains(".pos-action-tile--primary", css, StringComparison.Ordinal);
        Assert.Contains(".pos-dash .exds-page-header", css, StringComparison.Ordinal);

        Assert.Contains("name=\"Owner_QuickActionsSection\"", en, StringComparison.Ordinal);
        Assert.Contains("name=\"Owner_InsightsSection\"", en, StringComparison.Ordinal);
        Assert.Contains("name=\"More_ToolsSection\"", en, StringComparison.Ordinal);
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
