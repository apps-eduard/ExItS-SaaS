using ExItS.PinoyBuyNowPayLater.Application.Common;
using ExItS.PinoyBuyNowPayLater.Application.Customers;
using ExItS.PinoyBuyNowPayLater.Domain.Customers;

namespace ExItS.PinoyBuyNowPayLater.UnitTests.Customers;

public sealed class BnplCustomerUseCaseTests
{
    private static readonly Guid OrgA = Guid.Parse("11111111-1111-4111-8111-111111111111");
    private static readonly Guid OrgB = Guid.Parse("22222222-2222-4222-8222-222222222222");
    private static readonly Guid CustomerId = Guid.Parse("33333333-3333-4333-8333-333333333333");
    private static readonly Guid CommerceId = Guid.Parse("44444444-4444-4444-8444-444444444444");

    [Fact]
    public async Task Create_with_stable_id_converges_on_compatible_retry()
    {
        var harness = CreateHarness();
        var first = await harness.Create.ExecuteAsync(OrgA, "Maria Santos", CustomerId, mobile: "09171234567");
        Assert.True(first.IsSuccess);

        var retry = await harness.Create.ExecuteAsync(OrgA, "Maria Santos", CustomerId, mobile: "09171234567");
        Assert.True(retry.IsSuccess);
        Assert.Equal(CustomerId, retry.Value!.Id.Value);
        Assert.Equal(1, harness.Repository.Count);
    }

    [Fact]
    public async Task Create_same_id_conflicting_payload_returns_409()
    {
        var harness = CreateHarness();
        Assert.True((await harness.Create.ExecuteAsync(OrgA, "Maria Santos", CustomerId)).IsSuccess);

        var conflict = await harness.Create.ExecuteAsync(OrgA, "Other Name", CustomerId);
        Assert.False(conflict.IsSuccess);
        Assert.Equal(BnplCustomerErrorCodes.IdempotencyConflict, conflict.ErrorCode);
        Assert.Equal(409, conflict.SuggestedHttpStatus);
    }

    [Fact]
    public async Task Organization_isolation_prevents_cross_org_get()
    {
        var harness = CreateHarness();
        Assert.True((await harness.Create.ExecuteAsync(OrgA, "Maria Santos", CustomerId)).IsSuccess);

        var missing = await harness.Get.ExecuteAsync(OrgB, CustomerId);
        Assert.False(missing.IsSuccess);
        Assert.Equal(BnplCustomerErrorCodes.NotFound, missing.ErrorCode);
    }

    [Fact]
    public async Task Personal_link_duplicate_same_org_blocked_different_org_allowed()
    {
        var harness = CreateHarness();
        Assert.True((await harness.Create.ExecuteAsync(
            OrgA,
            "Customer A",
            Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa"),
            linkedPersonalPublicUserId: "EX-1234-5678")).IsSuccess);

        var sameOrg = await harness.Create.ExecuteAsync(
            OrgA,
            "Customer B",
            Guid.Parse("bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb"),
            linkedPersonalPublicUserId: "EX-1234-5678");
        Assert.False(sameOrg.IsSuccess);
        Assert.Equal(BnplCustomerErrorCodes.PersonalLinkConflict, sameOrg.ErrorCode);

        var otherOrg = await harness.Create.ExecuteAsync(
            OrgB,
            "Customer C",
            Guid.Parse("cccccccc-cccc-4ccc-8ccc-cccccccccccc"),
            linkedPersonalPublicUserId: "EX-1234-5678");
        Assert.True(otherOrg.IsSuccess);
    }

    [Fact]
    public async Task Commerce_link_duplicate_same_org_blocked()
    {
        var harness = CreateHarness();
        Assert.True((await harness.Create.ExecuteAsync(
            OrgA,
            "Customer A",
            Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa"),
            linkedCommerceCustomerId: CommerceId)).IsSuccess);

        var conflict = await harness.Create.ExecuteAsync(
            OrgA,
            "Customer B",
            Guid.Parse("bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb"),
            linkedCommerceCustomerId: CommerceId);
        Assert.False(conflict.IsSuccess);
        Assert.Equal(BnplCustomerErrorCodes.CommerceLinkConflict, conflict.ErrorCode);
    }

    [Fact]
    public async Task Profile_update_keeps_identity_immutable_and_email_is_not_auth()
    {
        var harness = CreateHarness();
        Assert.True((await harness.Create.ExecuteAsync(
            OrgA,
            "Maria Santos",
            CustomerId,
            email: "maria@example.com")).IsSuccess);

        var updated = await harness.Update.ExecuteAsync(
            OrgA,
            CustomerId,
            "Maria S.",
            mobile: "09170001111",
            email: "new@example.com");
        Assert.True(updated.IsSuccess);
        Assert.Equal(CustomerId, updated.Value!.Id.Value);
        Assert.Equal(OrgA, updated.Value.OrganizationId);
        Assert.Equal("new@example.com", updated.Value.Email);
        Assert.Null(updated.Value.LinkedPersonalPublicUserId);
    }

    [Fact]
    public async Task Search_is_organization_scoped()
    {
        var harness = CreateHarness();
        Assert.True((await harness.Create.ExecuteAsync(OrgA, "Alpha", Guid.NewGuid())).IsSuccess);
        Assert.True((await harness.Create.ExecuteAsync(OrgB, "Alpha", Guid.NewGuid())).IsSuccess);

        var page = await harness.Search.ExecuteAsync(OrgA, "Alpha", null, 1, 20);
        Assert.True(page.IsSuccess);
        Assert.Equal(1, page.Value!.TotalCount);
        Assert.All(page.Value.Items, c => Assert.Equal(OrgA, c.OrganizationId));
    }

    [Fact]
    public void Customer_is_not_staff_and_has_no_financing_state()
    {
        var customer = BnplCustomer.Create(OrgA, "Buyer", DateTimeOffset.UtcNow);
        Assert.Equal(BnplCustomerStatus.Active, customer.Status);
        Assert.Null(customer.LinkedPersonalPublicUserId);
        Assert.DoesNotContain("Eligible", customer.Status.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Approve", typeof(BnplCustomer).GetProperties().Select(p => p.Name), StringComparer.Ordinal);
    }

    private static Harness CreateHarness()
    {
        var repo = new InMemoryBnplCustomerRepository();
        var uow = new NoOpUnitOfWork();
        var clock = new FixedClock(DateTimeOffset.Parse("2026-08-27T00:00:00Z"));
        return new Harness(
            repo,
            new CreateBnplCustomer(repo, uow, clock),
            new GetBnplCustomer(repo),
            new SearchBnplCustomers(repo),
            new UpdateBnplCustomerProfile(repo, uow, clock));
    }

    private sealed record Harness(
        InMemoryBnplCustomerRepository Repository,
        CreateBnplCustomer Create,
        GetBnplCustomer Get,
        SearchBnplCustomers Search,
        UpdateBnplCustomerProfile Update);

    private sealed class FixedClock(DateTimeOffset utcNow) : IBnplClock
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
    }

    private sealed class NoOpUnitOfWork : IBnplUnitOfWork
    {
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class InMemoryBnplCustomerRepository : IBnplCustomerRepository
    {
        private readonly Dictionary<(Guid Org, Guid Id), BnplCustomer> _items = new();

        public int Count => _items.Count;

        public Task<BnplCustomer?> GetByIdAsync(
            Guid organizationId,
            BnplCustomerId customerId,
            CancellationToken cancellationToken = default)
        {
            _items.TryGetValue((organizationId, customerId.Value), out var customer);
            return Task.FromResult(customer);
        }

        public Task<BnplCustomer?> FindByLinkedPersonalPublicUserIdAsync(
            Guid organizationId,
            string linkedPersonalPublicUserId,
            CancellationToken cancellationToken = default)
        {
            var match = _items.Values.FirstOrDefault(c =>
                c.OrganizationId == organizationId
                && string.Equals(c.LinkedPersonalPublicUserId, linkedPersonalPublicUserId, StringComparison.Ordinal));
            return Task.FromResult(match);
        }

        public Task<BnplCustomer?> FindByLinkedCommerceCustomerIdAsync(
            Guid organizationId,
            Guid linkedCommerceCustomerId,
            CancellationToken cancellationToken = default)
        {
            var match = _items.Values.FirstOrDefault(c =>
                c.OrganizationId == organizationId
                && c.LinkedCommerceCustomerId == linkedCommerceCustomerId);
            return Task.FromResult(match);
        }

        public Task<(IReadOnlyList<BnplCustomer> Items, int TotalCount)> SearchAsync(
            Guid organizationId,
            string? search,
            BnplCustomerStatus? status,
            int skip,
            int take,
            CancellationToken cancellationToken = default)
        {
            IEnumerable<BnplCustomer> query = _items.Values.Where(c => c.OrganizationId == organizationId);
            if (status is not null)
            {
                query = query.Where(c => c.Status == status);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(c =>
                    c.DisplayName.Contains(term, StringComparison.OrdinalIgnoreCase)
                    || (c.Email?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false)
                    || (c.Mobile?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false));
            }

            var list = query.OrderBy(c => c.DisplayName).ToList();
            return Task.FromResult(((IReadOnlyList<BnplCustomer>)list.Skip(skip).Take(take).ToList(), list.Count));
        }

        public Task AddAsync(BnplCustomer customer, CancellationToken cancellationToken = default)
        {
            _items[(customer.OrganizationId, customer.Id.Value)] = customer;
            return Task.CompletedTask;
        }

        public Task UpdateAsync(BnplCustomer customer, CancellationToken cancellationToken = default)
        {
            _items[(customer.OrganizationId, customer.Id.Value)] = customer;
            return Task.CompletedTask;
        }
    }
}
