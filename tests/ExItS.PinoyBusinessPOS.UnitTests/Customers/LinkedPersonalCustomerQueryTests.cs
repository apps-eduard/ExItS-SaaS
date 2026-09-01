using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.UnitTests.Parties;

namespace ExItS.PinoyBusinessPOS.UnitTests.Customers;

public sealed class LinkedPersonalCustomerQueryTests
{
    [Fact]
    public async Task GetByLinkedPersonalPublicUserIdForCheckout_returns_active_customer_in_same_org_only()
    {
        var orgA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var orgB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var publicId = "EX-4827-1936";
        var repo = new LinkedPersonalInMemoryCustomerRepository();
        var (service, actor) = PartyBranchAccessTestSupport.Create();
        var queries = new POSCustomerQueryService(repo, service, actor);

        var now = DateTimeOffset.Parse("2026-08-01T00:00:00Z");
        var active = POSCustomer.Create(
            PosOrganizationId.From(orgA),
            "Rosa",
            now,
            linkedPersonalPublicUserId: publicId);
        var inactive = POSCustomer.Create(
            PosOrganizationId.From(orgA),
            "Inactive",
            now,
            linkedPersonalPublicUserId: "EX-1111-2222");
        inactive.Deactivate(now);
        var otherOrg = POSCustomer.Create(
            PosOrganizationId.From(orgB),
            "Other org",
            now,
            linkedPersonalPublicUserId: publicId);

        await repo.AddAsync(active);
        await repo.AddAsync(inactive);
        await repo.AddAsync(otherOrg);

        var found = await queries.GetByLinkedPersonalPublicUserIdForCheckoutAsync(orgA, publicId);
        Assert.NotNull(found);
        Assert.Equal(active.Id.Value, found!.CustomerId);
        Assert.Equal("Rosa", found.DisplayName);

        Assert.Null(await queries.GetByLinkedPersonalPublicUserIdForCheckoutAsync(orgA, "EX-1111-2222"));

        var orgBMatch = await queries.GetByLinkedPersonalPublicUserIdForCheckoutAsync(orgB, publicId);
        Assert.NotNull(orgBMatch);
        Assert.Equal("Other org", orgBMatch!.DisplayName);
        Assert.Null(await queries.GetByLinkedPersonalPublicUserIdForCheckoutAsync(orgA, "EX-NOT-FOUND-00"));
    }

    private sealed class LinkedPersonalInMemoryCustomerRepository : IPOSCustomerRepository
    {
        private readonly List<POSCustomer> _items = [];

        public Task AddAsync(POSCustomer customer, CancellationToken cancellationToken = default)
        {
            _items.Add(customer);
            return Task.CompletedTask;
        }

        public Task<POSCustomer?> GetByIdAsync(
            PosOrganizationId organizationId,
            POSCustomerId customerId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(c => c.OrganizationId == organizationId && c.Id == customerId));

        public Task<POSCustomer?> FindActiveByNormalizedMobileAsync(
            PosOrganizationId organizationId,
            string normalizedMobile,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<POSCustomer?>(null);

        public Task<POSCustomer?> FindByPlatformBusinessCustomerIdAsync(
            PosOrganizationId organizationId,
            Guid platformBusinessCustomerId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<POSCustomer?>(null);

        public Task<POSCustomer?> FindByLinkedPersonalPublicUserIdAsync(
            PosOrganizationId organizationId,
            string linkedPersonalPublicUserId,
            CancellationToken cancellationToken = default)
        {
            var normalized = linkedPersonalPublicUserId.Trim().ToUpperInvariant();
            return Task.FromResult(_items.FirstOrDefault(c =>
                c.OrganizationId == organizationId
                && string.Equals(c.LinkedPersonalPublicUserId, normalized, StringComparison.Ordinal)));
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
            Task.FromResult(0);

        public Task<(IReadOnlyList<POSCustomer> Items, int TotalCount)> ListAsync(
            PosOrganizationId organizationId,
            CustomerStatus? status,
            string? search,
            int skip,
            int take, IReadOnlyCollection<Guid>? restrictToCustomerIds = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(((IReadOnlyList<POSCustomer>)Array.Empty<POSCustomer>(), 0));

        public Task<(IReadOnlyList<POSCustomer> Items, int TotalCount)> ListUpdatedSinceAsync(
            PosOrganizationId organizationId,
            DateTimeOffset? sinceUtc,
            int skip,
            int take,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(((IReadOnlyList<POSCustomer>)Array.Empty<POSCustomer>(), 0));

        public Task<IReadOnlyList<POSCustomer>> ListByIdsAsync(
            PosOrganizationId organizationId,
            IReadOnlyCollection<POSCustomerId> customerIds,
            CancellationToken cancellationToken = default)
        {
            var ids = customerIds.ToHashSet();
            return Task.FromResult<IReadOnlyList<POSCustomer>>(
                _items.Where(c => c.OrganizationId == organizationId && ids.Contains(c.Id)).ToList());
        }

        public Task UpdateAsync(POSCustomer customer, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
