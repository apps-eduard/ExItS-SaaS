namespace ExItS.PinoyBusinessPOS.Maui.Tests;

public sealed class PurchasingPageGuardTests
{
    [Fact]
    public void Purchasing_routes_cover_list_create_detail_and_receive()
    {
        var pages = PurchasingPagesDirectory();

        var list = File.ReadAllText(Path.Combine(pages, "PurchasingList.razor"));
        Assert.Contains("@page \"/purchasing\"", list, StringComparison.Ordinal);
        Assert.Contains("IPosPurchaseOrderClient", list, StringComparison.Ordinal);

        var create = File.ReadAllText(Path.Combine(pages, "PurchasingCreate.razor"));
        Assert.Contains("@page \"/purchasing/new\"", create, StringComparison.Ordinal);
        Assert.Contains("CreateAsync", create, StringComparison.Ordinal);
        Assert.Contains("CreatePurchaseOrderRequest", create, StringComparison.Ordinal);
        Assert.Contains("IPosSupplierClient", create, StringComparison.Ordinal);
        Assert.Contains("IPosCatalogClient", create, StringComparison.Ordinal);
        Assert.Contains("Purchasing_AddLine", create, StringComparison.Ordinal);
        Assert.Contains("Purchasing_Field_Supplier", create, StringComparison.Ordinal);
        Assert.Contains("Purchasing_Reset", create, StringComparison.Ordinal);
        Assert.Contains("<StoreHeaderBack Href=\"/purchasing\" />", create, StringComparison.Ordinal);
        Assert.Contains("pos-action-grid--single", create, StringComparison.Ordinal);
        Assert.Contains("pos-action-grid--two", create, StringComparison.Ordinal);
        Assert.Contains("IconGlyphs.Get(\"refresh\")", create, StringComparison.Ordinal);
        Assert.Contains("IconGlyphs.Get(\"plus\")", create, StringComparison.Ordinal);
        Assert.DoesNotContain("IconGlyphs.Get(\"back\")", create, StringComparison.Ordinal);
        Assert.Contains("pos-settings__panel", create, StringComparison.Ordinal);
        Assert.DoesNotContain("<Actions>", create, StringComparison.Ordinal);
        Assert.Contains("Purchasing_UnitCostRequired", create, StringComparison.Ordinal);
        Assert.Contains("Required=\"true\"", create, StringComparison.Ordinal);
        Assert.Contains("_draftUnitCost is null", create, StringComparison.Ordinal);
        Assert.Contains("_draftUnitCost.Value <= 0m", create, StringComparison.Ordinal);
        Assert.Contains("/purchasing/{result.Data.PurchaseOrderId:D}", create, StringComparison.Ordinal);

        var detail = File.ReadAllText(Path.Combine(pages, "PurchasingDetail.razor"));
        Assert.Contains("@page \"/purchasing/{PurchaseOrderId:guid}\"", detail, StringComparison.Ordinal);
        Assert.Contains("<StoreHeaderBack Href=\"/purchasing\" />", detail, StringComparison.Ordinal);
        Assert.Contains("SubmitAsync", detail, StringComparison.Ordinal);
        Assert.Contains("CancelAsync", detail, StringComparison.Ordinal);
        Assert.DoesNotContain("Purchasing_BackToList", detail, StringComparison.Ordinal);

        var receive = File.ReadAllText(Path.Combine(pages, "PurchasingReceive.razor"));
        Assert.Contains("@page \"/purchasing/{PurchaseOrderId:guid}/receive\"", receive, StringComparison.Ordinal);
        Assert.Contains("StoreHeaderBack Href=\"@($\"/purchasing/{PurchaseOrderId:D}\")\"", receive, StringComparison.Ordinal);
        Assert.Contains("ReceiveAsync", receive, StringComparison.Ordinal);
        Assert.DoesNotContain(".AsTask()", receive, StringComparison.Ordinal);
        Assert.Contains("ReceivePurchaseOrderRequest", receive, StringComparison.Ordinal);
        Assert.Contains("OutstandingQty", receive, StringComparison.Ordinal);
        Assert.DoesNotContain("Purchasing_BackToDetail", receive, StringComparison.Ordinal);
    }

    [Fact]
    public void Purchasing_pages_guard_entry_and_gate_capabilities()
    {
        foreach (var file in Directory.EnumerateFiles(PurchasingPagesDirectory(), "*.razor"))
        {
            var text = File.ReadAllText(file);
            Assert.Contains("Gate.CanEnterProtectedShell", text, StringComparison.Ordinal);
            Assert.Contains("ResolveStartRouteAsync", text, StringComparison.Ordinal);
            Assert.Contains("UtangCapability.ViewPurchasing", text, StringComparison.Ordinal);
            Assert.Contains("UtangCapability.ManagePurchasing", text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Purchasing_pages_are_online_only()
    {
        foreach (var file in Directory.EnumerateFiles(PurchasingPagesDirectory(), "*.razor"))
        {
            var text = File.ReadAllText(file);
            Assert.DoesNotContain("IOfflineOperationQueue", text, StringComparison.Ordinal);
            Assert.DoesNotContain("LocalStore", text, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Connectivity.IsConnectedAsync", text, StringComparison.Ordinal);
            Assert.Contains("Purchasing_Offline", text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Purchasing_keys_are_localized_for_english_and_filipino()
    {
        var root = FindRepoRoot();
        var loc = Path.Combine(root, "src", "Products", "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Maui", "Localization");
        var en = File.ReadAllText(Path.Combine(loc, "PosResources.resx"));
        var fil = File.ReadAllText(Path.Combine(loc, "PosResources.fil-PH.resx"));

        foreach (var key in new[]
                 {
                     "Nav_Purchasing",
                     "Purchasing_Title",
                     "Purchasing_Subtitle",
                     "Purchasing_OfflineMessage",
                     "Purchasing_Create",
                     "Purchasing_Submit",
                     "Purchasing_Receive",
                     "Purchasing_AddLine",
                     "Purchasing_SupplierRequired",
                     "Purchasing_LinesRequired",
                     "Purchasing_NoSuppliersMessage",
                     "Purchasing_Reset",
                     "Purchasing_ResetTitle",
                     "Purchasing_ResetMessage",
                     "Purchasing_UnitCostRequired",
                     "Purchasing_UnitCostHint",
                     "Purchasing_CreateSupplier"
                 })
        {
            Assert.Contains($"name=\"{key}\"", en, StringComparison.Ordinal);
            Assert.Contains($"name=\"{key}\"", fil, StringComparison.Ordinal);
        }
    }

    private static string PurchasingPagesDirectory() => Path.Combine(
        FindRepoRoot(),
        "src",
        "Products",
        "PinoyBusinessPOS",
        "ExItS.PinoyBusinessPOS.Maui",
        "Components",
        "Pages",
        "Purchasing");

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
