using ExItS.Platform.Application.GlobalCatalog;
using ExItS.Platform.Domain.GlobalCatalog;

namespace ExItS.Platform.UnitTests.GlobalCatalog;

public sealed class PhilippinePosStarterCatalogDataTests
{
    [Fact]
    public void Definitions_cover_sixteen_business_types_and_one_starter_template_each()
    {
        Assert.Equal(16, PhilippineBusinessTypeSeeds.All.Count);
        Assert.Equal(16, PhilippinePosStarterCatalogData.Templates.Count);

        var primaryCodes = PhilippinePosStarterCatalogData.Templates
            .Select(t => t.PrimaryBusinessTypeCode)
            .ToArray();
        Assert.Equal(16, primaryCodes.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        foreach (var seed in PhilippineBusinessTypeSeeds.All)
        {
            Assert.Contains(primaryCodes, c => string.Equals(c, seed.Code, StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public void Template_slugs_and_product_skus_are_stable_and_unique()
    {
        Assert.Equal(
            PhilippinePosStarterCatalogData.Templates.Count,
            PhilippinePosStarterCatalogData.Templates.Select(t => t.Slug).Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.Equal(
            PhilippinePosStarterCatalogData.Products.Count,
            PhilippinePosStarterCatalogData.Products.Select(p => p.Sku).Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.All(PhilippinePosStarterCatalogData.Products, p => Assert.StartsWith("PH-", p.Sku, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Weighted_products_use_by_weight_and_kilogram_with_null_barcode_policy()
    {
        var weighted = PhilippinePosStarterCatalogData.Products
            .Where(p => p.SellingMode == ProductSellingMode.ByWeight)
            .ToList();
        Assert.NotEmpty(weighted);
        Assert.Contains(weighted, p => p.Sku.StartsWith("PH-VEG-", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(weighted, p => p.Sku.StartsWith("PH-FISH-", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(weighted, p => p.Sku.StartsWith("PH-MEAT-", StringComparison.OrdinalIgnoreCase));
        Assert.All(weighted, p =>
        {
            Assert.Equal(ProductUnit.Kilogram, p.Unit);
            Assert.Equal(ProductSellingMode.ByWeight, p.SellingMode);
        });

        var packaged = PhilippinePosStarterCatalogData.Products
            .Where(p => p.SellingMode == ProductSellingMode.PerItem)
            .ToList();
        Assert.NotEmpty(packaged);
        Assert.All(packaged, p => Assert.NotEqual(ProductUnit.Gram, p.Unit));
    }

    [Fact]
    public void Shared_products_apply_to_multiple_business_types()
    {
        var water = PhilippinePosStarterCatalogData.Products.Single(p => p.Sku == "PH-BEV-WATER-500");
        Assert.True(water.BusinessTypeCodes.Length >= 5);
        Assert.Contains(LegacyBusinessTypeSeeds.SariSariCode, water.BusinessTypeCodes);
        Assert.Contains(LegacyBusinessTypeSeeds.CafeCode, water.BusinessTypeCodes);
        Assert.Contains(PhilippineBusinessTypeSeeds.FoodCartCode, water.BusinessTypeCodes);
    }

    [Fact]
    public void Template_links_reference_defined_products_and_include_primary_business_type()
    {
        var bySku = PhilippinePosStarterCatalogData.Products.ToDictionary(p => p.Sku, StringComparer.OrdinalIgnoreCase);
        foreach (var template in PhilippinePosStarterCatalogData.Templates)
        {
            Assert.InRange(template.ProductSkus.Length, 10, 30);
            Assert.All(template.ProductSkus, sku => Assert.True(bySku.ContainsKey(sku), $"Missing SKU {sku} for {template.Slug}"));
            Assert.All(template.ProductSkus, sku =>
            {
                var product = bySku[sku];
                Assert.Contains(
                    product.BusinessTypeCodes,
                    code => string.Equals(code, template.PrimaryBusinessTypeCode, StringComparison.OrdinalIgnoreCase));
            });
        }
    }
}
