using ExItS.Platform.Admin.Models;

namespace ExItS.Platform.Admin.UnitTests;

public sealed class P20BusinessTypesAdminTests
{
    [Fact]
    public void Business_types_admin_page_exists_with_routes_and_permission_gates()
    {
        var root = FindRepositoryRoot();
        var page = File.ReadAllText(Path.Combine(
            root, "src", "Platform", "ExItS.Platform.Admin", "Components", "Pages", "GlobalCatalogBusinessTypes.razor"));

        Assert.Contains("@page \"/admin/global-catalog/business-types\"", page, StringComparison.Ordinal);
        Assert.Contains("@page \"/admin/global-catalog/business-types/{Id:guid}\"", page, StringComparison.Ordinal);
        Assert.Contains("ViewGlobalCatalog", page, StringComparison.Ordinal);
        Assert.Contains("ManageGlobalCategories", page, StringComparison.Ordinal);
        Assert.Contains("UnauthorizedPanel", page, StringComparison.Ordinal);
        Assert.Contains("GetBusinessTypesAsync", page, StringComparison.Ordinal);
        Assert.Contains("CreateBusinessTypeAsync", page, StringComparison.Ordinal);
        Assert.Contains("SetBusinessTypeStatusAsync", page, StringComparison.Ordinal);
        Assert.Contains("Archived", page, StringComparison.Ordinal);
        Assert.Contains("Resizable", page, StringComparison.Ordinal);
        Assert.Contains("AdminTableSort.ApplyChangeAsync", page, StringComparison.Ordinal);
        Assert.DoesNotContain("/api/v1/platform/catalog/", page, StringComparison.Ordinal);
        Assert.DoesNotContain("DeleteBusinessType", page, StringComparison.Ordinal);
    }

    [Fact]
    public void Admin_nav_and_related_pages_wire_dynamic_business_types()
    {
        var root = FindRepositoryRoot();
        var pages = Path.Combine(root, "src", "Platform", "ExItS.Platform.Admin", "Components", "Pages");
        var nav = File.ReadAllText(Path.Combine(
            root, "src", "Platform", "ExItS.Platform.Admin", "Components", "Layout", "AdminNav.razor"));
        var categories = File.ReadAllText(Path.Combine(pages, "GlobalCatalogCategories.razor"));
        var products = File.ReadAllText(Path.Combine(pages, "GlobalCatalogProducts.razor"));
        var templates = File.ReadAllText(Path.Combine(pages, "GlobalCatalogTemplates.razor"));
        var iface = File.ReadAllText(Path.Combine(
            root, "src", "Platform", "ExItS.Platform.Admin", "Services", "IPlatformApiClient.cs"));
        var client = File.ReadAllText(Path.Combine(
            root, "src", "Platform", "ExItS.Platform.Admin", "Services", "PlatformApiClient.cs"));
        var dtos = File.ReadAllText(Path.Combine(
            root, "src", "Platform", "ExItS.Platform.Admin", "Models", "PlatformDtos.cs"));

        Assert.Contains("/admin/global-catalog/business-types", nav, StringComparison.Ordinal);
        Assert.Contains("Nav_GlobalBusinessTypes", nav, StringComparison.Ordinal);

        Assert.Contains("GetBusinessTypesAsync", categories, StringComparison.Ordinal);
        Assert.Contains("BulkAssignCategoryBusinessTypesAsync", categories, StringComparison.Ordinal);
        Assert.Contains("BusinessTypeIds", categories, StringComparison.Ordinal);
        Assert.DoesNotContain("SariSari\", \"MiniGrocery\"", categories, StringComparison.Ordinal);

        Assert.Contains("GetBusinessTypesAsync", products, StringComparison.Ordinal);
        Assert.Contains("BusinessTypeIds", products, StringComparison.Ordinal);
        Assert.DoesNotContain("SariSari\", \"MiniGrocery\"", products, StringComparison.Ordinal);

        Assert.Contains("GetBusinessTypesAsync", templates, StringComparison.Ordinal);
        Assert.Contains("PrimaryBusinessTypeId", templates, StringComparison.Ordinal);
        Assert.Contains("GlobalBusinessTypes_HistoricalTypeNote", templates, StringComparison.Ordinal);
        Assert.DoesNotContain("SariSari\", \"MiniGrocery\"", templates, StringComparison.Ordinal);

        Assert.Contains("GetBusinessTypesAsync", iface, StringComparison.Ordinal);
        Assert.Contains("BulkAssignCategoryBusinessTypesAsync", iface, StringComparison.Ordinal);
        Assert.Contains("/api/v1/platform/global-catalog/business-types", client, StringComparison.Ordinal);
        Assert.Contains("businessTypeCode", client, StringComparison.Ordinal);

        Assert.Contains("record BusinessTypeDto", dtos, StringComparison.Ordinal);
        Assert.Contains("IReadOnlyList<Guid> BusinessTypeIds", dtos, StringComparison.Ordinal);
        Assert.Contains("Guid PrimaryBusinessTypeId", dtos, StringComparison.Ordinal);
        Assert.Contains(PlatformPermissionCodes.ManageGlobalCategories, PlatformPermissionCodes.All);
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
