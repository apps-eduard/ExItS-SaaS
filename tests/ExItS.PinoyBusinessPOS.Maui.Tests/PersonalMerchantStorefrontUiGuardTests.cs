namespace ExItS.PinoyBusinessPOS.Maui.Tests;

public sealed class PersonalMerchantStorefrontUiGuardTests
{
    [Fact]
    public void Shop_rows_match_connected_po_stepper()
    {
        var shop = File.ReadAllText(Path.Combine(MauiProject(), "Components", "Pages", "Personal", "PersonalMerchantShop.razor"));
        Assert.Contains("pos-purchasing-create__hit pos-purchasing-create__hit--ready", shop, StringComparison.Ordinal);
        Assert.Contains("pos-purchasing-create__hit-add", shop, StringComparison.Ordinal);
        Assert.Contains("Purchasing_AddedQty", shop, StringComparison.Ordinal);
        Assert.Contains("@if (added)", shop, StringComparison.Ordinal);
        Assert.DoesNotContain("pos-personal-shop__qty-value", shop, StringComparison.Ordinal);
        Assert.DoesNotContain("disabled=\"@(qty <= 0)\"", shop, StringComparison.Ordinal);
        Assert.Contains("disabled=\"@(!product.IsAvailable)\"", shop, StringComparison.Ordinal);
        Assert.Contains("Cart.Increment", shop, StringComparison.Ordinal);
        Assert.Contains("Cart.Decrement", shop, StringComparison.Ordinal);
    }

    [Fact]
    public void Review_uses_sales_summary_and_manual_payment_toggle()
    {
        var review = File.ReadAllText(Path.Combine(MauiProject(), "Components", "Pages", "Personal", "PersonalMerchantShopReview.razor"));
        Assert.Contains("pos-sell-payment__summary", review, StringComparison.Ordinal);
        Assert.Contains("pos-sell-payment__line-name", review, StringComparison.Ordinal);
        Assert.Contains("pos-sell-payment__line-qty", review, StringComparison.Ordinal);
        Assert.Contains("pos-sell-payment__line-total", review, StringComparison.Ordinal);
        Assert.Contains("pos-sell-payment__total", review, StringComparison.Ordinal);
        Assert.Contains("pos-sell-payment-method__toggle", review, StringComparison.Ordinal);
        Assert.Contains("pos-payment-method-tile", review, StringComparison.Ordinal);
        Assert.Contains("PersonalMerchantCheckoutUi.PaymentMethodCodes", review, StringComparison.Ordinal);
        Assert.Contains("PaymentMethod", review, StringComparison.Ordinal);
        Assert.DoesNotContain("Personal_ShopRemove", review, StringComparison.Ordinal);
        Assert.DoesNotContain("SalePaymentMethod.Card", review, StringComparison.Ordinal);
        Assert.Contains("ShowBranchSelector", review, StringComparison.Ordinal);
        Assert.Contains("Personal_ShopFulfillmentUnavailable", review, StringComparison.Ordinal);
    }

    [Fact]
    public void Order_details_show_payment_method_and_unpaid_status()
    {
        var personal = File.ReadAllText(Path.Combine(MauiProject(), "Components", "Pages", "Personal", "PersonalOrderDetail.razor"));
        var seller = File.ReadAllText(Path.Combine(MauiProject(), "Components", "Pages", "Orders", "SellerOrderDetail.razor"));
        Assert.Contains("Orders_PaymentMethod", personal, StringComparison.Ordinal);
        Assert.Contains("Orders_PaymentStatus", personal, StringComparison.Ordinal);
        Assert.Contains("PaymentMethodLabel", personal, StringComparison.Ordinal);
        Assert.Contains("Orders_PaymentMethod", seller, StringComparison.Ordinal);
        Assert.Contains("Orders_PaymentStatus", seller, StringComparison.Ordinal);
    }

    [Fact]
    public void Sales_checkout_and_connected_po_payment_remain_unchanged()
    {
        var checkout = File.ReadAllText(Path.Combine(MauiProject(), "Components", "Pages", "Sales", "SaleCheckout.razor"));
        var purchasing = File.ReadAllText(Path.Combine(MauiProject(), "Components", "Pages", "Purchasing", "PurchasingCreate.razor"));
        Assert.Contains("SalesUiOptions.CheckoutPaymentMethodCodes", checkout, StringComparison.Ordinal);
        Assert.Contains("_electronicPaymentOpen", checkout, StringComparison.Ordinal);
        Assert.Contains("(\"Cash\", \"Purchasing_Payment_Cash\")", purchasing, StringComparison.Ordinal);
        Assert.Contains("(\"ManualGCash\", \"Purchasing_Payment_GCash\")", purchasing, StringComparison.Ordinal);
        Assert.Contains("(\"Utang\", \"Purchasing_Payment_Utang\")", purchasing, StringComparison.Ordinal);
        var connected = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "src",
            "Products",
            "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Domain",
            "ConnectedSuppliers",
            "ConnectedSuppliers.cs"));
        Assert.Contains("enum ConnectedPoPaymentTerm", connected, StringComparison.Ordinal);
        Assert.Contains("Cash = 0", connected, StringComparison.Ordinal);
        Assert.Contains("ManualGCash = 1", connected, StringComparison.Ordinal);
        Assert.Contains("Utang = 2", connected, StringComparison.Ordinal);
    }

    [Fact]
    public void Localization_includes_manual_customer_order_payment_keys()
    {
        var localization = Path.Combine(MauiProject(), "Localization");
        var en = File.ReadAllText(Path.Combine(localization, "PosResources.resx"));
        var fil = File.ReadAllText(Path.Combine(localization, "PosResources.fil-PH.resx"));
        foreach (var key in new[]
                 {
                     "Personal_ShopPayment_Cash",
                     "Personal_ShopPayment_GCash",
                     "Personal_ShopPayment_Utang",
                     "Personal_ShopPayment_GCashHelp",
                     "Personal_ShopPayment_UtangHelp",
                     "Personal_ShopFulfillmentUnavailable",
                     "Orders_PaymentMethod",
                     "Orders_PaymentStatus",
                     "Orders_Payment_Unpaid"
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
