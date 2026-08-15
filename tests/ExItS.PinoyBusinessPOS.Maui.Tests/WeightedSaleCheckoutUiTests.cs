namespace ExItS.PinoyBusinessPOS.Maui.Tests;

public sealed class WeightedSaleCheckoutUiTests
{
    [Fact]
    public void ByWeight_add_opens_weight_dialog_instead_of_adding_one_kilogram()
    {
        var checkout = ReadCheckout();
        Assert.Contains("SalesUiOptions.IsByWeight(product.SellingMode)", checkout, StringComparison.Ordinal);
        Assert.Contains("OpenWeightEntry", checkout, StringComparison.Ordinal);
        Assert.Contains("WeightEntryDialog", checkout, StringComparison.Ordinal);
        Assert.Contains("ApplyWeightedQuantity", checkout, StringComparison.Ordinal);
        Assert.Contains("const decimal addQuantity = 1m;", checkout, StringComparison.Ordinal);
        // ByWeight path must open dialog before any Cart.Add of a default 1 kg.
        Assert.Contains("OpenWeightEntry(product, existing > 0m ? existing : null, unit);", checkout, StringComparison.Ordinal);
        Assert.Contains("SellingUnitPickerDialog", checkout, StringComparison.Ordinal);
        Assert.Contains("SellingUnitEntryDialog", checkout, StringComparison.Ordinal);
        Assert.Contains("OpenSellingUnitEntry", checkout, StringComparison.Ordinal);
        Assert.Contains("OpenSellingUnitPicker", checkout, StringComparison.Ordinal);
        Assert.DoesNotContain("Cart.Add(product, 1m, preferred);", checkout, StringComparison.Ordinal);
    }

    [Fact]
    public void Weight_dialog_component_reuses_WeightQuantities_via_WeightEntry()
    {
        var dialog = File.ReadAllText(Path.Combine(
            MauiProject(), "Components", "Sales", "WeightEntryDialog.razor"));
        Assert.Contains("WeightEntry.TryNormalize", dialog, StringComparison.Ordinal);
        Assert.Contains("WeightEntry.UnitGram", dialog, StringComparison.Ordinal);
        Assert.Contains("WeightEntry.UnitKilogram", dialog, StringComparison.Ordinal);
        Assert.Contains("_sessionUnit = WeightEntry.UnitKilogram", dialog, StringComparison.Ordinal);
        Assert.Contains("Inline=\"true\"", dialog, StringComparison.Ordinal);
        Assert.Contains("PreviewAmount", dialog, StringComparison.Ordinal);
        Assert.Contains("Sales_Checkout_WeightLineTotal", dialog, StringComparison.Ordinal);
        Assert.Contains("ConvertDisplayedWeight", dialog, StringComparison.Ordinal);
        Assert.Contains("NumberInput", dialog, StringComparison.Ordinal);
        Assert.Contains("RadioGroup", dialog, StringComparison.Ordinal);
        Assert.Contains("EventCallback Removed", dialog, StringComparison.Ordinal);
        Assert.Contains("RemoveAsync", dialog, StringComparison.Ordinal);
        Assert.Contains("Sales_Checkout_Remove", dialog, StringComparison.Ordinal);
    }

    [Fact]
    public void ByWeight_in_cart_row_offers_remove_without_opening_cart_sheet()
    {
        var checkout = ReadCheckout();
        Assert.Contains("RemoveWeightedLine", checkout, StringComparison.Ordinal);
        Assert.Contains("pos-product-row__stepper--weight", checkout, StringComparison.Ordinal);
        Assert.Contains("IconName=\"trash\"", checkout, StringComparison.Ordinal);
        Assert.Contains("AriaLabel=\"@L[\"Sales_Checkout_Remove\"]\"", checkout, StringComparison.Ordinal);
        Assert.Contains("RoundMoney(product.SellingPrice * qty)", checkout, StringComparison.Ordinal);
        Assert.Contains("Removed=\"OnWeightRemoved\"", checkout, StringComparison.Ordinal);
        Assert.Contains("OnWeightRemoved", checkout, StringComparison.Ordinal);
    }

    [Fact]
    public void Cart_panel_shows_price_per_kg_and_edit_weight_for_ByWeight()
    {
        var panel = File.ReadAllText(Path.Combine(
            MauiProject(), "Components", "Sales", "SaleCartPanel.razor"));
        Assert.Contains("Sales_Checkout_PricePerKg", panel, StringComparison.Ordinal);
        Assert.Contains("Sales_Checkout_WeightEdit", panel, StringComparison.Ordinal);
        Assert.Contains("item.SellingUnitName", panel, StringComparison.Ordinal);
        Assert.Contains("EditWeight", panel, StringComparison.Ordinal);
    }

    private static string ReadCheckout() =>
        File.ReadAllText(Path.Combine(
            MauiProject(), "Components", "Pages", "Sales", "SaleCheckout.razor"));

    private static string MauiProject() =>
        Path.Combine(
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
