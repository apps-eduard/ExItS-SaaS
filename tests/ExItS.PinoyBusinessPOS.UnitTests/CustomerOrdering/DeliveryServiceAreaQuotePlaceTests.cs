using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.CustomerOrdering;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.CustomerOrdering;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.UnitTests.CustomerOrdering;

/// <summary>
/// Service-area + distance defense-in-depth for quote and place delivery.
/// </summary>
public sealed class DeliveryServiceAreaQuotePlaceTests
{
    private static readonly Guid Seller = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid Branch = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly Guid Actor = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
    private static readonly Guid ProductGuid = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid AreaA = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid AreaForeign = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid TamperedArea = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid BusinessCustomer = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly DateTimeOffset Utc = new(2026, 8, 17, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Quote_available_for_valid_area_inside_radius()
    {
        var quote = await CreateQuote().ExecuteAsync(Seller, Quote(AreaA, 14.6m, 120.98m));
        Assert.True(quote.IsSuccess);
        Assert.True(quote.Value!.Available);
    }

    [Fact]
    public async Task Quote_unavailable_when_service_area_missing()
    {
        var quote = await CreateQuote().ExecuteAsync(Seller, Quote(null, 14.6m, 120.98m));
        Assert.True(quote.IsSuccess);
        Assert.False(quote.Value!.Available);
        Assert.Contains("service area", quote.Value.UnavailableReason!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Quote_unavailable_for_foreign_area_id()
    {
        var quote = await CreateQuote().ExecuteAsync(Seller, Quote(AreaForeign, 14.6m, 120.98m));
        Assert.True(quote.IsSuccess);
        Assert.False(quote.Value!.Available);
    }

    [Fact]
    public async Task Quote_unavailable_for_tampered_area_id()
    {
        var quote = await CreateQuote().ExecuteAsync(Seller, Quote(TamperedArea, 14.6m, 120.98m));
        Assert.True(quote.IsSuccess);
        Assert.False(quote.Value!.Available);
    }

    [Fact]
    public async Task Quote_unavailable_when_outside_maximum_distance()
    {
        // ~111km north of branch coordinates — beyond 15km max.
        var quote = await CreateQuote().ExecuteAsync(Seller, Quote(AreaA, 15.6m, 120.98m));
        Assert.True(quote.IsSuccess);
        Assert.False(quote.Value!.Available);
    }

    [Fact]
    public async Task Place_succeeds_with_valid_area_and_snapshots_city()
    {
        var result = await CreatePlace().ExecuteAsync(Seller, Delivery(AreaA), Actor);
        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.Equal("Bacolod City", result.Value!.Delivery!.City);
    }

    [Fact]
    public async Task Place_rejects_missing_service_area()
    {
        var result = await CreatePlace().ExecuteAsync(Seller, Delivery(null), Actor);
        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.CustomerOrderDeliveryServiceAreaInvalid, result.ErrorCode);
    }

    [Fact]
    public async Task Place_rejects_tampered_service_area()
    {
        var result = await CreatePlace().ExecuteAsync(Seller, Delivery(TamperedArea), Actor);
        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.CustomerOrderDeliveryServiceAreaInvalid, result.ErrorCode);
    }

    [Fact]
    public async Task Place_rejects_foreign_service_area()
    {
        var result = await CreatePlace().ExecuteAsync(Seller, Delivery(AreaForeign), Actor);
        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.CustomerOrderDeliveryServiceAreaInvalid, result.ErrorCode);
    }

    [Fact]
    public async Task Pickup_does_not_require_delivery_service_area()
    {
        var result = await CreatePlace().ExecuteAsync(Seller, PickupRequest(), Actor);
        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.Equal("Pickup", result.Value!.FulfillmentType);
    }

    private static QuoteCustomerOrderDelivery CreateQuote() =>
        new(new FakeBranches(), new FixedCapability(Seller, true, true));

    private static PlaceCustomerOrder CreatePlace() =>
        new(
            new FakeOrders(),
            new FakeProducts(),
            new FakeBranches(),
            new FakeStock(),
            new FixedClock(Utc),
            new FixedCapability(Seller, true, true));

    private static QuoteCustomerOrderDeliveryRequest Quote(Guid? areaId, decimal lat, decimal lng) =>
        new(Branch, MerchandiseSubtotal: 100m, DestinationLatitude: lat, DestinationLongitude: lng, areaId);

    private static PlaceCustomerOrderRequest Delivery(Guid? areaId) =>
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
                "Ignored Free Text City",
                null,
                14.6m,
                120.98m,
                areaId));

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
            [new PlaceCustomerOrderLineRequest(ProductGuid, 1m)]);

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
            Task.FromResult<CustomerOrderBranchSnapshot?>(
                branchId == Branch ? Operational() : null);

        public Task<IReadOnlyList<CustomerOrderBranchSnapshot>> ListBranchesAsync(
            Guid sellerOrganizationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CustomerOrderBranchSnapshot>>([Operational()]);

        private static CustomerOrderBranchSnapshot Operational() =>
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
                new CustomerOrderBranchDeliveryPolicySnapshot(0m, 49m, 2m, 10m, 15m, 500m),
                IsPrimary: true,
                DeliveryServiceAreas:
                [
                    new CustomerOrderDeliveryServiceAreaSnapshot(AreaA, "Bacolod City", "Negros Occidental")
                ]);
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
            CancellationToken cancellationToken = default) =>
            Task.FromResult(createOrder("SO-000001"));

        public Task UpdateAsync(CustomerOrder order, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
