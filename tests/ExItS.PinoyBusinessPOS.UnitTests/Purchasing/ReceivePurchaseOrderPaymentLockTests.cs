using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.ConnectedSuppliers;
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

public sealed class ReceivePurchaseOrderPaymentLockTests
{
    private static readonly PosOrganizationId Org = PosOrganizationId.From(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
    private static readonly Guid Warehouse = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Actor = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
    private static readonly DateTimeOffset Now = new(2026, 9, 17, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ExecuteAsync_partial_good_qty_uses_good_total_and_cash_settlement()
    {
        var product = CatalogProduct.Create(Org, "Rice", UnitOfMeasure.Piece, 50m, Now);
        var supplier = Supplier.Create(Org, "SUP-400001", "Mill", Now);
        var po = PurchaseOrder.CreateDraft(
            Org,
            supplier.Id,
            DateOnly.FromDateTime(Now.Date),
            [new PurchaseOrderLineDraft(product.Id, 10m, 70m)],
            Now,
            intendedReceivingBranchId: Warehouse,
            paymentTerm: ConnectedPoPaymentTerm.Cash);
        po.Submit(
            "260917-001",
            [
                new PurchaseOrderLineSnapshotInput(
                    product.Id,
                    product.Name,
                    product.UnitOfMeasure,
                    10m,
                    70m)
            ],
            Actor,
            Now.AddMinutes(1));

        var orders = new InMemoryOrders();
        orders.Seed(po);
        var products = new InMemoryProducts();
        products.Seed(product);
        var payables = new CapturingPayables();

        var receive = new ReceivePurchaseOrder(
            orders,
            products,
            new FakePurchaseStock(),
            new FakeAccess(),
            new InMemoryConnectedOrders(),
            new CreateSupplierPayableFromReceipt(payables),
            new FixedTimeProvider(Now));

        var result = await receive.ExecuteAsync(
            Org.Value,
            po.Id.Value,
            new ReceivePurchaseOrderRequest(
                [new ReceivePurchaseOrderLineRequest(
                    product.Id.Value,
                    ReceiveQty: 3m,
                    RejectedQty: 7m,
                    DiscrepancyKind: "Short")],
                PaymentMethodAtReceipt: "Cash",
                PaidNow: 210m),
            Actor,
            Warehouse);

        Assert.True(result.IsSuccess, $"{result.ErrorCode}: {result.ErrorMessage}");
        Assert.NotNull(payables.LastPayable);
        Assert.Equal(210m, payables.LastPayable!.OriginalAmount);
        Assert.Equal(210m, payables.LastPayable.PaidAtReceiptAmount);
    }

    [Fact]
    public async Task ExecuteAsync_rejects_mismatched_payment_method()
    {
        var (receive, po, product) = BuildFixture(ConnectedPoPaymentTerm.Cash);

        var result = await receive.ExecuteAsync(
            Org.Value,
            po.Id.Value,
            new ReceivePurchaseOrderRequest(
                [new ReceivePurchaseOrderLineRequest(product.Id.Value, 2m)],
                PaymentMethodAtReceipt: "GCash",
                PaidNow: 140m,
                GCashReference: "X"),
            Actor,
            Warehouse);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.PurchaseReceiptPaymentMethodMismatch, result.ErrorCode);
    }

    private static (ReceivePurchaseOrder UseCase, PurchaseOrder Po, CatalogProduct Product) BuildFixture(
        ConnectedPoPaymentTerm term)
    {
        var product = CatalogProduct.Create(Org, "Oil", UnitOfMeasure.Piece, 80m, Now);
        var supplier = Supplier.Create(Org, "SUP-400002", "Oil Co", Now);
        var po = PurchaseOrder.CreateDraft(
            Org,
            supplier.Id,
            DateOnly.FromDateTime(Now.Date),
            [new PurchaseOrderLineDraft(product.Id, 5m, 70m)],
            Now,
            intendedReceivingBranchId: Warehouse,
            paymentTerm: term);
        po.Submit(
            "260917-002",
            [
                new PurchaseOrderLineSnapshotInput(
                    product.Id,
                    product.Name,
                    product.UnitOfMeasure,
                    5m,
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
            new CreateSupplierPayableFromReceipt(new CapturingPayables()),
            new FixedTimeProvider(Now));

        return (receive, po, product);
    }

    private sealed class FakeAccess : IPosCommercialAccessAccessor
    {
        public PosCommercialAccess Current { get; set; } = PosCommercialAccess.DevelopmentDefault;
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

    private sealed class CapturingPayables : ISupplierPayableRepository
    {
        public SupplierPayable? LastPayable { get; private set; }

        public Task AddAsync(SupplierPayable payable, CancellationToken cancellationToken = default)
        {
            LastPayable = payable;
            return Task.CompletedTask;
        }

        public Task UpdateAsync(SupplierPayable payable, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<SupplierPayable?> GetByIdAsync(
            PosOrganizationId organizationId,
            SupplierPayableId payableId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<SupplierPayable?>(null);

        public Task<SupplierPayable?> FindBySourceAsync(
            PosOrganizationId organizationId,
            SupplierPayableSourceType sourceType,
            Guid sourceId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<SupplierPayable?>(null);

        public Task<(IReadOnlyList<SupplierPayable> Items, int TotalCount)> ListAsync(
            PosOrganizationId organizationId,
            SupplierPayableFilter filter,
            int skip,
            int take,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<SupplierPayablePayment>> ListPaymentsAsync(
            PosOrganizationId organizationId,
            SupplierPayableId payableId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<SupplierPayablePayment>>([]);

        public Task<SupplierPayableSummaryTotals> GetSupplierSummaryAsync(
            PosOrganizationId organizationId,
            SupplierId supplierId,
            DateOnly asOfDate,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new SupplierPayableSummaryTotals(0m, 0m, 0));
    }

    private sealed class InMemoryProducts : ICatalogProductRepository
    {
        private readonly List<CatalogProduct> _items = [];
        public void Seed(CatalogProduct product) => _items.Add(product);
        public Task AddAsync(CatalogProduct product, CancellationToken cancellationToken = default) => Task.CompletedTask;
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

    private sealed class InMemoryOrders : IPurchaseOrderRepository
    {
        public List<PurchaseOrder> Items { get; } = [];
        public void Seed(PurchaseOrder po) => Items.Add(po);
        public Task AddAsync(PurchaseOrder purchaseOrder, CancellationToken cancellationToken = default) => Task.CompletedTask;
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
            var applied = applyReceive("260917-001");
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
        public Task<IReadOnlyList<ConnectedPurchaseOrder>> ListBetweenOrganizationsAsync(
            PosOrganizationId supplierOrganizationId,
            PosOrganizationId buyerOrganizationId,
            CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<ConnectedPurchaseOrder>>([]);
    }
}
