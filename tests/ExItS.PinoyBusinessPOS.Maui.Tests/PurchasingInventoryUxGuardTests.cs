namespace ExItS.PinoyBusinessPOS.Maui.Tests;

public sealed class PurchasingInventoryUxGuardTests
{
    [Fact]
    public void OwnerHomeShowsPurchasing()
    {
        var owner = File.ReadAllText(Path.Combine(MauiProject(), "Components", "Pages", "Dashboards", "OwnerDashboard.razor"));
        Assert.Contains("GoPurchasing", owner, StringComparison.Ordinal);
        Assert.Contains("Nav_Purchasing", owner, StringComparison.Ordinal);
        Assert.Contains("/purchasing", owner, StringComparison.Ordinal);
    }

    [Fact]
    public void PurchasingShowsReceiveStock_PurchaseOrders_GoodsReceipts_Suppliers()
    {
        var hub = File.ReadAllText(Path.Combine(PurchasingPages(), "PurchasingHub.razor"));
        Assert.Contains("@page \"/purchasing\"", hub, StringComparison.Ordinal);
        Assert.Contains("Purchasing_HubSubtitle", hub, StringComparison.Ordinal);
        Assert.Contains("Purchasing_ReceiveStock", hub, StringComparison.Ordinal);
        Assert.Contains("Purchasing_Orders", hub, StringComparison.Ordinal);
        Assert.Contains("Purchasing_GoodsReceipts", hub, StringComparison.Ordinal);
        Assert.Contains("Purchasing_Suppliers", hub, StringComparison.Ordinal);
        Assert.Contains("/purchasing/receive-stock", hub, StringComparison.Ordinal);
        Assert.Contains("/purchasing/orders", hub, StringComparison.Ordinal);
        Assert.Contains("/purchasing/receipts", hub, StringComparison.Ordinal);
        Assert.Contains("/suppliers", hub, StringComparison.Ordinal);
        Assert.Contains("Purchasing_ChoiceReceive", hub, StringComparison.Ordinal);
        Assert.Contains("Purchasing_ChoiceOrder", hub, StringComparison.Ordinal);
        Assert.DoesNotContain("Direct Stock In", hub, StringComparison.Ordinal);
        Assert.DoesNotContain("Manual Purchase", hub, StringComparison.Ordinal);
    }

    [Fact]
    public void ReceiveStockUsesPlainLanguage()
    {
        var receive = File.ReadAllText(Path.Combine(PurchasingPages(), "ReceiveStock.razor"));
        var adjust = File.ReadAllText(Path.Combine(InventoryPages(), "InventoryAdjust.razor"));
        var en = File.ReadAllText(Path.Combine(MauiProject(), "Localization", "PosResources.resx"));

        Assert.Contains("@page \"/purchasing/receive-stock\"", receive, StringComparison.Ordinal);
        Assert.Contains("Purchasing_ReceiveStock", receive, StringComparison.Ordinal);
        Assert.Contains("Purchasing_ReceiveStockHelper", receive, StringComparison.Ordinal);
        Assert.Contains("Purchasing_ReceiveStockSearchPlaceholder", receive, StringComparison.Ordinal);
        Assert.Contains("Purchasing_ReceiveStockAction", receive, StringComparison.Ordinal);
        Assert.Contains("pos-receive-stock__search", receive, StringComparison.Ordinal);
        Assert.Contains("pos-receive-stock__row-action", receive, StringComparison.Ordinal);
        Assert.Contains("intent=receive", receive, StringComparison.Ordinal);
        Assert.Contains("IsReceiveStock", adjust, StringComparison.Ordinal);
        Assert.Contains("Purchasing_ReceiveStockWillIncrease", adjust, StringComparison.Ordinal);
        Assert.Contains("Purchasing_ReceiveStockSuccessTitle", adjust, StringComparison.Ordinal);
        Assert.Contains("<value>Receive stock</value>", en, StringComparison.Ordinal);
        Assert.Contains("<value>Receive</value>", en, StringComparison.Ordinal);
        Assert.Contains("<value>Search products</value>", en, StringComparison.Ordinal);
        Assert.Contains("<value>Stock received</value>", en, StringComparison.Ordinal);
        Assert.DoesNotContain("Direct Stock In", receive, StringComparison.Ordinal);
        Assert.DoesNotContain("Manual Purchase", receive, StringComparison.Ordinal);
        Assert.DoesNotContain("Direct Stock In", adjust, StringComparison.Ordinal);
    }

    [Fact]
    public void DirectStockInNotShownAsPrimaryUserFacingLabel()
    {
        var maui = MauiProject();
        foreach (var file in Directory.EnumerateFiles(Path.Combine(maui, "Components"), "*.razor", SearchOption.AllDirectories))
        {
            var text = File.ReadAllText(file);
            Assert.DoesNotContain("Direct Stock In", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Manual Purchase", text, StringComparison.OrdinalIgnoreCase);
        }

        var en = File.ReadAllText(Path.Combine(maui, "Localization", "PosResources.resx"));
        Assert.DoesNotContain(">Direct Stock In<", en, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(">Manual Purchase<", en, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void InventoryShowsStockControlActions_NotReceiveStockAsPrimary()
    {
        var list = File.ReadAllText(Path.Combine(InventoryPages(), "InventoryList.razor"));
        Assert.Contains("Inventory_StockOnHand", list, StringComparison.Ordinal);
        Assert.Contains("Inventory_StockCount", list, StringComparison.Ordinal);
        Assert.Contains("Inventory_AdjustStock", list, StringComparison.Ordinal);
        Assert.Contains("Inventory_OpenTransfers", list, StringComparison.Ordinal);
        Assert.Contains("Inventory_Expiration", list, StringComparison.Ordinal);
        Assert.Contains("/inventory/expiration", list, StringComparison.Ordinal);
        Assert.Contains("/inventory/counts", list, StringComparison.Ordinal);
        Assert.Contains("/inventory/transfers", list, StringComparison.Ordinal);
        Assert.Contains("Inventory_NeedReceiveStock", list, StringComparison.Ordinal);
        Assert.Contains("/purchasing/receive-stock", list, StringComparison.Ordinal);
        Assert.DoesNotContain("GoReceiveStock as inventory count", list, StringComparison.Ordinal);
        Assert.DoesNotContain("Inventory_OpenLowStock", list, StringComparison.Ordinal);
    }

    [Fact]
    public void PurchaseOrderListMovedOffPurchasingHubRoute()
    {
        var list = File.ReadAllText(Path.Combine(PurchasingPages(), "PurchasingList.razor"));
        Assert.Contains("@page \"/purchasing/orders\"", list, StringComparison.Ordinal);
        Assert.DoesNotContain("@page \"/purchasing\"", list, StringComparison.Ordinal);
        Assert.Contains("Purchasing_OrdersNoStockNote", list, StringComparison.Ordinal);
    }

    private static string MauiProject() => Path.Combine(
        FindRepoRoot(),
        "src",
        "Products",
        "PinoyBusinessPOS",
        "ExItS.PinoyBusinessPOS.Maui");

    private static string PurchasingPages() => Path.Combine(MauiProject(), "Components", "Pages", "Purchasing");

    private static string InventoryPages() => Path.Combine(MauiProject(), "Components", "Pages", "Inventory");

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
