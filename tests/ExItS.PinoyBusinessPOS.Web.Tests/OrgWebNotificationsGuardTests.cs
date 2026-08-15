namespace ExItS.PinoyBusinessPOS.Web.Tests;

public sealed class OrgWebNotificationsGuardTests
{
    [Fact]
    public void Notifications_page_supports_filters_and_supplier_accept_decline()
    {
        var page = File.ReadAllText(Path.Combine(WebProject(),
            "Components", "Pages", "Notifications", "Notifications.razor"));
        Assert.Contains("Notifications_FilterUnread", page, StringComparison.Ordinal);
        Assert.Contains("Notifications_FilterAll", page, StringComparison.Ordinal);
        Assert.Contains("SupplierConnectionNotificationTypes", page, StringComparison.Ordinal);
        Assert.Contains("ApproveAsync", page, StringComparison.Ordinal);
        Assert.Contains("DeclineAsync", page, StringComparison.Ordinal);
        Assert.Contains("ListRelationshipsAsync(\"supplier\")", page, StringComparison.Ordinal);
        Assert.Contains("MarkRelatedOrganizationNotificationsReadAsync", page, StringComparison.Ordinal);
        Assert.Contains("UtangCapability.ManageSuppliers", page, StringComparison.Ordinal);
        Assert.Contains("MarkReadOptimisticAsync", page, StringComparison.Ordinal);
        Assert.Contains("ApplyLocalRead", page, StringComparison.Ordinal);
        Assert.Contains("Nav.NavigateTo(\"/suppliers\")", page, StringComparison.Ordinal);
        Assert.Contains("Nav.NavigateTo(\"/suppliers/buyers\")", page, StringComparison.Ordinal);
        Assert.Contains("Notifications_ActionNeeded", page, StringComparison.Ordinal);
    }

    [Fact]
    public void OrgWebNotificationTapMarksRead_and_connected_buyers_nav_exists()
    {
        var page = File.ReadAllText(Path.Combine(WebProject(),
            "Components", "Pages", "Notifications", "Notifications.razor"));
        Assert.Contains("MarkOrganizationNotificationReadAsync", page, StringComparison.Ordinal);
        Assert.Contains("Shell.UnreadNotificationCount = _items.Count(n => !n.IsRead)", page, StringComparison.Ordinal);

        var suppliers = File.ReadAllText(Path.Combine(WebProject(),
            "Components", "Pages", "Suppliers", "Suppliers.razor"));
        Assert.Contains("@page \"/suppliers/buyers\"", suppliers, StringComparison.Ordinal);
        Assert.Contains("Suppliers_TabBuyers", suppliers, StringComparison.Ordinal);
        Assert.Contains("_connectedBuyers", suppliers, StringComparison.Ordinal);
        Assert.Contains("Suppliers_BuyerNotCustomerNote", suppliers, StringComparison.Ordinal);
        Assert.Contains("HeaderTitle", suppliers, StringComparison.Ordinal);
        Assert.Contains("Suppliers_BuyersHelp", suppliers, StringComparison.Ordinal);
        Assert.Contains("string.Equals(x.Status, \"Active\"", suppliers, StringComparison.Ordinal);

        var layout = File.ReadAllText(Path.Combine(WebProject(),
            "Components", "Layout", "MainLayout.razor"));
        Assert.Contains("/suppliers/buyers", layout, StringComparison.Ordinal);
        Assert.Contains("Nav_ConnectedBuyers", layout, StringComparison.Ordinal);
    }

    [Fact]
    public void Shell_allows_owner_and_manager_notifications_and_compact_overview_banner()
    {
        var shell = File.ReadAllText(Path.Combine(WebProject(), "Services", "WebHostServices.cs"));
        Assert.Contains("\"notifications\" => IsOrgOwner || IsOrgManager", shell, StringComparison.Ordinal);

        var overview = File.ReadAllText(Path.Combine(WebProject(),
            "Components", "Pages", "Overview.razor"));
        Assert.Contains("Suppliers_IncomingCompact", overview, StringComparison.Ordinal);
        Assert.DoesNotContain("Suppliers_IncomingBannerBody", overview, StringComparison.Ordinal);

        var layout = File.ReadAllText(Path.Combine(WebProject(),
            "Components", "Layout", "MainLayout.razor"));
        Assert.Contains("/notifications", layout, StringComparison.Ordinal);
        Assert.Contains("UnreadNotificationCount", layout, StringComparison.Ordinal);
        Assert.Contains("AriaLabel", layout, StringComparison.Ordinal);
        Assert.Contains("<Tooltip Title=\"@NotificationBellTitle\">", layout, StringComparison.Ordinal);
        Assert.Contains("Notifications_Aria", layout, StringComparison.Ordinal);
    }

    [Fact]
    public void Localization_includes_notification_action_keys()
    {
        var en = File.ReadAllText(Path.Combine(WebProject(), "Localization", "OrgWebResources.resx"));
        var fil = File.ReadAllText(Path.Combine(WebProject(), "Localization", "OrgWebResources.fil-PH.resx"));
        foreach (var key in new[]
                 {
                     "Notifications_FilterUnread",
                     "Notifications_Accept",
                     "Notifications_Decline",
                     "Notifications_AriaUnread",
                     "Suppliers_IncomingCompact"
                 })
        {
            Assert.Contains($"name=\"{key}\"", en, StringComparison.Ordinal);
            Assert.Contains($"name=\"{key}\"", fil, StringComparison.Ordinal);
        }
    }

    private static string WebProject() =>
        Path.Combine(FindRepoRoot(), "src", "Products", "PinoyBusinessPOS", "ExItS.PinoyBusinessPOS.Web");

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
