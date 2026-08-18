namespace ExItS.PinoyBusinessPOS.Maui.Tests;

public sealed class MauiListLoadPerformanceGuardTests
{
    [Fact]
    public void Customers_list_loads_from_api_without_awaiting_incremental_download()
    {
        var list = File.ReadAllText(Path.Combine(
            MauiPages(),
            "Customers",
            "CustomersList.razor"));

        var listCall = list.IndexOf("Customers.ListAsync", StringComparison.Ordinal);
        Assert.True(listCall >= 0, "Customers list must call ListAsync.");

        var awaitedDownload = list.IndexOf("await Sync.DownloadIncrementalAsync()", StringComparison.Ordinal);
        Assert.True(
            awaitedDownload < 0 || listCall < awaitedDownload,
            "Visible customer list must not wait on full incremental download.");

        Assert.Contains("ScheduleOfflineCacheDownload()", list, StringComparison.Ordinal);
        Assert.Contains("LoadCustomersAsync(syncOfflineCache: true)", list, StringComparison.Ordinal);
        Assert.Contains("await ReloadAsync();", ExtractMethod(list, "OnSearchInputAsync"), StringComparison.Ordinal);
        Assert.Contains("await ReloadAsync();", ExtractMethod(list, "OnPageChanged"), StringComparison.Ordinal);
    }

    [Fact]
    public void Sell_checkout_does_not_await_full_catalog_cache_before_ready()
    {
        var checkout = File.ReadAllText(Path.Combine(
            MauiPages(),
            "Sales",
            "SaleCheckout.razor"));

        Assert.Contains("ScheduleSellingCatalogCacheRefresh()", checkout, StringComparison.Ordinal);
        Assert.Contains("CatalogSync.RefreshFromServerAsync()", checkout, StringComparison.Ordinal);
        Assert.DoesNotContain(
            "await CatalogSync.RefreshFromServerAsync()",
            ExtractMethod(checkout, "OnInitializedAsync"),
            StringComparison.Ordinal);
        Assert.Contains("Task.WhenAll(LoadCategoriesAsync(), LoadBrowseProductsAsync())", checkout, StringComparison.Ordinal);
    }

    [Fact]
    public void Catalog_product_list_loads_categories_and_products_in_parallel()
    {
        var catalog = File.ReadAllText(Path.Combine(
            MauiPages(),
            "Catalog",
            "CatalogProductsList.razor"));

        Assert.Contains("Task.WhenAll(categoriesTask, productsTask)", catalog, StringComparison.Ordinal);
        Assert.Contains("ListProductsAsync", catalog, StringComparison.Ordinal);
    }

    [Fact]
    public void Incremental_credit_download_does_not_n_plus_one_credit_summaries()
    {
        var sync = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "src",
            "Products",
            "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Application",
            "Offline",
            "CustomerCreditOfflineSyncService.cs"));

        Assert.Contains("RebuildLocalBalancesAsync", sync, StringComparison.Ordinal);
        Assert.DoesNotContain(
            "await RefreshCustomerFinancialsFromServerAsync(customerId, ct)",
            ExtractMethod(sync, "DownloadCreditsAsync"),
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            "await RefreshCustomerFinancialsFromServerAsync(customerId, ct)",
            ExtractMethod(sync, "DownloadRepaymentsAsync"),
            StringComparison.Ordinal);
        Assert.Contains("WaitAsync(0, ct)", ExtractMethod(sync, "DownloadIncrementalAsync"), StringComparison.Ordinal);
    }

    [Fact]
    public void Product_lists_do_not_block_on_live_platform_image_meta()
    {
        var catalog = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "src",
            "Products",
            "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Application",
            "Catalog",
            "CatalogProductUseCases.cs"));
        var listAsync = catalog.Substring(
            catalog.IndexOf("public async Task<PagedResult<PosCatalogProductDto>> ListAsync", StringComparison.Ordinal));
        listAsync = listAsync[..listAsync.IndexOf("/// <summary>Exact SKU lookup", StringComparison.Ordinal)];
        Assert.DoesNotContain("TryGetVersions", listAsync, StringComparison.Ordinal);
        Assert.Contains("livePlatformImageVersion: null", listAsync, StringComparison.Ordinal);

        var storefront = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "src",
            "Products",
            "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Application",
            "CustomerOrdering",
            "GetCustomerStorefront.cs"));
        Assert.Contains("TryGetVersionsBestEffortAsync", storefront, StringComparison.Ordinal);
    }

    [Fact]
    public void Sign_in_does_not_bind_organization_under_the_login_spinner()
    {
        var signIn = File.ReadAllText(Path.Combine(MauiPages(), "SignIn.razor"));
        var navigate = ExtractMethod(signIn, "NavigateAfterSignInAsync");
        Assert.Contains("ListEligibleOrganizationsAsync", navigate, StringComparison.Ordinal);
        Assert.Contains("Nav.NavigateTo(\"/organization-select\", replace: true)", navigate, StringComparison.Ordinal);
        Assert.DoesNotContain("SelectOrganizationAsync", navigate, StringComparison.Ordinal);
        Assert.DoesNotContain("CancellationTokenSource(TimeSpan.FromSeconds(20))", navigate, StringComparison.Ordinal);
        Assert.DoesNotContain("Gate.ResolveStartRouteAsync", navigate, StringComparison.Ordinal);
        Assert.Contains("RequiresOfflinePinEnrollmentAsync", navigate, StringComparison.Ordinal);
        Assert.Contains("EvaluateCurrentUserOfflinePinReadinessAsync", signIn, StringComparison.Ordinal);
        Assert.Contains("EligibleListCacheTtl", File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "src",
            "Products",
            "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Application",
            "Auth",
            "ProductAccessResolver.cs")), StringComparison.Ordinal);
    }

    [Fact]
    public void Organization_select_paints_before_auto_bind_and_auth_shell_quiets_header_http()
    {
        var orgSelect = File.ReadAllText(Path.Combine(MauiPages(), "OrganizationSelect.razor"));
        var load = ExtractMethod(orgSelect, "LoadAsync");
        var loadingFalse = load.IndexOf("_loading = false;", StringComparison.Ordinal);
        var autoEnter = load.IndexOf("await SelectAsAsync(", StringComparison.Ordinal);
        Assert.True(loadingFalse >= 0, "LoadAsync must clear the full-page spinner.");
        Assert.True(autoEnter > loadingFalse, "Single-org auto-bind must not hold the Loading... spinner.");
        Assert.Contains("await InvokeAsync(StateHasChanged);", load, StringComparison.Ordinal);

        var auth = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "src",
            "Products",
            "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Maui",
            "Components",
            "Layout",
            "AuthShell.razor"));
        Assert.Contains("QuietHeaderNetwork", auth, StringComparison.Ordinal);
        Assert.Contains("ShowNotifications=\"@(quietHeaderNetwork ? false : (bool?)null)\"", auth, StringComparison.Ordinal);
        Assert.Contains("IdentityState.UseOrgSelectChrome ? null : EffectiveOrgId", auth, StringComparison.Ordinal);
    }

    private static string ExtractMethod(string source, string methodName)
    {
        var start = source.IndexOf($"private async Task {methodName}(", StringComparison.Ordinal);
        if (start < 0)
        {
            start = source.IndexOf($"public async Task {methodName}(", StringComparison.Ordinal);
        }

        if (start < 0)
        {
            start = source.IndexOf($"protected override async Task {methodName}(", StringComparison.Ordinal);
        }

        Assert.True(start >= 0, $"{methodName} not found.");
        var brace = source.IndexOf('{', start);
        Assert.True(brace > start);
        var depth = 0;
        for (var i = brace; i < source.Length; i++)
        {
            if (source[i] == '{')
            {
                depth++;
            }
            else if (source[i] == '}')
            {
                depth--;
                if (depth == 0)
                {
                    return source[start..(i + 1)];
                }
            }
        }

        throw new InvalidOperationException($"Could not extract {methodName}.");
    }

    private static string MauiPages() => Path.Combine(
        FindRepoRoot(),
        "src",
        "Products",
        "PinoyBusinessPOS",
        "ExItS.PinoyBusinessPOS.Maui",
        "Components",
        "Pages");

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
