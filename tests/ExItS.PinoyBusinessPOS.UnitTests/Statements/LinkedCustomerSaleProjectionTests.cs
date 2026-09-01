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

/// <summary>
/// End-to-end-ish projection: accepted linked customer → checkout sale with that POSCustomerId
/// → Personal statement/receipt visibility. Cash purchases appear as Sale/Purchase rows;
/// Utang remains Credit/UtangCharge (no duplicate Sale row).
/// </summary>
public sealed class LinkedCustomerSaleProjectionTests
{
    private static readonly DateTimeOffset T0 = new(2026, 8, 12, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid OrgKizzy = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid OrgOther = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid MicaPlatformCustomer = Guid.Parse("cccccccc-cccc-cccc-dddd-eeeeeeeeeeee");
    private static readonly Guid OtherPlatformCustomer = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
    private static readonly Guid MicaPersonalUser = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid WrongPersonalUser = Guid.Parse("99999999-9999-9999-9999-999999999999");
    private static readonly Guid LinkedId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid Actor = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly CashierShiftId Shift = CashierShiftId.New();
    private static readonly RegisterId Register = RegisterId.New();

    [Fact]
    public async Task Cash_sale_for_linked_mica_appears_in_activity_and_receipt()
    {
        var harness = await Harness.CreateAuthorizedAsync();

        // Correlation: PlatformBusinessCustomerId on the exact POSCustomer used at checkout.
        Assert.Equal(MicaPlatformCustomer, harness.MicaPosCustomer.PlatformBusinessCustomerId);

        var sale = CashSale(harness.MicaPosCustomer.Id, 75.50m, saleNumber: "SALE-20260812-000101");
        await harness.Sales.AddAsync(sale);

        // Selected customer ID survived into sale persistence (stable id, not name).
        Assert.Equal(harness.MicaPosCustomer.Id, sale.CustomerId);
        Assert.Null(sale.LinkedCreditEntryId);

        var activity = await harness.Activity.ExecuteAsync(OrgKizzy, MicaPlatformCustomer);
        Assert.True(activity.IsSuccess, activity.ErrorMessage);
        var purchase = Assert.Single(activity.Value!.Items);
        Assert.Equal("Purchase", purchase.Type);
        Assert.Equal(75.50m, purchase.PaymentAmount);
        Assert.Null(purchase.ChargeAmount);
        Assert.True(purchase.HasDetails);
        Assert.Equal(sale.Id.Value, purchase.SourceSaleId);
        // Cash does not change outstanding walk (SignedEffect = 0).
        Assert.Equal(0m, purchase.BalanceAfter);

        var receipt = await harness.Receipt.ExecuteAsync(OrgKizzy, MicaPlatformCustomer, sale.Id.Value);
        Assert.True(receipt.IsSuccess, receipt.ErrorMessage);
        Assert.Equal("Cash", receipt.Value!.PaymentMethod);
        Assert.Equal(75.50m, receipt.Value.Total);
        Assert.Equal(75.50m, receipt.Value.PaidAmount);
        Assert.Null(receipt.Value.UtangAmount);
        Assert.Equal(0m, receipt.Value.OutstandingEffect);
        Assert.Equal(harness.MicaPosCustomer.Id.Value, receipt.Value.PosCustomerId);
    }

    [Fact]
    public async Task Utang_sale_appears_as_credit_activity_with_debt_and_receipt_not_duplicate_sale_row()
    {
        var harness = await Harness.CreateAuthorizedAsync();
        var creditId = CreditEntryId.New();
        var sale = UtangSale(harness.MicaPosCustomer.Id, 120m, creditId);
        await harness.Sales.AddAsync(sale);
        await harness.Credits.AddAsync(CreditEntry.Create(
            PosOrganizationId.From(OrgKizzy),
            harness.MicaPosCustomer.Id,
            120m,
            "Goods",
            T0,
            id: creditId,
            sourceSaleId: sale.Id));

        var summary = await harness.Summary.ExecuteAsync(OrgKizzy, MicaPlatformCustomer);
        Assert.True(summary.IsSuccess);
        Assert.Equal(120m, summary.Value!.OutstandingBalance);

        var activity = await harness.Activity.ExecuteAsync(OrgKizzy, MicaPlatformCustomer);
        Assert.True(activity.IsSuccess, activity.ErrorMessage);
        var charge = Assert.Single(activity.Value!.Items);
        Assert.Equal("UtangCharge", charge.Type);
        Assert.Equal(120m, charge.ChargeAmount);
        Assert.Equal(sale.Id.Value, charge.SourceSaleId);
        Assert.DoesNotContain(activity.Value.Items, i => i.Type is "Purchase");

        var receipt = await harness.Receipt.ExecuteAsync(OrgKizzy, MicaPlatformCustomer, sale.Id.Value);
        Assert.True(receipt.IsSuccess, receipt.ErrorMessage);
        Assert.Equal("Utang", receipt.Value!.PaymentMethod);
        Assert.Equal(120m, receipt.Value.UtangAmount);
        Assert.Equal(120m, receipt.Value.OutstandingEffect);
    }

    [Fact]
    public async Task Mixed_cash_and_utang_keeps_outstanding_walk_stable_across_purchase()
    {
        var harness = await Harness.CreateAuthorizedAsync();
        await harness.Credits.AddAsync(CreditEntry.Create(
            PosOrganizationId.From(OrgKizzy), harness.MicaPosCustomer.Id, 100m, "Prior", T0));
        var cash = CashSale(harness.MicaPosCustomer.Id, 40m, recordedAt: T0.AddHours(1));
        await harness.Sales.AddAsync(cash);

        var activity = await harness.Activity.ExecuteAsync(OrgKizzy, MicaPlatformCustomer);
        Assert.True(activity.IsSuccess);
        Assert.Equal(2, activity.Value!.Items.Count);
        Assert.Equal("Purchase", activity.Value.Items[0].Type);
        Assert.Equal(100m, activity.Value.Items[0].BalanceAfter); // cash SignedEffect 0
        Assert.Equal("UtangCharge", activity.Value.Items[1].Type);
        Assert.Equal(100m, activity.Value.Items[1].BalanceAfter);
    }

    [Fact]
    public async Task Sale_without_customer_does_not_appear()
    {
        var harness = await Harness.CreateAuthorizedAsync();
        var anonymous = CashSale(customerId: null, total: 33m);
        await harness.Sales.AddAsync(anonymous);

        var activity = await harness.Activity.ExecuteAsync(OrgKizzy, MicaPlatformCustomer);
        Assert.True(activity.IsSuccess);
        Assert.Empty(activity.Value!.Items);

        var receipt = await harness.Receipt.ExecuteAsync(OrgKizzy, MicaPlatformCustomer, anonymous.Id.Value);
        Assert.False(receipt.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.ReceiptNotFound, receipt.ErrorCode);
    }

    [Fact]
    public async Task Sale_for_duplicate_name_other_customer_does_not_match()
    {
        var harness = await Harness.CreateAuthorizedAsync();
        var impostor = POSCustomer.Create(
            PosOrganizationId.From(OrgKizzy),
            "Mica Same Name",
            T0);
        await harness.Customers.AddAsync(impostor);
        var sale = CashSale(impostor.Id, 55m);
        await harness.Sales.AddAsync(sale);

        var activity = await harness.Activity.ExecuteAsync(OrgKizzy, MicaPlatformCustomer);
        Assert.Empty(activity.Value!.Items);

        var receipt = await harness.Receipt.ExecuteAsync(OrgKizzy, MicaPlatformCustomer, sale.Id.Value);
        Assert.Equal(ApplicationErrorCodes.ReceiptNotFound, receipt.ErrorCode);
    }

    [Fact]
    public async Task Sale_from_other_organization_does_not_appear()
    {
        var harness = await Harness.CreateAuthorizedAsync();
        var otherOrgCustomer = POSCustomer.Create(
            PosOrganizationId.From(OrgOther),
            "Mica",
            T0,
            platformBusinessCustomerId: MicaPlatformCustomer);
        await harness.Customers.AddAsync(otherOrgCustomer);
        var sale = Sale.Checkout(
            PosOrganizationId.From(OrgOther),
            "SALE-20260812-000201",
            SalePaymentMethod.Cash,
            [Line(20m)],
            Actor,
            T0,
            amountTendered: 20m,
            customerId: otherOrgCustomer.Id,
            cashierShiftId: Shift,
            registerId: Register);
        await harness.Sales.AddAsync(sale);

        var activity = await harness.Activity.ExecuteAsync(OrgKizzy, MicaPlatformCustomer);
        Assert.Empty(activity.Value!.Items);
    }

    [Fact]
    public async Task Wrong_personal_user_denied_sees_no_data()
    {
        var harness = await Harness.CreateAsync(FakePlatform.Denied());
        var sale = CashSale(harness.MicaPosCustomer.Id, 10m);
        await harness.Sales.AddAsync(sale);

        Assert.Equal(
            ApplicationErrorCodes.LinkedCustomerDenied,
            (await harness.Activity.ExecuteAsync(OrgKizzy, MicaPlatformCustomer)).ErrorCode);
        Assert.Equal(
            ApplicationErrorCodes.LinkedCustomerDenied,
            (await harness.Receipt.ExecuteAsync(OrgKizzy, MicaPlatformCustomer, sale.Id.Value)).ErrorCode);
    }

    [Fact]
    public async Task Declined_or_revoked_link_not_found_sees_no_data()
    {
        var harness = await Harness.CreateAsync(FakePlatform.NotFound());
        var sale = CashSale(harness.MicaPosCustomer.Id, 10m);
        await harness.Sales.AddAsync(sale);

        Assert.Equal(
            ApplicationErrorCodes.LinkedCustomerNotFound,
            (await harness.Activity.ExecuteAsync(OrgKizzy, MicaPlatformCustomer)).ErrorCode);
        Assert.Equal(
            ApplicationErrorCodes.LinkedCustomerNotFound,
            (await harness.Receipt.ExecuteAsync(OrgKizzy, MicaPlatformCustomer, sale.Id.Value)).ErrorCode);
    }

    [Fact]
    public async Task Wrong_platform_business_customer_sees_no_sale()
    {
        var harness = await Harness.CreateAuthorizedAsync();
        var sale = CashSale(harness.MicaPosCustomer.Id, 10m);
        await harness.Sales.AddAsync(sale);

        Assert.Equal(
            ApplicationErrorCodes.LinkedCustomerNotFound,
            (await harness.Activity.ExecuteAsync(OrgKizzy, OtherPlatformCustomer)).ErrorCode);
    }

    [Fact]
    public async Task Older_settled_cash_purchase_still_requires_entitlement_outside_free_window()
    {
        var harness = await Harness.CreateAuthorizedAsync();
        harness.Clock.Set(new DateTimeOffset(2026, 8, 13, 0, 0, 0, TimeSpan.Zero));
        var old = CashSale(
            harness.MicaPosCustomer.Id,
            25m,
            recordedAt: new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        await harness.Sales.AddAsync(old);

        var older = await harness.Older.ExecuteAsync(OrgKizzy, MicaPlatformCustomer);
        Assert.Equal(ApplicationErrorCodes.ExtendedHistoryRequired, older.ErrorCode);

        harness.Entitlements.Active = true;
        older = await harness.Older.ExecuteAsync(OrgKizzy, MicaPlatformCustomer);
        Assert.True(older.IsSuccess, older.ErrorMessage);
        var item = Assert.Single(older.Value!.Items);
        Assert.Equal("Purchase", item.Type);
        Assert.Equal(old.Id.Value, item.SourceSaleId);
    }

    [Fact]
    public async Task Open_debt_list_excludes_cash_purchases()
    {
        var harness = await Harness.CreateAuthorizedAsync();
        await harness.Credits.AddAsync(CreditEntry.Create(
            PosOrganizationId.From(OrgKizzy), harness.MicaPosCustomer.Id, 80m, "Debt", T0));
        await harness.Sales.AddAsync(CashSale(harness.MicaPosCustomer.Id, 15m, recordedAt: T0.AddHours(1)));

        var openDebt = await harness.OpenDebt.ExecuteAsync(OrgKizzy, MicaPlatformCustomer);
        Assert.True(openDebt.IsSuccess);
        var item = Assert.Single(openDebt.Value!.Items);
        Assert.Equal("UtangCharge", item.Type);
        Assert.DoesNotContain(openDebt.Value.Items, i => i.Type is "Purchase");
    }

    private static SaleLineDraft Line(decimal unitPrice, decimal qty = 1m) =>
        new(CatalogProductId.New(), "Pork", "SKU-P", null, UnitOfMeasure.Kilogram, unitPrice, qty, SellingMode.ByWeight);

    private static Sale CashSale(
        POSCustomerId? customerId,
        decimal total,
        string? saleNumber = null,
        DateTimeOffset? recordedAt = null) =>
        Sale.Checkout(
            PosOrganizationId.From(OrgKizzy),
            saleNumber ?? SaleNumbers.Format(new DateOnly(2026, 8, 12), 1),
            SalePaymentMethod.Cash,
            [Line(total)],
            Actor,
            recordedAt ?? T0,
            amountTendered: total,
            customerId: customerId,
            cashierShiftId: Shift,
            registerId: Register);

    private static Sale UtangSale(POSCustomerId customerId, decimal total, CreditEntryId creditId) =>
        Sale.Checkout(
            PosOrganizationId.From(OrgKizzy),
            SaleNumbers.Format(new DateOnly(2026, 8, 12), 2),
            SalePaymentMethod.Utang,
            [Line(total)],
            Actor,
            T0.AddMinutes(30),
            amountTendered: null,
            customerId: customerId,
            linkedCreditEntryId: creditId,
            cashierShiftId: Shift,
            registerId: Register);

    private sealed class Harness
    {
        public required POSCustomer MicaPosCustomer { get; init; }
        public required InMemoryCustomers Customers { get; init; }
        public required InMemorySales Sales { get; init; }
        public required InMemoryCredits Credits { get; init; }
        public required InMemoryRepayments Repayments { get; init; }
        public required GetLinkedCustomerStatementSummary Summary { get; init; }
        public required ListLinkedCustomerRecentActivity Activity { get; init; }
        public required ListLinkedCustomerOpenDebtActivity OpenDebt { get; init; }
        public required ListLinkedCustomerOlderSettledActivity Older { get; init; }
        public required GetLinkedCustomerSaleReceipt Receipt { get; init; }
        public required FakeEntitlements Entitlements { get; init; }
        public required MutableClock Clock { get; init; }

        public static async Task<Harness> CreateAuthorizedAsync() =>
            await CreateAsync(FakePlatform.Authorized());

        public static async Task<Harness> CreateAsync(ILinkedCustomerPlatformAuthorization platform)
        {
            var customers = new InMemoryCustomers();
            var sales = new InMemorySales();
            var credits = new InMemoryCredits();
            var repayments = new InMemoryRepayments();
            var clock = new MutableClock(T0.AddDays(1));
            var outstanding = new OutstandingBalanceService(credits, repayments, new InMemoryWriteOffRepository(), clock);
            var entitlements = new FakeEntitlements(active: false);
            var options = Microsoft.Extensions.Options.Options.Create(new PersonalStatementsOptions { FreeRecentMonths = 3 });

            var mica = POSCustomer.Create(
                PosOrganizationId.From(OrgKizzy),
                "Mica",
                T0,
                platformBusinessCustomerId: MicaPlatformCustomer);
            await customers.AddAsync(mica);

            var authorize = new AuthorizeLinkedCustomerStatementAccess(platform, customers);
            var activityQuery = new InMemoryRecentActivity(credits, repayments, sales);
            return new Harness
            {
                MicaPosCustomer = mica,
                Customers = customers,
                Sales = sales,
                Credits = credits,
                Repayments = repayments,
                Summary = new GetLinkedCustomerStatementSummary(authorize, customers, outstanding, clock),
                Activity = new ListLinkedCustomerRecentActivity(
                    authorize, activityQuery, outstanding, entitlements, options, clock),
                OpenDebt = new ListLinkedCustomerOpenDebtActivity(authorize, activityQuery, outstanding),
                Older = new ListLinkedCustomerOlderSettledActivity(
                    authorize, activityQuery, entitlements, options, clock),
                Receipt = new GetLinkedCustomerSaleReceipt(
                    authorize, sales, credits, outstanding, entitlements, options, clock),
                Entitlements = entitlements,
                Clock = clock
            };
        }
    }

    private sealed class FakeEntitlements(bool active) : IPersonalFeatureEntitlementClient
    {
        public bool Active { get; set; } = active;

        public Task<bool> HasActiveEntitlementAsync(string featureCode, CancellationToken cancellationToken = default) =>
            Task.FromResult(Active);
    }

    private sealed class MutableClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow { get; private set; } = utcNow;

        public void Set(DateTimeOffset utcNow) => UtcNow = utcNow;
    }

    private sealed class FakePlatform : ILinkedCustomerPlatformAuthorization
    {
        private readonly LinkedCustomerPlatformAuthorizationResult _result;

        private FakePlatform(LinkedCustomerPlatformAuthorizationResult result) => _result = result;

        public static FakePlatform Authorized() => new(
            new LinkedCustomerPlatformAuthorizationResult(
                LinkedCustomerPlatformAuthorizationOutcome.Authorized,
                new LinkedCustomerPlatformAuthorizationProof(
                    MicaPersonalUser, OrgKizzy, MicaPlatformCustomer, LinkedId)));

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

            _ = WrongPersonalUser; // documents wrong-user isolation is Platform-side Denied/NotFound
            return Task.FromResult(_result);
        }
    }

    private sealed class InMemoryRecentActivity(
        InMemoryCredits credits,
        InMemoryRepayments repayments,
        InMemorySales sales) : ILinkedCustomerRecentActivityQuery
    {
        public Task<IReadOnlyList<LinkedCustomerActivityRawRow>> ListRecentDescendingAsync(
            PosOrganizationId organizationId,
            POSCustomerId customerId,
            int skip,
            int take,
            DateTimeOffset? notBeforeUtc = null,
            DateTimeOffset? beforeUtc = null,
            CancellationToken cancellationToken = default)
        {
            take = Math.Min(take, LinkedCustomerStatementLimits.MaxPageSize + 1);
            var rows = credits.All
                .Where(c => c.OrganizationId == organizationId && c.CustomerId == customerId)
                .Where(c => notBeforeUtc is null || c.CreatedAtUtc >= notBeforeUtc)
                .Where(c => beforeUtc is null || c.CreatedAtUtc < beforeUtc)
                .Select(c => new LinkedCustomerActivityRawRow(
                    c.Id.Value,
                    "Credit",
                    c.Amount,
                    c.Status == CreditEntryStatus.Active ? c.Amount : 0m,
                    c.Status.ToString(),
                    c.CreatedAtUtc,
                    c.SourceSaleId?.Value))
                .Concat(repayments.All
                    .Where(r => r.OrganizationId == organizationId && r.CustomerId == customerId)
                    .Where(r => notBeforeUtc is null || r.RecordedAtUtc >= notBeforeUtc)
                    .Where(r => beforeUtc is null || r.RecordedAtUtc < beforeUtc)
                    .Select(r => new LinkedCustomerActivityRawRow(
                        r.Id.Value,
                        "Repayment",
                        r.Amount,
                        r.Status == RepaymentStatus.Active ? -r.Amount : 0m,
                        r.Status.ToString(),
                        r.RecordedAtUtc,
                        null)))
                .Concat(sales.All
                    .Where(s => s.OrganizationId == organizationId
                                && s.CustomerId == customerId
                                && s.PaymentMethod != SalePaymentMethod.Utang
                                && s.LinkedCreditEntryId is null
                                && s.Status is SaleStatus.Completed or SaleStatus.Voided)
                    .Where(s => notBeforeUtc is null || s.RecordedAtUtc >= notBeforeUtc)
                    .Where(s => beforeUtc is null || s.RecordedAtUtc < beforeUtc)
                    .Select(s => new LinkedCustomerActivityRawRow(
                        s.Id.Value,
                        "Sale",
                        s.Total,
                        0m,
                        s.Status.ToString(),
                        s.RecordedAtUtc,
                        s.Id.Value)))
                .OrderByDescending(r => r.RecordedAtUtc)
                .ThenByDescending(r => r.EntryId)
                .Skip(Math.Max(skip, 0))
                .Take(take)
                .ToList();
            return Task.FromResult<IReadOnlyList<LinkedCustomerActivityRawRow>>(rows);
        }

        public Task<IReadOnlyList<LinkedCustomerActivityRawRow>> ListActiveDescendingAsync(
            PosOrganizationId organizationId,
            POSCustomerId customerId,
            int skip,
            int take,
            CancellationToken cancellationToken = default)
        {
            take = Math.Min(take, LinkedCustomerStatementLimits.MaxPageSize + 1);
            var rows = credits.All
                .Where(c => c.OrganizationId == organizationId && c.CustomerId == customerId && c.Status == CreditEntryStatus.Active)
                .Select(c => new LinkedCustomerActivityRawRow(
                    c.Id.Value,
                    "Credit",
                    c.Amount,
                    c.Amount,
                    c.Status.ToString(),
                    c.CreatedAtUtc,
                    c.SourceSaleId?.Value))
                .Concat(repayments.All
                    .Where(r => r.OrganizationId == organizationId && r.CustomerId == customerId && r.Status == RepaymentStatus.Active)
                    .Select(r => new LinkedCustomerActivityRawRow(
                        r.Id.Value,
                        "Repayment",
                        r.Amount,
                        -r.Amount,
                        r.Status.ToString(),
                        r.RecordedAtUtc,
                        null)))
                .OrderByDescending(r => r.RecordedAtUtc)
                .ThenByDescending(r => r.EntryId)
                .Skip(Math.Max(skip, 0))
                .Take(take)
                .ToList();
            return Task.FromResult<IReadOnlyList<LinkedCustomerActivityRawRow>>(rows);
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
            PosOrganizationId organizationId, CustomerStatus? status, string? search, int skip, int take, IReadOnlyCollection<Guid>? restrictToCustomerIds = null, CancellationToken cancellationToken = default)
        {
            var list = _items.Where(c => c.OrganizationId == organizationId).ToList();
            return Task.FromResult(((IReadOnlyList<POSCustomer>)list.Skip(skip).Take(take).ToList(), list.Count));
        }

        public Task<(IReadOnlyList<POSCustomer> Items, int TotalCount)> ListUpdatedSinceAsync(
            PosOrganizationId organizationId, DateTimeOffset? sinceUtc, int skip, int take, CancellationToken cancellationToken = default) =>
            ListAsync(organizationId, null, null, skip, take, null, cancellationToken);

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
        public IReadOnlyList<Sale> All => _items;

        public Task AddAsync(Sale sale, CancellationToken cancellationToken = default)
        {
            _items.Add(sale);
            return Task.CompletedTask;
        }

        public Task<string> ReserveNextSaleNumberAsync(
            PosOrganizationId organizationId,
            DateOnly businessDateUtc,
            CancellationToken cancellationToken = default) =>
            Task.FromResult($"S-{businessDateUtc:yyyyMMdd}-001");

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
            Guid? branchId = null,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlySet<Guid>> ListSaleIdsInBranchAsync(
            PosOrganizationId organizationId,
            IReadOnlyCollection<Guid> saleIds,
            Guid branchId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlySet<Guid>>(new HashSet<Guid>());

        public Task<SalePeriodAggregate> AggregatePeriodAsync(PosOrganizationId organizationId, DateOnly fromDateUtc, DateOnly toDateUtc, SaleStatus? status = null, SalePaymentMethod? paymentMethod = null, Guid? customerId = null, Guid? branchId = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<SaleCostPeriodAggregate> AggregateCostForProfitabilityAsync(PosOrganizationId organizationId, DateOnly fromDateUtc, DateOnly toDateUtc, Guid? branchId = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<IReadOnlyList<ProductProfitabilitySaleAggregate>> AggregateProductProfitabilitySalesAsync(
            PosOrganizationId organizationId,
            DateOnly fromDateUtc,
            DateOnly toDateUtc,
            Guid? branchId = null,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();


        public Task<IReadOnlyList<SalePaymentAggregate>> AggregateCompletedByPaymentAsync(PosOrganizationId organizationId, DateOnly fromDateUtc, DateOnly toDateUtc, Guid? branchId = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<IReadOnlyList<SaleDailyAggregate>> AggregateCompletedByDayAsync(PosOrganizationId organizationId, DateOnly fromDateUtc, DateOnly toDateUtc, Guid? branchId = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<Sale> CheckoutAsync(
            PosOrganizationId organizationId,
            DateOnly businessDateUtc,
            Func<string, Sale> createSale,
            Func<Sale, CancellationToken, Task>? afterSaleCreated = null,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task UpdateAsync(Sale sale, CancellationToken cancellationToken = default)
        {
            var i = _items.FindIndex(s => s.Id == sale.Id);
            if (i >= 0)
            {
                _items[i] = sale;
            }

            return Task.CompletedTask;
        }

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
