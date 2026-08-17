namespace ExItS.PinoyBusinessPOS.Maui.Tests;

public sealed class ConnectedSupplierClientUiGuardTests
{
    [Fact]
    public void Connected_supplier_routes_and_clients_are_wired()
    {
        var suppliers = Path.Combine(MauiProject(), "Components", "Pages", "Suppliers");
        var purchasing = Path.Combine(MauiProject(), "Components", "Pages", "Purchasing");
        Assert.Contains("@page \"/suppliers/connected/request\"",
            File.ReadAllText(Path.Combine(suppliers, "ConnectedSupplierRequest.razor")), StringComparison.Ordinal);
        Assert.Contains("@page \"/suppliers/connected/requests\"",
            File.ReadAllText(Path.Combine(suppliers, "ConnectedSupplierIncomingRequests.razor")), StringComparison.Ordinal);
        Assert.Contains("ListRelationshipsAsync(\"supplier\")",
            File.ReadAllText(Path.Combine(suppliers, "ConnectedSupplierIncomingRequests.razor")), StringComparison.Ordinal);
        Assert.Contains("ApproveAsync",
            File.ReadAllText(Path.Combine(suppliers, "ConnectedSupplierIncomingRequests.razor")), StringComparison.Ordinal);
        var moreHub = File.ReadAllText(Path.Combine(MauiProject(), "Components", "Pages", "MoreHub.razor"));
        Assert.Contains("/suppliers/connected/requests", moreHub, StringComparison.Ordinal);
        Assert.Contains("ConnectedSuppliers_ReviewIncoming", moreHub, StringComparison.Ordinal);
        Assert.Contains("@page \"/suppliers/{SupplierId:guid}/connected-catalog\"",
            File.ReadAllText(Path.Combine(suppliers, "ConnectedSupplierCatalog.razor")), StringComparison.Ordinal);
        var catalogPage = File.ReadAllText(Path.Combine(suppliers, "ConnectedSupplierCatalog.razor"));
        Assert.Contains("ClassifyCatalogReadinessAsync", catalogPage, StringComparison.Ordinal);
        Assert.Contains("AutoLinkExactMatchesAsync", catalogPage, StringComparison.Ordinal);
        var client = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "src",
            "Products",
            "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.ApiClient",
            "PosConnectedSupplierClient.cs"));
        var iface = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "src",
            "Products",
            "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Application",
            "Abstractions",
            "IPosConnectedSupplierClient.cs"));
        Assert.Contains("ClassifyCatalogReadinessAsync", client, StringComparison.Ordinal);
        Assert.Contains("AutoLinkExactMatchesAsync", client, StringComparison.Ordinal);
        Assert.Contains("ClassifyCatalogReadinessAsync", iface, StringComparison.Ordinal);
        Assert.Contains("AutoLinkExactMatchesAsync", iface, StringComparison.Ordinal);
        Assert.Contains("@page \"/suppliers/{SupplierId:guid}/linked-products\"",
            File.ReadAllText(Path.Combine(suppliers, "LinkedSupplierProducts.razor")), StringComparison.Ordinal);
        Assert.Contains("@page \"/connected-suppliers/incoming\"",
            File.ReadAllText(Path.Combine(purchasing, "ConnectedSupplierIncomingOrders.razor")), StringComparison.Ordinal);
        Assert.Contains("@page \"/connected-suppliers/incoming/{OrderId:guid}\"",
            File.ReadAllText(Path.Combine(purchasing, "ConnectedSupplierIncomingOrderDetail.razor")), StringComparison.Ordinal);
        Assert.Contains("PrepareIncomingAsync",
            File.ReadAllText(Path.Combine(purchasing, "ConnectedSupplierIncomingOrderDetail.razor")), StringComparison.Ordinal);
        Assert.Contains("FulfillIncomingAsync",
            File.ReadAllText(Path.Combine(purchasing, "ConnectedSupplierIncomingOrderDetail.razor")), StringComparison.Ordinal);
        Assert.Contains("DisplayStatus",
            File.ReadAllText(Path.Combine(purchasing, "PurchasingDetail.razor")), StringComparison.Ordinal);
        var receive = File.ReadAllText(Path.Combine(purchasing, "PurchasingReceive.razor"));
        Assert.True(
            receive.Contains("Purchasing_ReviewReceipt", StringComparison.Ordinal)
            || receive.Contains("ReviewReceipt", StringComparison.Ordinal)
            || receive.Contains("ConfirmGoodsReceipt", StringComparison.Ordinal),
            "Receiving page must include a review/confirm step.");
    }

    [Fact]
    public void Connected_po_lifecycle_localization_keys_exist_in_en_and_fil()
    {
        var localization = Path.Combine(MauiProject(), "Localization");
        var en = File.ReadAllText(Path.Combine(localization, "PosResources.resx"));
        var fil = File.ReadAllText(Path.Combine(localization, "PosResources.fil-PH.resx"));
        foreach (var key in new[]
                 {
                     "Purchasing_Status_WaitingForSupplier",
                     "Purchasing_Status_SupplierAccepted",
                     "Purchasing_Status_SupplierDeclined",
                     "Purchasing_Status_ReceivedWithIssues",
                     "Purchasing_GoodReceived",
                     "Purchasing_CloseAsShort",
                     "Purchasing_ConfirmGoodsReceipt",
                     "Purchasing_ReviewReceipt",
                     "ConnectedSuppliers_StartPreparing",
                     "ConnectedSuppliers_MarkFulfilled"
                 })
        {
            Assert.Contains($"<data name=\"{key}\"", en, StringComparison.Ordinal);
            Assert.Contains($"<data name=\"{key}\"", fil, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Purchasing_keeps_external_picker_and_adds_connected_offline_contract()
    {
        var page = File.ReadAllText(Path.Combine(
            MauiProject(), "Components", "Pages", "Purchasing", "PurchasingCreate.razor"));
        Assert.Contains("Purchasing_SearchYourProducts", page, StringComparison.Ordinal);
        Assert.Contains("PurchaseOrderCreateUi.FilterEligibleProducts", page, StringComparison.Ordinal);
        Assert.Contains("Purchasing_AllCategories", page, StringComparison.Ordinal);
        Assert.Contains("ClassifyCatalogReadinessAsync", page, StringComparison.Ordinal);
        Assert.Contains("ListLinksAsync", page, StringComparison.Ordinal);
        Assert.Contains("SearchLocalAsync", page, StringComparison.Ordinal);
        Assert.Contains("RevalidateDraftAsync", page, StringComparison.Ordinal);
        Assert.Contains("ConnectedDrafts.SaveAsync", page, StringComparison.Ordinal);
        Assert.Contains("ConnectedSuppliers_ManageSupplierProducts", page, StringComparison.Ordinal);
        Assert.Contains("PurchaseOrderDraftSession", page, StringComparison.Ordinal);
        Assert.Contains("GoManageSupplierProducts", page, StringComparison.Ordinal);
        Assert.DoesNotContain("BeginLinkProduct", page, StringComparison.Ordinal);
        Assert.DoesNotContain("_supplierCatalogProducts", page, StringComparison.Ordinal);
        Assert.DoesNotContain("SearchCatalogAsync", page, StringComparison.Ordinal);
        Assert.DoesNotContain("ConnectedSuppliers_UseProduct", page, StringComparison.Ordinal);
    }

    [Fact]
    public void Offline_no_match_wording_is_localized_in_both_languages()
    {
        var localization = Path.Combine(MauiProject(), "Localization");
        var en = File.ReadAllText(Path.Combine(localization, "PosResources.resx"));
        var fil = File.ReadAllText(Path.Combine(localization, "PosResources.fil-PH.resx"));
        Assert.Contains("No offline result. Connect to search more products from this supplier.", en, StringComparison.Ordinal);
        Assert.Contains("Walang resulta offline. Kumonekta para maghanap pa ng produkto mula sa supplier na ito.", fil, StringComparison.Ordinal);
    }

    private static string MauiProject()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "src", "Products", "PinoyBusinessPOS", "ExItS.PinoyBusinessPOS.Maui");
            if (Directory.Exists(candidate)) return candidate;
            dir = dir.Parent;
        }
        throw new InvalidOperationException("MAUI project not found.");
    }

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
