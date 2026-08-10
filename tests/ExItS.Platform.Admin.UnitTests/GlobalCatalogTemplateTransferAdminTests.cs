using ExItS.Platform.Admin.Models;

namespace ExItS.Platform.Admin.UnitTests;

public sealed class GlobalCatalogTemplateTransferAdminTests
{
    [Fact]
    public void Template_composition_uses_transfer_list_not_product_dropdown()
    {
        var root = FindRepositoryRoot();
        var page = File.ReadAllText(Path.Combine(
            root, "src", "Platform", "ExItS.Platform.Admin", "Components", "Pages", "GlobalCatalogTemplates.razor"));
        var transfer = File.ReadAllText(Path.Combine(
            root, "src", "Platform", "ExItS.Platform.Admin", "Components", "Pages", "CatalogTemplateCompositionTransfer.razor"));

        Assert.Contains("CatalogTemplateCompositionTransfer", page, StringComparison.Ordinal);
        Assert.DoesNotContain("LoadProductOptionsAsync", page, StringComparison.Ordinal);
        Assert.DoesNotContain("GetGlobalProductsAsync", page, StringComparison.Ordinal);
        Assert.DoesNotContain("_assignProductId", page, StringComparison.Ordinal);
        Assert.DoesNotContain("GlobalTemplates_AssignProduct", page, StringComparison.Ordinal);
        Assert.Contains("GetBusinessTypesAsync", page, StringComparison.Ordinal);

        Assert.Contains("GetCatalogTemplateAvailableProductsAsync", transfer, StringComparison.Ordinal);
        Assert.Contains("BulkAssignCatalogTemplateProductsAsync", transfer, StringComparison.Ordinal);
        Assert.Contains("BulkRemoveCatalogTemplateProductsAsync", transfer, StringComparison.Ordinal);
        Assert.Contains("exits-template-transfer", transfer, StringComparison.Ordinal);
        Assert.Contains("status: \"Active\"", transfer, StringComparison.Ordinal);
        Assert.Contains("Unavailable product", transfer, StringComparison.Ordinal);
        Assert.DoesNotContain("pageSize: 200", transfer, StringComparison.Ordinal);
        Assert.DoesNotContain("ProductLabel", transfer, StringComparison.Ordinal);
    }

    [Fact]
    public void Api_client_exposes_available_products_and_bulk_composition_routes()
    {
        var root = FindRepositoryRoot();
        var client = File.ReadAllText(Path.Combine(
            root, "src", "Platform", "ExItS.Platform.Admin", "Services", "PlatformApiClient.cs"));
        var iface = File.ReadAllText(Path.Combine(
            root, "src", "Platform", "ExItS.Platform.Admin", "Services", "IPlatformApiClient.cs"));

        Assert.Contains("/available-products", client, StringComparison.Ordinal);
        Assert.Contains("/products/bulk", client, StringComparison.Ordinal);
        Assert.Contains("/products/bulk-remove", client, StringComparison.Ordinal);
        Assert.Contains("GetCatalogTemplateAvailableProductsAsync", iface, StringComparison.Ordinal);
        Assert.Contains("BulkAssignCatalogTemplateProductsAsync", iface, StringComparison.Ordinal);
        Assert.Contains("BulkRemoveCatalogTemplateProductsAsync", iface, StringComparison.Ordinal);
    }

    [Fact]
    public void Catalog_template_product_dto_carries_human_readable_metadata()
    {
        var dto = new CatalogTemplateProductDto(
            Guid.NewGuid(),
            Guid.NewGuid(),
            0,
            true,
            true,
            ProductName: "Century Tuna",
            Sku: "SKU-1",
            Barcode: "123",
            CategoryName: "Canned",
            Status: "Active",
            Unit: "Piece");

        Assert.Equal("Century Tuna", dto.ProductName);
        Assert.Equal("SKU-1", dto.Sku);
        Assert.Equal("Active", dto.Status);
    }

    [Fact]
    public void Transfer_localization_keys_exist_in_english_and_tagalog()
    {
        var root = FindRepositoryRoot();
        var en = File.ReadAllText(Path.Combine(
            root, "src", "Platform", "ExItS.Platform.Admin", "Localization", "AdminResources.resx"));
        var fil = File.ReadAllText(Path.Combine(
            root, "src", "Platform", "ExItS.Platform.Admin", "Localization", "AdminResources.fil-PH.resx"));

        string[] keys =
        [
            "GlobalTemplates_AvailablePane",
            "GlobalTemplates_AssignedPane",
            "GlobalTemplates_MoveRight",
            "GlobalTemplates_MoveLeft",
            "GlobalTemplates_ProductsAssigned",
            "GlobalTemplates_PublishChecklistTitle",
            "GlobalTemplates_PublishNeedProducts"
        ];

        foreach (var key in keys)
        {
            Assert.Contains($"name=\"{key}\"", en, StringComparison.Ordinal);
            Assert.Contains($"name=\"{key}\"", fil, StringComparison.Ordinal);
        }
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
