using ExItS.PinoyBusinessPOS.Application.Reporting;
using ExItS.PinoyBusinessPOS.Application.Returns;
using ExItS.PinoyBusinessPOS.Application.Sales;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Returns;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.UnitTests.Reporting;

public sealed class ProductProfitabilityRankingTests
{
    private static readonly PosOrganizationId Org = PosOrganizationId.From(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
    private static readonly Guid ProductHigh = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid ProductLow = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid ProductPartial = Guid.Parse("33333333-3333-3333-3333-333333333333");

    private sealed class FixedClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow => utcNow;
    }

    private static ProductProfitabilityReportService CreateService(
        IReadOnlyList<ProductProfitabilitySaleAggregate> sales,
        IReadOnlyList<ProductProfitabilityReturnAggregate>? returns = null) =>
        new(
            new FakeSaleRepo(sales),
            new FakeReturnRepo(returns ?? []),
            new FixedClock(new DateTimeOffset(2026, 8, 30, 12, 0, 0, TimeSpan.Zero)));

    [Fact]
    public async Task Ranks_highest_gross_profit_by_default_using_snapshots()
    {
        var svc = CreateService(
        [
            new(ProductHigh, "High GP", null, "Piece", 10m, 1000m, 100m, 900m, 300m, 10m, 0m),
            new(ProductLow, "Low GP", null, "Piece", 5m, 500m, 0m, 500m, 400m, 5m, 0m)
        ]);

        var result = await svc.GetAsync(Org.Value, new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 30));
        Assert.True(result.IsSuccess);
        var rows = result.Value!.Rows;
        Assert.Equal(ProductProfitabilityRankBy.GrossProfitDesc, result.Value.RankBy);
        Assert.Equal(2, rows.Count);
        Assert.Equal(ProductHigh, rows[0].ProductId);
        Assert.Equal(600m, rows[0].GrossProfit); // 900 - 300
        Assert.Equal(100m, rows[0].CommercialDiscounts);
        Assert.Equal(1000m, rows[0].SalesBeforeDiscounts);
        Assert.Equal(900m, rows[0].NetSales);
        Assert.Equal(300m, rows[0].TotalCogs);
        Assert.Equal(ProductLow, rows[1].ProductId);
        Assert.Equal(100m, rows[1].GrossProfit); // 500 - 400
    }

    [Fact]
    public async Task Returns_reduce_net_sales_and_cogs_from_original_snapshots()
    {
        var svc = CreateService(
            [new(ProductHigh, "Milk", null, "Piece", 10m, 1000m, 0m, 1000m, 400m, 10m, 0m)],
            [new(ProductHigh, 2m, 200m, 80m, false)]);

        var result = await svc.GetAsync(Org.Value, new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 30));
        var row = Assert.Single(result.Value!.Rows);
        Assert.Equal(800m, row.NetSales);
        Assert.Equal(200m, row.RefundAmount);
        Assert.Equal(2m, row.QuantityReturned);
        Assert.Equal(320m, row.KnownCogs); // 400 - 80
        Assert.Equal(480m, row.GrossProfit); // 800 - 320
        Assert.Equal(nameof(ProductionCostStatus.Complete), row.CogsStatus);
    }

    [Fact]
    public async Task Partial_cogs_hides_gross_profit()
    {
        var svc = CreateService(
        [
            new(ProductPartial, "Partial", null, "Piece", 10m, 1000m, 0m, 1000m, 200m, 5m, 5m)
        ]);

        var row = Assert.Single(
            (await svc.GetAsync(Org.Value, new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 30))).Value!.Rows);
        Assert.Equal(nameof(ProductionCostStatus.Partial), row.CogsStatus);
        Assert.Null(row.TotalCogs);
        Assert.Null(row.GrossProfit);
        Assert.Null(row.GrossMarginPercent);
        Assert.Equal(200m, row.KnownCogs);
        Assert.Equal(50m, row.CostCompletenessPercent);
    }

    [Fact]
    public async Task Unavailable_cogs_hides_gross_profit()
    {
        var svc = CreateService(
        [
            new(ProductPartial, "Unknown", null, "Piece", 4m, 400m, 0m, 400m, 0m, 0m, 4m)
        ]);

        var row = Assert.Single(
            (await svc.GetAsync(Org.Value, new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 30))).Value!.Rows);
        Assert.Equal(nameof(ProductionCostStatus.Unavailable), row.CogsStatus);
        Assert.Null(row.GrossProfit);
    }

    [Fact]
    public async Task Rank_by_net_sales_and_lowest_gross_profit()
    {
        var sales = new List<ProductProfitabilitySaleAggregate>
        {
            new(ProductHigh, "A", null, "Piece", 1m, 100m, 0m, 100m, 10m, 1m, 0m),
            new(ProductLow, "B", null, "Piece", 1m, 500m, 0m, 500m, 450m, 1m, 0m)
        };

        var byNet = await CreateService(sales)
            .GetAsync(Org.Value, new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 30),
                rankBy: ProductProfitabilityRankBy.NetSalesDesc);
        Assert.Equal(ProductLow, byNet.Value!.Rows[0].ProductId);

        var byLowGp = await CreateService(sales)
            .GetAsync(Org.Value, new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 30),
                rankBy: ProductProfitabilityRankBy.GrossProfitAsc);
        Assert.Equal(ProductLow, byLowGp.Value!.Rows[0].ProductId);
        Assert.Equal(50m, byLowGp.Value.Rows[0].GrossProfit);
    }

    [Fact]
    public async Task Does_not_double_subtract_commercial_discount_from_net_sales()
    {
        var svc = CreateService(
        [
            new(ProductHigh, "Disc", null, "Piece", 1m, 1000m, 100m, 900m, 600m, 1m, 0m)
        ]);

        var row = Assert.Single(
            (await svc.GetAsync(Org.Value, new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 30))).Value!.Rows);
        Assert.Equal(900m, row.NetSales);
        Assert.Equal(300m, row.GrossProfit); // not 200
    }

    private sealed class FakeSaleRepo(IReadOnlyList<ProductProfitabilitySaleAggregate> rows) : ISaleRepository
    {
        public Task<IReadOnlyList<ProductProfitabilitySaleAggregate>> AggregateProductProfitabilitySalesAsync(
            PosOrganizationId organizationId,
            DateOnly fromDateUtc,
            DateOnly toDateUtc,
            Guid? branchId = null,
            CancellationToken cancellationToken = default)
        {
            IEnumerable<ProductProfitabilitySaleAggregate> filtered = rows;
            // branch filtering exercised via dedicated aggregate inputs in callers
            _ = branchId;
            return Task.FromResult<IReadOnlyList<ProductProfitabilitySaleAggregate>>(filtered.ToList());
        }

        public Task<Sale?> GetByIdAsync(PosOrganizationId organizationId, SaleId saleId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Sale?> FindBySaleNumberAsync(PosOrganizationId organizationId, string saleNumber, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(IReadOnlyList<Sale> Items, int TotalCount)> ListAsync(PosOrganizationId organizationId, SaleFilter filter, int skip, int take, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<Sale>> ListForReportAsync(PosOrganizationId organizationId, DateOnly fromDateUtc, DateOnly toDateUtc, SaleStatus? status = null, SalePaymentMethod? paymentMethod = null, Guid? productId = null, Guid? customerId = null, Guid? branchId = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlySet<Guid>> ListSaleIdsInBranchAsync(PosOrganizationId organizationId, IReadOnlyCollection<Guid> saleIds, Guid branchId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<SalePeriodAggregate> AggregatePeriodAsync(PosOrganizationId organizationId, DateOnly fromDateUtc, DateOnly toDateUtc, SaleStatus? status = null, SalePaymentMethod? paymentMethod = null, Guid? customerId = null, Guid? branchId = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<SalePaymentAggregate>> AggregateCompletedByPaymentAsync(PosOrganizationId organizationId, DateOnly fromDateUtc, DateOnly toDateUtc, Guid? branchId = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<SaleDailyAggregate>> AggregateCompletedByDayAsync(PosOrganizationId organizationId, DateOnly fromDateUtc, DateOnly toDateUtc, Guid? branchId = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<SaleCostPeriodAggregate> AggregateCostForProfitabilityAsync(PosOrganizationId organizationId, DateOnly fromDateUtc, DateOnly toDateUtc, Guid? branchId = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Sale> CheckoutAsync(PosOrganizationId organizationId, DateOnly businessDateUtc, Func<string, Sale> createSale, Func<Sale, CancellationToken, Task>? afterSaleCreated = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task UpdateAsync(Sale sale, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task AddAsync(Sale sale, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<string> ReserveNextSaleNumberAsync(PosOrganizationId organizationId, DateOnly businessDateUtc, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> HasReturnsForSaleAsync(PosOrganizationId organizationId, SaleId saleId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class FakeReturnRepo(IReadOnlyList<ProductProfitabilityReturnAggregate> rows) : ISaleReturnRepository
    {
        public Task<IReadOnlyList<ProductProfitabilityReturnAggregate>> AggregateProductProfitabilityReturnsAsync(
            PosOrganizationId organizationId,
            DateOnly fromDateUtc,
            DateOnly toDateUtc,
            Guid? branchId = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(rows);

        public Task<SaleReturn?> GetByIdAsync(PosOrganizationId organizationId, SaleReturnId returnId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(IReadOnlyList<SaleReturn> Items, int TotalCount)> ListAsync(PosOrganizationId organizationId, SaleReturnFilter filter, int skip, int take, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<SaleReturn>> ListBySaleIdAsync(PosOrganizationId organizationId, SaleId saleId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> HasReturnsForSaleAsync(PosOrganizationId organizationId, SaleId saleId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyDictionary<Guid, SaleLineReturnTotals>> GetPriorTotalsBySaleLineAsync(PosOrganizationId organizationId, SaleId saleId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<decimal> SumCashRefundsForShiftAsync(PosOrganizationId organizationId, Guid cashierShiftId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<SaleReturnCogsPeriodAggregate> AggregateReturnCogsForPeriodAsync(PosOrganizationId organizationId, DateOnly fromDateUtc, DateOnly toDateUtc, Guid? branchId = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<decimal> SumRefundsForPeriodAsync(PosOrganizationId organizationId, DateOnly fromDateUtc, DateOnly toDateUtc, Guid? branchId = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<SaleReturn> CreateAsync(PosOrganizationId organizationId, DateOnly businessDateUtc, Func<string, SaleReturn> createReturn, Func<SaleReturn, CancellationToken, Task>? afterReturnCreated = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
