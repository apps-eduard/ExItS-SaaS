using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.CustomerOrdering;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.CustomerOrdering;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Purchasing;
using ExItS.PinoyBusinessPOS.Domain.Returns;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.UnitTests.CustomerOrdering;

/// <summary>
/// Personal storefront is not a public marketplace: storefront / quote / place must fail closed
/// when the Personal↔seller link or seller ordering entitlement is unavailable.
/// </summary>
public sealed class PersonalStorefrontAuthorizationTests
{
    private static readonly Guid Seller = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid OtherSeller = Guid.Parse("99999999-9999-9999-9999-999999999999");
    private static readonly Guid Branch = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly Guid Actor = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
    private static readonly Guid ProductGuid = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly DateTimeOffset Utc = new(2026, 8, 17, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Unlinked_personal_cannot_get_storefront_by_seller_org_id()
    {
        var capability = new MutableCapability(canOrder: false, canDelivery: false);
        var catalogTouched = false;
        var useCase = CreateStorefront(capability, onCatalog: () => catalogTouched = true);

        var result = await useCase.ExecuteAsync(Seller, search: null, categoryId: null, page: 1, pageSize: 20);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.CustomerOrderOrderingUnavailable, result.ErrorCode);
        Assert.False(catalogTouched);
    }

    [Fact]
    public async Task Revoked_or_inactive_link_cannot_get_storefront()
    {
        var capability = new MutableCapability(canOrder: false, canDelivery: false);
        var useCase = CreateStorefront(capability);

        var result = await useCase.ExecuteAsync(Seller, null, null, 1, 20);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.CustomerOrderOrderingUnavailable, result.ErrorCode);
    }

    [Fact]
    public async Task Unlinked_or_revoked_personal_cannot_place_order()
    {
        var orders = new FakeOrders();
        var stock = new CountingStock();
        var useCase = CreatePlace(
            new MutableCapability(canOrder: false, canDelivery: false),
            orders,
            stock);

        var result = await useCase.ExecuteAsync(Seller, PickupRequest(), Actor);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.CustomerOrderOrderingUnavailable, result.ErrorCode);
        Assert.Equal(0, orders.PlaceCount);
        Assert.Equal(0, stock.EnsureCallCount);
    }

    [Fact]
    public async Task Link_revoked_after_storefront_load_causes_place_to_fail_without_order_or_reservation()
    {
        var capability = new MutableCapability(canOrder: true, canDelivery: true);
        var storefront = CreateStorefront(capability);
        var loaded = await storefront.ExecuteAsync(Seller, null, null, 1, 20);
        Assert.True(loaded.IsSuccess, loaded.ErrorMessage);

        capability.CanOrder = false;
        capability.CanDelivery = false;

        var orders = new FakeOrders();
        var stock = new CountingStock();
        var place = CreatePlace(capability, orders, stock);
        var result = await place.ExecuteAsync(Seller, PickupRequest(), Actor);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.CustomerOrderOrderingUnavailable, result.ErrorCode);
        Assert.Equal(0, orders.PlaceCount);
        Assert.Equal(0, stock.EnsureCallCount);
    }

    [Fact]
    public async Task Unlinked_or_revoked_personal_cannot_obtain_delivery_quote()
    {
        var branches = new CountingBranches();
        var quote = new QuoteCustomerOrderDelivery(
            branches,
            new MutableCapability(canOrder: false, canDelivery: false));

        var result = await quote.ExecuteAsync(Seller, QuoteRequest());

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.CustomerOrderOrderingUnavailable, result.ErrorCode);
        Assert.Equal(0, branches.GetBranchCallCount);
    }

    [Fact]
    public async Task Delivery_quote_requires_CanCustomerDelivery()
    {
        var branches = new CountingBranches();
        var quote = new QuoteCustomerOrderDelivery(
            branches,
            new MutableCapability(canOrder: true, canDelivery: false));

        var result = await quote.ExecuteAsync(Seller, QuoteRequest());

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.CustomerOrderOrderingUnavailable, result.ErrorCode);
        Assert.Equal(0, branches.GetBranchCallCount);
    }

    [Fact]
    public async Task Linked_personal_pickup_and_delivery_quote_remain_available()
    {
        var capability = new MutableCapability(canOrder: true, canDelivery: true);
        var storefront = await CreateStorefront(capability).ExecuteAsync(Seller, null, null, 1, 20);
        Assert.True(storefront.IsSuccess, storefront.ErrorMessage);
        Assert.True(storefront.Value!.CanCustomerOrder);
        Assert.True(storefront.Value.CanCustomerDelivery);

        var pickup = await CreatePlace(capability).ExecuteAsync(Seller, PickupRequest(), Actor);
        Assert.True(pickup.IsSuccess, pickup.ErrorMessage);
        Assert.Equal("Pickup", pickup.Value!.FulfillmentType);

        var quote = await new QuoteCustomerOrderDelivery(new CountingBranches(), capability)
            .ExecuteAsync(Seller, QuoteRequest());
        Assert.True(quote.IsSuccess, quote.ErrorMessage);
        Assert.True(quote.Value!.Available);

        var delivery = await CreatePlace(capability).ExecuteAsync(Seller, DeliveryRequest(), Actor);
        Assert.True(delivery.IsSuccess, delivery.ErrorMessage);
        Assert.Equal("Delivery", delivery.Value!.FulfillmentType);
    }

    [Fact]
    public async Task Capability_for_other_seller_does_not_authorize_unlinked_seller_org()
    {
        // ResolveAsync is scoped by sellerOrganizationId; a deny for OtherSeller must not open Seller.
        var capability = new SellerScopedCapability(allowedSellerId: OtherSeller, canOrder: true, canDelivery: true);
        var result = await CreateStorefront(capability).ExecuteAsync(Seller, null, null, 1, 20);
        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.CustomerOrderOrderingUnavailable, result.ErrorCode);
    }

    private static QuoteCustomerOrderDeliveryRequest QuoteRequest() =>
        new(Branch, MerchandiseSubtotal: 100m, DestinationLatitude: 14.6m, DestinationLongitude: 120.98m);

    private static readonly Guid BusinessCustomer = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private static PlaceCustomerOrderRequest PickupRequest() =>
        new(
            "Pickup",
            Branch,
            "Personal",
            "Ana",
            Actor,
            BusinessCustomer,
            null,
            null,
            [new PlaceCustomerOrderLineRequest(ProductGuid, 2m)]);

    private static PlaceCustomerOrderRequest DeliveryRequest() =>
        new(
            "Delivery",
            Branch,
            "Personal",
            "Ana",
            Actor,
            BusinessCustomer,
            null,
            null,
            [new PlaceCustomerOrderLineRequest(ProductGuid, 1m)],
            new PlaceCustomerOrderDeliveryRequest(
                "Ana",
                "09171234567",
                "123 Main",
                null,
                "Manila",
                null,
                14.6m,
                120.98m));

    private static GetCustomerStorefront CreateStorefront(
        ISellerCustomerOrderingCapability capability,
        Action? onCatalog = null) =>
        new(
            capability,
            new FakeProducts(onCatalog),
            new FakeCategories(),
            new FakeInventory(),
            new CountingBranches(),
            new EmptyImages());

    private static PlaceCustomerOrder CreatePlace(
        ISellerCustomerOrderingCapability capability,
        FakeOrders? orders = null,
        CountingStock? stock = null) =>
        new(
            orders ?? new FakeOrders(),
            new FakeProducts(),
            new CountingBranches(),
            stock ?? new CountingStock(),
            new FixedClock(Utc),
            capability);

    private sealed class MutableCapability(bool canOrder, bool canDelivery) : ISellerCustomerOrderingCapability
    {
        public bool CanOrder { get; set; } = canOrder;
        public bool CanDelivery { get; set; } = canDelivery;

        public Task<SellerCustomerOrderingCapability> ResolveAsync(
            Guid sellerOrganizationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new SellerCustomerOrderingCapability(sellerOrganizationId, CanOrder, CanDelivery));
    }

    private sealed class SellerScopedCapability(Guid allowedSellerId, bool canOrder, bool canDelivery)
        : ISellerCustomerOrderingCapability
    {
        public Task<SellerCustomerOrderingCapability> ResolveAsync(
            Guid sellerOrganizationId,
            CancellationToken cancellationToken = default)
        {
            var allowed = sellerOrganizationId == allowedSellerId;
            return Task.FromResult(new SellerCustomerOrderingCapability(
                sellerOrganizationId,
                allowed && canOrder,
                allowed && canDelivery));
        }
    }

    private sealed class FixedClock(DateTimeOffset utc) : IClock
    {
        public DateTimeOffset UtcNow => utc;
    }

    private sealed class CountingBranches : ICustomerOrderBranchDirectory
    {
        public int GetBranchCallCount { get; private set; }

        public Task<CustomerOrderBranchSnapshot?> GetBranchAsync(
            Guid sellerOrganizationId,
            Guid branchId,
            CancellationToken cancellationToken = default)
        {
            GetBranchCallCount++;
            return Task.FromResult<CustomerOrderBranchSnapshot?>(OperationalBranch());
        }

        public Task<IReadOnlyList<CustomerOrderBranchSnapshot>> ListBranchesAsync(
            Guid sellerOrganizationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CustomerOrderBranchSnapshot>>([OperationalBranch()]);

        private CustomerOrderBranchSnapshot OperationalBranch() =>
            new(
                Branch,
                "Main",
                CustomerOrderingEnabled: true,
                PickupEnabled: true,
                DeliveryEnabled: true,
                CustomerOrderingOperational: true,
                PickupOperational: true,
                DeliveryOperational: true,
                OnlineOrdersPaused: false,
                StoreStatusMessage: "Open",
                14.5995m,
                120.9842m,
                new CustomerOrderBranchDeliveryPolicySnapshot(0m, 49m, 2m, 10m, 15m, 500m));
    }

    private sealed class CountingStock : ICustomerOrderStockService
    {
        public int EnsureCallCount { get; private set; }

        public Task<ApplicationResult> EnsureAvailableAsync(
            PosOrganizationId organizationId,
            IReadOnlyList<CustomerOrderLineDraft> lines,
            CancellationToken cancellationToken = default)
        {
            EnsureCallCount++;
            return Task.FromResult(ApplicationResult.Success());
        }

        public Task ReserveForAcceptAsync(
            CustomerOrder order,
            Guid actorId,
            DateTimeOffset utcNow,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task ReleaseIfReservedAsync(
            CustomerOrder order,
            DateTimeOffset utcNow,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task ConsumeOnCompleteAsync(
            CustomerOrder order,
            IReadOnlyDictionary<Guid, CatalogProduct> productsById,
            Guid actorId,
            DateTimeOffset utcNow,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class EmptyImages : ICatalogProductImageRepository
    {
        public Task<CatalogProductImage?> GetByProductIdAsync(
            PosOrganizationId organizationId,
            CatalogProductId productId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<CatalogProductImage?>(null);

        public Task<IReadOnlyList<CatalogProductImage>> ListByProductIdsAsync(
            PosOrganizationId organizationId,
            IReadOnlyList<CatalogProductId> productIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CatalogProductImage>>([]);

        public Task AddAsync(CatalogProductImage image, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task UpdateAsync(CatalogProductImage image, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task DeleteAsync(CatalogProductImage image, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class FakeCategories : IProductCategoryRepository
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

    private sealed class FakeInventory : IInventoryRepository
    {
        public Task<IReadOnlyList<InventoryAccount>> ListByProductIdsAsync(
            PosOrganizationId organizationId,
            IReadOnlyCollection<CatalogProductId> productIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<InventoryAccount>>([]);

        public Task<InventoryAccount?> GetByProductIdAsync(
            PosOrganizationId organizationId,
            CatalogProductId productId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<(IReadOnlyList<InventoryAccount> Items, int TotalCount)> ListAsync(
            PosOrganizationId organizationId,
            InventoryAccountFilter filter,
            int skip,
            int take,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<(IReadOnlyList<InventoryAccount> Items, int TotalCount)> ListLowStockAsync(
            PosOrganizationId organizationId,
            string? search,
            int skip,
            int take,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<InventoryAccount>> ListAllAccountsAsync(
            PosOrganizationId organizationId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

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

        public Task<StockMovement?> GetMovementByIdAsync(
            PosOrganizationId organizationId,
            StockMovementId movementId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> HasAnyMovementAsync(
            PosOrganizationId organizationId,
            CatalogProductId productId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> HasOpeningStockAsync(
            PosOrganizationId organizationId,
            CatalogProductId productId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<(IReadOnlyList<StockMovement> Items, int TotalCount)> ListMovementsAsync(
            PosOrganizationId organizationId,
            CatalogProductId productId,
            StockMovementFilter filter,
            int skip,
            int take,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<decimal> SumMovementEffectsAsync(
            PosOrganizationId organizationId,
            CatalogProductId productId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<(IReadOnlyList<InventoryAccount> Items, int TotalCount)> ListReorderSuggestionsAsync(
            PosOrganizationId organizationId,
            string? search,
            int skip,
            int take,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> HasStockCountVarianceAsync(
            PosOrganizationId organizationId,
            StockCountId stockCountId,
            CatalogProductId productId,
            StockMovementType movementType,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<StockMovement>> ListMovementsForReportAsync(
            PosOrganizationId organizationId,
            DateOnly fromDateUtc,
            DateOnly toDateUtc,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<StockMovement>> ListSaleDeductionsAsync(
            PosOrganizationId organizationId,
            SaleId saleId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> HasSaleDeductionAsync(
            PosOrganizationId organizationId,
            SaleId saleId,
            CatalogProductId productId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> HasCustomerOrderDeductionAsync(
            PosOrganizationId organizationId,
            CustomerOrderId orderId,
            CatalogProductId productId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> HasSaleVoidRestorationAsync(
            PosOrganizationId organizationId,
            SaleId saleId,
            CatalogProductId productId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> HasPurchaseReceiptAsync(
            PosOrganizationId organizationId,
            GoodsReceiptId goodsReceiptId,
            CatalogProductId productId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> HasDirectPurchaseReceiptAsync(
            PosOrganizationId organizationId,
            DirectPurchaseReceiptId receiptId,
            CatalogProductId productId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> HasStockUseAsync(PosOrganizationId organizationId, StockUseId stockUseId, CatalogProductId productId, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);
        public Task<bool> HasStockUseVoidRestorationAsync(PosOrganizationId organizationId, StockUseId stockUseId, CatalogProductId productId, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<bool> HasProductionMaterialConsumptionAsync(PosOrganizationId organizationId, ProductionRunId productionRunId, CatalogProductId productId, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);
        public Task<bool> HasProductionMaterialRestorationAsync(PosOrganizationId organizationId, ProductionRunId productionRunId, CatalogProductId productId, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);
        public Task<bool> HasProductionOutputAsync(PosOrganizationId organizationId, ProductionRunId productionRunId, CatalogProductId productId, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);
        public Task<bool> HasProductionOutputReversalAsync(PosOrganizationId organizationId, ProductionRunId productionRunId, CatalogProductId productId, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);
        public Task<bool> HasWasteLossAsync(PosOrganizationId organizationId, WasteLossId wasteLossId, CatalogProductId productId, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);
        public Task<bool> HasWasteLossVoidRestorationAsync(PosOrganizationId organizationId, WasteLossId wasteLossId, CatalogProductId productId, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<decimal?> GetLatestAcquisitionUnitCostAsync(PosOrganizationId organizationId, CatalogProductId productId, CancellationToken cancellationToken = default) =>
            Task.FromResult<decimal?>(null);
        public Task<bool> HasSaleReturnRestockAsync(
            PosOrganizationId organizationId,
            SaleReturnId saleReturnId,
            CatalogProductId productId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> HasInventoryTransferMovementAsync(
            PosOrganizationId organizationId,
            InventoryTransferId transferId,
            CatalogProductId productId,
            StockMovementType movementType,
            InventoryLotId? lotId = null,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<(DateTimeOffset? LatestAt, int Count)> GetMovementSummaryAsync(
            PosOrganizationId organizationId,
            CatalogProductId productId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyDictionary<Guid, (DateTimeOffset? LatestAt, int Count)>> GetMovementSummariesAsync(
            PosOrganizationId organizationId,
            IReadOnlyCollection<CatalogProductId> productIds,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public async Task<IReadOnlyDictionary<Guid, decimal?>> GetLatestAcquisitionUnitCostsAsync(
            PosOrganizationId organizationId,
            IReadOnlyCollection<CatalogProductId> productIds,
            CancellationToken cancellationToken = default)
        {
            var result = new Dictionary<Guid, decimal?>();
            foreach (var productId in productIds)
            {
                var cost = await GetLatestAcquisitionUnitCostAsync(organizationId, productId, cancellationToken)
                    .ConfigureAwait(false);
                if (cost is not null)
                {
                    result[productId.Value] = cost;
                }
            }

            return result;
        }
    }

    private sealed class FakeProducts(Action? onList = null) : ICatalogProductRepository
    {
        private readonly CatalogProduct _product = CatalogProduct.Create(
            PosOrganizationId.From(Seller),
            "Rice",
            UnitOfMeasure.Piece,
            25m,
            Utc,
            sku: "RICE",
            id: CatalogProductId.From(ProductGuid));

        public Task<CatalogProduct?> GetByIdAsync(
            PosOrganizationId organizationId,
            CatalogProductId productId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<CatalogProduct?>(_product.Id == productId ? _product : null);

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
            Task.FromResult<IReadOnlyList<CatalogProduct>>(
                productIds.Any(id => id == CatalogProductId.From(ProductGuid)) ? [_product] : []);

        public Task<(IReadOnlyList<CatalogProduct> Items, int TotalCount)> ListAsync(
            PosOrganizationId organizationId,
            CatalogProductFilter filter,
            int skip,
            int take,
            CancellationToken cancellationToken = default)
        {
            onList?.Invoke();
            return Task.FromResult<(IReadOnlyList<CatalogProduct>, int)>(([_product], 1));
        }

        public Task<IReadOnlyList<Guid>> ListIdsAsync(
            PosOrganizationId organizationId,
            CatalogProductFilter filter,
            int skip,
            int take,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Guid>>([_product.Id.Value]);

        public Task<(int TotalCount, int AvailableCount, int NotAvailableCount)> CountConnectedBuyerAvailabilityAsync(
            PosOrganizationId organizationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult((1, 1, 0));

        public Task<IReadOnlyList<(Guid? CategoryId, int Count)>> ListConnectedBuyerAvailabilityCategoryFacetsAsync(
            PosOrganizationId organizationId,
            CatalogProductFilter filter,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<(Guid?, int)>>([]);

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

        public Task AddAsync(CatalogProduct product, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task UpdateAsync(CatalogProduct product, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class FakeOrders : ICustomerOrderRepository
    {
        public int PlaceCount { get; private set; }

        public Task<CustomerOrder?> GetByIdAsync(
            PosOrganizationId sellerOrganizationId,
            CustomerOrderId orderId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<CustomerOrder?>(null);

        public Task<CustomerOrder?> FindByIdempotencyKeyAsync(
            PosOrganizationId sellerOrganizationId,
            string idempotencyKey,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<CustomerOrder?>(null);

        public Task<(IReadOnlyList<CustomerOrder> Items, int TotalCount)> ListAsync(
            PosOrganizationId sellerOrganizationId,
            CustomerOrderFilter filter,
            int skip,
            int take,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<(IReadOnlyList<CustomerOrder>, int)>(([], 0));

        public Task<(IReadOnlyList<CustomerOrder> Items, int TotalCount)> ListForCustomerPartyAsync(
            CustomerPartyType partyType,
            Guid? platformUserId,
            Guid? buyerOrganizationId,
            int skip,
            int take,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<(IReadOnlyList<CustomerOrder>, int)>(([], 0));

        public Task<CustomerOrder?> GetForCustomerPartyAsync(
            CustomerOrderId orderId,
            CustomerPartyType partyType,
            Guid? platformUserId,
            Guid? buyerOrganizationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<CustomerOrder?>(null);

        public Task<CustomerOrder> PlaceAsync(
            PosOrganizationId sellerOrganizationId,
            Func<string, CustomerOrder> createOrder,
            Func<CustomerOrder, CancellationToken, Task>? afterCreated = null,
            CancellationToken cancellationToken = default)
        {
            PlaceCount++;
            return Task.FromResult(createOrder("SO-000001"));
        }

        public Task UpdateAsync(CustomerOrder order, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
