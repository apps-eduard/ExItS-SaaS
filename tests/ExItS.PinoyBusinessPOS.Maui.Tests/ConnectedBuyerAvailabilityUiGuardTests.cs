namespace ExItS.PinoyBusinessPOS.Maui.Tests;

public sealed class ConnectedBuyerAvailabilityUiGuardTests
{
    [Fact]
    public void Catalog_connected_buyer_availability_page_is_mobile_list_not_desktop_grid()
    {
        var page = File.ReadAllText(Path.Combine(CatalogPagesDirectory(), "CatalogConnectedBuyerAvailability.razor"));
        Assert.Contains("@page \"/catalog/connected-buyer-availability\"", page, StringComparison.Ordinal);
        Assert.Contains("pos-manage-share__row", page, StringComparison.Ordinal);
        Assert.Contains("pos-manage-share__sticky", page, StringComparison.Ordinal);
        Assert.Contains("QueryConnectedBuyerAvailabilityAsync", page, StringComparison.Ordinal);
        Assert.Contains("BulkMutateConnectedBuyerAvailabilityAsync", page, StringComparison.Ordinal);
        Assert.Contains("PreviewDefaultConnectedPoPricingAsync", page, StringComparison.Ordinal);
        Assert.Contains("ApplyDefaultConnectedPoPricingAsync", page, StringComparison.Ordinal);
        Assert.Contains("SelectAllMatching", page, StringComparison.Ordinal);
        Assert.Contains("UtangCapability.ManageCatalog", page, StringComparison.Ordinal);
        Assert.Contains("Catalog_ConnectedAvailability_OfflineMessage", page, StringComparison.Ordinal);
        Assert.Contains("Catalog_ConnectedAvailability_ReviewPrices", page, StringComparison.Ordinal);
        Assert.Contains("Catalog_ConnectedAvailability_SetPoPriceTitle", page, StringComparison.Ordinal);
        Assert.Contains("pos-manage-share__price-grid", page, StringComparison.Ordinal);
        Assert.Contains("pos-denom-sheet__panel--fit", page, StringComparison.Ordinal);
        Assert.DoesNotContain("ResponsiveDataList", page, StringComparison.Ordinal);
        Assert.DoesNotContain("<table", page, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ILocalCustomerCreditStore", page, StringComparison.Ordinal);
        Assert.DoesNotContain("IOfflineOperationQueue", page, StringComparison.Ordinal);
    }

    [Fact]
    public void Catalog_hub_links_to_connected_buyer_availability()
    {
        var list = File.ReadAllText(Path.Combine(CatalogPagesDirectory(), "CatalogProductsList.razor"));
        Assert.Contains("/catalog/connected-buyer-availability", list, StringComparison.Ordinal);
        Assert.Contains("Catalog_Quick_ConnectedAvailability", list, StringComparison.Ordinal);
    }

    [Fact]
    public void Connected_availability_keys_are_localized_for_english_and_filipino()
    {
        var root = FindRepoRoot();
        var loc = Path.Combine(root, "src", "Products", "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Maui", "Localization");
        var en = File.ReadAllText(Path.Combine(loc, "PosResources.resx"));
        var fil = File.ReadAllText(Path.Combine(loc, "PosResources.fil-PH.resx"));

        foreach (var key in new[]
                 {
                     "Catalog_Quick_ConnectedAvailability",
                     "Catalog_ConnectedAvailability_Title",
                     "Catalog_ConnectedAvailability_Intro",
                     "Catalog_ConnectedAvailability_Summary",
                     "Catalog_ConnectedAvailability_FilterAll",
                     "Catalog_ConnectedAvailability_FilterAvailable",
                     "Catalog_ConnectedAvailability_FilterNotAvailable",
                     "Catalog_ConnectedAvailability_Enable",
                     "Catalog_ConnectedAvailability_Disable",
                     "Catalog_ConnectedAvailability_PoPrice",
                     "Catalog_ConnectedAvailability_SetPoPriceTitle",
                     "Catalog_ConnectedAvailability_SelectedProducts",
                     "Catalog_ConnectedAvailability_BulkHint",
                     "Catalog_ConnectedAvailability_SetFromRetail",
                     "Catalog_ConnectedAvailability_SetFromRetailHelp",
                     "Catalog_ConnectedAvailability_DiscountFromRetail",
                     "Catalog_ConnectedAvailability_DiscountFromRetailHelp",
                     "Catalog_ConnectedAvailability_AdjustRetail",
                     "Catalog_ConnectedAvailability_AdjustRetailHelp",
                     "Catalog_ConnectedAvailability_SetSamePrice",
                     "Catalog_ConnectedAvailability_SetSamePriceHelp",
                     "Catalog_ConnectedAvailability_ReviewPrices",
                     "Catalog_ConnectedAvailability_ReviewTitle",
                     "Catalog_ConnectedAvailability_CurrentDefaultPo",
                     "Catalog_ConnectedAvailability_NewDefaultPo",
                     "Catalog_ConnectedAvailability_OfflineMessage",
                     "Catalog_ConnectedAvailability_EmptyTitle",
                     "Catalog_ConnectedAvailability_EmptySearchTitle",
                     "Catalog_ConnectedAvailability_EmptyAvailableTitle",
                     "Catalog_ConnectedAvailability_EmptyUnavailableTitle",
                     "ConnectedBuyers_SelectProduct",
                     "ConnectedBuyers_ApplyToCount"
                 })
        {
            Assert.Contains($"name=\"{key}\"", en, StringComparison.Ordinal);
            Assert.Contains($"name=\"{key}\"", fil, StringComparison.Ordinal);
        }

        Assert.Contains("<value>{0} available · {1} unavailable</value>", en, StringComparison.Ordinal);
        Assert.Contains("<value>Apply to {0} products</value>", en, StringComparison.Ordinal);
        Assert.Contains("<value>Review prices</value>", en, StringComparison.Ordinal);
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
