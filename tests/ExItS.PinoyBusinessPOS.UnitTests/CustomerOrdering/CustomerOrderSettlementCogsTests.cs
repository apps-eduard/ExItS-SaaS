using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Credit;
using ExItS.PinoyBusinessPOS.Application.CustomerOrdering;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Application.Reporting;
using ExItS.PinoyBusinessPOS.Application.Sales;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Credit;
using ExItS.PinoyBusinessPOS.Domain.CustomerOrdering;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Sales;
using ExItS.PinoyBusinessPOS.UnitTests.TestDoubles;

namespace ExItS.PinoyBusinessPOS.UnitTests.CustomerOrdering;

public sealed class CustomerOrderSettlementCogsTests
{
    private static readonly PosOrganizationId Org =
        PosOrganizationId.From(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
    private static readonly Guid Branch = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly Guid Actor = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
    private static readonly Guid PersonalUser = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
    private static readonly Guid PlatformBusinessCustomerId =
        Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly CatalogProductId KnownProduct =
        CatalogProductId.From(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));
    private static readonly CatalogProductId UnknownProduct =
        CatalogProductId.From(Guid.Parse("22222222-2222-2222-2222-222222222222"));
    private static readonly DateTimeOffset Utc = new(2026, 8, 24, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Completed_cash_order_snapshots_known_unit_and_line_cost()
    {
        var inventory = new CostResolverInventoryStub { Costs = { [KnownProduct.Value] = 12.5m } };
        var order = CreateCompletedOrder(CustomerOrderPaymentMethod.Cash, KnownProduct, 2m, 100m);
        var sales = new FakeSales();
        var service = CreateService(sales, inventory);

        await service.PostOnCompleteIfNeededAsync(order, Actor, Utc);

        var sale = Assert.Single(sales.Added);
        Assert.Equal(ProductionCostStatus.Complete, sale.CostStatus);
        Assert.Equal(25m, sale.TotalCostSnapshot);
        Assert.Equal(12.5m, sale.Lines[0].UnitCostSnapshot);
        Assert.Equal(25m, sale.Lines[0].LineCostSnapshot);
    }

    [Fact]
    public async Task Completed_order_with_multiple_known_lines_snapshots_total_cost()
    {
        var inventory = new CostResolverInventoryStub
        {
            Costs =
            {
                [KnownProduct.Value] = 10m,
                [UnknownProduct.Value] = 5m,
            },
        };
        var order = CreateCompletedMultiLineOrder(CustomerOrderPaymentMethod.ManualGCash);
        var sales = new FakeSales();
        var service = CreateService(sales, inventory);

        await service.PostOnCompleteIfNeededAsync(order, Actor, Utc);

        var sale = Assert.Single(sales.Added);
        Assert.Equal(ProductionCostStatus.Complete, sale.CostStatus);
        Assert.Equal(30m, sale.TotalCostSnapshot);
        Assert.Equal(20m, sale.Lines[0].LineCostSnapshot);
        Assert.Equal(10m, sale.Lines[1].LineCostSnapshot);
    }

    [Fact]
    public async Task Mixed_known_and_unknown_lines_yield_partial_cost_status()
    {
        var inventory = new CostResolverInventoryStub { Costs = { [KnownProduct.Value] = 8m } };
        var order = CreateCompletedMultiLineOrder(CustomerOrderPaymentMethod.Cash);
        var sales = new FakeSales();
        var service = CreateService(sales, inventory);

        await service.PostOnCompleteIfNeededAsync(order, Actor, Utc);

        var sale = Assert.Single(sales.Added);
        Assert.Equal(ProductionCostStatus.Partial, sale.CostStatus);
        Assert.Equal(16m, sale.TotalCostSnapshot);
        Assert.Null(sale.Lines[1].UnitCostSnapshot);
        Assert.Null(sale.Lines[1].LineCostSnapshot);
    }

    [Fact]
    public async Task All_unknown_lines_yield_unavailable_cost_status_without_zeroing()
    {
        var order = CreateCompletedOrder(CustomerOrderPaymentMethod.Cash, UnknownProduct, 1m, 50m);
        var sales = new FakeSales();
        var service = CreateService(sales, new CostResolverInventoryStub());

        await service.PostOnCompleteIfNeededAsync(order, Actor, Utc);

        var sale = Assert.Single(sales.Added);
        Assert.Equal(ProductionCostStatus.Unavailable, sale.CostStatus);
        Assert.Null(sale.TotalCostSnapshot);
        Assert.Null(sale.Lines[0].UnitCostSnapshot);
        Assert.Null(sale.Lines[0].LineCostSnapshot);
    }

    [Fact]
    public async Task Delivery_fee_line_excluded_from_inventory_cost_enrichment()
    {
        var inventory = new CostResolverInventoryStub { Costs = { [KnownProduct.Value] = 20m } };
        var order = CreateCompletedDeliveryOrder(CustomerOrderPaymentMethod.Cash);
        var sales = new FakeSales();
        var service = CreateService(sales, inventory);

        await service.PostOnCompleteIfNeededAsync(order, Actor, Utc);

        var sale = Assert.Single(sales.Added);
        Assert.Equal(ProductionCostStatus.Partial, sale.CostStatus);
        Assert.Equal(40m, sale.TotalCostSnapshot);
        var feeLine = Assert.Single(sale.Lines, l => l.NameSnapshot == CustomerOrderUtangSettlementLines.DeliveryFeeLineName);
        Assert.Null(feeLine.UnitCostSnapshot);
        Assert.Null(feeLine.LineCostSnapshot);
    }

    [Fact]
    public async Task Later_inventory_cost_change_does_not_alter_settlement_snapshot()
    {
        var inventory = new CostResolverInventoryStub { Costs = { [KnownProduct.Value] = 15m } };
        var order = CreateCompletedOrder(CustomerOrderPaymentMethod.Cash, KnownProduct, 1m, 100m);
        var sales = new FakeSales();
        var service = CreateService(sales, inventory);

        await service.PostOnCompleteIfNeededAsync(order, Actor, Utc);
        inventory.Costs[KnownProduct.Value] = 99m;

        await service.PostOnCompleteIfNeededAsync(order, Actor, Utc);

        var sale = Assert.Single(sales.Added);
        Assert.Equal(15m, sale.Lines[0].UnitCostSnapshot);
        Assert.Equal(15m, sale.Lines[0].LineCostSnapshot);
    }

    [Fact]
    public async Task Retry_is_idempotent_for_cash_settlement_sale()
    {
        var inventory = new CostResolverInventoryStub { Costs = { [KnownProduct.Value] = 10m } };
        var order = CreateCompletedOrder(CustomerOrderPaymentMethod.Cash, KnownProduct, 1m, 100m);
        var settlementSaleId = CustomerOrderUtangSettlementIds.SaleIdForOrder(order.Id);
        var sales = new FakeSales
        {
            Existing = Sale.RecordCustomerOrderSettlement(
                Org,
                SaleNumbers.Format(DateOnly.FromDateTime(Utc.UtcDateTime), 1),
                order,
                order.Total,
                CustomerOrderUtangSettlementLines.FromOrder(order),
                Actor,
                Utc,
                SalePaymentMethod.Cash,
                id: settlementSaleId),
        };
        var service = CreateService(sales, inventory);

        await service.PostOnCompleteIfNeededAsync(order, Actor, Utc);

        Assert.Empty(sales.Added);
    }

    [Fact]
    public async Task Accepted_order_does_not_post_settlement_sale()
    {
        var order = CreateSubmittedOrder(CustomerOrderPaymentMethod.Cash, KnownProduct);
        order.Accept(Actor, Utc);
        var sales = new FakeSales();
        var service = CreateService(sales, new CostResolverInventoryStub { Costs = { [KnownProduct.Value] = 10m } });

        await service.PostOnCompleteIfNeededAsync(order, Actor, Utc);

        Assert.Empty(sales.Added);
    }

    [Fact]
    public void Customer_order_settlement_sale_profitability_uses_snapshots()
    {
        var inventory = new CostResolverInventoryStub { Costs = { [KnownProduct.Value] = 20m } };
        var order = CreateCompletedOrder(CustomerOrderPaymentMethod.Cash, KnownProduct, 2m, 100m);
        var drafts = CustomerOrderUtangSettlementLines.FromOrder(order);
        var enriched = new InventoryCostResolver(inventory)
            .EnrichDraftsWithCostsAsync(Org, drafts.Where(CustomerOrderUtangSettlementLines.IsInventoryCostLine).ToList())
            .GetAwaiter()
            .GetResult();
        var sale = Sale.RecordCustomerOrderSettlement(
            Org,
            SaleNumbers.Format(DateOnly.FromDateTime(Utc.UtcDateTime), 1),
            order,
            order.Total,
            enriched,
            Actor,
            Utc,
            SalePaymentMethod.Cash);

        var profit = SaleProfitability.Compute(sale);
        Assert.NotNull(profit);
        Assert.Equal(160m, profit!.GrossProfit);
    }

    [Fact]
    public void Return_cogs_uses_original_customer_order_sale_line_unit_cost_snapshot()
    {
        var inventory = new CostResolverInventoryStub { Costs = { [KnownProduct.Value] = 18m } };
        var order = CreateCompletedOrder(CustomerOrderPaymentMethod.Cash, KnownProduct, 2m, 100m);
        var drafts = CustomerOrderUtangSettlementLines.FromOrder(order);
        var enriched = new InventoryCostResolver(inventory)
            .EnrichDraftsWithCostsAsync(Org, drafts.Where(CustomerOrderUtangSettlementLines.IsInventoryCostLine).ToList())
            .GetAwaiter()
            .GetResult();
        var sale = Sale.RecordCustomerOrderSettlement(
            Org,
            SaleNumbers.Format(DateOnly.FromDateTime(Utc.UtcDateTime), 1),
            order,
            order.Total,
            enriched,
            Actor,
            Utc,
            SalePaymentMethod.Cash);

        inventory.Costs[KnownProduct.Value] = 99m;

        var returnCogs = sale.Lines[0].UnitCostSnapshot is null
            ? 0m
            : SaleMoney.RoundMoney(sale.Lines[0].UnitCostSnapshot!.Value * 1m);
        Assert.Equal(18m, returnCogs);
        Assert.NotEqual(99m, returnCogs);
    }

    [Fact]
    public async Task Batch_cost_resolver_invoked_once_for_multi_line_order()
    {
        var inventory = new CountingCostResolverInventoryStub
        {
            Costs =
            {
                [KnownProduct.Value] = 10m,
                [UnknownProduct.Value] = 5m,
            },
        };
        var order = CreateCompletedMultiLineOrder(CustomerOrderPaymentMethod.Cash);
        var sales = new FakeSales();
        var service = CreateService(sales, inventory);

        await service.PostOnCompleteIfNeededAsync(order, Actor, Utc);

        Assert.Equal(1, inventory.BatchLookupCount);
        Assert.Single(sales.Added);
    }

    private static CustomerOrderUtangLedgerService CreateService(FakeSales sales, IInventoryRepository inventory) =>
        new(sales, new FakeCredits(), new FakeCustomers(), new InventoryCostResolver(inventory));

    private static CustomerOrder CreateCompletedOrder(
        CustomerOrderPaymentMethod paymentMethod,
        CatalogProductId productId,
        decimal quantity,
        decimal unitPrice)
    {
        var order = CreateSubmittedOrder(paymentMethod, productId, quantity, unitPrice);
        order.Accept(Actor, Utc);
        order.MarkReady(Utc, Actor);
        order.MarkCollected(Utc, Actor);
        order.Complete(Actor, Utc);
        return order;
    }

    private static CustomerOrder CreateCompletedMultiLineOrder(CustomerOrderPaymentMethod paymentMethod)
    {
        var order = CustomerOrder.CreateSubmitted(
            Org,
            "SO-000002",
            CustomerOrderParty.Personal(PersonalUser, "Ana"),
            CustomerOrderFulfillmentType.Pickup,
            Branch,
            "Main",
            [
                new CustomerOrderLineDraft(KnownProduct, "Known", "KNOWN", UnitOfMeasure.Piece, 2m, 100m),
                new CustomerOrderLineDraft(UnknownProduct, "Unknown", "UNK", UnitOfMeasure.Piece, 2m, 50m),
            ],
            Actor,
            Utc,
            paymentMethod: paymentMethod,
            platformBusinessCustomerId: PlatformBusinessCustomerId);
        order.Accept(Actor, Utc);
        order.MarkReady(Utc, Actor);
        order.MarkCollected(Utc, Actor);
        order.Complete(Actor, Utc);
        return order;
    }

    private static CustomerOrder CreateCompletedDeliveryOrder(CustomerOrderPaymentMethod paymentMethod)
    {
        var order = CustomerOrder.CreateSubmitted(
            Org,
            "SO-000010",
            CustomerOrderParty.Personal(PersonalUser, "Ana"),
            CustomerOrderFulfillmentType.Delivery,
            Branch,
            "Main",
            [new CustomerOrderLineDraft(KnownProduct, "Rice", "RICE", UnitOfMeasure.Piece, 2m, 250m)],
            Actor,
            Utc,
            CustomerOrderDeliverySnapshot.Rehydrate(
                "Ana",
                null,
                "123 Main",
                null,
                "Manila",
                null,
                14.6m,
                120.98m,
                14.6m,
                120.98m,
                1m,
                0m,
                50m,
                3m,
                10m,
                20m,
                null,
                0m,
                50m,
                false),
            paymentMethod: paymentMethod,
            platformBusinessCustomerId: PlatformBusinessCustomerId);
        order.Accept(Actor, Utc);
        order.MarkReady(Utc, Actor);
        order.MarkOutForDelivery(Utc, Actor);
        order.MarkDelivered(Utc, Actor);
        order.Complete(Actor, Utc);
        return order;
    }

    private static CustomerOrder CreateSubmittedOrder(
        CustomerOrderPaymentMethod paymentMethod,
        CatalogProductId productId,
        decimal quantity = 1m,
        decimal unitPrice = 500m) =>
        CustomerOrder.CreateSubmitted(
            Org,
            "SO-000001",
            CustomerOrderParty.Personal(PersonalUser, "Ana"),
            CustomerOrderFulfillmentType.Pickup,
            Branch,
            "Main",
            [new CustomerOrderLineDraft(productId, "Rice", "RICE", UnitOfMeasure.Piece, quantity, unitPrice)],
            Actor,
            Utc,
            paymentMethod: paymentMethod,
            platformBusinessCustomerId: PlatformBusinessCustomerId);

    private sealed class CountingCostResolverInventoryStub : CostResolverInventoryStub
    {
        public int BatchLookupCount { get; private set; }

        public override Task<IReadOnlyDictionary<Guid, decimal?>> GetLatestAcquisitionUnitCostsAsync(
            PosOrganizationId organizationId,
            IReadOnlyCollection<CatalogProductId> productIds,
            CancellationToken cancellationToken = default)
        {
            BatchLookupCount++;
            return base.GetLatestAcquisitionUnitCostsAsync(organizationId, productIds, cancellationToken);
        }
    }

    private sealed class FakeCustomers : IPOSCustomerRepository
    {
        private readonly POSCustomer _customer = POSCustomer.Create(
            Org,
            "Linked Ana",
            Utc,
            platformBusinessCustomerId: PlatformBusinessCustomerId);

        public Task<POSCustomer?> FindByPlatformBusinessCustomerIdAsync(
            PosOrganizationId organizationId,
            Guid platformBusinessCustomerId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<POSCustomer?>(
                organizationId == Org && platformBusinessCustomerId == PlatformBusinessCustomerId
                    ? _customer
                    : null);

        public Task<int> CountByPlatformBusinessCustomerIdAsync(
            PosOrganizationId organizationId,
            Guid platformBusinessCustomerId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(1);

        public Task<POSCustomer?> GetByIdAsync(
            PosOrganizationId organizationId,
            POSCustomerId customerId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<POSCustomer?>(_customer);

        public Task<POSCustomer?> FindActiveByNormalizedMobileAsync(
            PosOrganizationId organizationId,
            string normalizedMobile,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<POSCustomer?>(null);

        public Task<POSCustomer?> FindByLinkedPersonalPublicUserIdAsync(
            PosOrganizationId organizationId,
            string linkedPersonalPublicUserId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<POSCustomer?>(null);

        public Task<POSCustomer?> FindByLinkedBuyerOrganizationIdAsync(
            PosOrganizationId organizationId,
            Guid linkedBuyerOrganizationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<POSCustomer?>(null);

        public Task<(IReadOnlyList<POSCustomer> Items, int TotalCount)> ListAsync(
            PosOrganizationId organizationId,
            CustomerStatus? status,
            string? search,
            int skip,
            int take, IReadOnlyCollection<Guid>? restrictToCustomerIds = null, CancellationToken cancellationToken = default) =>
            Task.FromResult<(IReadOnlyList<POSCustomer>, int)>(([_customer], 1));

        public Task<(IReadOnlyList<POSCustomer> Items, int TotalCount)> ListUpdatedSinceAsync(
            PosOrganizationId organizationId,
            DateTimeOffset? sinceUtc,
            int skip,
            int take,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<(IReadOnlyList<POSCustomer>, int)>(([_customer], 1));

        public Task<IReadOnlyList<POSCustomer>> ListByIdsAsync(
            PosOrganizationId organizationId,
            IReadOnlyCollection<POSCustomerId> customerIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<POSCustomer>>([_customer]);

        public Task AddAsync(POSCustomer customer, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task UpdateAsync(POSCustomer customer, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class FakeSales : ISaleRepository
    {
        public List<Sale> Added { get; } = [];
        public Sale? Existing { get; init; }

        public Task<Sale?> GetByIdAsync(
            PosOrganizationId organizationId,
            SaleId saleId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(
                Existing is not null && Existing.Id == saleId && Existing.OrganizationId == organizationId
                    ? Existing
                    : Added.FirstOrDefault(s => s.Id == saleId));

        public Task AddAsync(Sale sale, CancellationToken cancellationToken = default)
        {
            Added.Add(sale);
            return Task.CompletedTask;
        }

        public Task<string> ReserveNextSaleNumberAsync(
            PosOrganizationId organizationId,
            DateOnly businessDateUtc,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(SaleNumbers.Format(businessDateUtc, Added.Count + 1));

        public Task<Sale?> FindBySaleNumberAsync(
            PosOrganizationId organizationId,
            string saleNumber,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<Sale?>(null);

        public Task<(IReadOnlyList<Sale> Items, int TotalCount)> ListAsync(
            PosOrganizationId organizationId,
            SaleFilter filter,
            int skip,
            int take,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<(IReadOnlyList<Sale>, int)>((Added, Added.Count));

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
            Task.FromResult<IReadOnlyList<Sale>>(Added);

        public Task<IReadOnlySet<Guid>> ListSaleIdsInBranchAsync(
            PosOrganizationId organizationId,
            IReadOnlyCollection<Guid> saleIds,
            Guid branchId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlySet<Guid>>(new HashSet<Guid>());

        public Task<SalePeriodAggregate> AggregatePeriodAsync(
            PosOrganizationId organizationId,
            DateOnly fromDateUtc,
            DateOnly toDateUtc,
            SaleStatus? status = null,
            SalePaymentMethod? paymentMethod = null,
            Guid? customerId = null,
            Guid? branchId = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new SalePeriodAggregate(0, 0, 0, 0, 0, 0, 0, 0));

        public Task<SaleCostPeriodAggregate> AggregateCostForProfitabilityAsync(
            PosOrganizationId organizationId,
            DateOnly fromDateUtc,
            DateOnly toDateUtc,
            Guid? branchId = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new SaleCostPeriodAggregate(0, 0, 0, 0, 0m));

        public Task<IReadOnlyList<ProductProfitabilitySaleAggregate>> AggregateProductProfitabilitySalesAsync(
            PosOrganizationId organizationId,
            DateOnly fromDateUtc,
            DateOnly toDateUtc,
            Guid? branchId = null,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<SalePaymentAggregate>> AggregateCompletedByPaymentAsync(
            PosOrganizationId organizationId,
            DateOnly fromDateUtc,
            DateOnly toDateUtc,
            Guid? branchId = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<SalePaymentAggregate>>([]);

        public Task<IReadOnlyList<SaleDailyAggregate>> AggregateCompletedByDayAsync(
            PosOrganizationId organizationId,
            DateOnly fromDateUtc,
            DateOnly toDateUtc,
            Guid? branchId = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<SaleDailyAggregate>>([]);

        public Task<Sale> CheckoutAsync(
            PosOrganizationId organizationId,
            DateOnly businessDateUtc,
            Func<string, Sale> createSale,
            Func<Sale, CancellationToken, Task>? afterSaleCreated = null,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task UpdateAsync(Sale sale, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<bool> HasReturnsForSaleAsync(
            PosOrganizationId organizationId,
            SaleId saleId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(false);
    }

    private sealed class FakeCredits : ICreditEntryRepository
    {
        public Task AddAsync(CreditEntry entry, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<CreditEntry?> GetByIdAsync(
            PosOrganizationId organizationId,
            POSCustomerId customerId,
            CreditEntryId entryId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<CreditEntry?>(null);

        public Task<CreditEntry?> GetByIdForOrganizationAsync(
            PosOrganizationId organizationId,
            CreditEntryId entryId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<CreditEntry?>(null);

        public Task<(IReadOnlyList<CreditEntry> Items, int TotalCount)> ListByCustomerAsync(
            PosOrganizationId organizationId,
            POSCustomerId customerId,
            int skip,
            int take,
            CancellationToken cancellationToken = default, IReadOnlySet<Guid>? historyBranchIds = null) =>
            Task.FromResult<(IReadOnlyList<CreditEntry>, int)>(([], 0));

        public Task<(IReadOnlyList<CreditEntry> Items, int TotalCount)> ListCreatedSinceAsync(
            PosOrganizationId organizationId,
            DateTimeOffset? sinceUtc,
            int skip,
            int take,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<(IReadOnlyList<CreditEntry>, int)>(([], 0));

        public Task<IReadOnlyList<CreditEntry>> ListActiveByOrganizationAsync(
            PosOrganizationId organizationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CreditEntry>>([]);

        public Task<IReadOnlyList<CreditEntry>> ListRecordedInRangeAsync(
            PosOrganizationId organizationId,
            DateOnly fromDateUtc,
            DateOnly toDateUtc,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CreditEntry>>([]);

        public Task<decimal> SumActiveAmountAsync(
            PosOrganizationId organizationId,
            POSCustomerId customerId,
            CancellationToken cancellationToken = default,
            IReadOnlySet<Guid>? historyBranchIds = null) => Task.FromResult(0m);

        public Task<int> CountActiveAsync(
            PosOrganizationId organizationId,
            POSCustomerId customerId,
            CancellationToken cancellationToken = default,
            IReadOnlySet<Guid>? historyBranchIds = null) => Task.FromResult(0);

        public Task UpdateAsync(CreditEntry entry, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
