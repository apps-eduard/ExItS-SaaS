namespace ExItS.PinoyBusinessPOS.Maui.Tests;

/// <summary>
/// Guards the compact mobile Browse global catalog experience: auth-gated controls,
/// collapsible filters, selectable rows, and a selection bar that appears only when needed.
/// </summary>
public sealed class CatalogGlobalBrowseUiTests
{
    [Fact]
    public void Page_removes_oversized_products_navigation_and_keeps_template_secondary()
    {
        var page = Page();

        Assert.DoesNotContain("PageHeader", page, StringComparison.Ordinal);
        Assert.DoesNotContain("GoCatalog", page, StringComparison.Ordinal);
        Assert.DoesNotContain("OnClick=\"GoCatalog\"", page, StringComparison.Ordinal);
        Assert.Contains("pos-global__template", page, StringComparison.Ordinal);
        Assert.Contains("Catalog_Import_UseTemplate", page, StringComparison.Ordinal);
        Assert.Contains("GoImport", page, StringComparison.Ordinal);
        Assert.DoesNotContain("ButtonVariant.Secondary\" OnClick=\"GoImport\"", page, StringComparison.Ordinal);
    }

    [Fact]
    public void Sign_in_required_state_hides_search_filters_and_import_controls()
    {
        var page = Page();

        Assert.Contains("_authBlocked", page, StringComparison.Ordinal);
        Assert.Contains("Catalog_Global_AuthTitle", page, StringComparison.Ordinal);
        Assert.Contains("Catalog_Global_AuthMessage", page, StringComparison.Ordinal);
        Assert.Contains("Catalog_Global_AuthUnavailable", page, StringComparison.Ordinal);
        Assert.Contains("ApiCallStatus.Unauthorized", page, StringComparison.Ordinal);

        // Auth branch must not render the interactive toolbar/list.
        var authIdx = page.IndexOf("@if (_authBlocked)", StringComparison.Ordinal);
        var toolbarIdx = page.IndexOf("pos-global__toolbar", StringComparison.Ordinal);
        var elseIdx = page.IndexOf("else if (_accessDenied)", StringComparison.Ordinal);
        Assert.True(authIdx >= 0 && toolbarIdx > authIdx);
        Assert.True(elseIdx > authIdx && elseIdx < toolbarIdx);
    }

    [Fact]
    public void Auth_blocked_state_only_claims_signin_unavailable_when_no_session()
    {
        var page = Page();

        // A 401 while the app still holds a session renders the session-expired copy plus a retry,
        // never the misleading "sign-in is not available in this build" note.
        Assert.Contains("_sessionExpired", page, StringComparison.Ordinal);
        Assert.Contains("_sessionExpired = CurrentUser.IsAuthenticated;", page, StringComparison.Ordinal);
        Assert.Contains("Catalog_Global_SessionExpiredTitle", page, StringComparison.Ordinal);
        Assert.Contains("Catalog_Global_SessionExpiredMessage", page, StringComparison.Ordinal);

        // The "unavailable" note is only reachable in the no-session (else) branch.
        var sessionBranch = page.IndexOf("if (_sessionExpired)", StringComparison.Ordinal);
        var unavailable = page.IndexOf("Catalog_Global_AuthUnavailable", StringComparison.Ordinal);
        Assert.True(sessionBranch >= 0 && unavailable > sessionBranch);
    }

    [Fact]
    public void Sign_in_required_and_empty_states_are_mutually_exclusive()
    {
        var page = Page();

        Assert.Contains("Catalog_Global_EmptyTitle", page, StringComparison.Ordinal);
        Assert.Contains("Catalog_Global_NoMatchTitle", page, StringComparison.Ordinal);

        // Empty titles appear only inside the authenticated interactive branch.
        var authBlock = page.IndexOf("@if (_authBlocked)", StringComparison.Ordinal);
        var interactive = page.IndexOf("pos-global__toolbar", StringComparison.Ordinal);
        var empty = page.IndexOf("Catalog_Global_EmptyTitle", StringComparison.Ordinal);
        Assert.True(authBlock >= 0 && interactive > authBlock && empty > interactive);
    }

    [Fact]
    public void Authenticated_state_uses_compact_search_refresh_and_collapsible_filters()
    {
        var page = Page();

        Assert.Contains("pos-global__search", page, StringComparison.Ordinal);
        Assert.Contains("IconName=\"refresh\"", page, StringComparison.Ordinal);
        Assert.Contains("Catalog_Global_ClearSearch", page, StringComparison.Ordinal);
        Assert.Contains("pos-global__filter-toggle", page, StringComparison.Ordinal);
        Assert.Contains("Catalog_Global_FiltersCount", page, StringComparison.Ordinal);
        Assert.Contains("Catalog_Global_ClearFilters", page, StringComparison.Ordinal);
        Assert.Contains("Catalog_Field_Barcode", page, StringComparison.Ordinal);
        Assert.Contains("Catalog_Filter_Category", page, StringComparison.Ordinal);
        Assert.Contains("DebouncedReloadAsync", page, StringComparison.Ordinal);
        Assert.DoesNotContain("SearchToolbar", page, StringComparison.Ordinal);
        Assert.DoesNotContain("<Section Title=\"@L[\"Catalog_FiltersTitle\"]\">", page, StringComparison.Ordinal);
    }

    [Fact]
    public void Product_rows_are_selectable_without_raw_ids_and_selection_bar_is_conditional()
    {
        var page = Page();

        Assert.Contains("pos-global__row", page, StringComparison.Ordinal);
        Assert.Contains("aria-pressed", page, StringComparison.Ordinal);
        Assert.Contains("ToggleSelect", page, StringComparison.Ordinal);
        Assert.Contains("BuildIdentity", page, StringComparison.Ordinal);
        Assert.DoesNotContain("product.Id.ToString", page, StringComparison.Ordinal);
        Assert.DoesNotContain("product.Id:D", page, StringComparison.Ordinal);

        Assert.Contains("@if (_selectedIds.Count > 0)", page, StringComparison.Ordinal);
        Assert.Contains("pos-global__selection", page, StringComparison.Ordinal);
        Assert.Contains("Catalog_Global_ImportSelected", page, StringComparison.Ordinal);
        Assert.Contains("Catalog_Global_ClearSelection", page, StringComparison.Ordinal);
        Assert.DoesNotContain("Catalog_Global_SelectedCount\"].Value, _selectedIds.Count)\">\r\n            <FormActions>", page, StringComparison.Ordinal);
    }

    [Fact]
    public void Import_prevents_duplicates_and_preserves_contracts()
    {
        var page = Page();

        Assert.Contains("if (_selectedIds.Count == 0 || _busy || _isOffline || _authBlocked)", page, StringComparison.Ordinal);
        Assert.Contains("IdempotencyKey: Guid.NewGuid().ToString(\"N\")", page, StringComparison.Ordinal);
        Assert.Contains("ImportSelectedProductsAsync", page, StringComparison.Ordinal);
        Assert.Contains("finally", page, StringComparison.Ordinal);
        Assert.Contains("/catalog/import/jobs/", page, StringComparison.Ordinal);
    }

    [Fact]
    public void Offline_and_access_denied_states_hide_interactive_catalog()
    {
        var page = Page();

        Assert.Contains("Catalog_OfflineTitle", page, StringComparison.Ordinal);
        Assert.Contains("Catalog_Import_OfflineMessage", page, StringComparison.Ordinal);
        Assert.Contains("_accessDenied", page, StringComparison.Ordinal);
        Assert.Contains("Access_DeniedTitle", page, StringComparison.Ordinal);
        Assert.Contains("Api_Retry", page, StringComparison.Ordinal);
    }

    [Fact]
    public void Styles_and_localization_keys_exist()
    {
        var css = File.ReadAllText(Path.Combine(MauiProjectDirectory(), "wwwroot", "app.css"));
        foreach (var selector in new[]
                 {
                     ".pos-global__header",
                     ".pos-global__toolbar",
                     ".pos-global__filter-toggle",
                     ".pos-global__row",
                     ".pos-global__row--selected",
                     ".pos-global__selection",
                     ".pos-global__state"
                 })
        {
            Assert.Contains(selector, css, StringComparison.Ordinal);
        }

        var localization = Path.Combine(MauiProjectDirectory(), "Localization");
        var en = File.ReadAllText(Path.Combine(localization, "PosResources.resx"));
        var fil = File.ReadAllText(Path.Combine(localization, "PosResources.fil-PH.resx"));
        foreach (var key in new[]
                 {
                     "Catalog_Global_AuthTitle",
                     "Catalog_Global_AuthMessage",
                     "Catalog_Global_AuthUnavailable",
                     "Catalog_Global_SessionExpiredTitle",
                     "Catalog_Global_SessionExpiredMessage",
                     "Catalog_Global_NoMatchTitle",
                     "Catalog_Global_NoMatchMessage",
                     "Catalog_Global_Loading",
                     "Catalog_Global_ClearSearch",
                     "Catalog_Global_ClearFilters",
                     "Catalog_Global_FiltersCount"
                 })
        {
            Assert.Contains($"name=\"{key}\"", en, StringComparison.Ordinal);
            Assert.Contains($"name=\"{key}\"", fil, StringComparison.Ordinal);
        }
    }

    private static string Page() =>
        File.ReadAllText(Path.Combine(
            MauiProjectDirectory(),
            "Components",
            "Pages",
            "Catalog",
            "CatalogGlobalBrowse.razor"));

    private static string MauiProjectDirectory() => Path.Combine(
        FindRepoRoot(), "src", "Products", "PinoyBusinessPOS", "ExItS.PinoyBusinessPOS.Maui");

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
