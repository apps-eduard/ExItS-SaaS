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

/// <summary>P24-WP12: entitlement fail-closed + authz/privacy edge regressions.</summary>
public sealed class P24Wp12HistorySecurityRegressionTests
{
    private static readonly DateTimeOffset T0 = new(2026, 8, 12, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid OrgA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid PlatformCustomer = Guid.Parse("cccccccc-cccc-cccc-dddd-eeeeeeeeeeee");
    private static readonly Guid PersonalUser = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid LinkedId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid Actor = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly CashierShiftId Shift = CashierShiftId.New();
    private static readonly RegisterId Register = RegisterId.New();

    [Fact]
    public async Task Entitlement_client_fail_closed_keeps_old_settled_locked()
    {
        var harness = await StatementHarness.CreateAuthorizedAsync(entitled: null);
        await harness.Credits.AddAsync(CreditEntry.Create(
            PosOrganizationId.From(OrgA), harness.PosCustomer.Id, 100m, "Old",
            new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero)));
        await harness.Repayments.AddAsync(Repayment.Create(
            PosOrganizationId.From(OrgA), harness.PosCustomer.Id, 100m, "Paid", Actor,
            new DateTimeOffset(2026, 1, 20, 0, 0, 0, TimeSpan.Zero)));

        var activity = await harness.Activity.ExecuteAsync(OrgA, PlatformCustomer);
        Assert.True(activity.IsSuccess);
        Assert.False(activity.Value!.CanAccessExtendedHistory);
        Assert.Empty(activity.Value.Items);

        var older = await harness.Older.ExecuteAsync(OrgA, PlatformCustomer);
        Assert.False(older.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.ExtendedHistoryRequired, older.ErrorCode);
    }

    [Fact]
    public async Task Month_boundary_just_outside_window_is_locked_for_free_user()
    {
        var harness = await StatementHarness.CreateAuthorizedAsync(entitled: false);
        // Clock 2026-08-13 → free starts 2026-06-01; 2026-05-31 23:59:59 is outside.
        await harness.Credits.AddAsync(CreditEntry.Create(
            PosOrganizationId.From(OrgA), harness.PosCustomer.Id, 9m, "Boundary",
            new DateTimeOffset(2026, 5, 31, 23, 59, 59, TimeSpan.Zero)));
        await harness.Credits.AddAsync(CreditEntry.Create(
            PosOrganizationId.From(OrgA), harness.PosCustomer.Id, 11m, "Inside",
            new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero)));

        var result = await harness.Activity.ExecuteAsync(OrgA, PlatformCustomer);
        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Items);
        Assert.Equal(11m, result.Value.Items[0].ChargeAmount);
    }

    [Fact]
    public async Task Zero_outstanding_removes_receipt_open_debt_exception()
    {
        var harness = await ReceiptHarness.CreateAuthorizedAsync();
        var creditId = CreditEntryId.New();
        var sale = Sale.Checkout(
            PosOrganizationId.From(OrgA),
            SaleNumbers.Format(new DateOnly(2025, 1, 10), 1),
            SalePaymentMethod.Utang,
            [new SaleLineDraft(CatalogProductId.New(), "Old", null, null, UnitOfMeasure.Piece, 40m, 1m)],
            Actor,
            new DateTimeOffset(2025, 1, 10, 0, 0, 0, TimeSpan.Zero),
            amountTendered: null,
            customerId: harness.PosCustomer.Id,
            linkedCreditEntryId: creditId,
            cashierShiftId: Shift,
            registerId: Register);
        await harness.Sales.AddAsync(sale);
        await harness.Credits.AddAsync(CreditEntry.Create(
            PosOrganizationId.From(OrgA), harness.PosCustomer.Id, 40m, "Goods",
            sale.RecordedAtUtc, id: creditId, sourceSaleId: sale.Id));

        Assert.True((await harness.Receipt.ExecuteAsync(OrgA, PlatformCustomer, sale.Id.Value)).IsSuccess);

        await harness.Repayments.AddAsync(Repayment.Create(
            PosOrganizationId.From(OrgA), harness.PosCustomer.Id, 40m, "Settle", Actor,
            new DateTimeOffset(2025, 2, 1, 0, 0, 0, TimeSpan.Zero)));

        var after = await harness.Receipt.ExecuteAsync(OrgA, PlatformCustomer, sale.Id.Value);
        Assert.False(after.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.ExtendedHistoryRequired, after.ErrorCode);
    }

    [Fact]
    public void Privacy_dto_denylist_holds_for_activity_and_receipt()
    {
        Assert.DoesNotContain(
            typeof(LinkedCustomerActivityItemDto).GetProperties().Select(p => p.Name),
            name => name is "Cost" or "Margin" or "Remarks" or "RecordedBy" or "StaffNotes" or "Lines");
        Assert.DoesNotContain(
            typeof(LinkedCustomerSaleReceiptDto).GetProperties().Select(p => p.Name),
            name => name is "Cost" or "Margin" or "Remarks" or "RecordedBy" or "RegisterId" or "VoidReason");
        Assert.DoesNotContain(
            typeof(LinkedCustomerStatementSummaryDto).GetProperties().Select(p => p.Name),
            name => name is "Cost" or "Margin" or "InternalNotes" or "StaffId");
    }

    [Fact]
    public void Max_page_size_remains_twenty_for_activity_and_older()
    {
        Assert.Equal(20, LinkedCustomerStatementLimits.MaxPageSize);
        Assert.Equal(20, LinkedCustomerStatementLimits.NormalizePageSize(100));
        Assert.Equal(1, LinkedCustomerStatementLimits.NormalizePageSize(0));
    }

    private sealed class FailClosedEntitlements : IPersonalFeatureEntitlementClient
    {
        public Task<bool> HasActiveEntitlementAsync(string featureCode, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);
    }

    private sealed class FixedEntitlements(bool active) : IPersonalFeatureEntitlementClient
    {
        public Task<bool> HasActiveEntitlementAsync(string featureCode, CancellationToken cancellationToken = default) =>
            Task.FromResult(active);
    }

    private sealed class StatementHarness
    {
        public required POSCustomer PosCustomer { get; init; }
        public required InMemoryCredits Credits { get; init; }
        public required InMemoryRepayments Repayments { get; init; }
        public required ListLinkedCustomerRecentActivity Activity { get; init; }
        public required ListLinkedCustomerOlderSettledActivity Older { get; init; }

        public static async Task<StatementHarness> CreateAuthorizedAsync(bool? entitled)
        {
            var customers = new InMemoryCustomers();
            var credits = new InMemoryCredits();
            var repayments = new InMemoryRepayments();
            var clock = new FixedClock(T0.AddDays(1));
            var outstanding = new OutstandingBalanceService(credits, repayments, clock);
            var posCustomer = POSCustomer.Create(
                PosOrganizationId.From(OrgA), "Rosa", T0, platformBusinessCustomerId: PlatformCustomer);
            await customers.AddAsync(posCustomer);
            var authorize = new AuthorizeLinkedCustomerStatementAccess(FakePlatform.Authorized(), customers);
            var activityQuery = new InMemoryRecentActivity(credits, repayments);
            IPersonalFeatureEntitlementClient entitlements = entitled is null
                ? new FailClosedEntitlements()
                : new FixedEntitlements(entitled.Value);
            var options = Microsoft.Extensions.Options.Options.Create(new PersonalStatementsOptions { FreeRecentMonths = 3 });
            return new StatementHarness
            {
                PosCustomer = posCustomer,
                Credits = credits,
                Repayments = repayments,
                Activity = new ListLinkedCustomerRecentActivity(
                    authorize, activityQuery, outstanding, entitlements, options, clock),
                Older = new ListLinkedCustomerOlderSettledActivity(
                    authorize, activityQuery, entitlements, options, clock)
            };
        }
    }

    private sealed class ReceiptHarness
    {
        public required POSCustomer PosCustomer { get; init; }
        public required InMemorySales Sales { get; init; }
        public required InMemoryCredits Credits { get; init; }
        public required InMemoryRepayments Repayments { get; init; }
        public required GetLinkedCustomerSaleReceipt Receipt { get; init; }

        public static async Task<ReceiptHarness> CreateAuthorizedAsync()
        {
            var customers = new InMemoryCustomers();
            var sales = new InMemorySales();
            var credits = new InMemoryCredits();
            var repayments = new InMemoryRepayments();
            var clock = new FixedClock(T0.AddDays(1));
            var outstanding = new OutstandingBalanceService(credits, repayments, clock);
            var entitlements = new FailClosedEntitlements();
            var options = Microsoft.Extensions.Options.Options.Create(new PersonalStatementsOptions { FreeRecentMonths = 3 });
            var posCustomer = POSCustomer.Create(
                PosOrganizationId.From(OrgA), "Rosa", T0, platformBusinessCustomerId: PlatformCustomer);
            await customers.AddAsync(posCustomer);
            var authorize = new AuthorizeLinkedCustomerStatementAccess(FakePlatform.Authorized(), customers);
            return new ReceiptHarness
            {
                PosCustomer = posCustomer,
                Sales = sales,
                Credits = credits,
                Repayments = repayments,
                Receipt = new GetLinkedCustomerSaleReceipt(
                    authorize, sales, credits, outstanding, entitlements, options, clock)
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

        public Task<LinkedCustomerPlatformAuthorizationResult> VerifyAsync(
            Guid organizationId,
            Guid platformBusinessCustomerId,
            CancellationToken cancellationToken = default)
        {
            if (_result.Proof!.OrganizationId != organizationId
                || _result.Proof.PlatformBusinessCustomerId != platformBusinessCustomerId)
            {
                return Task.FromResult(new LinkedCustomerPlatformAuthorizationResult(
                    LinkedCustomerPlatformAuthorizationOutcome.NotFound, null));
            }

            return Task.FromResult(_result);
        }
    }

    private sealed class FixedClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
    }

    private sealed class InMemoryRecentActivity(InMemoryCredits credits, InMemoryRepayments repayments)
        : ILinkedCustomerRecentActivityQuery
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
                    c.Id.Value, "Credit", c.Amount,
                    c.Status == CreditEntryStatus.Active ? c.Amount : 0m,
                    c.Status.ToString(), c.CreatedAtUtc, c.SourceSaleId?.Value))
                .Concat(repayments.All
                    .Where(r => r.OrganizationId == organizationId && r.CustomerId == customerId)
                    .Where(r => notBeforeUtc is null || r.RecordedAtUtc >= notBeforeUtc)
                    .Where(r => beforeUtc is null || r.RecordedAtUtc < beforeUtc)
                    .Select(r => new LinkedCustomerActivityRawRow(
                        r.Id.Value, "Repayment", r.Amount,
                        r.Status == RepaymentStatus.Active ? -r.Amount : 0m,
                        r.Status.ToString(), r.RecordedAtUtc, null)))
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
                .Where(c => c.OrganizationId == organizationId && c.CustomerId == customerId
                            && c.Status == CreditEntryStatus.Active)
                .Select(c => new LinkedCustomerActivityRawRow(
                    c.Id.Value, "Credit", c.Amount, c.Amount, c.Status.ToString(),
                    c.CreatedAtUtc, c.SourceSaleId?.Value))
                .Concat(repayments.All
                    .Where(r => r.OrganizationId == organizationId && r.CustomerId == customerId
                                && r.Status == RepaymentStatus.Active)
                    .Select(r => new LinkedCustomerActivityRawRow(
                        r.Id.Value, "Repayment", r.Amount, -r.Amount, r.Status.ToString(),
                        r.RecordedAtUtc, null)))
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

        public Task UpdateAsync(POSCustomer customer, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<POSCustomer?> GetByIdAsync(
            PosOrganizationId organizationId, POSCustomerId customerId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(c => c.OrganizationId == organizationId && c.Id == customerId));

        public Task<POSCustomer?> FindActiveByNormalizedMobileAsync(
            PosOrganizationId organizationId, string normalizedMobile, CancellationToken cancellationToken = default) =>
            Task.FromResult<POSCustomer?>(null);

        public Task<POSCustomer?> FindByPlatformBusinessCustomerIdAsync(
            PosOrganizationId organizationId, Guid platformBusinessCustomerId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(c =>
                c.OrganizationId == organizationId && c.PlatformBusinessCustomerId == platformBusinessCustomerId));

        public Task<int> CountByPlatformBusinessCustomerIdAsync(
            PosOrganizationId organizationId, Guid platformBusinessCustomerId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.Count(c =>
                c.OrganizationId == organizationId && c.PlatformBusinessCustomerId == platformBusinessCustomerId));

        public Task<(IReadOnlyList<POSCustomer> Items, int TotalCount)> ListAsync(
            PosOrganizationId organizationId, CustomerStatus? status, string? search, int skip, int take,
            CancellationToken cancellationToken = default)
        {
            var list = _items.Where(c => c.OrganizationId == organizationId).ToList();
            return Task.FromResult(((IReadOnlyList<POSCustomer>)list.Skip(skip).Take(take).ToList(), list.Count));
        }

        public Task<(IReadOnlyList<POSCustomer> Items, int TotalCount)> ListUpdatedSinceAsync(
            PosOrganizationId organizationId, DateTimeOffset? sinceUtc, int skip, int take,
            CancellationToken cancellationToken = default) =>
            ListAsync(organizationId, null, null, skip, take, cancellationToken);

        public Task<IReadOnlyList<POSCustomer>> ListByIdsAsync(
            PosOrganizationId organizationId, IReadOnlyCollection<POSCustomerId> customerIds,
            CancellationToken cancellationToken = default)
        {
            var ids = customerIds.Select(c => c.Value).ToHashSet();
            return Task.FromResult<IReadOnlyList<POSCustomer>>(
                _items.Where(c => c.OrganizationId == organizationId && ids.Contains(c.Id.Value)).ToList());
        }
    }

    private sealed class InMemoryCredits : ICreditEntryRepository
    {
        public List<CreditEntry> All { get; } = [];

        public Task AddAsync(CreditEntry entry, CancellationToken cancellationToken = default)
        {
            All.Add(entry);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(CreditEntry entry, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<CreditEntry?> GetByIdAsync(
            PosOrganizationId organizationId, POSCustomerId customerId, CreditEntryId entryId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(All.FirstOrDefault(e =>
                e.OrganizationId == organizationId && e.CustomerId == customerId && e.Id == entryId));

        public Task<CreditEntry?> GetByIdForOrganizationAsync(
            PosOrganizationId organizationId, CreditEntryId entryId, CancellationToken cancellationToken = default) =>
            Task.FromResult(All.FirstOrDefault(e => e.OrganizationId == organizationId && e.Id == entryId));

        public Task<(IReadOnlyList<CreditEntry> Items, int TotalCount)> ListByCustomerAsync(
            PosOrganizationId organizationId, POSCustomerId customerId, int skip, int take,
            CancellationToken cancellationToken = default)
        {
            var list = All.Where(e => e.OrganizationId == organizationId && e.CustomerId == customerId).ToList();
            return Task.FromResult(((IReadOnlyList<CreditEntry>)list.Skip(skip).Take(take).ToList(), list.Count));
        }

        public Task<(IReadOnlyList<CreditEntry> Items, int TotalCount)> ListCreatedSinceAsync(
            PosOrganizationId organizationId, DateTimeOffset? sinceUtc, int skip, int take,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(((IReadOnlyList<CreditEntry>)Array.Empty<CreditEntry>(), 0));

        public Task<IReadOnlyList<CreditEntry>> ListActiveByOrganizationAsync(
            PosOrganizationId organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CreditEntry>>(
                All.Where(e => e.OrganizationId == organizationId && e.Status == CreditEntryStatus.Active).ToList());

        public Task<IReadOnlyList<CreditEntry>> ListRecordedInRangeAsync(
            PosOrganizationId organizationId, DateOnly fromDateUtc, DateOnly toDateUtc,
            CancellationToken cancellationToken = default) =>
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

        public Task UpdateAsync(Repayment repayment, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<Repayment?> GetByIdAsync(
            PosOrganizationId organizationId, RepaymentId repaymentId, CancellationToken cancellationToken = default) =>
            Task.FromResult(All.FirstOrDefault(r => r.OrganizationId == organizationId && r.Id == repaymentId));

        public Task<(IReadOnlyList<Repayment> Items, int TotalCount)> ListByCustomerAsync(
            PosOrganizationId organizationId, POSCustomerId customerId, int skip, int take,
            CancellationToken cancellationToken = default)
        {
            var list = All.Where(r => r.OrganizationId == organizationId && r.CustomerId == customerId).ToList();
            return Task.FromResult(((IReadOnlyList<Repayment>)list.Skip(skip).Take(take).ToList(), list.Count));
        }

        public Task<(IReadOnlyList<Repayment> Items, int TotalCount)> ListCreatedSinceAsync(
            PosOrganizationId organizationId, DateTimeOffset? sinceUtc, int skip, int take,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(((IReadOnlyList<Repayment>)Array.Empty<Repayment>(), 0));

        public Task<IReadOnlyList<Repayment>> ListRecordedInRangeAsync(
            PosOrganizationId organizationId, DateOnly fromDateUtc, DateOnly toDateUtc,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Repayment>>(Array.Empty<Repayment>());

        public Task<decimal> SumActiveAmountAsync(
            PosOrganizationId organizationId, POSCustomerId customerId, CancellationToken cancellationToken = default) =>
            Task.FromResult(All.Where(r =>
                r.OrganizationId == organizationId && r.CustomerId == customerId && r.Status == RepaymentStatus.Active)
                .Sum(r => r.Amount));

        public Task<IReadOnlyDictionary<Guid, decimal>> SumActiveAmountsByOrganizationAsync(
            PosOrganizationId organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<Guid, decimal>>(
                All.Where(r => r.OrganizationId == organizationId && r.Status == RepaymentStatus.Active)
                    .GroupBy(r => r.CustomerId.Value)
                    .ToDictionary(g => g.Key, g => g.Sum(r => r.Amount)));

        public Task<int> CountActiveAsync(
            PosOrganizationId organizationId, POSCustomerId customerId, CancellationToken cancellationToken = default) =>
            Task.FromResult(All.Count(r =>
                r.OrganizationId == organizationId && r.CustomerId == customerId && r.Status == RepaymentStatus.Active));
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
            PosOrganizationId organizationId, SaleId saleId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(s => s.OrganizationId == organizationId && s.Id == saleId));

        public Task<Sale?> FindBySaleNumberAsync(
            PosOrganizationId organizationId, string saleNumber, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<(IReadOnlyList<Sale> Items, int TotalCount)> ListAsync(
            PosOrganizationId organizationId, SaleFilter filter, int skip, int take,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<Sale>> ListForReportAsync(
            PosOrganizationId organizationId, DateOnly fromDateUtc, DateOnly toDateUtc,
            SaleStatus? status = null, SalePaymentMethod? paymentMethod = null, Guid? productId = null,
            Guid? customerId = null, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<Sale> CheckoutAsync(
            PosOrganizationId organizationId, DateOnly businessDateUtc, Func<string, Sale> createSale,
            Func<Sale, CancellationToken, Task>? afterSaleCreated = null,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task UpdateAsync(Sale sale, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> HasReturnsForSaleAsync(
            PosOrganizationId organizationId, SaleId saleId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
