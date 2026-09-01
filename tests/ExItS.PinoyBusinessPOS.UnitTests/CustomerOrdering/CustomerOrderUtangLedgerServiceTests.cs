using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Credit;
using ExItS.PinoyBusinessPOS.Application.CustomerOrdering;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Application.Inventory;
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

public sealed class CustomerOrderUtangLedgerServiceTests
{
    private static readonly PosOrganizationId Org =
        PosOrganizationId.From(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
    private static readonly Guid Branch = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly Guid Actor = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
    private static readonly Guid PersonalUser = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
    private static readonly Guid PlatformBusinessCustomerId =
        Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly CatalogProductId Product =
        CatalogProductId.From(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));
    private static readonly DateTimeOffset Utc = new(2026, 8, 24, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Submitted_utang_order_does_not_post_when_complete_hook_skips_non_completed()
    {
        var order = CreateOrder(CustomerOrderStatus.Submitted, CustomerOrderPaymentMethod.Utang);
        var sales = new FakeSales();
        var credits = new FakeCredits();
        var service = CreateService(sales, credits);

        await service.PostOnCompleteIfNeededAsync(order, Actor, Utc);

        Assert.Empty(sales.Added);
        Assert.Empty(credits.Added);
    }

    [Fact]
    public async Task Completed_utang_order_posts_one_sale_and_credit_for_authoritative_total()
    {
        var order = CreateOrder(CustomerOrderStatus.Completed, CustomerOrderPaymentMethod.Utang, total: 800m);
        var sales = new FakeSales();
        var credits = new FakeCredits();
        var service = CreateService(sales, credits);

        await service.PostOnCompleteIfNeededAsync(order, Actor, Utc);

        Assert.Single(sales.Added);
        Assert.Single(credits.Added);
        Assert.Equal(800m, credits.Added[0].Amount);
        Assert.Equal(800m, sales.Added[0].Total);
        Assert.Equal(SaleStockReservationState.Consumed, sales.Added[0].StockReservationState);
        Assert.Equal(CustomerOrderUtangSettlementIds.SaleIdForOrder(order.Id), sales.Added[0].Id);
        Assert.Equal(CustomerOrderUtangSettlementIds.CreditEntryIdForOrder(order.Id), credits.Added[0].Id);
        Assert.Equal(
            ProductBasedUtangRemarks.ForCustomerOrderNumber(order.OrderNumber),
            credits.Added[0].Remarks);
    }

    /// <summary>
    /// Seller Utang posting needs POS correlation (platformBusinessCustomerId), not an Active Personal link.
    /// </summary>
    [Theory]
    [InlineData("Pending")]
    [InlineData("Declined")]
    [InlineData("Revoked")]
    [InlineData("Blocked")]
    [InlineData("Unavailable")]
    public async Task Completed_utang_order_posts_without_requiring_active_personal_link(string connectionState)
    {
        _ = connectionState;
        var order = CreateOrder(CustomerOrderStatus.Completed, CustomerOrderPaymentMethod.Utang, total: 250m);
        var sales = new FakeSales();
        var credits = new FakeCredits();
        var service = CreateService(sales, credits);

        await service.PostOnCompleteIfNeededAsync(order, Actor, Utc);

        Assert.Single(sales.Added);
        Assert.Single(credits.Added);
        Assert.Equal(250m, credits.Added[0].Amount);
    }

    [Fact]
    public async Task Completed_cash_order_posts_settlement_sale_without_credit()
    {
        var order = CreateOrder(CustomerOrderStatus.Completed, CustomerOrderPaymentMethod.Cash);
        var sales = new FakeSales();
        var credits = new FakeCredits();
        var service = CreateService(sales, credits);

        await service.PostOnCompleteIfNeededAsync(order, Actor, Utc);

        Assert.Single(sales.Added);
        Assert.Empty(credits.Added);
        Assert.Equal(SalePaymentMethod.Cash, sales.Added[0].PaymentMethod);
        Assert.Equal(SaleStockReservationState.Consumed, sales.Added[0].StockReservationState);
        Assert.Equal(CustomerOrderUtangSettlementIds.SaleIdForOrder(order.Id), sales.Added[0].Id);
    }

    [Fact]
    public async Task Completed_manual_gcash_order_posts_settlement_sale_without_credit()
    {
        var order = CreateOrder(CustomerOrderStatus.Completed, CustomerOrderPaymentMethod.ManualGCash);
        var sales = new FakeSales();
        var credits = new FakeCredits();
        var service = CreateService(sales, credits);

        await service.PostOnCompleteIfNeededAsync(order, Actor, Utc);

        Assert.Single(sales.Added);
        Assert.Empty(credits.Added);
        Assert.Equal(SalePaymentMethod.ManualGCash, sales.Added[0].PaymentMethod);
    }

    [Fact]
    public async Task Accepted_utang_order_does_not_post()
    {
        var order = CreateOrder(CustomerOrderStatus.Accepted, CustomerOrderPaymentMethod.Utang);
        var sales = new FakeSales();
        var credits = new FakeCredits();
        var service = CreateService(sales, credits);

        await service.PostOnCompleteIfNeededAsync(order, Actor, Utc);

        Assert.Empty(sales.Added);
        Assert.Empty(credits.Added);
    }

    [Fact]
    public async Task Retry_is_idempotent_when_settlement_sale_already_exists()
    {
        var order = CreateOrder(CustomerOrderStatus.Completed, CustomerOrderPaymentMethod.Utang);
        var settlementSaleId = CustomerOrderUtangSettlementIds.SaleIdForOrder(order.Id);
        var customerId = POSCustomerId.From(Guid.Parse("99999999-9999-9999-9999-999999999999"));
        var sales = new FakeSales
        {
            Existing = Sale.RecordCustomerOrderUtangSettlement(
                Org,
                SaleNumbers.Format(DateOnly.FromDateTime(Utc.UtcDateTime), 1),
                order,
                order.Total,
                CustomerOrderUtangSettlementLines.FromOrder(order),
                Actor,
                Utc,
                customerId,
                CreditEntryId.New(),
                id: settlementSaleId),
        };
        var credits = new FakeCredits();
        var service = CreateService(sales, credits);

        await service.PostOnCompleteIfNeededAsync(order, Actor, Utc);

        Assert.Empty(sales.Added);
        Assert.Empty(credits.Added);
    }

    [Fact]
    public void Settlement_lines_include_delivery_fee_in_sale_total()
    {
        var order = CustomerOrder.CreateSubmitted(
            Org,
            "SO-000010",
            CustomerOrderParty.Personal(PersonalUser, "Ana"),
            CustomerOrderFulfillmentType.Delivery,
            Branch,
            "Main",
            [new CustomerOrderLineDraft(Product, "Rice", "RICE", UnitOfMeasure.Piece, 2m, 250m)],
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
            paymentMethod: CustomerOrderPaymentMethod.Utang,
            platformBusinessCustomerId: PlatformBusinessCustomerId);

        var drafts = CustomerOrderUtangSettlementLines.FromOrder(order);
        var sale = Sale.RecordCustomerOrderUtangSettlement(
            Org,
            SaleNumbers.Format(DateOnly.FromDateTime(Utc.UtcDateTime), 2),
            order,
            order.Total,
            drafts,
            Actor,
            Utc,
            POSCustomerId.From(Guid.Parse("99999999-9999-9999-9999-999999999999")),
            CreditEntryId.New());

        Assert.Equal(550m, sale.Total);
    }

    private static CustomerOrderUtangLedgerService CreateService(FakeSales sales, FakeCredits credits) =>
        new(sales, credits, new FakeCustomers(), new InventoryCostResolver(new CostResolverInventoryStub()));

    private static CustomerOrder CreateOrder(
        CustomerOrderStatus status,
        CustomerOrderPaymentMethod paymentMethod,
        decimal total = 500m)
    {
        var order = CustomerOrder.CreateSubmitted(
            Org,
            "SO-000001",
            CustomerOrderParty.Personal(PersonalUser, "Ana"),
            CustomerOrderFulfillmentType.Pickup,
            Branch,
            "Main",
            [new CustomerOrderLineDraft(Product, "Rice", "RICE", UnitOfMeasure.Piece, 1m, total)],
            Actor,
            Utc,
            paymentMethod: paymentMethod,
            platformBusinessCustomerId: PlatformBusinessCustomerId);

        if (status == CustomerOrderStatus.Accepted)
        {
            order.Accept(Actor, Utc);
        }
        else if (status == CustomerOrderStatus.Submitted)
        {
            // leave submitted
        }
        else if (status == CustomerOrderStatus.Completed)
        {
            order.Accept(Actor, Utc);
            order.MarkReady(Utc, Actor);
            order.MarkCollected(Utc, Actor);
            order.Complete(Actor, Utc);
        }

        return order;
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
            Task.FromResult(
                organizationId == Org && platformBusinessCustomerId == PlatformBusinessCustomerId ? 1 : 0);

        public Task<POSCustomer?> GetByIdAsync(
            PosOrganizationId organizationId,
            POSCustomerId customerId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<POSCustomer?>(_customer.Id == customerId ? _customer : null);

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
        public List<CreditEntry> Added { get; } = [];

        public Task AddAsync(CreditEntry entry, CancellationToken cancellationToken = default)
        {
            Added.Add(entry);
            return Task.CompletedTask;
        }

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
            Task.FromResult<(IReadOnlyList<CreditEntry>, int)>((Added, Added.Count));

        public Task<(IReadOnlyList<CreditEntry> Items, int TotalCount)> ListCreatedSinceAsync(
            PosOrganizationId organizationId,
            DateTimeOffset? sinceUtc,
            int skip,
            int take,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<(IReadOnlyList<CreditEntry>, int)>((Added, Added.Count));

        public Task<IReadOnlyList<CreditEntry>> ListActiveByOrganizationAsync(
            PosOrganizationId organizationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CreditEntry>>(Added);

        public Task<IReadOnlyList<CreditEntry>> ListRecordedInRangeAsync(
            PosOrganizationId organizationId,
            DateOnly fromDateUtc,
            DateOnly toDateUtc,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CreditEntry>>(Added);

        public Task<decimal> SumActiveAmountAsync(
            PosOrganizationId organizationId,
            POSCustomerId customerId,
            CancellationToken cancellationToken = default, IReadOnlySet<Guid>? historyBranchIds = null) =>
            Task.FromResult(Added.Where(c => c.Status == CreditEntryStatus.Active).Sum(c => c.Amount));

        public Task<int> CountActiveAsync(
            PosOrganizationId organizationId,
            POSCustomerId customerId,
            CancellationToken cancellationToken = default, IReadOnlySet<Guid>? historyBranchIds = null) =>
            Task.FromResult(Added.Count(c => c.Status == CreditEntryStatus.Active));

        public Task UpdateAsync(CreditEntry entry, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
