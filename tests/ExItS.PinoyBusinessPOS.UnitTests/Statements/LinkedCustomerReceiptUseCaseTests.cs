using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Credit;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Application.Payments;
using ExItS.PinoyBusinessPOS.Application.Sales;
using ExItS.PinoyBusinessPOS.Application.Statements;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.CashierShifts;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Credit;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Payments;
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
    public async Task Receipt_returns_cash_sale_when_customer_id_persisted()
    {
        var harness = await Harness.CreateAuthorizedAsync();
        var sale = Sale.Checkout(
            PosOrganizationId.From(OrgA),
            SaleNumbers.Format(new DateOnly(2026, 8, 12), 9),
            SalePaymentMethod.Cash,
            [
                new SaleLineDraft(
                    CatalogProductId.New(),
                    "Pork",
                    "SKU-P",
                    null,
                    UnitOfMeasure.Kilogram,
                    80m,
                    1m,
                    SellingMode.ByWeight)
            ],
            Actor,
            T0,
            amountTendered: 80m,
            customerId: harness.PosCustomer.Id,
            cashierShiftId: Shift,
            registerId: Register);
        await harness.Sales.AddAsync(sale);

        var result = await harness.Receipt.ExecuteAsync(OrgA, PlatformCustomer, sale.Id.Value);
        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.Equal("Cash", result.Value!.PaymentMethod);
        Assert.Equal(80m, result.Value.PaidAmount);
        Assert.Null(result.Value.UtangAmount);
        Assert.Equal(0m, result.Value.OutstandingEffect);
    }

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

    [Fact]
    public async Task Receipt_old_settled_requires_extended_entitlement()
    {
        var harness = await Harness.CreateAuthorizedAsync();
        var creditId = CreditEntryId.New();
        var sale = UtangSale(
            harness.PosCustomer.Id,
            new SaleLineDraft(
                CatalogProductId.New(),
                "Old",
                null,
                null,
                UnitOfMeasure.Piece,
                50m,
                1m),
            recordedAt: new DateTimeOffset(2025, 1, 10, 0, 0, 0, TimeSpan.Zero),
            creditId: creditId);
        await harness.Sales.AddAsync(sale);
        var credit = CreditEntry.Create(
            PosOrganizationId.From(OrgA),
            harness.PosCustomer.Id,
            50m,
            "Goods",
            sale.RecordedAtUtc,
            id: creditId,
            sourceSaleId: sale.Id);
        credit.Reverse("settled via repayment path", sale.RecordedAtUtc.AddDays(1));
        await harness.Credits.AddAsync(credit);

        var denied = await harness.Receipt.ExecuteAsync(OrgA, PlatformCustomer, sale.Id.Value);
        Assert.False(denied.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.ExtendedHistoryRequired, denied.ErrorCode);

        harness.Entitlements.Active = true;
        var allowed = await harness.Receipt.ExecuteAsync(OrgA, PlatformCustomer, sale.Id.Value);
        Assert.True(allowed.IsSuccess, allowed.ErrorMessage);
        Assert.Equal(sale.Id.Value, allowed.Value!.SaleId);
    }

    [Fact]
    public async Task Receipt_old_active_utang_allowed_under_open_debt_exception()
    {
        var harness = await Harness.CreateAuthorizedAsync();
        var creditId = CreditEntryId.New();
        var sale = UtangSale(
            harness.PosCustomer.Id,
            new SaleLineDraft(
                CatalogProductId.New(),
                "Still owed",
                null,
                null,
                UnitOfMeasure.Piece,
                80m,
                1m),
            recordedAt: new DateTimeOffset(2025, 1, 10, 0, 0, 0, TimeSpan.Zero),
            creditId: creditId);
        await harness.Sales.AddAsync(sale);
        await harness.Credits.AddAsync(CreditEntry.Create(
            PosOrganizationId.From(OrgA),
            harness.PosCustomer.Id,
            80m,
            "Goods",
            sale.RecordedAtUtc,
            id: creditId,
            sourceSaleId: sale.Id));

        var result = await harness.Receipt.ExecuteAsync(OrgA, PlatformCustomer, sale.Id.Value);
        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.Equal("Still owed", Assert.Single(result.Value!.Lines).ProductNameSnapshot);
    }

    [Fact]
    public async Task Receipt_settling_open_debt_removes_exception_then_entitlement_unlocks()
    {
        var harness = await Harness.CreateAuthorizedAsync();
        var creditId = CreditEntryId.New();
        var sale = UtangSale(
            harness.PosCustomer.Id,
            new SaleLineDraft(
                CatalogProductId.New(),
                "Settled later",
                null,
                null,
                UnitOfMeasure.Piece,
                90m,
                1m),
            recordedAt: new DateTimeOffset(2025, 1, 10, 0, 0, 0, TimeSpan.Zero),
            creditId: creditId);
        await harness.Sales.AddAsync(sale);
        await harness.Credits.AddAsync(CreditEntry.Create(
            PosOrganizationId.From(OrgA),
            harness.PosCustomer.Id,
            90m,
            "Goods",
            sale.RecordedAtUtc,
            id: creditId,
            sourceSaleId: sale.Id));

        Assert.True((await harness.Receipt.ExecuteAsync(OrgA, PlatformCustomer, sale.Id.Value)).IsSuccess);

        await harness.Repayments.AddAsync(Repayment.Create(
            PosOrganizationId.From(OrgA),
            harness.PosCustomer.Id,
            90m,
            "Paid off",
            Actor,
            new DateTimeOffset(2025, 2, 1, 0, 0, 0, TimeSpan.Zero)));

        var afterSettle = await harness.Receipt.ExecuteAsync(OrgA, PlatformCustomer, sale.Id.Value);
        Assert.False(afterSettle.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.ExtendedHistoryRequired, afterSettle.ErrorCode);

        harness.Entitlements.Active = true;
        Assert.True((await harness.Receipt.ExecuteAsync(OrgA, PlatformCustomer, sale.Id.Value)).IsSuccess);
    }

    [Fact]
    public async Task Receipt_guessed_id_stays_not_found_even_when_entitled()
    {
        var harness = await Harness.CreateAuthorizedAsync();
        harness.Entitlements.Active = true;
        var result = await harness.Receipt.ExecuteAsync(OrgA, PlatformCustomer, Guid.NewGuid());
        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.ReceiptNotFound, result.ErrorCode);
    }

    private static Sale UtangSale(
        POSCustomerId customerId,
        SaleLineDraft line,
        string? saleNumber = null,
        DateTimeOffset? recordedAt = null,
        CreditEntryId? creditId = null) =>
        Sale.Checkout(
            PosOrganizationId.From(OrgA),
            saleNumber ?? SaleNumbers.Format(new DateOnly(2026, 8, 12), 1),
            SalePaymentMethod.Utang,
            [line],
            Actor,
            recordedAt ?? T0,
            amountTendered: null,
            customerId: customerId,
            linkedCreditEntryId: creditId ?? CreditEntryId.New(),
            cashierShiftId: Shift,
            registerId: Register);

    private sealed class Harness
    {
        public required POSCustomer PosCustomer { get; init; }
        public required InMemoryCustomers Customers { get; init; }
        public required InMemorySales Sales { get; init; }
        public required InMemoryCredits Credits { get; init; }
        public required InMemoryRepayments Repayments { get; init; }
        public required FakeEntitlements Entitlements { get; init; }
        public required GetLinkedCustomerSaleReceipt Receipt { get; init; }

        public static async Task<Harness> CreateAuthorizedAsync() =>
            await CreateAsync(FakePlatform.Authorized());

        public static async Task<Harness> CreateAsync(ILinkedCustomerPlatformAuthorization platform)
        {
            var customers = new InMemoryCustomers();
            var sales = new InMemorySales();
            var credits = new InMemoryCredits();
            var repayments = new InMemoryRepayments();
            var clock = new FixedClock(T0.AddDays(1));
            var outstanding = new OutstandingBalanceService(credits, repayments, clock);
            var entitlements = new FakeEntitlements(active: false);
            var options = Microsoft.Extensions.Options.Options.Create(new PersonalStatementsOptions { FreeRecentMonths = 3 });
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
                Credits = credits,
                Repayments = repayments,
                Entitlements = entitlements,
                Receipt = new GetLinkedCustomerSaleReceipt(
                    authorize,
                    sales,
                    credits,
                    outstanding,
                    entitlements,
                    options,
                    clock)
            };
        }
    }

    private sealed class FakeEntitlements(bool active) : IPersonalFeatureEntitlementClient
    {
        public bool Active { get; set; } = active;

        public Task<bool> HasActiveEntitlementAsync(string featureCode, CancellationToken cancellationToken = default) =>
            Task.FromResult(Active);
    }

    private sealed class FixedClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
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

    private sealed class InMemoryCredits : ICreditEntryRepository
    {
        public List<CreditEntry> All { get; } = [];

        public Task AddAsync(CreditEntry entry, CancellationToken cancellationToken = default)
        {
            All.Add(entry);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(CreditEntry entry, CancellationToken cancellationToken = default)
        {
            var i = All.FindIndex(e => e.Id == entry.Id);
            if (i >= 0)
            {
                All[i] = entry;
            }

            return Task.CompletedTask;
        }

        public Task<CreditEntry?> GetByIdAsync(
            PosOrganizationId organizationId,
            POSCustomerId customerId,
            CreditEntryId entryId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(All.FirstOrDefault(e =>
                e.OrganizationId == organizationId && e.CustomerId == customerId && e.Id == entryId));

        public Task<CreditEntry?> GetByIdForOrganizationAsync(
            PosOrganizationId organizationId,
            CreditEntryId entryId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(All.FirstOrDefault(e => e.OrganizationId == organizationId && e.Id == entryId));

        public Task<(IReadOnlyList<CreditEntry> Items, int TotalCount)> ListByCustomerAsync(
            PosOrganizationId organizationId, POSCustomerId customerId, int skip, int take, CancellationToken cancellationToken = default)
        {
            var list = All.Where(e => e.OrganizationId == organizationId && e.CustomerId == customerId)
                .OrderByDescending(e => e.CreatedAtUtc).ThenByDescending(e => e.Id.Value).ToList();
            return Task.FromResult(((IReadOnlyList<CreditEntry>)list.Skip(skip).Take(take).ToList(), list.Count));
        }

        public Task<(IReadOnlyList<CreditEntry> Items, int TotalCount)> ListCreatedSinceAsync(
            PosOrganizationId organizationId, DateTimeOffset? sinceUtc, int skip, int take, CancellationToken cancellationToken = default) =>
            Task.FromResult(((IReadOnlyList<CreditEntry>)Array.Empty<CreditEntry>(), 0));

        public Task<IReadOnlyList<CreditEntry>> ListActiveByOrganizationAsync(
            PosOrganizationId organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CreditEntry>>(
                All.Where(e => e.OrganizationId == organizationId && e.Status == CreditEntryStatus.Active).ToList());

        public Task<IReadOnlyList<CreditEntry>> ListRecordedInRangeAsync(
            PosOrganizationId organizationId, DateOnly fromDateUtc, DateOnly toDateUtc, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CreditEntry>>(Array.Empty<CreditEntry>());

        public Task<decimal> SumActiveAmountAsync(
            PosOrganizationId organizationId, POSCustomerId customerId, CancellationToken cancellationToken = default) =>
            Task.FromResult(All.Where(e =>
                e.OrganizationId == organizationId && e.CustomerId == customerId && e.Status == CreditEntryStatus.Active)
                .Sum(e => e.Amount));

        public Task<int> CountActiveAsync(
            PosOrganizationId organizationId, POSCustomerId customerId, CancellationToken cancellationToken = default) =>
            Task.FromResult(All.Count(e =>
                e.OrganizationId == organizationId && e.CustomerId == customerId && e.Status == CreditEntryStatus.Active));
    }

    private sealed class InMemoryRepayments : IRepaymentRepository
    {
        public List<Repayment> All { get; } = [];

        public Task AddAsync(Repayment repayment, CancellationToken cancellationToken = default)
        {
            All.Add(repayment);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Repayment repayment, CancellationToken cancellationToken = default)
        {
            var i = All.FindIndex(r => r.Id == repayment.Id);
            if (i >= 0)
            {
                All[i] = repayment;
            }

            return Task.CompletedTask;
        }

        public Task<Repayment?> GetByIdAsync(PosOrganizationId organizationId, RepaymentId repaymentId, CancellationToken cancellationToken = default) =>
            Task.FromResult(All.FirstOrDefault(r => r.OrganizationId == organizationId && r.Id == repaymentId));

        public Task<(IReadOnlyList<Repayment> Items, int TotalCount)> ListByCustomerAsync(
            PosOrganizationId organizationId, POSCustomerId customerId, int skip, int take, CancellationToken cancellationToken = default)
        {
            var list = All.Where(r => r.OrganizationId == organizationId && r.CustomerId == customerId)
                .OrderByDescending(r => r.RecordedAtUtc).ThenByDescending(r => r.Id.Value).ToList();
            return Task.FromResult(((IReadOnlyList<Repayment>)list.Skip(skip).Take(take).ToList(), list.Count));
        }

        public Task<(IReadOnlyList<Repayment> Items, int TotalCount)> ListCreatedSinceAsync(
            PosOrganizationId organizationId, DateTimeOffset? sinceUtc, int skip, int take, CancellationToken cancellationToken = default) =>
            Task.FromResult(((IReadOnlyList<Repayment>)Array.Empty<Repayment>(), 0));

        public Task<IReadOnlyList<Repayment>> ListRecordedInRangeAsync(
            PosOrganizationId organizationId, DateOnly fromDateUtc, DateOnly toDateUtc, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Repayment>>(Array.Empty<Repayment>());

        public Task<decimal> SumActiveAmountAsync(
            PosOrganizationId organizationId, POSCustomerId customerId, CancellationToken cancellationToken = default) =>
            Task.FromResult(All.Where(r =>
                r.OrganizationId == organizationId && r.CustomerId == customerId && r.Status == RepaymentStatus.Active)
                .Sum(r => r.Amount));

        public Task<IReadOnlyDictionary<Guid, decimal>> SumActiveAmountsByOrganizationAsync(
            PosOrganizationId organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<Guid, decimal>>(new Dictionary<Guid, decimal>());

        public Task<int> CountActiveAsync(
            PosOrganizationId organizationId, POSCustomerId customerId, CancellationToken cancellationToken = default) =>
            Task.FromResult(All.Count(r =>
                r.OrganizationId == organizationId && r.CustomerId == customerId && r.Status == RepaymentStatus.Active));
    }
}
