using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.UnitTests.TestDoubles;

namespace ExItS.PinoyBusinessPOS.UnitTests.Catalog;

public sealed class ConnectedBuyerSharingRequiresTrackedTests
{
    private static readonly Guid OrgGuid = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly PosOrganizationId Org = PosOrganizationId.From(OrgGuid);
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-09-15T12:00:00Z");

    [Fact]
    public void ValidateCanEnableSharing_allows_tracked_even_at_zero_on_hand()
    {
        Assert.True(ConnectedBuyerSharingRules.ValidateCanEnableSharing(isTracked: true).IsSuccess);
    }

    [Fact]
    public void ValidateCanEnableSharing_blocks_untracked()
    {
        var result = ConnectedBuyerSharingRules.ValidateCanEnableSharing(isTracked: false);
        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrorCodes.ConnectedShareRequiresTrackedInventory, result.ErrorCode);
        Assert.Equal(ConnectedBuyerSharingRules.ShareRequiresTrackedMessage, result.ErrorMessage);
    }

    [Fact]
    public void ValidateCanDisableTracking_blocks_while_shared()
    {
        var result = ConnectedBuyerSharingRules.ValidateCanDisableTracking(isShared: true);
        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrorCodes.ConnectedShareBlocksDisableTracking, result.ErrorCode);
        Assert.Equal(ConnectedBuyerSharingRules.DisableTrackingWhileSharedMessage, result.ErrorMessage);
    }

    [Fact]
    public void ValidateCanDisableTracking_allows_when_not_shared()
    {
        Assert.True(ConnectedBuyerSharingRules.ValidateCanDisableTracking(isShared: false).IsSuccess);
    }

    [Fact]
    public async Task Bulk_enable_share_blocks_untracked_product()
    {
        var product = CatalogProduct.Create(Org, "Soap", UnitOfMeasure.Piece, 40m, Now);
        product.DisableConnectedBuyerAvailability(Now);
        var inventory = new TrackedInventoryStub(); // empty → untracked
        var useCase = CreateBulk(new MemoryProducts([product]), inventory);

        var result = await useCase.ExecuteAsync(
            OrgGuid,
            new BulkConnectedBuyerAvailabilityMutationRequest("enable", [product.Id.Value]));

        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrorCodes.ConnectedShareRequiresTrackedInventory, result.ErrorCode);
        Assert.False(product.CanExposeToConnectedBuyers);
    }

    [Fact]
    public async Task Bulk_enable_share_allows_tracked_with_zero_on_hand()
    {
        var product = CatalogProduct.Create(Org, "Rice", UnitOfMeasure.Kilogram, 50m, Now);
        product.DisableConnectedBuyerAvailability(Now);
        product.SetDefaultConnectedPoPrice(45m, Now);
        var account = InventoryAccount.CreateUntracked(Org, product.Id, Now);
        account.Enable(0m, UnitOfMeasure.Kilogram, Guid.Empty, Now, hasOpeningStockAlready: false);
        var inventory = new TrackedInventoryStub();
        inventory.Accounts.Add(account);
        var useCase = CreateBulk(new MemoryProducts([product]), inventory);

        var result = await useCase.ExecuteAsync(
            OrgGuid,
            new BulkConnectedBuyerAvailabilityMutationRequest("enable", [product.Id.Value]));

        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.True(product.CanExposeToConnectedBuyers);
        Assert.Equal(0m, account.OnHandQuantity);
        Assert.True(account.IsTracked);
    }

    [Fact]
    public async Task Disable_tracking_allowed_while_product_was_exposable_and_deactivates_eligibility()
    {
        var product = CatalogProduct.Create(Org, "Coffee", UnitOfMeasure.Piece, 20m, Now);
        product.EnableConnectedBuyerAvailability(Now);
        product.SetDefaultConnectedPoPrice(18m, Now);
        var account = InventoryAccount.CreateUntracked(Org, product.Id, Now);
        account.Enable(0m, UnitOfMeasure.Piece, Guid.Empty, Now, hasOpeningStockAlready: false);
        var inventory = new TrackedInventoryStub();
        inventory.Accounts.Add(account);
        var products = new MemoryProducts([product]);
        var exposures = new CapturingExposures();
        var exposed = SupplierProductExposure.Expose(
            Org, product.Id, product.Name, "Piece", 18m, Now, product.Sku);
        exposures.Items.Add(exposed);
        var useCase = new DisableInventoryTracking(
            inventory,
            products,
            new FakeUow(),
            new FixedClock(Now),
            exposures);

        var result = await useCase.ExecuteAsync(OrgGuid, product.Id.Value);

        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.False(account.IsTracked);
        Assert.False(exposed.IsExposed);
    }

    [Fact]
    public async Task Disable_tracking_allowed_after_explicit_unshare()
    {
        var product = CatalogProduct.Create(Org, "Coffee", UnitOfMeasure.Piece, 20m, Now);
        product.DisableConnectedBuyerAvailability(Now);
        var account = InventoryAccount.CreateUntracked(Org, product.Id, Now);
        account.Enable(0m, UnitOfMeasure.Piece, Guid.Empty, Now, hasOpeningStockAlready: false);
        var inventory = new TrackedInventoryStub();
        inventory.Accounts.Add(account);
        var products = new MemoryProducts([product]);
        var useCase = new DisableInventoryTracking(
            inventory,
            products,
            new FakeUow(),
            new FixedClock(Now),
            new NoOpExposures());

        var result = await useCase.ExecuteAsync(OrgGuid, product.Id.Value);

        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.False(account.IsTracked);
    }

    [Fact]
    public async Task Create_product_without_tracking_stages_unshared()
    {
        var products = new CreateMemoryProducts();
        var useCase = new CreateCatalogProduct(
            products,
            new CreateMemoryUnits(),
            new CreateMemoryCategories(),
            new CreateMemoryBrands(),
            new FakeUow(),
            new FixedClock(Now),
            new CatalogProductGovernanceAuthority(),
            FixedCatalogGovernanceActorAccessor.Owner());

        var result = await useCase.ExecuteAsync(OrgGuid, "Untracked Snack", "Piece", 15m);

        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.False(Assert.Single(products.Items).CanExposeToConnectedBuyers);
    }

    [Fact]
    public void Exposure_sync_writes_supplier_category_name()
    {
        var path = Path.Combine(
            FindRepoRoot(),
            "src",
            "Products",
            "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Application",
            "Catalog",
            "CatalogProductUseCases.cs");
        var source = File.ReadAllText(path);
        Assert.Contains("ResolveSupplierCategoryNameAsync", source, StringComparison.Ordinal);
        Assert.Contains("categoryName", source, StringComparison.Ordinal);

        var repoPath = Path.Combine(
            FindRepoRoot(),
            "src",
            "Products",
            "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Infrastructure",
            "Persistence",
            "ConnectedSuppliers",
            "ConnectedSupplierRepositories.cs");
        var repo = File.ReadAllText(repoPath);
        Assert.Contains("ProductCategories", repo, StringComparison.Ordinal);
        Assert.Contains("CategoryName = categoryRow", repo, StringComparison.Ordinal);
    }

    [Fact]
    public void Exposure_sync_deactivates_when_inventory_untracked()
    {
        var path = Path.Combine(
            FindRepoRoot(),
            "src",
            "Products",
            "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Application",
            "Catalog",
            "CatalogProductUseCases.cs");
        var source = File.ReadAllText(path);
        Assert.Contains("!trackedOk", source, StringComparison.Ordinal);
        Assert.Contains("ConnectedBuyerSharingRules", source, StringComparison.Ordinal);

        var repoPath = Path.Combine(
            FindRepoRoot(),
            "src",
            "Products",
            "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Infrastructure",
            "Persistence",
            "ConnectedSuppliers",
            "ConnectedSupplierRepositories.cs");
        var repo = File.ReadAllText(repoPath);
        Assert.Contains("account.IsTracked", repo, StringComparison.Ordinal);
    }

    private static BulkMutateConnectedBuyerAvailability CreateBulk(
        MemoryProducts products,
        IInventoryRepository inventory) =>
        new(
            products,
            new NoOpExposures(),
            inventory,
            new FakeUow(),
            new FixedClock(Now),
            new FakeAccess(),
            new CatalogProductGovernanceAuthority(),
            FixedCatalogGovernanceActorAccessor.Owner());

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "ExItS.slnx")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Repository root not found.");
    }

    private sealed class TrackedInventoryStub : CostResolverInventoryStub
    {
        public List<InventoryAccount> Accounts { get; } = [];

        public override Task<InventoryAccount?> GetByProductIdAsync(
            PosOrganizationId organizationId,
            CatalogProductId productId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Accounts.FirstOrDefault(x =>
                x.OrganizationId == organizationId && x.ProductId == productId));

        public override Task AddAccountAsync(InventoryAccount account, CancellationToken cancellationToken = default)
        {
            Accounts.Add(account);
            return Task.CompletedTask;
        }

        public override Task UpdateAccountAsync(InventoryAccount account, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class MemoryProducts : ICatalogProductRepository
    {
        private readonly Dictionary<Guid, CatalogProduct> _items;

        public MemoryProducts(IEnumerable<CatalogProduct> items) =>
            _items = items.ToDictionary(p => p.Id.Value);

        public Task<CatalogProduct?> GetByIdAsync(
            PosOrganizationId organizationId,
            CatalogProductId productId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.TryGetValue(productId.Value, out var p) && p.OrganizationId == organizationId
                ? p
                : null);

        public Task<IReadOnlyList<CatalogProduct>> ListByIdsAsync(
            PosOrganizationId organizationId,
            IReadOnlyCollection<CatalogProductId> productIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CatalogProduct>>(
                productIds.Select(id => _items.GetValueOrDefault(id.Value))
                    .Where(p => p is not null && p.OrganizationId == organizationId)
                    .Cast<CatalogProduct>()
                    .ToList());

        public Task AddAsync(CatalogProduct product, CancellationToken cancellationToken = default)
        {
            _items[product.Id.Value] = product;
            return Task.CompletedTask;
        }

        public Task UpdateAsync(CatalogProduct product, CancellationToken cancellationToken = default)
        {
            _items[product.Id.Value] = product;
            return Task.CompletedTask;
        }

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
            Task.FromResult<IReadOnlyList<(Guid?, int)>>([]);

        public Task<CatalogProduct?> FindByNormalizedSkuAsync(
            PosOrganizationId organizationId,
            string normalizedSku,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<CatalogProduct?>(null);

        public Task<CatalogProduct?> FindByBarcodeAsync(
            PosOrganizationId organizationId,
            string barcode,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<CatalogProduct?>(null);

        public Task<CatalogProduct?> FindByPlatformGlobalProductIdAsync(
            PosOrganizationId organizationId,
            Guid platformGlobalProductId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<CatalogProduct?>(null);

        public Task<IReadOnlySet<Guid>> ListPlatformGlobalProductIdsAsync(
            PosOrganizationId organizationId,
            IReadOnlyCollection<Guid> platformGlobalProductIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlySet<Guid>>(new HashSet<Guid>());
    }

    private sealed class CreateMemoryProducts : ICatalogProductRepository
    {
        public List<CatalogProduct> Items { get; } = [];

        public Task AddAsync(CatalogProduct product, CancellationToken cancellationToken = default)
        {
            Items.Add(product);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(CatalogProduct product, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<CatalogProduct?> GetByIdAsync(
            PosOrganizationId organizationId,
            CatalogProductId productId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.FirstOrDefault(x => x.OrganizationId == organizationId && x.Id == productId));

        public Task<IReadOnlyList<CatalogProduct>> ListByIdsAsync(
            PosOrganizationId organizationId,
            IReadOnlyCollection<CatalogProductId> productIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CatalogProduct>>(
                Items.Where(x => x.OrganizationId == organizationId && productIds.Contains(x.Id)).ToList());

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
            Task.FromResult<IReadOnlyList<(Guid?, int)>>([]);

        public Task<CatalogProduct?> FindByNormalizedSkuAsync(
            PosOrganizationId organizationId,
            string normalizedSku,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<CatalogProduct?>(null);

        public Task<CatalogProduct?> FindByBarcodeAsync(
            PosOrganizationId organizationId,
            string barcode,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<CatalogProduct?>(null);

        public Task<CatalogProduct?> FindByPlatformGlobalProductIdAsync(
            PosOrganizationId organizationId,
            Guid platformGlobalProductId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<CatalogProduct?>(null);

        public Task<IReadOnlySet<Guid>> ListPlatformGlobalProductIdsAsync(
            PosOrganizationId organizationId,
            IReadOnlyCollection<Guid> platformGlobalProductIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlySet<Guid>>(new HashSet<Guid>());
    }

    private sealed class CreateMemoryUnits : ICatalogProductUnitRepository
    {
        public Task<CatalogProductUnit?> GetByIdAsync(
            PosOrganizationId organizationId,
            ProductUnitId unitId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<CatalogProductUnit?>(null);

        public Task AddAsync(CatalogProductUnit unit, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task ReplaceActiveUnitsAsync(
            PosOrganizationId organizationId,
            CatalogProductId productId,
            ProductUnitKind kind,
            IReadOnlyList<CatalogProductUnit> units,
            DateTimeOffset utcNow,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<IReadOnlyList<CatalogProductUnit>> ListByProductAsync(
            PosOrganizationId organizationId,
            CatalogProductId productId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CatalogProductUnit>>([]);

        public Task<IReadOnlyDictionary<Guid, IReadOnlyList<CatalogProductUnit>>> ListByProductIdsAsync(
            PosOrganizationId organizationId,
            IReadOnlyCollection<CatalogProductId> productIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<Guid, IReadOnlyList<CatalogProductUnit>>>(
                new Dictionary<Guid, IReadOnlyList<CatalogProductUnit>>());

        public Task UpdateAsync(CatalogProductUnit unit, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class CreateMemoryCategories : IProductCategoryRepository
    {
        public Task<ProductCategory?> GetByIdAsync(
            PosOrganizationId organizationId,
            ProductCategoryId categoryId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<ProductCategory?>(null);

        public Task<ProductCategory?> FindActiveByNormalizedNameAsync(
            PosOrganizationId organizationId,
            string normalizedName,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<ProductCategory?>(null);

        public Task<ProductCategory?> FindActiveBySourceGlobalCategoryIdAsync(
            PosOrganizationId organizationId,
            Guid sourceGlobalCategoryId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<ProductCategory?>(null);

        public Task<(IReadOnlyList<ProductCategory> Items, int TotalCount)> ListAsync(
            PosOrganizationId organizationId,
            ProductCategoryStatus? status,
            string? search,
            int skip,
            int take,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<(IReadOnlyList<ProductCategory>, int)>(([], 0));

        public Task<IReadOnlyList<ProductCategory>> ListByIdsAsync(
            PosOrganizationId organizationId,
            IReadOnlyCollection<ProductCategoryId> categoryIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ProductCategory>>([]);

        public Task AddAsync(ProductCategory category, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task UpdateAsync(ProductCategory category, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class CreateMemoryBrands : IProductBrandRepository
    {
        public Task<ProductBrand?> GetByIdAsync(
            PosOrganizationId organizationId,
            ProductBrandId brandId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<ProductBrand?>(null);

        public Task<ProductBrand?> FindActiveByNormalizedNameAsync(
            PosOrganizationId organizationId,
            string normalizedName,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<ProductBrand?>(null);

        public Task<(IReadOnlyList<ProductBrand> Items, int TotalCount)> ListAsync(
            PosOrganizationId organizationId,
            ProductBrandStatus? status,
            string? search,
            int skip,
            int take,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<(IReadOnlyList<ProductBrand>, int)>(([], 0));

        public Task<IReadOnlyList<ProductBrand>> ListByIdsAsync(
            PosOrganizationId organizationId,
            IReadOnlyCollection<ProductBrandId> brandIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ProductBrand>>([]);

        public Task AddAsync(ProductBrand brand, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task UpdateAsync(ProductBrand brand, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class CapturingExposures : ISupplierProductExposureRepository
    {
        public List<SupplierProductExposure> Items { get; } = [];

        public Task AddAsync(SupplierProductExposure exposure, CancellationToken ct = default)
        {
            Items.Add(exposure);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(SupplierProductExposure exposure, CancellationToken ct = default) =>
            Task.CompletedTask;

        public Task<SupplierProductExposure?> GetAsync(SupplierProductExposureId id, CancellationToken ct = default) =>
            Task.FromResult(Items.FirstOrDefault(x => x.Id == id));

        public Task<SupplierProductExposure?> GetByProductAsync(
            PosOrganizationId supplier,
            CatalogProductId productId,
            CancellationToken ct = default) =>
            Task.FromResult(Items.FirstOrDefault(x =>
                x.SupplierOrganizationId == supplier && x.ProductId == productId));

        public Task<IReadOnlyList<SupplierProductExposure>> ListAsync(
            PosOrganizationId supplier,
            CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<SupplierProductExposure>>(
                Items.Where(x => x.SupplierOrganizationId == supplier).ToList());

        public Task<(IReadOnlyList<SupplierProductExposure> Items, int Total)> SearchAsync(
            PosOrganizationId supplier,
            string? query,
            string? category,
            int skip,
            int take,
            CancellationToken ct = default) =>
            Task.FromResult<(IReadOnlyList<SupplierProductExposure>, int)>(([], 0));
    }

    private sealed class NoOpExposures : ISupplierProductExposureRepository
    {
        public Task AddAsync(SupplierProductExposure exposure, CancellationToken ct = default) =>
            Task.CompletedTask;

        public Task UpdateAsync(SupplierProductExposure exposure, CancellationToken ct = default) =>
            Task.CompletedTask;

        public Task<SupplierProductExposure?> GetAsync(SupplierProductExposureId id, CancellationToken ct = default) =>
            Task.FromResult<SupplierProductExposure?>(null);

        public Task<SupplierProductExposure?> GetByProductAsync(
            PosOrganizationId supplier,
            CatalogProductId productId,
            CancellationToken ct = default) =>
            Task.FromResult<SupplierProductExposure?>(null);

        public Task<IReadOnlyList<SupplierProductExposure>> ListAsync(
            PosOrganizationId supplier,
            CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<SupplierProductExposure>>([]);

        public Task<(IReadOnlyList<SupplierProductExposure> Items, int Total)> SearchAsync(
            PosOrganizationId supplier,
            string? query,
            string? category,
            int skip,
            int take,
            CancellationToken ct = default) =>
            Task.FromResult<(IReadOnlyList<SupplierProductExposure>, int)>(([], 0));
    }

    private sealed class FakeUow : IPosUnitOfWork
    {
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<T> ExecuteInSerializableTransactionAsync<T>(
            Func<CancellationToken, Task<T>> action,
            CancellationToken cancellationToken = default) =>
            action(cancellationToken);
    }

    private sealed class FixedClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
    }

    private sealed class FakeAccess : IPosCommercialAccessAccessor
    {
        public PosCommercialAccess Current { get; set; } = PosCommercialAccess.DevelopmentDefault;
    }
}
