using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.CustomerOrdering;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.CustomerOrdering;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Purchasing;
using ExItS.PinoyBusinessPOS.Domain.Returns;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.UnitTests.CustomerOrdering;

public sealed class CustomerOrderDeliveryFeeCalculatorTests
{
    [Fact]
    public void Calculate_AppliesBasePlusDistanceCharge()
    {
        var policy = new CustomerOrderBranchDeliveryPolicySnapshot(0m, 50m, 2m, 10m, 20m, null);
        var quote = CustomerOrderDeliveryFeeCalculator.Calculate(policy, 100m, 5m);
        Assert.Equal(3m, quote.ExtraDistanceKm);
        Assert.Equal(30m, quote.DistanceCharge);
        Assert.Equal(80m, quote.DeliveryFee);
        Assert.False(quote.FreeDeliveryApplied);
    }

    [Fact]
    public void Calculate_AppliesFreeDeliveryThreshold()
    {
        var policy = new CustomerOrderBranchDeliveryPolicySnapshot(0m, 50m, 2m, 10m, 20m, 200m);
        var quote = CustomerOrderDeliveryFeeCalculator.Calculate(policy, 250m, 8m);
        Assert.Equal(0m, quote.DeliveryFee);
        Assert.True(quote.FreeDeliveryApplied);
    }
}

public sealed class CustomerOrderStockServiceTests
{
    [Fact]
    public async Task Reserve_And_Release_TrackedAccount()
    {
        var org = PosOrganizationId.From(Guid.Parse("11111111-1111-1111-1111-111111111111"));
        var productId = CatalogProductId.From(Guid.Parse("22222222-2222-2222-2222-222222222222"));
        var now = DateTimeOffset.Parse("2026-08-16T10:00:00Z");
        var account = InventoryAccount.CreateUntracked(org, productId, now);
        account.Enable(10m, UnitOfMeasure.Piece, Guid.Parse("33333333-3333-3333-3333-333333333333"), now, false);
        var inventory = new FakeInventoryRepository(account);
        var stock = new CustomerOrderStockService(inventory);

        var order = CustomerOrder.CreateSubmitted(
            org,
            "260816-001",
            CustomerOrderParty.Personal(Guid.Parse("44444444-4444-4444-4444-444444444444"), "Pat"),
            CustomerOrderFulfillmentType.Pickup,
            Guid.Parse("55555555-5555-5555-5555-555555555555"),
            "Main",
            [new CustomerOrderLineDraft(productId, "Rice", "SKU", UnitOfMeasure.Piece, 3m, 10m)],
            Guid.Parse("66666666-6666-6666-6666-666666666666"),
            now);

        order.Accept(Guid.Parse("66666666-6666-6666-6666-666666666666"), now);
        await stock.ReserveForAcceptAsync(order, Guid.Parse("66666666-6666-6666-6666-666666666666"), now);

        Assert.Equal(10m, inventory.Account.OnHandQuantity);
        Assert.Equal(3m, inventory.Account.ReservedQuantity);
        Assert.Equal(7m, inventory.Account.AvailableQuantity);
        Assert.Equal(CustomerOrderStockReservationState.Reserved, order.StockReservationState);

        await stock.ReleaseIfReservedAsync(order, now.AddMinutes(1));
        Assert.Equal(0m, inventory.Account.ReservedQuantity);
        Assert.Equal(CustomerOrderStockReservationState.Released, order.StockReservationState);
        Assert.Equal(2, inventory.LockCallCount);
    }

    [Fact]
    public async Task Reserve_IsIdempotent_WhenAlreadyReserved_AndDocumentsLockAcquisition()
    {
        var org = PosOrganizationId.From(Guid.Parse("11111111-1111-1111-1111-111111111111"));
        var productId = CatalogProductId.From(Guid.Parse("22222222-2222-2222-2222-222222222222"));
        var now = DateTimeOffset.Parse("2026-08-16T10:00:00Z");
        var account = InventoryAccount.CreateUntracked(org, productId, now);
        account.Enable(10m, UnitOfMeasure.Piece, Guid.Parse("33333333-3333-3333-3333-333333333333"), now, false);
        var inventory = new FakeInventoryRepository(account);
        var stock = new CustomerOrderStockService(inventory);

        var order = CustomerOrder.CreateSubmitted(
            org,
            "260816-002",
            CustomerOrderParty.Personal(Guid.Parse("44444444-4444-4444-4444-444444444444"), "Pat"),
            CustomerOrderFulfillmentType.Pickup,
            Guid.Parse("55555555-5555-5555-5555-555555555555"),
            "Main",
            [new CustomerOrderLineDraft(productId, "Rice", "SKU", UnitOfMeasure.Piece, 2m, 10m)],
            Guid.Parse("66666666-6666-6666-6666-666666666666"),
            now);

        order.Accept(Guid.Parse("66666666-6666-6666-6666-666666666666"), now);
        await stock.ReserveForAcceptAsync(order, Guid.Parse("66666666-6666-6666-6666-666666666666"), now);
        await stock.ReserveForAcceptAsync(order, Guid.Parse("66666666-6666-6666-6666-666666666666"), now.AddSeconds(1));

        Assert.Equal(2m, inventory.Account.ReservedQuantity);
        Assert.Equal(1, inventory.LockCallCount);
    }

    [Fact]
    public async Task EnsureAvailable_UsesOnHandMinusReserved_AndSkipsUntracked()
    {
        var org = PosOrganizationId.From(Guid.Parse("11111111-1111-1111-1111-111111111111"));
        var trackedId = CatalogProductId.From(Guid.Parse("22222222-2222-2222-2222-222222222222"));
        var untrackedId = CatalogProductId.From(Guid.Parse("77777777-7777-7777-7777-777777777777"));
        var now = DateTimeOffset.Parse("2026-08-16T10:00:00Z");
        var tracked = InventoryAccount.CreateUntracked(org, trackedId, now);
        tracked.Enable(10m, UnitOfMeasure.Piece, Guid.Parse("33333333-3333-3333-3333-333333333333"), now, false);
        tracked.Reserve(8m);
        var inventory = new FakeInventoryRepository(tracked);
        var stock = new CustomerOrderStockService(inventory);
        var over = new CustomerOrderLineDraft(trackedId, "Rice", "SKU", UnitOfMeasure.Piece, 3m, 10m);
        var limited = await stock.EnsureAvailableAsync(org, [over]);
        Assert.False(limited.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.InsufficientStock, limited.ErrorCode);
        Assert.Equal("2", limited.ErrorDetails!["availableQuantity"]);
        Assert.Equal("3", limited.ErrorDetails["requestedQuantity"]);

        var ok = await stock.EnsureAvailableAsync(org, [over with { Quantity = 2m }]);
        Assert.True(ok.IsSuccess);

        var untracked = InventoryAccount.CreateUntracked(org, untrackedId, now);
        inventory.Account = untracked;
        var untrackedLine = new CustomerOrderLineDraft(untrackedId, "Bread", "B", UnitOfMeasure.Piece, 50m, 25m);
        Assert.True((await stock.EnsureAvailableAsync(org, [untrackedLine])).IsSuccess);
    }

    [Fact]
    public async Task Consume_skips_cancelled_zero_quantity_lines()
    {
        var org = PosOrganizationId.From(Guid.Parse("11111111-1111-1111-1111-111111111111"));
        var keepId = CatalogProductId.From(Guid.Parse("22222222-2222-2222-2222-222222222222"));
        var cancelId = CatalogProductId.From(Guid.Parse("88888888-8888-8888-8888-888888888888"));
        var actor = Guid.Parse("66666666-6666-6666-6666-666666666666");
        var now = DateTimeOffset.Parse("2026-08-16T10:00:00Z");

        var keepAccount = InventoryAccount.CreateUntracked(org, keepId, now);
        keepAccount.Enable(10m, UnitOfMeasure.Piece, actor, now, false);
        var cancelAccount = InventoryAccount.CreateUntracked(org, cancelId, now);
        cancelAccount.Enable(10m, UnitOfMeasure.Piece, actor, now, false);
        var inventory = new FakeInventoryRepository([keepAccount, cancelAccount]);
        var stock = new CustomerOrderStockService(inventory);
        var keepProduct = CatalogProduct.Create(org, "Rice", UnitOfMeasure.Piece, 10m, now, id: keepId);
        var cancelProduct = CatalogProduct.Create(org, "Snack", UnitOfMeasure.Piece, 5m, now, id: cancelId);

        var order = CustomerOrder.CreateSubmitted(
            org,
            "260816-004",
            CustomerOrderParty.Personal(Guid.Parse("44444444-4444-4444-4444-444444444444"), "Pat"),
            CustomerOrderFulfillmentType.Pickup,
            Guid.Parse("55555555-5555-5555-5555-555555555555"),
            "Main",
            [
                new CustomerOrderLineDraft(keepId, "Rice", "R", UnitOfMeasure.Piece, 2m, 10m),
                new CustomerOrderLineDraft(cancelId, "Snack", "S", UnitOfMeasure.Piece, 1m, 5m),
            ],
            actor,
            now);
        order.Accept(actor, now);
        await stock.ReserveForAcceptAsync(order, actor, now);

        var cancelLine = order.Lines.Single(l => l.ProductId == cancelId);
        var releases = order.AdjustLines(
            [new CustomerOrderLineAdjustment(cancelLine.Id.Value, 0m, CustomerOrderLineAdjustmentReason.CustomerRequested)],
            actor,
            now.AddMinutes(1));
        await stock.ReleaseReservationDeltasAsync(order, releases, now.AddMinutes(1));

        await stock.ConsumeOnCompleteAsync(
            order,
            new Dictionary<Guid, CatalogProduct>
            {
                [keepId.Value] = keepProduct,
                [cancelId.Value] = cancelProduct,
            },
            actor,
            now.AddMinutes(2));

        Assert.Equal(CustomerOrderStockReservationState.Consumed, order.StockReservationState);
        Assert.Equal(8m, inventory.Accounts[keepId.Value].OnHandQuantity);
        Assert.Equal(0m, inventory.Accounts[keepId.Value].ReservedQuantity);
        Assert.Equal(10m, inventory.Accounts[cancelId.Value].OnHandQuantity);
        Assert.Equal(0m, inventory.Accounts[cancelId.Value].ReservedQuantity);
        Assert.Single(inventory.Movements);
        Assert.Equal(keepId, inventory.Movements[0].ProductId);
        Assert.Equal(-2m, inventory.Movements[0].QuantityEffect);
    }

    [Fact]
    public async Task Untracked_line_does_not_reserve_or_consume()
    {
        var org = PosOrganizationId.From(Guid.Parse("11111111-1111-1111-1111-111111111111"));
        var productId = CatalogProductId.From(Guid.Parse("22222222-2222-2222-2222-222222222222"));
        var now = DateTimeOffset.Parse("2026-08-16T10:00:00Z");
        var account = InventoryAccount.CreateUntracked(org, productId, now);
        var inventory = new FakeInventoryRepository(account);
        var stock = new CustomerOrderStockService(inventory);
        var product = CatalogProduct.Create(org, "Bread", UnitOfMeasure.Piece, 25m, now, id: productId);
        var order = CustomerOrder.CreateSubmitted(
            org,
            "260816-003",
            CustomerOrderParty.Personal(Guid.Parse("44444444-4444-4444-4444-444444444444"), "Pat"),
            CustomerOrderFulfillmentType.Pickup,
            Guid.Parse("55555555-5555-5555-5555-555555555555"),
            "Main",
            [new CustomerOrderLineDraft(productId, "Bread", "B", UnitOfMeasure.Piece, 2m, 25m)],
            Guid.Parse("66666666-6666-6666-6666-666666666666"),
            now);
        order.Accept(Guid.Parse("66666666-6666-6666-6666-666666666666"), now);
        await stock.ReserveForAcceptAsync(order, Guid.Parse("66666666-6666-6666-6666-666666666666"), now);
        Assert.Equal(0m, inventory.Account.ReservedQuantity);
        await stock.ConsumeOnCompleteAsync(
            order,
            new Dictionary<Guid, CatalogProduct> { [productId.Value] = product },
            Guid.Parse("66666666-6666-6666-6666-666666666666"),
            now.AddMinutes(3));
        Assert.Equal(0m, inventory.Account.OnHandQuantity);
        Assert.Empty(inventory.Movements);
    }

    private sealed class FakeInventoryRepository : IInventoryRepository
    {
        private readonly Dictionary<Guid, InventoryAccount> _accounts;
        private readonly List<StockMovement> _movements = [];

        public FakeInventoryRepository(InventoryAccount account)
            : this([account])
        {
        }

        public FakeInventoryRepository(IEnumerable<InventoryAccount> accounts)
        {
            _accounts = accounts.ToDictionary(a => a.ProductId.Value);
        }

        public InventoryAccount Account
        {
            get => _accounts.Values.First();
            set => _accounts[value.ProductId.Value] = value;
        }

        public IReadOnlyDictionary<Guid, InventoryAccount> Accounts => _accounts;

        public Task<InventoryAccount?> GetByProductIdAsync(
            PosOrganizationId organizationId,
            CatalogProductId productId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(
                _accounts.TryGetValue(productId.Value, out var account)
                    && account.OrganizationId == organizationId
                    ? account
                    : null);

        public Task<IReadOnlyList<InventoryAccount>> ListByProductIdsAsync(
            PosOrganizationId organizationId,
            IReadOnlyCollection<CatalogProductId> productIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<InventoryAccount>>(
                productIds
                    .Select(id => _accounts.GetValueOrDefault(id.Value))
                    .Where(a => a is not null && a.OrganizationId == organizationId)
                    .Cast<InventoryAccount>()
                    .ToList());

        public Task UpdateAccountAsync(InventoryAccount account, CancellationToken cancellationToken = default)
        {
            _accounts[account.ProductId.Value] = account;
            return Task.CompletedTask;
        }

        public Task ExecuteWithProductReservationLocksAsync(
            PosOrganizationId organizationId,
            IReadOnlyCollection<CatalogProductId> productIds,
            Func<IReadOnlyList<InventoryAccount>, CancellationToken, Task> action,
            CancellationToken cancellationToken = default)
        {
            LockCallCount++;
            var locked = productIds
                .Select(id => _accounts.GetValueOrDefault(id.Value))
                .Where(a => a is not null && a.OrganizationId == organizationId)
                .Cast<InventoryAccount>()
                .ToList();
            return action(locked, cancellationToken);
        }

        public int LockCallCount { get; private set; }
        public IReadOnlyList<StockMovement> Movements => _movements;

        public Task AddMovementAsync(StockMovement movement, CancellationToken cancellationToken = default)
        {
            _movements.Add(movement);
            return Task.CompletedTask;
        }

        public Task<StockMovement?> GetMovementByIdAsync(
            PosOrganizationId organizationId,
            StockMovementId movementId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<StockMovement?>(
                _movements.FirstOrDefault(m =>
                    m.OrganizationId == organizationId && m.Id == movementId));

        public Task<bool> HasCustomerOrderDeductionAsync(
            PosOrganizationId organizationId,
            CustomerOrderId orderId,
            CatalogProductId productId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_movements.Any(m =>
                m.SourceId == orderId.Value
                && m.ProductId == productId
                && m.SourceType == StockMovementSourceType.CustomerOrder));

        public Task AddAccountAsync(InventoryAccount account, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
        public Task<(IReadOnlyList<InventoryAccount> Items, int TotalCount)> ListAsync(PosOrganizationId organizationId, InventoryAccountFilter filter, int skip, int take, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<(IReadOnlyList<InventoryAccount> Items, int TotalCount)> ListLowStockAsync(PosOrganizationId organizationId, string? search, int skip, int take, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<InventoryAccount>> ListAllAccountsAsync(PosOrganizationId organizationId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> HasAnyMovementAsync(PosOrganizationId organizationId, CatalogProductId productId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> HasOpeningStockAsync(PosOrganizationId organizationId, CatalogProductId productId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<(IReadOnlyList<StockMovement> Items, int TotalCount)> ListMovementsAsync(PosOrganizationId organizationId, CatalogProductId productId, StockMovementFilter filter, int skip, int take, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<decimal> SumMovementEffectsAsync(PosOrganizationId organizationId, CatalogProductId productId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<(IReadOnlyList<InventoryAccount> Items, int TotalCount)> ListReorderSuggestionsAsync(PosOrganizationId organizationId, string? search, int skip, int take, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> HasStockCountVarianceAsync(PosOrganizationId organizationId, StockCountId stockCountId, CatalogProductId productId, StockMovementType movementType, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<StockMovement>> ListMovementsForReportAsync(PosOrganizationId organizationId, DateOnly fromDateUtc, DateOnly toDateUtc, Guid? branchId = null, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<StockMovement>> ListSaleDeductionsAsync(PosOrganizationId organizationId, SaleId saleId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> HasSaleDeductionAsync(PosOrganizationId organizationId, SaleId saleId, CatalogProductId productId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> HasSaleVoidRestorationAsync(PosOrganizationId organizationId, SaleId saleId, CatalogProductId productId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> HasPurchaseReceiptAsync(PosOrganizationId organizationId, GoodsReceiptId goodsReceiptId, CatalogProductId productId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> HasDirectPurchaseReceiptAsync(PosOrganizationId organizationId, DirectPurchaseReceiptId receiptId, CatalogProductId productId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
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

    public Task<bool> HasPurchaseReceiptReversalAsync(
        PosOrganizationId organizationId,
        GoodsReceiptId goodsReceiptId,
        CatalogProductId productId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(false);

    public Task<bool> HasDirectPurchaseReceiptReversalAsync(
        PosOrganizationId organizationId,
        DirectPurchaseReceiptId receiptId,
        CatalogProductId productId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(false);

    public Task<bool> HasConnectedPurchaseFulfillmentAsync(
        PosOrganizationId organizationId,
        ConnectedPurchaseOrderId connectedPurchaseOrderId,
        CatalogProductId productId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(false);


        public Task<decimal?> GetLatestAcquisitionUnitCostAsync(PosOrganizationId organizationId, CatalogProductId productId, CancellationToken cancellationToken = default) =>
            Task.FromResult<decimal?>(null);
        public Task<bool> HasSaleReturnRestockAsync(PosOrganizationId organizationId, SaleReturnId saleReturnId, CatalogProductId productId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> HasInventoryTransferMovementAsync(PosOrganizationId organizationId, InventoryTransferId transferId, CatalogProductId productId, StockMovementType movementType, InventoryLotId? lotId = null, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<(DateTimeOffset? LatestAt, int Count)> GetMovementSummaryAsync(PosOrganizationId organizationId, CatalogProductId productId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyDictionary<Guid, (DateTimeOffset? LatestAt, int Count)>> GetMovementSummariesAsync(PosOrganizationId organizationId, IReadOnlyCollection<CatalogProductId> productIds, CancellationToken cancellationToken = default) => throw new NotImplementedException();

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
}