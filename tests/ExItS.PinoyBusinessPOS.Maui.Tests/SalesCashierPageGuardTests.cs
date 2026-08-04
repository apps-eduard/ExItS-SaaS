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
        Assert.Contains("pos-product-tile", checkout, StringComparison.Ordinal);
        Assert.Contains("pos-product-tile__placeholder", checkout, StringComparison.Ordinal);
        Assert.Contains("ParseCategoryFilter", checkout, StringComparison.Ordinal);
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
        Assert.Contains("UtangCapability.ViewSales", receipt, StringComparison.Ordinal);
        Assert.Contains("GoNextSale", receipt, StringComparison.Ordinal);

        var detail = File.ReadAllText(Path.Combine(SalesPagesDirectory(), "SaleDetail.razor"));
        Assert.Contains("ViewGenerateReceipt", detail, StringComparison.Ordinal);
        Assert.Contains("/receipt", detail, StringComparison.Ordinal);

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
