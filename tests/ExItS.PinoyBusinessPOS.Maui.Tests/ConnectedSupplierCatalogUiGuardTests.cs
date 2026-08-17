namespace ExItS.PinoyBusinessPOS.Maui.Tests;

public sealed class ConnectedSupplierCatalogUiGuardTests
{
    [Fact]
    public void Setup_catalog_uses_readiness_flow_and_status_chips()
    {
        var page = File.ReadAllText(Path.Combine(MauiProject(), "Components", "Pages", "Suppliers", "ConnectedSupplierCatalog.razor"));
        Assert.Contains("ConnectedSuppliers_SetupTitle", page, StringComparison.Ordinal);
        Assert.Contains("ConnectedSuppliers_CatalogEmptyTitle", page, StringComparison.Ordinal);
        Assert.Contains("ConnectedSuppliers_CatalogEmptyMessage", page, StringComparison.Ordinal);
        Assert.Contains("AutoLinkExactMatchesAsync", page, StringComparison.Ordinal);
        Assert.Contains("ClassifyCatalogReadinessAsync", page, StringComparison.Ordinal);
        Assert.Contains("CreateBuyerProductAndLinkAsync", page, StringComparison.Ordinal);
        Assert.Contains("ConnectedSuppliers_AddToMyProducts", page, StringComparison.Ordinal);
        Assert.Contains("ConnectedSuppliers_SameProduct", page, StringComparison.Ordinal);
        Assert.Contains("ConnectedSuppliers_AddAsNew", page, StringComparison.Ordinal);
        Assert.Contains("ConnectedSuppliers_NeedsAttention", page, StringComparison.Ordinal);
        Assert.Contains("ConnectedSuppliers_PoPriceLabel", page, StringComparison.Ordinal);
        Assert.Contains("ApiLocalizer.Describe", page, StringComparison.Ordinal);
        Assert.Contains("ResolveRelationshipIdAsync", page, StringComparison.Ordinal);
        Assert.Contains("EmptyState", page, StringComparison.Ordinal);
        Assert.Contains("pos-connected-catalog__search-field", page, StringComparison.Ordinal);
        Assert.Contains("pos-connected-catalog__chips", page, StringComparison.Ordinal);
        Assert.Contains("ClearSearchAsync", page, StringComparison.Ordinal);
        Assert.Contains("LinkedSync.SyncAsync", page, StringComparison.Ordinal);
        Assert.Contains("returnUrl", page, StringComparison.Ordinal);
        Assert.Contains("filter", page, StringComparison.Ordinal);
        Assert.DoesNotContain("ConnectedSuppliers_UseProduct", page, StringComparison.Ordinal);
        Assert.DoesNotContain("SuggestBuyerProductMatchesAsync", page, StringComparison.Ordinal);
        Assert.DoesNotContain("pos-connected-catalog__search-btn", page, StringComparison.Ordinal);
        Assert.DoesNotContain("pos-connected-catalog__search-row", page, StringComparison.Ordinal);
        Assert.DoesNotContain("Name match", page, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Setup_catalog_localization_keys_exist_in_en_and_fil()
    {
        var localization = Path.Combine(MauiProject(), "Localization");
        var en = File.ReadAllText(Path.Combine(localization, "PosResources.resx"));
        var fil = File.ReadAllText(Path.Combine(localization, "PosResources.fil-PH.resx"));
        foreach (var key in new[]
                 {
                     "ConnectedSuppliers_SetupTitle",
                     "ConnectedSuppliers_SetupSubtitle",
                     "ConnectedSuppliers_SetupHint",
                     "ConnectedSuppliers_FilterReady",
                     "ConnectedSuppliers_AddToMyProducts",
                     "ConnectedSuppliers_SameProduct",
                     "ConnectedSuppliers_AddAsNew",
                     "ConnectedSuppliers_NeedsAttention",
                     "ConnectedSuppliers_ManageSupplierProducts",
                     "ConnectedSuppliers_ConflictMultipleMatch",
                     "ConnectedSuppliers_ReadinessSummary"
                 })
        {
            Assert.Contains($"name=\"{key}\"", en, StringComparison.Ordinal);
            Assert.Contains($"name=\"{key}\"", fil, StringComparison.Ordinal);
        }
    }

    private static string MauiProject()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(
                dir.FullName,
                "src",
                "Products",
                "PinoyBusinessPOS",
                "ExItS.PinoyBusinessPOS.Maui");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate ExItS.PinoyBusinessPOS.Maui project directory.");
    }
}
