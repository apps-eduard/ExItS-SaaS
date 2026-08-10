using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.GlobalCatalog;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.GlobalCatalog;

namespace ExItS.Platform.UnitTests.GlobalCatalog;

public sealed class GlobalCatalogNormalizationTests
{
    [Theory]
    [InlineData("  abc-123  ", "ABC-123")]
    [InlineData("sku001", "SKU001")]
    public void NormalizeBarcode_uppercases_and_trims(string? input, string expected)
    {
        Assert.Equal(expected, GlobalCatalogRules.NormalizeBarcode(input));
    }

    [Theory]
    [InlineData("  ", null)]
    [InlineData(null, null)]
    [InlineData("", null)]
    public void NormalizeBarcode_rejects_blank(string? input, string? _)
    {
        var ex = Assert.Throws<DomainException>(() => GlobalCatalogRules.NormalizeBarcode(input));
        Assert.Equal(DomainErrorCodes.InvalidGlobalProductBarcode, ex.ErrorCode);
    }

    [Theory]
    [InlineData("  sku-9  ", "SKU-9")]
    public void NormalizeSku_uppercases_and_trims(string? input, string expected)
    {
        Assert.Equal(expected, GlobalCatalogRules.NormalizeSku(input));
    }

    [Theory]
    [InlineData("  ", null)]
    [InlineData(null, null)]
    public void NormalizeSku_rejects_blank(string? input, string? _)
    {
        var ex = Assert.Throws<DomainException>(() => GlobalCatalogRules.NormalizeSku(input));
        Assert.Equal(DomainErrorCodes.InvalidGlobalProductSku, ex.ErrorCode);
    }

    [Theory]
    [InlineData("  Acme Brand  ", "Acme Brand")]
    public void NormalizeBrand_trims_and_collapses_whitespace(string? input, string expected)
    {
        Assert.Equal(expected, GlobalCatalogRules.NormalizeBrand(input));
    }

    [Fact]
    public void NormalizeBrand_rejects_blank()
    {
        var ex = Assert.Throws<DomainException>(() => GlobalCatalogRules.NormalizeBrand("  "));
        Assert.Equal(DomainErrorCodes.InvalidGlobalProductBrand, ex.ErrorCode);
    }

    [Fact]
    public void RequireCategory_rejects_null()
    {
        var ex = Assert.Throws<DomainException>(() => GlobalCatalogRules.RequireCategory(null));
        Assert.Equal(DomainErrorCodes.InvalidGlobalProductCategory, ex.ErrorCode);
    }

    [Fact]
    public void NormalizeBarcode_rejects_internal_whitespace()
    {
        var ex = Assert.Throws<DomainException>(() => GlobalCatalogRules.NormalizeBarcode("AB CD"));
        Assert.Equal(DomainErrorCodes.InvalidGlobalProductBarcode, ex.ErrorCode);
    }

    [Fact]
    public void NormalizeName_collapses_whitespace()
    {
        Assert.Equal("Soft Drink", GlobalCatalogRules.NormalizeName("  Soft   Drink  "));
    }

    [Fact]
    public void GlobalProduct_create_applies_normalization()
    {
        var t0 = new DateTimeOffset(2026, 8, 5, 0, 0, 0, TimeSpan.Zero);
        var category = GlobalCategory.Create("Beverages", t0);
        var product = GlobalProduct.Create(
            "  Coke  ",
            ProductUnit.Bottle,
            "  sku-1 ",
            " 480123 ",
            "  BrandX  ",
            category.Id,
            t0,
            8m,
            12m);

        Assert.Equal("Coke", product.Name);
        Assert.Equal("SKU-1", product.Sku);
        Assert.Equal("480123", product.Barcode);
        Assert.Equal("BrandX", product.Brand);
        Assert.Equal(category.Id, product.GlobalCategoryId);
        Assert.Equal(GlobalProductStatus.Draft, product.Status);
    }

    [Fact]
    public void GlobalProduct_create_rejects_missing_required_fields()
    {
        var t0 = new DateTimeOffset(2026, 8, 5, 0, 0, 0, TimeSpan.Zero);
        var category = GlobalCategory.Create("Beverages", t0);

        Assert.Throws<DomainException>(() =>
            GlobalProduct.Create("Coke", ProductUnit.Bottle, "", "480001", "Brand", category.Id, t0, 1m, 2m));
        Assert.Throws<DomainException>(() =>
            GlobalProduct.Create("Coke", ProductUnit.Bottle, "SKU1", "", "Brand", category.Id, t0, 1m, 2m));
        Assert.Throws<DomainException>(() =>
            GlobalProduct.Create("Coke", ProductUnit.Bottle, "SKU1", "480001", " ", category.Id, t0, 1m, 2m));
        Assert.Throws<DomainException>(() =>
            GlobalProduct.Create("Coke", ProductUnit.Bottle, "SKU1", "480001", "Brand", null!, t0, 1m, 2m));
    }
}

public sealed class GlobalCatalogPricingTests
{
    private static readonly DateTimeOffset T0 = new(2026, 8, 6, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_rejects_negative_cost_and_selling()
    {
        var category = GlobalCategory.Create("Snacks", T0);
        var costEx = Assert.Throws<DomainException>(() =>
            GlobalProduct.Create("Snack", ProductUnit.Pack, "SKU1", "480001", "Brand", category.Id, T0, -1m, 10m));
        Assert.Equal(DomainErrorCodes.InvalidGlobalProductMoney, costEx.ErrorCode);

        var sellEx = Assert.Throws<DomainException>(() =>
            GlobalProduct.Create("Snack", ProductUnit.Pack, "SKU1", "480001", "Brand", category.Id, T0, 1m, -2m));
        Assert.Equal(DomainErrorCodes.InvalidGlobalProductMoney, sellEx.ErrorCode);
    }

    [Fact]
    public void Create_rejects_selling_below_cost()
    {
        var category = GlobalCategory.Create("Snacks", T0);
        var ex = Assert.Throws<DomainException>(() =>
            GlobalProduct.Create("Snack", ProductUnit.Pack, "SKU1", "480001", "Brand", category.Id, T0, 20m, 10m));
        Assert.Equal(DomainErrorCodes.InvalidGlobalProductPriceRelationship, ex.ErrorCode);
    }

    [Fact]
    public void Create_and_update_require_prices()
    {
        var category = GlobalCategory.Create("Snacks", T0);
        Assert.Throws<DomainException>(() =>
            GlobalProduct.Create("Snack", ProductUnit.Pack, "SKU1", "480001", "Brand", category.Id, T0, null, 10m));

        var product = GlobalProduct.Create("Snack", ProductUnit.Pack, "SKU1", "480001", "Brand", category.Id, T0, 8m, 12m);
        Assert.Equal(8m, product.CostPrice);
        Assert.Equal(12m, product.SellingPrice);

        Assert.Throws<DomainException>(() =>
            product.Update("Snack", ProductUnit.Pack, "SKU1", "480001", "Brand", category.Id, T0.AddMinutes(1), null, 12m));
    }

    [Fact]
    public void Rehydrate_preserves_null_prices_for_legacy_rows()
    {
        var category = GlobalCategory.Create("Snacks", T0);
        var product = GlobalProduct.Rehydrate(
            GlobalProductId.New(),
            "Legacy",
            null,
            "SKU1",
            "480001",
            "Brand",
            category.Id,
            ProductUnit.Pack,
            null,
            null,
            null,
            GlobalProductStatus.Draft,
            [],
            [],
            T0,
            T0);

        Assert.Null(product.CostPrice);
        Assert.Null(product.SellingPrice);
    }
}

public sealed class GlobalCatalogLifecycleTests
{
    private static readonly DateTimeOffset T0 = new(2026, 8, 5, 12, 0, 0, TimeSpan.Zero);

    private static GlobalProduct CreateProduct(string name = "Snack") =>
        GlobalProduct.Create(
            name,
            ProductUnit.Pack,
            "SKU-1",
            "480001",
            "BrandX",
            GlobalCategory.Create("Snacks", T0).Id,
            T0,
            10m,
            15m);

    [Fact]
    public void Category_active_inactive_archive_and_blocks_archived_edits()
    {
        var category = GlobalCategory.Create("Beverages", T0);
        Assert.Equal(GlobalCategoryStatus.Active, category.Status);

        category.SetStatus(GlobalCategoryStatus.Inactive, T0.AddMinutes(1));
        Assert.Equal(GlobalCategoryStatus.Inactive, category.Status);

        category.SetStatus(GlobalCategoryStatus.Active, T0.AddMinutes(2));
        category.SetStatus(GlobalCategoryStatus.Archived, T0.AddMinutes(3));
        Assert.Equal(GlobalCategoryStatus.Archived, category.Status);

        var ex = Assert.Throws<DomainException>(() => category.Rename("X", T0.AddMinutes(4)));
        Assert.Equal(DomainErrorCodes.InvalidGlobalCategoryStatusTransition, ex.ErrorCode);

        Assert.Throws<DomainException>(() =>
            category.SetStatus(GlobalCategoryStatus.Active, T0.AddMinutes(5)));
    }

    [Fact]
    public void Product_draft_active_archive_and_blocks_archived_updates()
    {
        var product = CreateProduct();
        product.SetStatus(GlobalProductStatus.Active, T0.AddMinutes(1));
        Assert.Equal(GlobalProductStatus.Active, product.Status);

        product.SetStatus(GlobalProductStatus.Archived, T0.AddMinutes(2));
        Assert.Equal(GlobalProductStatus.Archived, product.Status);

        var category = GlobalCategory.Create("Snacks", T0);
        var ex = Assert.Throws<DomainException>(() =>
            product.Update(
                "Snack 2",
                ProductUnit.Pack,
                "SKU-1",
                "480001",
                "BrandX",
                category.Id,
                T0.AddMinutes(3),
                10m,
                15m));
        Assert.Equal(DomainErrorCodes.InvalidGlobalProductStatusTransition, ex.ErrorCode);

        Assert.Throws<DomainException>(() =>
            product.SetStatus(GlobalProductStatus.Draft, T0.AddMinutes(4)));
    }

    [Fact]
    public void Archive_is_soft_delete_entity_remains_addressable()
    {
        var product = CreateProduct("Kept");
        var id = product.Id;
        product.SetStatus(GlobalProductStatus.Archived, T0.AddMinutes(1));

        Assert.Equal(id, product.Id);
        Assert.Equal(GlobalProductStatus.Archived, product.Status);
        Assert.Equal(T0, product.CreatedAtUtc);
        Assert.True(product.UpdatedAtUtc > product.CreatedAtUtc);
    }
}

public sealed class GlobalCatalogConcurrencyTests
{
    private static readonly DateTimeOffset T0 = new(2026, 8, 5, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Update_rejects_stale_ExpectedUpdatedAtUtc()
    {
        var categories = new InMemoryGlobalCategoryRepository();
        var products = new InMemoryGlobalProductRepository();
        var uow = new NoOpUnitOfWork();
        var clock = new FixedClock(T0);

        var category = GlobalCategory.Create("General", T0);
        await categories.AddAsync(category);

        var create = new CreateGlobalProduct(products, categories, new FakeBusinessTypeRepository(), uow, clock);
        var created = await create.ExecuteAsync(new CreateGlobalProductRequest(
            "Item",
            "Piece",
            "A1",
            "480001",
            "BrandX",
            category.Id.Value,
            CostPrice: 8m,
            SellingPrice: 12m));
        Assert.True(created.IsSuccess);

        clock.Advance(TimeSpan.FromMinutes(1));
        var update = new UpdateGlobalProduct(products, categories, new FakeBusinessTypeRepository(), uow, clock);
        var stale = await update.ExecuteAsync(
            created.Value!.Id,
            new UpdateGlobalProductRequest(
                "Item 2",
                "Piece",
                "A1",
                "480001",
                "BrandX",
                category.Id.Value,
                CostPrice: 8m,
                SellingPrice: 12m,
                ExpectedUpdatedAtUtc: T0.AddSeconds(-1)));

        Assert.False(stale.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.ConcurrencyConflict, stale.ErrorCode);
    }
}

public sealed class GlobalCatalogUniquenessTests
{
    private static readonly DateTimeOffset T0 = new(2026, 8, 5, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Category_name_unique_within_parent_scope()
    {
        var categories = new InMemoryGlobalCategoryRepository();
        var uow = new NoOpUnitOfWork();
        var clock = new FixedClock(T0);
        var create = new CreateGlobalCategory(categories, new FakeBusinessTypeRepository(), uow, clock);

        var first = await create.ExecuteAsync(new CreateGlobalCategoryRequest("Drinks"));
        Assert.True(first.IsSuccess);

        var duplicate = await create.ExecuteAsync(new CreateGlobalCategoryRequest(" drinks "));
        Assert.False(duplicate.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.DuplicateGlobalCategoryName, duplicate.ErrorCode);

        var childOk = await create.ExecuteAsync(
            new CreateGlobalCategoryRequest("Drinks", ParentId: first.Value!.Id));
        Assert.True(childOk.IsSuccess);
    }

    [Fact]
    public async Task Product_barcode_and_sku_unique_when_present()
    {
        var categories = new InMemoryGlobalCategoryRepository();
        var products = new InMemoryGlobalProductRepository();
        var uow = new NoOpUnitOfWork();
        var clock = new FixedClock(T0);
        var create = new CreateGlobalProduct(products, categories, new FakeBusinessTypeRepository(), uow, clock);

        var category = GlobalCategory.Create("General", T0);
        await categories.AddAsync(category);

        var first = await create.ExecuteAsync(
            new CreateGlobalProductRequest(
                "One",
                "Piece",
                " sku-a ",
                " 111 ",
                "BrandA",
                category.Id.Value,
                CostPrice: 8m,
                SellingPrice: 12m));
        Assert.True(first.IsSuccess);

        var dupBarcode = await create.ExecuteAsync(
            new CreateGlobalProductRequest("Two", "Piece", "SKU-B", "111", "BrandB", category.Id.Value, CostPrice: 8m, SellingPrice: 12m));
        Assert.False(dupBarcode.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.DuplicateGlobalProductBarcode, dupBarcode.ErrorCode);

        var dupSku = await create.ExecuteAsync(
            new CreateGlobalProductRequest("Three", "Piece", "SKU-A", "222", "BrandC", category.Id.Value, CostPrice: 8m, SellingPrice: 12m));
        Assert.False(dupSku.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.DuplicateGlobalProductSku, dupSku.ErrorCode);
    }

    [Fact]
    public async Task Create_rejects_missing_required_product_fields()
    {
        var categories = new InMemoryGlobalCategoryRepository();
        var products = new InMemoryGlobalProductRepository();
        var uow = new NoOpUnitOfWork();
        var clock = new FixedClock(T0);
        var create = new CreateGlobalProduct(products, categories, new FakeBusinessTypeRepository(), uow, clock);

        var category = GlobalCategory.Create("General", T0);
        await categories.AddAsync(category);

        var missingSku = await create.ExecuteAsync(
            new CreateGlobalProductRequest("One", "Piece", "", "111", "Brand", category.Id.Value, CostPrice: 8m, SellingPrice: 12m));
        Assert.False(missingSku.IsSuccess);
        Assert.Equal(DomainErrorCodes.InvalidGlobalProductSku, missingSku.ErrorCode);

        var missingBrand = await create.ExecuteAsync(
            new CreateGlobalProductRequest("Two", "Piece", "SKU-2", "222", " ", category.Id.Value, CostPrice: 8m, SellingPrice: 12m));
        Assert.False(missingBrand.IsSuccess);
        Assert.Equal(DomainErrorCodes.InvalidGlobalProductBrand, missingBrand.ErrorCode);
    }
}

file sealed class FixedClock : IClock
{
    public FixedClock(DateTimeOffset utcNow) => UtcNow = utcNow;
    public DateTimeOffset UtcNow { get; private set; }
    public void Advance(TimeSpan delta) => UtcNow = UtcNow.Add(delta);
}

file sealed class NoOpUnitOfWork : IPlatformUnitOfWork
{
    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
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
        CancellationToken cancellationToken = default)
    {
        var normalized = name.Trim().ToUpperInvariant();
        var exists = _store.Values.Any(c =>
            c.Name.ToUpperInvariant() == normalized
            && Equals(c.ParentId, parentId)
            && (excludingId is null || c.Id != excludingId));
        return Task.FromResult(exists);
    }

    public Task<IReadOnlyList<GlobalCategory>> FindByNormalizedNameAsync(
        string normalizedName,
        CancellationToken cancellationToken = default)
    {
        var matches = _store.Values
            .Where(c => c.Name.ToUpperInvariant() == normalizedName)
            .ToList();
        return Task.FromResult<IReadOnlyList<GlobalCategory>>(matches);
    }

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
        bool sortDescending = false)
    {
        var items = _store.Values.AsEnumerable();
        if (status is not null)
        {
            items = items.Where(c => c.Status == status);
        }

        var list = items.Skip(skip).Take(take).ToList();
        return Task.FromResult(((IReadOnlyList<GlobalCategory>)list, _store.Count));
    }

    public Task<IReadOnlyList<GlobalCategory>> GetByIdsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default) =>
        Task.FromResult((IReadOnlyList<GlobalCategory>)ids
            .Where(id => _store.ContainsKey(id))
            .Select(id => _store[id])
            .ToList());

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

file sealed class InMemoryGlobalProductRepository : IGlobalProductRepository
{
    private readonly Dictionary<Guid, GlobalProduct> _store = new();

    public Task<GlobalProduct?> GetByIdAsync(GlobalProductId id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_store.TryGetValue(id.Value, out var p) ? p : null);

    public Task<bool> ExistsWithBarcodeAsync(
        string barcode,
        GlobalProductId? excludingId = null,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_store.Values.Any(p =>
            p.Barcode == barcode && (excludingId is null || p.Id != excludingId)));

    public Task<bool> ExistsWithSkuAsync(
        string sku,
        GlobalProductId? excludingId = null,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_store.Values.Any(p =>
            p.Sku == sku && (excludingId is null || p.Id != excludingId)));

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
        bool sortDescending = false)
    {
        var list = _store.Values.Skip(skip).Take(take).ToList();
        return Task.FromResult(((IReadOnlyList<GlobalProduct>)list, _store.Count));
    }

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
