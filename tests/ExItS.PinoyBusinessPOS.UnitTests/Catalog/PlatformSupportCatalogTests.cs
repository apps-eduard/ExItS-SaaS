using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;

namespace ExItS.PinoyBusinessPOS.UnitTests.Catalog;

public sealed class PlatformSupportCatalogTests
{
    private static readonly Guid OrgA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid OrgB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    [Theory]
    [InlineData(null, null, false)]
    [InlineData("", "anything", false)]
    [InlineData("dev-key", null, false)]
    [InlineData("dev-key", "", false)]
    [InlineData("dev-key", "wrong", false)]
    [InlineData("dev-key", "dev-key", true)]
    public void Support_api_key_guard_denies_missing_or_wrong_key(string? configured, string? provided, bool expected)
    {
        Assert.Equal(expected, PlatformSupportApiKeyGuard.IsAuthorized(configured, provided));
    }

    [Fact]
    public void Provenance_prefers_template_then_global_then_merchant()
    {
        var templateId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var globalId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        Assert.Equal(
            PlatformSupportCatalogProvenance.GlobalTemplate,
            PlatformSupportCatalogProvenance.ResolveSourceType(templateId, globalId));
        Assert.Equal(
            PlatformSupportCatalogProvenance.GlobalCatalog,
            PlatformSupportCatalogProvenance.ResolveSourceType(null, globalId));
        Assert.Equal(
            PlatformSupportCatalogProvenance.MerchantCreated,
            PlatformSupportCatalogProvenance.ResolveSourceType(null, null));
    }

    [Fact]
    public async Task Query_scopes_repository_calls_to_exact_organization_id()
    {
        var products = new RecordingProductRepository();
        var useCase = new GetOrganizationCatalogForPlatformSupport(
            products,
            new StubInventoryRepository(),
            new StubCategoryRepository());

        await useCase.ExecuteAsync(OrgA, page: 1, pageSize: 20);

        Assert.Contains(OrgA, products.ListedOrganizationIds);
        Assert.DoesNotContain(OrgB, products.ListedOrganizationIds);
        Assert.All(products.ListedOrganizationIds, id => Assert.Equal(OrgA, id));
    }

    private sealed class RecordingProductRepository : ICatalogProductRepository
    {
        public List<Guid> ListedOrganizationIds { get; } = [];

        public Task<CatalogProduct?> GetByIdAsync(
            PosOrganizationId organizationId,
            CatalogProductId productId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<CatalogProduct?>(null);

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
            CancellationToken cancellationToken = default)
        {
            ListedOrganizationIds.Add(organizationId.Value);
            return Task.FromResult<(IReadOnlyList<CatalogProduct>, int)>(([], 0));
        }

        public Task<CatalogProduct?> FindByPlatformGlobalProductIdAsync(
            PosOrganizationId organizationId,
            Guid platformGlobalProductId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<CatalogProduct?>(null);

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


        public Task<IReadOnlySet<Guid>> ListPlatformGlobalProductIdsAsync(
            PosOrganizationId organizationId,
            IReadOnlyCollection<Guid> platformGlobalProductIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlySet<Guid>>(new HashSet<Guid>());

        public Task AddAsync(CatalogProduct product, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task UpdateAsync(CatalogProduct product, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    /// <summary>Minimal inventory stub — only methods used by the support catalog query.</summary>
    private sealed class StubInventoryRepository : IInventoryRepository
    {
        public Task<IReadOnlyList<InventoryAccount>> ListByProductIdsAsync(
            PosOrganizationId organizationId,
            IReadOnlyCollection<CatalogProductId> productIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<InventoryAccount>>([]);

        public Task<InventoryAccount?> GetByProductIdAsync(
            PosOrganizationId organizationId,
            CatalogProductId productId,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<(IReadOnlyList<InventoryAccount> Items, int TotalCount)> ListAsync(
            PosOrganizationId organizationId,
            InventoryAccountFilter filter,
            int skip,
            int take,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<(IReadOnlyList<InventoryAccount> Items, int TotalCount)> ListLowStockAsync(
            PosOrganizationId organizationId,
            string? search,
            int skip,
            int take,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<IReadOnlyList<InventoryAccount>> ListAllAccountsAsync(
            PosOrganizationId organizationId,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task AddAccountAsync(InventoryAccount account, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task UpdateAccountAsync(InventoryAccount account, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task ExecuteWithProductReservationLocksAsync(
            PosOrganizationId organizationId,
            IReadOnlyCollection<CatalogProductId> productIds,
            Func<IReadOnlyList<InventoryAccount>, CancellationToken, Task> action,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task AddMovementAsync(StockMovement movement, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> HasAnyMovementAsync(
            PosOrganizationId organizationId,
            CatalogProductId productId,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<bool> HasOpeningStockAsync(
            PosOrganizationId organizationId,
            CatalogProductId productId,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<(IReadOnlyList<StockMovement> Items, int TotalCount)> ListMovementsAsync(
            PosOrganizationId organizationId,
            CatalogProductId productId,
            StockMovementFilter filter,
            int skip,
            int take,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<decimal> SumMovementEffectsAsync(
            PosOrganizationId organizationId,
            CatalogProductId productId,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<(IReadOnlyList<InventoryAccount> Items, int TotalCount)> ListReorderSuggestionsAsync(
            PosOrganizationId organizationId,
            string? search,
            int skip,
            int take,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<bool> HasStockCountVarianceAsync(
            PosOrganizationId organizationId,
            Domain.Inventory.StockCountId stockCountId,
            CatalogProductId productId,
            StockMovementType movementType,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<IReadOnlyList<StockMovement>> ListMovementsForReportAsync(
            PosOrganizationId organizationId,
            DateOnly fromDateUtc,
            DateOnly toDateUtc,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<IReadOnlyList<StockMovement>> ListSaleDeductionsAsync(
            PosOrganizationId organizationId,
            Domain.Sales.SaleId saleId,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<bool> HasSaleDeductionAsync(
            PosOrganizationId organizationId,
            Domain.Sales.SaleId saleId,
            CatalogProductId productId,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<bool> HasCustomerOrderDeductionAsync(
            PosOrganizationId organizationId,
            Domain.CustomerOrdering.CustomerOrderId orderId,
            CatalogProductId productId,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<bool> HasSaleVoidRestorationAsync(
            PosOrganizationId organizationId,
            Domain.Sales.SaleId saleId,
            CatalogProductId productId,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<bool> HasPurchaseReceiptAsync(
            PosOrganizationId organizationId,
            Domain.Purchasing.GoodsReceiptId goodsReceiptId,
            CatalogProductId productId,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<bool> HasSaleReturnRestockAsync(
            PosOrganizationId organizationId,
            Domain.Returns.SaleReturnId saleReturnId,
            CatalogProductId productId,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<bool> HasInventoryTransferMovementAsync(
            PosOrganizationId organizationId,
            Domain.Inventory.InventoryTransferId transferId,
            CatalogProductId productId,
            Domain.Inventory.StockMovementType movementType,
            Domain.Inventory.InventoryLotId? lotId = null,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<(DateTimeOffset? LatestAt, int Count)> GetMovementSummaryAsync(
            PosOrganizationId organizationId,
            CatalogProductId productId,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<IReadOnlyDictionary<Guid, (DateTimeOffset? LatestAt, int Count)>> GetMovementSummariesAsync(
            PosOrganizationId organizationId,
            IReadOnlyCollection<CatalogProductId> productIds,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class StubCategoryRepository : IProductCategoryRepository
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
}
