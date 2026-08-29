using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Application.Returns;
using ExItS.PinoyBusinessPOS.Domain.CashierShifts;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.CustomerOrdering;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Purchasing;
using ExItS.PinoyBusinessPOS.Domain.Returns;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.UnitTests.Returns;

public sealed class SaleReturnStockServiceTests
{
    private static readonly PosOrganizationId Org = PosOrganizationId.From(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
    private static readonly Guid Actor = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly DateTimeOffset Now = new(2026, 8, 13, 12, 0, 0, TimeSpan.Zero);
    private static readonly PosBranchId Branch = PosBranchId.From(Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"));

    [Fact]
    public async Task Aggregates_duplicate_product_return_lines_into_one_account_restock()
    {
        var productId = CatalogProductId.New();
        var account = EnableTracked(productId, 0m);
        var inventory = new FakeInventory(account);
        var product = CatalogProduct.Create(Org, "Milk", UnitOfMeasure.Piece, 10m, Now, id: productId);
        var sale = SaleWithTwoLinesSameProduct(productId, quantityEach: 2m);
        var saleReturn = ReturnForLines(sale, RestockDisposition.ReturnToStock);
        var lots = new FakeLots();
        var service = new SaleReturnStockService(
            inventory, new FakeProducts([product]), new InventoryLotStockService(lots), lots, new FakeReturns());

        await service.RestockForReturnAsync(Org, saleReturn, sale, Actor, Now);

        Assert.Single(inventory.Movements);
        Assert.Equal(4m, inventory.Movements[0].QuantityEffect);
        Assert.Equal(4m, account.OnHandQuantity);
    }

    [Fact]
    public async Task DoNotRestock_skips_account_branch_and_lots()
    {
        var productId = CatalogProductId.New();
        var inventory = new FakeInventory(EnableTracked(productId, 0m));
        var product = CatalogProduct.Create(Org, "Milk", UnitOfMeasure.Piece, 10m, Now, id: productId);
        product.SetExpirationTracking(true, 7, Now);
        var sale = SaleWithOneLine(productId, 2m, Branch);
        var saleReturn = ReturnForLines(sale, RestockDisposition.DoNotRestock);
        var lots = new FakeLots();
        var balances = new FakeBalances();
        balances.Items.Add(InventoryBranchBalance.Create(Org, Branch, productId, 0m, Now));
        var service = new SaleReturnStockService(
            inventory,
            new FakeProducts([product]),
            new InventoryLotStockService(lots),
            lots,
            new FakeReturns(),
            balances);

        await service.RestockForReturnAsync(Org, saleReturn, sale, Actor, Now);

        Assert.Empty(inventory.Movements);
        Assert.Empty(lots.Movements);
        Assert.Empty(balances.Upserts);
    }

    [Fact]
    public async Task Branch_delta_fails_when_sale_branch_missing_and_balances_exist()
    {
        var productId = CatalogProductId.New();
        var inventory = new FakeInventory(EnableTracked(productId, 0m));
        var product = CatalogProduct.Create(Org, "Soap", UnitOfMeasure.Piece, 10m, Now, id: productId);
        var sale = SaleWithOneLine(productId, 2m, branchId: null);
        var saleReturn = ReturnForLines(sale, RestockDisposition.ReturnToStock);
        var lots = new FakeLots();
        var balances = new FakeBalances();
        balances.Items.Add(InventoryBranchBalance.Create(Org, Branch, productId, 5m, Now));
        var service = new SaleReturnStockService(
            inventory,
            new FakeProducts([product]),
            new InventoryLotStockService(lots),
            lots,
            new FakeReturns(),
            balances);

        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            service.RestockForReturnAsync(Org, saleReturn, sale, Actor, Now));
        Assert.Equal(ApplicationErrorCodes.SaleReturnBranchRequired, ex.ErrorCode);
    }

    [Fact]
    public async Task Historical_account_restock_without_lot_evidence_fails_closed()
    {
        var productId = CatalogProductId.New();
        var inventory = new FakeInventory(EnableTracked(productId, 0m));
        var product = CatalogProduct.Create(Org, "Milk", UnitOfMeasure.Piece, 10m, Now, id: productId);
        product.SetExpirationTracking(true, 7, Now);
        var sale = SaleWithOneLine(productId, 5m, Branch);
        var priorReturnId = SaleReturnId.New();
        inventory.MarkRestocked(priorReturnId, productId);

        var priorReturn = SaleReturn.Rehydrate(
            priorReturnId,
            Org,
            "RET-1",
            sale.Id,
            CashierShiftId.New(),
            null,
            null,
            SalePaymentMethod.Cash,
            SaleReturnStatus.Completed,
            DateOnly.FromDateTime(Now.UtcDateTime),
            "prior",
            null,
            10m,
            Now,
            Actor,
            Now,
            [
                SaleReturnLine.Rehydrate(
                    SaleReturnLineId.New(),
                    priorReturnId,
                    Org,
                    sale.Lines[0].Id,
                    productId,
                    "Milk",
                    UnitOfMeasure.Piece,
                    2m,
                    10m,
                    20m,
                    RestockDisposition.ReturnToStock,
                    null,
                    Guid.NewGuid())
            ]);

        var currentReturn = ReturnForLines(sale, RestockDisposition.ReturnToStock, quantity: 1m);
        var lots = new FakeLots();
        var service = new SaleReturnStockService(
            inventory,
            new FakeProducts([product]),
            new InventoryLotStockService(lots),
            lots,
            new FakeReturns([priorReturn]));

        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            service.RestockForReturnAsync(Org, currentReturn, sale, Actor, Now));
        Assert.Equal(ApplicationErrorCodes.ExpiryReturnHistoryReconciliationGap, ex.ErrorCode);
    }

    private static InventoryAccount EnableTracked(CatalogProductId productId, decimal onHand)
    {
        var account = InventoryAccount.CreateUntracked(Org, productId, Now);
        account.Enable(onHand, UnitOfMeasure.Piece, Actor, Now, hasOpeningStockAlready: false);
        return account;
    }

    private static Sale SaleWithOneLine(CatalogProductId productId, decimal qty, PosBranchId? branchId)
    {
        var saleId = SaleId.New();
        var line = SaleLine.Create(
            saleId,
            Org,
            1,
            new SaleLineDraft(productId, "Item", "SKU", null, UnitOfMeasure.Piece, 10m, qty));
        return Sale.Rehydrate(
            saleId,
            Org,
            "S-1",
            SaleStatus.Completed,
            SalePaymentMethod.Cash,
            line.LineTotal,
            line.LineTotal,
            0m,
            line.LineTotal,
            0m,
            null,
            Now,
            Actor,
            null,
            null,
            null,
            Now,
            [line],
            cashierShiftId: CashierShiftId.New(),
            branchId: branchId);
    }

    private static Sale SaleWithTwoLinesSameProduct(CatalogProductId productId, decimal quantityEach)
    {
        var saleId = SaleId.New();
        var line1 = SaleLine.Create(
            saleId,
            Org,
            1,
            new SaleLineDraft(productId, "Item", "SKU", null, UnitOfMeasure.Piece, 10m, quantityEach));
        var line2 = SaleLine.Create(
            saleId,
            Org,
            2,
            new SaleLineDraft(productId, "Item", "SKU", null, UnitOfMeasure.Piece, 10m, quantityEach));
        var total = line1.LineTotal + line2.LineTotal;
        return Sale.Rehydrate(
            saleId,
            Org,
            "S-2",
            SaleStatus.Completed,
            SalePaymentMethod.Cash,
            total,
            total,
            0m,
            total,
            0m,
            null,
            Now,
            Actor,
            null,
            null,
            null,
            Now,
            [line1, line2],
            cashierShiftId: CashierShiftId.New());
    }

    private static SaleReturn ReturnForLines(
        Sale sale,
        RestockDisposition disposition,
        decimal? quantity = null)
    {
        var returnId = SaleReturnId.New();
        var lines = sale.Lines.Select(sl =>
            SaleReturnLine.Rehydrate(
                SaleReturnLineId.New(),
                returnId,
                Org,
                sl.Id,
                sl.ProductId,
                sl.NameSnapshot,
                sl.UnitOfMeasureSnapshot,
                quantity ?? sl.Quantity,
                sl.UnitPrice,
                SaleMoney.RoundMoney((quantity ?? sl.Quantity) * sl.UnitPrice),
                disposition,
                null,
                null)).ToList();
        return SaleReturn.Rehydrate(
            returnId,
            Org,
            "RET-CUR",
            sale.Id,
            CashierShiftId.New(),
            null,
            null,
            SalePaymentMethod.Cash,
            SaleReturnStatus.Completed,
            DateOnly.FromDateTime(Now.UtcDateTime),
            "return",
            null,
            lines.Sum(l => l.RefundAmount),
            Now,
            Actor,
            Now,
            lines);
    }

    private sealed class FakeInventory(InventoryAccount account) : IInventoryRepository
    {
        private readonly List<StockMovement> _movements = [];
        private readonly HashSet<(Guid, Guid)> _restocked = [];

        public IReadOnlyList<StockMovement> Movements => _movements;

        public void MarkRestocked(SaleReturnId returnId, CatalogProductId productId) =>
            _restocked.Add((returnId.Value, productId.Value));

        public Task<IReadOnlyList<InventoryAccount>> ListByProductIdsAsync(
            PosOrganizationId organizationId,
            IReadOnlyCollection<CatalogProductId> productIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<InventoryAccount>>(
                productIds.Any(p => p == account.ProductId) ? [account] : []);

        public Task UpdateAccountAsync(InventoryAccount updated, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task AddMovementAsync(StockMovement movement, CancellationToken cancellationToken = default)
        {
            _movements.Add(movement);
            if (movement.MovementType == StockMovementType.SaleReturnRestock && movement.SourceId is Guid sid)
            {
                _restocked.Add((sid, movement.ProductId.Value));
            }

            return Task.CompletedTask;
        }

        public Task<StockMovement?> GetMovementByIdAsync(
            PosOrganizationId organizationId,
            StockMovementId movementId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<StockMovement?>(
                _movements.FirstOrDefault(m =>
                    m.OrganizationId == organizationId && m.Id == movementId));

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
            Task.FromResult(_restocked.Contains((saleReturnId.Value, productId.Value)));

        public Task<InventoryAccount?> GetByProductIdAsync(PosOrganizationId organizationId, CatalogProductId productId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<(IReadOnlyList<InventoryAccount> Items, int TotalCount)> ListAsync(PosOrganizationId organizationId, InventoryAccountFilter filter, int skip, int take, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<(IReadOnlyList<InventoryAccount> Items, int TotalCount)> ListLowStockAsync(PosOrganizationId organizationId, string? search, int skip, int take, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<InventoryAccount>> ListAllAccountsAsync(PosOrganizationId organizationId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task AddAccountAsync(InventoryAccount account, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task ExecuteWithProductReservationLocksAsync(PosOrganizationId organizationId, IReadOnlyCollection<CatalogProductId> productIds, Func<IReadOnlyList<InventoryAccount>, CancellationToken, Task> action, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> HasAnyMovementAsync(PosOrganizationId organizationId, CatalogProductId productId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> HasOpeningStockAsync(PosOrganizationId organizationId, CatalogProductId productId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<(IReadOnlyList<StockMovement> Items, int TotalCount)> ListMovementsAsync(PosOrganizationId organizationId, CatalogProductId productId, StockMovementFilter filter, int skip, int take, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<decimal> SumMovementEffectsAsync(PosOrganizationId organizationId, CatalogProductId productId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<(IReadOnlyList<InventoryAccount> Items, int TotalCount)> ListReorderSuggestionsAsync(PosOrganizationId organizationId, string? search, int skip, int take, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> HasStockCountVarianceAsync(PosOrganizationId organizationId, StockCountId stockCountId, CatalogProductId productId, StockMovementType movementType, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<StockMovement>> ListMovementsForReportAsync(PosOrganizationId organizationId, DateOnly fromDateUtc, DateOnly toDateUtc, Guid? branchId = null, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<StockMovement>> ListSaleDeductionsAsync(PosOrganizationId organizationId, SaleId saleId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> HasSaleDeductionAsync(PosOrganizationId organizationId, SaleId saleId, CatalogProductId productId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> HasCustomerOrderDeductionAsync(PosOrganizationId organizationId, CustomerOrderId orderId, CatalogProductId productId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> HasSaleVoidRestorationAsync(PosOrganizationId organizationId, SaleId saleId, CatalogProductId productId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> HasPurchaseReceiptAsync(PosOrganizationId organizationId, GoodsReceiptId goodsReceiptId, CatalogProductId productId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> HasDirectPurchaseReceiptAsync(PosOrganizationId organizationId, DirectPurchaseReceiptId receiptId, CatalogProductId productId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
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

    private sealed class FakeProducts(IReadOnlyList<CatalogProduct> items) : ICatalogProductRepository
    {
        public Task<IReadOnlyList<CatalogProduct>> ListByIdsAsync(
            PosOrganizationId organizationId,
            IReadOnlyCollection<CatalogProductId> productIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CatalogProduct>>(
                items.Where(p => productIds.Contains(p.Id)).ToList());

        public Task<CatalogProduct?> GetByIdAsync(PosOrganizationId organizationId, CatalogProductId productId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<CatalogProduct?> FindByNormalizedSkuAsync(PosOrganizationId organizationId, string normalizedSku, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<CatalogProduct?> FindByBarcodeAsync(PosOrganizationId organizationId, string barcode, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<(IReadOnlyList<CatalogProduct> Items, int TotalCount)> ListAsync(PosOrganizationId organizationId, CatalogProductFilter filter, int skip, int take, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<Guid>> ListIdsAsync(PosOrganizationId organizationId, CatalogProductFilter filter, int skip, int take, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<(int TotalCount, int AvailableCount, int NotAvailableCount)> CountConnectedBuyerAvailabilityAsync(PosOrganizationId organizationId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<(Guid? CategoryId, int Count)>> ListConnectedBuyerAvailabilityCategoryFacetsAsync(PosOrganizationId organizationId, CatalogProductFilter filter, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<CatalogProduct?> FindByPlatformGlobalProductIdAsync(PosOrganizationId organizationId, Guid platformGlobalProductId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlySet<Guid>> ListPlatformGlobalProductIdsAsync(PosOrganizationId organizationId, IReadOnlyCollection<Guid> platformGlobalProductIds, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task AddAsync(CatalogProduct product, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task UpdateAsync(CatalogProduct product, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private sealed class FakeReturns(IReadOnlyList<SaleReturn>? items = null) : ISaleReturnRepository
    {
        public Task<IReadOnlyList<SaleReturn>> ListBySaleIdAsync(
            PosOrganizationId organizationId,
            SaleId saleId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(items ?? Array.Empty<SaleReturn>());

        public Task<SaleReturn?> GetByIdAsync(PosOrganizationId organizationId, SaleReturnId returnId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<(IReadOnlyList<SaleReturn> Items, int TotalCount)> ListAsync(PosOrganizationId organizationId, SaleReturnFilter filter, int skip, int take, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> HasReturnsForSaleAsync(PosOrganizationId organizationId, SaleId saleId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyDictionary<Guid, SaleLineReturnTotals>> GetPriorTotalsBySaleLineAsync(PosOrganizationId organizationId, SaleId saleId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<decimal> SumCashRefundsForShiftAsync(PosOrganizationId organizationId, Guid cashierShiftId, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<SaleReturnCogsPeriodAggregate> AggregateReturnCogsForPeriodAsync(
            PosOrganizationId organizationId,
            DateOnly fromDateUtc,
            DateOnly toDateUtc,
            Guid? branchId = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new SaleReturnCogsPeriodAggregate(0m, false));

        public Task<IReadOnlyList<ProductProfitabilityReturnAggregate>> AggregateProductProfitabilityReturnsAsync(
            PosOrganizationId organizationId,
            DateOnly fromDateUtc,
            DateOnly toDateUtc,
            Guid? branchId = null,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();


        public Task<decimal> SumRefundsForPeriodAsync(
            PosOrganizationId organizationId,
            DateOnly fromDateUtc,
            DateOnly toDateUtc,
            Guid? branchId = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(0m);

        public Task<SaleReturn> CreateAsync(PosOrganizationId organizationId, DateOnly businessDateUtc, Func<string, SaleReturn> createReturn, Func<SaleReturn, CancellationToken, Task>? afterReturnCreated = null, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private sealed class FakeLots : IInventoryLotRepository
    {
        public List<InventoryLotMovement> Movements { get; } = [];

        public Task<InventoryLot?> GetByIdAsync(PosOrganizationId organizationId, InventoryLotId lotId, CancellationToken cancellationToken = default) => Task.FromResult<InventoryLot?>(null);
        public Task<InventoryLot?> FindAsync(PosOrganizationId organizationId, CatalogProductId productId, DateOnly expirationDate, string normalizedLotNumber, PosBranchId? branchId, CancellationToken cancellationToken = default) => Task.FromResult<InventoryLot?>(null);
        public Task<IReadOnlyList<InventoryLot>> ListOnHandAsync(PosOrganizationId organizationId, CatalogProductId productId, PosBranchId? branchId, bool includeDepleted, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<InventoryLot>>([]);
        public Task<(IReadOnlyList<InventoryLot> Items, int TotalCount)> ListPagedAsync(PosOrganizationId organizationId, CatalogProductId productId, PosBranchId? branchId, bool includeDepleted, int skip, int take, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<(IReadOnlyList<InventoryLot> Items, int TotalCount)> ListExpiringPagedAsync(PosOrganizationId organizationId, PosBranchId? branchId, DateOnly expireOnOrBefore, DateOnly? expireOnOrAfter, string? search, int skip, int take, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<(int ExpiredCount, int NearExpiryCount)> CountExpiryAsync(PosOrganizationId organizationId, DateOnly today, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task AdoptOrgLevelLotsForBranchAsync(PosOrganizationId organizationId, CatalogProductId productId, PosBranchId branchId, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task AddAsync(InventoryLot lot, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task UpdateAsync(InventoryLot lot, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task AddMovementAsync(InventoryLotMovement movement, CancellationToken cancellationToken = default)
        {
            Movements.Add(movement);
            return Task.CompletedTask;
        }

        public Task<bool> HasMovementAsync(PosOrganizationId organizationId, Guid sourceId, InventoryLotId lotId, StockMovementType movementType, CancellationToken cancellationToken = default) =>
            Task.FromResult(Movements.Any(m => m.SourceId == sourceId && m.LotId == lotId && m.MovementType == movementType));

        public Task<IReadOnlyList<InventoryLotMovement>> ListBySourceAsync(PosOrganizationId organizationId, Guid sourceId, StockMovementType movementType, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<InventoryLotMovement>>(
                Movements.Where(m => m.SourceId == sourceId && m.MovementType == movementType).ToList());
    }

    private sealed class FakeBalances : IInventoryBranchBalanceRepository
    {
        public List<InventoryBranchBalance> Items { get; } = [];
        public List<InventoryBranchBalance> Upserts { get; } = [];

        public Task<InventoryBranchBalance?> GetAsync(PosOrganizationId organizationId, PosBranchId branchId, CatalogProductId productId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.FirstOrDefault(b => b.BranchId == branchId && b.ProductId == productId));

        public Task<IReadOnlyList<InventoryBranchBalance>> ListByProductIdsAsync(PosOrganizationId organizationId, IReadOnlyCollection<CatalogProductId> productIds, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<InventoryBranchBalance>>(Items.Where(b => productIds.Contains(b.ProductId)).ToList());

        public Task UpsertAsync(InventoryBranchBalance balance, CancellationToken cancellationToken = default)
        {
            Upserts.Add(balance);
            return Task.CompletedTask;
        }
    }
}
