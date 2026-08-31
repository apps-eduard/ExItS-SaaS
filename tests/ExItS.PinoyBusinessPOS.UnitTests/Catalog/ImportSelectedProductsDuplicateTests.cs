using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.UnitTests.Catalog;

public sealed class ImportSelectedProductsDuplicateTests
{
    private static readonly PosOrganizationId OrgA = PosOrganizationId.From(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
    private static readonly PosOrganizationId OrgB = PosOrganizationId.From(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));
    private static readonly Guid GlobalBacon = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid GlobalChicken = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid GlobalNew = Guid.Parse("33333333-3333-3333-3333-333333333333");

    [Fact]
    public async Task Import_rejects_when_all_selected_ids_already_imported()
    {
        var products = new MemoryProducts();
        products.MarkImported(OrgA, GlobalBacon);
        var useCase = Create(products, entitled: [GlobalBacon]);

        var result = await useCase.ExecuteAsync(
            OrgA.Value,
            [GlobalBacon],
            requestedBy: Guid.NewGuid().ToString("D"),
            platformSessionToken: "session");

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.CatalogImportProductAlreadyImported, result.ErrorCode);
        Assert.Empty(products.Added);
    }

    [Fact]
    public async Task Import_queues_only_missing_products_when_mix_of_imported_and_new()
    {
        var products = new MemoryProducts();
        products.MarkImported(OrgA, GlobalBacon);
        var imports = new MemoryImports();
        var useCase = Create(products, entitled: [GlobalBacon, GlobalChicken], imports);

        var result = await useCase.ExecuteAsync(
            OrgA.Value,
            [GlobalBacon, GlobalChicken],
            requestedBy: Guid.NewGuid().ToString("D"),
            platformSessionToken: "session");

        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.Equal(1, result.Value!.TotalCount);
        var job = Assert.Single(imports.Jobs);
        Assert.Equal(GlobalChicken, Assert.Single(job.Items).PlatformGlobalProductId);
    }

    [Fact]
    public async Task Same_global_id_can_exist_independently_in_another_organization()
    {
        var products = new MemoryProducts();
        products.MarkImported(OrgA, GlobalBacon);
        var useCase = Create(products, entitled: [GlobalBacon]);

        var result = await useCase.ExecuteAsync(
            OrgB.Value,
            [GlobalBacon],
            requestedBy: Guid.NewGuid().ToString("D"),
            platformSessionToken: "session");

        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.Equal(1, result.Value!.TotalCount);
    }

    [Fact]
    public async Task List_imported_returns_only_matching_org_ids()
    {
        var products = new MemoryProducts();
        products.MarkImported(OrgA, GlobalBacon);
        products.MarkImported(OrgB, GlobalChicken);
        var useCase = new ListImportedGlobalProducts(products);

        var forA = await useCase.ExecuteAsync(OrgA.Value, [GlobalBacon, GlobalChicken, GlobalNew]);
        Assert.True(forA.IsSuccess);
        Assert.Equal([GlobalBacon], forA.Value!.ImportedIds);

        var forB = await useCase.ExecuteAsync(OrgB.Value, [GlobalBacon, GlobalChicken]);
        Assert.True(forB.IsSuccess);
        Assert.Equal([GlobalChicken], forB.Value!.ImportedIds);
    }

    [Fact]
    public async Task Replay_of_already_imported_global_id_does_not_create_job_items()
    {
        var products = new MemoryProducts();
        products.MarkImported(OrgA, GlobalBacon);
        products.MarkImported(OrgA, GlobalChicken);
        var imports = new MemoryImports();
        var useCase = Create(products, entitled: [GlobalBacon, GlobalChicken], imports);

        var result = await useCase.ExecuteAsync(
            OrgA.Value,
            [GlobalBacon, GlobalChicken, GlobalBacon],
            requestedBy: Guid.NewGuid().ToString("D"),
            platformSessionToken: "session");

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.CatalogImportProductAlreadyImported, result.ErrorCode);
        Assert.Empty(imports.Jobs);
    }

    private static ImportSelectedProducts Create(
        MemoryProducts products,
        IReadOnlyCollection<Guid> entitled,
        MemoryImports? imports = null) =>
        new(
            imports ?? new MemoryImports(),
            products,
            new FakePlatform(entitled),
            new FakeUnitOfWork(),
            new FixedClock(),
            new CatalogProductGovernanceAuthority(),
            FixedCatalogGovernanceActorAccessor.Owner());

    private sealed class FixedClock : IClock
    {
        public DateTimeOffset UtcNow { get; } = DateTimeOffset.Parse("2026-08-13T00:00:00Z");
    }

    private sealed class FakeUnitOfWork : IPosUnitOfWork
    {
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<T> ExecuteInSerializableTransactionAsync<T>(
            Func<CancellationToken, Task<T>> action,
            CancellationToken cancellationToken = default) =>
            action(cancellationToken);
    }

    private sealed class FakePlatform(IReadOnlyCollection<Guid> entitled) : IPlatformMerchantCatalogClient
    {
        public Task<PlatformMerchantCatalogTemplateDto?> GetPublishedTemplateAsync(
            Guid templateId,
            string? platformSessionToken,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<PlatformMerchantCatalogTemplateDto?>(null);

        public Task<PlatformMerchantGlobalProductDto?> GetActiveProductAsync(
            Guid productId,
            string? platformSessionToken,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(entitled.Contains(productId) ? Build(productId) : null);

        public Task<IReadOnlyList<PlatformMerchantGlobalProductDto>> GetActiveProductsAsync(
            IReadOnlyList<Guid> productIds,
            string? platformSessionToken,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<PlatformMerchantGlobalProductDto>>(
                productIds.Where(entitled.Contains).Select(Build).ToList());

        public Task<PagedResult<PlatformMerchantGlobalProductDto>> SearchActiveProductsAsync(
            string? search,
            Guid? categoryId,
            string? businessTypeCode,
            string? barcode,
            string? sku,
            int? page,
            int? pageSize,
            string? platformSessionToken,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new PagedResult<PlatformMerchantGlobalProductDto>([], 0, 1, 50));

        public Task<PagedResult<PlatformMerchantGlobalCategoryDto>> ListActiveCategoriesAsync(
            string? search,
            string? businessTypeCode,
            Guid? parentId,
            int? page,
            int? pageSize,
            string? platformSessionToken,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new PagedResult<PlatformMerchantGlobalCategoryDto>([], 0, 1, 50));

        public Task<IReadOnlyList<PlatformGlobalProductImageMetaDto>> ListProductImageMetaAsync(
            IReadOnlyList<Guid> productIds,
            string? platformSessionToken,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<PlatformGlobalProductImageMetaDto>>([]);

        public Task<ProductImageBytes?> GetProductImageAsync(
            Guid productId,
            string variant,
            string? platformSessionToken,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<ProductImageBytes?>(null);

        private static PlatformMerchantGlobalProductDto Build(Guid id) =>
            new(
                id,
                "Product",
                null,
                "SKU-1",
                null,
                "Brand",
                null,
                "Piece",
                "PerItem",
                5m,
                10m,
                null,
                "Active",
                DateTimeOffset.Parse("2026-08-01T00:00:00Z"),
                DateTimeOffset.Parse("2026-08-01T00:00:00Z"));
    }

    private sealed class MemoryImports : ICatalogImportJobRepository
    {
        public List<CatalogImportJob> Jobs { get; } = [];

        public Task AddAsync(CatalogImportJob job, CancellationToken cancellationToken = default)
        {
            Jobs.Add(job);
            return Task.CompletedTask;
        }

        public Task<CatalogImportJob?> ClaimNextAsync(DateTimeOffset utcNow, TimeSpan staleAfter, CancellationToken cancellationToken = default) =>
            Task.FromResult<CatalogImportJob?>(null);

        public Task<CatalogImportJob?> FindByIdempotencyKeyAsync(PosOrganizationId organizationId, string idempotencyKey, CancellationToken cancellationToken = default) =>
            Task.FromResult(Jobs.FirstOrDefault(j => j.OrganizationId == organizationId && j.IdempotencyKey == idempotencyKey));

        public Task<CatalogImportJob?> GetByIdAsync(PosOrganizationId organizationId, CatalogImportJobId jobId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Jobs.FirstOrDefault(j => j.OrganizationId == organizationId && j.Id == jobId));

        public Task<(IReadOnlyList<CatalogImportItemResult> Items, int TotalCount)> ListItemsAsync(
            PosOrganizationId organizationId,
            CatalogImportJobId jobId,
            PosCatalogImportItemStatus? status,
            int skip,
            int take,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<(IReadOnlyList<CatalogImportItemResult>, int)>(([], 0));

        public Task UpdateAsync(CatalogImportJob job, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class MemoryProducts : ICatalogProductRepository
    {
        private readonly HashSet<(Guid Org, Guid Global)> _imported = [];
        public List<CatalogProduct> Added { get; } = [];

        public void MarkImported(PosOrganizationId organizationId, Guid platformGlobalProductId) =>
            _imported.Add((organizationId.Value, platformGlobalProductId));

        public Task<CatalogProduct?> GetByIdAsync(PosOrganizationId organizationId, CatalogProductId productId, CancellationToken cancellationToken = default) =>
            Task.FromResult<CatalogProduct?>(null);

        public Task<CatalogProduct?> FindByNormalizedSkuAsync(PosOrganizationId organizationId, string normalizedSku, CancellationToken cancellationToken = default) =>
            Task.FromResult<CatalogProduct?>(null);

        public Task<CatalogProduct?> FindByBarcodeAsync(PosOrganizationId organizationId, string barcode, CancellationToken cancellationToken = default) =>
            Task.FromResult<CatalogProduct?>(null);

        public Task<CatalogProduct?> FindByPlatformGlobalProductIdAsync(PosOrganizationId organizationId, Guid platformGlobalProductId, CancellationToken cancellationToken = default) =>
            Task.FromResult<CatalogProduct?>(null);

        public Task<IReadOnlySet<Guid>> ListPlatformGlobalProductIdsAsync(
            PosOrganizationId organizationId,
            IReadOnlyCollection<Guid> platformGlobalProductIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlySet<Guid>>(
                platformGlobalProductIds
                    .Where(id => _imported.Contains((organizationId.Value, id)))
                    .ToHashSet());

        public Task<IReadOnlyList<CatalogProduct>> ListByIdsAsync(
            PosOrganizationId organizationId,
            IReadOnlyCollection<CatalogProductId> productIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CatalogProduct>>([]);

        public Task<(IReadOnlyList<CatalogProduct> Items, int TotalCount)> ListAsync(
            PosOrganizationId organizationId,
            CatalogProductFilter filter,
            int skip,
            int take,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<(IReadOnlyList<CatalogProduct>, int)>(([], 0));

        public Task<IReadOnlyList<Guid>> ListIdsAsync(
            PosOrganizationId organizationId,
            CatalogProductFilter filter,
            int skip,
            int take,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Guid>>([]);

        public Task<(int TotalCount, int AvailableCount, int NotAvailableCount)> CountConnectedBuyerAvailabilityAsync(
            PosOrganizationId organizationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult((0, 0, 0));

        public Task<IReadOnlyList<(Guid? CategoryId, int Count)>> ListConnectedBuyerAvailabilityCategoryFacetsAsync(
            PosOrganizationId organizationId,
            CatalogProductFilter filter,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<(Guid? CategoryId, int Count)>>([]);


        public Task AddAsync(CatalogProduct product, CancellationToken cancellationToken = default)
        {
            Added.Add(product);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(CatalogProduct product, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
