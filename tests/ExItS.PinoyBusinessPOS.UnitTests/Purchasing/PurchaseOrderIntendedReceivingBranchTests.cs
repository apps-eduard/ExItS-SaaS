using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Application.Purchasing;
using ExItS.PinoyBusinessPOS.Application.SupplierPayables;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Purchasing;
using ExItS.PinoyBusinessPOS.Domain.SupplierPayables;
using ExItS.PinoyBusinessPOS.Domain.Suppliers;

namespace ExItS.PinoyBusinessPOS.UnitTests.Purchasing;

public sealed class PurchaseOrderIntendedReceivingBranchTests
{
    private static readonly PosOrganizationId Org = PosOrganizationId.From(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
    private static readonly Guid Warehouse = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Retail = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid Actor = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
    private static readonly DateTimeOffset Now = new(2026, 9, 7, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Domain_create_draft_stores_intended_receiving_branch()
    {
        var po = PurchaseOrder.CreateDraft(
            Org,
            SupplierId.From(Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc")),
            DateOnly.FromDateTime(Now.Date),
            [new PurchaseOrderLineDraft(CatalogProductId.New(), 1m, 10m)],
            Now,
            intendedReceivingBranchId: Warehouse);

        Assert.Equal(Warehouse, po.IntendedReceivingBranchId);
        Assert.Equal(Warehouse, PurchaseMapper.Map(po).IntendedReceivingBranchId);
    }

    [Fact]
    public async Task Create_defaults_intended_branch_to_acting_branch_when_omitted()
    {
        var product = CatalogProduct.Create(Org, "Rice", UnitOfMeasure.Piece, 50m, Now);
        var supplier = Supplier.Create(Org, "SUP-300001", "Local Mill", Now);
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
            Org.Value,
            new CreatePurchaseOrderRequest(
                supplier.Id.Value,
                DateOnly.FromDateTime(Now.Date),
                [new CreatePurchaseOrderLineRequest(product.Id.Value, 2m, 40m)]),
            actorId: Actor,
            actingBranchId: Warehouse);

        Assert.True(result.IsSuccess, $"{result.ErrorCode}: {result.ErrorMessage}");
        Assert.Equal(Warehouse, result.Value!.IntendedReceivingBranchId);
        Assert.Equal(Warehouse, orders.Items.Single().IntendedReceivingBranchId);
    }

    [Fact]
    public async Task Create_without_acting_branch_leaves_intended_null()
    {
        var product = CatalogProduct.Create(Org, "Soap", UnitOfMeasure.Piece, 20m, Now);
        var supplier = Supplier.Create(Org, "SUP-300002", "Walk-in", Now);
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
            Org.Value,
            new CreatePurchaseOrderRequest(
                supplier.Id.Value,
                DateOnly.FromDateTime(Now.Date),
                [new CreatePurchaseOrderLineRequest(product.Id.Value, 1m, 15m)]),
            actorId: Actor);

        Assert.True(result.IsSuccess, $"{result.ErrorCode}: {result.ErrorMessage}");
        Assert.Null(result.Value!.IntendedReceivingBranchId);
    }

    [Fact]
    public async Task Receive_rejects_wrong_branch_when_intended_is_set()
    {
        var (receive, po, product) = BuildReceiveFixture(intendedBranch: Warehouse);

        var wrong = await receive.ExecuteAsync(
            Org.Value,
            po.Id.Value,
            new ReceivePurchaseOrderRequest([new ReceivePurchaseOrderLineRequest(product.Id.Value, 5m)]),
            Actor,
            Retail);
        Assert.False(wrong.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.PurchasingReceivingBranchMismatch, wrong.ErrorCode);
    }

    [Fact]
    public async Task Receive_succeeds_at_intended_branch_and_null_intended_allows_acting_branch()
    {
        var (receiveIntended, poIntended, productIntended) = BuildReceiveFixture(intendedBranch: Warehouse);
        var ok = await receiveIntended.ExecuteAsync(
            Org.Value,
            poIntended.Id.Value,
            new ReceivePurchaseOrderRequest([new ReceivePurchaseOrderLineRequest(productIntended.Id.Value, 5m)]),
            Actor,
            Warehouse);
        Assert.True(ok.IsSuccess, $"{ok.ErrorCode}: {ok.ErrorMessage}");

        var (receiveNull, poNull, productNull) = BuildReceiveFixture(intendedBranch: null);
        var legacy = await receiveNull.ExecuteAsync(
            Org.Value,
            poNull.Id.Value,
            new ReceivePurchaseOrderRequest([new ReceivePurchaseOrderLineRequest(productNull.Id.Value, 3m)]),
            Actor,
            Retail);
        Assert.True(legacy.IsSuccess, $"{legacy.ErrorCode}: {legacy.ErrorMessage}");
    }

    private static (ReceivePurchaseOrder UseCase, PurchaseOrder Po, CatalogProduct Product) BuildReceiveFixture(Guid? intendedBranch)
    {
        var product = CatalogProduct.Create(Org, "Oil", UnitOfMeasure.Piece, 80m, Now);
        var supplier = Supplier.Create(Org, "SUP-300003", "Oil Co", Now);
        var po = PurchaseOrder.CreateDraft(
            Org,
            supplier.Id,
            DateOnly.FromDateTime(Now.Date),
            [new PurchaseOrderLineDraft(product.Id, intendedBranch is null ? 3m : 5m, 70m)],
            Now,
            intendedReceivingBranchId: intendedBranch);
        po.Submit(
            intendedBranch is null ? "PO-20260907-000002" : "PO-20260907-000001",
            [
                new PurchaseOrderLineSnapshotInput(
                    product.Id,
                    product.Name,
                    product.UnitOfMeasure,
                    intendedBranch is null ? 3m : 5m,
                    70m)
            ],
            Actor,
            Now.AddMinutes(1));

        var orders = new InMemoryOrders();
        orders.Seed(po);
        var products = new InMemoryProducts();
        products.Seed(product);

        var receive = new ReceivePurchaseOrder(
            orders,
            products,
            new FakePurchaseStock(),
            new FakeAccess(),
            new InMemoryConnectedOrders(),
            new CreateSupplierPayableFromReceipt(new FakePayables()),
            new FixedTimeProvider(Now));

        return (receive, po, product);
    }

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

    private sealed class FakePurchaseStock : IPurchaseStockService
    {
        public Task<IReadOnlyList<PurchaseReceiptStockOutcome>> ApplyReceiptAsync(
            PosOrganizationId organizationId,
            GoodsReceipt receipt,
            PurchaseOrder purchaseOrder,
            Guid actorId,
            DateTimeOffset utcNow,
            bool enableTrackingIfNeeded = false,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<PurchaseReceiptStockOutcome>>([]);
    }

    private sealed class FakePayables : ISupplierPayableRepository
    {
        public Task AddAsync(SupplierPayable payable, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task UpdateAsync(SupplierPayable payable, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<SupplierPayable?> GetByIdAsync(PosOrganizationId organizationId, SupplierPayableId payableId, CancellationToken cancellationToken = default) =>
            Task.FromResult<SupplierPayable?>(null);
        public Task<SupplierPayable?> FindBySourceAsync(PosOrganizationId organizationId, SupplierPayableSourceType sourceType, Guid sourceId, CancellationToken cancellationToken = default) =>
            Task.FromResult<SupplierPayable?>(null);
        public Task<(IReadOnlyList<SupplierPayable> Items, int TotalCount)> ListAsync(PosOrganizationId organizationId, SupplierPayableFilter filter, int skip, int take, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<IReadOnlyList<SupplierPayablePayment>> ListPaymentsAsync(PosOrganizationId organizationId, SupplierPayableId payableId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<SupplierPayablePayment>>([]);
        public Task<SupplierPayableSummaryTotals> GetSupplierSummaryAsync(PosOrganizationId organizationId, SupplierId supplierId, DateOnly asOfDate, CancellationToken cancellationToken = default) =>
            Task.FromResult(new SupplierPayableSummaryTotals(0m, 0m, 0));
    }

    private sealed class InMemoryConnectedOrders : IConnectedPurchaseOrderRepository
    {
        public Task AddAsync(ConnectedPurchaseOrder order, CancellationToken ct = default) => Task.CompletedTask;
        public Task UpdateAsync(ConnectedPurchaseOrder order, CancellationToken ct = default) => Task.CompletedTask;
        public Task<ConnectedPurchaseOrder?> GetAsync(ConnectedPurchaseOrderId id, CancellationToken ct = default) =>
            Task.FromResult<ConnectedPurchaseOrder?>(null);
        public Task<ConnectedPurchaseOrder?> GetByBuyerPurchaseOrderAsync(PurchaseOrderId id, CancellationToken ct = default) =>
            Task.FromResult<ConnectedPurchaseOrder?>(null);
        public Task<IReadOnlyList<ConnectedPurchaseOrder>> ListIncomingAsync(PosOrganizationId supplier, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<ConnectedPurchaseOrder>>([]);
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
        public Task<Supplier?> FindByConnectedRelationshipIdAsync(
            PosOrganizationId organizationId,
            ConnectedSupplierRelationshipId relationshipId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<Supplier?>(null);
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
        public Task<(IReadOnlyList<Supplier> Items, int TotalCount)> ListAsync(PosOrganizationId organizationId, SupplierFilter filter, int skip, int take, IReadOnlyCollection<Guid>? restrictToSupplierIds = null, CancellationToken cancellationToken = default) =>
            Task.FromResult<(IReadOnlyList<Supplier>, int)>((_items.Where(x => x.OrganizationId == organizationId).ToList(), _items.Count));
        public Task UpdateAsync(Supplier supplier, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<IReadOnlyDictionary<Guid, string>> GetDisplayNamesByIdsAsync(PosOrganizationId organizationId, IReadOnlyCollection<Guid> supplierIds, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<Guid, string>>(new Dictionary<Guid, string>());
    }

    private sealed class InMemoryRelationships : IConnectedSupplierRelationshipRepository
    {
        public Task AddAsync(ConnectedSupplierRelationship relationship, CancellationToken ct = default) => Task.CompletedTask;
        public Task<ConnectedSupplierRelationship?> FindOpenAsync(PosOrganizationId buyer, PosOrganizationId supplier, CancellationToken ct = default) =>
            Task.FromResult<ConnectedSupplierRelationship?>(null);
        public Task<ConnectedSupplierRelationship?> GetAsync(ConnectedSupplierRelationshipId id, CancellationToken ct = default) =>
            Task.FromResult<ConnectedSupplierRelationship?>(null);
        public Task<IReadOnlyList<ConnectedSupplierRelationship>> ListAsync(PosOrganizationId organizationId, bool supplierView, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<ConnectedSupplierRelationship>>([]);
        public Task UpdateAsync(ConnectedSupplierRelationship relationship, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class InMemoryLinks : IBuyerSupplierProductLinkRepository
    {
        public Task AddAsync(BuyerSupplierProductLink link, CancellationToken ct = default) => Task.CompletedTask;
        public Task<BuyerSupplierProductLink?> GetAsync(BuyerSupplierProductLinkId id, CancellationToken ct = default) =>
            Task.FromResult<BuyerSupplierProductLink?>(null);
        public Task<BuyerSupplierProductLink?> FindAsync(ConnectedSupplierRelationshipId relationshipId, CatalogProductId buyerProductId, CancellationToken ct = default) =>
            Task.FromResult<BuyerSupplierProductLink?>(null);
        public Task<BuyerSupplierProductLink?> FindBySupplierProductAsync(ConnectedSupplierRelationshipId relationshipId, CatalogProductId supplierProductId, CancellationToken ct = default) =>
            Task.FromResult<BuyerSupplierProductLink?>(null);
        public Task<IReadOnlyList<BuyerSupplierProductLink>> ListAsync(ConnectedSupplierRelationshipId relationshipId, PosOrganizationId buyer, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<BuyerSupplierProductLink>>([]);
        public Task<IReadOnlyList<BuyerSupplierProductLink>> DeltaAsync(ConnectedSupplierRelationshipId relationshipId, PosOrganizationId buyer, long sinceVersion, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<BuyerSupplierProductLink>>([]);
        public Task UpdateAsync(BuyerSupplierProductLink link, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class InMemoryOrders : IPurchaseOrderRepository
    {
        public List<PurchaseOrder> Items { get; } = [];
        public void Seed(PurchaseOrder po) => Items.Add(po);
        public Task AddAsync(PurchaseOrder purchaseOrder, CancellationToken cancellationToken = default)
        {
            Items.Add(purchaseOrder);
            return Task.CompletedTask;
        }
        public Task<PurchaseOrder?> GetByIdAsync(PosOrganizationId organizationId, PurchaseOrderId purchaseOrderId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.FirstOrDefault(x => x.OrganizationId == organizationId && x.Id == purchaseOrderId));
        public Task<GoodsReceipt?> GetGoodsReceiptByIdAsync(PosOrganizationId organizationId, GoodsReceiptId goodsReceiptId, CancellationToken cancellationToken = default) =>
            Task.FromResult<GoodsReceipt?>(null);
        public Task UpdateGoodsReceiptAsync(GoodsReceipt receipt, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<(IReadOnlyList<PurchaseOrder> Items, int TotalCount)> ListAsync(PosOrganizationId organizationId, PurchaseOrderFilter filter, int skip, int take, CancellationToken cancellationToken = default) =>
            Task.FromResult<(IReadOnlyList<PurchaseOrder>, int)>((Items.Where(x => x.OrganizationId == organizationId).ToList(), Items.Count));
        public Task<IReadOnlyList<GoodsReceipt>> ListGoodsReceiptsForPurchaseOrderAsync(PosOrganizationId organizationId, PurchaseOrderId purchaseOrderId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<GoodsReceipt>>([]);
        public async Task<(PurchaseOrder PurchaseOrder, GoodsReceipt GoodsReceipt)> ReceiveAsync(
            PosOrganizationId organizationId,
            PurchaseOrderId purchaseOrderId,
            DateOnly businessDateUtc,
            Func<string, (PurchaseOrder UpdatedPo, GoodsReceipt Receipt)> applyReceive,
            Func<GoodsReceipt, PurchaseOrder, CancellationToken, Task>? afterReceiptCreated = null,
            CancellationToken cancellationToken = default)
        {
            var applied = applyReceive("GRN-20260907-000001");
            if (afterReceiptCreated is not null)
            {
                await afterReceiptCreated(applied.Receipt, applied.UpdatedPo, cancellationToken).ConfigureAwait(false);
            }

            return applied;
        }
        public Task<PurchaseOrder> SubmitAsync(
            PosOrganizationId organizationId,
            PurchaseOrderId purchaseOrderId,
            DateOnly businessDateUtc,
            Func<string, PurchaseOrder> applySubmit,
            Func<PurchaseOrder, CancellationToken, Task>? beforeCommit = null,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task UpdateAsync(PurchaseOrder purchaseOrder, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
