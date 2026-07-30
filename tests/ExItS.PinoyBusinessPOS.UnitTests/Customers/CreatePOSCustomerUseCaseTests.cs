using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.UnitTests.Customers;

public sealed class CreatePOSCustomerUseCaseTests
{
    [Fact]
    public async Task Create_rejects_duplicate_active_mobile_in_same_organization()
    {
        var org = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var repo = new InMemoryCustomerRepository();
        var clock = new FixedClock(DateTimeOffset.Parse("2026-07-30T08:00:00Z"));
        var create = new CreatePOSCustomer(repo, new ImmediateUnitOfWork(), clock);

        var first = await create.ExecuteAsync(org, "One", "09171234567", null, null);
        Assert.True(first.IsSuccess);

        var second = await create.ExecuteAsync(org, "Two", "0917-123-4567", null, null);
        Assert.False(second.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.MobileConflict, second.ErrorCode);
    }

    [Fact]
    public async Task Create_with_clientCustomerId_uses_that_id()
    {
        var org = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var clientId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        var repo = new InMemoryCustomerRepository();
        var create = new CreatePOSCustomer(repo, new ImmediateUnitOfWork(), new FixedClock(DateTimeOffset.Parse("2026-07-30T08:00:00Z")));

        var result = await create.ExecuteAsync(org, "Client Id Customer", "09170001111", "Addr", "Notes", clientId);
        Assert.True(result.IsSuccess);
        Assert.Equal(clientId, result.Value!.Id.Value);
    }

    [Fact]
    public async Task Update_with_mismatched_expectedUpdatedAtUtc_returns_concurrency_conflict()
    {
        var org = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var repo = new InMemoryCustomerRepository();
        var clock = new FixedClock(DateTimeOffset.Parse("2026-07-30T08:00:00Z"));
        var create = new CreatePOSCustomer(repo, new ImmediateUnitOfWork(), clock);
        var update = new UpdatePOSCustomer(repo, new ImmediateUnitOfWork(), clock);

        var created = await create.ExecuteAsync(org, "Original", "09175556666", null, null);
        Assert.True(created.IsSuccess);

        var stale = created.Value!.UpdatedAtUtc.AddMinutes(-5);
        var result = await update.ExecuteAsync(
            org,
            created.Value.Id.Value,
            "Updated",
            "09175556666",
            null,
            null,
            stale);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.CustomerConcurrencyConflict, result.ErrorCode);
    }

    [Fact]
    public async Task Create_allows_same_mobile_across_organizations()
    {
        var orgA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var orgB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var repo = new InMemoryCustomerRepository();
        var create = new CreatePOSCustomer(repo, new ImmediateUnitOfWork(), new FixedClock(DateTimeOffset.Parse("2026-07-30T08:00:00Z")));

        Assert.True((await create.ExecuteAsync(orgA, "A", "09171234567", null, null)).IsSuccess);
        Assert.True((await create.ExecuteAsync(orgB, "B", "09171234567", null, null)).IsSuccess);
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
            Task.FromResult(_items.FirstOrDefault(c => c.OrganizationId == organizationId && c.Id == customerId));

        public Task<POSCustomer?> FindActiveByNormalizedMobileAsync(PosOrganizationId organizationId, string normalizedMobile, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(c =>
                c.OrganizationId == organizationId
                && c.Status == CustomerStatus.Active
                && c.NormalizedMobile == normalizedMobile));

        public Task<(IReadOnlyList<POSCustomer> Items, int TotalCount)> ListAsync(
            PosOrganizationId organizationId,
            CustomerStatus? status,
            string? search,
            int skip,
            int take,
            CancellationToken cancellationToken = default)
        {
            var query = _items.Where(c => c.OrganizationId == organizationId);
            if (status is not null)
            {
                query = query.Where(c => c.Status == status);
            }

            var list = query.OrderBy(c => c.DisplayName).ThenBy(c => c.Id.Value).ToList();
            return Task.FromResult(((IReadOnlyList<POSCustomer>)list.Skip(skip).Take(take).ToList(), list.Count));
        }

        public Task<(IReadOnlyList<POSCustomer> Items, int TotalCount)> ListUpdatedSinceAsync(
            PosOrganizationId organizationId,
            DateTimeOffset? sinceUtc,
            int skip,
            int take,
            CancellationToken cancellationToken = default)
        {
            var query = _items.Where(c => c.OrganizationId == organizationId);
            if (sinceUtc is not null)
            {
                var since = sinceUtc.Value.ToUniversalTime();
                query = query.Where(c => c.UpdatedAtUtc.ToUniversalTime() > since);
            }

            var list = query.OrderBy(c => c.UpdatedAtUtc).ThenBy(c => c.Id.Value).ToList();
            return Task.FromResult(((IReadOnlyList<POSCustomer>)list.Skip(skip).Take(take).ToList(), list.Count));
        }

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

        public Task UpdateAsync(POSCustomer customer, CancellationToken cancellationToken = default)
        {
            var index = _items.FindIndex(c => c.Id == customer.Id);
            if (index >= 0)
            {
                _items[index] = customer;
            }

            return Task.CompletedTask;
        }
    }
}
