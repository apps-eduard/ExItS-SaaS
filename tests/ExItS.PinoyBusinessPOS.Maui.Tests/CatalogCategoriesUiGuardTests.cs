namespace ExItS.PinoyBusinessPOS.Maui.Tests;

public sealed class CatalogCategoriesUiGuardTests
{
    [Fact]
    public void Categories_page_uses_compact_header_without_content_back_button()
    {
        var page = ReadCategories();
        Assert.Contains("@page \"/catalog/categories\"", page, StringComparison.Ordinal);
        Assert.Contains("pos-categories__title", page, StringComparison.Ordinal);
        Assert.Contains("Catalog_Category_Title", page, StringComparison.Ordinal);
        Assert.Contains("Catalog_Category_Add", page, StringComparison.Ordinal);
        Assert.Contains("pos-categories__add", page, StringComparison.Ordinal);
        Assert.DoesNotContain("Catalog_BackToList", page, StringComparison.Ordinal);
        Assert.DoesNotContain("GoCatalog", page, StringComparison.Ordinal);
        Assert.DoesNotContain("NavigateTo(\"/catalog\")", page, StringComparison.Ordinal);
        Assert.Contains("Gate.CanEnterProtectedShell", page, StringComparison.Ordinal);
        Assert.Contains("ResolveStartRouteAsync", page, StringComparison.Ordinal);
    }

    [Fact]
    public void Categories_create_form_is_compact_with_busy_and_duplicate_submit_guards()
    {
        var page = ReadCategories();
        Assert.Contains("_formOpen", page, StringComparison.Ordinal);
        Assert.Contains("BeginCreate", page, StringComparison.Ordinal);
        Assert.Contains("TextInput", page, StringComparison.Ordinal);
        Assert.DoesNotContain("TextArea", page, StringComparison.Ordinal);
        Assert.Contains("_name.Trim()", page, StringComparison.Ordinal);
        Assert.Contains("Catalog_Category_NameRequired", page, StringComparison.Ordinal);
        Assert.Contains("if (_saving)", page, StringComparison.Ordinal);
        Assert.Contains("finally", page, StringComparison.Ordinal);
        Assert.Contains("_saving = false", page, StringComparison.Ordinal);
        Assert.Contains("CreateCategoryAsync", page, StringComparison.Ordinal);
        Assert.Contains("ReloadAsync(showLoading: false)", page, StringComparison.Ordinal);
        Assert.Contains("EmptyState", page, StringComparison.Ordinal);
        Assert.Contains("showEmptyAdd", page, StringComparison.Ordinal);
    }

    [Fact]
    public void Categories_search_status_refresh_and_pagination_preserve_query()
    {
        var page = ReadCategories();
        Assert.Contains("pos-categories__search-input", page, StringComparison.Ordinal);
        Assert.Contains("Catalog_Category_SearchPlaceholder", page, StringComparison.Ordinal);
        Assert.Contains("Task.Delay(250", page, StringComparison.Ordinal);
        Assert.Contains("role=\"radiogroup\"", page, StringComparison.Ordinal);
        Assert.Contains("Catalog_Category_Filter_All", page, StringComparison.Ordinal);
        Assert.Contains("Catalog_Status_Active", page, StringComparison.Ordinal);
        Assert.Contains("Catalog_Status_Inactive", page, StringComparison.Ordinal);
        Assert.Contains("ListCategoriesAsync", page, StringComparison.Ordinal);
        Assert.Contains("pos-categories__refresh", page, StringComparison.Ordinal);
        Assert.Contains("TotalPages > 1", page, StringComparison.Ordinal);
        Assert.Contains("_page = 1", page, StringComparison.Ordinal);
        Assert.Contains("Pagination", page, StringComparison.Ordinal);
    }

    [Fact]
    public void Categories_rows_use_compact_overflow_actions_without_browser_links()
    {
        var page = ReadCategories();
        Assert.Contains("pos-categories__row", page, StringComparison.Ordinal);
        Assert.Contains("pos-categories__badge", page, StringComparison.Ordinal);
        Assert.Contains("pos-categories__menu", page, StringComparison.Ordinal);
        Assert.Contains("Catalog_Category_Rename", page, StringComparison.Ordinal);
        Assert.Contains("Catalog_Deactivate", page, StringComparison.Ordinal);
        Assert.Contains("Catalog_Reactivate", page, StringComparison.Ordinal);
        Assert.Contains("ConfirmDialog", page, StringComparison.Ordinal);
        Assert.Contains("pos-categories__menu-item--danger", page, StringComparison.Ordinal);
        Assert.Contains("RequestDeactivate", page, StringComparison.Ordinal);
        Assert.Contains("CloseMenu", page, StringComparison.Ordinal);
        Assert.Contains("pos-categories__menu-backdrop", page, StringComparison.Ordinal);
        Assert.Contains("pos-categories__menu-item--cancel", page, StringComparison.Ordinal);
        Assert.Contains("Catalog_Cancel", page, StringComparison.Ordinal);
        Assert.Contains("NavigationLock", page, StringComparison.Ordinal);
        Assert.Contains("OnMenuBackNavigation", page, StringComparison.Ordinal);
        Assert.DoesNotContain("ResponsiveDataList", page, StringComparison.Ordinal);
        Assert.DoesNotContain("class=\"pos-link\"", page, StringComparison.Ordinal);
        Assert.DoesNotContain("CategoryId.ToString", page, StringComparison.Ordinal);
        Assert.DoesNotContain("Catalog_Col_Actions", page, StringComparison.Ordinal);
    }

    [Fact]
    public void Categories_offline_auth_and_states_are_enforced()
    {
        var page = ReadCategories();
        Assert.Contains("UtangCapability.ManageCatalog", page, StringComparison.Ordinal);
        Assert.Contains("UtangCapability.ViewCatalog", page, StringComparison.Ordinal);
        Assert.Contains("Connectivity.IsConnectedAsync", page, StringComparison.Ordinal);
        Assert.Contains("ConnectivityChanged", page, StringComparison.Ordinal);
        Assert.Contains("Catalog_Offline", page, StringComparison.Ordinal);
        Assert.Contains("disabled=\"@(!onlineActionsEnabled", page, StringComparison.Ordinal);
        Assert.Contains("ErrorState", page, StringComparison.Ordinal);
        Assert.Contains("role=\"status\"", page, StringComparison.Ordinal);
        Assert.Contains("Api_Unavailable", page, StringComparison.Ordinal);
        Assert.DoesNotContain("ex.Message", page, StringComparison.Ordinal);
    }

    [Fact]
    public void Categories_compact_styles_and_localization_keys_exist()
    {
        var root = FindRepoRoot();
        var css = File.ReadAllText(Path.Combine(
            root,
            "src",
            "Products",
            "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Maui",
            "wwwroot",
            "app.css"));
        Assert.Contains(".pos-categories", css, StringComparison.Ordinal);
        Assert.Contains(".pos-categories__row", css, StringComparison.Ordinal);
        Assert.Contains(".pos-categories__segments", css, StringComparison.Ordinal);
        Assert.Contains(".pos-categories__menu-item--danger", css, StringComparison.Ordinal);
        Assert.Contains(".pos-categories__menu-backdrop", css, StringComparison.Ordinal);
        Assert.Contains(".pos-categories__menu-item--cancel", css, StringComparison.Ordinal);

        var loc = Path.Combine(root, "src", "Products", "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Maui", "Localization");
        var en = File.ReadAllText(Path.Combine(loc, "PosResources.resx"));
        var fil = File.ReadAllText(Path.Combine(loc, "PosResources.fil-PH.resx"));
        foreach (var key in new[]
                 {
                     "Catalog_Category_ClearSearch",
                     "Catalog_Category_Filter_All",
                     "Catalog_Category_MoreActions",
                     "Catalog_Category_NoMatchTitle",
                     "Catalog_Category_NoMatchMessage",
                     "Catalog_Category_ConfirmDeactivateTitle",
                     "Catalog_Category_ConfirmDeactivateMessage"
                 })
        {
            Assert.Contains($"name=\"{key}\"", en, StringComparison.Ordinal);
            Assert.Contains($"name=\"{key}\"", fil, StringComparison.Ordinal);
        }
    }

    private static string ReadCategories() => File.ReadAllText(Path.Combine(
        FindRepoRoot(),
        "src",
        "Products",
        "PinoyBusinessPOS",
        "ExItS.PinoyBusinessPOS.Maui",
        "Components",
        "Pages",
        "Catalog",
        "CatalogCategories.razor"));

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
