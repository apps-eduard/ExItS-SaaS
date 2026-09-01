using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.UnitTests.Parties;

namespace ExItS.PinoyBusinessPOS.UnitTests.Customers;

public sealed class CreatePOSCustomerUseCaseTests
{
    private static CreatePOSCustomer CreateUseCase(InMemoryCustomerRepository repo, IClock clock)
    {
        var (service, actor) = PartyBranchAccessTestSupport.Create();
        return new CreatePOSCustomer(repo, new ImmediateUnitOfWork(), clock, service, actor);
    }

    private static POSCustomerQueryService CreateQueries(InMemoryCustomerRepository repo)
    {
        var (service, actor) = PartyBranchAccessTestSupport.Create();
        return new POSCustomerQueryService(repo, service, actor);
    }

    [Fact]
    public async Task Create_rejects_duplicate_active_mobile_in_same_organization()
    {
        var org = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var repo = new InMemoryCustomerRepository();
        var clock = new FixedClock(DateTimeOffset.Parse("2026-07-30T08:00:00Z"));
        var create = CreateUseCase(repo, clock);

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
        var create = CreateUseCase(repo, new FixedClock(DateTimeOffset.Parse("2026-07-30T08:00:00Z")));

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
        var create = CreateUseCase(repo, clock);
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
        var create = CreateUseCase(repo, new FixedClock(DateTimeOffset.Parse("2026-07-30T08:00:00Z")));

        Assert.True((await create.ExecuteAsync(orgA, "A", "09171234567", null, null)).IsSuccess);
        Assert.True((await create.ExecuteAsync(orgB, "B", "09171234567", null, null)).IsSuccess);
    }

    [Fact]
    public async Task Create_rejects_duplicate_platform_correlation_in_same_organization()
    {
        var org = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var platformId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var repo = new InMemoryCustomerRepository();
        var create = CreateUseCase(repo, new FixedClock(DateTimeOffset.Parse("2026-07-30T08:00:00Z")));

        var first = await create.ExecuteAsync(org, "One", null, null, null, platformBusinessCustomerId: platformId);
        Assert.True(first.IsSuccess);

        var second = await create.ExecuteAsync(org, "Two", null, null, null, platformBusinessCustomerId: platformId);
        Assert.False(second.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.PlatformBusinessCustomerCorrelationConflict, second.ErrorCode);
    }

    [Fact]
    public async Task Create_allows_same_platform_correlation_across_organizations()
    {
        var orgA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var orgB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var platformId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var repo = new InMemoryCustomerRepository();
        var create = CreateUseCase(repo, new FixedClock(DateTimeOffset.Parse("2026-07-30T08:00:00Z")));

        Assert.True((await create.ExecuteAsync(orgA, "A", null, null, null, platformBusinessCustomerId: platformId)).IsSuccess);
        Assert.True((await create.ExecuteAsync(orgB, "B", null, null, null, platformBusinessCustomerId: platformId)).IsSuccess);
    }

    [Fact]
    public async Task Create_rejects_duplicate_personal_exits_id_in_same_organization()
    {
        var org = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var repo = new InMemoryCustomerRepository();
        var create = CreateUseCase(repo, new FixedClock(DateTimeOffset.Parse("2026-07-30T08:00:00Z")));

        var first = await create.ExecuteAsync(org, "One", null, null, null, linkedPersonalPublicUserId: "EX-4827-1936");
        Assert.True(first.IsSuccess);
        Assert.Equal("EX-4827-1936", first.Value!.LinkedPersonalPublicUserId);

        var second = await create.ExecuteAsync(org, "Two", null, null, null, linkedPersonalPublicUserId: "ex-4827-1936");
        Assert.False(second.IsSuccess);
        Assert.Equal(DomainErrorCodes.CustomerExItsIdentityLinkConflict, second.ErrorCode);
    }

    [Fact]
    public async Task Create_rejects_personal_exits_id_already_tagged_in_notes()
    {
        var org = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var repo = new InMemoryCustomerRepository();
        var create = CreateUseCase(repo, new FixedClock(DateTimeOffset.Parse("2026-07-30T08:00:00Z")));

        var legacy = await create.ExecuteAsync(org, "Eduardo", null, null, "exits-id:EX-4827-1936");
        Assert.True(legacy.IsSuccess);
        Assert.Null(legacy.Value!.LinkedPersonalPublicUserId);

        var second = await create.ExecuteAsync(org, "Clone", null, null, null, linkedPersonalPublicUserId: "EX-4827-1936");
        Assert.False(second.IsSuccess);
        Assert.Equal(DomainErrorCodes.CustomerExItsIdentityLinkConflict, second.ErrorCode);
    }

    [Fact]
    public async Task Correlate_is_idempotent_and_rejects_conflicts()
    {
        var org = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var platformId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var otherId = Guid.Parse("ffffffff-bbbb-cccc-dddd-eeeeeeeeeeee");
        var repo = new InMemoryCustomerRepository();
        var clock = new FixedClock(DateTimeOffset.Parse("2026-07-30T08:00:00Z"));
        var uow = new ImmediateUnitOfWork();
        var create = CreateUseCase(repo, clock);
        var correlate = new CorrelatePOSCustomerToPlatformBusinessCustomer(repo, uow, clock);

        var first = await create.ExecuteAsync(org, "One", null, null, null);
        var second = await create.ExecuteAsync(org, "Two", null, null, null);
        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);

        var correlated = await correlate.ExecuteAsync(org, first.Value!.Id.Value, platformId);
        Assert.True(correlated.IsSuccess);
        var again = await correlate.ExecuteAsync(org, first.Value.Id.Value, platformId);
        Assert.True(again.IsSuccess);

        var taken = await correlate.ExecuteAsync(org, second.Value!.Id.Value, platformId);
        Assert.False(taken.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.PlatformBusinessCustomerCorrelationConflict, taken.ErrorCode);

        var different = await correlate.ExecuteAsync(org, first.Value.Id.Value, otherId);
        Assert.False(different.IsSuccess);
        Assert.Equal(DomainErrorCodes.PlatformBusinessCustomerCorrelationConflict, different.ErrorCode);
    }

    [Fact]
    public async Task Clear_correlation_is_idempotent_and_lookup_is_org_scoped()
    {
        var orgA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var orgB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var platformId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var repo = new InMemoryCustomerRepository();
        var clock = new FixedClock(DateTimeOffset.Parse("2026-07-30T08:00:00Z"));
        var uow = new ImmediateUnitOfWork();
        var create = CreateUseCase(repo, clock);
        var clear = new ClearPOSCustomerPlatformCorrelation(repo, uow, clock);
        var queries = CreateQueries(repo);

        var created = await create.ExecuteAsync(orgA, "Rosa", null, null, null, platformBusinessCustomerId: platformId);
        Assert.True(created.IsSuccess);

        Assert.NotNull(await queries.GetByPlatformBusinessCustomerIdAsync(orgA, platformId));
        Assert.Null(await queries.GetByPlatformBusinessCustomerIdAsync(orgB, platformId));

        var cleared = await clear.ExecuteAsync(orgA, created.Value!.Id.Value);
        Assert.True(cleared.IsSuccess);
        Assert.Null(cleared.Value!.PlatformBusinessCustomerId);
        var again = await clear.ExecuteAsync(orgA, created.Value.Id.Value);
        Assert.True(again.IsSuccess);
        Assert.Null(await queries.GetByPlatformBusinessCustomerIdAsync(orgA, platformId));
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
            CancellationToken cancellationToken = default)
        {
            var normalized = linkedPersonalPublicUserId.Trim().ToUpperInvariant();
            return Task.FromResult(_items.FirstOrDefault(c =>
                c.OrganizationId == organizationId
                && c.LinkedPersonalPublicUserId == normalized));
        }

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
            var query = _items.Where(c => c.OrganizationId == organizationId);
            if (status is not null)
            {
                query = query.Where(c => c.Status == status);
            }

            var list = query.OrderBy(c => c.DisplayName).ThenBy(c => c.Id.Value).ToList();
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                list = list.Where(c =>
                        c.DisplayName.Contains(term, StringComparison.OrdinalIgnoreCase)
                        || (c.Notes is not null && c.Notes.Contains(term, StringComparison.OrdinalIgnoreCase))
                        || (c.LinkedPersonalPublicUserId is not null
                            && c.LinkedPersonalPublicUserId.Contains(term, StringComparison.OrdinalIgnoreCase)))
                    .ToList();
            }

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
