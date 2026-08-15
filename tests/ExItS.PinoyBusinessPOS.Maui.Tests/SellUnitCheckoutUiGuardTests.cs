namespace ExItS.PinoyBusinessPOS.Maui.Tests;

public sealed class SellUnitCheckoutUiGuardTests
{
    [Fact]
    public void SaleCheckoutShowsSellUnitSelectorWhenMultipleUnits()
    {
        var checkout = ReadCheckout();
        Assert.Contains("SellingUnitEntryDialog", checkout, StringComparison.Ordinal);
        Assert.Contains("OpenSellingUnitEntry", checkout, StringComparison.Ordinal);
        Assert.Contains("sellUnits.Count > 1", checkout, StringComparison.Ordinal);
        Assert.Contains("Sales_SellAs", checkout, StringComparison.Ordinal);
    }

    [Fact]
    public void SaleCheckoutHidesSelectorWhenSingleUnit()
    {
        var checkout = ReadCheckout();
        // Classic 1:1 path adds without SellingUnitId / entry dialog.
        Assert.Contains("Cart.Add(product, addQuantity);", checkout, StringComparison.Ordinal);
        Assert.Contains("const decimal addQuantity = 1m;", checkout, StringComparison.Ordinal);
    }

    [Fact]
    public void SaleCheckoutShowsUnitSpecificPrice_AndConversionForNonBaseUnit()
    {
        var entry = ReadEntryDialog();
        Assert.Contains("Sales_SellAs", entry, StringComparison.Ordinal);
        Assert.Contains("Sales_Checkout_UnitPrice", entry, StringComparison.Ordinal);
        Assert.Contains("Sales_Checkout_StockUsed", entry, StringComparison.Ordinal);
        Assert.Contains("Sales_Checkout_UnitEqualsBase", entry, StringComparison.Ordinal);
        Assert.Contains("SelectedUnit.MultiplierToBase != 1m", entry, StringComparison.Ordinal);
        Assert.Contains("unit.SellingPrice ?? Product.SellingPrice", entry, StringComparison.Ordinal);
    }

    [Fact]
    public void SaleCheckoutShowsBaseStockUsed_AndUsesSelectedUnitQuantity()
    {
        var entry = ReadEntryDialog();
        Assert.Contains("BaseQuantityNeeded", entry, StringComparison.Ordinal);
        Assert.Contains("OtherBaseQuantityInCart", entry, StringComparison.Ordinal);
        Assert.Contains("Sales_Checkout_InsufficientStockForUnit", entry, StringComparison.Ordinal);
        Assert.Contains("Confirmed.InvokeAsync((SelectedUnit, _quantity))", entry, StringComparison.Ordinal);
    }

    [Fact]
    public void SaleCheckoutCartShowsSelectedUnit_AndIndependentUnitPriceGuard()
    {
        var panel = File.ReadAllText(Path.Combine(MauiProject(), "Components", "Sales", "SaleCartPanel.razor"));
        Assert.Contains("item.SellingUnitName", panel, StringComparison.Ordinal);
        Assert.Contains("item.EnteredQuantity", panel, StringComparison.Ordinal);
        Assert.Contains("Sales_Checkout_CartStockFromBase", panel, StringComparison.Ordinal);
        Assert.Contains("SetEnteredQuantity", panel, StringComparison.Ordinal);

        var en = File.ReadAllText(Path.Combine(MauiProject(), "Localization", "PosResources.resx"));
        Assert.Contains("Each selling unit can have its own price", en, StringComparison.Ordinal);
        Assert.Contains("name=\"Sales_SellAs\"", en, StringComparison.Ordinal);
        Assert.Contains("<value>Sell as</value>", en, StringComparison.Ordinal);
    }

    [Fact]
    public void CatalogFormExplainsIndependentSellUnitPrices()
    {
        var form = File.ReadAllText(Path.Combine(
            MauiProject(), "Components", "Pages", "Catalog", "CatalogProductForm.razor"));
        Assert.Contains("Catalog_SellingOptionsHint", form, StringComparison.Ordinal);
        var en = File.ReadAllText(Path.Combine(MauiProject(), "Localization", "PosResources.resx"));
        Assert.Contains("Each selling unit can have its own price", en, StringComparison.Ordinal);
    }

    private static string ReadCheckout() =>
        File.ReadAllText(Path.Combine(MauiProject(), "Components", "Pages", "Sales", "SaleCheckout.razor"));

    private static string ReadEntryDialog() =>
        File.ReadAllText(Path.Combine(MauiProject(), "Components", "Sales", "SellingUnitEntryDialog.razor"));

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
