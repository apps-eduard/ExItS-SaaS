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

    [Theory]
    [InlineData(null, "Cash")]
    [InlineData("Cash", "Cash")]
    [InlineData("GCash", "ManualGCash")]
    [InlineData("ManualGCash", "ManualGCash")]
    [InlineData("Utang", "Utang")]
    public async Task Place_persists_manual_payment_method_and_stays_unpaid(string? requested, string expected)
    {
        var useCase = CreateUseCase(canOrder: true, canDelivery: true);
        var result = await useCase.ExecuteAsync(Seller, PickupRequest(paymentMethod: requested), Actor);
        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.Equal(expected, result.Value!.PaymentMethod);
        Assert.Equal(nameof(CustomerOrderPaymentStatus.Unpaid), result.Value.PaymentStatus);
    }

    [Fact]
    public async Task Place_rejects_invalid_payment_method()
    {
        var useCase = CreateUseCase(canOrder: true, canDelivery: true);
        var result = await useCase.ExecuteAsync(Seller, PickupRequest(paymentMethod: "Card"), Actor);
        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrorCodes.InvalidCustomerOrderPaymentMethod, result.ErrorCode);
    }

    [Fact]
    public void Dto_and_query_map_preserve_selected_payment_method()
    {
        var order = CustomerOrder.CreateSubmitted(
            PosOrganizationId.From(Seller),
            "SO-000001",
            CustomerOrderParty.Personal(Actor, "Ana"),
            CustomerOrderFulfillmentType.Pickup,
            Branch,
            "Main",
            [new CustomerOrderLineDraft(
                CatalogProductId.From(ProductGuid),
                "Rice",
                "RICE",
                UnitOfMeasure.Piece,
                2m,
                25m)],
            Actor,
            Utc,
            paymentMethod: CustomerOrderPaymentMethod.Utang);

        var dto = CustomerOrderMaps.Map(order);
        Assert.Equal(nameof(CustomerOrderPaymentMethod.Utang), dto.PaymentMethod);
        Assert.Equal(nameof(CustomerOrderPaymentStatus.Unpaid), dto.PaymentStatus);
    }

    [Fact]
    public void Place_use_case_does_not_create_payment_attempts_or_utang_ledger()
    {
        var source = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "src",
            "Products",
            "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Application",
            "CustomerOrdering",
            "CustomerOrderUseCases.cs"));
        Assert.DoesNotContain("IPaymentAttempt", source, StringComparison.Ordinal);
        Assert.DoesNotContain("PaymentAttempt", source, StringComparison.Ordinal);
        Assert.DoesNotContain("ICreditEntryRepository", source, StringComparison.Ordinal);
        Assert.DoesNotContain("ProductBasedUtang", source, StringComparison.Ordinal);
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

    private static readonly Guid BusinessCustomer = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private static PlaceCustomerOrderRequest PickupRequest(
        string? idempotencyKey = null,
        string? paymentMethod = null) =>
        new(
            "Pickup",
            Branch,
            "Personal",
            "Ana",
            Actor,
            BusinessCustomer,
            null,
            null,
            [new PlaceCustomerOrderLineRequest(ProductGuid, 2m)],
            null,
            null,
            idempotencyKey,
            paymentMethod);

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
            Task.FromResult<CustomerOrderBranchSnapshot?>(OperationalBranch(Branch, "Main"));

        public Task<IReadOnlyList<CustomerOrderBranchSnapshot>> ListBranchesAsync(
            Guid sellerOrganizationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CustomerOrderBranchSnapshot>>([
                OperationalBranch(Branch, "Main")
            ]);

        private static CustomerOrderBranchSnapshot OperationalBranch(Guid branchId, string name) =>
            new(
                branchId,
                name,
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

    private sealed class FakeStock : ICustomerOrderStockService
    {
        public Task<ApplicationResult> EnsureAvailableAsync(
            PosOrganizationId organizationId,
            IReadOnlyList<CustomerOrderLineDraft> lines,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(ApplicationResult.Success());

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
}
