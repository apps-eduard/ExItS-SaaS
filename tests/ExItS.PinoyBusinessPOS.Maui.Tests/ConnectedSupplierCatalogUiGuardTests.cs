namespace ExItS.PinoyBusinessPOS.Maui.Tests;

public sealed class ConnectedSupplierCatalogUiGuardTests
{
    [Fact]
    public void Browse_catalog_shows_empty_and_loading_states()
    {
        var page = File.ReadAllText(Path.Combine(MauiProject(), "Components", "Pages", "Suppliers", "ConnectedSupplierCatalog.razor"));
        Assert.Contains("ConnectedSuppliers_CatalogEmptyTitle", page, StringComparison.Ordinal);
        Assert.Contains("ConnectedSuppliers_CatalogEmptyMessage", page, StringComparison.Ordinal);
        Assert.Contains("ConnectedSuppliers_UseProduct", page, StringComparison.Ordinal);
        Assert.Contains("CreateBuyerProductAndLinkAsync", page, StringComparison.Ordinal);
        Assert.Contains("SuggestBuyerProductMatchesAsync", page, StringComparison.Ordinal);
        Assert.Contains("ConnectedSuppliers_PoPriceLabel", page, StringComparison.Ordinal);
        Assert.Contains("ApiLocalizer.Describe", page, StringComparison.Ordinal);
        Assert.Contains("ResolveRelationshipIdAsync", page, StringComparison.Ordinal);
        Assert.Contains("EmptyState", page, StringComparison.Ordinal);
        Assert.Contains("pos-connected-catalog__search-field", page, StringComparison.Ordinal);
        Assert.Contains("ClearSearchAsync", page, StringComparison.Ordinal);
        Assert.DoesNotContain("pos-connected-catalog__search-btn", page, StringComparison.Ordinal);
        Assert.DoesNotContain("pos-connected-catalog__search-row", page, StringComparison.Ordinal);
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
