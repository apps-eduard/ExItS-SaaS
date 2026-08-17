namespace ExItS.PinoyBusinessPOS.Maui.Tests;

public sealed class PurchasingCreateUiGuardTests
{
    [Fact]
    public void Create_page_enforces_supplier_first_product_selection()
    {
        var create = CreatePage();

        Assert.Contains("Purchasing_SelectSupplierFirst", create, StringComparison.Ordinal);
        Assert.Contains("Purchasing_SelectSupplierFirstHelp", create, StringComparison.Ordinal);
        Assert.Contains("HasSupplierSelected", create, StringComparison.Ordinal);
        Assert.Contains("SupplyParameterFromQuery(Name = \"supplierId\")", create, StringComparison.Ordinal);
        Assert.Contains("ApplyPrefillSupplier", create, StringComparison.Ordinal);
        Assert.Contains("RequiresSupplierChangeConfirmation", create, StringComparison.Ordinal);
        Assert.Contains("ClearSupplierDependentDraftState", create, StringComparison.Ordinal);
        Assert.Contains("Purchasing_ChangeSupplierTitle", create, StringComparison.Ordinal);
        Assert.Contains("Purchasing_PoPriceLabel", create, StringComparison.Ordinal);
        Assert.Contains("Purchasing_ProductsFromSupplier", create, StringComparison.Ordinal);
        Assert.Contains("Purchasing_SearchYourProducts", create, StringComparison.Ordinal);
        Assert.Contains("Purchasing_SearchSupplierProducts", create, StringComparison.Ordinal);
        Assert.Contains("!HasSupplierSelected", create, StringComparison.Ordinal);
        Assert.Contains("_confirmSupplierChange", create, StringComparison.Ordinal);
    }

    [Fact]
    public void Create_page_has_search_category_filter_and_product_list()
    {
        var create = CreatePage();

        Assert.Contains("Purchasing_SearchYourProducts", create, StringComparison.Ordinal);
        Assert.Contains("pos-purchasing-create__search-input", create, StringComparison.Ordinal);
        Assert.Contains("Purchasing_CategoryFilter", create, StringComparison.Ordinal);
        Assert.Contains("Purchasing_AllCategories", create, StringComparison.Ordinal);
        Assert.Contains("pos-category-chip", create, StringComparison.Ordinal);
        Assert.Contains("@if (HasSupplierSelected)", create, StringComparison.Ordinal);
        Assert.Contains("IsConnectedSupplier ? _connectedReadyAll.Count == 0 : _products.Count == 0", create, StringComparison.Ordinal);
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
        Assert.Contains("PurchaseOrderCreateUi.LineTotal", create, StringComparison.Ordinal);
        Assert.Contains("MoneyDisplay Amount=\"@line.UnitPurchaseCost\"", create, StringComparison.Ordinal);
        Assert.Contains("pos-po-line__amount", create, StringComparison.Ordinal);
        Assert.Contains("IconGlyphs.Get(\"edit\")", create, StringComparison.Ordinal);
        Assert.Contains("IconGlyphs.Get(\"trash\")", create, StringComparison.Ordinal);
        Assert.Contains("Purchasing_EditLine", create, StringComparison.Ordinal);
        Assert.Contains("Purchasing_DeleteLine", create, StringComparison.Ordinal);
        Assert.Contains("pos-po-line__action--danger", create, StringComparison.Ordinal);
        Assert.DoesNotContain("pos-po-line__action-label", create, StringComparison.Ordinal);
        Assert.DoesNotContain("Emphasized=\"true\"", create, StringComparison.Ordinal);
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
        Assert.Contains("Purchasing_DraftSummaryOne", create, StringComparison.Ordinal);
        Assert.Contains("Purchasing_DraftSummaryMany", create, StringComparison.Ordinal);
        Assert.DoesNotContain("Purchasing_LinesCount", create, StringComparison.Ordinal);
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
    public void Create_page_shows_connected_readiness_summary_without_inline_setup()
    {
        var create = CreatePage();

        Assert.Contains("ConnectedSuppliers_ReadinessSummary", create, StringComparison.Ordinal);
        Assert.Contains("ConnectedSuppliers_ManageSupplierProducts", create, StringComparison.Ordinal);
        Assert.Contains("ClassifyCatalogReadinessAsync", create, StringComparison.Ordinal);
        Assert.Contains("GoManageSupplierProducts", create, StringComparison.Ordinal);
        Assert.Contains("PersistDraftSession", create, StringComparison.Ordinal);
        Assert.Contains("RestoreDraftSession", create, StringComparison.Ordinal);
        Assert.Contains("ReconcileOnlineReadyProducts", create, StringComparison.Ordinal);
        Assert.Contains("FilterOfflineReadyProducts", create, StringComparison.Ordinal);
        Assert.Contains("FilterConnectedReadyProducts", create, StringComparison.Ordinal);
        Assert.Contains("ListByRelationshipAsync", create, StringComparison.Ordinal);
        Assert.Contains("EligibleConnectedProducts", create, StringComparison.Ordinal);
        Assert.Contains("BuyerCategoryId", create, StringComparison.Ordinal);
        Assert.Contains("ApplyBuyerCatalog", create, StringComparison.Ordinal);
        Assert.Contains("Purchasing_AddedQty", create, StringComparison.Ordinal);
        Assert.Contains("pos-purchasing-create__hit-add", create, StringComparison.Ordinal);
        Assert.Contains("pos-purchasing-create__qty", create, StringComparison.Ordinal);
        Assert.Contains("ApplyConnectedQuantityDelta", create, StringComparison.Ordinal);
        Assert.Contains("AdjustConnectedQty", create, StringComparison.Ordinal);
        Assert.Contains("Purchasing_IncreaseQtyNamed", create, StringComparison.Ordinal);
        Assert.Contains("Purchasing_DecreaseQtyNamed", create, StringComparison.Ordinal);
        Assert.Contains("RemoveConnectedLine", create, StringComparison.Ordinal);
        Assert.Contains("@if (!IsConnectedSupplier)", create, StringComparison.Ordinal);
        Assert.DoesNotContain("QuickAddConnectedProduct", create, StringComparison.Ordinal);
        Assert.DoesNotContain("OnConnectedProductActivated", create, StringComparison.Ordinal);
        Assert.DoesNotContain("SearchLocalAsync", create, StringComparison.Ordinal);
        Assert.DoesNotContain("BeginLinkProduct", create, StringComparison.Ordinal);
        Assert.DoesNotContain("_supplierCatalogProducts", create, StringComparison.Ordinal);
        Assert.DoesNotContain("ConnectedSuppliers_UseProduct", create, StringComparison.Ordinal);
        Assert.DoesNotContain("pos-purchasing-create__link-panel", create, StringComparison.Ordinal);
    }

    [Fact]
    public void Create_page_surfaces_create_failures_next_to_the_footer()
    {
        var create = CreatePage();

        Assert.Contains("pos-purchasing-create__footer", create, StringComparison.Ordinal);
        Assert.Contains("@onclick=\"SaveAsync\"", create, StringComparison.Ordinal);
        Assert.Contains("catch (Exception)", create, StringComparison.Ordinal);
        Assert.Contains("Purchasing_CreateFailed", create, StringComparison.Ordinal);
        Assert.Contains("_saving = false", create, StringComparison.Ordinal);
        Assert.Contains("StateHasChanged();", create, StringComparison.Ordinal);
        Assert.Contains("ConnectedSuppliers_ReviewChangesHelp", create, StringComparison.Ordinal);
        Assert.Contains("pos-purchasing-create__review", create, StringComparison.Ordinal);
        Assert.Contains("_errorMessage ?? _linesError ?? _supplierError ?? _orderDateError", create, StringComparison.Ordinal);
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
                     "Purchasing_SearchYourProducts",
                     "Purchasing_SearchSupplierProducts",
                     "Purchasing_ProductsFromSupplier",
                     "Purchasing_SelectSupplierFirst",
                     "Purchasing_SelectSupplierFirstHelp",
                     "Purchasing_PoPriceLabel",
                     "Purchasing_ChangeSupplierTitle",
                     "Purchasing_ChangeSupplierMessage",
                     "Purchasing_ChangeSupplierConfirm",
                     "Purchasing_ChangeSupplierCancel",
                     "Purchasing_CategoryFilter",
                     "Purchasing_AllCategories",
                     "Purchasing_Uncategorized",
                     "Purchasing_NoProductsMatch",
                     "Purchasing_EditLine",
                     "Purchasing_DeleteLine",
                     "Purchasing_SaveChanges",
                     "Purchasing_CancelEdit",
                     "Purchasing_LineQtyPrefix",
                     "Purchasing_DraftSummaryOne",
                     "Purchasing_DraftSummaryMany",
                     "Purchasing_AddedQty",
                     "Purchasing_AddProductNamed",
                     "Purchasing_IncreaseQtyNamed",
                     "Purchasing_DecreaseQtyNamed",
                     "Purchasing_DeleteLineTitle",
                     "Purchasing_DeleteLineMessage",
                     "Purchasing_CreateFailed",
                     "ConnectedSuppliers_ManageSupplierProducts",
                     "ConnectedSuppliers_ReadinessSummary",
                     "ConnectedSuppliers_NoReadyProducts",
                     "ConnectedSuppliers_ReviewChanges",
                     "ConnectedSuppliers_ReviewChangesHelp"
                 })
        {
            Assert.Contains($"name=\"{key}\"", en, StringComparison.Ordinal);
            Assert.Contains($"name=\"{key}\"", fil, StringComparison.Ordinal);
        }

        foreach (var selector in new[]
                 {
                     ".pos-purchasing-create__search",
                     ".pos-purchasing-create__product-list",
                     ".pos-purchasing-create__supplier-first",
                     ".pos-purchasing-create__po-price",
                     ".pos-purchasing-create__readiness",
                     ".pos-purchasing-create__readiness-chips",
                     ".pos-purchasing-create__hit--ready",
                     ".pos-purchasing-create__hit-add",
                     ".pos-purchasing-create__qty",
                     ".pos-purchasing-create__review",
                     ".pos-connected-catalog__chips",
                     ".pos-po-line__actions",
                     ".pos-po-line__action--danger",
                     ".pos-po-line__amount"
                 })
        {
            Assert.Contains(selector, css, StringComparison.Ordinal);
        }

        Assert.Contains(".pos-purchasing-create .pos-po-line", css, StringComparison.Ordinal);
        Assert.Contains("padding: 0.75rem 1rem;", css, StringComparison.Ordinal);
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
