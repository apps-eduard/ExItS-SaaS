namespace ExItS.PinoyBusinessPOS.Maui.Tests;

public sealed class CatalogTodaysPricesUiTests
{
    [Fact]
    public void Todays_prices_page_is_manage_catalog_gated_online_required_and_price_only()
    {
        var page = File.ReadAllText(Path.Combine(CatalogPagesDirectory(), "CatalogTodaysPrices.razor"));
        Assert.Contains("@page \"/catalog/todays-prices\"", page, StringComparison.Ordinal);
        Assert.Contains("UtangCapability.ManageCatalog", page, StringComparison.Ordinal);
        Assert.Contains("OnlineRequired.EnsureOnlineForActionAsync", page, StringComparison.Ordinal);
        Assert.Contains("PosOfflineActionKeys.CatalogManage", page, StringComparison.Ordinal);
        Assert.Contains("UpdateProductPricesAsync", page, StringComparison.Ordinal);
        Assert.Contains("UpsertProductsAsync", page, StringComparison.Ordinal);
        Assert.Contains("SalesUiOptions.PriceUnitSuffix", page, StringComparison.Ordinal);
        Assert.Contains("Catalog_TodaysPrices_Title", page, StringComparison.Ordinal);
        Assert.Contains("pos-todays-prices__row--changed", page, StringComparison.Ordinal);
        Assert.Contains("StoreHeaderBack Href=\"/catalog\"", page, StringComparison.Ordinal);
        Assert.DoesNotContain("IOfflineOperationQueue", page, StringComparison.Ordinal);
        Assert.DoesNotContain("UpdateProductAsync", page, StringComparison.Ordinal);
        Assert.DoesNotContain("AddToCart", page, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Catalog_list_exposes_todays_prices_quick_action()
    {
        var list = File.ReadAllText(Path.Combine(CatalogPagesDirectory(), "CatalogProductsList.razor"));
        Assert.Contains("/catalog/todays-prices", list, StringComparison.Ordinal);
        Assert.Contains("Catalog_Quick_TodaysPrices", list, StringComparison.Ordinal);
        Assert.Contains("GoTodaysPrices", list, StringComparison.Ordinal);
    }

    [Fact]
    public void Localization_and_offline_policy_cover_todays_prices()
    {
        var en = File.ReadAllText(Path.Combine(FindRepoRoot(),
            "src", "Products", "PinoyBusinessPOS", "ExItS.PinoyBusinessPOS.Maui",
            "Localization", "PosResources.resx"));
        Assert.Contains("Catalog_TodaysPrices_Title", en, StringComparison.Ordinal);
        Assert.Contains("Catalog_Quick_TodaysPrices", en, StringComparison.Ordinal);

        var fil = File.ReadAllText(Path.Combine(FindRepoRoot(),
            "src", "Products", "PinoyBusinessPOS", "ExItS.PinoyBusinessPOS.Maui",
            "Localization", "PosResources.fil-PH.resx"));
        Assert.Contains("Catalog_TodaysPrices_Title", fil, StringComparison.Ordinal);

        var policy = File.ReadAllText(Path.Combine(FindRepoRoot(),
            "src", "Products", "PinoyBusinessPOS", "ExItS.PinoyBusinessPOS.Application",
            "Offline", "PosOfflineCapabilityPolicy.cs"));
        Assert.Contains("[\"/catalog/todays-prices\"]", policy, StringComparison.Ordinal);

        var client = File.ReadAllText(Path.Combine(FindRepoRoot(),
            "src", "Products", "PinoyBusinessPOS", "ExItS.PinoyBusinessPOS.Application",
            "Abstractions", "IPosCatalogClient.cs"));
        Assert.Contains("UpdateProductPricesAsync", client, StringComparison.Ordinal);
    }

    private static string CatalogPagesDirectory() =>
        Path.Combine(
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
