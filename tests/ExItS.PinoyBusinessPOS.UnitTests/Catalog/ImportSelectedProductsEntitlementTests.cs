using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.UnitTests.Catalog;

public sealed class ImportSelectedProductsEntitlementTests
{
    private static readonly PosOrganizationId Org = PosOrganizationId.From(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
    private static readonly Guid EntitledId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid ForgedId = Guid.Parse("99999999-9999-9999-9999-999999999999");

    [Fact]
    public async Task Import_rejects_when_all_requested_ids_are_unentitled()
    {
        var platform = new FakePlatform(entitled: []);
        var useCase = new ImportSelectedProducts(
            new MemoryImports(),
            platform,
            new FakeUnitOfWork(),
            new FixedClock());

        var result = await useCase.ExecuteAsync(
            Org.Value,
            [ForgedId],
            requestedBy: Guid.NewGuid().ToString("D"),
            platformSessionToken: "session");

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.CatalogImportNoProducts, result.ErrorCode);
    }

    [Fact]
    public async Task Import_imports_only_entitled_products_and_skips_forged_ids()
    {
        var platform = new FakePlatform(entitled: [EntitledId]);
        var useCase = new ImportSelectedProducts(
            new MemoryImports(),
            platform,
            new FakeUnitOfWork(),
            new FixedClock());

        var result = await useCase.ExecuteAsync(
            Org.Value,
            [EntitledId, ForgedId],
            requestedBy: Guid.NewGuid().ToString("D"),
            platformSessionToken: "session");

        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.Equal(1, result.Value!.TotalCount);
    }

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
            Task.FromResult(
                entitled.Contains(productId)
                    ? Build(productId)
                    : null);

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

        private static PlatformMerchantGlobalProductDto Build(Guid id) =>
            new(
                id,
                "Entitled Product",
                null,
                "SKU-1",
                null,
                "Brand",
                null,
                "Piece",
                5m,
                10m,
                null,
                "Active",
                DateTimeOffset.Parse("2026-08-01T00:00:00Z"),
                DateTimeOffset.Parse("2026-08-01T00:00:00Z"));
    }

    private sealed class MemoryImports : ICatalogImportJobRepository
    {
        private readonly List<CatalogImportJob> _jobs = [];

        public Task AddAsync(CatalogImportJob job, CancellationToken cancellationToken = default)
        {
            _jobs.Add(job);
            return Task.CompletedTask;
        }

        public Task<CatalogImportJob?> ClaimNextAsync(DateTimeOffset utcNow, TimeSpan staleAfter, CancellationToken cancellationToken = default) =>
            Task.FromResult<CatalogImportJob?>(null);

        public Task<CatalogImportJob?> FindByIdempotencyKeyAsync(PosOrganizationId organizationId, string idempotencyKey, CancellationToken cancellationToken = default) =>
            Task.FromResult(_jobs.FirstOrDefault(j => j.OrganizationId == organizationId && j.IdempotencyKey == idempotencyKey));

        public Task<CatalogImportJob?> GetByIdAsync(PosOrganizationId organizationId, CatalogImportJobId jobId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_jobs.FirstOrDefault(j => j.OrganizationId == organizationId && j.Id == jobId));

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
}
