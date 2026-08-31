using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.CustomerOrdering;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.CustomerOrdering;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.UnitTests.CustomerOrdering;

/// <summary>
/// Distance-exception matrix: bypasses only MaximumDeliveryDistanceKm via linked-customer proof.
/// </summary>
public sealed class DeliveryDistanceExceptionQuotePlaceTests
{
    private static readonly Guid Seller = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid OtherSeller = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid Branch = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly Guid Actor = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
    private static readonly Guid OtherActor = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
    private static readonly Guid ProductGuid = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff");
    private static readonly Guid AreaA = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid TamperedArea = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid BusinessCustomer = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid LinkedAppUser = Guid.Parse("55555555-5555-5555-5555-555555555555");
    private static readonly DateTimeOffset Utc = new(2026, 8, 17, 12, 0, 0, TimeSpan.Zero);

    // ~111km north of branch — beyond 15km max.
    private const decimal FarLat = 15.6m;
    private const decimal FarLng = 120.98m;
    private const decimal NearLat = 14.6m;
    private const decimal NearLng = 120.98m;

    [Fact]
    public async Task A_Off_outside_max_rejects()
    {
        var quote = await CreateQuote(allowBeyond: false)
            .ExecuteAsync(Seller, Quote(FarLat, FarLng, BusinessCustomer));
        Assert.True(quote.IsSuccess);
        Assert.False(quote.Value!.Available);
        Assert.False(quote.Value.DistanceExceptionApplied);

        var place = await CreatePlace(allowBeyond: false)
            .ExecuteAsync(Seller, Delivery(FarLat, FarLng), Actor);
        Assert.False(place.IsSuccess);
        Assert.Equal(DomainErrorCodes.InvalidCustomerOrderDelivery, place.ErrorCode);
    }

    [Fact]
    public async Task B_On_outside_max_with_area_allows_fee_from_actual_distance()
    {
        var quote = await CreateQuote(allowBeyond: true)
            .ExecuteAsync(Seller, Quote(FarLat, FarLng, BusinessCustomer));
        Assert.True(quote.IsSuccess, quote.ErrorMessage);
        Assert.True(quote.Value!.Available);
        Assert.True(quote.Value.DistanceExceptionApplied);
        Assert.True(quote.Value.DistanceKm > 15m);
        // Fee uses actual distance (not clamped to max): base 49 + (distance - 2) * 10
        var expectedExtra = quote.Value.DistanceKm - 2m;
        Assert.Equal(expectedExtra, quote.Value.ExtraDistanceKm);
        Assert.Equal(49m + expectedExtra * 10m, quote.Value.DeliveryFee);

        var place = await CreatePlace(allowBeyond: true)
            .ExecuteAsync(Seller, Delivery(FarLat, FarLng), Actor);
        Assert.True(place.IsSuccess, place.ErrorMessage);
        Assert.True(place.Value!.Delivery!.DistanceExceptionApplied);
        Assert.True(place.Value.Delivery.DistanceKm > 15m);
        Assert.Equal(place.Value.Delivery.DistanceKm, quote.Value.DistanceKm);
    }

    [Fact]
    public async Task C_On_inside_max_allows()
    {
        var quote = await CreateQuote(allowBeyond: true)
            .ExecuteAsync(Seller, Quote(NearLat, NearLng, BusinessCustomer));
        Assert.True(quote.IsSuccess);
        Assert.True(quote.Value!.Available);
        Assert.False(quote.Value.DistanceExceptionApplied);
        Assert.True(quote.Value.DistanceKm <= 15m);
    }

    [Fact]
    public async Task D_On_unconfigured_area_rejects()
    {
        var quote = await CreateQuote(allowBeyond: true)
            .ExecuteAsync(Seller, Quote(FarLat, FarLng, BusinessCustomer, areaId: Guid.Empty));
        Assert.True(quote.IsSuccess);
        Assert.False(quote.Value!.Available);
        Assert.Contains("service area", quote.Value.UnavailableReason!, StringComparison.OrdinalIgnoreCase);

        var place = await CreatePlace(allowBeyond: true)
            .ExecuteAsync(Seller, Delivery(FarLat, FarLng, areaId: Guid.Empty), Actor);
        Assert.False(place.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.CustomerOrderDeliveryServiceAreaInvalid, place.ErrorCode);
    }

    [Fact]
    public async Task E_On_DeliveryEnabled_false_rejects()
    {
        var branches = new ConfigurableBranches(deliveryEnabled: false, deliveryOperational: false);
        var quote = await CreateQuote(allowBeyond: true, branches)
            .ExecuteAsync(Seller, Quote(FarLat, FarLng, BusinessCustomer));
        Assert.True(quote.IsSuccess);
        Assert.False(quote.Value!.Available);
        Assert.Contains("not enabled", quote.Value.UnavailableReason!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task F_On_DeliveryReady_false_rejects()
    {
        // DeliveryEnabled true but DeliveryOperational false ⇒ readiness / ops gate.
        var branches = new ConfigurableBranches(deliveryEnabled: true, deliveryOperational: false);
        var quote = await CreateQuote(allowBeyond: true, branches)
            .ExecuteAsync(Seller, Quote(FarLat, FarLng, BusinessCustomer));
        Assert.True(quote.IsSuccess);
        Assert.False(quote.Value!.Available);
        Assert.Contains("not available", quote.Value.UnavailableReason!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task G_On_paused_rejects()
    {
        var branches = new ConfigurableBranches(
            deliveryEnabled: true,
            deliveryOperational: false,
            onlineOrdersPaused: true,
            customerOrderingOperational: false);
        var quote = await CreateQuote(allowBeyond: true, branches)
            .ExecuteAsync(Seller, Quote(FarLat, FarLng, BusinessCustomer));
        Assert.True(quote.IsSuccess);
        Assert.False(quote.Value!.Available);
        Assert.Contains("temporarily", quote.Value.UnavailableReason!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task H_On_min_order_fail_rejects()
    {
        var quote = await CreateQuote(allowBeyond: true)
            .ExecuteAsync(Seller, Quote(FarLat, FarLng, BusinessCustomer, merchandise: 50m));
        Assert.True(quote.IsSuccess);
        Assert.False(quote.Value!.Available);
        Assert.Contains("at least", quote.Value.UnavailableReason!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task I_client_fake_override_ignored_without_server_proof()
    {
        // No PlatformBusinessCustomerId / no auth proof → exception never applies even far away.
        var quote = await CreateQuote(allowBeyond: true)
            .ExecuteAsync(Seller, Quote(FarLat, FarLng, platformBusinessCustomerId: null));
        Assert.True(quote.IsSuccess);
        Assert.False(quote.Value!.Available);
        Assert.False(quote.Value.DistanceExceptionApplied);
    }

    [Fact]
    public async Task J_other_Personal_user_cannot_use_approved_customer_exception()
    {
        var auth = new FakeLinkedAuth(
            Seller,
            BusinessCustomer,
            personalUserId: Actor,
            allowBeyond: true);
        var place = new PlaceCustomerOrder(
            new FakeOrders(),
            new FakeProducts(),
            new ConfigurableBranches(),
            new FakeStock(),
            new FixedClock(Utc),
            new FixedCapability(Seller, true, true),
            linkedCustomerAuth: auth);

        var result = await place.ExecuteAsync(
            Seller,
            Delivery(FarLat, FarLng) with { CustomerPlatformUserId = OtherActor },
            OtherActor);
        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.LinkedCustomerNotFound, result.ErrorCode);
    }

    [Fact]
    public async Task K_OrgA_exception_does_not_apply_to_OrgB()
    {
        var auth = new FakeLinkedAuth(
            Seller,
            BusinessCustomer,
            personalUserId: Actor,
            allowBeyond: true);
        var quote = new QuoteCustomerOrderDelivery(
            new ConfigurableBranches(),
            new FixedCapability(OtherSeller, true, true),
            auth);

        var result = await quote.ExecuteAsync(
            OtherSeller,
            Quote(FarLat, FarLng, BusinessCustomer));
        Assert.True(result.IsSuccess);
        Assert.False(result.Value!.Available);
        Assert.False(result.Value.DistanceExceptionApplied);
    }

    [Fact]
    public async Task L_cross_branch_service_area_tampering_rejected()
    {
        var quote = await CreateQuote(allowBeyond: true)
            .ExecuteAsync(Seller, Quote(FarLat, FarLng, BusinessCustomer, areaId: TamperedArea));
        Assert.True(quote.IsSuccess);
        Assert.False(quote.Value!.Available);

        var place = await CreatePlace(allowBeyond: true)
            .ExecuteAsync(Seller, Delivery(FarLat, FarLng, areaId: TamperedArea), Actor);
        Assert.False(place.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.CustomerOrderDeliveryServiceAreaInvalid, place.ErrorCode);
    }

    [Fact]
    public void Fee_calculator_allowBeyond_skips_max_only()
    {
        var policy = new CustomerOrderBranchDeliveryPolicySnapshot(100m, 49m, 2m, 10m, 15m, null);
        Assert.Throws<DomainException>(() =>
            CustomerOrderDeliveryFeeCalculator.Calculate(policy, 200m, 20m));

        var allowed = CustomerOrderDeliveryFeeCalculator.Calculate(policy, 200m, 20m, allowBeyondMaximumDistance: true);
        Assert.Equal(20m, allowed.DistanceKm);
        Assert.Equal(18m, allowed.ExtraDistanceKm);
        Assert.Equal(49m + 180m, allowed.DeliveryFee);

        Assert.Throws<DomainException>(() =>
            CustomerOrderDeliveryFeeCalculator.Calculate(policy, 50m, 20m, allowBeyondMaximumDistance: true));
    }

    private static QuoteCustomerOrderDelivery CreateQuote(
        bool allowBeyond,
        ICustomerOrderBranchDirectory? branches = null) =>
        new(
            branches ?? new ConfigurableBranches(),
            new FixedCapability(Seller, true, true),
            new FakeLinkedAuth(Seller, BusinessCustomer, Actor, allowBeyond));

    private static PlaceCustomerOrder CreatePlace(bool allowBeyond) =>
        new(
            new FakeOrders(),
            new FakeProducts(),
            new ConfigurableBranches(),
            new FakeStock(),
            new FixedClock(Utc),
            new FixedCapability(Seller, true, true),
            linkedCustomerAuth: new FakeLinkedAuth(Seller, BusinessCustomer, Actor, allowBeyond));

    private static QuoteCustomerOrderDeliveryRequest Quote(
        decimal lat,
        decimal lng,
        Guid? platformBusinessCustomerId,
        Guid? areaId = null,
        decimal merchandise = 200m) =>
        new(
            Branch,
            merchandise,
            lat,
            lng,
            areaId == Guid.Empty ? null : areaId ?? AreaA,
            platformBusinessCustomerId);

    private static PlaceCustomerOrderRequest Delivery(
        decimal lat,
        decimal lng,
        Guid? areaId = null) =>
        new(
            "Delivery",
            Branch,
            "Personal",
            "Ana",
            Actor,
            BusinessCustomer,
            null,
            null,
            [new PlaceCustomerOrderLineRequest(ProductGuid, 8m)],
            new PlaceCustomerOrderDeliveryRequest(
                "Ana",
                "09171234567",
                "123 Main",
                null,
                "Ignored",
                null,
                lat,
                lng,
                areaId == Guid.Empty ? null : areaId ?? AreaA));

    private sealed class FakeLinkedAuth(
        Guid organizationId,
        Guid businessCustomerId,
        Guid personalUserId,
        bool allowBeyond) : ILinkedCustomerPlatformAuthorization
    {
        public Task<LinkedCustomerPlatformAuthorizationResult> VerifyAsync(
            Guid orgId,
            Guid platformBusinessCustomerId,
            CancellationToken cancellationToken = default)
        {
            if (orgId != organizationId || platformBusinessCustomerId != businessCustomerId)
            {
                return Task.FromResult(new LinkedCustomerPlatformAuthorizationResult(
                    LinkedCustomerPlatformAuthorizationOutcome.NotFound, null));
            }

            return Task.FromResult(new LinkedCustomerPlatformAuthorizationResult(
                LinkedCustomerPlatformAuthorizationOutcome.Authorized,
                new LinkedCustomerPlatformAuthorizationProof(
                    personalUserId,
                    organizationId,
                    businessCustomerId,
                    LinkedAppUser,
                    allowBeyond)));
        }
    }

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

    private sealed class ConfigurableBranches(
        bool deliveryEnabled = true,
        bool deliveryOperational = true,
        bool onlineOrdersPaused = false,
        bool customerOrderingOperational = true,
        decimal minimumOrderAmount = 100m) : ICustomerOrderBranchDirectory
    {
        public Task<CustomerOrderBranchSnapshot?> GetBranchAsync(
            Guid sellerOrganizationId,
            Guid branchId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<CustomerOrderBranchSnapshot?>(
                branchId == Branch ? Snapshot() : null);

        public Task<IReadOnlyList<CustomerOrderBranchSnapshot>> ListBranchesAsync(
            Guid sellerOrganizationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CustomerOrderBranchSnapshot>>([Snapshot()]);

        private CustomerOrderBranchSnapshot Snapshot() =>
            new(
                Branch,
                "Main",
                CustomerOrderingEnabled: true,
                PickupEnabled: true,
                DeliveryEnabled: deliveryEnabled,
                CustomerOrderingOperational: customerOrderingOperational,
                PickupOperational: true,
                DeliveryOperational: deliveryOperational,
                OnlineOrdersPaused: onlineOrdersPaused,
                StoreStatusMessage: onlineOrdersPaused ? "Paused" : "Open",
                14.5995m,
                120.9842m,
                new CustomerOrderBranchDeliveryPolicySnapshot(
                    minimumOrderAmount, 49m, 2m, 10m, 15m, 500m),
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
