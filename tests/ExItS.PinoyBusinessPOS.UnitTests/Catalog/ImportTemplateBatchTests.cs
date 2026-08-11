using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.UnitTests.Catalog;

public sealed class ImportTemplateBatchTests
{
    private static readonly PosOrganizationId Org = PosOrganizationId.From(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
    private static readonly Guid TemplateId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid ProductA = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid ProductB = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid ProductC = Guid.Parse("33333333-3333-3333-3333-333333333333");

    [Fact]
    public async Task Import_WhenTemplateHasEnrichedNames_StillReChecksEntitlementViaLiveProductFetch()
    {
        var platform = new RecordingPlatform(BuildEnrichedTemplate(firstBatchFlags: false, defaultBatchSize: 2));
        var imports = new MemoryImports();
        var products = new MemoryProducts();
        var useCase = new ImportTemplateBatch(imports, products, platform, new FakeUnitOfWork(), new FixedClock());

        var result = await useCase.ExecuteAsync(
            Org.Value,
            TemplateId,
            batchNumber: 1,
            requestedBy: Guid.NewGuid().ToString("D"),
            platformSessionToken: "session",
            idempotencyKey: "key-1");

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.TotalCount);
        Assert.Equal(1, platform.LiveFetchCalls);
        Assert.Equal(1, platform.TemplateFetchCalls);
    }

    [Fact]
    public async Task Import_SkipsProductsNotReturnedByEntitledPlatformGet()
    {
        var platform = new RecordingPlatform(
            BuildEnrichedTemplate(firstBatchFlags: false, defaultBatchSize: 2),
            entitledProductIds: [ProductA]);
        var useCase = new ImportTemplateBatch(
            new MemoryImports(),
            new MemoryProducts(),
            platform,
            new FakeUnitOfWork(),
            new FixedClock());

        var result = await useCase.ExecuteAsync(
            Org.Value,
            TemplateId,
            batchNumber: 1,
            requestedBy: Guid.NewGuid().ToString("D"),
            platformSessionToken: "session");

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value!.TotalCount);
    }

    [Fact]
    public async Task Import_WhenTemplateNotReturned_IsDenied()
    {
        var platform = new RecordingPlatform(BuildEnrichedTemplate(firstBatchFlags: false, defaultBatchSize: 2));
        platform.ReturnNullTemplate = true;
        var useCase = new ImportTemplateBatch(
            new MemoryImports(),
            new MemoryProducts(),
            platform,
            new FakeUnitOfWork(),
            new FixedClock());

        var result = await useCase.ExecuteAsync(
            Org.Value,
            Guid.NewGuid(),
            batchNumber: 1,
            requestedBy: Guid.NewGuid().ToString("D"),
            platformSessionToken: "session");

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.CatalogImportTemplateNotFound, result.ErrorCode);
    }

    [Fact]
    public async Task Import_WhenHttpClientTimesOut_ReturnsPlatformUnavailableNotUnhandled()
    {
        var platform = new TimingOutPlatform();
        var useCase = new ImportTemplateBatch(
            new MemoryImports(),
            new MemoryProducts(),
            platform,
            new FakeUnitOfWork(),
            new FixedClock());

        var result = await useCase.ExecuteAsync(
            Org.Value,
            TemplateId,
            batchNumber: 1,
            requestedBy: Guid.NewGuid().ToString("D"),
            platformSessionToken: "session");

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.CatalogImportPlatformUnavailable, result.ErrorCode);
    }

    [Fact]
    public void IsTransientPlatformFailure_TreatsHttpClientTimeoutAsTransient()
    {
        using var open = new CancellationTokenSource();
        Assert.True(ImportTemplateBatch.IsTransientPlatformFailure(
            new TaskCanceledException("HttpClient timeout"),
            open.Token));
    }

    [Fact]
    public void IsTransientPlatformFailure_PropagatesCallerCancellation()
    {
        using var canceled = new CancellationTokenSource();
        canceled.Cancel();
        Assert.False(ImportTemplateBatch.IsTransientPlatformFailure(
            new OperationCanceledException(canceled.Token),
            canceled.Token));
    }

    private static PlatformMerchantCatalogTemplateDto BuildEnrichedTemplate(bool firstBatchFlags, int defaultBatchSize) =>
        new(
            Id: TemplateId,
            Name: "Mini Bakery",
            Slug: "mini-bakery",
            Description: null,
            IconReference: null,
            PrimaryBusinessType: "Bakery",
            Status: "Published",
            DefaultBatchSize: defaultBatchSize,
            SelectionMode: "Curated",
            PublishedAtUtc: DateTimeOffset.Parse("2026-08-01T00:00:00Z"),
            ProductCount: 3,
            FirstBatchCount: firstBatchFlags ? 2 : 0,
            Products:
            [
                new(Guid.NewGuid(), ProductA, 1, false, firstBatchFlags, "Pandosal", Unit: "Piece", SellingPrice: 5m),
                new(Guid.NewGuid(), ProductB, 2, false, firstBatchFlags, "Ensaymada", Unit: "Piece", SellingPrice: 20m),
                new(Guid.NewGuid(), ProductC, 3, false, false, "Cake", Unit: "Piece", SellingPrice: 250m)
            ],
            CreatedAtUtc: DateTimeOffset.Parse("2026-08-01T00:00:00Z"),
            UpdatedAtUtc: DateTimeOffset.Parse("2026-08-01T00:00:00Z"));

    private sealed class FixedClock : IClock
    {
        public DateTimeOffset UtcNow { get; } = DateTimeOffset.Parse("2026-08-11T00:00:00Z");
    }

    private sealed class FakeUnitOfWork : IPosUnitOfWork
    {
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<T> ExecuteInSerializableTransactionAsync<T>(
            Func<CancellationToken, Task<T>> action,
            CancellationToken cancellationToken = default) =>
            action(cancellationToken);
    }

    private sealed class RecordingPlatform(
        PlatformMerchantCatalogTemplateDto template,
        IReadOnlyCollection<Guid>? entitledProductIds = null) : IPlatformMerchantCatalogClient
    {
        private readonly HashSet<Guid> _entitled =
            entitledProductIds?.ToHashSet()
            ?? template.Products.Select(p => p.GlobalProductId).ToHashSet();

        public int TemplateFetchCalls { get; private set; }
        public int LiveFetchCalls { get; private set; }
        public bool ReturnNullTemplate { get; set; }

        public Task<PlatformMerchantCatalogTemplateDto?> GetPublishedTemplateAsync(
            Guid templateId,
            string? platformSessionToken,
            CancellationToken cancellationToken = default)
        {
            TemplateFetchCalls++;
            if (ReturnNullTemplate || templateId != template.Id)
            {
                return Task.FromResult<PlatformMerchantCatalogTemplateDto?>(null);
            }

            return Task.FromResult<PlatformMerchantCatalogTemplateDto?>(template);
        }

        public Task<PlatformMerchantGlobalProductDto?> GetActiveProductAsync(
            Guid productId,
            string? platformSessionToken,
            CancellationToken cancellationToken = default)
        {
            LiveFetchCalls++;
            if (!_entitled.Contains(productId))
            {
                return Task.FromResult<PlatformMerchantGlobalProductDto?>(null);
            }

            var link = template.Products.First(p => p.GlobalProductId == productId);
            return Task.FromResult<PlatformMerchantGlobalProductDto?>(Map(link));
        }

        public Task<IReadOnlyList<PlatformMerchantGlobalProductDto>> GetActiveProductsAsync(
            IReadOnlyList<Guid> productIds,
            string? platformSessionToken,
            CancellationToken cancellationToken = default)
        {
            LiveFetchCalls++;
            var items = productIds
                .Where(_entitled.Contains)
                .Select(id => Map(template.Products.First(p => p.GlobalProductId == id)))
                .ToList();
            return Task.FromResult<IReadOnlyList<PlatformMerchantGlobalProductDto>>(items);
        }

        private static PlatformMerchantGlobalProductDto Map(PlatformMerchantCatalogTemplateProductDto link) =>
            new(
                link.GlobalProductId,
                link.ProductName ?? "Product",
                null,
                link.Sku,
                link.Barcode,
                link.Brand,
                link.CategoryId,
                link.Unit ?? "Piece",
                string.IsNullOrWhiteSpace(link.SellingMode) ? "PerItem" : link.SellingMode!,
                link.CostPrice,
                link.SellingPrice,
                null,
                "Active",
                DateTimeOffset.Parse("2026-08-01T00:00:00Z"),
                DateTimeOffset.Parse("2026-08-01T00:00:00Z"));

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
    }

    private sealed class TimingOutPlatform : IPlatformMerchantCatalogClient
    {
        public Task<PlatformMerchantCatalogTemplateDto?> GetPublishedTemplateAsync(
            Guid templateId,
            string? platformSessionToken,
            CancellationToken cancellationToken = default) =>
            throw new TaskCanceledException("The request was canceled due to the configured HttpClient.Timeout.");

        public Task<PlatformMerchantGlobalProductDto?> GetActiveProductAsync(
            Guid productId,
            string? platformSessionToken,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<PlatformMerchantGlobalProductDto?>(null);

        public Task<IReadOnlyList<PlatformMerchantGlobalProductDto>> GetActiveProductsAsync(
            IReadOnlyList<Guid> productIds,
            string? platformSessionToken,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<PlatformMerchantGlobalProductDto>>([]);

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
    }

    private sealed class MemoryImports : ICatalogImportJobRepository
    {
        private readonly List<CatalogImportJob> _jobs = [];

        public Task<CatalogImportJob?> GetByIdAsync(PosOrganizationId organizationId, CatalogImportJobId jobId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_jobs.FirstOrDefault(j => j.OrganizationId == organizationId && j.Id == jobId));

        public Task<CatalogImportJob?> FindByIdempotencyKeyAsync(PosOrganizationId organizationId, string idempotencyKey, CancellationToken cancellationToken = default) =>
            Task.FromResult(_jobs.FirstOrDefault(j => j.OrganizationId == organizationId && j.IdempotencyKey == idempotencyKey));

        public Task<CatalogImportJob?> ClaimNextAsync(DateTimeOffset utcNow, TimeSpan staleAfter, CancellationToken cancellationToken = default) =>
            Task.FromResult<CatalogImportJob?>(null);

        public Task<(IReadOnlyList<CatalogImportItemResult> Items, int TotalCount)> ListItemsAsync(
            PosOrganizationId organizationId,
            CatalogImportJobId jobId,
            PosCatalogImportItemStatus? status,
            int skip,
            int take,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<(IReadOnlyList<CatalogImportItemResult>, int)>(([], 0));

        public Task AddAsync(CatalogImportJob job, CancellationToken cancellationToken = default)
        {
            _jobs.Add(job);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(CatalogImportJob job, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class MemoryProducts : ICatalogProductRepository
    {
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
            Task.FromResult<IReadOnlySet<Guid>>(new HashSet<Guid>());

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

        public Task AddAsync(CatalogProduct product, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task UpdateAsync(CatalogProduct product, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
