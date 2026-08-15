using ExItS.PinoyBusinessPOS.Application.ConnectedSuppliers;

namespace ExItS.PinoyBusinessPOS.Maui.Tests;

public sealed class OrganizationNotificationsUiGuardTests
{
    [Fact]
    public void Unified_org_notifications_page_covers_routes_filters_and_supplier_actions()
    {
        var page = File.ReadAllText(Path.Combine(MauiProject(),
            "Components", "Pages", "Organization", "OrganizationNotifications.razor"));

        Assert.Contains("@page \"/org/notifications\"", page, StringComparison.Ordinal);
        Assert.Contains("@page \"/org/customer-link-notifications\"", page, StringComparison.Ordinal);
        Assert.Contains("OrgNotifications_Title", page, StringComparison.Ordinal);
        Assert.Contains("OrgNotifications_FilterUnread", page, StringComparison.Ordinal);
        Assert.Contains("OrgNotifications_FilterAll", page, StringComparison.Ordinal);
        Assert.Contains(nameof(SupplierConnectionNotificationTypes), page, StringComparison.Ordinal);
        Assert.Contains("SupplierConnectionNotificationTypes.Requested", page, StringComparison.Ordinal);
        Assert.Contains("ListRelationshipsAsync(\"supplier\")", page, StringComparison.Ordinal);
        Assert.Contains("ListRelationshipsAsync(\"buyer\")", page, StringComparison.Ordinal);
        Assert.Contains("ApproveAsync", page, StringComparison.Ordinal);
        Assert.Contains("DeclineAsync", page, StringComparison.Ordinal);
        Assert.Contains("UtangCapability.ManageSuppliers", page, StringComparison.Ordinal);
        Assert.Contains("OrgNotifications_OfflineRespond", page, StringComparison.Ordinal);
        Assert.Contains("OrgNotifications_StatusConnected", page, StringComparison.Ordinal);
        Assert.Contains("OrgNotifications_StatusDeclined", page, StringComparison.Ordinal);
        Assert.Contains("OrgNotifications_StatusUnavailable", page, StringComparison.Ordinal);
        Assert.Contains("OrgNotifications_ActionNeeded", page, StringComparison.Ordinal);
        Assert.Contains("MarkOrganizationNotificationReadAsync", page, StringComparison.Ordinal);
        Assert.Contains("MarkRelatedOrganizationNotificationsReadAsync", page, StringComparison.Ordinal);
        Assert.Contains("AcceptedConfirmation", page, StringComparison.Ordinal);
        Assert.Contains("DeclinedConfirmation", page, StringComparison.Ordinal);
        Assert.Contains("UnreadState.NotifyChanged", page, StringComparison.Ordinal);
        Assert.Contains("ApplyLocalRead", page, StringComparison.Ordinal);
        Assert.Contains("/suppliers/connected/requests", page, StringComparison.Ordinal);
        Assert.Contains("/suppliers/connected/buyers", page, StringComparison.Ordinal);
        Assert.Contains("Nav.NavigateTo(\"/suppliers\")", page, StringComparison.Ordinal);
        Assert.Contains("Pending", page, StringComparison.Ordinal);
    }

    [Fact]
    public void MauiNotificationTapMarksRead_and_connected_buyers_page_exists()
    {
        var page = File.ReadAllText(Path.Combine(MauiProject(),
            "Components", "Pages", "Organization", "OrganizationNotifications.razor"));
        Assert.Contains("disabled=\"@_busy\"", page, StringComparison.Ordinal);
        Assert.Contains("MarkReadOptimisticAsync", page, StringComparison.Ordinal);
        Assert.Contains("ApplyLocalRead", page, StringComparison.Ordinal);
        Assert.DoesNotContain("disabled=\"_busy\"", page, StringComparison.Ordinal);

        var buyers = File.ReadAllText(Path.Combine(MauiProject(),
            "Components", "Pages", "Suppliers", "ConnectedBuyers.razor"));
        Assert.Contains("@page \"/suppliers/connected/buyers\"", buyers, StringComparison.Ordinal);
        Assert.Contains("ListRelationshipsAsync(\"supplier\")", buyers, StringComparison.Ordinal);
        Assert.Contains("ConnectedBuyers_Title", buyers, StringComparison.Ordinal);
        Assert.Contains("ConnectedBuyers_NotCustomerNote", buyers, StringComparison.Ordinal);
        Assert.Contains("They buy from your business", File.ReadAllText(Path.Combine(MauiProject(), "Localization", "PosResources.resx")), StringComparison.Ordinal);

        var list = File.ReadAllText(Path.Combine(MauiProject(),
            "Components", "Pages", "Suppliers", "SuppliersList.razor"));
        Assert.Contains("GoConnectedBuyers", list, StringComparison.Ordinal);
        Assert.Contains("/suppliers/connected/buyers", list, StringComparison.Ordinal);
    }

    [Fact]
    public void Localization_includes_org_notification_keys()
    {
        var en = File.ReadAllText(Path.Combine(MauiProject(), "Localization", "PosResources.resx"));
        var fil = File.ReadAllText(Path.Combine(MauiProject(), "Localization", "PosResources.fil-PH.resx"));
        foreach (var key in new[]
                 {
                     "OrgNotifications_Title",
                     "OrgNotifications_FilterUnread",
                     "OrgNotifications_FilterAll",
                     "OrgNotifications_OfflineRespond",
                     "OrgNotifications_StatusConnected",
                     "ConnectedSuppliers_IncomingCompact"
                 })
        {
            Assert.Contains($"name=\"{key}\"", en, StringComparison.Ordinal);
            Assert.Contains($"name=\"{key}\"", fil, StringComparison.Ordinal);
        }

        Assert.Contains("supplier requests need your review", en, StringComparison.Ordinal);
        Assert.Contains("Connect to the internet to respond.", en, StringComparison.Ordinal);
    }

    [Fact]
    public void Compact_incoming_banners_use_single_line_copy()
    {
        var owner = File.ReadAllText(Path.Combine(MauiProject(),
            "Components", "Pages", "Dashboards", "OwnerDashboard.razor"));
        var more = File.ReadAllText(Path.Combine(MauiProject(),
            "Components", "Pages", "MoreHub.razor"));
        var suppliers = File.ReadAllText(Path.Combine(MauiProject(),
            "Components", "Pages", "Suppliers", "SuppliersList.razor"));

        Assert.Contains("ConnectedSuppliers_IncomingCompact", owner, StringComparison.Ordinal);
        Assert.Contains("ConnectedSuppliers_IncomingCompact", more, StringComparison.Ordinal);
        Assert.Contains("ConnectedSuppliers_IncomingCompact", suppliers, StringComparison.Ordinal);
        Assert.DoesNotContain("ConnectedSuppliers_IncomingBannerBody", owner, StringComparison.Ordinal);
        Assert.DoesNotContain("ConnectedSuppliers_IncomingBannerBody", more, StringComparison.Ordinal);
        Assert.DoesNotContain("ConnectedSuppliers_IncomingBannerBody", suppliers, StringComparison.Ordinal);
        Assert.Contains("ConnectedSuppliers_IncomingRequests", more, StringComparison.Ordinal);
        Assert.Contains("_incomingPendingCount", more, StringComparison.Ordinal);
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
