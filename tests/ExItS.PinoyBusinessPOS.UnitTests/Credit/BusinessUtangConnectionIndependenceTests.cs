using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Credit;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Application.Payments;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Credit;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Payments;

namespace ExItS.PinoyBusinessPOS.UnitTests.Credit;

/// <summary>
/// Organization Business Utang / repayment must not depend on Personal customer-link status.
/// Connection states are scenario labels only — CreateCreditEntry / CreateRepayment never receive them.
/// </summary>
public sealed class BusinessUtangConnectionIndependenceTests
{
    private static readonly Guid OrgId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid Actor = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid PlatformBusinessCustomerId =
        Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-08-25T05:00:00Z");

    public static TheoryData<string> ConnectionStates =>
    [
        "NotLinked",
        "Pending",
        "Linked",
        "Declined",
        "Expired",
        "Revoked",
        "Cancelled",
        "Unavailable",
        "Blocked"
    ];

    [Theory]
    [MemberData(nameof(ConnectionStates))]
    public async Task Active_pos_customer_records_business_utang_for_any_connection_state(string connectionState)
    {
        _ = connectionState; // documented scenario; POS credit path does not consult Platform link status
        var harness = await Harness.CreateAsync();

        var credit = await harness.CreateCredit.ExecuteAsync(
            OrgId,
            harness.Customer.Id.Value,
            125.50m,
            $"Goods under {connectionState}",
            cancellationToken: default);

        Assert.True(credit.IsSuccess, credit.ErrorMessage);
        Assert.Equal(125.50m, credit.Value!.Amount);
        Assert.Equal(harness.Customer.Id, credit.Value.CustomerId);
        Assert.Equal(PlatformBusinessCustomerId, harness.Customer.PlatformBusinessCustomerId);
        Assert.Equal(1, harness.Customers.Count);
    }

    [Theory]
    [InlineData("Revoked")]
    [InlineData("Cancelled")]
    [InlineData("Blocked")]
    [InlineData("Unavailable")]
    [InlineData("Declined")]
    [InlineData("Pending")]
    [InlineData("NotLinked")]
    public async Task Active_pos_customer_accepts_repayment_for_any_connection_state(string connectionState)
    {
        _ = connectionState;
        var harness = await Harness.CreateAsync();
        Assert.True((await harness.CreateCredit.ExecuteAsync(
            OrgId,
            harness.Customer.Id.Value,
            100m,
            "Opening",
            cancellationToken: default)).IsSuccess);

        var repay = await harness.CreateRepayment.ExecuteAsync(
            OrgId,
            harness.Customer.Id.Value,
            40m,
            $"Partial under {connectionState}",
            Actor,
            cancellationToken: default);

        Assert.True(repay.IsSuccess, repay.ErrorMessage);
        Assert.Equal(60m, await harness.Outstanding.GetOutstandingAsync(
            PosOrganizationId.From(OrgId),
            harness.Customer.Id));
        Assert.Equal(1, harness.Customers.Count);
    }

    [Fact]
    public async Task Same_pos_customer_reused_across_connection_state_changes_without_duplicate()
    {
        var harness = await Harness.CreateAsync();
        var customerId = harness.Customer.Id.Value;

        foreach (var state in new[] { "Pending", "Declined", "Revoked", "Blocked", "Linked" })
        {
            var credit = await harness.CreateCredit.ExecuteAsync(
                OrgId,
                customerId,
                10m,
                state,
                cancellationToken: default);
            Assert.True(credit.IsSuccess, $"{state}: {credit.ErrorMessage}");
        }

        Assert.Equal(1, harness.Customers.Count);
        Assert.Equal(customerId, harness.Customers.Single().Id.Value);
        Assert.Equal(50m, await harness.Outstanding.GetOutstandingAsync(
            PosOrganizationId.From(OrgId),
            harness.Customer.Id));
    }

    private sealed class Harness
    {
        public required POSCustomer Customer { get; init; }
        public required InMemoryCustomerRepository Customers { get; init; }
        public required CreateCreditEntry CreateCredit { get; init; }
        public required CreateRepayment CreateRepayment { get; init; }
        public required OutstandingBalanceService Outstanding { get; init; }

        public static async Task<Harness> CreateAsync()
        {
            var customers = new InMemoryCustomerRepository();
            var entries = new InMemoryCreditRepository();
            var repayments = new InMemoryRepaymentRepository();
            var clock = new FixedClock(Now);
            var outstanding = new OutstandingBalanceService(entries, repayments, clock);
            var uow = new ImmediateUnitOfWork();

            var customer = POSCustomer.Create(
                PosOrganizationId.From(OrgId),
                "Rosa Customer",
                Now,
                platformBusinessCustomerId: PlatformBusinessCustomerId,
                linkedPersonalPublicUserId: "EX-1234-5678");
            await customers.AddAsync(customer);

            return new Harness
            {
                Customer = customer,
                Customers = customers,
                CreateCredit = new CreateCreditEntry(customers, entries, uow, clock),
                CreateRepayment = new CreateRepayment(customers, repayments, outstanding, uow, clock),
                Outstanding = outstanding
            };
        }
    }

    private sealed class FixedClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
    }

    private sealed class ImmediateUnitOfWork : IPosUnitOfWork
    {
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<T> ExecuteInSerializableTransactionAsync<T>(
            Func<CancellationToken, Task<T>> action,
            CancellationToken cancellationToken = default) =>
            action(cancellationToken);
    }

    private sealed class InMemoryCustomerRepository : IPOSCustomerRepository
    {
        private readonly List<POSCustomer> _items = [];

        public int Count => _items.Count;

        public POSCustomer Single() => _items.Single();

        public Task<POSCustomer?> GetByIdAsync(
            PosOrganizationId organizationId,
            POSCustomerId customerId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(c => c.Id == customerId && c.OrganizationId == organizationId));

        public Task<POSCustomer?> FindActiveByNormalizedMobileAsync(
            PosOrganizationId organizationId,
            string normalizedMobile,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(c =>
                c.OrganizationId == organizationId
                && c.Status == CustomerStatus.Active
                && c.NormalizedMobile == normalizedMobile));

        public Task<POSCustomer?> FindByPlatformBusinessCustomerIdAsync(
            PosOrganizationId organizationId,
            Guid platformBusinessCustomerId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(c =>
                c.OrganizationId == organizationId
                && c.PlatformBusinessCustomerId == platformBusinessCustomerId));

        public Task<POSCustomer?> FindByLinkedPersonalPublicUserIdAsync(
            PosOrganizationId organizationId,
            string linkedPersonalPublicUserId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(c =>
                c.OrganizationId == organizationId
                && c.Status == CustomerStatus.Active
                && string.Equals(c.LinkedPersonalPublicUserId, linkedPersonalPublicUserId, StringComparison.Ordinal)));

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
                c.OrganizationId == organizationId
                && c.PlatformBusinessCustomerId == platformBusinessCustomerId));

        public Task<(IReadOnlyList<POSCustomer> Items, int TotalCount)> ListAsync(
            PosOrganizationId organizationId,
            CustomerStatus? status,
            string? search,
            int skip,
            int take,
            CancellationToken cancellationToken = default)
        {
            var list = _items.Where(c => c.OrganizationId == organizationId).ToList();
            return Task.FromResult(((IReadOnlyList<POSCustomer>)list.Skip(skip).Take(take).ToList(), list.Count));
        }

        public Task<(IReadOnlyList<POSCustomer> Items, int TotalCount)> ListUpdatedSinceAsync(
            PosOrganizationId organizationId,
            DateTimeOffset? sinceUtc,
            int skip,
            int take,
            CancellationToken cancellationToken = default) =>
            ListAsync(organizationId, null, null, skip, take, cancellationToken);

        public Task<IReadOnlyList<POSCustomer>> ListByIdsAsync(
            PosOrganizationId organizationId,
            IReadOnlyCollection<POSCustomerId> customerIds,
            CancellationToken cancellationToken = default)
        {
            var ids = customerIds.Select(c => c.Value).ToHashSet();
            return Task.FromResult<IReadOnlyList<POSCustomer>>(
                _items.Where(c => c.OrganizationId == organizationId && ids.Contains(c.Id.Value)).ToList());
        }

        public Task AddAsync(POSCustomer customer, CancellationToken cancellationToken = default)
        {
            _items.Add(customer);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(POSCustomer customer, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class InMemoryCreditRepository : ICreditEntryRepository
    {
        private readonly List<CreditEntry> _items = [];

        public Task<CreditEntry?> GetByIdAsync(
            PosOrganizationId organizationId,
            POSCustomerId customerId,
            CreditEntryId entryId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(e =>
                e.Id == entryId && e.CustomerId == customerId && e.OrganizationId == organizationId));

        public Task<CreditEntry?> GetByIdForOrganizationAsync(
            PosOrganizationId organizationId,
            CreditEntryId entryId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(e => e.Id == entryId && e.OrganizationId == organizationId));

        public Task<(IReadOnlyList<CreditEntry> Items, int TotalCount)> ListByCustomerAsync(
            PosOrganizationId organizationId,
            POSCustomerId customerId,
            int skip,
            int take,
            CancellationToken cancellationToken = default)
        {
            var list = _items
                .Where(e => e.OrganizationId == organizationId && e.CustomerId == customerId)
                .OrderByDescending(e => e.CreatedAtUtc)
                .ToList();
            return Task.FromResult(((IReadOnlyList<CreditEntry>)list.Skip(skip).Take(take).ToList(), list.Count));
        }

        public Task<IReadOnlyList<CreditEntry>> ListActiveByOrganizationAsync(
            PosOrganizationId organizationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult((IReadOnlyList<CreditEntry>)_items
                .Where(e => e.OrganizationId == organizationId && e.Status == CreditEntryStatus.Active)
                .ToList());

        public Task<IReadOnlyList<CreditEntry>> ListRecordedInRangeAsync(
            PosOrganizationId organizationId,
            DateOnly fromDateUtc,
            DateOnly toDateUtc,
            CancellationToken cancellationToken = default) =>
            Task.FromResult((IReadOnlyList<CreditEntry>)Array.Empty<CreditEntry>());

        public Task<decimal> SumActiveAmountAsync(
            PosOrganizationId organizationId,
            POSCustomerId customerId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_items
                .Where(e => e.OrganizationId == organizationId
                            && e.CustomerId == customerId
                            && e.Status == CreditEntryStatus.Active)
                .Sum(e => e.Amount));

        public Task<int> CountActiveAsync(
            PosOrganizationId organizationId,
            POSCustomerId customerId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.Count(e =>
                e.OrganizationId == organizationId
                && e.CustomerId == customerId
                && e.Status == CreditEntryStatus.Active));

        public Task<(IReadOnlyList<CreditEntry> Items, int TotalCount)> ListCreatedSinceAsync(
            PosOrganizationId organizationId,
            DateTimeOffset? sinceUtc,
            int skip,
            int take,
            CancellationToken cancellationToken = default)
        {
            var list = _items.Where(e => e.OrganizationId == organizationId).ToList();
            return Task.FromResult(((IReadOnlyList<CreditEntry>)list.Skip(skip).Take(take).ToList(), list.Count));
        }

        public Task AddAsync(CreditEntry entry, CancellationToken cancellationToken = default)
        {
            _items.Add(entry);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(CreditEntry entry, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class InMemoryRepaymentRepository : IRepaymentRepository
    {
        private readonly List<Repayment> _items = [];

        public Task<Repayment?> GetByIdAsync(
            PosOrganizationId organizationId,
            RepaymentId repaymentId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(r => r.Id == repaymentId && r.OrganizationId == organizationId));

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
            var list = _items.Where(r => r.OrganizationId == organizationId).ToList();
            return Task.FromResult(((IReadOnlyList<Repayment>)list.Skip(skip).Take(take).ToList(), list.Count));
        }

        public Task<IReadOnlyList<Repayment>> ListRecordedInRangeAsync(
            PosOrganizationId organizationId,
            DateOnly fromDateUtc,
            DateOnly toDateUtc,
            CancellationToken cancellationToken = default) =>
            Task.FromResult((IReadOnlyList<Repayment>)Array.Empty<Repayment>());

        public Task<decimal> SumActiveAmountAsync(
            PosOrganizationId organizationId,
            POSCustomerId customerId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_items
                .Where(r => r.OrganizationId == organizationId
                            && r.CustomerId == customerId
                            && r.Status == RepaymentStatus.Active)
                .Sum(r => r.Amount));

        public Task<IReadOnlyDictionary<Guid, decimal>> SumActiveAmountsByOrganizationAsync(
            PosOrganizationId organizationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<Guid, decimal>>(
                _items
                    .Where(r => r.OrganizationId == organizationId && r.Status == RepaymentStatus.Active)
                    .GroupBy(r => r.CustomerId.Value)
                    .ToDictionary(g => g.Key, g => g.Sum(r => r.Amount)));

        public Task<int> CountActiveAsync(
            PosOrganizationId organizationId,
            POSCustomerId customerId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.Count(r =>
                r.OrganizationId == organizationId
                && r.CustomerId == customerId
                && r.Status == RepaymentStatus.Active));

        public Task AddAsync(Repayment repayment, CancellationToken cancellationToken = default)
        {
            _items.Add(repayment);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Repayment repayment, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
