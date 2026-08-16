using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.UnitTests.Catalog;

public sealed class GetTemplateImportStatusTests
{
    private static readonly PosOrganizationId Org = PosOrganizationId.From(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
    private static readonly Guid TemplateId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid FirstA = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid FirstB = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid NextA = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid NextB = Guid.Parse("44444444-4444-4444-4444-444444444444");

    [Fact]
    public async Task Status_WhenNoFirstBatchFlags_UsesOrderedWindowAsFirstBatch()
    {
        var products = new MemoryProducts();
        var template = new PlatformMerchantCatalogTemplateDto(
            Id: TemplateId,
            Name: "Bakery",
            Slug: "bakery",
            Description: null,
            IconReference: null,
            PrimaryBusinessType: "Bakery",
            PrimaryBusinessTypeId: Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
            Status: "Published",
            DefaultBatchSize: 2,
            SelectionMode: "Curated",
            PublishedAtUtc: DateTimeOffset.Parse("2026-08-01T00:00:00Z"),
            ProductCount: 4,
            FirstBatchCount: 0,
            Products:
            [
                new(Guid.NewGuid(), FirstA, 1, false, false, "A"),
                new(Guid.NewGuid(), FirstB, 2, false, false, "B"),
                new(Guid.NewGuid(), NextA, 3, false, false, "C"),
                new(Guid.NewGuid(), NextB, 4, false, false, "D")
            ],
            CreatedAtUtc: DateTimeOffset.Parse("2026-08-01T00:00:00Z"),
            UpdatedAtUtc: DateTimeOffset.Parse("2026-08-01T00:00:00Z"));

        var status = await new GetTemplateImportStatus(products, new FakePlatform(template))
            .ExecuteAsync(Org.Value, TemplateId, platformSessionToken: null);

        Assert.True(status.IsSuccess);
        Assert.True(status.Value!.CanImportFirstBatch);
        Assert.Equal(2, status.Value.FirstBatchTotal);
        Assert.Equal(2, status.Value.SubsequentTotal);
        Assert.False(status.Value.CanImportNextBatch);
    }

    [Fact]
    public async Task Status_WhenNothingImported_AllowsFirstBatchOnly()
    {
        var products = new MemoryProducts();
        var status = await new GetTemplateImportStatus(products, new FakePlatform(BuildTemplate()))
            .ExecuteAsync(Org.Value, TemplateId, platformSessionToken: null);

        Assert.True(status.IsSuccess);
        Assert.True(status.Value!.CanImportFirstBatch);
        Assert.False(status.Value.FirstBatchComplete);
        Assert.False(status.Value.CanImportNextBatch);
        Assert.True(status.Value.HasSubsequentBatches);
        Assert.Equal(2, status.Value.FirstBatchTotal);
        Assert.Equal(2, status.Value.SubsequentRemainingCount);
    }

    [Fact]
    public async Task Status_WhenFirstBatchDone_AllowsNextBatch()
    {
        var products = new MemoryProducts();
        await products.AddImportedAsync(FirstA);
        await products.AddImportedAsync(FirstB);

        var status = await new GetTemplateImportStatus(products, new FakePlatform(BuildTemplate()))
            .ExecuteAsync(Org.Value, TemplateId, platformSessionToken: null);

        Assert.True(status.IsSuccess);
        Assert.False(status.Value!.CanImportFirstBatch);
        Assert.True(status.Value.FirstBatchComplete);
        Assert.True(status.Value.CanImportNextBatch);
        Assert.Equal(2, status.Value.SuggestedNextBatchNumber);
        Assert.Equal(2, status.Value.NextBatchSizeEstimate); // DefaultBatchSize=2, remaining=2
    }

    [Fact]
    public async Task Status_WhenAllImported_DisablesSelectPath()
    {
        var products = new MemoryProducts();
        await products.AddImportedAsync(FirstA);
        await products.AddImportedAsync(FirstB);
        await products.AddImportedAsync(NextA);
        await products.AddImportedAsync(NextB);

        var status = await new GetTemplateImportStatus(products, new FakePlatform(BuildTemplate()))
            .ExecuteAsync(Org.Value, TemplateId, platformSessionToken: null);

        Assert.True(status.IsSuccess);
        Assert.False(status.Value!.CanImportFirstBatch);
        Assert.False(status.Value.CanImportNextBatch);
        Assert.True(status.Value.FirstBatchComplete);
        Assert.Equal(0, status.Value.SubsequentRemainingCount);
    }

    private static PlatformMerchantCatalogTemplateDto BuildTemplate() =>
        new(
            Id: TemplateId,
            Name: "Sari-Sari",
            Slug: "sari-sari",
            Description: null,
            IconReference: null,
            PrimaryBusinessType: "SariSari",
            PrimaryBusinessTypeId: Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
            Status: "Published",
            DefaultBatchSize: 2,
            SelectionMode: "Curated",
            PublishedAtUtc: DateTimeOffset.Parse("2026-08-01T00:00:00Z"),
            ProductCount: 4,
            FirstBatchCount: 2,
            Products:
            [
                new(Guid.NewGuid(), FirstA, 1, false, true, "A"),
                new(Guid.NewGuid(), FirstB, 2, false, true, "B"),
                new(Guid.NewGuid(), NextA, 3, false, false, "C"),
                new(Guid.NewGuid(), NextB, 4, false, false, "D")
            ],
            CreatedAtUtc: DateTimeOffset.Parse("2026-08-01T00:00:00Z"),
            UpdatedAtUtc: DateTimeOffset.Parse("2026-08-01T00:00:00Z"));

    private sealed class FakePlatform(PlatformMerchantCatalogTemplateDto template) : IPlatformMerchantCatalogClient
    {
        public Task<PlatformMerchantCatalogTemplateDto?> GetPublishedTemplateAsync(
            Guid templateId,
            string? platformSessionToken,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<PlatformMerchantCatalogTemplateDto?>(
                templateId == template.Id ? template : null);

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

    private sealed class MemoryProducts : ICatalogProductRepository
    {
        private readonly List<CatalogProduct> _items = [];

        public Task AddImportedAsync(Guid globalId)
        {
            _items.Add(CatalogProduct.CreateImportedSnapshot(
                Org,
                globalId.ToString("N")[..8],
                UnitOfMeasure.Piece,
                10m,
                globalId,
                CatalogSource.Template,
                DateTimeOffset.Parse("2026-08-01T00:00:00Z")));
            return Task.CompletedTask;
        }

        public Task<CatalogProduct?> GetByIdAsync(PosOrganizationId organizationId, CatalogProductId productId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(p => p.OrganizationId == organizationId && p.Id == productId));

        public Task<CatalogProduct?> FindByNormalizedSkuAsync(PosOrganizationId organizationId, string normalizedSku, CancellationToken cancellationToken = default) =>
            Task.FromResult<CatalogProduct?>(null);

        public Task<CatalogProduct?> FindByBarcodeAsync(PosOrganizationId organizationId, string barcode, CancellationToken cancellationToken = default) =>
            Task.FromResult<CatalogProduct?>(null);

        public Task<CatalogProduct?> FindByPlatformGlobalProductIdAsync(PosOrganizationId organizationId, Guid platformGlobalProductId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(p => p.OrganizationId == organizationId && p.PlatformGlobalProductId == platformGlobalProductId));

        public Task<IReadOnlySet<Guid>> ListPlatformGlobalProductIdsAsync(
            PosOrganizationId organizationId,
            IReadOnlyCollection<Guid> platformGlobalProductIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlySet<Guid>>(_items
                .Where(p => p.OrganizationId == organizationId
                            && p.PlatformGlobalProductId is Guid id
                            && platformGlobalProductIds.Contains(id))
                .Select(p => p.PlatformGlobalProductId!.Value)
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
            _items.Add(product);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(CatalogProduct product, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
