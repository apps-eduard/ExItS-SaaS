namespace ExItS.PinoyBusinessPOS.Maui.Tests;

public sealed class CatalogPageGuardTests
{
    [Fact]
    public void Catalog_routes_cover_list_create_detail_edit_categories_and_barcode_lookup()
    {
        var catalog = CatalogPagesDirectory();

        var list = File.ReadAllText(Path.Combine(catalog, "CatalogProductsList.razor"));
        Assert.Contains("@page \"/catalog\"", list, StringComparison.Ordinal);
        Assert.Contains("ResponsiveDataList", list, StringComparison.Ordinal);
        Assert.Contains("IPosCatalogClient", list, StringComparison.Ordinal);

        var create = File.ReadAllText(Path.Combine(catalog, "CatalogProductCreate.razor"));
        Assert.Contains("@page \"/catalog/products/new\"", create, StringComparison.Ordinal);

        var detail = File.ReadAllText(Path.Combine(catalog, "CatalogProductDetail.razor"));
        Assert.Contains("@page \"/catalog/products/{ProductId:guid}\"", detail, StringComparison.Ordinal);
        Assert.Contains("DeactivateProductAsync", detail, StringComparison.Ordinal);
        Assert.Contains("ReactivateProductAsync", detail, StringComparison.Ordinal);

        var edit = File.ReadAllText(Path.Combine(catalog, "CatalogProductEdit.razor"));
        Assert.Contains("@page \"/catalog/products/{ProductId:guid}/edit\"", edit, StringComparison.Ordinal);
        Assert.Contains("UpdateProductAsync", edit, StringComparison.Ordinal);
        Assert.Contains("_product.UpdatedAtUtc", edit, StringComparison.Ordinal);

        var categories = File.ReadAllText(Path.Combine(catalog, "CatalogCategories.razor"));
        Assert.Contains("@page \"/catalog/categories\"", categories, StringComparison.Ordinal);

        var lookup = File.ReadAllText(Path.Combine(catalog, "CatalogBarcodeLookup.razor"));
        Assert.Contains("@page \"/catalog/barcode-lookup\"", lookup, StringComparison.Ordinal);
        Assert.Contains("LookupByBarcodeAsync", lookup, StringComparison.Ordinal);
        Assert.Contains("LookupBySkuAsync", lookup, StringComparison.Ordinal);

        Assert.True(File.Exists(Path.Combine(catalog, "CatalogProductForm.razor")));
    }

    [Fact]
    public void Catalog_pages_guard_entry_and_gate_management_on_capability()
    {
        foreach (var file in Directory.EnumerateFiles(CatalogPagesDirectory(), "*.razor"))
        {
            var text = File.ReadAllText(file);
            if (!text.Contains("@page", StringComparison.Ordinal)
                || Path.GetFileName(file) == "ProductsRedirect.razor")
            {
                continue;
            }

            Assert.Contains("Gate.CanEnterProtectedShell", text, StringComparison.Ordinal);
            Assert.Contains("ResolveStartRouteAsync", text, StringComparison.Ordinal);
        }

        foreach (var page in new[]
                 {
                     "CatalogProductsList.razor",
                     "CatalogProductCreate.razor",
                     "CatalogProductDetail.razor",
                     "CatalogProductEdit.razor",
                     "CatalogCategories.razor"
                 })
        {
            var text = File.ReadAllText(Path.Combine(CatalogPagesDirectory(), page));
            Assert.Contains("UtangCapability.ManageCatalog", text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Catalog_pages_show_reconnect_required_and_never_read_local_storage()
    {
        foreach (var file in Directory.EnumerateFiles(CatalogPagesDirectory(), "*.razor"))
        {
            var text = File.ReadAllText(file);
            Assert.DoesNotContain("ILocalCustomerCreditStore", text, StringComparison.Ordinal);
            Assert.DoesNotContain("IOfflineOperationQueue", text, StringComparison.Ordinal);
            Assert.DoesNotContain("SQLite", text, StringComparison.OrdinalIgnoreCase);
        }

        foreach (var page in new[]
                 {
                     "CatalogProductsList.razor",
                     "CatalogProductDetail.razor",
                     "CatalogCategories.razor",
                     "CatalogBarcodeLookup.razor"
                 })
        {
            var text = File.ReadAllText(Path.Combine(CatalogPagesDirectory(), page));
            Assert.Contains("Connectivity.IsConnectedAsync", text, StringComparison.Ordinal);
            Assert.Contains("Catalog_Offline", text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Catalog_pages_have_no_stock_sale_cart_or_tax_surface()
    {
        foreach (var file in Directory.EnumerateFiles(CatalogPagesDirectory(), "*.razor"))
        {
            var text = File.ReadAllText(file);
            foreach (var forbidden in new[]
                     {
                         "StockOnHand", "QuantityOnHand", "Reorder", "AddToCart", "Checkout",
                         "RecordSale", "SaleLine", "TaxRate", "DiscountRate", "SupplierId"
                     })
            {
                Assert.DoesNotContain(forbidden, text, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    [Fact]
    public void Shell_navigation_points_products_tab_at_catalog_and_deferred_no_longer_owns_products()
    {
        var root = FindRepoRoot();
        var components = Path.Combine(root, "src", "Products", "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Maui", "Components");

        var shell = File.ReadAllText(Path.Combine(components, "Layout", "PosShell.razor"));
        Assert.Contains("href=\"/catalog\"", shell, StringComparison.Ordinal);
        Assert.DoesNotContain("href=\"/products\"", shell, StringComparison.Ordinal);

        var deferred = File.ReadAllText(Path.Combine(components, "Pages", "DeferredPage.razor"));
        Assert.DoesNotContain("@page \"/products\"", deferred, StringComparison.Ordinal);
        Assert.DoesNotContain("@page \"/sales\"", deferred, StringComparison.Ordinal);
        Assert.Contains("@page \"/more\"", deferred, StringComparison.Ordinal);

        var redirect = File.ReadAllText(Path.Combine(CatalogPagesDirectory(), "ProductsRedirect.razor"));
        Assert.Contains("@page \"/products\"", redirect, StringComparison.Ordinal);
        Assert.Contains("/catalog", redirect, StringComparison.Ordinal);
    }

    [Fact]
    public void Catalog_keys_are_localized_for_english_and_filipino()
    {
        var root = FindRepoRoot();
        var loc = Path.Combine(root, "src", "Products", "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Maui", "Localization");
        var en = File.ReadAllText(Path.Combine(loc, "PosResources.resx"));
        var fil = File.ReadAllText(Path.Combine(loc, "PosResources.fil-PH.resx"));

        foreach (var key in new[]
                 {
                     "Catalog_Title",
                     "Catalog_Subtitle",
                     "Catalog_OfflineMessage",
                     "Catalog_Field_SellingPrice",
                     "Catalog_Field_Barcode",
                     "Catalog_SkuConflict",
                     "Catalog_BarcodeConflict",
                     "Catalog_Category_Title",
                     "Catalog_Category_NameConflict",
                     "Catalog_Lookup_Title",
                     "Catalog_Lookup_NoMatchMessage",
                     "Catalog_Status_Active",
                     "Catalog_Status_Inactive",
                     "Catalog_Uom_Piece",
                     "Catalog_Uom_Kilogram",
                     "Catalog_Uom_Meter"
                 })
        {
            Assert.Contains($"name=\"{key}\"", en, StringComparison.Ordinal);
            Assert.Contains($"name=\"{key}\"", fil, StringComparison.Ordinal);
        }
    }

    private static string CatalogPagesDirectory() => Path.Combine(
        FindRepoRoot(),
        "src",
        "Products",
        "PinoyBusinessPOS",
        "ExItS.PinoyBusinessPOS.Maui",
        "Components",
        "Pages",
        "Catalog");

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
