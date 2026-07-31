namespace ExItS.PinoyBusinessPOS.Maui.Tests;

public sealed class SalePageGuardTests
{
    [Fact]
    public void Sales_routes_cover_history_checkout_and_detail()
    {
        var sales = SalesPagesDirectory();

        var list = File.ReadAllText(Path.Combine(sales, "SalesList.razor"));
        Assert.Contains("@page \"/sales\"", list, StringComparison.Ordinal);
        Assert.Contains("IPosSaleClient", list, StringComparison.Ordinal);
        Assert.Contains("ResponsiveDataList", list, StringComparison.Ordinal);
        Assert.Contains("Sales_Filter_Status", list, StringComparison.Ordinal);
        Assert.Contains("Sales_Filter_Payment", list, StringComparison.Ordinal);
        Assert.Contains("Sales_Filter_FromDate", list, StringComparison.Ordinal);

        var checkout = File.ReadAllText(Path.Combine(sales, "SaleCheckout.razor"));
        Assert.Contains("@page \"/sales/new\"", checkout, StringComparison.Ordinal);
        Assert.Contains("LookupByBarcodeAsync", checkout, StringComparison.Ordinal);
        Assert.Contains("LookupBySkuAsync", checkout, StringComparison.Ordinal);
        Assert.Contains("SaleCartService", checkout, StringComparison.Ordinal);
        Assert.Contains("Cart.SetQuantity", checkout, StringComparison.Ordinal);
        Assert.Contains("Cart.Remove", checkout, StringComparison.Ordinal);
        Assert.Contains("IPosCustomerClient", checkout, StringComparison.Ordinal);
        Assert.Contains("PosSaleOptions.UtangPaymentMethod", checkout, StringComparison.Ordinal);
        Assert.Contains("Sales_GCash_ManualWarning", checkout, StringComparison.Ordinal);
        Assert.Contains("Sales_Checkout_UtangWarning", checkout, StringComparison.Ordinal);
        Assert.Contains("Sales_Field_ChangeAmount", checkout, StringComparison.Ordinal);
        Assert.Contains("ConfirmDialog", checkout, StringComparison.Ordinal);
        Assert.Contains("GetCreditSummaryAsync", checkout, StringComparison.Ordinal);

        var detail = File.ReadAllText(Path.Combine(sales, "SaleDetail.razor"));
        Assert.Contains("@page \"/sales/{SaleId:guid}\"", detail, StringComparison.Ordinal);
        Assert.Contains("VoidSaleAsync", detail, StringComparison.Ordinal);
        Assert.Contains("ShowReason=\"true\"", detail, StringComparison.Ordinal);
        Assert.Contains("Sales_VoidUtangMessage", detail, StringComparison.Ordinal);
        Assert.Contains("Sales_Detail_LinkedCustomer", detail, StringComparison.Ordinal);
        Assert.Contains("UtangCapability.ReverseCredit", detail, StringComparison.Ordinal);
        Assert.Contains("UtangCapability.ViewReturns", detail, StringComparison.Ordinal);
        Assert.Contains("UtangCapability.ProcessReturn", detail, StringComparison.Ordinal);
        Assert.Contains("IPosSaleReturnClient", detail, StringComparison.Ordinal);

        var returnsDir = Path.Combine(MauiProject(), "Components", "Pages", "Returns");
        Assert.True(Directory.Exists(returnsDir));
        var returnPage = File.ReadAllText(Path.Combine(returnsDir, "SaleReturn.razor"));
        Assert.Contains("@page \"/sales/{SaleId:guid}/return\"", returnPage, StringComparison.Ordinal);
        Assert.Contains("Returns_OfflineMessage", returnPage, StringComparison.Ordinal);

        Assert.True(File.Exists(Path.Combine(sales, "SalesUiOptions.cs")));
    }

    [Fact]
    public void Sales_pages_guard_entry_and_gate_mutations_on_capability()
    {
        foreach (var file in Directory.EnumerateFiles(SalesPagesDirectory(), "*.razor"))
        {
            var text = File.ReadAllText(file);
            Assert.Contains("Gate.CanEnterProtectedShell", text, StringComparison.Ordinal);
            Assert.Contains("ResolveStartRouteAsync", text, StringComparison.Ordinal);
        }

        var list = File.ReadAllText(Path.Combine(SalesPagesDirectory(), "SalesList.razor"));
        Assert.Contains("UtangCapability.ViewSales", list, StringComparison.Ordinal);
        Assert.Contains("UtangCapability.CreateSale", list, StringComparison.Ordinal);

        var checkout = File.ReadAllText(Path.Combine(SalesPagesDirectory(), "SaleCheckout.razor"));
        Assert.Contains("UtangCapability.CreateSale", checkout, StringComparison.Ordinal);
        Assert.Contains("UtangCapability.CreateCredit", checkout, StringComparison.Ordinal);

        var detail = File.ReadAllText(Path.Combine(SalesPagesDirectory(), "SaleDetail.razor"));
        Assert.Contains("UtangCapability.VoidSale", detail, StringComparison.Ordinal);
        Assert.Contains("UtangCapability.ReverseCredit", detail, StringComparison.Ordinal);
        Assert.Contains("UtangCapability.ProcessReturn", detail, StringComparison.Ordinal);

        var returnPage = File.ReadAllText(Path.Combine(MauiProject(), "Components", "Pages", "Returns", "SaleReturn.razor"));
        Assert.Contains("Gate.CanEnterProtectedShell", returnPage, StringComparison.Ordinal);
        Assert.Contains("UtangCapability.ProcessReturn", returnPage, StringComparison.Ordinal);
        Assert.Contains("Connectivity.IsConnectedAsync", returnPage, StringComparison.Ordinal);
    }

    [Fact]
    public void Sales_pages_require_reconnect_and_never_read_local_storage()
    {
        foreach (var file in Directory.EnumerateFiles(SalesPagesDirectory(), "*.razor"))
        {
            var text = File.ReadAllText(file);
            Assert.Contains("Connectivity.IsConnectedAsync", text, StringComparison.Ordinal);
            Assert.Contains("Sales_Offline", text, StringComparison.Ordinal);
            Assert.DoesNotContain("ILocalCustomerCreditStore", text, StringComparison.Ordinal);
            Assert.DoesNotContain("IOfflineOperationQueue", text, StringComparison.Ordinal);
            Assert.DoesNotContain("ILocalContextManager", text, StringComparison.Ordinal);
            Assert.DoesNotContain("SQLite", text, StringComparison.OrdinalIgnoreCase);
        }

        // Checkout must re-check connectivity at the moment of recording, not only on load.
        var checkout = File.ReadAllText(Path.Combine(SalesPagesDirectory(), "SaleCheckout.razor"));
        Assert.Contains("Sales_Checkout_OfflineMessage", checkout, StringComparison.Ordinal);
    }

    [Fact]
    public void Sales_pages_have_no_stock_discount_tax_refund_or_offline_utang_queue_surface()
    {
        foreach (var file in Directory.EnumerateFiles(SalesPagesDirectory(), "*"))
        {
            var text = File.ReadAllText(file);
            foreach (var forbidden in new[]
                     {
                         "StockOnHand", "QuantityOnHand", "Reorder", "TaxRate", "DiscountRate",
                         "DiscountAmount", "SaleRefund", "RefundId", "ProcessRefund", "SplitTender", "PrintReceipt", "PaymentGateway",
                         "UtangBalance", "Installment", "IOfflineOperationQueue", "SaleCheckoutOffline"
                     })
            {
                Assert.DoesNotContain(forbidden, text, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    [Fact]
    public void Cart_is_in_memory_only_and_clears_on_session_change()
    {
        var cart = File.ReadAllText(Path.Combine(
            FindRepoRoot(), "src", "Products", "PinoyBusinessPOS", "ExItS.PinoyBusinessPOS.Maui",
            "Services", "SaleCartService.cs"));

        Assert.Contains("ICurrentUserContext", cart, StringComparison.Ordinal);
        Assert.Contains("_currentUser.Changed +=", cart, StringComparison.Ordinal);
        Assert.Contains("Clear()", cart, StringComparison.Ordinal);

        // No persistence of any kind: the cart must not reach SQLite, preferences, or secure storage.
        foreach (var forbidden in new[]
                 {
                     "using ExItS.PinoyBusinessPOS.LocalStore",
                     "Microsoft.Data.Sqlite",
                     "ILocalCustomerCreditStore",
                     "IOfflineOperationQueue",
                     "Preferences.Set",
                     "SecureStorage."
                 })
        {
            Assert.DoesNotContain(forbidden, cart, StringComparison.Ordinal);
        }

        var program = File.ReadAllText(Path.Combine(
            FindRepoRoot(), "src", "Products", "PinoyBusinessPOS", "ExItS.PinoyBusinessPOS.Maui",
            "MauiProgram.cs"));
        Assert.Contains("SaleCartService", program, StringComparison.Ordinal);
        Assert.Contains("P8-WP02-simple-sales", program, StringComparison.Ordinal);
    }

    [Fact]
    public void Shell_sales_tab_points_at_the_sales_history_page()
    {
        var components = Path.Combine(FindRepoRoot(), "src", "Products", "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Maui", "Components");

        var shell = File.ReadAllText(Path.Combine(components, "Layout", "PosShell.razor"));
        Assert.Contains("href=\"/sales\"", shell, StringComparison.Ordinal);

        var deferred = File.ReadAllText(Path.Combine(components, "Pages", "DeferredPage.razor"));
        Assert.DoesNotContain("@page \"/sales\"", deferred, StringComparison.Ordinal);
    }

    [Fact]
    public void Sales_keys_are_localized_for_english_and_filipino()
    {
        var loc = Path.Combine(FindRepoRoot(), "src", "Products", "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Maui", "Localization");
        var en = File.ReadAllText(Path.Combine(loc, "PosResources.resx"));
        var fil = File.ReadAllText(Path.Combine(loc, "PosResources.fil-PH.resx"));

        foreach (var key in new[]
                 {
                     "Sales_Title",
                     "Sales_Subtitle",
                     "Sales_OfflineMessage",
                     "Sales_Status_Completed",
                     "Sales_Status_Voided",
                     "Sales_Payment_Cash",
                     "Sales_Payment_ManualGCash",
                     "Sales_Payment_Utang",
                     "Sales_Field_AmountTendered",
                     "Sales_Field_ChangeAmount",
                     "Sales_Field_GCashReference",
                     "Sales_GCash_ManualWarning",
                     "Sales_GCash_ConfirmReceived",
                     "Sales_Void",
                     "Sales_VoidMessage",
                     "Sales_VoidUtangMessage",
                     "Sales_Checkout_Title",
                     "Sales_Checkout_ConfirmMessage",
                     "Sales_Checkout_ConfirmUtangMessage",
                     "Sales_Checkout_UtangSection",
                     "Sales_Checkout_UtangWarning",
                     "Sales_Checkout_SelectCustomer",
                     "Sales_Checkout_CustomerSearch",
                     "Sales_Checkout_Outstanding",
                     "Sales_Checkout_AmountToUtang",
                     "Sales_Checkout_DueDateOptional",
                     "Sales_Checkout_CustomerRequired",
                     "Sales_Checkout_ZeroTotalUtang",
                     "Sales_Checkout_QuantityWhole",
                     "Sales_Checkout_OfflineMessage",
                     "Sales_Detail_LinkedCustomer",
                     "Sales_Detail_LinkedCredit",
                     "Sales_Detail_LinkedDueDate",
                     "Sales_Detail_OutstandingAfter",
                     "Returns_Title",
                     "Returns_StartReturn",
                     "Returns_HistorySection",
                     "Returns_OfflineMessage",
                     "Credit_LinkedSale",
                     "Credit_ReverseViaSaleVoid"
                 })
        {
            Assert.Contains($"name=\"{key}\"", en, StringComparison.Ordinal);
            Assert.Contains($"name=\"{key}\"", fil, StringComparison.Ordinal);
        }
    }

    private static string SalesPagesDirectory() => Path.Combine(
        FindRepoRoot(),
        "src",
        "Products",
        "PinoyBusinessPOS",
        "ExItS.PinoyBusinessPOS.Maui",
        "Components",
        "Pages",
        "Sales");

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
