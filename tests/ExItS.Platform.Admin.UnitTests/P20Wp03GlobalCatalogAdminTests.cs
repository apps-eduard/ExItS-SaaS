using ExItS.Platform.Admin.Models;

namespace ExItS.Platform.Admin.UnitTests;

public sealed class P20Wp03GlobalCatalogAdminTests
{
    [Fact]
    public void Platform_permission_codes_include_global_catalog_permissions()
    {
        Assert.Equal("platform.permission.view_global_catalog", PlatformPermissionCodes.ViewGlobalCatalog);
        Assert.Equal("platform.permission.manage_global_categories", PlatformPermissionCodes.ManageGlobalCategories);
        Assert.Equal("platform.permission.manage_global_products", PlatformPermissionCodes.ManageGlobalProducts);
        Assert.Contains(PlatformPermissionCodes.ViewGlobalCatalog, PlatformPermissionCodes.All);
        Assert.Contains(PlatformPermissionCodes.ManageGlobalCategories, PlatformPermissionCodes.All);
        Assert.Contains(PlatformPermissionCodes.ManageGlobalProducts, PlatformPermissionCodes.All);
    }

    [Fact]
    public void Global_catalog_admin_pages_exist_with_routes_and_permission_gates()
    {
        var root = FindRepositoryRoot();
        var pages = Path.Combine(root, "src", "Platform", "ExItS.Platform.Admin", "Components", "Pages");
        var categories = File.ReadAllText(Path.Combine(pages, "GlobalCatalogCategories.razor"));
        var products = File.ReadAllText(Path.Combine(pages, "GlobalCatalogProducts.razor"));
        var templates = File.ReadAllText(Path.Combine(pages, "GlobalCatalogTemplates.razor"));
        var imports = File.ReadAllText(Path.Combine(pages, "GlobalCatalogImports.razor"));
        var transfer = File.ReadAllText(Path.Combine(pages, "CatalogTemplateCompositionTransfer.razor"));

        Assert.Contains("@page \"/admin/global-catalog/categories\"", categories, StringComparison.Ordinal);
        Assert.Contains("@page \"/admin/global-catalog/categories/{Id:guid}\"", categories, StringComparison.Ordinal);
        Assert.Contains("ViewGlobalCatalog", categories, StringComparison.Ordinal);
        Assert.Contains("ManageGlobalCategories", categories, StringComparison.Ordinal);
        Assert.Contains("UnauthorizedPanel", categories, StringComparison.Ordinal);
        Assert.Contains("GetGlobalCategoriesAsync", categories, StringComparison.Ordinal);
        Assert.DoesNotContain("/api/v1/platform/catalog/", categories, StringComparison.Ordinal);

        Assert.Contains("@page \"/admin/global-catalog/products\"", products, StringComparison.Ordinal);
        Assert.Contains("@page \"/admin/global-catalog/products/{Id:guid}\"", products, StringComparison.Ordinal);
        Assert.Contains("ViewGlobalCatalog", products, StringComparison.Ordinal);
        Assert.Contains("ManageGlobalProducts", products, StringComparison.Ordinal);
        Assert.Contains("UnauthorizedPanel", products, StringComparison.Ordinal);
        Assert.Contains("GetGlobalProductsAsync", products, StringComparison.Ordinal);
        Assert.Contains("GlobalProducts_Brand", products, StringComparison.Ordinal);
        Assert.Contains("AdminTableSort.ApplyChangeAsync", products, StringComparison.Ordinal);
        Assert.Contains("Resizable", products, StringComparison.Ordinal);
        Assert.Contains("GlobalProducts_CostPrice", products, StringComparison.Ordinal);
        Assert.Contains("GlobalProducts_SellingPrice", products, StringComparison.Ordinal);
        Assert.Contains("CostPrice", products, StringComparison.Ordinal);
        Assert.Contains("SellingPrice", products, StringComparison.Ordinal);
        Assert.Contains("@implements IDisposable", products, StringComparison.Ordinal);
        Assert.DoesNotContain("/api/v1/platform/catalog/", products, StringComparison.Ordinal);

        Assert.Contains("Resizable", categories, StringComparison.Ordinal);
        Assert.Contains("AdminTableSort.ApplyChangeAsync", categories, StringComparison.Ordinal);
        Assert.Contains("Resizable", templates, StringComparison.Ordinal);
        Assert.Contains("AdminTableSort.ApplyChangeAsync", templates, StringComparison.Ordinal);
        Assert.Contains("Resizable", imports, StringComparison.Ordinal);
        Assert.Contains("Resizable", transfer, StringComparison.Ordinal);
        Assert.Contains("AdminCurrency.FormatPhp", imports, StringComparison.Ordinal);
    }

    [Fact]
    public void Admin_nav_exposes_product_catalog_submenu_including_imports_and_templates()
    {
        var root = FindRepositoryRoot();
        var nav = File.ReadAllText(Path.Combine(
            root, "src", "Platform", "ExItS.Platform.Admin", "Components", "Layout", "AdminNav.razor"));

        Assert.Contains("ViewGlobalCatalog", nav, StringComparison.Ordinal);
        Assert.Contains("/admin/global-catalog/categories", nav, StringComparison.Ordinal);
        Assert.Contains("/admin/global-catalog/products", nav, StringComparison.Ordinal);
        Assert.Contains("Nav_ProductCatalog", nav, StringComparison.Ordinal);
        Assert.Contains("/admin/global-catalog/imports", nav, StringComparison.Ordinal);
        Assert.Contains("/admin/global-catalog/templates", nav, StringComparison.Ordinal);
        Assert.Contains("/admin/products", nav, StringComparison.Ordinal);
        Assert.Contains("/admin/plans", nav, StringComparison.Ordinal);
    }

    [Fact]
    public void Api_client_uses_global_catalog_routes_not_commercial_catalog()
    {
        var root = FindRepositoryRoot();
        var client = File.ReadAllText(Path.Combine(
            root, "src", "Platform", "ExItS.Platform.Admin", "Services", "PlatformApiClient.cs"));
        var iface = File.ReadAllText(Path.Combine(
            root, "src", "Platform", "ExItS.Platform.Admin", "Services", "IPlatformApiClient.cs"));

        Assert.Contains("/api/v1/platform/global-catalog/categories", client, StringComparison.Ordinal);
        Assert.Contains("/api/v1/platform/global-catalog/products", client, StringComparison.Ordinal);
        Assert.Contains("GetGlobalCategoriesAsync", iface, StringComparison.Ordinal);
        Assert.Contains("GetGlobalProductsAsync", iface, StringComparison.Ordinal);
        Assert.Contains("sortBy", client, StringComparison.Ordinal);
        Assert.Contains("/api/v1/platform/catalog/products", client, StringComparison.Ordinal);
        Assert.Contains("/api/v1/platform/catalog/plans", client, StringComparison.Ordinal);
    }

    private static string FindRepositoryRoot()
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

        throw new InvalidOperationException("Could not locate repository root.");
    }
}
