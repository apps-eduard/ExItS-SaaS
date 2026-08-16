namespace ExItS.PinoyBusinessPOS.Maui.Tests;

public sealed class CustomerOrderUiGuardTests
{
    [Fact]
    public void Seller_orders_page_uses_compact_cards_not_table()
    {
        var maui = MauiProject();
        var list = File.ReadAllText(Path.Combine(maui, "Components", "Pages", "Orders", "SellerOrders.razor"));
        var detail = File.ReadAllText(Path.Combine(maui, "Components", "Pages", "Orders", "SellerOrderDetail.razor"));
        var css = File.ReadAllText(Path.Combine(maui, "wwwroot", "app.css"));
        var more = File.ReadAllText(Path.Combine(maui, "Components", "Pages", "MoreHub.razor"));

        Assert.Contains("@page \"/orders\"", list, StringComparison.Ordinal);
        Assert.Contains("pos-orders__card", list, StringComparison.Ordinal);
        Assert.Contains("pos-orders__filters", list, StringComparison.Ordinal);
        Assert.Contains("Orders_Filter_New", list, StringComparison.Ordinal);
        Assert.Contains("Orders_Filter_Preparing", list, StringComparison.Ordinal);
        Assert.Contains("Orders_Filter_Ready", list, StringComparison.Ordinal);
        Assert.Contains("Orders_Filter_Issues", list, StringComparison.Ordinal);
        Assert.Contains("ListSellerOrdersAsync", list, StringComparison.Ordinal);
        Assert.DoesNotContain("<table", list, StringComparison.OrdinalIgnoreCase);

        Assert.Contains("@page \"/orders/{OrderId:guid}\"", detail, StringComparison.Ordinal);
        Assert.Contains("pos-orders__actions", detail, StringComparison.Ordinal);
        Assert.Contains("AcceptAsync", detail, StringComparison.Ordinal);
        Assert.Contains("MarkReadyAsync", detail, StringComparison.Ordinal);
        Assert.Contains("MarkOutForDeliveryAsync", detail, StringComparison.Ordinal);

        Assert.Contains(".pos-orders__", css, StringComparison.Ordinal);
        Assert.Contains("pos-orders__actions--sticky", css, StringComparison.Ordinal);

        Assert.Contains("/orders", more, StringComparison.Ordinal);
        Assert.Contains("UtangCapability.ViewCustomerOrders", more, StringComparison.Ordinal);
        Assert.Contains("UtangCapability.ManageCustomerOrders", more, StringComparison.Ordinal);
        Assert.Contains("Orders_Title", more, StringComparison.Ordinal);
        Assert.Contains("GoOrders", more, StringComparison.Ordinal);
    }

    [Fact]
    public void Personal_orders_pages_and_timeline_exist()
    {
        var maui = MauiProject();
        var personalMore = File.ReadAllText(Path.Combine(maui, "Components", "Pages", "Personal", "PersonalMore.razor"));
        var list = File.ReadAllText(Path.Combine(maui, "Components", "Pages", "Personal", "PersonalOrders.razor"));
        var detail = File.ReadAllText(Path.Combine(maui, "Components", "Pages", "Personal", "PersonalOrderDetail.razor"));

        Assert.Contains("/personal/orders", personalMore, StringComparison.Ordinal);
        Assert.Contains("Orders_Title", personalMore, StringComparison.Ordinal);
        Assert.Contains("@page \"/personal/orders\"", list, StringComparison.Ordinal);
        Assert.Contains("ListMineAsync", list, StringComparison.Ordinal);
        Assert.Contains("pos-orders__card", list, StringComparison.Ordinal);
        Assert.DoesNotContain("<table", list, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("@page \"/personal/orders/{OrderId:guid}\"", detail, StringComparison.Ordinal);
        Assert.Contains("pos-orders__timeline", detail, StringComparison.Ordinal);
        Assert.Contains("Orders_Timeline_Placed", detail, StringComparison.Ordinal);
        Assert.Contains("Orders_WaitingForSeller", detail, StringComparison.Ordinal);
    }

    [Fact]
    public void Customer_order_strings_exist_in_en_and_fil()
    {
        var localization = Path.Combine(MauiProject(), "Localization");
        var en = File.ReadAllText(Path.Combine(localization, "PosResources.resx"));
        var fil = File.ReadAllText(Path.Combine(localization, "PosResources.fil-PH.resx"));
        foreach (var key in new[]
                 {
                     "Orders_Title",
                     "Orders_Filter_New",
                     "Orders_Filter_Preparing",
                     "Orders_Filter_Ready",
                     "Orders_Pickup",
                     "Orders_Delivery",
                     "Orders_Accept",
                     "Orders_Reject",
                     "Orders_PlaceOrder",
                     "Orders_WaitingForSeller",
                     "Orders_OutForDelivery",
                     "Orders_Status_Delivered",
                     "Orders_Status_Collected",
                     "Orders_Status_Completed",
                     "Orders_Status_Cancelled",
                     "Orders_MinOrderHelper",
                     "Orders_OutsideDeliveryArea",
                     "Orders_ChooseBranch"
                 })
        {
            Assert.Contains($"<data name=\"{key}\"", en, StringComparison.Ordinal);
            Assert.Contains($"<data name=\"{key}\"", fil, StringComparison.Ordinal);
        }
    }

    private static string MauiProject()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "src", "Products", "PinoyBusinessPOS", "ExItS.PinoyBusinessPOS.Maui");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("MAUI project not found.");
    }
}
