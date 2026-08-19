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
        Assert.Contains("GoPurchasing", owner, StringComparison.Ordinal);
        Assert.DoesNotContain("GoStaff", owner, StringComparison.Ordinal);
        Assert.DoesNotContain("/org/staff", owner, StringComparison.Ordinal);
        Assert.Contains("GoRegisters", owner, StringComparison.Ordinal);
        Assert.Contains("GoShifts", owner, StringComparison.Ordinal);
        Assert.Contains("GoSales", owner, StringComparison.Ordinal);
        Assert.Contains("GoPermissions", owner, StringComparison.Ordinal);
        Assert.Contains("GoReports", owner, StringComparison.Ordinal);
        Assert.Contains("GoSettings", owner, StringComparison.Ordinal);
        Assert.Contains("Owner_SetupStatusComplete", owner, StringComparison.Ordinal);
        Assert.Contains("pos-dash__header", owner, StringComparison.Ordinal);
        Assert.Contains("pos-dash__title", owner, StringComparison.Ordinal);
        Assert.Contains("LoadIncomingSupplierCountAsync", owner, StringComparison.Ordinal);
        Assert.Contains("LoadIncomingOrderCountAsync", owner, StringComparison.Ordinal);
        Assert.Contains("GoIncomingOrders", owner, StringComparison.Ordinal);
        Assert.Contains("/connected-suppliers/incoming", owner, StringComparison.Ordinal);
        Assert.Contains("finally", owner, StringComparison.Ordinal);
        Assert.DoesNotContain("PageHeader", owner, StringComparison.Ordinal);
        Assert.DoesNotContain("InlineMessage", owner, StringComparison.Ordinal);
        Assert.DoesNotContain("OrganizationDisplayName", owner, StringComparison.Ordinal);

        Assert.Contains("pos-action-grid", more, StringComparison.Ordinal);
        Assert.Contains("More_ToolsSection", more, StringComparison.Ordinal);
        Assert.Contains("UtangCapability.ViewInventory", more, StringComparison.Ordinal);
        Assert.Contains("AccountContextSwitcher", more, StringComparison.Ordinal);
        Assert.Contains("pos-more__header", more, StringComparison.Ordinal);
        Assert.DoesNotContain("PageHeader", more, StringComparison.Ordinal);

        Assert.Contains(".pos-action-grid", css, StringComparison.Ordinal);
        Assert.Contains(".pos-action-tile--primary", css, StringComparison.Ordinal);
        Assert.Contains(".pos-dash__header", css, StringComparison.Ordinal);
        Assert.Contains(".pos-dash__title", css, StringComparison.Ordinal);

        Assert.Contains("name=\"Owner_QuickActionsSection\"", en, StringComparison.Ordinal);
        Assert.Contains("name=\"Owner_InsightsSection\"", en, StringComparison.Ordinal);
        Assert.Contains("name=\"More_ToolsSection\"", en, StringComparison.Ordinal);
    }

    [Fact]
    public void Manager_dashboard_uses_compact_action_grids_and_preserves_routes()
    {
        var manager = File.ReadAllText(Path.Combine(MauiProject(), "Components", "Pages", "Dashboards", "ManagerDashboard.razor"));
        var en = File.ReadAllText(Path.Combine(MauiProject(), "Localization", "PosResources.resx"));

        Assert.Contains("@page \"/manager\"", manager, StringComparison.Ordinal);
        Assert.Contains("pos-action-grid", manager, StringComparison.Ordinal);
        Assert.Contains("pos-action-tile--primary", manager, StringComparison.Ordinal);
        Assert.Contains("Manager_QuickActionsSection", manager, StringComparison.Ordinal);
        Assert.Contains("Manager_OperationsSection", manager, StringComparison.Ordinal);
        Assert.Contains("Manager_Subtitle", manager, StringComparison.Ordinal);
        Assert.Contains("pos-dash__header", manager, StringComparison.Ordinal);
        Assert.Contains("StartSelling", manager, StringComparison.Ordinal);
        Assert.Contains("/sales/new", manager, StringComparison.Ordinal);
        Assert.Contains("GoCatalog", manager, StringComparison.Ordinal);
        Assert.Contains("GoInventory", manager, StringComparison.Ordinal);
        Assert.Contains("GoRegisters", manager, StringComparison.Ordinal);
        Assert.Contains("GoShifts", manager, StringComparison.Ordinal);
        Assert.Contains("GoSales", manager, StringComparison.Ordinal);
        Assert.Contains("GoReports", manager, StringComparison.Ordinal);
        Assert.DoesNotContain("PageHeader", manager, StringComparison.Ordinal);
        Assert.DoesNotContain("InlineMessage", manager, StringComparison.Ordinal);
        Assert.DoesNotContain("OrganizationDisplayName", manager, StringComparison.Ordinal);
        Assert.DoesNotContain("GoReturns", manager, StringComparison.Ordinal);

        Assert.Contains("name=\"Manager_Subtitle\"", en, StringComparison.Ordinal);
        Assert.Contains("name=\"Manager_QuickActionsSection\"", en, StringComparison.Ordinal);
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
