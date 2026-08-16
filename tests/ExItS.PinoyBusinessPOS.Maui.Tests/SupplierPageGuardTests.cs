namespace ExItS.PinoyBusinessPOS.Maui.Tests;

public sealed class SupplierPageGuardTests
{
    [Fact]
    public void Supplier_routes_cover_list_create_detail_and_edit()
    {
        var suppliers = SuppliersPagesDirectory();

        var list = File.ReadAllText(Path.Combine(suppliers, "SuppliersList.razor"));
        Assert.Contains("@page \"/suppliers\"", list, StringComparison.Ordinal);
        Assert.Contains("pos-suppliers__header", list, StringComparison.Ordinal);
        Assert.Contains("pos-suppliers__row", list, StringComparison.Ordinal);
        Assert.Contains("pos-suppliers__list", list, StringComparison.Ordinal);
        Assert.Contains("IPosSupplierClient", list, StringComparison.Ordinal);
        Assert.Contains("IconGlyphs.Get(\"plus\")", list, StringComparison.Ordinal);
        Assert.Contains("IconGlyphs.Get(\"qr\")", list, StringComparison.Ordinal);
        Assert.Contains("IconGlyphs.Get(\"inbox\")", list, StringComparison.Ordinal);
        Assert.Contains("IconGlyphs.Get(\"customers\")", list, StringComparison.Ordinal);
        Assert.Contains("IconGlyphs.Get(\"refresh\")", list, StringComparison.Ordinal);
        Assert.Contains("IconGlyphs.Get(\"search\")", list, StringComparison.Ordinal);
        Assert.DoesNotContain("PageHeader", list, StringComparison.Ordinal);

        var en = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "src", "Products", "PinoyBusinessPOS", "ExItS.PinoyBusinessPOS.Maui",
            "Localization", "PosResources.resx"));
        Assert.Contains("<data name=\"Suppliers_Add\"", en, StringComparison.Ordinal);
        Assert.Contains("<value>Supplier</value>", en, StringComparison.Ordinal);
        Assert.DoesNotContain("<value>Add supplier</value>", en, StringComparison.Ordinal);

        var create = File.ReadAllText(Path.Combine(suppliers, "SupplierCreate.razor"));
        Assert.Contains("@page \"/suppliers/new\"", create, StringComparison.Ordinal);
        Assert.Contains("StoreHeaderBack Href=\"/suppliers\"", create, StringComparison.Ordinal);
        Assert.Contains("CreateAsync", create, StringComparison.Ordinal);
        Assert.Contains("OnCancel=\"GoBack\"", create, StringComparison.Ordinal);
        Assert.Contains("pos-supplier-create__header", create, StringComparison.Ordinal);
        Assert.Contains("pos-supplier-create__form", create, StringComparison.Ordinal);
        Assert.DoesNotContain("PageHeader", create, StringComparison.Ordinal);
        Assert.DoesNotContain("OnClick=\"GoBack\"", create, StringComparison.Ordinal);

        var detail = File.ReadAllText(Path.Combine(suppliers, "SupplierDetail.razor"));
        Assert.Contains("@page \"/suppliers/{SupplierId:guid}\"", detail, StringComparison.Ordinal);
        Assert.Contains("StoreHeaderBack Href=\"/suppliers\"", detail, StringComparison.Ordinal);
        Assert.Contains("DeactivateAsync", detail, StringComparison.Ordinal);
        Assert.Contains("ReactivateAsync", detail, StringComparison.Ordinal);
        Assert.Contains("pos-supplier-detail__header", detail, StringComparison.Ordinal);
        Assert.Contains("pos-supplier-detail__facts", detail, StringComparison.Ordinal);
        Assert.Contains("pos-supplier-detail__code", detail, StringComparison.Ordinal);
        Assert.Contains("Suppliers_ContactSection", detail, StringComparison.Ordinal);
        Assert.Contains("Suppliers_AddressSection", detail, StringComparison.Ordinal);
        Assert.Contains("Suppliers_DetailsEmptyTitle", detail, StringComparison.Ordinal);
        Assert.Contains("IconGlyphs.Get(\"edit\")", detail, StringComparison.Ordinal);
        Assert.Contains("IconGlyphs.Get(\"products\")", detail, StringComparison.Ordinal);
        Assert.Contains("pos-action-grid--two", detail, StringComparison.Ordinal);
        Assert.Contains("IconGlyphs.Get(\"edit\")", detail, StringComparison.Ordinal);
        Assert.DoesNotContain("PageHeader", detail, StringComparison.Ordinal);
        Assert.DoesNotContain("<Actions>", detail, StringComparison.Ordinal);
        Assert.DoesNotContain("Suppliers_BackToList", detail, StringComparison.Ordinal);

        var edit = File.ReadAllText(Path.Combine(suppliers, "SupplierEdit.razor"));
        Assert.Contains("@page \"/suppliers/{SupplierId:guid}/edit\"", edit, StringComparison.Ordinal);
        Assert.Contains("StoreHeaderBack Href=\"/suppliers\"", edit, StringComparison.Ordinal);
        Assert.Contains("UpdateAsync", edit, StringComparison.Ordinal);
        Assert.Contains("_supplier.UpdatedAtUtc", edit, StringComparison.Ordinal);
        Assert.Contains("pos-supplier-edit__header", edit, StringComparison.Ordinal);
        Assert.Contains("pos-supplier-edit__form", edit, StringComparison.Ordinal);
        Assert.DoesNotContain("PageHeader", edit, StringComparison.Ordinal);
        Assert.DoesNotContain("<Actions>", edit, StringComparison.Ordinal);
        Assert.DoesNotContain("Suppliers_BackToList", edit, StringComparison.Ordinal);

        var form = File.ReadAllText(Path.Combine(suppliers, "SupplierForm.razor"));
        Assert.Contains("FormActions", form, StringComparison.Ordinal);
        Assert.Contains("FormClass", form, StringComparison.Ordinal);
        Assert.DoesNotContain("<Section", form, StringComparison.Ordinal);
    }

    [Fact]
    public void Supplier_pages_guard_entry_and_gate_management_on_capability()
    {
        foreach (var file in Directory.EnumerateFiles(SuppliersPagesDirectory(), "*.razor"))
        {
            var text = File.ReadAllText(file);
            if (!text.Contains("@page", StringComparison.Ordinal))
            {
                continue;
            }

            Assert.Contains("Gate.CanEnterProtectedShell", text, StringComparison.Ordinal);
            Assert.Contains("ResolveStartRouteAsync", text, StringComparison.Ordinal);
        }

        foreach (var page in new[]
                 {
                     "SuppliersList.razor",
                     "SupplierCreate.razor",
                     "SupplierDetail.razor",
                     "SupplierEdit.razor"
                 })
        {
            var text = File.ReadAllText(Path.Combine(SuppliersPagesDirectory(), page));
            Assert.Contains("UtangCapability.ManageSuppliers", text, StringComparison.Ordinal);
            Assert.Contains("UtangCapability.ViewSuppliers", text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Supplier_pages_are_online_only_and_never_queue_offline_mutations()
    {
        foreach (var file in Directory.EnumerateFiles(SuppliersPagesDirectory(), "*.razor"))
        {
            var text = File.ReadAllText(file);
            Assert.DoesNotContain("IOfflineOperationQueue", text, StringComparison.Ordinal);
            Assert.DoesNotContain("SQLite", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("LocalStore", text, StringComparison.OrdinalIgnoreCase);
        }

        foreach (var page in new[]
                 {
                     "SuppliersList.razor",
                     "SupplierDetail.razor",
                     "SupplierCreate.razor",
                     "SupplierEdit.razor"
                 })
        {
            var text = File.ReadAllText(Path.Combine(SuppliersPagesDirectory(), page));
            Assert.Contains("Connectivity.IsConnectedAsync", text, StringComparison.Ordinal);
            Assert.Contains("Suppliers_Offline", text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Supplier_pages_have_no_purchasing_or_payables_controls()
    {
        foreach (var file in Directory.EnumerateFiles(SuppliersPagesDirectory(), "*.*"))
        {
            if (!file.EndsWith(".razor", StringComparison.OrdinalIgnoreCase)
                && !file.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var text = File.ReadAllText(file);
            foreach (var forbidden in new[]
                     {
                         "GoodsReceipt", "Receiving", "AccountsPayable",
                         "SupplierInvoice", "SupplierPayment", "CostHistory", "PurchaseReturn"
                     })
            {
                Assert.DoesNotContain(forbidden, text, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    [Fact]
    public void Supplier_keys_are_localized_for_english_and_filipino()
    {
        var root = FindRepoRoot();
        var loc = Path.Combine(root, "src", "Products", "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Maui", "Localization");
        var en = File.ReadAllText(Path.Combine(loc, "PosResources.resx"));
        var fil = File.ReadAllText(Path.Combine(loc, "PosResources.fil-PH.resx"));

        foreach (var key in new[]
                 {
                     "Suppliers_Title",
                     "Suppliers_Subtitle",
                     "Suppliers_OfflineMessage",
                     "Suppliers_Field_Name",
                     "Suppliers_Field_Code",
                     "Suppliers_NameConflict",
                     "Suppliers_EmailConflict",
                     "Suppliers_MobileConflict",
                     "Suppliers_TaxConflict",
                     "Suppliers_Status_Active",
                     "Suppliers_Status_Inactive",
                     "Suppliers_Deactivate",
                     "Suppliers_Reactivate",
                     "Suppliers_ConcurrencyConflict"
                 })
        {
            Assert.Contains($"name=\"{key}\"", en, StringComparison.Ordinal);
            Assert.Contains($"name=\"{key}\"", fil, StringComparison.Ordinal);
        }
    }

    private static string SuppliersPagesDirectory() => Path.Combine(
        FindRepoRoot(),
        "src",
        "Products",
        "PinoyBusinessPOS",
        "ExItS.PinoyBusinessPOS.Maui",
        "Components",
        "Pages",
        "Suppliers");

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
