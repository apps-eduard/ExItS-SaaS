using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.GlobalCatalog;
using ExItS.Platform.IntegrationTests.Support;
using Microsoft.Extensions.DependencyInjection;

namespace ExItS.Platform.IntegrationTests;

/// <summary>
/// Covers template composition round-trips against PostgreSQL. Every operation runs in its own
/// scope so each call uses a fresh <c>DbContext</c>, matching one admin HTTP request.
/// </summary>
[Collection(PostgreSqlCollection.Name)]
public sealed class GlobalCatalogTemplateCompositionPersistenceTests(PostgreSqlFixture fixture)
{
    [Fact]
    public async Task Assigning_multiple_products_persists_full_composition()
    {
        await using var provider = GlobalCatalogTestServices.Build(fixture.ConnectionString);
        var template = await CreateTemplateAsync(provider, "Sari-Sari Starter");
        var tuna = await CreateProductAsync(provider, "Century Tuna Flakes 155g");
        var noodles = await CreateProductAsync(provider, "Lucky Me Pancit Canton");
        var coffee = await CreateProductAsync(provider, "Kopiko Brown 3-in-1");

        var afterFirst = await AssignAsync(provider, template.Id, tuna, template.UpdatedAtUtc, isFirstBatch: true);
        Assert.True(afterFirst.IsSuccess, afterFirst.ErrorMessage);

        var afterSecond = await AssignAsync(provider, template.Id, noodles, afterFirst.Value!.UpdatedAtUtc, isFirstBatch: true);
        Assert.True(afterSecond.IsSuccess, afterSecond.ErrorMessage);

        var afterThird = await AssignAsync(provider, template.Id, coffee, afterSecond.Value!.UpdatedAtUtc, isFeatured: true);
        Assert.True(afterThird.IsSuccess, afterThird.ErrorMessage);

        var reloaded = await GetTemplateAsync(provider, template.Id);
        Assert.Equal(3, reloaded.Products.Count);
        Assert.Equal([tuna, noodles, coffee], reloaded.Products.OrderBy(p => p.SortOrder).Select(p => p.GlobalProductId));
        Assert.Equal([0, 1, 2], reloaded.Products.OrderBy(p => p.SortOrder).Select(p => p.SortOrder));
        Assert.Equal(2, reloaded.FirstBatchCount);
        Assert.Single(reloaded.Products, p => p.IsFeatured);
    }

    [Fact]
    public async Task Assigning_an_already_assigned_product_is_rejected()
    {
        await using var provider = GlobalCatalogTestServices.Build(fixture.ConnectionString);
        var template = await CreateTemplateAsync(provider, "Duplicate Guard");
        var product = await CreateProductAsync(provider, "Datu Puti Vinegar 385ml");

        var first = await AssignAsync(provider, template.Id, product, template.UpdatedAtUtc);
        Assert.True(first.IsSuccess, first.ErrorMessage);

        var duplicate = await AssignAsync(provider, template.Id, product, first.Value!.UpdatedAtUtc);
        Assert.False(duplicate.IsSuccess);

        var reloaded = await GetTemplateAsync(provider, template.Id);
        Assert.Single(reloaded.Products);
    }

    [Fact]
    public async Task Removing_one_product_keeps_the_remaining_composition()
    {
        await using var provider = GlobalCatalogTestServices.Build(fixture.ConnectionString);
        var template = await CreateTemplateAsync(provider, "Removal Flow");
        var keep = await CreateProductAsync(provider, "Argentina Corned Beef 150g");
        var drop = await CreateProductAsync(provider, "Ligo Sardines 155g");

        var afterKeep = await AssignAsync(provider, template.Id, keep, template.UpdatedAtUtc);
        var afterDrop = await AssignAsync(provider, template.Id, drop, afterKeep.Value!.UpdatedAtUtc);
        Assert.True(afterDrop.IsSuccess, afterDrop.ErrorMessage);

        using (var scope = NewRequest(provider))
        {
            var result = await scope.ServiceProvider
                .GetRequiredService<RemoveCatalogTemplateProduct>()
                .ExecuteAsync(template.Id, drop, afterDrop.Value!.UpdatedAtUtc);
            Assert.True(result.IsSuccess, result.ErrorMessage);
        }

        var reloaded = await GetTemplateAsync(provider, template.Id);
        Assert.Single(reloaded.Products);
        Assert.Equal(keep, reloaded.Products[0].GlobalProductId);
    }

    [Fact]
    public async Task Updating_flags_persists_without_disturbing_other_rows()
    {
        await using var provider = GlobalCatalogTestServices.Build(fixture.ConnectionString);
        var template = await CreateTemplateAsync(provider, "Flag Flow");
        var first = await CreateProductAsync(provider, "Nescafe Classic 25g");
        var second = await CreateProductAsync(provider, "Bear Brand Powder 320g");

        var afterFirst = await AssignAsync(provider, template.Id, first, template.UpdatedAtUtc, isFirstBatch: true);
        var afterSecond = await AssignAsync(provider, template.Id, second, afterFirst.Value!.UpdatedAtUtc);
        Assert.True(afterSecond.IsSuccess, afterSecond.ErrorMessage);

        using (var scope = NewRequest(provider))
        {
            var result = await scope.ServiceProvider
                .GetRequiredService<UpdateCatalogTemplateProductFlags>()
                .ExecuteAsync(
                    template.Id,
                    second,
                    new UpdateCatalogTemplateProductFlagsRequest(
                        IsFeatured: true,
                        IsFirstBatch: null,
                        ExpectedUpdatedAtUtc: afterSecond.Value!.UpdatedAtUtc));
            Assert.True(result.IsSuccess, result.ErrorMessage);
        }

        var reloaded = await GetTemplateAsync(provider, template.Id);
        var firstRow = reloaded.Products.Single(p => p.GlobalProductId == first);
        var secondRow = reloaded.Products.Single(p => p.GlobalProductId == second);
        Assert.True(firstRow.IsFirstBatch);
        Assert.False(firstRow.IsFeatured);
        Assert.True(secondRow.IsFeatured);
        Assert.False(secondRow.IsFirstBatch);
    }

    [Fact]
    public async Task Reordering_products_persists_new_sort_order()
    {
        await using var provider = GlobalCatalogTestServices.Build(fixture.ConnectionString);
        var template = await CreateTemplateAsync(provider, "Reorder Flow");
        var first = await CreateProductAsync(provider, "Sky Flakes Crackers 25g");
        var second = await CreateProductAsync(provider, "Rebisco Sandwich 32g");

        var afterFirst = await AssignAsync(provider, template.Id, first, template.UpdatedAtUtc);
        var afterSecond = await AssignAsync(provider, template.Id, second, afterFirst.Value!.UpdatedAtUtc);
        Assert.True(afterSecond.IsSuccess, afterSecond.ErrorMessage);

        using (var scope = NewRequest(provider))
        {
            var result = await scope.ServiceProvider
                .GetRequiredService<ReorderCatalogTemplateProducts>()
                .ExecuteAsync(
                    template.Id,
                    new ReorderCatalogTemplateProductsRequest([second, first], afterSecond.Value!.UpdatedAtUtc));
            Assert.True(result.IsSuccess, result.ErrorMessage);
        }

        var reloaded = await GetTemplateAsync(provider, template.Id);
        Assert.Equal([second, first], reloaded.Products.OrderBy(p => p.SortOrder).Select(p => p.GlobalProductId));
    }

    [Fact]
    public async Task Stale_expected_timestamp_is_rejected_as_conflict()
    {
        await using var provider = GlobalCatalogTestServices.Build(fixture.ConnectionString);
        var template = await CreateTemplateAsync(provider, "Conflict Guard");
        var first = await CreateProductAsync(provider, "Alaska Evaporada 370ml");
        var second = await CreateProductAsync(provider, "Milo Activ-Go 300g");

        var afterFirst = await AssignAsync(provider, template.Id, first, template.UpdatedAtUtc);
        Assert.True(afterFirst.IsSuccess, afterFirst.ErrorMessage);

        var stale = await AssignAsync(provider, template.Id, second, template.UpdatedAtUtc);
        Assert.False(stale.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.ConcurrencyConflict, stale.ErrorCode);
    }

    private static IServiceScope NewRequest(ServiceProvider provider)
    {
        provider.GetRequiredService<GlobalCatalogTestServices.MutableClock>().Advance(TimeSpan.FromSeconds(5));
        return provider.CreateScope();
    }

    private static async Task<CatalogTemplateDto> CreateTemplateAsync(ServiceProvider provider, string name)
    {
        using var scope = NewRequest(provider);
        var result = await scope.ServiceProvider
            .GetRequiredService<CreateCatalogTemplate>()
            .ExecuteAsync(new CreateCatalogTemplateRequest(
                Name: $"{name} {Guid.NewGuid():N}"[..Math.Min(64, name.Length + 9)],
                PrimaryBusinessType: "SariSari",
                Slug: $"tpl-{Guid.NewGuid():N}",
                DefaultBatchSize: 50,
                SelectionMode: "Curated"));
        Assert.True(result.IsSuccess, result.ErrorMessage);
        return result.Value!;
    }

    private static async Task<Guid> CreateProductAsync(ServiceProvider provider, string name)
    {
        using var scope = NewRequest(provider);
        var result = await scope.ServiceProvider
            .GetRequiredService<CreateGlobalProduct>()
            .ExecuteAsync(new CreateGlobalProductRequest(
                Name: name,
                Unit: "Piece",
                Sku: $"SKU-{Guid.NewGuid():N}"[..20],
                BusinessTypes: ["SariSari"]));
        Assert.True(result.IsSuccess, result.ErrorMessage);
        return result.Value!.Id;
    }

    private static async Task<ApplicationResult<CatalogTemplateDto>> AssignAsync(
        ServiceProvider provider,
        Guid templateId,
        Guid productId,
        DateTimeOffset expectedUpdatedAtUtc,
        bool isFeatured = false,
        bool isFirstBatch = false)
    {
        using var scope = NewRequest(provider);
        return await scope.ServiceProvider
            .GetRequiredService<AssignCatalogTemplateProduct>()
            .ExecuteAsync(templateId, new AssignCatalogTemplateProductRequest(
                productId,
                isFeatured,
                isFirstBatch,
                SortOrder: null,
                ExpectedUpdatedAtUtc: expectedUpdatedAtUtc));
    }

    private static async Task<CatalogTemplateDto> GetTemplateAsync(ServiceProvider provider, Guid templateId)
    {
        using var scope = provider.CreateScope();
        var template = await scope.ServiceProvider
            .GetRequiredService<CatalogTemplateQueryService>()
            .GetByIdAsync(templateId);
        Assert.NotNull(template);
        return template!;
    }
}
