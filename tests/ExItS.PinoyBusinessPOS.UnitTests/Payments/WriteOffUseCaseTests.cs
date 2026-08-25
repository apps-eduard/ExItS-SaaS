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

public sealed class WriteOffUseCaseTests
{
    private static readonly Guid OrgId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly Guid Actor = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-07-30T12:00:00Z");

    [Fact]
    public async Task Write_off_reduces_outstanding_without_counting_as_repayment()
    {
        var customers = new InMemoryCustomerRepository();
        var credits = new InMemoryCreditRepository();
        var repayments = new InMemoryRepaymentRepository();
        var writeOffs = new InMemoryWriteOffRepository();
        var outstanding = new OutstandingBalanceService(credits, repayments, writeOffs, new FixedClock(Now));
        var uow = new ImmediateUnitOfWork();
        var clock = new FixedClock(Now);

        var customer = POSCustomer.Create(PosOrganizationId.From(OrgId), "Rosa", Now);
        await customers.AddAsync(customer);
        await credits.AddAsync(CreditEntry.Create(PosOrganizationId.From(OrgId), customer.Id, 1000m, "Goods", Now));

        var createRepayment = new CreateRepayment(customers, repayments, outstanding, uow, clock);
        Assert.True((await createRepayment.ExecuteAsync(OrgId, customer.Id.Value, 300m, "Partial", Actor)).IsSuccess);

        var createWriteOff = new CreateWriteOff(customers, writeOffs, outstanding, uow, clock);
        var missingReason = await createWriteOff.ExecuteAsync(OrgId, customer.Id.Value, 200m, " ", Actor);
        Assert.False(missingReason.IsSuccess);
        Assert.Equal(DomainErrorCodes.InvalidWriteOffReason, missingReason.ErrorCode);

        var over = await createWriteOff.ExecuteAsync(OrgId, customer.Id.Value, 800m, "Too much", Actor);
        Assert.False(over.IsSuccess);
        Assert.Equal(DomainErrorCodes.WriteOffExceedsOutstanding, over.ErrorCode);

        var ok = await createWriteOff.ExecuteAsync(OrgId, customer.Id.Value, 200m, "Uncollectible", Actor);
        Assert.True(ok.IsSuccess);
        Assert.Equal(500m, await outstanding.GetOutstandingAsync(PosOrganizationId.From(OrgId), customer.Id));

        var summary = await outstanding.GetSummaryAsync(OrgId, customer.Id.Value);
        Assert.Equal(300m, summary.ActiveRepaymentTotal);
        Assert.Equal(200m, summary.ActiveWriteOffTotal);
        Assert.Equal(500m, summary.OutstandingAmount);

        var reverse = new ReverseWriteOff(customers, writeOffs, uow, new FixedClock(Now.AddMinutes(1)));
        Assert.True((await reverse.ExecuteAsync(OrgId, ok.Value!.Id.Value, "Undo", Actor)).IsSuccess);
        Assert.Equal(700m, await outstanding.GetOutstandingAsync(PosOrganizationId.From(OrgId), customer.Id));
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

        public Task<POSCustomer?> FindByPlatformBusinessCustomerIdAsync(PosOrganizationId organizationId, Guid platformBusinessCustomerId, CancellationToken cancellationToken = default) =>
            Task.FromResult<POSCustomer?>(null);

        public Task<POSCustomer?> FindByLinkedPersonalPublicUserIdAsync(PosOrganizationId organizationId, string linkedPersonalPublicUserId, CancellationToken cancellationToken = default) =>
            Task.FromResult<POSCustomer?>(null);

        public Task<POSCustomer?> FindByLinkedBuyerOrganizationIdAsync(PosOrganizationId organizationId, Guid linkedBuyerOrganizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult<POSCustomer?>(null);

        public Task<int> CountByPlatformBusinessCustomerIdAsync(PosOrganizationId organizationId, Guid platformBusinessCustomerId, CancellationToken cancellationToken = default) =>
            Task.FromResult(0);

        public Task<(IReadOnlyList<POSCustomer> Items, int TotalCount)> ListAsync(
            PosOrganizationId organizationId, CustomerStatus? status, string? search, int skip, int take, CancellationToken cancellationToken = default) =>
            Task.FromResult(((IReadOnlyList<POSCustomer>)Array.Empty<POSCustomer>(), 0));

        public Task<(IReadOnlyList<POSCustomer> Items, int TotalCount)> ListUpdatedSinceAsync(
            PosOrganizationId organizationId, DateTimeOffset? sinceUtc, int skip, int take, CancellationToken cancellationToken = default) =>
            ListAsync(organizationId, null, null, skip, take, cancellationToken);

        public Task<IReadOnlyList<POSCustomer>> ListByIdsAsync(
            PosOrganizationId organizationId, IReadOnlyCollection<POSCustomerId> customerIds, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<POSCustomer>>(
                _items.Where(c => c.OrganizationId == organizationId && customerIds.Select(x => x.Value).Contains(c.Id.Value)).ToList());

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
            PosOrganizationId organizationId, POSCustomerId customerId, int skip, int take, CancellationToken cancellationToken = default)
        {
            var list = _items.Where(e => e.OrganizationId == organizationId && e.CustomerId == customerId).ToList();
            return Task.FromResult(((IReadOnlyList<CreditEntry>)list.Skip(skip).Take(take).ToList(), list.Count));
        }

        public Task<IReadOnlyList<CreditEntry>> ListActiveByOrganizationAsync(PosOrganizationId organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult((IReadOnlyList<CreditEntry>)_items.Where(e => e.OrganizationId == organizationId && e.Status == CreditEntryStatus.Active).ToList());

        public Task<IReadOnlyList<CreditEntry>> ListRecordedInRangeAsync(
            PosOrganizationId organizationId, DateOnly fromDateUtc, DateOnly toDateUtc, CancellationToken cancellationToken = default) =>
            Task.FromResult((IReadOnlyList<CreditEntry>)Array.Empty<CreditEntry>());

        public Task<decimal> SumActiveAmountAsync(PosOrganizationId organizationId, POSCustomerId customerId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.Where(e => e.OrganizationId == organizationId && e.CustomerId == customerId && e.Status == CreditEntryStatus.Active).Sum(e => e.Amount));

        public Task<int> CountActiveAsync(PosOrganizationId organizationId, POSCustomerId customerId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.Count(e => e.OrganizationId == organizationId && e.CustomerId == customerId && e.Status == CreditEntryStatus.Active));

        public Task<(IReadOnlyList<CreditEntry> Items, int TotalCount)> ListCreatedSinceAsync(
            PosOrganizationId organizationId, DateTimeOffset? sinceUtc, int skip, int take, CancellationToken cancellationToken = default) =>
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
            PosOrganizationId organizationId, POSCustomerId customerId, int skip, int take, CancellationToken cancellationToken = default)
        {
            var list = _items.Where(e => e.OrganizationId == organizationId && e.CustomerId == customerId).ToList();
            return Task.FromResult(((IReadOnlyList<Repayment>)list.Skip(skip).Take(take).ToList(), list.Count));
        }

        public Task<(IReadOnlyList<Repayment> Items, int TotalCount)> ListCreatedSinceAsync(
            PosOrganizationId organizationId, DateTimeOffset? sinceUtc, int skip, int take, CancellationToken cancellationToken = default) =>
            Task.FromResult(((IReadOnlyList<Repayment>)Array.Empty<Repayment>(), 0));

        public Task<IReadOnlyList<Repayment>> ListRecordedInRangeAsync(
            PosOrganizationId organizationId, DateOnly fromDateUtc, DateOnly toDateUtc, CancellationToken cancellationToken = default) =>
            Task.FromResult((IReadOnlyList<Repayment>)Array.Empty<Repayment>());

        public Task<decimal> SumActiveAmountAsync(PosOrganizationId organizationId, POSCustomerId customerId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.Where(e => e.OrganizationId == organizationId && e.CustomerId == customerId && e.Status == RepaymentStatus.Active).Sum(e => e.Amount));

        public Task<IReadOnlyDictionary<Guid, decimal>> SumActiveAmountsByOrganizationAsync(PosOrganizationId organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult((IReadOnlyDictionary<Guid, decimal>)new Dictionary<Guid, decimal>());

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
