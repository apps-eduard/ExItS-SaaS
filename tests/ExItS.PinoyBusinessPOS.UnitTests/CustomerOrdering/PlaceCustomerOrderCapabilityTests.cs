using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.CustomerOrdering;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.CustomerOrdering;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.UnitTests.CustomerOrdering;

public sealed class PlaceCustomerOrderCapabilityTests
{
    private static readonly Guid Seller = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid Branch = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly Guid Actor = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
    private static readonly Guid ProductGuid = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly DateTimeOffset Utc = new(2026, 8, 17, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Place_fails_when_seller_cannot_accept_customer_orders()
    {
        var useCase = CreateUseCase(canOrder: false, canDelivery: false);
        var result = await useCase.ExecuteAsync(Seller, PickupRequest(), Actor);
        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.CustomerOrderOrderingUnavailable, result.ErrorCode);
    }

    [Fact]
    public async Task Delivery_fails_when_seller_lacks_delivery_feature()
    {
        var useCase = CreateUseCase(canOrder: true, canDelivery: false);
        var result = await useCase.ExecuteAsync(Seller, DeliveryRequest(), Actor);
        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.CustomerOrderOrderingUnavailable, result.ErrorCode);
    }

    [Fact]
    public async Task Pickup_succeeds_when_seller_can_accept_orders()
    {
        var useCase = CreateUseCase(canOrder: true, canDelivery: true);
        var result = await useCase.ExecuteAsync(Seller, PickupRequest(), Actor);
        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.Equal("Pickup", result.Value!.FulfillmentType);
    }

    [Fact]
    public async Task Idempotency_key_returns_existing_order()
    {
        var orders = new FakeOrders();
        var useCase = CreateUseCase(canOrder: true, canDelivery: true, orders);
        var first = await useCase.ExecuteAsync(Seller, PickupRequest("idem-1"), Actor);
        Assert.True(first.IsSuccess, first.ErrorMessage);
        var second = await useCase.ExecuteAsync(Seller, PickupRequest("idem-1"), Actor);
        Assert.True(second.IsSuccess, second.ErrorMessage);
        Assert.Equal(first.Value!.OrderId, second.Value!.OrderId);
        Assert.Equal(1, orders.PlaceCount);
    }

    private static PlaceCustomerOrder CreateUseCase(
        bool canOrder,
        bool canDelivery,
        FakeOrders? orders = null)
    {
        orders ??= new FakeOrders();
        return new PlaceCustomerOrder(
            orders,
            new FakeProducts(),
            new FakeBranches(),
            new FakeStock(),
            new FixedClock(Utc),
            new FixedCapability(Seller, canOrder, canDelivery));
    }

    private static PlaceCustomerOrderRequest PickupRequest(string? idempotencyKey = null) =>
        new(
            "Pickup",
            Branch,
            "Personal",
            "Ana",
            Actor,
            null,
            null,
            [new PlaceCustomerOrderLineRequest(ProductGuid, 2m)],
            null,
            null,
            idempotencyKey);

    private static PlaceCustomerOrderRequest DeliveryRequest() =>
        new(
            "Delivery",
            Branch,
            "Personal",
            "Ana",
            Actor,
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

    private sealed class FixedCapability(Guid orgId, bool canOrder, bool canDelivery)
        : ISellerCustomerOrderingCapability
    {
        public Task<SellerCustomerOrderingCapability> ResolveAsync(
            Guid sellerOrganizationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new SellerCustomerOrderingCapability(orgId, canOrder, canDelivery));
    }

    private sealed class FixedClock(DateTimeOffset utc) : IClock
    {
        public DateTimeOffset UtcNow => utc;
    }

    private sealed class FakeBranches : ICustomerOrderBranchDirectory
    {
        public Task<CustomerOrderBranchSnapshot?> GetBranchAsync(
            Guid sellerOrganizationId,
            Guid branchId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<CustomerOrderBranchSnapshot?>(new CustomerOrderBranchSnapshot(
                Branch,
                "Main",
                PickupEnabled: true,
                DeliveryEnabled: true,
                14.5995m,
                120.9842m,
                new CustomerOrderBranchDeliveryPolicySnapshot(0m, 49m, 2m, 10m, 15m, 500m)));

        public Task<IReadOnlyList<CustomerOrderBranchSnapshot>> ListBranchesAsync(
            Guid sellerOrganizationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CustomerOrderBranchSnapshot>>([
                new CustomerOrderBranchSnapshot(
                    Branch, "Main", true, true, 14.5995m, 120.9842m,
                    new CustomerOrderBranchDeliveryPolicySnapshot(0m, 49m, 2m, 10m, 15m, 500m))
            ]);
    }

    private sealed class FakeStock : ICustomerOrderStockService
    {
        public Task EnsureAvailableAsync(
            PosOrganizationId organizationId,
            IReadOnlyList<CustomerOrderLineDraft> lines,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

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

    private sealed class FakeProducts : ICatalogProductRepository
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
            CancellationToken cancellationToken = default) =>
            Task.FromResult<(IReadOnlyList<CatalogProduct>, int)>(([_product], 1));

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
        private readonly Dictionary<string, CustomerOrder> _byIdempotency = new(StringComparer.Ordinal);
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
            Task.FromResult(_byIdempotency.TryGetValue(idempotencyKey, out var order) ? order : null);

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
            var order = createOrder("SO-000001");
            if (!string.IsNullOrWhiteSpace(order.IdempotencyKey))
            {
                _byIdempotency[order.IdempotencyKey] = order;
            }

            return Task.FromResult(order);
        }

        public Task UpdateAsync(CustomerOrder order, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
