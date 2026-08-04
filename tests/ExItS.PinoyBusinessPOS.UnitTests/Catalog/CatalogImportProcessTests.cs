using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Permissions;

namespace ExItS.PinoyBusinessPOS.UnitTests.Catalog;

public sealed class CatalogImportProcessTests
{
    private static readonly PosOrganizationId OrgA = PosOrganizationId.From(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
    private static readonly PosOrganizationId OrgB = PosOrganizationId.From(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));
    private static readonly Guid GlobalId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public async Task Process_SkipsDuplicatePlatformGlobalProductId()
    {
        var clock = new FixedClock(DateTimeOffset.Parse("2026-08-05T00:00:00Z"));
        var products = new FakeProductRepository();
        var categories = new FakeCategoryRepository();
        var imports = new FakeImportRepository();
        var uow = new FakeUnitOfWork();

        var existing = CatalogProduct.CreateImportedSnapshot(
            OrgA,
            "Existing",
            UnitOfMeasure.Piece,
            10m,
            GlobalId,
            CatalogSource.Template,
            clock.UtcNow);
        await products.AddAsync(existing);

        var item = CatalogImportItemResult.CreatePending(GlobalId, 0, "Again", "Piece", 12m);
        var job = CatalogImportJob.CreateQueued(
            OrgA,
            PosCatalogImportJobKind.SelectedProducts,
            CatalogSource.GlobalSearch,
            "actor",
            [item],
            clock.UtcNow);
        await imports.AddAsync(job);

        var processor = new ProcessPosCatalogImportChunk(imports, products, categories, uow, clock);
        var worked = await processor.ExecuteOnceAsync();

        Assert.True(worked);
        var refreshed = await imports.GetByIdAsync(OrgA, job.Id);
        Assert.NotNull(refreshed);
        Assert.Equal(PosCatalogImportJobStatus.CompletedWithWarnings, refreshed!.Status);
        Assert.Equal(1, refreshed.SkippedCount);
        Assert.Equal(0, refreshed.ImportedCount);
        Assert.Equal(1, products.Count);
    }

    [Fact]
    public async Task Process_OrgIsolation_DoesNotSeeOtherOrgProducts()
    {
        var clock = new FixedClock(DateTimeOffset.Parse("2026-08-05T00:00:00Z"));
        var products = new FakeProductRepository();
        var categories = new FakeCategoryRepository();
        var imports = new FakeImportRepository();
        var uow = new FakeUnitOfWork();

        var otherOrgProduct = CatalogProduct.CreateImportedSnapshot(
            OrgB,
            "Other org",
            UnitOfMeasure.Piece,
            10m,
            GlobalId,
            CatalogSource.Template,
            clock.UtcNow);
        await products.AddAsync(otherOrgProduct);

        var item = CatalogImportItemResult.CreatePending(GlobalId, 0, "Org A product", "Piece", 15m);
        var job = CatalogImportJob.CreateQueued(
            OrgA,
            PosCatalogImportJobKind.SelectedProducts,
            CatalogSource.GlobalSearch,
            "actor",
            [item],
            clock.UtcNow);
        await imports.AddAsync(job);

        var processor = new ProcessPosCatalogImportChunk(imports, products, categories, uow, clock);
        await processor.ExecuteOnceAsync();

        var refreshed = await imports.GetByIdAsync(OrgA, job.Id);
        Assert.Equal(PosCatalogImportJobStatus.Completed, refreshed!.Status);
        Assert.Equal(1, refreshed.ImportedCount);
        var local = await products.FindByPlatformGlobalProductIdAsync(OrgA, GlobalId);
        Assert.NotNull(local);
        Assert.Equal(OrgA, local!.OrganizationId);
        Assert.Equal(15m, local.SellingPrice);
        Assert.Equal(CatalogSource.GlobalSearch, local.CatalogSource);
    }

    [Fact]
    public void Cashier_DoesNotAllowManageCatalog()
    {
        Assert.False(Application.Permissions.PosRoleMatrix.Allows(PosRole.Cashier, Application.Commercial.UtangCapability.ManageCatalog));
        Assert.True(Application.Permissions.PosRoleMatrix.Allows(PosRole.StoreManager, Application.Commercial.UtangCapability.ManageCatalog));
        Assert.True(Application.Permissions.PosRoleMatrix.Allows(PosRole.Owner, Application.Commercial.UtangCapability.ManageCatalog));
    }

    private sealed class FixedClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
    }

    private sealed class FakeUnitOfWork : IPosUnitOfWork
    {
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<T> ExecuteInSerializableTransactionAsync<T>(
            Func<CancellationToken, Task<T>> action,
            CancellationToken cancellationToken = default) =>
            action(cancellationToken);
    }

    private sealed class FakeProductRepository : ICatalogProductRepository
    {
        private readonly List<CatalogProduct> _items = [];
        public int Count => _items.Count;

        public Task<CatalogProduct?> GetByIdAsync(PosOrganizationId organizationId, CatalogProductId productId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(p => p.OrganizationId == organizationId && p.Id == productId));

        public Task<CatalogProduct?> FindByNormalizedSkuAsync(PosOrganizationId organizationId, string normalizedSku, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(p => p.OrganizationId == organizationId && p.NormalizedSku == normalizedSku));

        public Task<CatalogProduct?> FindByBarcodeAsync(PosOrganizationId organizationId, string barcode, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(p => p.OrganizationId == organizationId && p.Barcode == barcode));

        public Task<CatalogProduct?> FindByPlatformGlobalProductIdAsync(PosOrganizationId organizationId, Guid platformGlobalProductId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(p => p.OrganizationId == organizationId && p.PlatformGlobalProductId == platformGlobalProductId));

        public Task<IReadOnlySet<Guid>> ListPlatformGlobalProductIdsAsync(PosOrganizationId organizationId, IReadOnlyCollection<Guid> platformGlobalProductIds, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlySet<Guid>>(_items
                .Where(p => p.OrganizationId == organizationId && p.PlatformGlobalProductId is Guid id && platformGlobalProductIds.Contains(id))
                .Select(p => p.PlatformGlobalProductId!.Value)
                .ToHashSet());

        public Task<IReadOnlyList<CatalogProduct>> ListByIdsAsync(PosOrganizationId organizationId, IReadOnlyCollection<CatalogProductId> productIds, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CatalogProduct>>(_items.Where(p => p.OrganizationId == organizationId && productIds.Contains(p.Id)).ToList());

        public Task<(IReadOnlyList<CatalogProduct> Items, int TotalCount)> ListAsync(PosOrganizationId organizationId, CatalogProductFilter filter, int skip, int take, CancellationToken cancellationToken = default)
        {
            var items = _items.Where(p => p.OrganizationId == organizationId).Skip(skip).Take(take).ToList();
            return Task.FromResult<(IReadOnlyList<CatalogProduct>, int)>((items, _items.Count(p => p.OrganizationId == organizationId)));
        }

        public Task AddAsync(CatalogProduct product, CancellationToken cancellationToken = default)
        {
            _items.Add(product);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(CatalogProduct product, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FakeCategoryRepository : IProductCategoryRepository
    {
        private readonly List<ProductCategory> _items = [];

        public Task<ProductCategory?> GetByIdAsync(PosOrganizationId organizationId, ProductCategoryId categoryId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(c => c.OrganizationId == organizationId && c.Id == categoryId));

        public Task<ProductCategory?> FindActiveByNormalizedNameAsync(PosOrganizationId organizationId, string normalizedName, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(c => c.OrganizationId == organizationId && c.NormalizedName == normalizedName && c.Status == ProductCategoryStatus.Active));

        public Task<ProductCategory?> FindActiveBySourceGlobalCategoryIdAsync(PosOrganizationId organizationId, Guid sourceGlobalCategoryId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(c => c.OrganizationId == organizationId && c.SourceGlobalCategoryId == sourceGlobalCategoryId && c.Status == ProductCategoryStatus.Active));

        public Task<(IReadOnlyList<ProductCategory> Items, int TotalCount)> ListAsync(PosOrganizationId organizationId, ProductCategoryStatus? status, string? search, int skip, int take, CancellationToken cancellationToken = default) =>
            Task.FromResult<(IReadOnlyList<ProductCategory>, int)>(([], 0));

        public Task<IReadOnlyList<ProductCategory>> ListByIdsAsync(PosOrganizationId organizationId, IReadOnlyCollection<ProductCategoryId> categoryIds, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ProductCategory>>([]);

        public Task AddAsync(ProductCategory category, CancellationToken cancellationToken = default)
        {
            _items.Add(category);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(ProductCategory category, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FakeImportRepository : ICatalogImportJobRepository
    {
        private readonly List<CatalogImportJob> _jobs = [];

        public Task<CatalogImportJob?> GetByIdAsync(PosOrganizationId organizationId, CatalogImportJobId jobId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_jobs.FirstOrDefault(j => j.OrganizationId == organizationId && j.Id == jobId));

        public Task<CatalogImportJob?> FindByIdempotencyKeyAsync(PosOrganizationId organizationId, string idempotencyKey, CancellationToken cancellationToken = default) =>
            Task.FromResult(_jobs.FirstOrDefault(j => j.OrganizationId == organizationId && j.IdempotencyKey == idempotencyKey));

        public Task<CatalogImportJob?> ClaimNextAsync(DateTimeOffset utcNow, TimeSpan staleAfter, CancellationToken cancellationToken = default)
        {
            var staleBefore = utcNow - staleAfter;
            var job = _jobs
                .Where(j => j.Status == PosCatalogImportJobStatus.Queued
                            || (j.Status == PosCatalogImportJobStatus.Processing
                                && (j.LastHeartbeatAtUtc is null || j.LastHeartbeatAtUtc < staleBefore)))
                .OrderBy(j => j.CreatedAtUtc)
                .FirstOrDefault();
            return Task.FromResult(job);
        }

        public Task<(IReadOnlyList<CatalogImportItemResult> Items, int TotalCount)> ListItemsAsync(PosOrganizationId organizationId, CatalogImportJobId jobId, PosCatalogImportItemStatus? status, int skip, int take, CancellationToken cancellationToken = default)
        {
            var job = _jobs.FirstOrDefault(j => j.OrganizationId == organizationId && j.Id == jobId);
            if (job is null)
            {
                return Task.FromResult<(IReadOnlyList<CatalogImportItemResult>, int)>(([], 0));
            }

            var items = job.Items.Where(i => status is null || i.Status == status).Skip(skip).Take(take).ToList();
            return Task.FromResult<(IReadOnlyList<CatalogImportItemResult>, int)>((items, job.Items.Count));
        }

        public Task AddAsync(CatalogImportJob job, CancellationToken cancellationToken = default)
        {
            _jobs.Add(job);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(CatalogImportJob job, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
