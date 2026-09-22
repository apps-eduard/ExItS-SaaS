using ExItS.PinoyBusinessPOS.Application.CashierShifts;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Credit;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Application.Payments;
using ExItS.PinoyBusinessPOS.Application.Returns;
using ExItS.PinoyBusinessPOS.Application.Sales;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.CashierShifts;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Credit;
using ExItS.PinoyBusinessPOS.Domain.CustomerOrdering;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Purchasing;
using ExItS.PinoyBusinessPOS.Domain.Returns;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.UnitTests.Returns;

public sealed class FinalizeReturnBatchUseCaseTests
{
    private static readonly PosOrganizationId Org = PosOrganizationId.From(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
    private static readonly Guid Actor = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly DateTimeOffset Now = new(2026, 9, 18, 17, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Split_disposition_reduces_pending_and_restocks_sellable_with_full_refund()
    {
        var fixture = BuildFixture(SalePaymentMethod.ManualGCash, acceptedQty: 10m, lineTotal: 80m);
        fixture.Batch.ClassifyLine(fixture.Batch.Lines[0].Id, 3m, 7m, "inspection", Actor, Now);

        var result = await fixture.UseCase.ExecuteAsync(
            Org.Value,
            fixture.Batch.Id.Value,
            fixture.Batch.UpdatedAtUtc,
            Actor);

        Assert.True(result.IsSuccess);
        Assert.Equal(23m, fixture.Account.OnHandQuantity);
        Assert.Equal(0m, fixture.Account.PendingReturnQuantity);
        Assert.Equal(80m, fixture.LastSaleReturn!.TotalRefundAmount);
    }

    [Fact]
    public async Task All_sellable_increases_on_hand_by_full_accepted_quantity()
    {
        var fixture = BuildFixture(SalePaymentMethod.ManualGCash, acceptedQty: 10m, lineTotal: 100m);
        fixture.Batch.ClassifyLine(fixture.Batch.Lines[0].Id, 10m, 0m, null, Actor, Now);

        var result = await fixture.UseCase.ExecuteAsync(
            Org.Value,
            fixture.Batch.Id.Value,
            fixture.Batch.UpdatedAtUtc,
            Actor);

        Assert.True(result.IsSuccess);
        Assert.Equal(30m, fixture.Account.OnHandQuantity);
        Assert.Equal(0m, fixture.Account.PendingReturnQuantity);
    }

    [Fact]
    public async Task All_damaged_keeps_on_hand_unchanged()
    {
        var fixture = BuildFixture(SalePaymentMethod.ManualGCash, acceptedQty: 10m, lineTotal: 100m);
        fixture.Batch.ClassifyLine(fixture.Batch.Lines[0].Id, 0m, 10m, null, Actor, Now);

        var result = await fixture.UseCase.ExecuteAsync(
            Org.Value,
            fixture.Batch.Id.Value,
            fixture.Batch.UpdatedAtUtc,
            Actor);

        Assert.True(result.IsSuccess);
        Assert.Equal(20m, fixture.Account.OnHandQuantity);
        Assert.Equal(0m, fixture.Account.PendingReturnQuantity);
    }

    [Fact]
    public async Task Double_finalize_is_idempotent()
    {
        var fixture = BuildFixture(SalePaymentMethod.ManualGCash, acceptedQty: 10m, lineTotal: 100m);
        fixture.Batch.ClassifyLine(fixture.Batch.Lines[0].Id, 10m, 0m, null, Actor, Now);

        var first = await fixture.UseCase.ExecuteAsync(
            Org.Value,
            fixture.Batch.Id.Value,
            fixture.Batch.UpdatedAtUtc,
            Actor);
        Assert.True(first.IsSuccess);
        var firstSaleReturnId = fixture.Batch.SaleReturnId;

        var second = await fixture.UseCase.ExecuteAsync(
            Org.Value,
            fixture.Batch.Id.Value,
            fixture.Batch.UpdatedAtUtc,
            Actor);

        Assert.True(second.IsSuccess);
        Assert.Equal(firstSaleReturnId, fixture.Batch.SaleReturnId);
        Assert.Equal(30m, fixture.Account.OnHandQuantity);
    }

    [Fact]
    public async Task Cannot_finalize_until_all_lines_classified()
    {
        var fixture = BuildFixture(SalePaymentMethod.ManualGCash, acceptedQty: 10m, lineTotal: 100m);
        var result = await fixture.UseCase.ExecuteAsync(
            Org.Value,
            fixture.Batch.Id.Value,
            fixture.Batch.UpdatedAtUtc,
            Actor);

        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrorCodes.ReturnBatchNotReadyForFinalize, result.ErrorCode);
    }

    [Fact]
    public async Task Fully_paid_manual_payment_sets_refund_due()
    {
        var fixture = BuildFixture(SalePaymentMethod.ManualGCash, acceptedQty: 3m, lineTotal: 30m);
        fixture.Batch.ClassifyLine(fixture.Batch.Lines[0].Id, 3m, 0m, null, Actor, Now);

        var result = await fixture.UseCase.ExecuteAsync(Org.Value, fixture.Batch.Id.Value, fixture.Batch.UpdatedAtUtc, Actor);

        Assert.True(result.IsSuccess);
        Assert.Equal(ReturnBatchRefundStatus.RefundDue, fixture.Batch.RefundStatus);
        Assert.Equal(30m, fixture.Batch.RefundDueAmount);
        Assert.Equal(0m, fixture.Batch.RefundedAmount);
    }

    [Fact]
    public async Task Pending_check_sets_obligation_reduced()
    {
        var fixture = BuildFixture(
            SalePaymentMethod.Check,
            acceptedQty: 3m,
            lineTotal: 30m,
            checkSettlementStatus: CheckSettlementStatus.Pending);
        fixture.Batch.ClassifyLine(fixture.Batch.Lines[0].Id, 3m, 0m, null, Actor, Now);

        var result = await fixture.UseCase.ExecuteAsync(Org.Value, fixture.Batch.Id.Value, fixture.Batch.UpdatedAtUtc, Actor);

        Assert.True(result.IsSuccess);
        Assert.Equal(ReturnBatchRefundStatus.ObligationReduced, fixture.Batch.RefundStatus);
        Assert.Equal(0m, fixture.Batch.RefundDueAmount);
        Assert.Equal(0m, fixture.Batch.RefundedAmount);
    }

    [Fact]
    public async Task Cleared_check_sets_refund_due()
    {
        var fixture = BuildFixture(
            SalePaymentMethod.Check,
            acceptedQty: 3m,
            lineTotal: 30m,
            checkSettlementStatus: CheckSettlementStatus.Cleared);
        fixture.Batch.ClassifyLine(fixture.Batch.Lines[0].Id, 3m, 0m, null, Actor, Now);

        var result = await fixture.UseCase.ExecuteAsync(Org.Value, fixture.Batch.Id.Value, fixture.Batch.UpdatedAtUtc, Actor);

        Assert.True(result.IsSuccess);
        Assert.Equal(ReturnBatchRefundStatus.RefundDue, fixture.Batch.RefundStatus);
        Assert.Equal(30m, fixture.Batch.RefundDueAmount);
        Assert.Equal(0m, fixture.Batch.RefundedAmount);
    }

    [Fact]
    public async Task Utang_sets_credit_reduced_status()
    {
        var fixture = BuildFixture(SalePaymentMethod.Utang, acceptedQty: 3m, lineTotal: 30m);
        fixture.Batch.ClassifyLine(fixture.Batch.Lines[0].Id, 3m, 0m, null, Actor, Now);

        var result = await fixture.UseCase.ExecuteAsync(Org.Value, fixture.Batch.Id.Value, fixture.Batch.UpdatedAtUtc, Actor);

        Assert.True(result.IsSuccess);
        Assert.Equal(ReturnBatchRefundStatus.CreditReduced, fixture.Batch.RefundStatus);
    }

    private static FinalizeFixture BuildFixture(
        SalePaymentMethod paymentMethod,
        decimal acceptedQty,
        decimal lineTotal,
        CheckSettlementStatus? checkSettlementStatus = null)
    {
        var saleHolder = BuildCompletedSale(paymentMethod, quantity: acceptedQty, lineTotal, checkSettlementStatus);
        var sale = saleHolder.DomainSale;
        var account = InventoryAccount.CreateUntracked(Org, sale.Lines[0].ProductId, Now);
        account.Enable(20m, UnitOfMeasure.Piece, Actor, Now, hasOpeningStockAlready: false);
        account.IncreasePendingReturn(acceptedQty);
        account.Touch(Now);

        var batch = ReturnBatch.CreateAccepted(
            Org,
            "260918-001",
            sale,
            [new ReturnBatchAcceptedLineDraft(sale.Lines[0].Id, acceptedQty)],
            new Dictionary<Guid, (decimal, decimal)>(),
            "customer return",
            Actor,
            Now);

        var batchRepo = new InMemoryBatchRepository(batch);
        var salesRepo = new InMemorySaleRepository(saleHolder);
        var saleReturnRepo = new InMemorySaleReturnRepository();
        var inventoryRepo = new InMemoryInventoryRepository(account);
        var returnStock = new FakeReturnStockService(account);
        var clock = new FixedClock(Now);
        var creditRepo = new NoOpCreditRepository();
        var outstanding = new NoOpOutstandingBalanceService();
        if (paymentMethod == SalePaymentMethod.Utang)
        {
            creditRepo.Entry = BuildLinkedCreditEntry(saleHolder.DomainSale);
            outstanding.Outstanding = acceptedQty * 10m;
        }

        var useCase = new FinalizeReturnBatch(
            batchRepo,
            salesRepo,
            new NoOpSaleMutationLock(),
            saleReturnRepo,
            returnStock,
            inventoryRepo,
            new NoOpShiftRepository(),
            creditRepo,
            outstanding,
            new PassthroughUnitOfWork(),
            clock,
            branchBalances: null);
        return new FinalizeFixture(useCase, batch, account, saleReturnRepo, saleHolder, creditRepo, outstanding);
    }

    private static SaleHolder BuildCompletedSale(
        SalePaymentMethod paymentMethod,
        decimal quantity,
        decimal lineTotal,
        CheckSettlementStatus? checkSettlementStatus)
    {
        var saleId = SaleId.New();
        var customerId = paymentMethod == SalePaymentMethod.Utang ? POSCustomerId.New() : null;
        var linkedCreditId = paymentMethod == SalePaymentMethod.Utang ? CreditEntryId.New() : null;
        var line = SaleLine.Rehydrate(
            SaleLineId.New(),
            saleId,
            Org,
            CatalogProductId.New(),
            lineNumber: 1,
            "Milk",
            "MILK-1",
            null,
            UnitOfMeasure.Piece,
            unitPrice: 10m,
            quantity,
            lineTotal,
            grossLineTotal: quantity * 10m,
            lineDiscountAmount: (quantity * 10m) - lineTotal);

        var sale = Sale.Rehydrate(
            saleId,
            Org,
            "260918-100",
            SaleStatus.Completed,
            paymentMethod,
            lineTotal,
            lineTotal,
            0m,
            lineTotal,
            0m,
            null,
            Now,
            Actor,
            null,
            null,
            null,
            Now,
            [line],
            customerId,
            linkedCreditId,
            checkSettlementStatus: checkSettlementStatus);
        return new SaleHolder(sale);
    }

    private static CreditEntry BuildLinkedCreditEntry(Sale sale)
    {
        return CreditEntry.Create(
            Org,
            sale.CustomerId!,
            sale.Total,
            "utang sale",
            Now,
            id: sale.LinkedCreditEntryId,
            sourceSaleId: sale.Id);
    }

    private sealed record FinalizeFixture(
        FinalizeReturnBatch UseCase,
        ReturnBatch Batch,
        InventoryAccount Account,
        InMemorySaleReturnRepository Returns,
        SaleHolder Sale,
        NoOpCreditRepository CreditRepo,
        NoOpOutstandingBalanceService Outstanding)
    {
        public SaleReturn? LastSaleReturn => Returns.LastCreated;
    }

    private sealed class SaleHolder(Sale sale)
    {
        public Sale DomainSale { get; private set; } = sale;
    }

    private sealed class InMemoryBatchRepository(ReturnBatch batch) : IReturnBatchRepository
    {
        public Task<ReturnBatch?> GetByIdAsync(PosOrganizationId organizationId, ReturnBatchId returnBatchId, CancellationToken cancellationToken = default) =>
            Task.FromResult<ReturnBatch?>(batch.Id == returnBatchId ? batch : null);

        public Task<IReadOnlyList<ReturnBatch>> ListBySaleIdAsync(PosOrganizationId organizationId, SaleId saleId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ReturnBatch>>([batch]);

        public Task<IReadOnlyList<ReturnBatch>> ListByPurchaseOrderIdAsync(PosOrganizationId organizationId, PurchaseOrderId purchaseOrderId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ReturnBatch>>([]);

        public Task<IReadOnlyList<ReturnBatch>> ListByConnectedPurchaseOrderIdAsync(PosOrganizationId organizationId, ConnectedPurchaseOrderId connectedPurchaseOrderId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ReturnBatch>>([]);

        public Task<IReadOnlyList<ReturnBatch>> ListOpenConnectedForSellerAsync(PosOrganizationId organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ReturnBatch>>([]);

        public Task<ReturnBatch> CreateAsync(PosOrganizationId organizationId, DateOnly businessDateUtc, Func<string, ReturnBatch> createBatch, Func<ReturnBatch, CancellationToken, Task>? afterCreated = null, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task UpdateAsync(ReturnBatch updated, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class InMemorySaleRepository(SaleHolder sale) : ISaleRepository
    {
        public Task<Sale?> GetByIdAsync(PosOrganizationId organizationId, SaleId saleId, CancellationToken cancellationToken = default) =>
            Task.FromResult<Sale?>(sale.DomainSale.Id == saleId ? sale.DomainSale : null);

        public Task<Sale?> FindBySaleNumberAsync(PosOrganizationId organizationId, string saleNumber, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(IReadOnlyList<Sale> Items, int TotalCount)> ListAsync(PosOrganizationId organizationId, SaleFilter filter, int skip, int take, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<Sale>> ListForReportAsync(PosOrganizationId organizationId, DateOnly fromDateUtc, DateOnly toDateUtc, SaleStatus? status = null, SalePaymentMethod? paymentMethod = null, Guid? productId = null, Guid? customerId = null, Guid? branchId = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlySet<Guid>> ListSaleIdsInBranchAsync(PosOrganizationId organizationId, IReadOnlyCollection<Guid> saleIds, Guid branchId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<SalePeriodAggregate> AggregatePeriodAsync(PosOrganizationId organizationId, DateOnly fromDateUtc, DateOnly toDateUtc, SaleStatus? status = null, SalePaymentMethod? paymentMethod = null, Guid? customerId = null, Guid? branchId = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<SalePeriodAggregate> AggregateAsync(PosOrganizationId organizationId, SaleFilter filter, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<SalePaymentAggregate>> AggregateCompletedByPaymentAsync(PosOrganizationId organizationId, DateOnly fromDateUtc, DateOnly toDateUtc, Guid? branchId = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<SaleDailyAggregate>> AggregateCompletedByDayAsync(PosOrganizationId organizationId, DateOnly fromDateUtc, DateOnly toDateUtc, Guid? branchId = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<SaleCostPeriodAggregate> AggregateCostForProfitabilityAsync(PosOrganizationId organizationId, DateOnly fromDateUtc, DateOnly toDateUtc, Guid? branchId = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<ProductProfitabilitySaleAggregate>> AggregateProductProfitabilitySalesAsync(PosOrganizationId organizationId, DateOnly fromDateUtc, DateOnly toDateUtc, Guid? branchId = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Sale> CheckoutAsync(PosOrganizationId organizationId, DateOnly businessDateUtc, Func<string, Sale> createSale, Func<Sale, CancellationToken, Task>? afterSaleCreated = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task UpdateAsync(Sale sale, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task AddAsync(Sale sale, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<string> ReserveNextSaleNumberAsync(PosOrganizationId organizationId, DateOnly businessDateUtc, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> HasReturnsForSaleAsync(PosOrganizationId organizationId, SaleId saleId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class InMemorySaleReturnRepository : ISaleReturnRepository
    {
        public SaleReturn? LastCreated { get; private set; }

        public Task<SaleReturn?> GetByIdAsync(PosOrganizationId organizationId, SaleReturnId returnId, CancellationToken cancellationToken = default) => Task.FromResult<SaleReturn?>(null);
        public Task<(IReadOnlyList<SaleReturn> Items, int TotalCount)> ListAsync(PosOrganizationId organizationId, SaleReturnFilter filter, int skip, int take, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<SaleReturn>> ListBySaleIdAsync(PosOrganizationId organizationId, SaleId saleId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<SaleReturn>>([]);
        public Task<bool> HasReturnsForSaleAsync(PosOrganizationId organizationId, SaleId saleId, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<IReadOnlyDictionary<Guid, SaleLineReturnTotals>> GetPriorTotalsBySaleLineAsync(PosOrganizationId organizationId, SaleId saleId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyDictionary<Guid, SaleLineReturnTotals>>(new Dictionary<Guid, SaleLineReturnTotals>());
        public Task<decimal> SumCashRefundsForShiftAsync(PosOrganizationId organizationId, Guid cashierShiftId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<SaleReturnCogsPeriodAggregate> AggregateReturnCogsForPeriodAsync(PosOrganizationId organizationId, DateOnly fromDateUtc, DateOnly toDateUtc, Guid? branchId = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<ProductProfitabilityReturnAggregate>> AggregateProductProfitabilityReturnsAsync(PosOrganizationId organizationId, DateOnly fromDateUtc, DateOnly toDateUtc, Guid? branchId = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<decimal> SumRefundsForPeriodAsync(PosOrganizationId organizationId, DateOnly fromDateUtc, DateOnly toDateUtc, Guid? branchId = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public async Task<SaleReturn> CreateAsync(
            PosOrganizationId organizationId,
            DateOnly businessDateUtc,
            Func<string, SaleReturn> createReturn,
            Func<SaleReturn, CancellationToken, Task>? afterReturnCreated = null,
            CancellationToken cancellationToken = default)
        {
            LastCreated = createReturn("260918-001");
            if (afterReturnCreated is not null)
            {
                await afterReturnCreated(LastCreated, cancellationToken).ConfigureAwait(false);
            }

            return LastCreated;
        }
    }

    private sealed class InMemoryInventoryRepository(InventoryAccount account) : IInventoryRepository
    {
        public Task<InventoryAccount?> GetByProductIdAsync(PosOrganizationId organizationId, CatalogProductId productId, CancellationToken cancellationToken = default) => Task.FromResult<InventoryAccount?>(account.ProductId == productId ? account : null);
        public Task<IReadOnlyList<InventoryAccount>> ListByProductIdsAsync(PosOrganizationId organizationId, IReadOnlyCollection<CatalogProductId> productIds, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<InventoryAccount>>([account]);
        public Task<(IReadOnlyList<InventoryAccount> Items, int TotalCount)> ListAsync(PosOrganizationId organizationId, InventoryAccountFilter filter, int skip, int take, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(IReadOnlyList<InventoryAccount> Items, int TotalCount)> ListLowStockAsync(PosOrganizationId organizationId, string? search, int skip, int take, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<InventoryAccount>> ListAllAccountsAsync(PosOrganizationId organizationId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task AddAccountAsync(InventoryAccount account, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task UpdateAccountAsync(InventoryAccount account, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task ExecuteWithProductReservationLocksAsync(PosOrganizationId organizationId, IReadOnlyCollection<CatalogProductId> productIds, Func<IReadOnlyList<InventoryAccount>, CancellationToken, Task> action, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task AddMovementAsync(StockMovement movement, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<StockMovement?> GetMovementByIdAsync(PosOrganizationId organizationId, StockMovementId movementId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> HasAnyMovementAsync(PosOrganizationId organizationId, CatalogProductId productId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> HasOpeningStockAsync(PosOrganizationId organizationId, CatalogProductId productId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(IReadOnlyList<StockMovement> Items, int TotalCount)> ListMovementsAsync(PosOrganizationId organizationId, CatalogProductId productId, StockMovementFilter filter, int skip, int take, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<decimal> SumMovementEffectsAsync(PosOrganizationId organizationId, CatalogProductId productId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(IReadOnlyList<InventoryAccount> Items, int TotalCount)> ListReorderSuggestionsAsync(PosOrganizationId organizationId, string? search, int skip, int take, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> HasStockCountVarianceAsync(PosOrganizationId organizationId, StockCountId stockCountId, CatalogProductId productId, StockMovementType movementType, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<StockMovement>> ListMovementsForReportAsync(PosOrganizationId organizationId, DateOnly fromDateUtc, DateOnly toDateUtc, Guid? branchId = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<StockMovement>> ListSaleDeductionsAsync(PosOrganizationId organizationId, SaleId saleId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> HasSaleDeductionAsync(PosOrganizationId organizationId, SaleId saleId, CatalogProductId productId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> HasCustomerOrderDeductionAsync(PosOrganizationId organizationId, CustomerOrderId orderId, CatalogProductId productId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> HasSaleVoidRestorationAsync(PosOrganizationId organizationId, SaleId saleId, CatalogProductId productId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> HasPurchaseReceiptAsync(PosOrganizationId organizationId, GoodsReceiptId goodsReceiptId, CatalogProductId productId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> HasDirectPurchaseReceiptAsync(PosOrganizationId organizationId, DirectPurchaseReceiptId receiptId, CatalogProductId productId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> HasStockUseAsync(PosOrganizationId organizationId, Domain.Inventory.StockUseId stockUseId, CatalogProductId productId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> HasStockUseVoidRestorationAsync(PosOrganizationId organizationId, Domain.Inventory.StockUseId stockUseId, CatalogProductId productId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> HasProductionMaterialConsumptionAsync(PosOrganizationId organizationId, Domain.Inventory.ProductionRunId productionRunId, CatalogProductId productId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> HasProductionMaterialRestorationAsync(PosOrganizationId organizationId, Domain.Inventory.ProductionRunId productionRunId, CatalogProductId productId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> HasProductionOutputAsync(PosOrganizationId organizationId, Domain.Inventory.ProductionRunId productionRunId, CatalogProductId productId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> HasProductionOutputReversalAsync(PosOrganizationId organizationId, Domain.Inventory.ProductionRunId productionRunId, CatalogProductId productId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> HasWasteLossAsync(PosOrganizationId organizationId, Domain.Inventory.WasteLossId wasteLossId, CatalogProductId productId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> HasWasteLossVoidRestorationAsync(PosOrganizationId organizationId, Domain.Inventory.WasteLossId wasteLossId, CatalogProductId productId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> HasPurchaseReceiptReversalAsync(PosOrganizationId organizationId, GoodsReceiptId goodsReceiptId, CatalogProductId productId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> HasDirectPurchaseReceiptReversalAsync(PosOrganizationId organizationId, DirectPurchaseReceiptId receiptId, CatalogProductId productId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> HasConnectedPurchaseFulfillmentAsync(PosOrganizationId organizationId, ConnectedPurchaseOrderId connectedPurchaseOrderId, CatalogProductId productId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<decimal?> GetLatestAcquisitionUnitCostAsync(PosOrganizationId organizationId, CatalogProductId productId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyDictionary<Guid, decimal?>> GetLatestAcquisitionUnitCostsAsync(PosOrganizationId organizationId, IReadOnlyCollection<CatalogProductId> productIds, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> HasSaleReturnRestockAsync(PosOrganizationId organizationId, SaleReturnId saleReturnId, CatalogProductId productId, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<bool> HasSaleReturnWriteOffAsync(PosOrganizationId organizationId, SaleReturnId saleReturnId, CatalogProductId productId, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<bool> HasInventoryTransferMovementAsync(PosOrganizationId organizationId, Domain.Inventory.InventoryTransferId transferId, CatalogProductId productId, StockMovementType movementType, InventoryLotId? lotId = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(DateTimeOffset? LatestAt, int Count)> GetMovementSummaryAsync(PosOrganizationId organizationId, CatalogProductId productId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyDictionary<Guid, (DateTimeOffset? LatestAt, int Count)>> GetMovementSummariesAsync(PosOrganizationId organizationId, IReadOnlyCollection<CatalogProductId> productIds, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class FakeReturnStockService(InventoryAccount account) : ISaleReturnStockService
    {
        public Task RestockForReturnAsync(
            PosOrganizationId organizationId,
            SaleReturn saleReturn,
            Sale originalSale,
            Guid actorId,
            DateTimeOffset utcNow,
            CancellationToken cancellationToken = default)
        {
            var sellable = saleReturn.Lines.Sum(l => l.SellableQuantity);
            if (sellable > 0m)
            {
                account.ApplyMovementEffect(sellable);
                account.Touch(utcNow);
            }

            return Task.CompletedTask;
        }
    }

    private sealed class NoOpSaleMutationLock : ISaleMutationLock
    {
        public Task AcquireAsync(PosOrganizationId organizationId, SaleId saleId, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class NoOpShiftRepository : ICashierShiftRepository
    {
        public Task<CashierShift?> GetByIdAsync(PosOrganizationId organizationId, CashierShiftId shiftId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> HasOpenShiftForActorAsync(PosOrganizationId organizationId, Guid actorId, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<CashierShift?> FindOpenForActorAsync(PosOrganizationId organizationId, Guid actorId, CancellationToken cancellationToken = default) => Task.FromResult<CashierShift?>(null);
        public Task<CashierShift?> FindOpenForRegisterAsync(PosOrganizationId organizationId, Guid registerId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(IReadOnlyList<CashierShift> Items, int TotalCount)> ListAsync(PosOrganizationId organizationId, CashierShiftFilter filter, int skip, int take, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<CashierShift> OpenAsync(PosOrganizationId organizationId, DateOnly businessDateUtc, Guid actorId, decimal openingCashAmount, Guid openedBy, Func<string, CashierShift> createShift, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task UpdateAsync(CashierShift shift, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task AddMovementAsync(CashierShiftMovement movement, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<CashierShiftMovement?> GetMovementByIdAsync(PosOrganizationId organizationId, CashierShiftMovementId movementId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<CashierShiftMovement>> ListMovementsAsync(PosOrganizationId organizationId, CashierShiftId shiftId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> HasLinkedSalesAsync(PosOrganizationId organizationId, CashierShiftId shiftId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<CashierShiftSalesTotals> GetSalesTotalsAsync(PosOrganizationId organizationId, CashierShiftId shiftId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyDictionary<Guid, CashierShiftCompletedSalesRollup>> GetCompletedSalesRollupsAsync(PosOrganizationId organizationId, IReadOnlyCollection<Guid> shiftIds, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class NoOpCreditRepository : ICreditEntryRepository
    {
        public CreditEntry? Entry { get; set; }

        public Task<CreditEntry?> GetByIdAsync(PosOrganizationId organizationId, POSCustomerId customerId, CreditEntryId entryId, CancellationToken cancellationToken = default) =>
            Task.FromResult(
                Entry is not null
                && Entry.OrganizationId == organizationId
                && Entry.CustomerId == customerId
                && Entry.Id == entryId
                    ? Entry
                    : null);

        public Task<CreditEntry?> GetByIdForOrganizationAsync(PosOrganizationId organizationId, CreditEntryId entryId, CancellationToken cancellationToken = default) =>
            Task.FromResult(
                Entry is not null
                && Entry.OrganizationId == organizationId
                && Entry.Id == entryId
                    ? Entry
                    : null);
        public Task<(IReadOnlyList<CreditEntry> Items, int TotalCount)> ListByCustomerAsync(PosOrganizationId organizationId, POSCustomerId customerId, int skip, int take, CancellationToken cancellationToken = default, IReadOnlySet<Guid>? historyBranchIds = null) => throw new NotSupportedException();
        public Task<(IReadOnlyList<CreditEntry> Items, int TotalCount)> ListCreatedSinceAsync(PosOrganizationId organizationId, DateTimeOffset? sinceUtc, int skip, int take, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<CreditEntry>> ListActiveByOrganizationAsync(PosOrganizationId organizationId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<CreditEntry>> ListRecordedInRangeAsync(PosOrganizationId organizationId, DateOnly fromDateUtc, DateOnly toDateUtc, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<decimal> SumActiveAmountAsync(PosOrganizationId organizationId, POSCustomerId customerId, CancellationToken cancellationToken = default, IReadOnlySet<Guid>? historyBranchIds = null) => throw new NotSupportedException();
        public Task<int> CountActiveAsync(PosOrganizationId organizationId, POSCustomerId customerId, CancellationToken cancellationToken = default, IReadOnlySet<Guid>? historyBranchIds = null) => throw new NotSupportedException();
        public Task AddAsync(CreditEntry entry, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task UpdateAsync(CreditEntry entry, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class NoOpOutstandingBalanceService : IOutstandingBalanceService
    {
        public decimal Outstanding { get; set; }
        public Task<decimal> GetOutstandingAsync(PosOrganizationId organizationId, POSCustomerId customerId, CancellationToken cancellationToken = default) => Task.FromResult(Outstanding);
        public Task<IReadOnlyDictionary<Guid, decimal>> GetOutstandingBatchAsync(PosOrganizationId organizationId, IReadOnlyCollection<Guid> customerIds, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<CustomerUtangSummaryDto> GetSummaryAsync(Guid organizationId, Guid customerId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class PassthroughUnitOfWork : IPosUnitOfWork
    {
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<T> ExecuteInSerializableTransactionAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken = default) =>
            action(cancellationToken);
    }

    private sealed class FixedClock(DateTimeOffset now) : IClock
    {
        public DateTimeOffset UtcNow => now;
    }
}
