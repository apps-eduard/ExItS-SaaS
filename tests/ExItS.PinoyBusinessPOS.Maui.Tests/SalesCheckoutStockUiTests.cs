namespace ExItS.PinoyBusinessPOS.Maui.Tests;

public sealed class SalesCheckoutStockUiTests
{
    [Fact]
    public void SalesUiOptions_distinguishes_tracked_and_untracked_stock_labels()
    {
        var options = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "src",
            "Products",
            "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Maui",
            "Components",
            "Pages",
            "Sales",
            "SalesUiOptions.cs"));

        Assert.Contains("Sales_Checkout_StockNotTracked", options, StringComparison.Ordinal);
        Assert.Contains("Sales_Checkout_OutOfStock", options, StringComparison.Ordinal);
        Assert.Contains("Sales_Checkout_LowStock", options, StringComparison.Ordinal);
        Assert.Contains("Sales_Checkout_InStock", options, StringComparison.Ordinal);
        Assert.Contains("CanAcceptQuantity", options, StringComparison.Ordinal);
        Assert.Contains("!isTracked || requestedQuantity <= onHandQuantity", options, StringComparison.Ordinal);
        Assert.Contains("IsOutOfStock", options, StringComparison.Ordinal);
    }

    [Fact]
    public void SaleCheckout_uses_inventory_IsTracked_policy_and_sticky_cart_bar()
    {
        var checkout = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "src",
            "Products",
            "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Maui",
            "Components",
            "Pages",
            "Sales",
            "SaleCheckout.razor"));

        Assert.Contains("product.IsTracked", checkout, StringComparison.Ordinal);
        Assert.Contains("SalesUiOptions.CanAcceptQuantity", checkout, StringComparison.Ordinal);
        Assert.Contains("SalesUiOptions.IsOutOfStock", checkout, StringComparison.Ordinal);
        Assert.Contains("SalesUiOptions.ProductRowMeta", checkout, StringComparison.Ordinal);
        Assert.Contains("pos-sell-sticky-bar", checkout, StringComparison.Ordinal);
        Assert.Contains("Sales_Checkout_SellTitle", checkout, StringComparison.Ordinal);
        Assert.Contains("TryExactLookupAsync", checkout, StringComparison.Ordinal);
        Assert.Contains("DebouncedSearchAsync", checkout, StringComparison.Ordinal);
        Assert.DoesNotContain("pos-sell-cart-fab", checkout, StringComparison.Ordinal);
        Assert.DoesNotContain("Sales_BackToList", checkout, StringComparison.Ordinal);
        Assert.DoesNotContain("SellingMode_Exit", checkout, StringComparison.Ordinal);
        Assert.DoesNotContain("â€”", checkout, StringComparison.Ordinal);
        Assert.DoesNotContain("StockOnHand", checkout, StringComparison.Ordinal);
        Assert.DoesNotContain("QuantityOnHand", checkout, StringComparison.Ordinal);
    }

    [Fact]
    public void Catalog_product_dto_exposes_inventory_IsTracked_not_a_competing_flag()
    {
        var dto = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "src",
            "Products",
            "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Application",
            "Catalog",
            "CatalogClientDtos.cs"));
        var query = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "src",
            "Products",
            "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Application",
            "Catalog",
            "CatalogProductUseCases.cs"));

        Assert.Contains("bool IsTracked = false", dto, StringComparison.Ordinal);
        Assert.Contains("OnHandQuantity", dto, StringComparison.Ordinal);
        Assert.Contains("StockStatus", dto, StringComparison.Ordinal);
        Assert.Contains("account?.IsTracked", query, StringComparison.Ordinal);
        Assert.Contains("IInventoryRepository", query, StringComparison.Ordinal);
        Assert.DoesNotContain("TracksInventory", dto, StringComparison.Ordinal);
        Assert.DoesNotContain("IsStockMonitored", dto, StringComparison.Ordinal);
    }

    [Fact]
    public void Localization_has_stock_mode_keys_without_mojibake()
    {
        var loc = Path.Combine(
            FindRepoRoot(),
            "src",
            "Products",
            "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Maui",
            "Localization");
        var en = File.ReadAllText(Path.Combine(loc, "PosResources.resx"));
        var fil = File.ReadAllText(Path.Combine(loc, "PosResources.fil-PH.resx"));

        foreach (var key in new[]
                 {
                     "Sales_Checkout_SellTitle",
                     "Sales_Checkout_LookupPlaceholder",
                     "Sales_Checkout_StockNotTracked",
                     "Sales_Checkout_OutOfStock",
                     "Sales_Checkout_LowStock",
                     "Sales_Checkout_InsufficientStock",
                     "SellingMode_Banner"
                 })
        {
            Assert.Contains($"name=\"{key}\"", en, StringComparison.Ordinal);
            Assert.Contains($"name=\"{key}\"", fil, StringComparison.Ordinal);
        }

        Assert.DoesNotContain("â€”", en, StringComparison.Ordinal);
        Assert.DoesNotContain("â€”", fil, StringComparison.Ordinal);
        Assert.Contains("Selling mode — role unchanged", en, StringComparison.Ordinal);
    }

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
