using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Credit;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Application.Payments;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Credit;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Payments;

namespace ExItS.PinoyBusinessPOS.UnitTests.Payments;

public sealed class RepaymentUseCaseTests
{
    private static readonly Guid OrgId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid Actor = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-07-30T12:00:00Z");

    [Fact]
    public async Task Create_rejects_overpayment_zero_outstanding_and_allows_inactive_customer()
    {
        var customers = new InMemoryCustomerRepository();
        var credits = new InMemoryCreditRepository();
        var repayments = new InMemoryRepaymentRepository();
        var outstanding = new OutstandingBalanceService(credits, repayments, new FixedClock(Now));
        var uow = new ImmediateUnitOfWork();
        var clock = new FixedClock(Now);

        var customer = POSCustomer.Create(PosOrganizationId.From(OrgId), "Rosa", Now);
        await customers.AddAsync(customer);
        await credits.AddAsync(CreditEntry.Create(PosOrganizationId.From(OrgId), customer.Id, 100m, "Goods", Now));

        var create = new CreateRepayment(customers, repayments, outstanding, uow, clock);
        var over = await create.ExecuteAsync(OrgId, customer.Id.Value, 150m, "Too much", Actor, default);
        Assert.False(over.IsSuccess);
        Assert.Equal(DomainErrorCodes.RepaymentExceedsOutstanding, over.ErrorCode);

        var partial = await create.ExecuteAsync(OrgId, customer.Id.Value, 40m, "Partial", Actor, default);
        Assert.True(partial.IsSuccess);
        Assert.Equal(60m, await outstanding.GetOutstandingAsync(PosOrganizationId.From(OrgId), customer.Id));

        var exact = await create.ExecuteAsync(OrgId, customer.Id.Value, 60m, "Settle", Actor, default);
        Assert.True(exact.IsSuccess);
        Assert.Equal(0m, await outstanding.GetOutstandingAsync(PosOrganizationId.From(OrgId), customer.Id));

        var zero = await create.ExecuteAsync(OrgId, customer.Id.Value, 1m, "Zero bal", Actor, default);
        Assert.False(zero.IsSuccess);
        Assert.Equal(DomainErrorCodes.RepaymentOutstandingZero, zero.ErrorCode);

        // Reverse one repayment to open balance, deactivate, repay again (inactive allowed).
        var reverse = new ReverseRepayment(customers, repayments, uow, new FixedClock(Now.AddMinutes(1)));
        var reversed = await reverse.ExecuteAsync(OrgId, exact.Value!.Id.Value, "Undo settle", Actor, default);
        Assert.True(reversed.IsSuccess);

        customer.Deactivate(Now.AddMinutes(2));
        await customers.UpdateAsync(customer);
        var inactivePay = await create.ExecuteAsync(OrgId, customer.Id.Value, 60m, "Inactive ok", Actor, default);
        Assert.True(inactivePay.IsSuccess);
        Assert.Equal(0m, await outstanding.GetOutstandingAsync(PosOrganizationId.From(OrgId), customer.Id));

        var dup = await reverse.ExecuteAsync(OrgId, exact.Value.Id.Value, "Again", Actor, default);
        Assert.False(dup.IsSuccess);
        Assert.Equal(DomainErrorCodes.InvalidRepaymentStatusTransition, dup.ErrorCode);
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
            Task.FromResult<POSCustomer?>(null);

        public Task<(IReadOnlyList<POSCustomer> Items, int TotalCount)> ListAsync(
            PosOrganizationId organizationId,
            CustomerStatus? status,
            string? search,
            int skip,
            int take,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(((IReadOnlyList<POSCustomer>)Array.Empty<POSCustomer>(), 0));

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

        public Task<CreditEntry?> GetByIdAsync(PosOrganizationId organizationId, POSCustomerId customerId, CreditEntryId entryId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(e => e.Id == entryId && e.OrganizationId == organizationId && e.CustomerId == customerId));

        public Task<CreditEntry?> GetByIdForOrganizationAsync(PosOrganizationId organizationId, CreditEntryId entryId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(e => e.Id == entryId && e.OrganizationId == organizationId));

        public Task<(IReadOnlyList<CreditEntry> Items, int TotalCount)> ListByCustomerAsync(
            PosOrganizationId organizationId,
            POSCustomerId customerId,
            int skip,
            int take,
            CancellationToken cancellationToken = default)
        {
            var list = _items.Where(e => e.OrganizationId == organizationId && e.CustomerId == customerId).ToList();
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
        public Task<decimal> SumActiveAmountAsync(PosOrganizationId organizationId, POSCustomerId customerId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.Where(e => e.OrganizationId == organizationId && e.CustomerId == customerId && e.Status == CreditEntryStatus.Active).Sum(e => e.Amount));

        public Task<int> CountActiveAsync(PosOrganizationId organizationId, POSCustomerId customerId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.Count(e => e.OrganizationId == organizationId && e.CustomerId == customerId && e.Status == CreditEntryStatus.Active));

        public Task<(IReadOnlyList<CreditEntry> Items, int TotalCount)> ListCreatedSinceAsync(
            PosOrganizationId organizationId,
            DateTimeOffset? sinceUtc,
            int skip,
            int take,
            CancellationToken cancellationToken = default) =>
            ListByCustomerAsync(organizationId, POSCustomerId.From(Guid.Empty), skip, take, cancellationToken);

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

        public Task<Repayment?> GetByIdAsync(PosOrganizationId organizationId, RepaymentId repaymentId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(e => e.Id == repaymentId && e.OrganizationId == organizationId));

        public Task<(IReadOnlyList<Repayment> Items, int TotalCount)> ListByCustomerAsync(
            PosOrganizationId organizationId,
            POSCustomerId customerId,
            int skip,
            int take,
            CancellationToken cancellationToken = default)
        {
            var list = _items.Where(e => e.OrganizationId == organizationId && e.CustomerId == customerId).ToList();
            return Task.FromResult(((IReadOnlyList<Repayment>)list.Skip(skip).Take(take).ToList(), list.Count));
        }

        public Task<(IReadOnlyList<Repayment> Items, int TotalCount)> ListCreatedSinceAsync(
            PosOrganizationId organizationId,
            DateTimeOffset? sinceUtc,
            int skip,
            int take,
            CancellationToken cancellationToken = default)
        {
            var list = _items.Where(e => e.OrganizationId == organizationId).AsEnumerable();
            if (sinceUtc is not null)
            {
                var since = sinceUtc.Value.ToUniversalTime();
                list = list.Where(e =>
                    e.RecordedAtUtc > since
                    || (e.ReversedAtUtc is not null && e.ReversedAtUtc > since));
            }

            var ordered = list.OrderBy(e => e.RecordedAtUtc).ThenBy(e => e.Id).ToList();
            return Task.FromResult(((IReadOnlyList<Repayment>)ordered.Skip(skip).Take(take).ToList(), ordered.Count));
        }

        public Task<IReadOnlyList<Repayment>> ListRecordedInRangeAsync(
            PosOrganizationId organizationId,
            DateOnly fromDateUtc,
            DateOnly toDateUtc,
            CancellationToken cancellationToken = default) =>
            Task.FromResult((IReadOnlyList<Repayment>)Array.Empty<Repayment>());

        public Task<decimal> SumActiveAmountAsync(PosOrganizationId organizationId, POSCustomerId customerId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.Where(e => e.OrganizationId == organizationId && e.CustomerId == customerId && e.Status == RepaymentStatus.Active).Sum(e => e.Amount));

        public Task<IReadOnlyDictionary<Guid, decimal>> SumActiveAmountsByOrganizationAsync(
            PosOrganizationId organizationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<Guid, decimal>>(
                _items
                    .Where(e => e.OrganizationId == organizationId && e.Status == RepaymentStatus.Active)
                    .GroupBy(e => e.CustomerId.Value)
                    .ToDictionary(g => g.Key, g => g.Sum(e => e.Amount)));

        public Task<int> CountActiveAsync(PosOrganizationId organizationId, POSCustomerId customerId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.Count(e => e.OrganizationId == organizationId && e.CustomerId == customerId && e.Status == RepaymentStatus.Active));

        public Task AddAsync(Repayment repayment, CancellationToken cancellationToken = default)
        {
            _items.Add(repayment);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Repayment repayment, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
