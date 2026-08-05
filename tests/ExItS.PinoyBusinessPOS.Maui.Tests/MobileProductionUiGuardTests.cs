namespace ExItS.PinoyBusinessPOS.Maui.Tests;

/// <summary>
/// Architecture guards for the production mobile visual system (tokens, sell floor, touch, a11y).
/// </summary>
public sealed class MobileProductionUiGuardTests
{
    [Fact]
    public void App_css_defines_pos_semantic_aliases_and_sell_floor_layouts()
    {
        var css = ReadMauiCss();

        foreach (var token in new[]
                 {
                     "--pos-surface-page", "--pos-surface-panel", "--pos-surface-raised",
                     "--pos-border-subtle", "--pos-text-primary", "--pos-text-secondary",
                     "--pos-accent", "--pos-danger", "--pos-success", "--pos-total-font",
                     "--pos-touch-target", "--pos-category-width", "--pos-cart-width"
                 })
        {
            Assert.Contains(token, css, StringComparison.Ordinal);
        }

        Assert.Contains(".pos-sell-floor", css, StringComparison.Ordinal);
        Assert.Contains(".pos-sell-categories", css, StringComparison.Ordinal);
        Assert.Contains(".pos-sell-products", css, StringComparison.Ordinal);
        Assert.Contains(".pos-sell-cart", css, StringComparison.Ordinal);
        Assert.Contains(".pos-sell-sticky-bar", css, StringComparison.Ordinal);
        Assert.Contains(".pos-sell-cart-fab", css, StringComparison.Ordinal);
        Assert.Contains(".pos-category-chip", css, StringComparison.Ordinal);
        Assert.Contains(".pos-product-row", css, StringComparison.Ordinal);
        Assert.Contains(".pos-product-tile", css, StringComparison.Ordinal);
        Assert.Contains(".pos-sell-payment", css, StringComparison.Ordinal);
        Assert.Contains(".pos-cart-line", css, StringComparison.Ordinal);
        Assert.Contains("min-width: 900px", css, StringComparison.Ordinal);
        Assert.Contains("orientation: landscape", css, StringComparison.Ordinal);
        Assert.Contains("safe-area-inset-bottom", css, StringComparison.Ordinal);
        Assert.Contains("var(--exits-touch-target-min)", css, StringComparison.Ordinal);
        Assert.Contains("font-variant-numeric: tabular-nums", css, StringComparison.Ordinal);
        Assert.DoesNotContain("bootstrap", css, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("tailwind", css, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Sell_floor_preserves_cart_across_category_filter_and_avoids_api_on_quantity()
    {
        var checkout = File.ReadAllText(Path.Combine(MauiProject(), "Components", "Pages", "Sales", "SaleCheckout.razor"));
        var cart = File.ReadAllText(Path.Combine(MauiProject(), "Services", "SaleCartService.cs"));
        var panel = File.ReadAllText(Path.Combine(MauiProject(), "Components", "Sales", "SaleCartPanel.razor"));

        Assert.Contains("OnCategoryFilterChanged", checkout, StringComparison.Ordinal);
        Assert.Contains("LoadBrowseProductsAsync", checkout, StringComparison.Ordinal);
        var categoryMethodStart = checkout.IndexOf("private async Task OnCategoryFilterChanged", StringComparison.Ordinal);
        Assert.True(categoryMethodStart >= 0);
        var categoryMethodSlice = checkout.Substring(categoryMethodStart, Math.Min(280, checkout.Length - categoryMethodStart));
        Assert.DoesNotContain("Cart.Clear", categoryMethodSlice, StringComparison.Ordinal);
        Assert.Contains("LoadBrowseProductsAsync", categoryMethodSlice, StringComparison.Ordinal);

        Assert.Contains("QuantityStepper", checkout, StringComparison.Ordinal);
        Assert.Contains("SetQuantity", checkout, StringComparison.Ordinal);
        Assert.Contains("GetQuantity", cart, StringComparison.Ordinal);
        Assert.Contains("Changed?.Invoke()", cart, StringComparison.Ordinal);

        // Quantity changes are in-memory only — no HTTP clients in cart service / panel.
        Assert.DoesNotContain("HttpClient", cart, StringComparison.Ordinal);
        Assert.DoesNotContain("IPosCatalogClient", panel, StringComparison.Ordinal);
        Assert.DoesNotContain("IPosSaleClient", panel, StringComparison.Ordinal);
        Assert.Contains("DebouncedSearchAsync", checkout, StringComparison.Ordinal);
        Assert.Contains("_searchGeneration", checkout, StringComparison.Ordinal);
        Assert.Contains("@key=\"product.ProductId\"", checkout, StringComparison.Ordinal);
    }

    [Fact]
    public void Sell_floor_exposes_accessibility_labels_and_states()
    {
        var checkout = File.ReadAllText(Path.Combine(MauiProject(), "Components", "Pages", "Sales", "SaleCheckout.razor"));
        Assert.Contains("aria-pressed", checkout, StringComparison.Ordinal);
        Assert.Contains("aria-label", checkout, StringComparison.Ordinal);
        Assert.Contains("aria-expanded", checkout, StringComparison.Ordinal);
        Assert.Contains("Sales_Checkout_DecreaseQty", checkout, StringComparison.Ordinal);
        Assert.Contains("Sales_Checkout_IncreaseQty", checkout, StringComparison.Ordinal);
        Assert.Contains("Sales_Checkout_ProductInCartAria", checkout, StringComparison.Ordinal);
        Assert.Contains("<Skeleton", checkout, StringComparison.Ordinal);
        Assert.Contains("EmptyState", checkout, StringComparison.Ordinal);
        Assert.Contains("pos-product-row--in-cart", checkout, StringComparison.Ordinal);
        Assert.Contains("pos-sell-sticky-bar", checkout, StringComparison.Ordinal);
    }

    [Fact]
    public void Design_system_touch_target_is_48dp_and_quantity_stepper_exists()
    {
        var ds = File.ReadAllText(Path.Combine(FindRepoRoot(), "src", "Shared", "ExItS.DesignSystem",
            "wwwroot", "exits-design-system.css"));
        Assert.Contains("--exits-touch-target-min: 3rem", ds, StringComparison.Ordinal);
        Assert.Contains(".exds-qty-stepper", ds, StringComparison.Ordinal);
        Assert.Contains("IBM Plex Sans", ds, StringComparison.Ordinal);

        Assert.True(File.Exists(Path.Combine(FindRepoRoot(), "src", "Shared", "ExItS.DesignSystem",
            "Components", "Forms", "QuantityStepper.razor")));
    }

    private static string ReadMauiCss() =>
        File.ReadAllText(Path.Combine(MauiProject(), "wwwroot", "app.css"));

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
