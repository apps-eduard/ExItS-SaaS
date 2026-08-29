using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Application.Reporting;
using ExItS.PinoyBusinessPOS.Application.Returns;
using ExItS.PinoyBusinessPOS.Application.Sales;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Returns;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.UnitTests.Reporting;

/// <summary>
/// Discount reporting hardening: pre-discount aggregates, no double-subtraction of DiscountTotal,
/// NetSales = CompletedTotal − Refunds (voided already excluded from Completed).
/// </summary>
public sealed class DiscountReportingHardeningTests
{
    private static readonly Guid OrgGuid = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly PosOrganizationId Org = PosOrganizationId.From(OrgGuid);
    private static readonly DateTimeOffset Now = new(2026, 8, 15, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Sales_summary_exposes_pre_discount_and_discount_without_double_subtraction()
    {
        var period = new SalePeriodAggregate(
            CompletedTotal: 900m,
            CompletedCount: 1,
            VoidedTotal: 0m,
            VoidedCount: 0,
            CashTotal: 900m,
            ManualGCashTotal: 0m,
            UtangTotal: 0m,
            UtangCount: 0,
            CompletedGrossSubtotal: 1000m,
            CompletedDiscountTotal: 100m,
            CompletedNetSubtotal: 900m,
            CompletedTaxAmount: 0m);

        var service = CreateOperational(period);
        var result = await service.GetSalesSummaryAsync(OrgGuid, new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31));

        Assert.True(result.IsSuccess);
        var dto = result.Value!;
        Assert.Equal(1000m, dto.PreDiscountGrossSales);
        Assert.Equal(100m, dto.CommercialDiscountTotal);
        Assert.Equal(900m, dto.NetSubtotal);
        Assert.Equal(900m, dto.CompletedGrossSales);
        Assert.Equal(900m, dto.NetSales);
        Assert.NotEqual(800m, dto.NetSales);
    }

    [Fact]
    public async Task Sales_summary_net_sales_does_not_subtract_voided_again()
    {
        var period = new SalePeriodAggregate(
            CompletedTotal: 500m,
            CompletedCount: 2,
            VoidedTotal: 200m,
            VoidedCount: 1,
            CashTotal: 500m,
            ManualGCashTotal: 0m,
            UtangTotal: 0m,
            UtangCount: 0,
            CompletedGrossSubtotal: 550m,
            CompletedDiscountTotal: 50m,
            CompletedNetSubtotal: 500m,
            CompletedTaxAmount: 0m,
            VoidedDiscountTotal: 20m);

        var service = CreateOperational(period);
        var result = await service.GetSalesSummaryAsync(OrgGuid, new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31));

        Assert.Equal(500m, result.Value!.NetSales);
        Assert.Equal(200m, result.Value.VoidedSales);
        Assert.Equal(50m, result.Value.CommercialDiscountTotal);
    }

    [Fact]
    public async Task Profitability_keeps_post_discount_net_sales_and_exposes_discount()
    {
        var service = new ProfitabilityReportService(
            new FakeSaleRepo
            {
                Period = new SalePeriodAggregate(
                    900m, 1, 0m, 0, 900m, 0m, 0m, 0,
                    CompletedGrossSubtotal: 1000m,
                    CompletedDiscountTotal: 100m,
                    CompletedNetSubtotal: 900m),
                Costs = new SaleCostPeriodAggregate(1, 1, 0, 0, 600m),
            },
            new FakeReturnRepo(),
            new FakeWasteRepo(),
            new FakeStockUseRepo(),
            new FixedClock(Now));

        var result = await service.GetAsync(OrgGuid, new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31));

        Assert.True(result.IsSuccess);
        Assert.Equal(900m, result.Value!.NetSales);
        Assert.Equal(100m, result.Value.CommercialDiscountTotal);
        Assert.Equal(600m, result.Value.TotalCogs);
        Assert.Equal(300m, result.Value.GrossProfit);
    }

    private static OperationalReportService CreateOperational(SalePeriodAggregate period) =>
        new(
            new FakeSaleRepo { Period = period },
            new FakeReturnRepo(),
            shifts: null!,
            inventory: null!,
            stockCounts: null!,
            products: null!,
            purchaseOrders: null!,
            suppliers: null!,
            expenses: null!,
            expenseCategories: null!,
            utangReports: null!,
            clock: new FixedClock(Now));

    private sealed class FakeSaleRepo : ISaleRepository
    {
        public SalePeriodAggregate Period { get; init; } = new(0m, 0, 0m, 0, 0m, 0m, 0m, 0);
        public SaleCostPeriodAggregate Costs { get; init; } = new(0, 0, 0, 0, 0m);

        public Task<SalePeriodAggregate> AggregatePeriodAsync(
            PosOrganizationId organizationId,
            DateOnly fromDateUtc,
            DateOnly toDateUtc,
            SaleStatus? status = null,
            SalePaymentMethod? paymentMethod = null,
            Guid? customerId = null,
            Guid? branchId = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Period);

        public Task<SaleCostPeriodAggregate> AggregateCostForProfitabilityAsync(
            PosOrganizationId organizationId,
            DateOnly fromDateUtc,
            DateOnly toDateUtc,
            Guid? branchId = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Costs);

        public Task<IReadOnlySet<Guid>> ListSaleIdsInBranchAsync(
            PosOrganizationId organizationId,
            IReadOnlyCollection<Guid> saleIds,
            Guid branchId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlySet<Guid>>(new HashSet<Guid>());

        public Task<IReadOnlyList<Sale>> ListForReportAsync(
            PosOrganizationId organizationId,
            DateOnly fromDateUtc,
            DateOnly toDateUtc,
            SaleStatus? status = null,
            SalePaymentMethod? paymentMethod = null,
            Guid? productId = null,
            Guid? customerId = null,
            Guid? branchId = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Sale>>(Array.Empty<Sale>());

        public Task<Sale?> GetByIdAsync(PosOrganizationId organizationId, SaleId saleId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<Sale?> FindBySaleNumberAsync(PosOrganizationId organizationId, string saleNumber, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<(IReadOnlyList<Sale> Items, int TotalCount)> ListAsync(PosOrganizationId organizationId, SaleFilter filter, int skip, int take, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<IReadOnlyList<SalePaymentAggregate>> AggregateCompletedByPaymentAsync(PosOrganizationId organizationId, DateOnly fromDateUtc, DateOnly toDateUtc, Guid? branchId = null, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<IReadOnlyList<SaleDailyAggregate>> AggregateCompletedByDayAsync(PosOrganizationId organizationId, DateOnly fromDateUtc, DateOnly toDateUtc, Guid? branchId = null, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<Sale> CheckoutAsync(PosOrganizationId organizationId, DateOnly businessDateUtc, Func<string, Sale> createSale, Func<Sale, CancellationToken, Task>? afterSaleCreated = null, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task UpdateAsync(Sale sale, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task AddAsync(Sale sale, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<string> ReserveNextSaleNumberAsync(PosOrganizationId organizationId, DateOnly businessDateUtc, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<bool> HasReturnsForSaleAsync(PosOrganizationId organizationId, SaleId saleId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class FakeReturnRepo : ISaleReturnRepository
    {
        public Task<(IReadOnlyList<SaleReturn> Items, int TotalCount)> ListAsync(
            PosOrganizationId organizationId,
            SaleReturnFilter filter,
            int skip,
            int take,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<(IReadOnlyList<SaleReturn>, int)>((Array.Empty<SaleReturn>(), 0));

        public Task<SaleReturnCogsPeriodAggregate> AggregateReturnCogsForPeriodAsync(
            PosOrganizationId organizationId,
            DateOnly fromDateUtc,
            DateOnly toDateUtc,
            Guid? branchId = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new SaleReturnCogsPeriodAggregate(0m, false));

        public Task<decimal> SumRefundsForPeriodAsync(
            PosOrganizationId organizationId,
            DateOnly fromDateUtc,
            DateOnly toDateUtc,
            Guid? branchId = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(0m);

        public Task<SaleReturn?> GetByIdAsync(PosOrganizationId organizationId, SaleReturnId returnId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<IReadOnlyList<SaleReturn>> ListBySaleIdAsync(PosOrganizationId organizationId, SaleId saleId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<bool> HasReturnsForSaleAsync(PosOrganizationId organizationId, SaleId saleId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<IReadOnlyDictionary<Guid, SaleLineReturnTotals>> GetPriorTotalsBySaleLineAsync(PosOrganizationId organizationId, SaleId saleId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<decimal> SumCashRefundsForShiftAsync(PosOrganizationId organizationId, Guid cashierShiftId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<SaleReturn> CreateAsync(PosOrganizationId organizationId, DateOnly businessDateUtc, Func<string, SaleReturn> createReturn, Func<SaleReturn, CancellationToken, Task>? afterReturnCreated = null, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class FakeWasteRepo : IWasteLossRepository
    {
        public Task<InventoryDocumentCostPeriodAggregate> AggregatePostedCostForPeriodAsync(
            PosOrganizationId organizationId,
            DateOnly fromDateUtc,
            DateOnly toDateUtc,
            Guid? branchId = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new InventoryDocumentCostPeriodAggregate(0m, 0, 0, 0, 0));

        public Task<WasteLoss?> GetByIdAsync(PosOrganizationId organizationId, WasteLossId wasteLossId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<WasteLoss?> FindByIdempotencyKeyAsync(PosOrganizationId organizationId, string idempotencyKey, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<(IReadOnlyList<WasteLoss> Items, int TotalCount)> ListAsync(PosOrganizationId organizationId, WasteLossFilter filter, int skip, int take, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task AddAsync(WasteLoss wasteLoss, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task UpdateAsync(WasteLoss wasteLoss, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<string> AllocateNextNumberAsync(PosOrganizationId organizationId, DateOnly businessDateUtc, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class FakeStockUseRepo : IStockUseRepository
    {
        public Task<InventoryDocumentCostPeriodAggregate> AggregatePostedCostForPeriodAsync(
            PosOrganizationId organizationId,
            DateOnly fromDateUtc,
            DateOnly toDateUtc,
            Guid? branchId = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new InventoryDocumentCostPeriodAggregate(0m, 0, 0, 0, 0));

        public Task<StockUse?> GetByIdAsync(PosOrganizationId organizationId, StockUseId stockUseId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<StockUse?> FindByIdempotencyKeyAsync(PosOrganizationId organizationId, string idempotencyKey, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<(IReadOnlyList<StockUse> Items, int TotalCount)> ListAsync(PosOrganizationId organizationId, StockUseFilter filter, int skip, int take, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task AddAsync(StockUse stockUse, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task UpdateAsync(StockUse stockUse, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<string> AllocateNextNumberAsync(PosOrganizationId organizationId, DateOnly businessDateUtc, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class FixedClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow => utcNow;
    }
}
