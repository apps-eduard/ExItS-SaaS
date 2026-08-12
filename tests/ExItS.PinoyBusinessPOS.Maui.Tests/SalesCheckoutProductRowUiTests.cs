namespace ExItS.PinoyBusinessPOS.Maui.Tests;

public sealed class SalesCheckoutProductRowUiTests
{
    [Fact]
    public void Product_row_is_tappable_and_drops_the_full_width_add_button()
    {
        var checkout = ReadCheckout();

        Assert.Contains("pos-product-row__hit", checkout, StringComparison.Ordinal);
        Assert.Contains("role=\"button\"", checkout, StringComparison.Ordinal);
        Assert.Contains("OnRowTap", checkout, StringComparison.Ordinal);
        Assert.Contains("OnRowKeyDown", checkout, StringComparison.Ordinal);
        Assert.Contains("aria-disabled=\"@tapDisabled\"", checkout, StringComparison.Ordinal);
        Assert.DoesNotContain("Sales_Checkout_AddToCart", checkout, StringComparison.Ordinal);
    }

    [Fact]
    public void Stepper_only_renders_for_cart_lines_and_stops_row_propagation()
    {
        var checkout = ReadCheckout();

        Assert.Contains("@if (inCart && !byWeight)", checkout, StringComparison.Ordinal);
        Assert.Contains("else if (inCart && byWeight)", checkout, StringComparison.Ordinal);
        Assert.Contains("pos-product-row__stepper", checkout, StringComparison.Ordinal);
        Assert.Contains("RemoveWeightedLine", checkout, StringComparison.Ordinal);
        Assert.Contains("IconName=\"trash\"", checkout, StringComparison.Ordinal);
        Assert.Contains("@onclick:stopPropagation=\"true\"", checkout, StringComparison.Ordinal);
        Assert.Contains("@onkeydown:stopPropagation=\"true\"", checkout, StringComparison.Ordinal);
        Assert.Contains("Compact=\"true\"", checkout, StringComparison.Ordinal);
    }

    [Fact]
    public void Out_of_stock_rows_are_not_tappable()
    {
        var checkout = ReadCheckout();

        Assert.Contains("var tapDisabled = blocked || outOfStock || (!byWeight && !canAddMore) || (byWeight && !inCart && !canAddMore);", checkout, StringComparison.Ordinal);
        Assert.Contains("pos-product-row--unavailable", checkout, StringComparison.Ordinal);
    }

    [Fact]
    public void Product_row_css_is_compact_with_a_single_line_layout()
    {
        var css = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "src",
            "Products",
            "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Maui",
            "wwwroot",
            "app.css"));

        Assert.Contains(".pos-product-row__hit", css, StringComparison.Ordinal);
        Assert.Contains("padding: 0.75rem 1rem;", css, StringComparison.Ordinal);
        Assert.Contains(".pos-product-row__info", css, StringComparison.Ordinal);
        Assert.Contains(".pos-product-row__side", css, StringComparison.Ordinal);
        Assert.Contains(".pos-product-row__stepper", css, StringComparison.Ordinal);
        Assert.DoesNotContain(".pos-product-row__qty", css, StringComparison.Ordinal);
    }

    [Fact]
    public void Design_system_offers_a_compact_stepper_with_a_44dp_tap_area()
    {
        var root = FindRepoRoot();
        var stepper = File.ReadAllText(Path.Combine(
            root, "src", "Shared", "ExItS.DesignSystem", "Components", "Forms", "QuantityStepper.razor"));
        var ds = File.ReadAllText(Path.Combine(
            root, "src", "Shared", "ExItS.DesignSystem", "wwwroot", "exits-design-system.css"));

        Assert.Contains("public bool Compact", stepper, StringComparison.Ordinal);
        Assert.Contains("exds-qty-stepper--compact", stepper, StringComparison.Ordinal);
        Assert.Contains(".exds-qty-stepper--compact", ds, StringComparison.Ordinal);
        Assert.Contains("min-height: 2.125rem;", ds, StringComparison.Ordinal);
        Assert.Contains("inset: -0.3125rem;", ds, StringComparison.Ordinal);
    }

    private static string ReadCheckout() => File.ReadAllText(Path.Combine(
        FindRepoRoot(),
        "src",
        "Products",
        "PinoyBusinessPOS",
        "ExItS.PinoyBusinessPOS.Maui",
        "Components",
        "Pages",
        "Sales",
        "SaleCheckout.razor"));

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
