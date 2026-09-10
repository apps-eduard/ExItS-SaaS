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

public sealed class CustomerCreditPolicyUseCaseTests
{
    private static readonly Guid OrgId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid Actor = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-07-30T10:00:00Z");

    [Fact]
    public async Task Upsert_then_approve_returns_approved_policy_with_available_credit()
    {
        var (customers, policies, outstanding, uow) = CreateHarness();
        var customer = POSCustomer.Create(PosOrganizationId.From(OrgId), "Rosa", Now);
        await customers.AddAsync(customer);

        var upsert = new UpsertCustomerCreditPolicy(customers, policies, outstanding, uow, new FixedClock(Now));
        var configured = await upsert.ExecuteAsync(
            OrgId,
            customer.Id.Value,
            creditLimit: 1_000m,
            defaultTermDays: 30,
            Actor,
            reason: "test configure");
        Assert.True(configured.IsSuccess);
        Assert.Equal(nameof(CustomerCreditPolicyStatus.PendingApproval), configured.Value!.Status);
        Assert.Equal(0m, configured.Value.AvailableCredit);
        Assert.NotNull(configured.Value.ExpectedUpdatedAtUtc);

        var approve = new ApproveCustomerCreditPolicy(
            customers,
            policies,
            outstanding,
            uow,
            new FixedClock(Now.AddSeconds(1)));
        var approved = await approve.ExecuteAsync(
            OrgId,
            customer.Id.Value,
            Actor,
            "test approve",
            configured.Value.ExpectedUpdatedAtUtc!.Value);
        Assert.True(approved.IsSuccess);
        Assert.Equal(nameof(CustomerCreditPolicyStatus.Approved), approved.Value!.Status);
        Assert.Equal(1_000m, approved.Value.AvailableCredit);
    }

    [Fact]
    public async Task Approve_rejects_stale_expected_updated_at()
    {
        var (customers, policies, outstanding, uow) = CreateHarness();
        var customer = POSCustomer.Create(PosOrganizationId.From(OrgId), "Rosa", Now);
        await customers.AddAsync(customer);

        var clock = new FixedClock(Now);
        var upsert = new UpsertCustomerCreditPolicy(customers, policies, outstanding, uow, clock);
        var configured = await upsert.ExecuteAsync(OrgId, customer.Id.Value, 500m, 15, Actor, "cfg");
        Assert.True(configured.IsSuccess);

        var approve = new ApproveCustomerCreditPolicy(customers, policies, outstanding, uow, clock);
        var stale = await approve.ExecuteAsync(
            OrgId,
            customer.Id.Value,
            Actor,
            "stale",
            configured.Value!.ExpectedUpdatedAtUtc!.Value.AddSeconds(-1));
        Assert.False(stale.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.CustomerCreditPolicyConcurrencyConflict, stale.ErrorCode);
    }

    private static (
        InMemoryCustomerRepository Customers,
        InMemoryCustomerCreditPolicyRepository Policies,
        OutstandingBalanceService Outstanding,
        ImmediateUnitOfWork UnitOfWork) CreateHarness()
    {
        var customers = new InMemoryCustomerRepository();
        var policies = new InMemoryCustomerCreditPolicyRepository();
        var outstanding = new OutstandingBalanceService(
            new EmptyCreditRepository(),
            new EmptyRepaymentRepository(),
            new InMemoryWriteOffRepository(),
            new FixedClock(Now));
        return (customers, policies, outstanding, new ImmediateUnitOfWork());
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

    private sealed class InMemoryCustomerCreditPolicyRepository : ICustomerCreditPolicyRepository
    {
        private readonly List<CustomerCreditPolicy> _policies = [];
        private readonly List<CustomerCreditPolicyChange> _changes = [];

        public Task<CustomerCreditPolicy?> GetByCustomerAsync(
            PosOrganizationId organizationId,
            POSCustomerId customerId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_policies.FirstOrDefault(p =>
                p.OrganizationId == organizationId && p.CustomerId == customerId));

        public Task<IReadOnlyList<CustomerCreditPolicy>> ListByCustomerIdsAsync(
            PosOrganizationId organizationId,
            IReadOnlyCollection<Guid> customerIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CustomerCreditPolicy>>(
                _policies
                    .Where(p => p.OrganizationId == organizationId && customerIds.Contains(p.CustomerId.Value))
                    .ToList());

        public Task AddAsync(CustomerCreditPolicy policy, CancellationToken cancellationToken = default)
        {
            _policies.Add(policy);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(CustomerCreditPolicy policy, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task AddChangeAsync(CustomerCreditPolicyChange change, CancellationToken cancellationToken = default)
        {
            _changes.Add(change);
            return Task.CompletedTask;
        }

        public Task<(IReadOnlyList<CustomerCreditPolicyChange> Items, int TotalCount)> ListChangesAsync(
            PosOrganizationId organizationId,
            POSCustomerId customerId,
            int skip,
            int take,
            CancellationToken cancellationToken = default)
        {
            var list = _changes
                .Where(c => c.OrganizationId == organizationId && c.CustomerId == customerId)
                .OrderByDescending(c => c.ChangedAtUtc)
                .ToList();
            return Task.FromResult(((IReadOnlyList<CustomerCreditPolicyChange>)list.Skip(skip).Take(take).ToList(), list.Count));
        }

        public Task AcquireCustomerCreditLockAsync(
            PosOrganizationId organizationId,
            POSCustomerId customerId,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class InMemoryCustomerRepository : IPOSCustomerRepository
    {
        private readonly List<POSCustomer> _items = [];

        public Task<POSCustomer?> GetByIdAsync(PosOrganizationId organizationId, POSCustomerId customerId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(c => c.Id == customerId && c.OrganizationId == organizationId));

        public Task<POSCustomer?> FindActiveByNormalizedMobileAsync(PosOrganizationId organizationId, string normalizedMobile, CancellationToken cancellationToken = default) =>
            Task.FromResult<POSCustomer?>(null);

        public Task<POSCustomer?> FindByPlatformBusinessCustomerIdAsync(
            PosOrganizationId organizationId,
            Guid platformBusinessCustomerId,
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

        public Task<int> CountByPlatformBusinessCustomerIdAsync(
            PosOrganizationId organizationId,
            Guid platformBusinessCustomerId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(0);

        public Task<(IReadOnlyList<POSCustomer> Items, int TotalCount)> ListAsync(
            PosOrganizationId organizationId,
            CustomerStatus? status,
            string? search,
            int skip,
            int take,
            IReadOnlyCollection<Guid>? restrictToCustomerIds = null,
            bool peopleOnly = false,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(((IReadOnlyList<POSCustomer>)_items.Where(c => c.OrganizationId == organizationId).ToList(), _items.Count));

        public Task<(IReadOnlyList<POSCustomer> Items, int TotalCount)> ListUpdatedSinceAsync(
            PosOrganizationId organizationId,
            DateTimeOffset? sinceUtc,
            int skip,
            int take,
            CancellationToken cancellationToken = default) =>
            ListAsync(organizationId, null, null, skip, take, null, false, cancellationToken);

        public Task<IReadOnlyList<POSCustomer>> ListByIdsAsync(
            PosOrganizationId organizationId,
            IReadOnlyCollection<POSCustomerId> customerIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<POSCustomer>>(_items.Where(c => c.OrganizationId == organizationId).ToList());

        public Task AddAsync(POSCustomer customer, CancellationToken cancellationToken = default)
        {
            _items.Add(customer);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(POSCustomer customer, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class EmptyCreditRepository : ICreditEntryRepository
    {
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
            CancellationToken cancellationToken = default,
            IReadOnlySet<Guid>? historyBranchIds = null) =>
            Task.FromResult(((IReadOnlyList<CreditEntry>)Array.Empty<CreditEntry>(), 0));

        public Task<IReadOnlyList<CreditEntry>> ListActiveByOrganizationAsync(
            PosOrganizationId organizationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult((IReadOnlyList<CreditEntry>)Array.Empty<CreditEntry>());

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
            IReadOnlySet<Guid>? historyBranchIds = null) =>
            Task.FromResult(0m);

        public Task<int> CountActiveAsync(
            PosOrganizationId organizationId,
            POSCustomerId customerId,
            CancellationToken cancellationToken = default,
            IReadOnlySet<Guid>? historyBranchIds = null) =>
            Task.FromResult(0);

        public Task<(IReadOnlyList<CreditEntry> Items, int TotalCount)> ListCreatedSinceAsync(
            PosOrganizationId organizationId,
            DateTimeOffset? sinceUtc,
            int skip,
            int take,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(((IReadOnlyList<CreditEntry>)Array.Empty<CreditEntry>(), 0));

        public Task AddAsync(CreditEntry entry, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task UpdateAsync(CreditEntry entry, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class EmptyRepaymentRepository : IRepaymentRepository
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
            CancellationToken cancellationToken = default) =>
            Task.FromResult(0m);

        public Task<IReadOnlyDictionary<Guid, decimal>> SumActiveAmountsByOrganizationAsync(
            PosOrganizationId organizationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<Guid, decimal>>(new Dictionary<Guid, decimal>());

        public Task<int> CountActiveAsync(
            PosOrganizationId organizationId,
            POSCustomerId customerId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(0);

        public Task AddAsync(Repayment repayment, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task UpdateAsync(Repayment repayment, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
