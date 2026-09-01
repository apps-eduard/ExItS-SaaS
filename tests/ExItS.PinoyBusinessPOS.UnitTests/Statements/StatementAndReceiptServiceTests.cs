using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Credit;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Application.Payments;
using ExItS.PinoyBusinessPOS.Application.Statements;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.Credit;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Payments;

namespace ExItS.PinoyBusinessPOS.UnitTests.Statements;

public sealed class StatementAndReceiptServiceTests
{
    private static readonly DateTimeOffset T0 = new(2026, 7, 1, 10, 0, 0, TimeSpan.Zero);
    private static readonly Guid OrgId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid Actor = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    [Fact]
    public async Task Statement_opening_closing_reconcile_with_ledger_and_show_reversals()
    {
        var harness = await Harness.CreateAsync();
        var customer = POSCustomer.Create(PosOrganizationId.From(OrgId), "Rosa", T0);
        await harness.Customers.AddAsync(customer);

        var c1 = CreditEntry.Create(customer.OrganizationId, customer.Id, 100m, "Goods", T0);
        var c2 = CreditEntry.Create(customer.OrganizationId, customer.Id, 50m, "More", T0.AddDays(5));
        await harness.Credits.AddAsync(c1);
        await harness.Credits.AddAsync(c2);
        c2.Reverse("mistake", T0.AddDays(6));
        await harness.Credits.UpdateAsync(c2);

        var r1 = Repayment.Create(customer.OrganizationId, customer.Id, 40m, "Partial", Actor, T0.AddDays(10));
        await harness.Repayments.AddAsync(r1);

        var statement = await harness.Statements.GenerateAsync(
            customer.OrganizationId,
            customer.Id.Value,
            DateOnly.FromDateTime(T0.AddDays(3).UtcDateTime),
            DateOnly.FromDateTime(T0.AddDays(20).UtcDateTime),
            "Store A",
            "PHP",
            "en-PH");

        Assert.True(statement.IsSuccess);
        var dto = statement.Value!;
        Assert.Equal(100m, dto.OpeningBalance); // only c1 before period
        Assert.Contains(dto.Lines, l => l.IsReversed && l.EntryType == "Credit");
        Assert.Contains(dto.Lines, l => l.EntryType == "Repayment" && l.Amount == 40m);
        Assert.Equal(dto.ClosingBalance, dto.OpeningBalance + dto.Lines.Sum(l => l.SignedEffect));

        var ledger = await harness.Ledger.ListAllChronologicalAsync(customer.OrganizationId, customer.Id);
        var periodEndExclusive = new DateTimeOffset(dto.PeriodEnd.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var expectedClosing = ledger.Where(e => e.RecordedAtUtc < periodEndExclusive).Select(e => e.RunningBalance).LastOrDefault();
        Assert.Equal(expectedClosing, dto.ClosingBalance);
    }

    [Fact]
    public async Task Receipt_is_idempotent_deterministic_and_marks_reversed()
    {
        var harness = await Harness.CreateAsync();
        var customer = POSCustomer.Create(PosOrganizationId.From(OrgId), "Rosa", T0);
        await harness.Customers.AddAsync(customer);
        await harness.Credits.AddAsync(CreditEntry.Create(customer.OrganizationId, customer.Id, 80m, "Goods", T0));
        var repayment = Repayment.Create(customer.OrganizationId, customer.Id, 30m, "Cash", Actor, T0.AddHours(1));
        await harness.Repayments.AddAsync(repayment);

        var first = await harness.Receipts.GetAsync(customer.OrganizationId, repayment.Id.Value, "Store A", "PHP", "en-PH");
        var second = await harness.Receipts.GetAsync(customer.OrganizationId, repayment.Id.Value, "Store A", "PHP", "en-PH");
        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.Equal(first.Value!.ReceiptReference, second.Value!.ReceiptReference);
        Assert.Equal(RepaymentReceiptService.BuildReceiptReference(repayment.Id.Value), first.Value.ReceiptReference);
        Assert.False(first.Value.IsReversed);

        repayment.Reverse("void", Actor, T0.AddHours(2));
        await harness.Repayments.UpdateAsync(repayment);

        var reversed = await harness.Receipts.GetAsync(customer.OrganizationId, repayment.Id.Value, "Store A", "PHP", "en-PH");
        Assert.True(reversed.IsSuccess);
        Assert.True(reversed.Value!.IsReversed);
        Assert.Equal("Reversed", reversed.Value.Status);
        Assert.Equal(first.Value.ReceiptReference, reversed.Value.ReceiptReference);
        Assert.Contains("not a tax invoice", reversed.Value.Disclaimer, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Receipt_conceals_cross_organization()
    {
        var harness = await Harness.CreateAsync();
        var orgA = PosOrganizationId.From(OrgId);
        var orgB = PosOrganizationId.From(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));
        var customer = POSCustomer.Create(orgA, "Rosa", T0);
        await harness.Customers.AddAsync(customer);
        await harness.Credits.AddAsync(CreditEntry.Create(orgA, customer.Id, 50m, "Goods", T0));
        var repayment = Repayment.Create(orgA, customer.Id, 10m, null, Actor, T0.AddHours(1));
        await harness.Repayments.AddAsync(repayment);

        var hidden = await harness.Receipts.GetAsync(orgB, repayment.Id.Value, null, "PHP", "en-PH");
        Assert.False(hidden.IsSuccess);
        Assert.Equal(Application.Common.ApplicationErrorCodes.ReceiptNotFound, hidden.ErrorCode);
    }

    private sealed class Harness
    {
        public required InMemoryCustomers Customers { get; init; }
        public required InMemoryCredits Credits { get; init; }
        public required InMemoryRepayments Repayments { get; init; }
        public required InMemoryLedger Ledger { get; init; }
        public required CustomerStatementService Statements { get; init; }
        public required RepaymentReceiptService Receipts { get; init; }

        public static Task<Harness> CreateAsync()
        {
            var customers = new InMemoryCustomers();
            var credits = new InMemoryCredits();
            var repayments = new InMemoryRepayments();
            var ledger = new InMemoryLedger(credits, repayments);
            var access = new PosCommercialAccessAccessor
            {
                Current = PosCommercialAccess.DevelopmentDefault
            };
            var clock = new FixedClock(T0.AddDays(30));
            var outstanding = new OutstandingBalanceService(credits, repayments, new InMemoryWriteOffRepository(), clock);
            var statements = new CustomerStatementService(customers, ledger, credits, repayments, new InMemoryWriteOffRepository(), outstanding, access, clock);
            var receipts = new RepaymentReceiptService(repayments, customers, ledger, access, clock);
            return Task.FromResult(new Harness
            {
                Customers = customers,
                Credits = credits,
                Repayments = repayments,
                Ledger = ledger,
                Statements = statements,
                Receipts = receipts
            });
        }
    }

    private sealed class FixedClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow { get; set; } = utcNow;
    }

    private sealed class InMemoryCustomers : IPOSCustomerRepository
    {
        private readonly Dictionary<Guid, POSCustomer> _items = new();

        public Task AddAsync(POSCustomer customer, CancellationToken cancellationToken = default)
        {
            _items[customer.Id.Value] = customer;
            return Task.CompletedTask;
        }

        public Task UpdateAsync(POSCustomer customer, CancellationToken cancellationToken = default)
        {
            _items[customer.Id.Value] = customer;
            return Task.CompletedTask;
        }

        public Task<POSCustomer?> GetByIdAsync(PosOrganizationId organizationId, POSCustomerId customerId, CancellationToken cancellationToken = default)
        {
            _items.TryGetValue(customerId.Value, out var c);
            return Task.FromResult(c is not null && c.OrganizationId == organizationId ? c : null);
        }

        public Task<POSCustomer?> FindActiveByNormalizedMobileAsync(PosOrganizationId organizationId, string normalizedMobile, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.Values.FirstOrDefault(c =>
                c.OrganizationId == organizationId
                && c.Status == CustomerStatus.Active
                && c.NormalizedMobile == normalizedMobile));

        public Task<POSCustomer?> FindByPlatformBusinessCustomerIdAsync(
            PosOrganizationId organizationId,
            Guid platformBusinessCustomerId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.Values.FirstOrDefault(c =>
                c.OrganizationId == organizationId
                && c.PlatformBusinessCustomerId == platformBusinessCustomerId));

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
            Task.FromResult(_items.Values.Count(c =>
                c.OrganizationId == organizationId
                && c.PlatformBusinessCustomerId == platformBusinessCustomerId));

        public Task<(IReadOnlyList<POSCustomer> Items, int TotalCount)> ListAsync(
            PosOrganizationId organizationId,
            CustomerStatus? status,
            string? search,
            int skip,
            int take, IReadOnlyCollection<Guid>? restrictToCustomerIds = null, CancellationToken cancellationToken = default)
        {
            var q = _items.Values.Where(c => c.OrganizationId == organizationId);
            if (status is not null)
            {
                q = q.Where(c => c.Status == status);
            }

            var list = q.OrderBy(c => c.DisplayName).ToList();
            return Task.FromResult(((IReadOnlyList<POSCustomer>)list.Skip(skip).Take(take).ToList(), list.Count));
        }

        public Task<(IReadOnlyList<POSCustomer> Items, int TotalCount)> ListUpdatedSinceAsync(
            PosOrganizationId organizationId,
            DateTimeOffset? sinceUtc,
            int skip,
            int take,
            CancellationToken cancellationToken = default) =>
            ListAsync(organizationId, null, null, skip, take, null, cancellationToken);

        public Task<IReadOnlyList<POSCustomer>> ListByIdsAsync(
            PosOrganizationId organizationId,
            IReadOnlyCollection<POSCustomerId> customerIds,
            CancellationToken cancellationToken = default)
        {
            var ids = customerIds.Select(c => c.Value).ToHashSet();
            return Task.FromResult<IReadOnlyList<POSCustomer>>(
                _items.Values.Where(c => c.OrganizationId == organizationId && ids.Contains(c.Id.Value)).ToList());
        }
    }

    private sealed class InMemoryCredits : ICreditEntryRepository
    {
        private readonly List<CreditEntry> _items = [];

        public Task AddAsync(CreditEntry entry, CancellationToken cancellationToken = default)
        {
            _items.Add(entry);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(CreditEntry entry, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<CreditEntry?> GetByIdAsync(PosOrganizationId organizationId, POSCustomerId customerId, CreditEntryId entryId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(e =>
                e.OrganizationId == organizationId && e.CustomerId == customerId && e.Id == entryId));

        public Task<CreditEntry?> GetByIdForOrganizationAsync(PosOrganizationId organizationId, CreditEntryId entryId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(e => e.OrganizationId == organizationId && e.Id == entryId));

        public Task<(IReadOnlyList<CreditEntry> Items, int TotalCount)> ListByCustomerAsync(
            PosOrganizationId organizationId,
            POSCustomerId customerId,
            int skip,
            int take,
            CancellationToken cancellationToken = default)
        {
            var list = _items.Where(e => e.OrganizationId == organizationId && e.CustomerId == customerId)
                .OrderBy(e => e.CreatedAtUtc).ThenBy(e => e.Id.Value).ToList();
            return Task.FromResult(((IReadOnlyList<CreditEntry>)list.Skip(skip).Take(take).ToList(), list.Count));
        }

        public Task<IReadOnlyList<CreditEntry>> ListActiveByOrganizationAsync(PosOrganizationId organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult((IReadOnlyList<CreditEntry>)_items.Where(e => e.OrganizationId == organizationId && e.Status == CreditEntryStatus.Active).ToList());

        public Task<IReadOnlyList<CreditEntry>> ListRecordedInRangeAsync(
            PosOrganizationId organizationId,
            DateOnly fromDateUtc,
            DateOnly toDateUtc,
            CancellationToken cancellationToken = default) =>
            Task.FromResult((IReadOnlyList<CreditEntry>)Array.Empty<CreditEntry>());
        public Task<decimal> SumActiveAmountAsync(PosOrganizationId organizationId, POSCustomerId customerId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.Where(e => e.OrganizationId == organizationId && e.CustomerId == customerId && e.Status == CreditEntryStatus.Active).Sum(e => e.Amount));

        public Task<int> CountActiveAsync(PosOrganizationId organizationId, POSCustomerId customerId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.Count(e => e.OrganizationId == organizationId && e.CustomerId == customerId && e.Status == CreditEntryStatus.Active));

        public Task<(IReadOnlyList<CreditEntry> Items, int TotalCount)> ListCreatedSinceAsync(
            PosOrganizationId organizationId,
            DateTimeOffset? sinceUtc,
            int skip,
            int take,
            CancellationToken cancellationToken = default)
        {
            var list = _items.Where(e => e.OrganizationId == organizationId).OrderBy(e => e.CreatedAtUtc).ThenBy(e => e.Id.Value).ToList();
            return Task.FromResult(((IReadOnlyList<CreditEntry>)list.Skip(skip).Take(take).ToList(), list.Count));
        }
    }

    private sealed class InMemoryRepayments : IRepaymentRepository
    {
        private readonly List<Repayment> _items = [];

        public Task AddAsync(Repayment repayment, CancellationToken cancellationToken = default)
        {
            _items.Add(repayment);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Repayment repayment, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<Repayment?> GetByIdAsync(PosOrganizationId organizationId, RepaymentId repaymentId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(r => r.OrganizationId == organizationId && r.Id == repaymentId));

        public Task<(IReadOnlyList<Repayment> Items, int TotalCount)> ListByCustomerAsync(
            PosOrganizationId organizationId,
            POSCustomerId customerId,
            int skip,
            int take,
            CancellationToken cancellationToken = default)
        {
            var list = _items.Where(r => r.OrganizationId == organizationId && r.CustomerId == customerId).ToList();
            return Task.FromResult(((IReadOnlyList<Repayment>)list.Skip(skip).Take(take).ToList(), list.Count));
        }

        public Task<(IReadOnlyList<Repayment> Items, int TotalCount)> ListCreatedSinceAsync(
            PosOrganizationId organizationId,
            DateTimeOffset? sinceUtc,
            int skip,
            int take,
            CancellationToken cancellationToken = default)
        {
            var list = _items.Where(r => r.OrganizationId == organizationId).AsEnumerable();
            if (sinceUtc is not null)
            {
                var since = sinceUtc.Value.ToUniversalTime();
                list = list.Where(r =>
                    r.RecordedAtUtc > since
                    || (r.ReversedAtUtc is not null && r.ReversedAtUtc > since));
            }

            var ordered = list.OrderBy(r => r.RecordedAtUtc).ThenBy(r => r.Id).ToList();
            return Task.FromResult(((IReadOnlyList<Repayment>)ordered.Skip(skip).Take(take).ToList(), ordered.Count));
        }

        public Task<IReadOnlyList<Repayment>> ListRecordedInRangeAsync(
            PosOrganizationId organizationId,
            DateOnly fromDateUtc,
            DateOnly toDateUtc,
            CancellationToken cancellationToken = default) =>
            Task.FromResult((IReadOnlyList<Repayment>)Array.Empty<Repayment>());

        public Task<decimal> SumActiveAmountAsync(PosOrganizationId organizationId, POSCustomerId customerId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.Where(r => r.OrganizationId == organizationId && r.CustomerId == customerId && r.Status == RepaymentStatus.Active).Sum(r => r.Amount));

        public Task<IReadOnlyDictionary<Guid, decimal>> SumActiveAmountsByOrganizationAsync(
            PosOrganizationId organizationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<Guid, decimal>>(
                _items
                    .Where(r => r.OrganizationId == organizationId && r.Status == RepaymentStatus.Active)
                    .GroupBy(r => r.CustomerId.Value)
                    .ToDictionary(g => g.Key, g => g.Sum(r => r.Amount)));

        public Task<int> CountActiveAsync(PosOrganizationId organizationId, POSCustomerId customerId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.Count(r => r.OrganizationId == organizationId && r.CustomerId == customerId && r.Status == RepaymentStatus.Active));
    }

    private sealed class InMemoryLedger(InMemoryCredits credits, InMemoryRepayments repayments) : IUtangLedgerQuery
    {
        public async Task<(IReadOnlyList<LedgerEntryDto> Items, int TotalCount)> ListAsync(
            PosOrganizationId organizationId,
            POSCustomerId customerId,
            int skip,
            int take,
            CancellationToken cancellationToken = default)
        {
            var all = await ListAllChronologicalAsync(organizationId, customerId, cancellationToken).ConfigureAwait(false);
            return (all.Skip(skip).Take(take).ToList(), all.Count);
        }

        public async Task<IReadOnlyList<LedgerEntryDto>> ListAllChronologicalAsync(
            PosOrganizationId organizationId,
            POSCustomerId customerId,
            CancellationToken cancellationToken = default)
        {
            var (creditItems, _) = await credits.ListByCustomerAsync(organizationId, customerId, 0, 10_000, cancellationToken);
            var (repaymentItems, _) = await repayments.ListByCustomerAsync(organizationId, customerId, 0, 10_000, cancellationToken);

            var rows = creditItems.Select(c => new
            {
                Id = c.Id.Value,
                Type = "Credit",
                Amount = c.Amount,
                Effect = c.Status == CreditEntryStatus.Active ? c.Amount : 0m,
                c.Remarks,
                Status = c.Status.ToString(),
                At = c.CreatedAtUtc,
                By = (Guid?)null,
                c.ReversedAtUtc,
                c.ReversalReason,
                ReversedBy = (Guid?)null
            }).Concat(repaymentItems.Select(r => new
            {
                Id = r.Id.Value,
                Type = "Repayment",
                Amount = r.Amount,
                Effect = r.Status == RepaymentStatus.Active ? -r.Amount : 0m,
                r.Remarks,
                Status = r.Status.ToString(),
                At = r.RecordedAtUtc,
                By = (Guid?)r.RecordedBy,
                r.ReversedAtUtc,
                r.ReversalReason,
                ReversedBy = r.ReversedBy
            }))
            .OrderBy(x => x.At).ThenBy(x => x.Id)
            .ToList();

            decimal running = 0m;
            var list = new List<LedgerEntryDto>();
            foreach (var row in rows)
            {
                running += row.Effect;
                list.Add(new LedgerEntryDto(
                    row.Id, row.Type, organizationId.Value, customerId.Value, row.Amount, row.Effect,
                    row.Remarks, row.Status, row.At, row.By, row.ReversedAtUtc, row.ReversalReason, row.ReversedBy, running));
            }

            return list;
        }
    }
}
