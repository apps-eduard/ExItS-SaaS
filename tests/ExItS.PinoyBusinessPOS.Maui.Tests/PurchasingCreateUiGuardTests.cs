namespace ExItS.PinoyBusinessPOS.Maui.Tests;

public sealed class PurchasingCreateUiGuardTests
{
    [Fact]
    public void Create_page_has_search_category_filter_and_product_list()
    {
        var create = CreatePage();

        Assert.Contains("Purchasing_SearchProducts", create, StringComparison.Ordinal);
        Assert.Contains("pos-purchasing-create__search-input", create, StringComparison.Ordinal);
        Assert.Contains("Purchasing_CategoryFilter", create, StringComparison.Ordinal);
        Assert.Contains("Purchasing_AllCategories", create, StringComparison.Ordinal);
        Assert.Contains("pos-category-chip", create, StringComparison.Ordinal);
        Assert.Contains("pos-sell-categories", create, StringComparison.Ordinal);
        Assert.Contains("pos-purchasing-create__product-list", create, StringComparison.Ordinal);
        Assert.Contains("PurchaseOrderCreateUi.FilterEligibleProducts", create, StringComparison.Ordinal);
        Assert.Contains("PurchaseOrderCreateUi.RelevantCategories", create, StringComparison.Ordinal);
        Assert.Contains("ListCategoriesAsync", create, StringComparison.Ordinal);
        Assert.DoesNotContain("ProductOptions", create, StringComparison.Ordinal);
    }

    [Fact]
    public void Create_page_added_lines_show_qty_cost_line_total_edit_and_delete()
    {
        var create = CreatePage();

        Assert.Contains("Purchasing_LineQtyPrefix", create, StringComparison.Ordinal);
        Assert.Contains("Purchasing_LineTotalLabel", create, StringComparison.Ordinal);
        Assert.Contains("PurchaseOrderCreateUi.LineTotal", create, StringComparison.Ordinal);
        Assert.Contains("MoneyDisplay Amount=\"@line.UnitPurchaseCost\"", create, StringComparison.Ordinal);
        Assert.Contains("MoneyDisplay Amount=\"@lineTotal\"", create, StringComparison.Ordinal);
        Assert.Contains("IconGlyphs.Get(\"edit\")", create, StringComparison.Ordinal);
        Assert.Contains("IconGlyphs.Get(\"trash\")", create, StringComparison.Ordinal);
        Assert.Contains("Purchasing_EditLine", create, StringComparison.Ordinal);
        Assert.Contains("Purchasing_DeleteLine", create, StringComparison.Ordinal);
        Assert.Contains("pos-po-line__action--danger", create, StringComparison.Ordinal);
        Assert.DoesNotContain("IconGlyphs.Get(\"close\")", create, StringComparison.Ordinal);
        Assert.DoesNotContain("pos-po-line__remove", create, StringComparison.Ordinal);
    }

    [Fact]
    public void Create_page_edit_prefills_and_saves_same_line_without_duplicate()
    {
        var create = CreatePage();

        Assert.Contains("BeginEditLine", create, StringComparison.Ordinal);
        Assert.Contains("_draftQtyText = FormatQty(line.OrderedQty)", create, StringComparison.Ordinal);
        Assert.Contains("_draftUnitCost = line.UnitPurchaseCost", create, StringComparison.Ordinal);
        Assert.Contains("Purchasing_SaveChanges", create, StringComparison.Ordinal);
        Assert.Contains("SaveLineChanges", create, StringComparison.Ordinal);
        Assert.Contains("replaceExisting: true", create, StringComparison.Ordinal);
        Assert.Contains("PurchaseOrderCreateUi.UpsertLine", create, StringComparison.Ordinal);
        Assert.Contains("productId != _editingProductId.Value", create, StringComparison.Ordinal);
    }

    [Fact]
    public void Create_page_delete_confirms_and_recalculates_totals()
    {
        var create = CreatePage();

        Assert.Contains("Purchasing_DeleteLineMessage", create, StringComparison.Ordinal);
        Assert.Contains("Remove this item from the purchase order?", File.ReadAllText(Path.Combine(LocalizationDirectory(), "PosResources.resx")), StringComparison.Ordinal);
        Assert.Contains("ConfirmDeleteLine", create, StringComparison.Ordinal);
        Assert.Contains("PurchaseOrderCreateUi.RemoveLine", create, StringComparison.Ordinal);
        Assert.Contains("PurchaseOrderCreateUi.OrderTotal", create, StringComparison.Ordinal);
        Assert.Contains("Purchasing_LinesCount", create, StringComparison.Ordinal);
    }

    [Fact]
    public void Create_page_keeps_existing_validation_and_create_request_shape()
    {
        var create = CreatePage();

        Assert.Contains("Purchasing_InvalidOrderedQty", create, StringComparison.Ordinal);
        Assert.Contains("Purchasing_UnitCostRequired", create, StringComparison.Ordinal);
        Assert.Contains("PurchaseOrderLine.MaxUnitPurchaseCost", create, StringComparison.Ordinal);
        Assert.Contains("CreatePurchaseOrderRequest", create, StringComparison.Ordinal);
        Assert.Contains("CreatePurchaseOrderLineRequest(l.ProductId, l.OrderedQty, l.UnitPurchaseCost)", create, StringComparison.Ordinal);
        Assert.Contains("CreateAsync", create, StringComparison.Ordinal);
        Assert.DoesNotContain("IOfflineOperationQueue", create, StringComparison.Ordinal);
    }

    [Fact]
    public void Create_page_keys_and_styles_exist()
    {
        var en = File.ReadAllText(Path.Combine(LocalizationDirectory(), "PosResources.resx"));
        var fil = File.ReadAllText(Path.Combine(LocalizationDirectory(), "PosResources.fil-PH.resx"));
        var css = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "src",
            "Products",
            "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Maui",
            "wwwroot",
            "app.css"));

        foreach (var key in new[]
                 {
                     "Purchasing_SearchProducts",
                     "Purchasing_CategoryFilter",
                     "Purchasing_AllCategories",
                     "Purchasing_Uncategorized",
                     "Purchasing_NoProductsMatch",
                     "Purchasing_EditLine",
                     "Purchasing_DeleteLine",
                     "Purchasing_SaveChanges",
                     "Purchasing_CancelEdit",
                     "Purchasing_LineQtyPrefix",
                     "Purchasing_LineTotalLabel",
                     "Purchasing_DeleteLineTitle",
                     "Purchasing_DeleteLineMessage"
                 })
        {
            Assert.Contains($"name=\"{key}\"", en, StringComparison.Ordinal);
            Assert.Contains($"name=\"{key}\"", fil, StringComparison.Ordinal);
        }

        foreach (var selector in new[]
                 {
                     ".pos-purchasing-create__search",
                     ".pos-purchasing-create__product-list",
                     ".pos-po-line__actions",
                     ".pos-po-line__action--danger",
                     ".pos-po-line__total"
                 })
        {
            Assert.Contains(selector, css, StringComparison.Ordinal);
        }
    }

    private static string CreatePage() => File.ReadAllText(Path.Combine(
        FindRepoRoot(),
        "src",
        "Products",
        "PinoyBusinessPOS",
        "ExItS.PinoyBusinessPOS.Maui",
        "Components",
        "Pages",
        "Purchasing",
        "PurchasingCreate.razor"));

    private static string LocalizationDirectory() => Path.Combine(
        FindRepoRoot(),
        "src",
        "Products",
        "PinoyBusinessPOS",
        "ExItS.PinoyBusinessPOS.Maui",
        "Localization");

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
