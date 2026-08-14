namespace ExItS.PinoyBusinessPOS.Maui.Tests;

/// <summary>
/// Guards the compact mobile New product experience: one bottom action pair, grouped fields,
/// and an explicit stock-tracking control backed by the existing inventory API.
/// </summary>
public sealed class CatalogProductFormUiTests
{
    [Fact]
    public void Create_page_has_no_top_cancel_action()
    {
        var create = CreatePage();

        Assert.DoesNotContain("<Actions>", create, StringComparison.Ordinal);
        Assert.DoesNotContain("PageHeader", create, StringComparison.Ordinal);
        Assert.DoesNotContain("Catalog_Cancel", create, StringComparison.Ordinal);
        Assert.Contains("pos-product-form-page__header", create, StringComparison.Ordinal);
        Assert.Contains("Catalog_Product_CreateTitle", create, StringComparison.Ordinal);
        Assert.Contains("Catalog_Product_CreateSubtitle", create, StringComparison.Ordinal);
    }

    [Fact]
    public void Form_renders_one_secondary_cancel_and_one_primary_save_at_the_bottom()
    {
        var form = FormComponent();

        Assert.Contains("pos-product-form__actions", form, StringComparison.Ordinal);
        Assert.Equal(1, Occurrences(form, "Catalog_Cancel"));
        Assert.Equal(1, Occurrences(form, "@SaveLabel"));
        Assert.Equal(1, Occurrences(form, "ButtonVariant.Secondary"));
        Assert.Equal(1, Occurrences(form, "ButtonVariant.Primary"));

        var actionsIndex = form.IndexOf("pos-product-form__actions", StringComparison.Ordinal);
        Assert.True(actionsIndex > form.IndexOf("Catalog_Field_SellingPrice", StringComparison.Ordinal));
    }

    [Fact]
    public void Form_groups_fields_into_compact_sections()
    {
        var form = FormComponent();

        Assert.DoesNotContain("<Section", form, StringComparison.Ordinal);
        Assert.DoesNotContain("<FormActions", form, StringComparison.Ordinal);
        Assert.Contains("pos-product-form__group", form, StringComparison.Ordinal);
        Assert.Equal(5, Occurrences(form, "pos-product-form__group-title"));

        foreach (var key in new[]
                 {
                     "Catalog_Product_DetailsSection",
                     "Catalog_Product_CodesSection",
                     "Catalog_Product_PricingSection",
                     "Catalog_Product_StockSection"
                 })
        {
            Assert.Contains(key, form, StringComparison.Ordinal);
        }

        Assert.Contains("Rows=\"2\"", form, StringComparison.Ordinal);
    }

    [Fact]
    public void Form_exposes_an_explicit_stock_tracking_control()
    {
        var form = FormComponent();
        var create = CreatePage();

        Assert.Contains("ShowStockTracking", form, StringComparison.Ordinal);
        Assert.Contains("<Switch", form, StringComparison.Ordinal);
        Assert.Contains("Catalog_TrackStock", form, StringComparison.Ordinal);
        Assert.Contains("Catalog_TrackStockOnHint", form, StringComparison.Ordinal);
        Assert.Contains("Catalog_TrackStockOffHint", form, StringComparison.Ordinal);
        Assert.Contains("Catalog_TrackExpiration", form, StringComparison.Ordinal);
        Assert.Contains("Catalog_TrackExpirationHint", form, StringComparison.Ordinal);
        Assert.Contains("Catalog_ExpirationWarningDays", form, StringComparison.Ordinal);

        Assert.Contains("IPosInventoryClient", create, StringComparison.Ordinal);
        Assert.Contains("UtangCapability.ManageInventory", create, StringComparison.Ordinal);
        Assert.Contains("EnableInventoryTrackingRequest", create, StringComparison.Ordinal);
        Assert.Contains("ShowStockTracking=\"@(canTrackStock && !_isOffline)\"", create, StringComparison.Ordinal);
    }

    [Fact]
    public void Create_page_prevents_duplicate_submission_and_reports_tracking_failure()
    {
        var create = CreatePage();

        Assert.Contains("if (_saving || _createdProductId is not null)", create, StringComparison.Ordinal);
        Assert.Contains("finally", create, StringComparison.Ordinal);
        Assert.Contains("Catalog_TrackStockFailed", create, StringComparison.Ordinal);
        Assert.Contains("Catalog_Product_View", create, StringComparison.Ordinal);
    }

    [Fact]
    public void Edit_page_shows_track_stock_and_calls_inventory_enable_disable()
    {
        var edit = File.ReadAllText(Path.Combine(CatalogPagesDirectory(), "CatalogProductEdit.razor"));

        Assert.Contains("<CatalogProductForm", edit, StringComparison.Ordinal);
        Assert.Contains("ShowStockTracking=\"@(canTrackStock && !_isOffline)\"", edit, StringComparison.Ordinal);
        Assert.Contains("TrackStock=\"@_trackStock\"", edit, StringComparison.Ordinal);
        Assert.Contains("IPosInventoryClient", edit, StringComparison.Ordinal);
        Assert.Contains("UtangCapability.ManageInventory", edit, StringComparison.Ordinal);
        Assert.Contains("_product.IsTracked", edit, StringComparison.Ordinal);
        Assert.Contains("EnableAsync", edit, StringComparison.Ordinal);
        Assert.Contains("DisableAsync", edit, StringComparison.Ordinal);
        Assert.Contains("Catalog_TrackStockDisableRequiresZero", edit, StringComparison.Ordinal);
    }

    [Fact]
    public void Product_form_styles_and_keys_exist_in_english_and_filipino()
    {
        var css = File.ReadAllText(Path.Combine(
            MauiProjectDirectory(), "wwwroot", "app.css"));

        foreach (var selector in new[]
                 {
                     ".pos-product-form-page__header",
                     ".pos-product-form__group",
                     ".pos-product-form__group-title",
                     ".pos-product-form__fields",
                     ".pos-product-form__actions"
                 })
        {
            Assert.Contains(selector, css, StringComparison.Ordinal);
        }

        var localization = Path.Combine(MauiProjectDirectory(), "Localization");
        var en = File.ReadAllText(Path.Combine(localization, "PosResources.resx"));
        var fil = File.ReadAllText(Path.Combine(localization, "PosResources.fil-PH.resx"));

        foreach (var key in new[]
                 {
                     "Catalog_Product_CodesSection",
                     "Catalog_Product_PricingSection",
                     "Catalog_Product_StockSection",
                     "Catalog_TrackStock",
                     "Catalog_TrackStockOnHint",
                     "Catalog_TrackStockOffHint",
                     "Catalog_TrackStockFailed",
                     "Catalog_TrackStockDisableRequiresZero",
                     "Catalog_TrackExpiration",
                     "Catalog_TrackExpirationHint",
                     "Catalog_ExpirationWarningDays",
                     "Catalog_ExpirationWarningDaysHint",
                     "Catalog_Product_CreatedTitle",
                     "Catalog_Product_View"
                 })
        {
            Assert.Contains($"name=\"{key}\"", en, StringComparison.Ordinal);
            Assert.Contains($"name=\"{key}\"", fil, StringComparison.Ordinal);
        }

        Assert.DoesNotContain("EAN-8, UPC-A, EAN-13, and GTIN-14 check digits are verified", en, StringComparison.Ordinal);

        foreach (var key in new[]
                 {
                     "Catalog_BuyingUnits",
                     "Catalog_SellingOptions",
                     "Catalog_SetPackages",
                     "Catalog_AddBuyingUnit",
                     "Catalog_AddSellingOption",
                     "Catalog_CustomAmount",
                     "Catalog_PriceForThisOption"
                 })
        {
            Assert.Contains($"name=\"{key}\"", en, StringComparison.Ordinal);
            Assert.Contains($"name=\"{key}\"", fil, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Form_exposes_buying_and_selling_package_editors()
    {
        var form = FormComponent();
        var create = CreatePage();
        var edit = File.ReadAllText(Path.Combine(CatalogPagesDirectory(), "CatalogProductEdit.razor"));

        Assert.Contains("Catalog_SetPackages", form, StringComparison.Ordinal);
        Assert.Contains("PurchaseUnits", form, StringComparison.Ordinal);
        Assert.Contains("SellUnits", form, StringComparison.Ordinal);
        Assert.Contains("Catalog_BuyingUnits", form, StringComparison.Ordinal);
        Assert.Contains("Catalog_SellingOptions", form, StringComparison.Ordinal);
        Assert.Contains("Catalog_CustomAmount", form, StringComparison.Ordinal);
        Assert.DoesNotContain("Multiplier", form, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Base UOM", form, StringComparison.OrdinalIgnoreCase);

        Assert.Contains("_purchaseUnits", create, StringComparison.Ordinal);
        Assert.Contains("_sellUnits", create, StringComparison.Ordinal);
        Assert.Contains("Units: units", create, StringComparison.Ordinal);
        Assert.Contains("_purchaseUnits", edit, StringComparison.Ordinal);
        Assert.Contains("_sellUnits", edit, StringComparison.Ordinal);
        Assert.Contains("Units: units", edit, StringComparison.Ordinal);
        Assert.Contains("ProductUnitDraft", create, StringComparison.Ordinal);
    }

    private static int Occurrences(string text, string value)
    {
        var count = 0;
        var index = text.IndexOf(value, StringComparison.Ordinal);
        while (index >= 0)
        {
            count++;
            index = text.IndexOf(value, index + value.Length, StringComparison.Ordinal);
        }

        return count;
    }

    private static string CreatePage() =>
        File.ReadAllText(Path.Combine(CatalogPagesDirectory(), "CatalogProductCreate.razor"));

    private static string FormComponent() =>
        File.ReadAllText(Path.Combine(CatalogPagesDirectory(), "CatalogProductForm.razor"));

    private static string CatalogPagesDirectory() =>
        Path.Combine(MauiProjectDirectory(), "Components", "Pages", "Catalog");

    private static string MauiProjectDirectory() => Path.Combine(
        FindRepoRoot(), "src", "Products", "PinoyBusinessPOS", "ExItS.PinoyBusinessPOS.Maui");

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
