using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.GlobalCatalog;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.GlobalCatalog;

namespace ExItS.Platform.UnitTests.GlobalCatalog;

file static class GlobalCatalogTestProducts
{
    private static readonly DateTimeOffset T0 = new(2026, 8, 5, 12, 0, 0, TimeSpan.Zero);

    public static GlobalProduct Make(string name, string sku, string barcode = "480001", decimal cost = 10m, decimal selling = 15m) =>
        GlobalProduct.Create(
            name,
            ProductUnit.Piece,
            sku,
            barcode,
            "BrandX",
            GlobalCategory.Create("General", T0).Id,
            T0,
            cost,
            selling);
}

public sealed class CatalogTemplateDomainTests
{
    private static readonly DateTimeOffset T0 = new(2026, 8, 5, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_normalizes_slug_and_defaults_draft_curated()
    {
        var template = CatalogTemplate.Create(
            "  Sari Sari Starter  ",
            BusinessTypeId.From(LegacyBusinessTypeSeeds.SariSariId),
            T0,
            slug: "Sari Sari Starter");

        Assert.Equal("Sari Sari Starter", template.Name);
        Assert.Equal("sari-sari-starter", template.Slug);
        Assert.Equal(CatalogTemplateStatus.Draft, template.Status);
        Assert.Equal(SelectionMode.Curated, template.SelectionMode);
        Assert.Equal(50, template.DefaultBatchSize);
        Assert.Null(template.PublishedAtUtc);
    }

    [Fact]
    public void Publish_requires_products_and_sets_published_at()
    {
        var template = CatalogTemplate.Create("Mini Grocery", BusinessTypeId.From(LegacyBusinessTypeSeeds.MiniGroceryId), T0);
        var ex = Assert.Throws<DomainException>(() => template.Publish(T0.AddMinutes(1)));
        Assert.Equal(DomainErrorCodes.CatalogTemplatePublishRequiresProducts, ex.ErrorCode);

        var productId = GlobalProductId.New();
        template.AssignProduct(productId, T0.AddMinutes(2), isFirstBatch: true);
        template.Publish(T0.AddMinutes(3));

        Assert.Equal(CatalogTemplateStatus.Published, template.Status);
        Assert.Equal(T0.AddMinutes(3), template.PublishedAtUtc);
        Assert.Equal(1, template.FirstBatchCount);
    }

    [Fact]
    public void Unpublish_and_archive_lifecycle()
    {
        var template = CatalogTemplate.Create("Cafe Starter", BusinessTypeId.From(LegacyBusinessTypeSeeds.CafeId), T0);
        template.AssignProduct(GlobalProductId.New(), T0.AddMinutes(1));
        template.Publish(T0.AddMinutes(2));
        template.Unpublish(T0.AddMinutes(3));
        Assert.Equal(CatalogTemplateStatus.Draft, template.Status);

        template.Publish(T0.AddMinutes(4));
        template.Archive(T0.AddMinutes(5));
        Assert.Equal(CatalogTemplateStatus.Archived, template.Status);

        var ex = Assert.Throws<DomainException>(() =>
            template.AssignProduct(GlobalProductId.New(), T0.AddMinutes(6)));
        Assert.Equal(DomainErrorCodes.InvalidCatalogTemplateStatusTransition, ex.ErrorCode);
    }

    [Fact]
    public void AssignProduct_enforces_unique_global_product()
    {
        var template = CatalogTemplate.Create("Dup Check", BusinessTypeId.From(LegacyBusinessTypeSeeds.GeneralRetailId), T0);
        var productId = GlobalProductId.New();
        template.AssignProduct(productId, T0.AddMinutes(1));

        var ex = Assert.Throws<DomainException>(() =>
            template.AssignProduct(productId, T0.AddMinutes(2)));
        Assert.Equal(DomainErrorCodes.CatalogTemplateProductDuplicate, ex.ErrorCode);
    }

    [Fact]
    public void TryAssignProduct_is_idempotent_noop_on_duplicate()
    {
        var template = CatalogTemplate.Create("Bakery", BusinessTypeId.From(LegacyBusinessTypeSeeds.BakeryId), T0);
        var productId = GlobalProductId.New();
        Assert.True(template.TryAssignProduct(productId, T0.AddMinutes(1)));
        Assert.False(template.TryAssignProduct(productId, T0.AddMinutes(2)));
        Assert.Equal(1, template.ProductCount);
    }

    [Fact]
    public void ReorderProducts_updates_sort_order()
    {
        var template = CatalogTemplate.Create("Order Check", BusinessTypeId.From(LegacyBusinessTypeSeeds.BakeryId), T0);
        var a = GlobalProductId.New();
        var b = GlobalProductId.New();
        var c = GlobalProductId.New();
        template.AssignProduct(a, T0.AddMinutes(1));
        template.AssignProduct(b, T0.AddMinutes(2));
        template.AssignProduct(c, T0.AddMinutes(3));

        template.ReorderProducts([c, a, b], T0.AddMinutes(4));

        Assert.Equal([c.Value, a.Value, b.Value], template.Products.Select(p => p.GlobalProductId.Value));
        Assert.Equal([0, 1, 2], template.Products.Select(p => p.SortOrder));
    }

    [Fact]
    public void ReorderProducts_rejects_incomplete_list()
    {
        var template = CatalogTemplate.Create("Order Bad", BusinessTypeId.From(LegacyBusinessTypeSeeds.PharmacyId), T0);
        var a = GlobalProductId.New();
        var b = GlobalProductId.New();
        template.AssignProduct(a, T0.AddMinutes(1));
        template.AssignProduct(b, T0.AddMinutes(2));

        var ex = Assert.Throws<DomainException>(() =>
            template.ReorderProducts([a], T0.AddMinutes(3)));
        Assert.Equal(DomainErrorCodes.CatalogTemplateCompositionOrderInvalid, ex.ErrorCode);
    }

    [Fact]
    public void Published_template_composition_edits_remain_allowed()
    {
        var template = CatalogTemplate.Create("Live Edit", BusinessTypeId.From(LegacyBusinessTypeSeeds.SariSariId), T0);
        var first = GlobalProductId.New();
        template.AssignProduct(first, T0.AddMinutes(1), isFeatured: true, isFirstBatch: true);
        template.Publish(T0.AddMinutes(2));

        var second = GlobalProductId.New();
        template.AssignProduct(second, T0.AddMinutes(3), isFirstBatch: true);
        template.SetProductFlags(first, T0.AddMinutes(4), isFeatured: false);
        template.ReorderProducts([second, first], T0.AddMinutes(5));

        Assert.Equal(CatalogTemplateStatus.Published, template.Status);
        Assert.Equal(2, template.ProductCount);
        Assert.False(template.Products.First(p => p.GlobalProductId == first).IsFeatured);
    }
}

public sealed class CatalogTemplateUseCaseTests
{
    private static readonly DateTimeOffset T0 = new(2026, 8, 5, 14, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Publish_use_case_blocks_empty_template()
    {
        var templates = new InMemoryCatalogTemplateRepository();
        var clock = new FixedClock(T0);
        var create = new CreateCatalogTemplate(templates, new FakeBusinessTypeRepository(), new NoOpUnitOfWork(), clock);
        var created = await create.ExecuteAsync(new CreateCatalogTemplateRequest(
            "Empty Starter",
            LegacyBusinessTypeSeeds.SariSariCode));
        Assert.True(created.IsSuccess);

        var publish = new PublishCatalogTemplate(templates, new NoOpUnitOfWork(), clock);
        var result = await publish.ExecuteAsync(created.Value!.Id);
        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrorCodes.CatalogTemplatePublishRequiresProducts, result.ErrorCode);
    }

    [Fact]
    public async Task Assign_rejects_duplicate_product_via_use_case()
    {
        var templates = new InMemoryCatalogTemplateRepository();
        var products = new InMemoryGlobalProductRepository();
        var clock = new FixedClock(T0);
        var uow = new NoOpUnitOfWork();

        var product = GlobalProduct.Create(
            "Coke",
            ProductUnit.Bottle,
            "COKE-1",
            "480001",
            "BrandX",
            GlobalCategory.Create("Beverages", T0).Id,
            T0,
            8m,
            12m);
        await products.AddAsync(product);

        var create = new CreateCatalogTemplate(templates, new FakeBusinessTypeRepository(), uow, clock);
        var created = await create.ExecuteAsync(new CreateCatalogTemplateRequest(
            "Sari Starter",
            LegacyBusinessTypeSeeds.SariSariCode));
        Assert.True(created.IsSuccess);

        var assign = new AssignCatalogTemplateProduct(templates, products, uow, clock);
        var first = await assign.ExecuteAsync(
            created.Value!.Id,
            new AssignCatalogTemplateProductRequest(product.Id.Value, IsFirstBatch: true));
        Assert.True(first.IsSuccess);

        var second = await assign.ExecuteAsync(
            created.Value.Id,
            new AssignCatalogTemplateProductRequest(product.Id.Value));
        Assert.False(second.IsSuccess);
        Assert.Equal(DomainErrorCodes.CatalogTemplateProductDuplicate, second.ErrorCode);
    }

    [Fact]
    public async Task Bulk_assign_is_transactional_all_or_nothing()
    {
        var templates = new InMemoryCatalogTemplateRepository();
        var products = new InMemoryGlobalProductRepository();
        var clock = new FixedClock(T0);
        var uow = new NoOpUnitOfWork();

        var a = GlobalCatalogTestProducts.Make("Product A", "SKU-A", "480001");
        var b = GlobalCatalogTestProducts.Make("Product B", "SKU-B", "480002");
        await products.AddAsync(a);
        await products.AddAsync(b);

        var created = await new CreateCatalogTemplate(templates, new FakeBusinessTypeRepository(), uow, clock)
            .ExecuteAsync(new CreateCatalogTemplateRequest("Bulk Pack", LegacyBusinessTypeSeeds.SariSariCode));
        Assert.True(created.IsSuccess);

        var bulk = new BulkAssignCatalogTemplateProducts(templates, products, uow, clock);
        var result = await bulk.ExecuteAsync(
            created.Value!.Id,
            new BulkAssignCatalogTemplateProductsRequest(
                [a.Id.Value, b.Id.Value],
                IsFeatured: false,
                IsFirstBatch: true));
        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.Equal(2, result.Value!.ProductCount);
        Assert.Equal(2, result.Value.FirstBatchCount);

        var duplicate = await bulk.ExecuteAsync(
            created.Value.Id,
            new BulkAssignCatalogTemplateProductsRequest([a.Id.Value], ExpectedUpdatedAtUtc: result.Value.UpdatedAtUtc));
        Assert.False(duplicate.IsSuccess);
        Assert.Equal(DomainErrorCodes.CatalogTemplateProductDuplicate, duplicate.ErrorCode);
    }

    [Fact]
    public async Task Bulk_remove_clears_selected_products()
    {
        var templates = new InMemoryCatalogTemplateRepository();
        var products = new InMemoryGlobalProductRepository();
        var clock = new FixedClock(T0);
        var uow = new NoOpUnitOfWork();

        var keep = GlobalCatalogTestProducts.Make("Keep", "SKU-KEEP", "480010");
        var drop = GlobalCatalogTestProducts.Make("Drop", "SKU-DROP", "480011");
        await products.AddAsync(keep);
        await products.AddAsync(drop);

        var created = await new CreateCatalogTemplate(templates, new FakeBusinessTypeRepository(), uow, clock)
            .ExecuteAsync(new CreateCatalogTemplateRequest("Remove Pack", LegacyBusinessTypeSeeds.SariSariCode));
        var assigned = await new BulkAssignCatalogTemplateProducts(templates, products, uow, clock)
            .ExecuteAsync(created.Value!.Id, new BulkAssignCatalogTemplateProductsRequest([keep.Id.Value, drop.Id.Value]));
        Assert.True(assigned.IsSuccess);

        var removed = await new BulkRemoveCatalogTemplateProducts(templates, uow, clock)
            .ExecuteAsync(
                created.Value.Id,
                new BulkRemoveCatalogTemplateProductsRequest([drop.Id.Value], assigned.Value!.UpdatedAtUtc));
        Assert.True(removed.IsSuccess, removed.ErrorMessage);
        Assert.Single(removed.Value!.Products);
        Assert.Equal(keep.Id.Value, removed.Value.Products[0].GlobalProductId);
    }

    [Fact]
    public async Task Create_rejects_duplicate_slug()
    {
        var templates = new InMemoryCatalogTemplateRepository();
        var clock = new FixedClock(T0);
        var create = new CreateCatalogTemplate(templates, new FakeBusinessTypeRepository(), new NoOpUnitOfWork(), clock);

        var first = await create.ExecuteAsync(new CreateCatalogTemplateRequest(
            "Mini Grocery",
            LegacyBusinessTypeSeeds.MiniGroceryCode,
            Slug: "mini-grocery"));
        Assert.True(first.IsSuccess);

        var second = await create.ExecuteAsync(new CreateCatalogTemplateRequest(
            "Mini Grocery 2",
            LegacyBusinessTypeSeeds.MiniGroceryCode,
            Slug: "mini-grocery"));
        Assert.False(second.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.DuplicateCatalogTemplateSlug, second.ErrorCode);
    }

    [Fact]
    public async Task EnrichAsync_includes_brand_and_prices()
    {
        var templates = new InMemoryCatalogTemplateRepository();
        var products = new InMemoryGlobalProductRepository();
        var categories = new InMemoryGlobalCategoryRepository();
        var clock = new FixedClock(T0);

        var category = GlobalCategory.Create("Beverages", T0);
        await categories.AddAsync(category);
        var product = GlobalProduct.Create(
            "Coke",
            ProductUnit.Piece,
            "COKE-1",
            "480001",
            "BrandX",
            category.Id,
            T0,
            8m,
            12m);
        await products.AddAsync(product);

        var template = CatalogTemplate.Create("Sari Starter", BusinessTypeId.From(LegacyBusinessTypeSeeds.SariSariId), T0);
        template.AssignProduct(product.Id, T0.AddMinutes(1), isFirstBatch: true);
        await templates.AddAsync(template);

        var service = new CatalogTemplateQueryService(templates, products, categories, new FakeBusinessTypeRepository());
        var enriched = await service.GetByIdAsync(template.Id.Value);
        Assert.NotNull(enriched);
        var row = Assert.Single(enriched!.Products);
        Assert.Equal("Coke", row.ProductName);
        Assert.Equal("BrandX", row.Brand);
        Assert.Equal(8m, row.CostPrice);
        Assert.Equal(12m, row.SellingPrice);
        Assert.Equal("Beverages", row.CategoryName);
    }
}

file sealed class InMemoryCatalogTemplateRepository : ICatalogTemplateRepository
{
    private readonly Dictionary<Guid, CatalogTemplate> _store = new();

    public Task<CatalogTemplate?> GetByIdAsync(CatalogTemplateId id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_store.TryGetValue(id.Value, out var t) ? t : null);

    public Task<bool> ExistsWithSlugAsync(
        string slug,
        CatalogTemplateId? excludingId = null,
        CancellationToken cancellationToken = default)
    {
        var exists = _store.Values.Any(t =>
            string.Equals(t.Slug, slug, StringComparison.OrdinalIgnoreCase)
            && (excludingId is null || t.Id != excludingId));
        return Task.FromResult(exists);
    }

    public Task<(IReadOnlyList<CatalogTemplate> Items, int TotalCount)> ListAsync(
        CatalogTemplateStatus? status,
        Guid? primaryBusinessTypeId,
        string? primaryBusinessTypeCode,
        string? search,
        int skip,
        int take,
        CancellationToken cancellationToken = default,
        CatalogTemplateListSortBy sortBy = CatalogTemplateListSortBy.Name,
        bool sortDescending = false)
    {
        var items = _store.Values.AsEnumerable();
        if (status is not null)
        {
            items = items.Where(t => t.Status == status);
        }

        var list = items.Skip(skip).Take(take).ToList();
        return Task.FromResult(((IReadOnlyList<CatalogTemplate>)list, _store.Count));
    }

    public Task AddAsync(CatalogTemplate template, CancellationToken cancellationToken = default)
    {
        _store[template.Id.Value] = template;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(CatalogTemplate template, CancellationToken cancellationToken = default)
    {
        _store[template.Id.Value] = template;
        return Task.CompletedTask;
    }
}

file sealed class FixedClock : IClock
{
    public FixedClock(DateTimeOffset utcNow) => UtcNow = utcNow;
    public DateTimeOffset UtcNow { get; }
}

file sealed class NoOpUnitOfWork : IPlatformUnitOfWork
{
    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}

file sealed class InMemoryGlobalProductRepository : IGlobalProductRepository
{
    private readonly Dictionary<Guid, GlobalProduct> _store = new();

    public Task<GlobalProduct?> GetByIdAsync(GlobalProductId id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_store.TryGetValue(id.Value, out var p) ? p : null);

    public Task<bool> ExistsWithBarcodeAsync(
        string barcode,
        GlobalProductId? excludingId = null,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(false);

    public Task<bool> ExistsWithSkuAsync(
        string sku,
        GlobalProductId? excludingId = null,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(false);

    public Task<(IReadOnlyList<GlobalProduct> Items, int TotalCount)> ListAsync(
        GlobalProductStatus? status,
        GlobalCategoryId? categoryId,
        Guid? businessTypeId,
        string? businessTypeCode,
        string? search,
        string? barcode,
        string? sku,
        int skip,
        int take,
        CancellationToken cancellationToken = default,
        IReadOnlyCollection<Guid>? excludeProductIds = null,
        GlobalProductListSortBy sortBy = GlobalProductListSortBy.Name,
        bool sortDescending = false) =>
        Task.FromResult(((IReadOnlyList<GlobalProduct>)_store.Values.ToList(), _store.Count));

    public Task<IReadOnlyList<GlobalProduct>> GetByIdsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default) =>
        Task.FromResult((IReadOnlyList<GlobalProduct>)ids
            .Where(id => _store.ContainsKey(id))
            .Select(id => _store[id])
            .ToList());

    public Task AddAsync(GlobalProduct product, CancellationToken cancellationToken = default)
    {
        _store[product.Id.Value] = product;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(GlobalProduct product, CancellationToken cancellationToken = default)
    {
        _store[product.Id.Value] = product;
        return Task.CompletedTask;
    }
}

file sealed class InMemoryGlobalCategoryRepository : IGlobalCategoryRepository
{
    private readonly Dictionary<Guid, GlobalCategory> _store = new();

    public Task<GlobalCategory?> GetByIdAsync(GlobalCategoryId id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_store.TryGetValue(id.Value, out var c) ? c : null);

    public Task<bool> ExistsWithNameUnderParentAsync(
        string name,
        GlobalCategoryId? parentId,
        GlobalCategoryId? excludingId = null,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(false);

    public Task<(IReadOnlyList<GlobalCategory> Items, int TotalCount)> ListAsync(
        GlobalCategoryStatus? status,
        GlobalCategoryId? parentId,
        Guid? businessTypeId,
        string? businessTypeCode,
        string? search,
        int skip,
        int take,
        CancellationToken cancellationToken = default,
        GlobalCategoryListSortBy sortBy = GlobalCategoryListSortBy.SortOrder,
        bool sortDescending = false) =>
        Task.FromResult(((IReadOnlyList<GlobalCategory>)_store.Values.ToList(), _store.Count));

    public Task<IReadOnlyList<GlobalCategory>> GetByIdsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default) =>
        Task.FromResult((IReadOnlyList<GlobalCategory>)ids
            .Where(id => _store.ContainsKey(id))
            .Select(id => _store[id])
            .ToList());

    public Task<IReadOnlyList<GlobalCategory>> FindByNormalizedNameAsync(
        string normalizedName,
        CancellationToken cancellationToken = default) =>
        Task.FromResult((IReadOnlyList<GlobalCategory>)[]);

    public Task AddAsync(GlobalCategory category, CancellationToken cancellationToken = default)
    {
        _store[category.Id.Value] = category;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(GlobalCategory category, CancellationToken cancellationToken = default)
    {
        _store[category.Id.Value] = category;
        return Task.CompletedTask;
    }
}
