namespace ExItS.PinoyBusinessPOS.Maui.Tests;

public sealed class LinkedSupplierProductsUiGuardTests
{
    [Fact]
    public void Linked_products_page_has_empty_state_and_stacked_search()
    {
        var page = File.ReadAllText(Path.Combine(MauiProject(), "Components", "Pages", "Suppliers", "LinkedSupplierProducts.razor"));
        Assert.Contains("ConnectedSuppliers_LinkedEmptyTitle", page, StringComparison.Ordinal);
        Assert.Contains("ConnectedSuppliers_LinkedEmptyMessage", page, StringComparison.Ordinal);
        Assert.Contains("EmptyState", page, StringComparison.Ordinal);
        Assert.Contains("pos-linked-products__search-field", page, StringComparison.Ordinal);
        Assert.Contains("ResolveRelationshipIdAsync", page, StringComparison.Ordinal);
        Assert.Contains("ConnectedSuppliers_BrowseProducts", page, StringComparison.Ordinal);
        Assert.DoesNotContain("<input type=\"search\" value=\"@_query\"", page, StringComparison.Ordinal);
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
