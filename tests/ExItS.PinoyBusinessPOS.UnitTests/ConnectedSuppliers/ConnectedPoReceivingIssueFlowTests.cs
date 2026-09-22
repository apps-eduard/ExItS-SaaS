using System.Reflection;
using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Application.Purchasing;
using ExItS.PinoyBusinessPOS.Application.Returns;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Purchasing;
using ExItS.PinoyBusinessPOS.Domain.Returns;
using ExItS.PinoyBusinessPOS.UnitTests.TestDoubles;

namespace ExItS.PinoyBusinessPOS.UnitTests.ConnectedSuppliers;

/// <summary>
/// End-to-end inventory invariants for connected PO receiving-issue reconciliation
/// (fakes mirror <see cref="Purchasing.ConnectedPurchaseOrderFulfillStockTests"/>).
/// </summary>
public sealed class ConnectedPoReceivingIssueFlowTests
{
    private static readonly PosOrganizationId Buyer =
        PosOrganizationId.From(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
    private static readonly PosOrganizationId Seller =
        PosOrganizationId.From(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));
    private static readonly Guid SupplierBranchId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid BuyerBranchId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid Actor = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly DateTimeOffset Now = new(2026, 9, 21, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ScenarioA_FoundAtSeller_restores_one_and_is_idempotent()
    {
        var productId = CatalogProductId.New();
        var harness = await BuildFulfilledAsync(productId, sellerOnHand: 100m, fulfillQty: 10m);
        Assert.Equal(90m, harness.SellerInventory.Account.OnHandQuantity);

        // Buyer receives 8 good + 1 damaged + 1 missing (buyer on-hand only tracks good).
        harness.BuyerInventory.SeedTracked(Buyer, productId, onHand: 0m);
        harness.BuyerInventory.ApplyDelta(productId, +8m);
        Assert.Equal(8m, harness.BuyerInventory.Account.OnHandQuantity);
        Assert.Equal(90m, harness.SellerInventory.Account.OnHandQuantity);

        var issue = CreateIssue(
            harness.Order,
            harness.BuyerPo,
            productId,
            good: 8m,
            damaged: 1m,
            missing: 1m,
            fulfillmentSourceId: harness.Order.Id.Value);
        await harness.Issues.AddAsync(issue);
        Assert.Equal(ConnectedPoReceivingIssueStatus.PendingSellerReview, issue.Status);

        var missing = issue.Lines.Single(l => l.LineKind == ConnectedPoReceivingIssueLineKind.Missing);
        var resolve = harness.CreateResolveUseCase();
        var result = await resolve.ExecuteAsync(
            Seller.Value,
            harness.Order.Id.Value,
            issue.Id.Value,
            new ResolveConnectedPoReceivingIssueRequest(
            [
                new ResolveConnectedPoReceivingIssueLineRequest(
                    missing.Id.Value,
                    MissingResolution: nameof(ConnectedPoMissingResolution.FoundAtSeller),
                    ResolutionQty: 1m),
            ]),
            Actor);
        Assert.True(result.IsSuccess, $"{result.ErrorCode}: {result.ErrorMessage}");

        Assert.Equal(91m, harness.SellerInventory.Account.OnHandQuantity);
        Assert.Equal(8m, harness.BuyerInventory.Account.OnHandQuantity);
        var fulfill = Assert.Single(
            harness.SellerInventory.Movements,
            m => m.MovementType == StockMovementType.ConnectedPurchaseFulfillment);
        Assert.Equal(-10m, fulfill.QuantityEffect);
        var recon = Assert.Single(
            harness.SellerInventory.Movements,
            m => m.MovementType == StockMovementType.ConnectedPurchaseFulfillmentReconciliation);
        Assert.Equal(1m, recon.QuantityEffect);
        Assert.Equal(missing.Id.Value, recon.SourceId);

        var retry = await resolve.ExecuteAsync(
            Seller.Value,
            harness.Order.Id.Value,
            issue.Id.Value,
            new ResolveConnectedPoReceivingIssueRequest(
            [
                new ResolveConnectedPoReceivingIssueLineRequest(
                    missing.Id.Value,
                    MissingResolution: nameof(ConnectedPoMissingResolution.FoundAtSeller),
                    ResolutionQty: 1m),
            ]),
            Actor);
        Assert.True(retry.IsSuccess, $"{retry.ErrorCode}: {retry.ErrorMessage}");
        Assert.Equal(91m, harness.SellerInventory.Account.OnHandQuantity);
        Assert.Equal(
            1,
            harness.SellerInventory.Movements.Count(m =>
                m.MovementType == StockMovementType.ConnectedPurchaseFulfillmentReconciliation));
    }

    [Fact]
    public async Task ScenarioB_LostInTransit_leaves_seller_at_fulfilled_qty()
    {
        var productId = CatalogProductId.New();
        var harness = await BuildFulfilledAsync(productId, sellerOnHand: 100m, fulfillQty: 10m);
        var issue = CreateIssue(
            harness.Order,
            harness.BuyerPo,
            productId,
            good: 9m,
            damaged: 0m,
            missing: 1m,
            fulfillmentSourceId: harness.Order.Id.Value);
        await harness.Issues.AddAsync(issue);
        var missing = Assert.Single(issue.Lines);

        var result = await harness.CreateResolveUseCase().ExecuteAsync(
            Seller.Value,
            harness.Order.Id.Value,
            issue.Id.Value,
            new ResolveConnectedPoReceivingIssueRequest(
            [
                new ResolveConnectedPoReceivingIssueLineRequest(
                    missing.Id.Value,
                    MissingResolution: nameof(ConnectedPoMissingResolution.LostInTransit),
                    ResolutionQty: 1m),
            ]),
            Actor);

        Assert.True(result.IsSuccess, $"{result.ErrorCode}: {result.ErrorMessage}");
        Assert.Equal(90m, harness.SellerInventory.Account.OnHandQuantity);
        Assert.DoesNotContain(
            harness.SellerInventory.Movements,
            m => m.MovementType == StockMovementType.ConnectedPurchaseFulfillmentReconciliation);
        Assert.Equal(ConnectedPoReceivingIssueStatuses.ToCode(ConnectedPoReceivingIssueStatus.Resolved), result.Value!.Status);
    }

    [Fact]
    public async Task ScenarioC_Damaged_ReturnRequested_links_batch_when_po_received_no_duplicate()
    {
        var productId = CatalogProductId.New();
        var harness = await BuildFulfilledAsync(productId, sellerOnHand: 100m, fulfillQty: 10m);
        // Clear outstanding via good receive + short-close of the damaged unit so PO is Received
        // (return-batch create requires buyer PO Status = Received).
        harness.BuyerPo.ApplyReceiptLines(
            [
                new PurchaseOrderReceiveLineDraft(
                    productId,
                    ReceiveQty: 9m,
                    DamagedQty: 1m,
                    RejectedQty: 0m,
                    ShortClosedQty: 1m,
                    DiscrepancyKind: ConnectedPoReceivingDiscrepancyKind.Damaged),
            ],
            Now.AddMinutes(20));
        Assert.Equal(PurchaseOrderStatus.Received, harness.BuyerPo.Status);

        var issue = CreateIssue(
            harness.Order,
            harness.BuyerPo,
            productId,
            good: 9m,
            damaged: 1m,
            missing: 0m,
            fulfillmentSourceId: harness.Order.Id.Value);
        await harness.Issues.AddAsync(issue);
        var damaged = Assert.Single(issue.Lines);
        var returns = new CapturingReturnBatches();
        var resolve = harness.CreateResolveUseCase(returns);

        var first = await resolve.ExecuteAsync(
            Seller.Value,
            harness.Order.Id.Value,
            issue.Id.Value,
            new ResolveConnectedPoReceivingIssueRequest(
            [
                new ResolveConnectedPoReceivingIssueLineRequest(
                    damaged.Id.Value,
                    DamagedResolution: nameof(ConnectedPoDamagedResolution.ReturnRequested),
                    ResolutionQty: 1m,
                    SellerNote: "Please return"),
            ]),
            Actor);
        Assert.True(first.IsSuccess, $"{first.ErrorCode}: {first.ErrorMessage}");
        Assert.Equal(90m, harness.SellerInventory.Account.OnHandQuantity);
        Assert.DoesNotContain(
            harness.SellerInventory.Movements,
            m => m.MovementType == StockMovementType.ConnectedPurchaseFulfillmentReconciliation);
        Assert.Single(returns.Created);
        Assert.NotNull(first.Value!.Lines[0].ReturnBatchId);

        var second = await resolve.ExecuteAsync(
            Seller.Value,
            harness.Order.Id.Value,
            issue.Id.Value,
            new ResolveConnectedPoReceivingIssueRequest(
            [
                new ResolveConnectedPoReceivingIssueLineRequest(
                    damaged.Id.Value,
                    DamagedResolution: nameof(ConnectedPoDamagedResolution.ReturnRequested),
                    ResolutionQty: 1m,
                    SellerNote: "Please return"),
            ]),
            Actor);
        Assert.True(second.IsSuccess, $"{second.ErrorCode}: {second.ErrorMessage}");
        Assert.Single(returns.Created);
    }

    [Fact]
    public async Task MultiWave_receipt_attributes_latest_fulfillment_source()
    {
        var productId = CatalogProductId.New();
        var harness = await BuildFulfilledAsync(productId, sellerOnHand: 100m, fulfillQty: 6m);
        harness.Order.ReopenForRemainingFulfillment(Now.AddMinutes(10));
        harness.Order.StartPreparing(Now.AddMinutes(11));
        await harness.FulfillStock.ApplyAsync(
            harness.Order,
            harness.Relationship,
            Actor,
            Now.AddMinutes(12),
            shipQtyBySupplierProduct: new Dictionary<Guid, decimal> { [productId.Value] = 4m });

        Assert.Equal(2, harness.SellerInventory.Movements.Count(m =>
            m.MovementType == StockMovementType.ConnectedPurchaseFulfillment));
        var wave1Source = harness.Order.Id.Value;
        var wave2Source = ConnectedPurchaseOrderFulfillStock.WaveFulfillmentSourceId(
            harness.Order.Id.Value,
            harness.Order.InventoryReservationRevision);
        Assert.Contains(harness.SellerInventory.Movements, m => m.SourceId == wave1Source && m.QuantityEffect == -6m);
        Assert.Contains(harness.SellerInventory.Movements, m => m.SourceId == wave2Source && m.QuantityEffect == -4m);

        var candidates = ConnectedPoReceivingIssueFactory.BuildFulfillmentSourceCandidates(harness.Order);
        var attributed = await harness.SellerInventory.FindLatestConnectedPurchaseFulfillmentSourceIdAsync(
            Seller,
            productId,
            candidates);
        Assert.Equal(wave2Source, attributed);

        var issue = CreateIssue(
            harness.Order,
            harness.BuyerPo,
            productId,
            good: 9m,
            damaged: 0m,
            missing: 1m,
            fulfillmentSourceId: attributed!.Value);
        await harness.Issues.AddAsync(issue);
        var missing = Assert.Single(issue.Lines);
        Assert.Equal(wave2Source, missing.FulfillmentSourceId);

        var result = await harness.CreateResolveUseCase().ExecuteAsync(
            Seller.Value,
            harness.Order.Id.Value,
            issue.Id.Value,
            new ResolveConnectedPoReceivingIssueRequest(
            [
                new ResolveConnectedPoReceivingIssueLineRequest(
                    missing.Id.Value,
                    MissingResolution: nameof(ConnectedPoMissingResolution.FoundAtSeller),
                    ResolutionQty: 1m),
            ]),
            Actor);
        Assert.True(result.IsSuccess, $"{result.ErrorCode}: {result.ErrorMessage}");

        var recon = Assert.Single(
            harness.SellerInventory.Movements,
            m => m.MovementType == StockMovementType.ConnectedPurchaseFulfillmentReconciliation);
        Assert.Equal(missing.Id.Value, recon.SourceId);
        Assert.Contains(wave2Source.ToString("D"), recon.Reason, StringComparison.Ordinal);
        Assert.Equal(91m, harness.SellerInventory.Account.OnHandQuantity); // 100-6-4+1
        var wave1 = Assert.Single(
            harness.SellerInventory.Movements,
            m => m.SourceId == wave1Source && m.MovementType == StockMovementType.ConnectedPurchaseFulfillment);
        Assert.Equal(-6m, wave1.QuantityEffect);
    }

    [Fact]
    public async Task Resolve_qty_greater_than_missing_fails()
    {
        var productId = CatalogProductId.New();
        var harness = await BuildFulfilledAsync(productId, sellerOnHand: 50m, fulfillQty: 5m);
        var issue = CreateIssue(harness.Order, harness.BuyerPo, productId, 4m, 0m, 1m, harness.Order.Id.Value);
        await harness.Issues.AddAsync(issue);
        var missing = Assert.Single(issue.Lines);

        var result = await harness.CreateResolveUseCase().ExecuteAsync(
            Seller.Value,
            harness.Order.Id.Value,
            issue.Id.Value,
            new ResolveConnectedPoReceivingIssueRequest(
            [
                new ResolveConnectedPoReceivingIssueLineRequest(
                    missing.Id.Value,
                    MissingResolution: nameof(ConnectedPoMissingResolution.FoundAtSeller),
                    ResolutionQty: 2m),
            ]),
            Actor);

        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrorCodes.InvalidConnectedPoReceivingIssueResolutionQty, result.ErrorCode);
        Assert.Equal(45m, harness.SellerInventory.Account.OnHandQuantity);
    }

    [Fact]
    public async Task Already_resolved_cannot_change_to_different_inventory_resolution()
    {
        var productId = CatalogProductId.New();
        var harness = await BuildFulfilledAsync(productId, sellerOnHand: 50m, fulfillQty: 5m);
        var issue = CreateIssue(harness.Order, harness.BuyerPo, productId, 4m, 0m, 1m, harness.Order.Id.Value);
        await harness.Issues.AddAsync(issue);
        var missing = Assert.Single(issue.Lines);
        var resolve = harness.CreateResolveUseCase();

        var first = await resolve.ExecuteAsync(
            Seller.Value,
            harness.Order.Id.Value,
            issue.Id.Value,
            new ResolveConnectedPoReceivingIssueRequest(
            [
                new ResolveConnectedPoReceivingIssueLineRequest(
                    missing.Id.Value,
                    MissingResolution: nameof(ConnectedPoMissingResolution.FoundAtSeller),
                    ResolutionQty: 1m),
            ]),
            Actor);
        Assert.True(first.IsSuccess);

        var second = await resolve.ExecuteAsync(
            Seller.Value,
            harness.Order.Id.Value,
            issue.Id.Value,
            new ResolveConnectedPoReceivingIssueRequest(
            [
                new ResolveConnectedPoReceivingIssueLineRequest(
                    missing.Id.Value,
                    MissingResolution: nameof(ConnectedPoMissingResolution.LostInTransit),
                    ResolutionQty: 1m),
            ]),
            Actor);
        Assert.False(second.IsSuccess);
        Assert.Equal(DomainErrorCodes.ConnectedPoReceivingIssueAlreadyResolved, second.ErrorCode);
        Assert.Equal(46m, harness.SellerInventory.Account.OnHandQuantity);
    }

    [Fact]
    public void Resolve_request_dto_has_no_inventoryDelta_field()
    {
        var props = typeof(ResolveConnectedPoReceivingIssueLineRequest)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(p => p.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("inventoryDelta", props);
        Assert.DoesNotContain("InventoryDelta", props);

        var batchProps = typeof(ResolveConnectedPoReceivingIssueRequest)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(p => p.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("inventoryDelta", batchProps);
    }

    [Fact]
    public async Task Cross_product_or_wrong_line_fails()
    {
        var productId = CatalogProductId.New();
        var harness = await BuildFulfilledAsync(productId, sellerOnHand: 50m, fulfillQty: 5m);
        var issue = CreateIssue(harness.Order, harness.BuyerPo, productId, 4m, 0m, 1m, harness.Order.Id.Value);
        await harness.Issues.AddAsync(issue);

        var result = await harness.CreateResolveUseCase().ExecuteAsync(
            Seller.Value,
            harness.Order.Id.Value,
            issue.Id.Value,
            new ResolveConnectedPoReceivingIssueRequest(
            [
                new ResolveConnectedPoReceivingIssueLineRequest(
                    Guid.NewGuid(),
                    MissingResolution: nameof(ConnectedPoMissingResolution.FoundAtSeller),
                    ResolutionQty: 1m),
            ]),
            Actor);

        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrorCodes.ConnectedPoReceivingIssueLineNotFound, result.ErrorCode);
    }

    private static ConnectedPoReceivingIssue CreateIssue(
        ConnectedPurchaseOrder order,
        PurchaseOrder buyerPo,
        CatalogProductId productId,
        decimal good,
        decimal damaged,
        decimal missing,
        Guid fulfillmentSourceId)
    {
        var drafts = new List<ConnectedPoReceivingIssueLineDraft>();
        var grnLineId = GoodsReceiptLineId.New();
        var poLineId = buyerPo.Lines[0].Id;
        if (missing > 0m)
        {
            drafts.Add(new ConnectedPoReceivingIssueLineDraft(
                grnLineId,
                poLineId,
                productId,
                productId,
                "Item",
                "Piece",
                good + damaged + missing,
                good,
                damaged,
                missing,
                ConnectedPoReceivingIssueLineKind.Missing,
                ConnectedPoReceivingDiscrepancyKind.Short,
                "short",
                fulfillmentSourceId));
        }

        if (damaged > 0m)
        {
            drafts.Add(new ConnectedPoReceivingIssueLineDraft(
                grnLineId,
                poLineId,
                productId,
                productId,
                "Item",
                "Piece",
                good + damaged + missing,
                good,
                damaged,
                missing,
                ConnectedPoReceivingIssueLineKind.Damaged,
                ConnectedPoReceivingDiscrepancyKind.Damaged,
                "dented",
                fulfillmentSourceId));
        }

        return ConnectedPoReceivingIssue.CreateFromReceipt(
            order.Id,
            buyerPo.Id,
            GoodsReceiptId.New(),
            Buyer,
            Seller,
            fulfillmentSourceId,
            Actor,
            Now.AddMinutes(10),
            drafts);
    }

    private static async Task<Harness> BuildFulfilledAsync(
        CatalogProductId productId,
        decimal sellerOnHand,
        decimal fulfillQty)
    {
        var relationship = ConnectedSupplierRelationship.Request(
            Buyer,
            Seller,
            Now,
            supplierBranchId: SupplierBranchId,
            supplierBranchName: "Main Branch");
        relationship.Approve(Now.AddMinutes(1));

        var sellerProduct = CatalogProduct.Create(Seller, "Item", UnitOfMeasure.Piece, 12m, Now, id: productId);

        var buyerPo = PurchaseOrder.CreateDraft(
            Buyer,
            Domain.Suppliers.SupplierId.New(),
            DateOnly.FromDateTime(Now.UtcDateTime),
            [new PurchaseOrderLineDraft(productId, 10m, 12m)],
            Now,
            intendedReceivingBranchId: BuyerBranchId);
        buyerPo.Submit(
            "260921-001",
            [
                new PurchaseOrderLineSnapshotInput(
                    productId,
                    "Item",
                    UnitOfMeasure.Piece,
                    10m,
                    12m),
            ],
            Actor,
            Now.AddMinutes(1));

        var line = ConnectedPurchaseOrderLine.Create(productId, "Item", "SKU", 10m, 12m, "Piece");
        var order = ConnectedPurchaseOrder.CreateFromBuyerSubmission(
            relationship,
            buyerPo.Id,
            buyerPo.PoNumber ?? "260921-001",
            DateOnly.FromDateTime(Now.UtcDateTime),
            null,
            [line],
            Now.AddMinutes(2));
        order.Accept(Now.AddMinutes(3));

        var sellerInventory = new FlowInventoryStub();
        sellerInventory.SeedTracked(Seller, productId, sellerOnHand);
        var buyerInventory = new FlowInventoryStub();
        var balances = new FlowBalances();
        balances.Seed(InventoryBranchBalance.Create(
            Seller,
            PosBranchId.From(SupplierBranchId),
            productId,
            onHandQuantity: sellerOnHand,
            Now));
        var products = new FlowProducts(sellerProduct);
        var fulfillStock = new ConnectedPurchaseOrderFulfillStock(
            sellerInventory,
            products,
            new FlowUnits(),
            balances,
            new BranchInventoryMutationService());

        await fulfillStock.ApplyAsync(
            order,
            relationship,
            Actor,
            Now.AddMinutes(4),
            shipQtyBySupplierProduct: new Dictionary<Guid, decimal> { [productId.Value] = fulfillQty });
        order.StartPreparing(Now.AddMinutes(5));
        order.MarkFulfilled(Now.AddMinutes(6));

        return new Harness(
            order,
            relationship,
            buyerPo,
            sellerInventory,
            buyerInventory,
            balances,
            products,
            fulfillStock,
            new InMemoryIssues(),
            new InMemoryOrders(order),
            new InMemoryBuyerOrders(buyerPo),
            new InMemoryRelationships(relationship));
    }

    private sealed class Harness(
        ConnectedPurchaseOrder Order,
        ConnectedSupplierRelationship Relationship,
        PurchaseOrder BuyerPo,
        FlowInventoryStub SellerInventory,
        FlowInventoryStub BuyerInventory,
        FlowBalances Balances,
        FlowProducts Products,
        ConnectedPurchaseOrderFulfillStock FulfillStock,
        InMemoryIssues Issues,
        InMemoryOrders Orders,
        InMemoryBuyerOrders BuyerOrders,
        InMemoryRelationships Relationships)
    {
        public ConnectedPurchaseOrder Order { get; } = Order;
        public ConnectedSupplierRelationship Relationship { get; } = Relationship;
        public PurchaseOrder BuyerPo { get; } = BuyerPo;
        public FlowInventoryStub SellerInventory { get; } = SellerInventory;
        public FlowInventoryStub BuyerInventory { get; } = BuyerInventory;
        public FlowBalances Balances { get; } = Balances;
        public FlowProducts Products { get; } = Products;
        public ConnectedPurchaseOrderFulfillStock FulfillStock { get; } = FulfillStock;
        public InMemoryIssues Issues { get; } = Issues;
        public InMemoryOrders Orders { get; } = Orders;
        public InMemoryBuyerOrders BuyerOrders { get; } = BuyerOrders;
        public InMemoryRelationships Relationships { get; } = Relationships;

        public ResolveIncomingOrderReceivingIssue CreateResolveUseCase(
            IReturnBatchRepository? returnBatches = null) =>
            new(
                Orders,
                Issues,
                Relationships,
                BuyerOrders,
                SellerInventory,
                Products,
                Balances,
                new BranchInventoryMutationService(),
                new NoOpUnitOfWork(),
                new FakeAccess(),
                branches: new FixedBranches(),
                returnBatches: returnBatches,
                clock: new FixedClock(Now.AddMinutes(30)));
    }

    private sealed class FlowInventoryStub : CostResolverInventoryStub
    {
        private readonly Dictionary<Guid, InventoryAccount> _accounts = new();
        public List<StockMovement> Movements { get; } = [];
        public InventoryAccount Account => _accounts.Values.Single();

        public void SeedTracked(PosOrganizationId org, CatalogProductId productId, decimal onHand) =>
            _accounts[productId.Value] = InventoryAccount.Rehydrate(
                InventoryAccountId.From(productId.Value),
                org,
                productId,
                isTracked: true,
                reorderLevel: null,
                reorderQuantity: null,
                onHandQuantity: onHand,
                createdAtUtc: Now,
                updatedAtUtc: Now);

        public void ApplyDelta(CatalogProductId productId, decimal delta)
        {
            var account = _accounts[productId.Value];
            account.ApplyMovementEffect(delta);
            account.Touch(Now);
            _accounts[productId.Value] = account;
        }

        public override Task<InventoryAccount?> GetByProductIdAsync(
            PosOrganizationId organizationId,
            CatalogProductId productId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(
                _accounts.TryGetValue(productId.Value, out var a) && a.OrganizationId == organizationId
                    ? a
                    : null);

        public override Task<IReadOnlyList<InventoryAccount>> ListByProductIdsAsync(
            PosOrganizationId organizationId,
            IReadOnlyCollection<CatalogProductId> productIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<InventoryAccount>>(
                productIds
                    .Select(id => _accounts.TryGetValue(id.Value, out var a) && a.OrganizationId == organizationId ? a : null)
                    .Where(a => a is not null)
                    .Cast<InventoryAccount>()
                    .ToList());

        public override Task UpdateAccountAsync(InventoryAccount account, CancellationToken cancellationToken = default)
        {
            _accounts[account.ProductId.Value] = account;
            return Task.CompletedTask;
        }

        public override Task AddMovementAsync(StockMovement movement, CancellationToken cancellationToken = default)
        {
            Movements.Add(movement);
            return Task.CompletedTask;
        }

        public override Task ExecuteWithProductReservationLocksAsync(
            PosOrganizationId organizationId,
            IReadOnlyCollection<CatalogProductId> productIds,
            Func<IReadOnlyList<InventoryAccount>, CancellationToken, Task> action,
            CancellationToken cancellationToken = default) =>
            action(
                productIds
                    .Select(id => _accounts.TryGetValue(id.Value, out var a) ? a : null)
                    .Where(a => a is not null)
                    .Cast<InventoryAccount>()
                    .ToList(),
                cancellationToken);

        public override Task<bool> HasConnectedPurchaseFulfillmentAsync(
            PosOrganizationId organizationId,
            ConnectedPurchaseOrderId connectedPurchaseOrderId,
            CatalogProductId productId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Movements.Any(m =>
                m.OrganizationId == organizationId
                && m.ProductId == productId
                && m.SourceId == connectedPurchaseOrderId.Value
                && m.MovementType == StockMovementType.ConnectedPurchaseFulfillment));

        public override Task<bool> HasConnectedPurchaseFulfillmentReconciliationAsync(
            PosOrganizationId organizationId,
            Guid receivingIssueLineId,
            CatalogProductId productId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Movements.Any(m =>
                m.OrganizationId == organizationId
                && m.ProductId == productId
                && m.SourceId == receivingIssueLineId
                && m.MovementType == StockMovementType.ConnectedPurchaseFulfillmentReconciliation));

        public override Task<Guid?> FindLatestConnectedPurchaseFulfillmentSourceIdAsync(
            PosOrganizationId organizationId,
            CatalogProductId productId,
            IReadOnlyCollection<Guid> candidateSourceIds,
            CancellationToken cancellationToken = default)
        {
            var match = Movements
                .Where(m =>
                    m.OrganizationId == organizationId
                    && m.ProductId == productId
                    && m.SourceId is Guid sid
                    && candidateSourceIds.Contains(sid)
                    && m.MovementType == StockMovementType.ConnectedPurchaseFulfillment)
                .OrderByDescending(m => m.RecordedAtUtc)
                .FirstOrDefault();
            return Task.FromResult(match?.SourceId);
        }
    }

    private sealed class FlowBalances : IInventoryBranchBalanceRepository
    {
        private readonly Dictionary<(Guid Branch, Guid Product), InventoryBranchBalance> _rows = new();

        public void Seed(InventoryBranchBalance balance) =>
            _rows[(balance.BranchId.Value, balance.ProductId.Value)] = balance;

        public Task<InventoryBranchBalance?> GetAsync(
            PosOrganizationId organizationId,
            PosBranchId branchId,
            CatalogProductId productId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(
                _rows.TryGetValue((branchId.Value, productId.Value), out var row) ? row : null);

        public Task<IReadOnlyList<InventoryBranchBalance>> ListByProductIdsAsync(
            PosOrganizationId organizationId,
            IReadOnlyCollection<CatalogProductId> productIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<InventoryBranchBalance>>(
                _rows.Values.Where(r => productIds.Any(p => p.Value == r.ProductId.Value)).ToList());

        public Task UpsertAsync(InventoryBranchBalance balance, CancellationToken cancellationToken = default)
        {
            _rows[(balance.BranchId.Value, balance.ProductId.Value)] = balance;
            return Task.CompletedTask;
        }
    }

    private sealed class FlowProducts(params CatalogProduct[] products) : ICatalogProductRepository
    {
        private readonly Dictionary<Guid, CatalogProduct> _byId = products.ToDictionary(p => p.Id.Value);

        public Task<CatalogProduct?> GetByIdAsync(
            PosOrganizationId organizationId,
            CatalogProductId productId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(
                _byId.TryGetValue(productId.Value, out var p) && p.OrganizationId == organizationId ? p : null);

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
                productIds
                    .Select(id => _byId.TryGetValue(id.Value, out var p) && p.OrganizationId == organizationId ? p : null)
                    .Where(p => p is not null)
                    .Cast<CatalogProduct>()
                    .ToList());

        public Task<(IReadOnlyList<CatalogProduct> Items, int TotalCount)> ListAsync(
            PosOrganizationId organizationId,
            CatalogProductFilter filter,
            int skip,
            int take,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<Guid>> ListIdsAsync(
            PosOrganizationId organizationId,
            CatalogProductFilter filter,
            int skip,
            int take,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<(int TotalCount, int AvailableCount, int NotAvailableCount)> CountConnectedBuyerAvailabilityAsync(
            PosOrganizationId organizationId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<(Guid? CategoryId, int Count)>> ListConnectedBuyerAvailabilityCategoryFacetsAsync(
            PosOrganizationId organizationId,
            CatalogProductFilter filter,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

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

        public Task AddAsync(CatalogProduct productToAdd, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task UpdateAsync(CatalogProduct productToUpdate, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class FlowUnits : ICatalogProductUnitRepository
    {
        public Task<CatalogProductUnit?> GetByIdAsync(
            PosOrganizationId organizationId,
            ProductUnitId unitId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<CatalogProductUnit?>(null);

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
                productIds.ToDictionary(p => p.Value, _ => (IReadOnlyList<CatalogProductUnit>)[]));

        public Task AddAsync(CatalogProductUnit unit, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task UpdateAsync(CatalogProductUnit unit, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task ReplaceActiveUnitsAsync(
            PosOrganizationId organizationId,
            CatalogProductId productId,
            ProductUnitKind kind,
            IReadOnlyList<CatalogProductUnit> units,
            DateTimeOffset utcNow,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class InMemoryIssues : IConnectedPoReceivingIssueRepository
    {
        private readonly Dictionary<Guid, ConnectedPoReceivingIssue> _byId = new();

        public Task AddAsync(ConnectedPoReceivingIssue issue, CancellationToken ct = default)
        {
            _byId[issue.Id.Value] = issue;
            return Task.CompletedTask;
        }

        public Task UpdateAsync(ConnectedPoReceivingIssue issue, CancellationToken ct = default)
        {
            _byId[issue.Id.Value] = issue;
            return Task.CompletedTask;
        }

        public Task<ConnectedPoReceivingIssue?> GetAsync(ConnectedPoReceivingIssueId id, CancellationToken ct = default) =>
            Task.FromResult(_byId.TryGetValue(id.Value, out var i) ? i : null);

        public Task<ConnectedPoReceivingIssue?> GetByGoodsReceiptAsync(
            GoodsReceiptId goodsReceiptId,
            CancellationToken ct = default) =>
            Task.FromResult(_byId.Values.FirstOrDefault(i => i.GoodsReceiptId == goodsReceiptId));

        public Task<IReadOnlyList<ConnectedPoReceivingIssue>> ListByConnectedOrderAsync(
            ConnectedPurchaseOrderId connectedPurchaseOrderId,
            CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<ConnectedPoReceivingIssue>>(
                _byId.Values.Where(i => i.ConnectedPurchaseOrderId == connectedPurchaseOrderId).ToList());
    }

    private sealed class InMemoryOrders(ConnectedPurchaseOrder order) : IConnectedPurchaseOrderRepository
    {
        public Task<ConnectedPurchaseOrder?> GetAsync(ConnectedPurchaseOrderId id, CancellationToken ct = default) =>
            Task.FromResult(id == order.Id ? order : null);

        public Task<ConnectedPurchaseOrder?> GetByBuyerPurchaseOrderAsync(
            PurchaseOrderId buyerPurchaseOrderId,
            CancellationToken ct = default) =>
            Task.FromResult(order.BuyerPurchaseOrderId == buyerPurchaseOrderId ? order : null);

        public Task AddAsync(ConnectedPurchaseOrder entity, CancellationToken ct = default) => Task.CompletedTask;
        public Task UpdateAsync(ConnectedPurchaseOrder entity, CancellationToken ct = default) => Task.CompletedTask;

        public Task<IReadOnlyList<ConnectedPurchaseOrder>> ListIncomingAsync(
            PosOrganizationId supplier,
            CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<ConnectedPurchaseOrder>>([order]);

        public Task<IReadOnlyList<ConnectedPurchaseOrder>> ListBetweenOrganizationsAsync(
            PosOrganizationId supplierOrganizationId,
            PosOrganizationId buyerOrganizationId,
            CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<ConnectedPurchaseOrder>>([order]);
    }

    private sealed class InMemoryBuyerOrders(PurchaseOrder po) : IPurchaseOrderRepository
    {
        public Task<PurchaseOrder?> GetByIdAsync(
            PosOrganizationId organizationId,
            PurchaseOrderId purchaseOrderId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(organizationId == po.OrganizationId && purchaseOrderId == po.Id ? po : null);

        public Task AddAsync(PurchaseOrder purchaseOrder, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task UpdateAsync(PurchaseOrder purchaseOrder, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<(IReadOnlyList<PurchaseOrder> Items, int TotalCount)> ListAsync(
            PosOrganizationId organizationId,
            PurchaseOrderFilter filter,
            int skip,
            int take,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<PurchaseOrder> SubmitAsync(
            PosOrganizationId organizationId,
            PurchaseOrderId purchaseOrderId,
            DateOnly businessDateUtc,
            Func<string, PurchaseOrder> applySubmit,
            Func<PurchaseOrder, CancellationToken, Task>? beforeCommit = null,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<(PurchaseOrder PurchaseOrder, GoodsReceipt GoodsReceipt)> ReceiveAsync(
            PosOrganizationId organizationId,
            PurchaseOrderId purchaseOrderId,
            DateOnly businessDateUtc,
            Func<string, (PurchaseOrder UpdatedPo, GoodsReceipt Receipt)> applyReceive,
            Func<GoodsReceipt, PurchaseOrder, CancellationToken, Task>? afterReceiptCreated = null,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<GoodsReceipt?> GetGoodsReceiptByIdAsync(
            PosOrganizationId organizationId,
            GoodsReceiptId goodsReceiptId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<GoodsReceipt?>(null);

        public Task UpdateGoodsReceiptAsync(GoodsReceipt receipt, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<IReadOnlyList<GoodsReceipt>> ListGoodsReceiptsForPurchaseOrderAsync(
            PosOrganizationId organizationId,
            PurchaseOrderId purchaseOrderId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<GoodsReceipt>>([]);
    }

    private sealed class InMemoryRelationships(ConnectedSupplierRelationship relationship)
        : IConnectedSupplierRelationshipRepository
    {
        public Task<ConnectedSupplierRelationship?> GetAsync(
            ConnectedSupplierRelationshipId id,
            CancellationToken ct = default) =>
            Task.FromResult(id == relationship.Id ? relationship : null);

        public Task AddAsync(ConnectedSupplierRelationship entity, CancellationToken ct = default) =>
            Task.CompletedTask;

        public Task UpdateAsync(ConnectedSupplierRelationship entity, CancellationToken ct = default) =>
            Task.CompletedTask;

        public Task<ConnectedSupplierRelationship?> FindOpenAsync(
            PosOrganizationId buyer,
            PosOrganizationId supplier,
            CancellationToken ct = default) =>
            Task.FromResult<ConnectedSupplierRelationship?>(relationship);

        public Task<IReadOnlyList<ConnectedSupplierRelationship>> ListAsync(
            PosOrganizationId organizationId,
            bool supplierView,
            CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<ConnectedSupplierRelationship>>([relationship]);
    }

    private sealed class CapturingReturnBatches : IReturnBatchRepository
    {
        public List<ReturnBatch> Created { get; } = [];

        public Task<ReturnBatch> CreateAsync(
            PosOrganizationId organizationId,
            DateOnly businessDateUtc,
            Func<string, ReturnBatch> createBatch,
            Func<ReturnBatch, CancellationToken, Task>? afterCreated = null,
            CancellationToken cancellationToken = default)
        {
            var batch = createBatch(ReturnBatchNumbers.Format(new DateOnly(2026, 9, 21), Created.Count + 1));
            Created.Add(batch);
            return Task.FromResult(batch);
        }

        public Task<ReturnBatch?> GetByIdAsync(
            PosOrganizationId organizationId,
            ReturnBatchId returnBatchId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Created.FirstOrDefault(b => b.Id == returnBatchId));

        public Task UpdateAsync(ReturnBatch batch, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<IReadOnlyList<ReturnBatch>> ListBySaleIdAsync(
            PosOrganizationId organizationId,
            Domain.Sales.SaleId saleId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ReturnBatch>>([]);

        public Task<IReadOnlyList<ReturnBatch>> ListByPurchaseOrderIdAsync(
            PosOrganizationId organizationId,
            PurchaseOrderId purchaseOrderId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ReturnBatch>>(
                Created.Where(b => b.PurchaseOrderId == purchaseOrderId).ToList());

        public Task<IReadOnlyList<ReturnBatch>> ListByConnectedPurchaseOrderIdAsync(
            PosOrganizationId organizationId,
            ConnectedPurchaseOrderId connectedPurchaseOrderId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ReturnBatch>>(
                Created.Where(b => b.ConnectedPurchaseOrderId == connectedPurchaseOrderId).ToList());

        public Task<IReadOnlyList<ReturnBatch>> ListOpenConnectedForSellerAsync(
            PosOrganizationId sellerOrganizationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ReturnBatch>>(Created);
    }

    private sealed class NoOpUnitOfWork : IPosUnitOfWork
    {
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<T> ExecuteInSerializableTransactionAsync<T>(
            Func<CancellationToken, Task<T>> action,
            CancellationToken cancellationToken = default) =>
            action(cancellationToken);
    }

    private sealed class FakeAccess : IPosCommercialAccessAccessor
    {
        public PosCommercialAccess Current { get; set; } = PosCommercialAccess.DevelopmentDefault;
    }

    private sealed class FixedClock(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    private sealed class FixedBranches : IOrganizationBranchDirectory
    {
        public Task<bool> ExistsInOrganizationAsync(
            Guid organizationId,
            Guid branchId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(true);

        public Task<IReadOnlyDictionary<Guid, string>> GetNamesAsync(
            Guid organizationId,
            IReadOnlyCollection<Guid> branchIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<Guid, string>>(
                branchIds.ToDictionary(id => id, _ => "Branch"));

        public Task<Guid?> GetPrimaryBranchIdAsync(Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult<Guid?>(SupplierBranchId);
    }
}
