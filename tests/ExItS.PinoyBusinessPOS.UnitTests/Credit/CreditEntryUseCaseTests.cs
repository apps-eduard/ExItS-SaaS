using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Credit;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Application.Parties;
using ExItS.PinoyBusinessPOS.Application.Payments;
using ExItS.PinoyBusinessPOS.UnitTests.Parties;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Credit;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Payments;

namespace ExItS.PinoyBusinessPOS.UnitTests.Credit;

public sealed class CreditEntryUseCaseTests
{
    private static readonly Guid OrgId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-07-30T10:00:00Z");

    [Fact]
    public async Task Create_requires_active_customer_and_sums_outstanding_from_active_only()
    {
        var customers = new InMemoryCustomerRepository();
        var entries = new InMemoryCreditRepository();
        var repayments = new InMemoryRepaymentRepository();
        var outstanding = new OutstandingBalanceService(entries, repayments, new InMemoryWriteOffRepository(), new FixedClock(Now));
        var clock = new FixedClock(Now);
        var customer = POSCustomer.Create(PosOrganizationId.From(OrgId), "Rosa", Now);
        await customers.AddAsync(customer);

        var create = new CreateCreditEntry(customers, entries, new ImmediateUnitOfWork(), clock);
        var first = await create.ExecuteAsync(OrgId, customer.Id.Value, 100m, "Goods", default);
        Assert.True(first.IsSuccess);
        var second = await create.ExecuteAsync(OrgId, customer.Id.Value, 40m, "More goods", default);
        Assert.True(second.IsSuccess);

        customer.Deactivate(Now.AddMinutes(1));
        await customers.UpdateAsync(customer);
        var inactive = await create.ExecuteAsync(OrgId, customer.Id.Value, 10m, "Should fail", default);
        Assert.False(inactive.IsSuccess);
        Assert.Equal(DomainErrorCodes.CustomerNotActive, inactive.ErrorCode);

        var reverse = new ReverseCreditEntry(
            customers,
            entries,
            outstanding,
            new ImmediateUnitOfWork(),
            new FixedClock(Now.AddMinutes(2)));
        var reversed = await reverse.ExecuteAsync(OrgId, customer.Id.Value, first.Value!.Id.Value, "Mistake", default);
        Assert.True(reversed.IsSuccess);

        var historyScope = new PartyBranchHistoryScopeService(
            new PartyBranchAccessGovernanceAuthority(),
            FixedPartyBranchAccessActorAccessor.Owner());
        var queries = new CreditEntryQueryService(entries, outstanding, historyScope);
        var summary = await queries.GetSummaryAsync(OrgId, customer.Id.Value);
        Assert.Equal(40m, summary.OutstandingAmount);
        Assert.Equal(1, summary.ActiveEntryCount);
        Assert.Equal(2, summary.TotalEntryCount);
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

        public Task<POSCustomer?> GetByIdAsync(PosOrganizationId organizationId, POSCustomerId customerId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(c => c.Id == customerId && c.OrganizationId == organizationId));

        public Task<POSCustomer?> FindActiveByNormalizedMobileAsync(PosOrganizationId organizationId, string normalizedMobile, CancellationToken cancellationToken = default) =>
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
                c.OrganizationId == organizationId
                && c.PlatformBusinessCustomerId == platformBusinessCustomerId));

        public Task<(IReadOnlyList<POSCustomer> Items, int TotalCount)> ListAsync(
            PosOrganizationId organizationId,
            CustomerStatus? status,
            string? search,
            int skip,
            int take, IReadOnlyCollection<Guid>? restrictToCustomerIds = null, CancellationToken cancellationToken = default)
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
            ListAsync(organizationId, null, null, skip, take, null, cancellationToken);

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
            CancellationToken cancellationToken = default, IReadOnlySet<Guid>? historyBranchIds = null)
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
                .OrderBy(e => e.CreatedAtUtc)
                .ThenBy(e => e.Id.Value)
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
            CancellationToken cancellationToken = default,
            IReadOnlySet<Guid>? historyBranchIds = null) => Task.FromResult(_items
                .Where(e => e.OrganizationId == organizationId
                            && e.CustomerId == customerId
                            && e.Status == CreditEntryStatus.Active)
                .Sum(e => e.Amount));

        public Task<int> CountActiveAsync(
            PosOrganizationId organizationId,
            POSCustomerId customerId,
            CancellationToken cancellationToken = default,
            IReadOnlySet<Guid>? historyBranchIds = null) => Task.FromResult(_items.Count(e =>
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
            var list = _items
                .Where(e => e.OrganizationId == organizationId)
                .OrderByDescending(e => e.CreatedAtUtc)
                .ToList();
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
        public Task<Repayment?> GetByIdAsync(PosOrganizationId organizationId, RepaymentId repaymentId, CancellationToken cancellationToken = default) =>
            Task.FromResult<Repayment?>(null);

        public Task<(IReadOnlyList<Repayment> Items, int TotalCount)> ListByCustomerAsync(
            PosOrganizationId organizationId,
            POSCustomerId customerId,
            int skip,
            int take,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(((IReadOnlyList<Repayment>)Array.Empty<Repayment>(), 0));

        public Task<(IReadOnlyList<Repayment> Items, int TotalCount)> ListCreatedSinceAsync(
            PosOrganizationId organizationId,
            DateTimeOffset? sinceUtc,
            int skip,
            int take,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(((IReadOnlyList<Repayment>)Array.Empty<Repayment>(), 0));

        public Task<IReadOnlyList<Repayment>> ListRecordedInRangeAsync(
            PosOrganizationId organizationId,
            DateOnly fromDateUtc,
            DateOnly toDateUtc,
            CancellationToken cancellationToken = default) =>
            Task.FromResult((IReadOnlyList<Repayment>)Array.Empty<Repayment>());

        public Task<decimal> SumActiveAmountAsync(
            PosOrganizationId organizationId,
            POSCustomerId customerId,
            CancellationToken cancellationToken = default) => Task.FromResult(0m);

        public Task<IReadOnlyDictionary<Guid, decimal>> SumActiveAmountsByOrganizationAsync(
            PosOrganizationId organizationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<Guid, decimal>>(new Dictionary<Guid, decimal>());

        public Task<int> CountActiveAsync(
            PosOrganizationId organizationId,
            POSCustomerId customerId,
            CancellationToken cancellationToken = default) => Task.FromResult(0);

        public Task AddAsync(Repayment repayment, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task UpdateAsync(Repayment repayment, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
