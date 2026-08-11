using ExItS.Platform.Application.GlobalCatalog;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.GlobalCatalog;
using ExItS.Platform.Infrastructure;
using ExItS.Platform.IntegrationTests.Support;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace ExItS.Platform.IntegrationTests;

[Collection(PostgreSqlCollection.Name)]
public sealed class EnsurePhilippinePosStarterCatalogTests(PostgreSqlFixture fixture)
{
    [Fact]
    public async Task Ensure_seeds_sixteen_types_templates_and_is_idempotent()
    {
        await using var provider = Build(fixture.ConnectionString);
        using var scope = provider.CreateScope();
        var ensure = scope.ServiceProvider.GetRequiredService<EnsurePhilippinePosStarterCatalog>();
        var businessTypes = scope.ServiceProvider.GetRequiredService<IBusinessTypeRepository>();
        var products = scope.ServiceProvider.GetRequiredService<IGlobalProductRepository>();
        var templates = scope.ServiceProvider.GetRequiredService<CatalogTemplateQueryService>();

        var first = await ensure.ExecuteAsync();
        Assert.True(first.BusinessTypesAdded + first.BusinessTypesUpdated >= 0);
        Assert.True(first.ProductsAdded > 0);
        Assert.True(first.TemplatesAdded > 0);
        Assert.True(first.TemplateLinksAdded > 0);

        var (btItems, btTotal) = await businessTypes.ListAsync(
            BusinessTypeStatus.Active, search: null, skip: 0, take: 100);
        Assert.True(btTotal >= 16);
        foreach (var seed in PhilippineBusinessTypeSeeds.All)
        {
            Assert.Contains(btItems, b => b.Code == seed.Code && b.Status == BusinessTypeStatus.Active);
        }

        var listedTemplates = await templates.ListAsync(
            CatalogTemplateStatus.Published,
            primaryBusinessTypeId: null,
            primaryBusinessTypeCode: null,
            search: "starter",
            page: 1,
            pageSize: 50);
        foreach (var def in PhilippinePosStarterCatalogData.Templates)
        {
            Assert.Contains(
                listedTemplates.Items,
                t => string.Equals(t.Slug, def.Slug, StringComparison.OrdinalIgnoreCase)
                     && string.Equals(t.PrimaryBusinessType, def.PrimaryBusinessTypeCode, StringComparison.OrdinalIgnoreCase));
        }

        var tomato = await FindBySkuAsync(products, "PH-VEG-TOMATO");
        Assert.NotNull(tomato);
        Assert.Equal(ProductSellingMode.ByWeight, tomato!.SellingMode);
        Assert.Equal(ProductUnit.Kilogram, tomato.Unit);
        Assert.Null(tomato.Barcode);

        var water = await FindBySkuAsync(products, "PH-BEV-WATER-500");
        Assert.NotNull(water);
        Assert.Equal(ProductSellingMode.PerItem, water!.SellingMode);
        Assert.True(water.BusinessTypeIds.Count >= 5);
        Assert.Null(water.Barcode);

        var second = await ensure.ExecuteAsync();
        Assert.Equal(0, second.BusinessTypesAdded);
        Assert.Equal(0, second.ProductsAdded);
        Assert.Equal(0, second.TemplatesAdded);
        Assert.Equal(0, second.TemplateLinksAdded);

        var (btAfter, _) = await businessTypes.ListAsync(
            BusinessTypeStatus.Active, search: null, skip: 0, take: 100);
        Assert.Equal(
            btItems.Count(b => PhilippineBusinessTypeSeeds.All.Any(s => s.Code == b.Code)),
            btAfter.Count(b => PhilippineBusinessTypeSeeds.All.Any(s => s.Code == b.Code)));

        var phCount = 0;
        foreach (var def in PhilippinePosStarterCatalogData.Products)
        {
            var found = await FindBySkuAsync(products, def.Sku);
            Assert.NotNull(found);
            Assert.Null(found!.Barcode);
            phCount++;
        }

        Assert.Equal(PhilippinePosStarterCatalogData.Products.Count, phCount);
    }

    [Fact]
    public async Task Merchant_entitlement_filtering_hides_unentitled_starter_templates_and_products()
    {
        await using var provider = Build(fixture.ConnectionString);
        using (var seedScope = provider.CreateScope())
        {
            await seedScope.ServiceProvider.GetRequiredService<EnsurePhilippinePosStarterCatalog>().ExecuteAsync();
        }

        using var scope = provider.CreateScope();
        var businessTypes = scope.ServiceProvider.GetRequiredService<IBusinessTypeRepository>();
        var sari = await businessTypes.GetByCodeAsync(LegacyBusinessTypeSeeds.SariSariCode);
        var fish = await businessTypes.GetByCodeAsync(PhilippineBusinessTypeSeeds.FishVendorCode);
        Assert.NotNull(sari);
        Assert.NotNull(fish);

        var products = scope.ServiceProvider.GetRequiredService<GlobalProductQueryService>();
        var templates = scope.ServiceProvider.GetRequiredService<CatalogTemplateQueryService>();

        var waterScoped = await products.ListAsync(
            GlobalProductStatus.Active,
            categoryId: null,
            businessTypeId: null,
            businessTypeCode: null,
            search: null,
            barcode: null,
            sku: "PH-BEV-WATER-500",
            page: 1,
            pageSize: 10,
            allowedBusinessTypeIds: [sari!.Id.Value]);
        Assert.Contains(waterScoped.Items, i => i.Sku == "PH-BEV-WATER-500");

        var bangusHidden = await products.ListAsync(
            GlobalProductStatus.Active,
            categoryId: null,
            businessTypeId: null,
            businessTypeCode: null,
            search: null,
            barcode: null,
            sku: "PH-FISH-BANGUS",
            page: 1,
            pageSize: 10,
            allowedBusinessTypeIds: [sari.Id.Value]);
        Assert.Empty(bangusHidden.Items);

        var tomatoHidden = await products.ListAsync(
            GlobalProductStatus.Active,
            categoryId: null,
            businessTypeId: null,
            businessTypeCode: null,
            search: null,
            barcode: null,
            sku: "PH-VEG-TOMATO",
            page: 1,
            pageSize: 10,
            allowedBusinessTypeIds: [sari.Id.Value]);
        Assert.Empty(tomatoHidden.Items);

        var driedVisible = await products.ListAsync(
            GlobalProductStatus.Active,
            categoryId: null,
            businessTypeId: null,
            businessTypeCode: null,
            search: null,
            barcode: null,
            sku: "PH-FISH-DRIED-FISH",
            page: 1,
            pageSize: 10,
            allowedBusinessTypeIds: [sari.Id.Value]);
        Assert.Contains(driedVisible.Items, i => i.Sku == "PH-FISH-DRIED-FISH");

        var fishOnly = await products.ListAsync(
            GlobalProductStatus.Active,
            categoryId: null,
            businessTypeId: null,
            businessTypeCode: null,
            search: null,
            barcode: null,
            sku: "PH-FISH-BANGUS",
            page: 1,
            pageSize: 10,
            allowedBusinessTypeIds: [fish!.Id.Value]);
        Assert.Contains(fishOnly.Items, i => i.Sku == "PH-FISH-BANGUS");
        Assert.All(fishOnly.Items, i => Assert.Contains(fish.Id.Value, i.BusinessTypeIds));

        var merchantTemplates = await templates.ListPublishedForMerchantsAsync(
            primaryBusinessTypeId: null,
            primaryBusinessTypeCode: null,
            search: "starter",
            page: 1,
            pageSize: 50,
            allowedPrimaryBusinessTypeIds: [sari.Id.Value]);
        Assert.Contains(merchantTemplates.Items, t => t.Slug == "sari-sari-starter");
        Assert.DoesNotContain(merchantTemplates.Items, t => t.Slug == "fish-vendor-starter");

        var adminTemplates = await templates.ListAsync(
            CatalogTemplateStatus.Published,
            primaryBusinessTypeId: null,
            primaryBusinessTypeCode: null,
            search: "fish-vendor-starter",
            page: 1,
            pageSize: 20);
        Assert.Contains(adminTemplates.Items, t => t.Slug == "fish-vendor-starter");
    }

    private static ServiceProvider Build(string connectionString)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:PlatformDatabase"] = connectionString
            })
            .Build();

        var services = new ServiceCollection();
        services.AddPlatformPersistence(configuration);
        services.AddScoped<EnsurePhilippinePosStarterCatalog>();
        services.AddScoped<BusinessTypeQueryService>();
        services.AddScoped<GlobalCategoryQueryService>();
        services.AddScoped<GlobalProductQueryService>();
        services.AddScoped<CatalogTemplateQueryService>();
        services.AddSingleton<IClock>(new GlobalCatalogTestServices.MutableClock(
            new DateTimeOffset(2026, 8, 11, 12, 0, 0, TimeSpan.Zero)));
        services.AddSingleton(typeof(Microsoft.Extensions.Logging.ILogger<>), typeof(NullLogger<>));
        services.AddLogging();
        return services.BuildServiceProvider();
    }

    private static async Task<GlobalProduct?> FindBySkuAsync(IGlobalProductRepository products, string sku)
    {
        var (items, _) = await products.ListAsync(
            status: null,
            categoryId: null,
            businessTypeId: null,
            businessTypeCode: null,
            search: null,
            barcode: null,
            sku: sku,
            skip: 0,
            take: 5);
        return items.FirstOrDefault(p => string.Equals(p.Sku, sku, StringComparison.OrdinalIgnoreCase));
    }
}
