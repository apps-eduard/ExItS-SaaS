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
