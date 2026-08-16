using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Application.Purchasing;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Purchasing;
using ExItS.PinoyBusinessPOS.Domain.Suppliers;

namespace ExItS.PinoyBusinessPOS.UnitTests.Purchasing;

public sealed class ConnectedPurchaseOrderLineEligibilityTests
{
    private static readonly PosOrganizationId Buyer =
        PosOrganizationId.From(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
    private static readonly PosOrganizationId SupplierOrg =
        PosOrganizationId.From(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));
    private static readonly PosOrganizationId OtherSupplierOrg =
        PosOrganizationId.From(Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"));
    private static readonly DateTimeOffset Now = new(2026, 8, 16, 18, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task External_supplier_skips_connected_eligibility()
    {
        var supplier = ExternalSupplier("Acme");
        var result = await ConnectedPurchaseOrderLineEligibility.ValidateIfConnectedAsync(
            Buyer,
            supplier,
            [Guid.NewGuid()],
            new InMemoryRelationships(),
            new InMemoryLinks(),
            new InMemoryExposures(),
            new InMemoryShares(),
            CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Connected_valid_line_uses_buyer_specific_then_default_po_never_retail()
    {
        var fixture = CreateConnectedFixture(defaultPo: 200m, buyerSpecific: 180m, retail: 999m);
        var result = await ConnectedPurchaseOrderLineEligibility.ValidateIfConnectedAsync(
            Buyer,
            fixture.Supplier,
            [fixture.BuyerProduct.Id.Value],
            fixture.Relationships,
            fixture.Links,
            fixture.Exposures,
            fixture.Shares,
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.True(result!.IsSuccess, $"{result.ErrorCode}: {result.ErrorMessage}");
        Assert.Equal(180m, result.Value!.EffectivePriceByBuyerProductId[fixture.BuyerProduct.Id.Value]);
        Assert.NotEqual(999m, result.Value.EffectivePriceByBuyerProductId[fixture.BuyerProduct.Id.Value]);
    }

    [Fact]
    public async Task Connected_falls_back_to_default_po_when_no_buyer_override()
    {
        var fixture = CreateConnectedFixture(defaultPo: 200m, buyerSpecific: null, retail: 999m);
        var result = await ConnectedPurchaseOrderLineEligibility.ValidateIfConnectedAsync(
            Buyer,
            fixture.Supplier,
            [fixture.BuyerProduct.Id.Value],
            fixture.Relationships,
            fixture.Links,
            fixture.Exposures,
            fixture.Shares,
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.True(result!.IsSuccess);
        Assert.Equal(200m, result.Value!.EffectivePriceByBuyerProductId[fixture.BuyerProduct.Id.Value]);
    }

    [Fact]
    public async Task Unshared_product_is_rejected()
    {
        var fixture = CreateConnectedFixture(includeShare: false);
        var result = await ConnectedPurchaseOrderLineEligibility.ValidateIfConnectedAsync(
            Buyer,
            fixture.Supplier,
            [fixture.BuyerProduct.Id.Value],
            fixture.Relationships,
            fixture.Links,
            fixture.Exposures,
            fixture.Shares,
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.False(result!.IsSuccess);
        Assert.Equal(ConnectedSupplierErrorCodes.ExposureNotFound, result.ErrorCode);
    }

    [Fact]
    public async Task Not_orderable_product_is_rejected()
    {
        var fixture = CreateConnectedFixture(orderable: false);
        var result = await ConnectedPurchaseOrderLineEligibility.ValidateIfConnectedAsync(
            Buyer,
            fixture.Supplier,
            [fixture.BuyerProduct.Id.Value],
            fixture.Relationships,
            fixture.Links,
            fixture.Exposures,
            fixture.Shares,
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.False(result!.IsSuccess);
        Assert.Equal(ConnectedSupplierErrorCodes.ExposureNotFound, result.ErrorCode);
    }

    [Fact]
    public async Task Inactive_relationship_is_rejected()
    {
        var fixture = CreateConnectedFixture(active: false);
        var result = await ConnectedPurchaseOrderLineEligibility.ValidateIfConnectedAsync(
            Buyer,
            fixture.Supplier,
            [fixture.BuyerProduct.Id.Value],
            fixture.Relationships,
            fixture.Links,
            fixture.Exposures,
            fixture.Shares,
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.False(result!.IsSuccess);
        Assert.Equal(ConnectedSupplierErrorCodes.RelationshipInactive, result.ErrorCode);
    }

    [Fact]
    public async Task Unlinked_buyer_catalog_product_is_rejected()
    {
        var fixture = CreateConnectedFixture(includeLink: false);
        var result = await ConnectedPurchaseOrderLineEligibility.ValidateIfConnectedAsync(
            Buyer,
            fixture.Supplier,
            [fixture.BuyerProduct.Id.Value],
            fixture.Relationships,
            fixture.Links,
            fixture.Exposures,
            fixture.Shares,
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.False(result!.IsSuccess);
        Assert.Equal(ConnectedSupplierErrorCodes.LinkNotFound, result.ErrorCode);
    }

    [Fact]
    public async Task Product_linked_to_other_connected_supplier_is_rejected()
    {
        var fixtureA = CreateConnectedFixture();
        var other = CreateConnectedFixture(
            supplierOrg: OtherSupplierOrg,
            buyerProductId: fixtureA.BuyerProduct.Id);

        var result = await ConnectedPurchaseOrderLineEligibility.ValidateIfConnectedAsync(
            Buyer,
            fixtureA.Supplier,
            [fixtureA.BuyerProduct.Id.Value],
            other.Relationships,
            other.Links,
            other.Exposures,
            other.Shares,
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.False(result!.IsSuccess);
        Assert.Equal(ConnectedSupplierErrorCodes.RelationshipInactive, result.ErrorCode);
    }

    [Fact]
    public async Task Create_overwrites_connected_unit_cost_with_effective_po_price()
    {
        var fixture = CreateConnectedFixture(defaultPo: 200m, buyerSpecific: 180m, retail: 999m);
        var useCase = new CreatePurchaseOrder(
            fixture.Orders,
            fixture.Suppliers,
            fixture.Products,
            new InMemoryUnits(),
            fixture.Relationships,
            fixture.Links,
            new FakeUow(),
            new FakeAccess(),
            new FixedTimeProvider(Now),
            fixture.Exposures,
            fixture.Shares);

        var result = await useCase.ExecuteAsync(
            Buyer.Value,
            new CreatePurchaseOrderRequest(
                fixture.Supplier.Id.Value,
                DateOnly.FromDateTime(Now.UtcDateTime),
                [new CreatePurchaseOrderLineRequest(fixture.BuyerProduct.Id.Value, 2m, 999m)]));

        Assert.True(result.IsSuccess, $"{result.ErrorCode}: {result.ErrorMessage}");
        Assert.Equal(180m, Assert.Single(result.Value!.Lines).UnitPurchaseCost);
        Assert.Single(fixture.Orders.Items);
    }

    [Fact]
    public async Task Create_external_allows_buyer_catalog_and_manual_cost()
    {
        var product = CatalogProduct.Create(Buyer, "Local Candy", UnitOfMeasure.Piece, 25m, Now, sku: "CANDY");
        var supplier = ExternalSupplier("Walk-in");
        var products = new InMemoryProducts();
        products.Seed(product);
        var suppliers = new InMemorySuppliers();
        suppliers.Seed(supplier);
        var orders = new InMemoryOrders();

        var useCase = new CreatePurchaseOrder(
            orders,
            suppliers,
            products,
            new InMemoryUnits(),
            new InMemoryRelationships(),
            new InMemoryLinks(),
            new FakeUow(),
            new FakeAccess(),
            new FixedTimeProvider(Now));

        var result = await useCase.ExecuteAsync(
            Buyer.Value,
            new CreatePurchaseOrderRequest(
                supplier.Id.Value,
                DateOnly.FromDateTime(Now.UtcDateTime),
                [new CreatePurchaseOrderLineRequest(product.Id.Value, 3m, 12.5m)]));

        Assert.True(result.IsSuccess, $"{result.ErrorCode}: {result.ErrorMessage}");
        Assert.Equal(12.5m, Assert.Single(result.Value!.Lines).UnitPurchaseCost);
    }

    [Fact]
    public async Task Create_rejects_missing_supplier()
    {
        var product = CatalogProduct.Create(Buyer, "Local Candy", UnitOfMeasure.Piece, 25m, Now);
        var products = new InMemoryProducts();
        products.Seed(product);

        var useCase = new CreatePurchaseOrder(
            new InMemoryOrders(),
            new InMemorySuppliers(),
            products,
            new InMemoryUnits(),
            new InMemoryRelationships(),
            new InMemoryLinks(),
            new FakeUow(),
            new FakeAccess(),
            new FixedTimeProvider(Now));

        var result = await useCase.ExecuteAsync(
            Buyer.Value,
            new CreatePurchaseOrderRequest(
                Guid.NewGuid(),
                DateOnly.FromDateTime(Now.UtcDateTime),
                [new CreatePurchaseOrderLineRequest(product.Id.Value, 1m, 10m)]));

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.SupplierNotFound, result.ErrorCode);
    }

    [Fact]
    public async Task Create_rejects_crafted_connected_product_from_other_relationship()
    {
        var fixtureA = CreateConnectedFixture();
        var fixtureB = CreateConnectedFixture(supplierOrg: OtherSupplierOrg);

        var useCase = new CreatePurchaseOrder(
            fixtureA.Orders,
            fixtureA.Suppliers,
            fixtureA.Products,
            new InMemoryUnits(),
            fixtureB.Relationships,
            fixtureB.Links,
            new FakeUow(),
            new FakeAccess(),
            new FixedTimeProvider(Now),
            fixtureB.Exposures,
            fixtureB.Shares);

        var result = await useCase.ExecuteAsync(
            Buyer.Value,
            new CreatePurchaseOrderRequest(
                fixtureA.Supplier.Id.Value,
                DateOnly.FromDateTime(Now.UtcDateTime),
                [new CreatePurchaseOrderLineRequest(fixtureA.BuyerProduct.Id.Value, 1m, 10m)]));

        Assert.False(result.IsSuccess);
        Assert.Equal(ConnectedSupplierErrorCodes.RelationshipInactive, result.ErrorCode);
    }

    private static Fixture CreateConnectedFixture(
        decimal defaultPo = 200m,
        decimal? buyerSpecific = 180m,
        decimal retail = 999m,
        bool includeShare = true,
        bool includeLink = true,
        bool orderable = true,
        bool active = true,
        PosOrganizationId? supplierOrg = null,
        CatalogProductId? buyerProductId = null)
    {
        var org = supplierOrg ?? SupplierOrg;
        var relationship = ConnectedSupplierRelationship.Request(Buyer, org, Now);
        if (active)
        {
            relationship.Approve(Now.AddMinutes(1));
        }

        var relationships = new InMemoryRelationships();
        relationships.Seed(relationship);

        var supplier = Supplier.Create(Buyer, "SUP-100001", "Paul Supply", Now);
        supplier.AttachConnectedRelationship(relationship.Id, Now.AddMinutes(2));
        var suppliers = new InMemorySuppliers();
        suppliers.Seed(supplier);

        var exposure = SupplierProductExposure.Expose(
            org,
            CatalogProductId.New(),
            "Apple",
            "Kilogram",
            defaultPo,
            Now.AddMinutes(3),
            sku: "APPLE");
        if (!orderable)
        {
            exposure.MarkNotOrderable(Now.AddMinutes(4));
        }

        var exposures = new InMemoryExposures();
        exposures.Seed(exposure);

        var shares = new InMemoryShares();
        if (includeShare)
        {
            shares.Seed(ConnectedBuyerProductShare.Share(
                relationship.Id,
                Buyer,
                org,
                exposure.ProductId,
                Now.AddMinutes(5),
                buyerSpecific));
        }

        var buyerProduct = CatalogProduct.Create(
            Buyer,
            "Apple",
            UnitOfMeasure.Kilogram,
            retail,
            Now.AddMinutes(6),
            sku: "BUY-APPLE",
            id: buyerProductId);
        var products = new InMemoryProducts();
        products.Seed(buyerProduct);

        var links = new InMemoryLinks();
        if (includeLink && active)
        {
            links.Seed(BuyerSupplierProductLink.Create(
                relationship.Id,
                Buyer,
                org,
                buyerProduct.Id,
                exposure,
                Now.AddMinutes(7),
                effectiveOrderPrice: buyerSpecific ?? defaultPo));
        }

        return new Fixture(
            supplier,
            buyerProduct,
            relationship,
            exposure,
            relationships,
            exposures,
            shares,
            links,
            products,
            suppliers,
            new InMemoryOrders());
    }

    private static Supplier ExternalSupplier(string name) =>
        Supplier.Create(Buyer, "SUP-200001", name, Now);

    private sealed record Fixture(
        Supplier Supplier,
        CatalogProduct BuyerProduct,
        ConnectedSupplierRelationship Relationship,
        SupplierProductExposure Exposure,
        InMemoryRelationships Relationships,
        InMemoryExposures Exposures,
        InMemoryShares Shares,
        InMemoryLinks Links,
        InMemoryProducts Products,
        InMemorySuppliers Suppliers,
        InMemoryOrders Orders);

    private sealed class FakeAccess : IPosCommercialAccessAccessor
    {
        public PosCommercialAccess Current { get; set; } = PosCommercialAccess.DevelopmentDefault;
    }

    private sealed class FakeUow : IPosUnitOfWork
    {
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<T> ExecuteInSerializableTransactionAsync<T>(
            Func<CancellationToken, Task<T>> action,
            CancellationToken cancellationToken = default) =>
            action(cancellationToken);
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    private sealed class InMemoryRelationships : IConnectedSupplierRelationshipRepository
    {
        private readonly List<ConnectedSupplierRelationship> _items = [];
        public void Seed(ConnectedSupplierRelationship relationship) => _items.Add(relationship);
        public Task AddAsync(ConnectedSupplierRelationship relationship, CancellationToken ct = default)
        {
            _items.Add(relationship);
            return Task.CompletedTask;
        }
        public Task<ConnectedSupplierRelationship?> FindOpenAsync(PosOrganizationId buyer, PosOrganizationId supplier, CancellationToken ct = default) =>
            Task.FromResult(_items.FirstOrDefault(x => x.BuyerOrganizationId == buyer && x.SupplierOrganizationId == supplier));
        public Task<ConnectedSupplierRelationship?> GetAsync(ConnectedSupplierRelationshipId id, CancellationToken ct = default) =>
            Task.FromResult(_items.FirstOrDefault(x => x.Id.Value == id.Value));
        public Task<IReadOnlyList<ConnectedSupplierRelationship>> ListAsync(PosOrganizationId organizationId, bool supplierView, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<ConnectedSupplierRelationship>>(_items.ToList());
        public Task UpdateAsync(ConnectedSupplierRelationship relationship, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class InMemoryExposures : ISupplierProductExposureRepository
    {
        private readonly List<SupplierProductExposure> _items = [];
        public void Seed(SupplierProductExposure exposure) => _items.Add(exposure);
        public Task AddAsync(SupplierProductExposure exposure, CancellationToken ct = default)
        {
            _items.Add(exposure);
            return Task.CompletedTask;
        }
        public Task<SupplierProductExposure?> GetAsync(SupplierProductExposureId id, CancellationToken ct = default) =>
            Task.FromResult(_items.FirstOrDefault(x => x.Id.Value == id.Value));
        public Task<SupplierProductExposure?> GetByProductAsync(PosOrganizationId supplier, CatalogProductId productId, CancellationToken ct = default) =>
            Task.FromResult(_items.FirstOrDefault(x => x.SupplierOrganizationId == supplier && x.ProductId == productId));
        public Task<IReadOnlyList<SupplierProductExposure>> ListAsync(PosOrganizationId supplier, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<SupplierProductExposure>>(_items.Where(x => x.SupplierOrganizationId == supplier).ToList());
        public Task<(IReadOnlyList<SupplierProductExposure> Items, int Total)> SearchAsync(PosOrganizationId supplier, string? query, string? category, int skip, int take, CancellationToken ct = default) =>
            Task.FromResult<(IReadOnlyList<SupplierProductExposure>, int)>((_items.ToList(), _items.Count));
        public Task UpdateAsync(SupplierProductExposure exposure, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class InMemoryShares : IConnectedBuyerProductShareRepository
    {
        private readonly List<ConnectedBuyerProductShare> _items = [];
        public void Seed(ConnectedBuyerProductShare share) => _items.Add(share);
        public Task AddAsync(ConnectedBuyerProductShare share, CancellationToken ct = default)
        {
            _items.Add(share);
            return Task.CompletedTask;
        }
        public Task<ConnectedBuyerProductShare?> FindAsync(ConnectedSupplierRelationshipId relationshipId, CatalogProductId supplierProductId, CancellationToken ct = default) =>
            Task.FromResult(_items.FirstOrDefault(x => x.RelationshipId == relationshipId && x.SupplierProductId == supplierProductId));
        public Task<ConnectedBuyerProductShare?> GetAsync(ConnectedBuyerProductShareId id, CancellationToken ct = default) =>
            Task.FromResult(_items.FirstOrDefault(x => x.Id == id));
        public Task<IReadOnlyList<ConnectedBuyerProductShare>> ListAsync(ConnectedSupplierRelationshipId relationshipId, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<ConnectedBuyerProductShare>>(_items.Where(x => x.RelationshipId == relationshipId).ToList());
        public Task<(IReadOnlyList<SupplierProductExposure> Exposures, IReadOnlyList<ConnectedBuyerProductShare> Shares, int Total)> SearchSharedCatalogAsync(
            ConnectedSupplierRelationshipId relationshipId, PosOrganizationId supplier, string? query, string? category, int skip, int take, CancellationToken ct = default) =>
            Task.FromResult<(IReadOnlyList<SupplierProductExposure>, IReadOnlyList<ConnectedBuyerProductShare>, int)>(([], [], 0));
        public Task<BuyerProductShareSearchPage> SearchForSupplierManagementAsync(
            ConnectedSupplierRelationshipId relationshipId, PosOrganizationId supplier, string? query, string? category, string? shareFilter, int skip, int take, bool idsOnly, CancellationToken ct = default) =>
            Task.FromResult(new BuyerProductShareSearchPage([], [], [], 0, 0, 0, []));
        public Task UpdateAsync(ConnectedBuyerProductShare share, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class InMemoryLinks : IBuyerSupplierProductLinkRepository
    {
        private readonly List<BuyerSupplierProductLink> _items = [];
        public void Seed(BuyerSupplierProductLink link) => _items.Add(link);
        public Task AddAsync(BuyerSupplierProductLink link, CancellationToken ct = default)
        {
            _items.Add(link);
            return Task.CompletedTask;
        }
        public Task<BuyerSupplierProductLink?> FindAsync(ConnectedSupplierRelationshipId relationshipId, CatalogProductId buyerProductId, CancellationToken ct = default) =>
            Task.FromResult(_items.FirstOrDefault(x => x.RelationshipId == relationshipId && x.BuyerProductId == buyerProductId));
        public Task<BuyerSupplierProductLink?> FindBySupplierProductAsync(ConnectedSupplierRelationshipId relationshipId, CatalogProductId supplierProductId, CancellationToken ct = default) =>
            Task.FromResult(_items.FirstOrDefault(x => x.RelationshipId == relationshipId && x.SupplierProductId == supplierProductId));
        public Task<BuyerSupplierProductLink?> GetAsync(BuyerSupplierProductLinkId id, CancellationToken ct = default) =>
            Task.FromResult(_items.FirstOrDefault(x => x.Id == id));
        public Task<IReadOnlyList<BuyerSupplierProductLink>> ListAsync(ConnectedSupplierRelationshipId relationshipId, PosOrganizationId buyer, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<BuyerSupplierProductLink>>(_items.Where(x => x.RelationshipId == relationshipId && x.BuyerOrganizationId == buyer).ToList());
        public Task<IReadOnlyList<BuyerSupplierProductLink>> DeltaAsync(ConnectedSupplierRelationshipId relationshipId, PosOrganizationId buyer, long sinceVersion, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<BuyerSupplierProductLink>>([]);
        public Task UpdateAsync(BuyerSupplierProductLink link, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class InMemoryProducts : ICatalogProductRepository
    {
        private readonly List<CatalogProduct> _items = [];
        public void Seed(CatalogProduct product) => _items.Add(product);
        public Task AddAsync(CatalogProduct product, CancellationToken cancellationToken = default)
        {
            _items.Add(product);
            return Task.CompletedTask;
        }
        public Task<(int TotalCount, int AvailableCount, int NotAvailableCount)> CountConnectedBuyerAvailabilityAsync(PosOrganizationId organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult((0, 0, 0));
        public Task<CatalogProduct?> FindByBarcodeAsync(PosOrganizationId organizationId, string barcode, CancellationToken cancellationToken = default) =>
            Task.FromResult<CatalogProduct?>(null);
        public Task<CatalogProduct?> FindByNormalizedSkuAsync(PosOrganizationId organizationId, string normalizedSku, CancellationToken cancellationToken = default) =>
            Task.FromResult<CatalogProduct?>(null);
        public Task<CatalogProduct?> FindByPlatformGlobalProductIdAsync(PosOrganizationId organizationId, Guid platformGlobalProductId, CancellationToken cancellationToken = default) =>
            Task.FromResult<CatalogProduct?>(null);
        public Task<CatalogProduct?> GetByIdAsync(PosOrganizationId organizationId, CatalogProductId productId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(x => x.OrganizationId == organizationId && x.Id == productId));
        public Task<(IReadOnlyList<CatalogProduct> Items, int TotalCount)> ListAsync(PosOrganizationId organizationId, CatalogProductFilter filter, int skip, int take, CancellationToken cancellationToken = default) =>
            Task.FromResult<(IReadOnlyList<CatalogProduct>, int)>((_items.Where(x => x.OrganizationId == organizationId).ToList(), _items.Count));
        public Task<IReadOnlyList<CatalogProduct>> ListByIdsAsync(PosOrganizationId organizationId, IReadOnlyCollection<CatalogProductId> productIds, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CatalogProduct>>(_items.Where(x => x.OrganizationId == organizationId && productIds.Contains(x.Id)).ToList());
        public Task<IReadOnlyList<(Guid? CategoryId, int Count)>> ListConnectedBuyerAvailabilityCategoryFacetsAsync(PosOrganizationId organizationId, CatalogProductFilter filter, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<(Guid?, int)>>([]);
        public Task<IReadOnlyList<Guid>> ListIdsAsync(PosOrganizationId organizationId, CatalogProductFilter filter, int skip, int take, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Guid>>([]);
        public Task<IReadOnlySet<Guid>> ListPlatformGlobalProductIdsAsync(PosOrganizationId organizationId, IReadOnlyCollection<Guid> platformGlobalProductIds, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlySet<Guid>>(new HashSet<Guid>());
        public Task UpdateAsync(CatalogProduct product, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class InMemoryUnits : ICatalogProductUnitRepository
    {
        public Task AddAsync(CatalogProductUnit unit, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<CatalogProductUnit?> GetByIdAsync(PosOrganizationId organizationId, ProductUnitId unitId, CancellationToken cancellationToken = default) =>
            Task.FromResult<CatalogProductUnit?>(null);
        public Task<IReadOnlyList<CatalogProductUnit>> ListByProductAsync(PosOrganizationId organizationId, CatalogProductId productId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CatalogProductUnit>>([]);
        public Task<IReadOnlyDictionary<Guid, IReadOnlyList<CatalogProductUnit>>> ListByProductIdsAsync(PosOrganizationId organizationId, IReadOnlyCollection<CatalogProductId> productIds, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<Guid, IReadOnlyList<CatalogProductUnit>>>(new Dictionary<Guid, IReadOnlyList<CatalogProductUnit>>());
        public Task ReplaceActiveUnitsAsync(PosOrganizationId organizationId, CatalogProductId productId, ProductUnitKind kind, IReadOnlyList<CatalogProductUnit> units, DateTimeOffset utcNow, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
        public Task UpdateAsync(CatalogProductUnit unit, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class InMemorySuppliers : ISupplierRepository
    {
        private readonly List<Supplier> _items = [];
        public void Seed(Supplier supplier) => _items.Add(supplier);
        public Task AddAsync(Supplier supplier, CancellationToken cancellationToken = default)
        {
            _items.Add(supplier);
            return Task.CompletedTask;
        }
        public Task<string> AllocateNextSupplierCodeAsync(PosOrganizationId organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult("SUP-000001");
        public Task<Supplier?> FindActiveByNormalizedEmailAsync(PosOrganizationId organizationId, string normalizedEmail, CancellationToken cancellationToken = default) =>
            Task.FromResult<Supplier?>(null);
        public Task<Supplier?> FindActiveByNormalizedMobileAsync(PosOrganizationId organizationId, string normalizedMobile, CancellationToken cancellationToken = default) =>
            Task.FromResult<Supplier?>(null);
        public Task<Supplier?> FindActiveByNormalizedNameAsync(PosOrganizationId organizationId, string normalizedName, CancellationToken cancellationToken = default) =>
            Task.FromResult<Supplier?>(null);
        public Task<Supplier?> FindActiveByNormalizedTaxAsync(PosOrganizationId organizationId, string normalizedTax, CancellationToken cancellationToken = default) =>
            Task.FromResult<Supplier?>(null);
        public Task<Supplier?> GetByIdAsync(PosOrganizationId organizationId, SupplierId supplierId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(x => x.OrganizationId == organizationId && x.Id == supplierId));
        public Task<(IReadOnlyList<Supplier> Items, int TotalCount)> ListAsync(PosOrganizationId organizationId, SupplierFilter filter, int skip, int take, CancellationToken cancellationToken = default) =>
            Task.FromResult<(IReadOnlyList<Supplier>, int)>((_items.Where(x => x.OrganizationId == organizationId).ToList(), _items.Count));
        public Task UpdateAsync(Supplier supplier, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class InMemoryOrders : IPurchaseOrderRepository
    {
        public List<PurchaseOrder> Items { get; } = [];
        public Task AddAsync(PurchaseOrder purchaseOrder, CancellationToken cancellationToken = default)
        {
            Items.Add(purchaseOrder);
            return Task.CompletedTask;
        }
        public Task<PurchaseOrder?> GetByIdAsync(PosOrganizationId organizationId, PurchaseOrderId purchaseOrderId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.FirstOrDefault(x => x.OrganizationId == organizationId && x.Id == purchaseOrderId));
        public Task<GoodsReceipt?> GetGoodsReceiptByIdAsync(PosOrganizationId organizationId, GoodsReceiptId goodsReceiptId, CancellationToken cancellationToken = default) =>
            Task.FromResult<GoodsReceipt?>(null);
        public Task<(IReadOnlyList<PurchaseOrder> Items, int TotalCount)> ListAsync(PosOrganizationId organizationId, PurchaseOrderFilter filter, int skip, int take, CancellationToken cancellationToken = default) =>
            Task.FromResult<(IReadOnlyList<PurchaseOrder>, int)>((Items.Where(x => x.OrganizationId == organizationId).ToList(), Items.Count));
        public Task<IReadOnlyList<GoodsReceipt>> ListGoodsReceiptsForPurchaseOrderAsync(PosOrganizationId organizationId, PurchaseOrderId purchaseOrderId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<GoodsReceipt>>([]);
        public Task<(PurchaseOrder PurchaseOrder, GoodsReceipt GoodsReceipt)> ReceiveAsync(
            PosOrganizationId organizationId, PurchaseOrderId purchaseOrderId, DateOnly businessDateUtc,
            Func<string, (PurchaseOrder UpdatedPo, GoodsReceipt Receipt)> applyReceive,
            Func<GoodsReceipt, PurchaseOrder, CancellationToken, Task>? afterReceiptCreated = null,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<PurchaseOrder> SubmitAsync(
            PosOrganizationId organizationId, PurchaseOrderId purchaseOrderId, DateOnly businessDateUtc,
            Func<string, PurchaseOrder> applySubmit,
            Func<PurchaseOrder, CancellationToken, Task>? beforeCommit = null,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task UpdateAsync(PurchaseOrder purchaseOrder, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
