namespace ExItS.PinoyBusinessPOS.Maui.Tests;

public sealed class SalesCashierPageGuardTests
{
    [Fact]
    public void SaleCheckout_covers_cashier_selling_workflow_surfaces()
    {
        var checkout = File.ReadAllText(Path.Combine(SalesPagesDirectory(), "SaleCheckout.razor"));
        Assert.Contains("@page \"/sales/new\"", checkout, StringComparison.Ordinal);
        Assert.Contains("UtangCapability.CreateSale", checkout, StringComparison.Ordinal);
        Assert.Contains("GetCurrentAsync", checkout, StringComparison.Ordinal);
        Assert.Contains("Sales_Checkout_CategoryFilter", checkout, StringComparison.Ordinal);
        Assert.Contains("Sales_Checkout_BrowseSection", checkout, StringComparison.Ordinal);
        Assert.Contains("ListProductsAsync", checkout, StringComparison.Ordinal);
        Assert.Contains("LookupByBarcodeAsync", checkout, StringComparison.Ordinal);
        Assert.Contains("LookupBySkuAsync", checkout, StringComparison.Ordinal);
        Assert.Contains("OnBrowseProductTap", checkout, StringComparison.Ordinal);
        Assert.Contains("pos-product-row", checkout, StringComparison.Ordinal);
        Assert.Contains("pos-sell-sticky-bar", checkout, StringComparison.Ordinal);
        Assert.Contains("pos-sell-payment", checkout, StringComparison.Ordinal);
        Assert.Contains("_paymentMethodExpanded", checkout, StringComparison.Ordinal);
        Assert.Contains("pos-sell-payment-method__toggle", checkout, StringComparison.Ordinal);
        Assert.Contains("pos-sell-shift-cta", checkout, StringComparison.Ordinal);
        Assert.Contains("Sales_Checkout_Reset", checkout, StringComparison.Ordinal);
        Assert.Contains("NavigationLock", checkout, StringComparison.Ordinal);
        Assert.DoesNotContain("pos-product-tile__placeholder", checkout, StringComparison.Ordinal);
        Assert.Contains("CheckoutPaymentMethodLabel", checkout, StringComparison.Ordinal);
        Assert.Contains("Sales_Checkout_AddCustomerShort", checkout, StringComparison.Ordinal);
        Assert.Contains("Sales_Checkout_ExItsIdShort", checkout, StringComparison.Ordinal);
        Assert.Contains("Sales_Checkout_ChooseCustomer", checkout, StringComparison.Ordinal);
        Assert.Contains("NavigatePreservingCart", checkout, StringComparison.Ordinal);
        Assert.Contains("_customerPickerOpen", checkout, StringComparison.Ordinal);
        Assert.Contains("pos-sell-customer__actions", checkout, StringComparison.Ordinal);
        Assert.Contains("IsCartPreservingSideTrip", checkout, StringComparison.Ordinal);
        Assert.Contains("TryApplyCheckoutResumeAsync", checkout, StringComparison.Ordinal);
        Assert.Contains("Sales_GCash_ReferenceRequired", checkout, StringComparison.Ordinal);
        Assert.Contains("Sales_Checkout_CustomerSearchEmpty", checkout, StringComparison.Ordinal);
        Assert.Contains("c.Notes", File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "src",
            "Products",
            "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Infrastructure",
            "Persistence",
            "Repositories",
            "POSCustomerRepository.cs")), StringComparison.Ordinal);
        Assert.Contains("ParseCategoryFilter", checkout, StringComparison.Ordinal);

        var uiOptions = File.ReadAllText(Path.Combine(SalesPagesDirectory(), "SalesUiOptions.cs"));
        var checkoutCodesStart = uiOptions.IndexOf("CheckoutPaymentMethodCodes", StringComparison.Ordinal);
        Assert.True(checkoutCodesStart >= 0);
        var checkoutCodesBlock = uiOptions.Substring(checkoutCodesStart, Math.Min(400, uiOptions.Length - checkoutCodesStart));
        Assert.Contains("CashPaymentMethod", checkoutCodesBlock, StringComparison.Ordinal);
        Assert.Contains("ManualGCashPaymentMethod", checkoutCodesBlock, StringComparison.Ordinal);
        Assert.Contains("UtangPaymentMethod", checkoutCodesBlock, StringComparison.Ordinal);
        Assert.DoesNotContain("CardPaymentMethod", checkoutCodesBlock, StringComparison.Ordinal);
        Assert.DoesNotContain("PosSaleOptions.GCashPaymentMethod", checkoutCodesBlock, StringComparison.Ordinal);
        Assert.Contains("Sales_Field_AmountTendered", checkout, StringComparison.Ordinal);
        Assert.Contains("ChangePreview", checkout, StringComparison.Ordinal);
        Assert.Contains("/receipt", checkout, StringComparison.Ordinal);
        Assert.Contains("IPosCatalogClient", checkout, StringComparison.Ordinal);
        Assert.DoesNotContain("IMerchantCatalogDiscoveryClient", checkout, StringComparison.Ordinal);
        Assert.DoesNotContain("IPosCatalogImportClient", checkout, StringComparison.Ordinal);
        Assert.DoesNotContain("/catalog/global", checkout, StringComparison.Ordinal);
        Assert.DoesNotContain("/catalog/import", checkout, StringComparison.Ordinal);
        Assert.DoesNotContain("UtangCapability.ManageCatalog", checkout, StringComparison.Ordinal);
    }

    [Fact]
    public void SaleReceipt_and_detail_support_receipt_history_navigation()
    {
        var receipt = File.ReadAllText(Path.Combine(SalesPagesDirectory(), "SaleReceipt.razor"));
        Assert.Contains("@page \"/sales/{SaleId:guid}/receipt\"", receipt, StringComparison.Ordinal);
        Assert.Contains("StoreHeaderBack Href=\"/sales\"", receipt, StringComparison.Ordinal);
        Assert.Contains("UtangCapability.ViewSales", receipt, StringComparison.Ordinal);
        Assert.Contains("GoNextSale", receipt, StringComparison.Ordinal);
        Assert.DoesNotContain("Sales_BackToList", receipt, StringComparison.Ordinal);

        var detail = File.ReadAllText(Path.Combine(SalesPagesDirectory(), "SaleDetail.razor"));
        Assert.Contains("ViewGenerateReceipt", detail, StringComparison.Ordinal);
        Assert.Contains("/receipt", detail, StringComparison.Ordinal);
        Assert.Contains("pos-sale-detail__summary", detail, StringComparison.Ordinal);
        Assert.Contains("StoreHeaderBack Href=\"/sales\"", detail, StringComparison.Ordinal);
        Assert.DoesNotContain("Sales_BackToList", detail, StringComparison.Ordinal);

        var list = File.ReadAllText(Path.Combine(SalesPagesDirectory(), "SalesList.razor"));
        Assert.Contains("@page \"/sales\"", list, StringComparison.Ordinal);
        Assert.Contains("UtangCapability.ViewSales", list, StringComparison.Ordinal);
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
