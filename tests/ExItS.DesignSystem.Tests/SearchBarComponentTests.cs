namespace ExItS.DesignSystem.Tests;

public sealed class SearchBarComponentTests
{
    [Fact]
    public void SearchBar_component_exists_with_required_contract()
    {
        var path = Path.Combine(FindRepoRoot(), "src", "Shared", "ExItS.DesignSystem",
            "Components", "Data", "SearchBar.razor");
        Assert.True(File.Exists(path));
        var text = File.ReadAllText(path);

        Assert.Contains("type=\"search\"", text, StringComparison.Ordinal);
        Assert.Contains("exds-search-bar__icon", text, StringComparison.Ordinal);
        Assert.Contains("exds-search-bar__clear", text, StringComparison.Ordinal);
        Assert.Contains("ShowFilterButton", text, StringComparison.Ordinal);
        Assert.Contains("ActiveFilterCount", text, StringComparison.Ordinal);
        Assert.Contains("FilterChips", text, StringComparison.Ordinal);
        Assert.Contains("DebounceMilliseconds", text, StringComparison.Ordinal);
        Assert.Contains("OnFilterClick", text, StringComparison.Ordinal);
        Assert.Contains("OnSearch", text, StringComparison.Ordinal);
        Assert.Contains("OnClear", text, StringComparison.Ordinal);
        Assert.Contains("Action_ClearSearch", text, StringComparison.Ordinal);
        Assert.Contains("Search_Filters", text, StringComparison.Ordinal);
        Assert.Contains("Search_ActiveFiltersCount", text, StringComparison.Ordinal);
        Assert.Contains("IconGlyphs.Get(\"sliders\")", text, StringComparison.Ordinal);
        Assert.DoesNotContain("CategoryId", text, StringComparison.Ordinal);
        Assert.DoesNotContain("OrderStatus", text, StringComparison.Ordinal);
    }

    [Fact]
    public void SearchBar_styles_define_mobile_friendly_layout()
    {
        var css = File.ReadAllText(Path.Combine(FindRepoRoot(), "src", "Shared", "ExItS.DesignSystem",
            "wwwroot", "exits-design-system.css"));
        Assert.Contains(".exds-search-bar__row", css, StringComparison.Ordinal);
        Assert.Contains(".exds-search-bar__filter", css, StringComparison.Ordinal);
        Assert.Contains(".exds-search-bar__badge", css, StringComparison.Ordinal);
        Assert.Contains("min-height: var(--exits-touch-target-min)", css, StringComparison.Ordinal);
    }

    [Fact]
    public void SearchBox_and_SearchToolbar_delegate_to_SearchBar()
    {
        var root = Path.Combine(FindRepoRoot(), "src", "Shared", "ExItS.DesignSystem", "Components");
        var searchBox = File.ReadAllText(Path.Combine(root, "Feedback", "SearchBox.razor"));
        var toolbar = File.ReadAllText(Path.Combine(root, "Data", "SearchToolbar.razor"));
        Assert.Contains("<SearchBar", searchBox, StringComparison.Ordinal);
        Assert.Contains("<SearchBar", toolbar, StringComparison.Ordinal);
    }

    [Fact]
    public void Migrated_maui_pages_use_SearchBar_except_sale_checkout()
    {
        var mauiPages = Path.Combine(FindRepoRoot(), "src", "Products", "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Maui", "Components", "Pages");
        var checkout = File.ReadAllText(Path.Combine(mauiPages, "Sales", "SaleCheckout.razor"));
        Assert.Contains("type=\"search\"", checkout, StringComparison.Ordinal);
        Assert.DoesNotContain("<SearchBar", checkout, StringComparison.Ordinal);

        var listPages = new[]
        {
            "Customers/CustomersList.razor",
            "Catalog/CatalogProductsList.razor",
            "Inventory/InventoryList.razor",
            "Sales/SalesList.razor",
        };
        foreach (var relative in listPages)
        {
            var text = File.ReadAllText(Path.Combine(mauiPages, relative));
            Assert.Contains("<SearchBar", text, StringComparison.Ordinal);
            Assert.DoesNotContain("type=\"search\"", text, StringComparison.Ordinal);
        }
    }

    private static string FindRepoRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir, "ExItS.slnx")))
            {
                return dir;
            }

            dir = Directory.GetParent(dir)?.FullName;
        }

        throw new InvalidOperationException("Could not locate repository root.");
    }
}
