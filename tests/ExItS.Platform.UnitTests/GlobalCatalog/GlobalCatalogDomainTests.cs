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
    [InlineData("  ", null)]
    [InlineData(null, null)]
    [InlineData("", null)]
    public void NormalizeBarcode_uppercases_trims_and_nulls_blank(string? input, string? expected)
    {
        Assert.Equal(expected, GlobalCatalogRules.NormalizeBarcode(input));
    }

    [Theory]
    [InlineData("  sku-9  ", "SKU-9")]
    [InlineData("  ", null)]
    [InlineData(null, null)]
    public void NormalizeSku_uppercases_trims_and_nulls_blank(string? input, string? expected)
    {
        Assert.Equal(expected, GlobalCatalogRules.NormalizeSku(input));
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
        var product = GlobalProduct.Create(
            "  Coke  ",
            ProductUnit.Bottle,
            t0,
            sku: "  sku-1 ",
            barcode: " 480123 ");

        Assert.Equal("Coke", product.Name);
        Assert.Equal("SKU-1", product.Sku);
        Assert.Equal("480123", product.Barcode);
        Assert.Equal(GlobalProductStatus.Draft, product.Status);
    }
}

public sealed class GlobalCatalogLifecycleTests
{
    private static readonly DateTimeOffset T0 = new(2026, 8, 5, 12, 0, 0, TimeSpan.Zero);

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
        var product = GlobalProduct.Create("Snack", ProductUnit.Pack, T0);
        product.SetStatus(GlobalProductStatus.Active, T0.AddMinutes(1));
        Assert.Equal(GlobalProductStatus.Active, product.Status);

        product.SetStatus(GlobalProductStatus.Archived, T0.AddMinutes(2));
        Assert.Equal(GlobalProductStatus.Archived, product.Status);

        var ex = Assert.Throws<DomainException>(() =>
            product.Update("Snack 2", ProductUnit.Pack, T0.AddMinutes(3)));
        Assert.Equal(DomainErrorCodes.InvalidGlobalProductStatusTransition, ex.ErrorCode);

        Assert.Throws<DomainException>(() =>
            product.SetStatus(GlobalProductStatus.Draft, T0.AddMinutes(4)));
    }

    [Fact]
    public void Archive_is_soft_delete_entity_remains_addressable()
    {
        var product = GlobalProduct.Create("Kept", ProductUnit.Piece, T0);
        var id = product.Id;
        product.SetStatus(GlobalProductStatus.Archived, T0.AddMinutes(1));

        // Soft lifecycle: identity and timestamps remain; no hard delete API exists on the aggregate.
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

        var create = new CreateGlobalProduct(products, categories, uow, clock);
        var created = await create.ExecuteAsync(new CreateGlobalProductRequest("Item", "Piece", Sku: "A1"));
        Assert.True(created.IsSuccess);

        clock.Advance(TimeSpan.FromMinutes(1));
        var update = new UpdateGlobalProduct(products, categories, uow, clock);
        var stale = await update.ExecuteAsync(
            created.Value!.Id,
            new UpdateGlobalProductRequest(
                "Item 2",
                "Piece",
                Sku: "A1",
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
        var create = new CreateGlobalCategory(categories, uow, clock);

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
        var create = new CreateGlobalProduct(products, categories, uow, clock);

        var first = await create.ExecuteAsync(
            new CreateGlobalProductRequest("One", "Piece", Barcode: " 111 ", Sku: " sku-a "));
        Assert.True(first.IsSuccess);

        var dupBarcode = await create.ExecuteAsync(
            new CreateGlobalProductRequest("Two", "Piece", Barcode: "111"));
        Assert.False(dupBarcode.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.DuplicateGlobalProductBarcode, dupBarcode.ErrorCode);

        var dupSku = await create.ExecuteAsync(
            new CreateGlobalProductRequest("Three", "Piece", Sku: "SKU-A"));
        Assert.False(dupSku.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.DuplicateGlobalProductSku, dupSku.ErrorCode);

        var blankOk = await create.ExecuteAsync(new CreateGlobalProductRequest("Four", "Piece"));
        Assert.True(blankOk.IsSuccess);
        var blankOk2 = await create.ExecuteAsync(new CreateGlobalProductRequest("Five", "Piece"));
        Assert.True(blankOk2.IsSuccess);
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
        BusinessType? businessType,
        string? search,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
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
        BusinessType? businessType,
        string? search,
        string? barcode,
        string? sku,
        int skip,
        int take,
        CancellationToken cancellationToken = default,
        IReadOnlyCollection<Guid>? excludeProductIds = null)
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
