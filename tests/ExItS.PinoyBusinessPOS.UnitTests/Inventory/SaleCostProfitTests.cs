using System.Reflection;
using ExItS.PinoyBusinessPOS.Application.CustomerOrdering;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Application.Reporting;
using ExItS.PinoyBusinessPOS.Application.Returns;
using ExItS.PinoyBusinessPOS.Application.Sales;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.CashierShifts;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Registers;
using ExItS.PinoyBusinessPOS.Domain.Returns;
using ExItS.PinoyBusinessPOS.Domain.Sales;
using ExItS.PinoyBusinessPOS.UnitTests.TestDoubles;

namespace ExItS.PinoyBusinessPOS.UnitTests.Inventory;

public sealed class SaleCostProfitTests
{
    private static readonly PosOrganizationId Org = PosOrganizationId.From(Guid.NewGuid());
    private static readonly Guid Actor = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 8, 29, 12, 0, 0, TimeSpan.Zero);
    private static readonly CashierShiftId Shift = CashierShiftId.New();
    private static readonly RegisterId Register = RegisterId.New();

    private static SaleLineDraft Draft(
        CatalogProductId productId,
        decimal unitPrice,
        decimal quantity,
        decimal? unitCostSnapshot = null,
        UnitOfMeasure unit = UnitOfMeasure.Piece,
        SellingMode sellingMode = SellingMode.PerItem,
        ProductUnitId? sellingUnitId = null,
        decimal? enteredQuantity = null,
        decimal? multiplierToBaseSnapshot = null,
        string name = "Item") =>
        new(
            productId,
            name,
            "SKU-1",
            null,
            unit,
            unitPrice,
            quantity,
            sellingMode,
            sellingUnitId,
            null,
            enteredQuantity,
            multiplierToBaseSnapshot,
            unitCostSnapshot);

    private static Sale Checkout(
        IReadOnlyList<SaleLineDraft> lines,
        IReadOnlyList<CommercialDiscountIntent>? discounts = null,
        decimal? tendered = 100_000m) =>
        Sale.Checkout(
            Org,
            SaleNumbers.Format(new DateOnly(2026, 8, 29), 1),
            SalePaymentMethod.Cash,
            lines,
            Actor,
            Now,
            tendered,
            cashierShiftId: Shift,
            registerId: Register,
            commercialDiscounts: discounts);

    [Fact]
    public void Checkout_with_known_resale_cost_snapshots_complete_cogs()
    {
        var product = CatalogProductId.New();
        var sale = Checkout([Draft(product, 50m, 2m, unitCostSnapshot: 20m)]);

        Assert.Equal(ProductionCostStatus.Complete, sale.CostStatus);
        Assert.Equal(40m, sale.TotalCostSnapshot);
        Assert.Equal(20m, sale.Lines[0].UnitCostSnapshot);
        Assert.Equal(40m, sale.Lines[0].LineCostSnapshot);

        var profit = SaleProfitability.Compute(sale);
        Assert.NotNull(profit);
        Assert.Equal(60m, profit!.GrossProfit);
        Assert.Equal(60m, profit.GrossMarginPercent);
    }

    [Fact]
    public void Checkout_uses_production_output_unit_cost_for_produced_item()
    {
        var produced = CatalogProductId.New();
        var sale = Checkout([Draft(produced, 80m, 1m, unitCostSnapshot: 35m)]);

        Assert.Equal(ProductionCostStatus.Complete, sale.CostStatus);
        Assert.Equal(35m, sale.TotalCostSnapshot);
        Assert.Equal(45m, SaleProfitability.Compute(sale)!.GrossProfit);
    }

    [Fact]
    public void Weighted_sale_line_cost_uses_base_quantity()
    {
        var product = CatalogProductId.New();
        var sale = Checkout([Draft(product, 0.05m, 2.5m, unitCostSnapshot: 0.02m, unit: UnitOfMeasure.Kilogram)]);

        Assert.Equal(0.05m, sale.Lines[0].LineCostSnapshot);
        Assert.Equal(ProductionCostStatus.Complete, sale.CostStatus);
    }

    [Fact]
    public void Package_sale_cost_multiplies_by_converted_base_quantity()
    {
        var product = CatalogProductId.New();
        var pack = CatalogProductUnit.Create(
            Org,
            product,
            ProductUnitKind.Sell,
            "6-pack",
            "6pk",
            6m,
            Now,
            sellingPrice: 55m);

        var sale = Checkout([
            Draft(
                product,
                55m,
                12m,
                unitCostSnapshot: 7m,
                sellingUnitId: pack.Id,
                enteredQuantity: 2m,
                multiplierToBaseSnapshot: 6m),
        ]);

        Assert.Equal(12m, sale.Lines[0].Quantity);
        Assert.Equal(84m, sale.Lines[0].LineCostSnapshot);
        Assert.Equal(84m, sale.TotalCostSnapshot);
    }

    [Fact]
    public void Sale_profitability_uses_discounted_sale_total()
    {
        var product = CatalogProductId.New();
        var sale = Checkout(
            [Draft(product, 100m, 1m, unitCostSnapshot: 40m)],
            [new CommercialDiscountIntent(SaleDiscountScope.Line, SaleDiscountMethod.FixedAmount, 10m, "Promo", product, 1)]);

        Assert.Equal(90m, sale.Total);
        Assert.Equal(40m, sale.TotalCostSnapshot);

        var profit = SaleProfitability.Compute(sale);
        Assert.Equal(50m, profit!.GrossProfit);
        Assert.Equal(55.56m, profit.GrossMarginPercent);
    }

    [Fact]
    public void Partial_cost_when_one_line_lacks_acquisition_cost()
    {
        var known = CatalogProductId.New();
        var unknown = CatalogProductId.New();
        var sale = Checkout([
            Draft(known, 30m, 1m, unitCostSnapshot: 10m),
            Draft(unknown, 20m, 1m),
        ]);

        Assert.Equal(ProductionCostStatus.Partial, sale.CostStatus);
        Assert.Equal(10m, sale.TotalCostSnapshot);
        Assert.Null(SaleProfitability.Compute(sale));
    }

    [Fact]
    public void Unknown_cost_when_no_line_has_acquisition_cost()
    {
        var sale = Checkout([Draft(CatalogProductId.New(), 25m, 1m)]);

        Assert.Equal(ProductionCostStatus.Unavailable, sale.CostStatus);
        Assert.Null(sale.TotalCostSnapshot);
        Assert.Null(sale.Lines[0].UnitCostSnapshot);
    }

    [Fact]
    public void Legacy_rehydrated_sale_without_cost_fields_is_unavailable()
    {
        var saleId = SaleId.New();
        var product = CatalogProductId.New();
        var line = SaleLine.Rehydrate(
            SaleLineId.New(),
            saleId,
            Org,
            product,
            1,
            "Legacy",
            null,
            null,
            UnitOfMeasure.Piece,
            25m,
            1m,
            25m);

        var sale = Sale.Rehydrate(
            saleId,
            Org,
            "SALE-LEGACY",
            SaleStatus.Completed,
            SalePaymentMethod.Cash,
            25m,
            25m,
            0m,
            100m,
            75m,
            null,
            Now,
            Actor,
            null,
            null,
            null,
            Now,
            [line],
            cashierShiftId: Shift,
            registerId: Register);

        Assert.Equal(ProductionCostStatus.Unavailable, sale.CostStatus);
        Assert.Null(sale.TotalCostSnapshot);
    }

    [Fact]
    public void Cost_snapshots_remain_immutable_after_later_cost_changes()
    {
        var product = CatalogProductId.New();
        var inventory = new CostResolverInventoryStub { Costs = { [product.Value] = 15m } };
        var resolver = new InventoryCostResolver(inventory);

        var enriched = resolver.EnrichDraftsWithCostsAsync(
                Org,
                [Draft(product, 40m, 1m)],
                CancellationToken.None)
            .GetAwaiter()
            .GetResult();
        var sale = Checkout(enriched);

        inventory.Costs[product.Value] = 99m;

        Assert.Equal(15m, sale.Lines[0].UnitCostSnapshot);
        Assert.Equal(15m, sale.TotalCostSnapshot);
    }

    [Fact]
    public async Task Batch_resolver_returns_latest_costs_per_product()
    {
        var rice = CatalogProductId.New();
        var beans = CatalogProductId.New();
        var inventory = new CostResolverInventoryStub
        {
            Costs =
            {
                [rice.Value] = 12m,
                [beans.Value] = 8m,
            },
        };
        var resolver = new InventoryCostResolver(inventory);

        var costs = await resolver.ResolveUnitCostsAsync(Org, [rice, beans, CatalogProductId.New()]);

        Assert.Equal(12m, costs[rice.Value]);
        Assert.Equal(8m, costs[beans.Value]);
        Assert.Null(costs.Values.Last());
    }

    [Fact]
    public async Task Profitability_report_excludes_voided_totals_from_net_sales()
    {
        var org = Org.Value;
        var service = new ProfitabilityReportService(
            new FakeSaleRepo
            {
                Period = new SalePeriodAggregate(500m, 2, 200m, 1, 0m, 0m, 0m, 0),
                Costs = new SaleCostPeriodAggregate(2, 2, 0, 0, 120m),
            },
            new FakeReturnRepo(),
            new FakeWasteRepo(),
            new FakeStockUseRepo(),
            new FixedClock(Now));

        var result = await service.GetAsync(org, new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31));

        Assert.True(result.IsSuccess);
        Assert.Equal(300m, result.Value!.NetSales);
        Assert.Equal("Complete", result.Value.CogsStatus);
        Assert.Equal(120m, result.Value.TotalCogs);
        Assert.Equal(180m, result.Value.GrossProfit);
    }

    [Fact]
    public async Task Profitability_report_keeps_waste_and_stock_use_separate_from_sale_cogs()
    {
        var service = new ProfitabilityReportService(
            new FakeSaleRepo
            {
                Period = new SalePeriodAggregate(100m, 1, 0m, 0, 0m, 0m, 0m, 0),
                Costs = new SaleCostPeriodAggregate(1, 1, 0, 0, 30m),
            },
            new FakeReturnRepo(),
            new FakeWasteRepo { Waste = new InventoryDocumentCostPeriodAggregate(25m, 1, 1, 0, 0) },
            new FakeStockUseRepo { StockUse = new InventoryDocumentCostPeriodAggregate(10m, 1, 0, 1, 0) },
            new FixedClock(Now));

        var result = await service.GetAsync(Org.Value, new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31));

        Assert.Equal(30m, result.Value!.TotalCogs);
        Assert.Equal(25m, result.Value.WasteLossKnownCost);
        Assert.Equal(10m, result.Value.StockUseKnownCost);
    }

    [Fact]
    public async Task Return_cogs_subtraction_keeps_period_partial_when_unknown_return_line_exists()
    {
        var service = new ProfitabilityReportService(
            new FakeSaleRepo
            {
                Period = new SalePeriodAggregate(100m, 1, 0m, 0, 0m, 0m, 0m, 0),
                Costs = new SaleCostPeriodAggregate(1, 1, 0, 0, 50m),
            },
            new FakeReturnRepo
            {
                ReturnCogs = new SaleReturnCogsPeriodAggregate(5m, HasUnknownCostReturn: true),
            },
            new FakeWasteRepo(),
            new FakeStockUseRepo(),
            new FixedClock(Now));

        var result = await service.GetAsync(Org.Value, new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31));

        Assert.Equal("Partial", result.Value!.CogsStatus);
        Assert.Equal(45m, result.Value.KnownCogs);
        Assert.Null(result.Value.TotalCogs);
    }

    [Fact]
    public void Customer_facing_order_dtos_do_not_expose_cost_snapshots()
    {
        var forbidden = new[]
        {
            nameof(CustomerOrderDto),
            nameof(CustomerOrderLineDto),
            nameof(CustomerOrderListItemDto),
        };

        var assembly = typeof(CustomerOrderDto).Assembly;
        foreach (var typeName in forbidden)
        {
            var type = assembly.GetType($"ExItS.PinoyBusinessPOS.Application.CustomerOrdering.{typeName}", throwOnError: true)!;
            var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(p => p.Name)
                .ToList();

            Assert.DoesNotContain(props, p => p.Contains("Cost", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(props, p => p.Contains("Cogs", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(props, p => p.Contains("Profit", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(props, p => p.Contains("Margin", StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public void Sale_with_only_produced_output_cost_does_not_double_count_materials()
    {
        var finished = CatalogProductId.New();
        var sale = Checkout([Draft(finished, 120m, 1m, unitCostSnapshot: 45m)]);

        Assert.Equal(45m, sale.TotalCostSnapshot);
        Assert.Equal(75m, SaleProfitability.Compute(sale)!.GrossProfit);
    }

    private sealed class FakeSaleRepo : ISaleRepository
    {
        public SalePeriodAggregate Period { get; init; } =
            new(0m, 0, 0m, 0, 0m, 0m, 0m, 0);

        public SaleCostPeriodAggregate Costs { get; init; } =
            new(0, 0, 0, 0, 0m);

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

        public Task<Sale?> GetByIdAsync(
            PosOrganizationId organizationId,
            SaleId saleId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<Sale?> FindBySaleNumberAsync(
            PosOrganizationId organizationId,
            string saleNumber,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<(IReadOnlyList<Sale> Items, int TotalCount)> ListAsync(
            PosOrganizationId organizationId,
            SaleFilter filter,
            int skip,
            int take,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<Sale>> ListForReportAsync(
            PosOrganizationId organizationId,
            DateOnly fromDateUtc,
            DateOnly toDateUtc,
            SaleStatus? status = null,
            SalePaymentMethod? paymentMethod = null,
            Guid? productId = null,
            Guid? customerId = null,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<SalePaymentAggregate>> AggregateCompletedByPaymentAsync(
            PosOrganizationId organizationId,
            DateOnly fromDateUtc,
            DateOnly toDateUtc,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<SaleDailyAggregate>> AggregateCompletedByDayAsync(
            PosOrganizationId organizationId,
            DateOnly fromDateUtc,
            DateOnly toDateUtc,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<Sale> CheckoutAsync(
            PosOrganizationId organizationId,
            DateOnly businessDateUtc,
            Func<string, Sale> createSale,
            Func<Sale, CancellationToken, Task>? afterSaleCreated = null,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task AddAsync(Sale sale, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task UpdateAsync(Sale sale, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> HasReturnsForSaleAsync(
            PosOrganizationId organizationId,
            SaleId saleId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<string> ReserveNextSaleNumberAsync(
            PosOrganizationId organizationId,
            DateOnly businessDateUtc,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class FakeReturnRepo : ISaleReturnRepository
    {
        public SaleReturnCogsPeriodAggregate ReturnCogs { get; init; } =
            new(0m, false);

        public Task<SaleReturnCogsPeriodAggregate> AggregateReturnCogsForPeriodAsync(
            PosOrganizationId organizationId,
            DateOnly fromDateUtc,
            DateOnly toDateUtc,
            Guid? branchId = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(ReturnCogs);

        public Task<decimal> SumRefundsForPeriodAsync(
            PosOrganizationId organizationId,
            DateOnly fromDateUtc,
            DateOnly toDateUtc,
            Guid? branchId = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(0m);

        public Task<SaleReturn?> GetByIdAsync(
            PosOrganizationId organizationId,
            SaleReturnId returnId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<(IReadOnlyList<SaleReturn> Items, int TotalCount)> ListAsync(
            PosOrganizationId organizationId,
            SaleReturnFilter filter,
            int skip,
            int take,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<SaleReturn>> ListBySaleIdAsync(
            PosOrganizationId organizationId,
            SaleId saleId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> HasReturnsForSaleAsync(
            PosOrganizationId organizationId,
            SaleId saleId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyDictionary<Guid, SaleLineReturnTotals>> GetPriorTotalsBySaleLineAsync(
            PosOrganizationId organizationId,
            SaleId saleId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<decimal> SumCashRefundsForShiftAsync(
            PosOrganizationId organizationId,
            Guid cashierShiftId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<SaleReturn> CreateAsync(
            PosOrganizationId organizationId,
            DateOnly businessDateUtc,
            Func<string, SaleReturn> createReturn,
            Func<SaleReturn, CancellationToken, Task>? afterReturnCreated = null,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class FakeWasteRepo : IWasteLossRepository
    {
        public InventoryDocumentCostPeriodAggregate Waste { get; init; } =
            new(0m, 0, 0, 0, 0);

        public Task<InventoryDocumentCostPeriodAggregate> AggregatePostedCostForPeriodAsync(
            PosOrganizationId organizationId,
            DateOnly fromDateUtc,
            DateOnly toDateUtc,
            Guid? branchId = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Waste);

        public Task<WasteLoss?> GetByIdAsync(
            PosOrganizationId organizationId,
            WasteLossId wasteLossId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<WasteLoss?> FindByIdempotencyKeyAsync(
            PosOrganizationId organizationId,
            string idempotencyKey,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<(IReadOnlyList<WasteLoss> Items, int TotalCount)> ListAsync(
            PosOrganizationId organizationId,
            WasteLossFilter filter,
            int skip,
            int take,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task AddAsync(WasteLoss wasteLoss, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task UpdateAsync(WasteLoss wasteLoss, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<string> AllocateNextNumberAsync(
            PosOrganizationId organizationId,
            DateOnly businessDateUtc,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class FakeStockUseRepo : IStockUseRepository
    {
        public InventoryDocumentCostPeriodAggregate StockUse { get; init; } =
            new(0m, 0, 0, 0, 0);

        public Task<InventoryDocumentCostPeriodAggregate> AggregatePostedCostForPeriodAsync(
            PosOrganizationId organizationId,
            DateOnly fromDateUtc,
            DateOnly toDateUtc,
            Guid? branchId = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(StockUse);

        public Task<StockUse?> GetByIdAsync(
            PosOrganizationId organizationId,
            StockUseId stockUseId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<StockUse?> FindByIdempotencyKeyAsync(
            PosOrganizationId organizationId,
            string idempotencyKey,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<(IReadOnlyList<StockUse> Items, int TotalCount)> ListAsync(
            PosOrganizationId organizationId,
            StockUseFilter filter,
            int skip,
            int take,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task AddAsync(StockUse stockUse, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task UpdateAsync(StockUse stockUse, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<string> AllocateNextNumberAsync(
            PosOrganizationId organizationId,
            DateOnly businessDateUtc,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class FixedClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow => utcNow;
    }
}
