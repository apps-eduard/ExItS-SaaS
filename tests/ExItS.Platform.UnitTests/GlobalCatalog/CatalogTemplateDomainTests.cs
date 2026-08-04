using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.GlobalCatalog;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.GlobalCatalog;

namespace ExItS.Platform.UnitTests.GlobalCatalog;

public sealed class CatalogTemplateDomainTests
{
    private static readonly DateTimeOffset T0 = new(2026, 8, 5, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_normalizes_slug_and_defaults_draft_curated()
    {
        var template = CatalogTemplate.Create(
            "  Sari Sari Starter  ",
            BusinessType.SariSari,
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
        var template = CatalogTemplate.Create("Mini Grocery", BusinessType.MiniGrocery, T0);
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
        var template = CatalogTemplate.Create("Cafe Starter", BusinessType.Cafe, T0);
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
        var template = CatalogTemplate.Create("Dup Check", BusinessType.GeneralRetail, T0);
        var productId = GlobalProductId.New();
        template.AssignProduct(productId, T0.AddMinutes(1));

        var ex = Assert.Throws<DomainException>(() =>
            template.AssignProduct(productId, T0.AddMinutes(2)));
        Assert.Equal(DomainErrorCodes.CatalogTemplateProductDuplicate, ex.ErrorCode);
    }

    [Fact]
    public void ReorderProducts_updates_sort_order()
    {
        var template = CatalogTemplate.Create("Order Check", BusinessType.Bakery, T0);
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
        var template = CatalogTemplate.Create("Order Bad", BusinessType.Pharmacy, T0);
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
        var template = CatalogTemplate.Create("Live Edit", BusinessType.SariSari, T0);
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
        var create = new CreateCatalogTemplate(templates, new NoOpUnitOfWork(), clock);
        var created = await create.ExecuteAsync(new CreateCatalogTemplateRequest(
            "Empty Starter",
            nameof(BusinessType.SariSari)));
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

        var product = GlobalProduct.Create("Coke", ProductUnit.Bottle, T0);
        await products.AddAsync(product);

        var create = new CreateCatalogTemplate(templates, uow, clock);
        var created = await create.ExecuteAsync(new CreateCatalogTemplateRequest(
            "Sari Starter",
            nameof(BusinessType.SariSari)));
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
    public async Task Create_rejects_duplicate_slug()
    {
        var templates = new InMemoryCatalogTemplateRepository();
        var clock = new FixedClock(T0);
        var create = new CreateCatalogTemplate(templates, new NoOpUnitOfWork(), clock);

        var first = await create.ExecuteAsync(new CreateCatalogTemplateRequest(
            "Mini Grocery",
            nameof(BusinessType.MiniGrocery),
            Slug: "mini-grocery"));
        Assert.True(first.IsSuccess);

        var second = await create.ExecuteAsync(new CreateCatalogTemplateRequest(
            "Mini Grocery 2",
            nameof(BusinessType.MiniGrocery),
            Slug: "mini-grocery"));
        Assert.False(second.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.DuplicateCatalogTemplateSlug, second.ErrorCode);
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
        BusinessType? primaryBusinessType,
        string? search,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
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
        BusinessType? businessType,
        string? search,
        string? barcode,
        string? sku,
        int skip,
        int take,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(((IReadOnlyList<GlobalProduct>)_store.Values.ToList(), _store.Count));

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
