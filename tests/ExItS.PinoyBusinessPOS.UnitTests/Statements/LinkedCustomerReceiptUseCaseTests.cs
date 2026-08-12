using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Application.Sales;
using ExItS.PinoyBusinessPOS.Application.Statements;
using ExItS.PinoyBusinessPOS.Domain.CashierShifts;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Credit;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Registers;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.UnitTests.Statements;

public sealed class LinkedCustomerReceiptUseCaseTests
{
    private static readonly DateTimeOffset T0 = new(2026, 8, 12, 14, 30, 0, TimeSpan.Zero);
    private static readonly Guid OrgA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid OrgB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid PlatformCustomer = Guid.Parse("cccccccc-cccc-cccc-dddd-eeeeeeeeeeee");
    private static readonly Guid OtherPlatformCustomer = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
    private static readonly Guid PersonalUser = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid LinkedId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid Actor = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly CashierShiftId Shift = CashierShiftId.New();
    private static readonly RegisterId Register = RegisterId.New();

    [Fact]
    public async Task Receipt_returns_per_item_utang_sale_with_line_snapshots()
    {
        var harness = await Harness.CreateAuthorizedAsync();
        var sale = UtangSale(
            harness.PosCustomer.Id,
            new SaleLineDraft(
                CatalogProductId.New(),
                "Sardinas",
                "SKU-1",
                null,
                UnitOfMeasure.Can,
                25.50m,
                2m,
                SellingMode.PerItem));
        await harness.Sales.AddAsync(sale);

        var result = await harness.Receipt.ExecuteAsync(OrgA, PlatformCustomer, sale.Id.Value);

        Assert.True(result.IsSuccess, result.ErrorMessage);
        var dto = result.Value!;
        Assert.Equal(sale.Id.Value, dto.SaleId);
        Assert.Equal(sale.SaleNumber, dto.ReceiptNumber);
        Assert.Equal("Completed", dto.Status);
        Assert.Equal("Utang", dto.PaymentMethod);
        Assert.Equal(51.00m, dto.Total);
        Assert.Equal(51.00m, dto.Subtotal);
        Assert.Equal(51.00m, dto.UtangAmount);
        Assert.Equal(0m, dto.PaidAmount);
        Assert.Equal(51.00m, dto.OutstandingEffect);
        Assert.Null(dto.DiscountAmount);
        Assert.Null(dto.MerchantDisplayName);
        Assert.Null(dto.BranchDisplayName);
        Assert.Equal(0m, dto.TaxAmount);

        var line = Assert.Single(dto.Lines);
        Assert.Equal("Sardinas", line.ProductNameSnapshot);
        Assert.Equal(2m, line.Quantity);
        Assert.Equal("Can", line.UnitOfMeasure);
        Assert.Equal("PerItem", line.SellingMode);
        Assert.Equal(25.50m, line.UnitPriceSnapshot);
        Assert.Equal(51.00m, line.LineTotal);

        Assert.DoesNotContain(
            typeof(LinkedCustomerSaleReceiptDto).GetProperties().Select(p => p.Name),
            name => name is "Cost" or "Margin" or "Remarks" or "RecordedBy" or "VoidReason" or "RegisterId");
        Assert.DoesNotContain(
            typeof(LinkedCustomerSaleReceiptLineDto).GetProperties().Select(p => p.Name),
            name => name is "Cost" or "Margin" or "SkuSnapshot" or "BarcodeSnapshot" or "ProductId");
    }

    [Fact]
    public async Task Receipt_preserves_by_weight_quantity_and_unit_price_snapshot()
    {
        var harness = await Harness.CreateAuthorizedAsync();
        var sale = UtangSale(
            harness.PosCustomer.Id,
            new SaleLineDraft(
                CatalogProductId.New(),
                "Tomato",
                null,
                null,
                UnitOfMeasure.Kilogram,
                120m,
                0.350m,
                SellingMode.ByWeight));
        await harness.Sales.AddAsync(sale);

        var result = await harness.Receipt.ExecuteAsync(OrgA, PlatformCustomer, sale.Id.Value);
        Assert.True(result.IsSuccess, result.ErrorMessage);
        var line = Assert.Single(result.Value!.Lines);
        Assert.Equal(0.350m, line.Quantity);
        Assert.Equal(120m, line.UnitPriceSnapshot);
        Assert.Equal(42.00m, line.LineTotal);
        Assert.Equal("Kilogram", line.UnitOfMeasure);
        Assert.Equal("ByWeight", line.SellingMode);
        Assert.Equal(42.00m, result.Value.Total);
    }

    [Fact]
    public async Task Receipt_exposes_voided_status_without_active_outstanding_effect()
    {
        var harness = await Harness.CreateAuthorizedAsync();
        var sale = UtangSale(
            harness.PosCustomer.Id,
            new SaleLineDraft(
                CatalogProductId.New(),
                "Kape",
                null,
                null,
                UnitOfMeasure.Piece,
                10m,
                1m));
        sale.Void("customer cancel", Actor, T0.AddMinutes(5));
        await harness.Sales.AddAsync(sale);

        var result = await harness.Receipt.ExecuteAsync(OrgA, PlatformCustomer, sale.Id.Value);
        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.Equal("Voided", result.Value!.Status);
        Assert.Equal(10m, result.Value.Total);
        Assert.Equal(10m, result.Value.UtangAmount);
        Assert.Equal(0m, result.Value.OutstandingEffect);
        Assert.Equal("Kape", Assert.Single(result.Value.Lines).ProductNameSnapshot);
    }

    [Fact]
    public async Task Receipt_not_found_for_guessed_sale_id()
    {
        var harness = await Harness.CreateAuthorizedAsync();
        var result = await harness.Receipt.ExecuteAsync(OrgA, PlatformCustomer, Guid.NewGuid());
        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.ReceiptNotFound, result.ErrorCode);
    }

    [Fact]
    public async Task Receipt_not_found_when_sale_belongs_to_other_pos_customer()
    {
        var harness = await Harness.CreateAuthorizedAsync();
        var otherCustomer = POSCustomer.Create(
            PosOrganizationId.From(OrgA),
            "Other",
            T0,
            mobileNumber: "+639171111111");
        await harness.Customers.AddAsync(otherCustomer);

        var sale = UtangSale(
            otherCustomer.Id,
            new SaleLineDraft(
                CatalogProductId.New(),
                "Secret",
                null,
                null,
                UnitOfMeasure.Piece,
                99m,
                1m));
        await harness.Sales.AddAsync(sale);

        var result = await harness.Receipt.ExecuteAsync(OrgA, PlatformCustomer, sale.Id.Value);
        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.ReceiptNotFound, result.ErrorCode);
    }

    [Fact]
    public async Task Receipt_denied_when_platform_denied()
    {
        var harness = await Harness.CreateAsync(FakePlatform.Denied());
        var result = await harness.Receipt.ExecuteAsync(OrgA, PlatformCustomer, Guid.NewGuid());
        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.LinkedCustomerDenied, result.ErrorCode);
    }

    [Fact]
    public async Task Receipt_not_found_when_platform_unreachable()
    {
        var harness = await Harness.CreateAsync(FakePlatform.NotFound());
        var result = await harness.Receipt.ExecuteAsync(OrgA, PlatformCustomer, Guid.NewGuid());
        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.LinkedCustomerNotFound, result.ErrorCode);
    }

    [Fact]
    public async Task Receipt_not_found_for_wrong_org()
    {
        var harness = await Harness.CreateAuthorizedAsync();
        var sale = UtangSale(
            harness.PosCustomer.Id,
            new SaleLineDraft(
                CatalogProductId.New(),
                "Item",
                null,
                null,
                UnitOfMeasure.Piece,
                5m,
                1m));
        await harness.Sales.AddAsync(sale);

        var result = await harness.Receipt.ExecuteAsync(OrgB, PlatformCustomer, sale.Id.Value);
        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.LinkedCustomerNotFound, result.ErrorCode);
    }

    [Fact]
    public async Task Receipt_not_found_for_wrong_platform_customer()
    {
        var harness = await Harness.CreateAuthorizedAsync();
        var sale = UtangSale(
            harness.PosCustomer.Id,
            new SaleLineDraft(
                CatalogProductId.New(),
                "Item",
                null,
                null,
                UnitOfMeasure.Piece,
                5m,
                1m));
        await harness.Sales.AddAsync(sale);

        var result = await harness.Receipt.ExecuteAsync(OrgA, OtherPlatformCustomer, sale.Id.Value);
        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.LinkedCustomerNotFound, result.ErrorCode);
    }

    [Fact]
    public async Task Receipt_returns_exactly_one_sale_payload()
    {
        var harness = await Harness.CreateAuthorizedAsync();
        var sale1 = UtangSale(
            harness.PosCustomer.Id,
            new SaleLineDraft(
                CatalogProductId.New(),
                "A",
                null,
                null,
                UnitOfMeasure.Piece,
                1m,
                1m));
        var sale2 = UtangSale(
            harness.PosCustomer.Id,
            new SaleLineDraft(
                CatalogProductId.New(),
                "B",
                null,
                null,
                UnitOfMeasure.Piece,
                2m,
                1m),
            saleNumber: SaleNumbers.Format(new DateOnly(2026, 8, 12), 2));
        await harness.Sales.AddAsync(sale1);
        await harness.Sales.AddAsync(sale2);

        var result = await harness.Receipt.ExecuteAsync(OrgA, PlatformCustomer, sale1.Id.Value);
        Assert.True(result.IsSuccess);
        Assert.Equal(sale1.Id.Value, result.Value!.SaleId);
        Assert.Equal("A", Assert.Single(result.Value.Lines).ProductNameSnapshot);
    }

    private static Sale UtangSale(
        POSCustomerId customerId,
        SaleLineDraft line,
        string? saleNumber = null) =>
        Sale.Checkout(
            PosOrganizationId.From(OrgA),
            saleNumber ?? SaleNumbers.Format(new DateOnly(2026, 8, 12), 1),
            SalePaymentMethod.Utang,
            [line],
            Actor,
            T0,
            amountTendered: null,
            customerId: customerId,
            linkedCreditEntryId: CreditEntryId.New(),
            cashierShiftId: Shift,
            registerId: Register);

    private sealed class Harness
    {
        public required POSCustomer PosCustomer { get; init; }
        public required InMemoryCustomers Customers { get; init; }
        public required InMemorySales Sales { get; init; }
        public required GetLinkedCustomerSaleReceipt Receipt { get; init; }

        public static async Task<Harness> CreateAuthorizedAsync() =>
            await CreateAsync(FakePlatform.Authorized());

        public static async Task<Harness> CreateAsync(ILinkedCustomerPlatformAuthorization platform)
        {
            var customers = new InMemoryCustomers();
            var sales = new InMemorySales();
            var posCustomer = POSCustomer.Create(
                PosOrganizationId.From(OrgA),
                "Rosa Customer",
                T0,
                platformBusinessCustomerId: PlatformCustomer);
            await customers.AddAsync(posCustomer);

            var authorize = new AuthorizeLinkedCustomerStatementAccess(platform, customers);
            return new Harness
            {
                PosCustomer = posCustomer,
                Customers = customers,
                Sales = sales,
                Receipt = new GetLinkedCustomerSaleReceipt(authorize, sales)
            };
        }
    }

    private sealed class FakePlatform : ILinkedCustomerPlatformAuthorization
    {
        private readonly LinkedCustomerPlatformAuthorizationResult _result;

        private FakePlatform(LinkedCustomerPlatformAuthorizationResult result) => _result = result;

        public static FakePlatform Authorized() => new(
            new LinkedCustomerPlatformAuthorizationResult(
                LinkedCustomerPlatformAuthorizationOutcome.Authorized,
                new LinkedCustomerPlatformAuthorizationProof(
                    PersonalUser, OrgA, PlatformCustomer, LinkedId)));

        public static FakePlatform Denied() => new(
            new LinkedCustomerPlatformAuthorizationResult(
                LinkedCustomerPlatformAuthorizationOutcome.Denied, null));

        public static FakePlatform NotFound() => new(
            new LinkedCustomerPlatformAuthorizationResult(
                LinkedCustomerPlatformAuthorizationOutcome.NotFound, null));

        public Task<LinkedCustomerPlatformAuthorizationResult> VerifyAsync(
            Guid organizationId,
            Guid platformBusinessCustomerId,
            CancellationToken cancellationToken = default)
        {
            if (_result.Outcome == LinkedCustomerPlatformAuthorizationOutcome.Authorized
                && (_result.Proof!.OrganizationId != organizationId
                    || _result.Proof.PlatformBusinessCustomerId != platformBusinessCustomerId))
            {
                return Task.FromResult(new LinkedCustomerPlatformAuthorizationResult(
                    LinkedCustomerPlatformAuthorizationOutcome.NotFound, null));
            }

            return Task.FromResult(_result);
        }
    }

    private sealed class InMemoryCustomers : IPOSCustomerRepository
    {
        private readonly List<POSCustomer> _items = [];

        public Task AddAsync(POSCustomer customer, CancellationToken cancellationToken = default)
        {
            _items.Add(customer);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(POSCustomer customer, CancellationToken cancellationToken = default)
        {
            var i = _items.FindIndex(c => c.Id == customer.Id);
            if (i >= 0)
            {
                _items[i] = customer;
            }

            return Task.CompletedTask;
        }

        public Task<POSCustomer?> GetByIdAsync(PosOrganizationId organizationId, POSCustomerId customerId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(c => c.OrganizationId == organizationId && c.Id == customerId));

        public Task<POSCustomer?> FindActiveByNormalizedMobileAsync(PosOrganizationId organizationId, string normalizedMobile, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(c =>
                c.OrganizationId == organizationId && c.Status == CustomerStatus.Active && c.NormalizedMobile == normalizedMobile));

        public Task<POSCustomer?> FindByPlatformBusinessCustomerIdAsync(
            PosOrganizationId organizationId,
            Guid platformBusinessCustomerId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(c =>
                c.OrganizationId == organizationId && c.PlatformBusinessCustomerId == platformBusinessCustomerId));

        public Task<int> CountByPlatformBusinessCustomerIdAsync(
            PosOrganizationId organizationId,
            Guid platformBusinessCustomerId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.Count(c =>
                c.OrganizationId == organizationId && c.PlatformBusinessCustomerId == platformBusinessCustomerId));

        public Task<(IReadOnlyList<POSCustomer> Items, int TotalCount)> ListAsync(
            PosOrganizationId organizationId, CustomerStatus? status, string? search, int skip, int take, CancellationToken cancellationToken = default)
        {
            var list = _items.Where(c => c.OrganizationId == organizationId).ToList();
            return Task.FromResult(((IReadOnlyList<POSCustomer>)list.Skip(skip).Take(take).ToList(), list.Count));
        }

        public Task<(IReadOnlyList<POSCustomer> Items, int TotalCount)> ListUpdatedSinceAsync(
            PosOrganizationId organizationId, DateTimeOffset? sinceUtc, int skip, int take, CancellationToken cancellationToken = default) =>
            ListAsync(organizationId, null, null, skip, take, cancellationToken);

        public Task<IReadOnlyList<POSCustomer>> ListByIdsAsync(
            PosOrganizationId organizationId, IReadOnlyCollection<POSCustomerId> customerIds, CancellationToken cancellationToken = default)
        {
            var ids = customerIds.Select(c => c.Value).ToHashSet();
            return Task.FromResult<IReadOnlyList<POSCustomer>>(
                _items.Where(c => c.OrganizationId == organizationId && ids.Contains(c.Id.Value)).ToList());
        }
    }

    private sealed class InMemorySales : ISaleRepository
    {
        private readonly List<Sale> _items = [];

        public Task AddAsync(Sale sale)
        {
            _items.Add(sale);
            return Task.CompletedTask;
        }

        public Task<Sale?> GetByIdAsync(
            PosOrganizationId organizationId,
            SaleId saleId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(s => s.OrganizationId == organizationId && s.Id == saleId));

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

        public Task<Sale> CheckoutAsync(
            PosOrganizationId organizationId,
            DateOnly businessDateUtc,
            Func<string, Sale> createSale,
            Func<Sale, CancellationToken, Task>? afterSaleCreated = null,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task UpdateAsync(Sale sale, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> HasReturnsForSaleAsync(
            PosOrganizationId organizationId,
            SaleId saleId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
