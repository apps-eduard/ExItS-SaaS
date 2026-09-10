using ExItS.PinoyBusinessPOS.Application.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.UnitTests.Parties;

namespace ExItS.PinoyBusinessPOS.UnitTests.Customers;

public sealed class CheckoutBusinessCustomerSearchTests
{
    private static readonly Guid Org = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid BuyerOrg = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly DateTimeOffset Utc = DateTimeOffset.Parse("2026-09-10T08:00:00Z");

    [Fact]
    public async Task Business_kind_blank_search_includes_pos_org_linked_business_customer()
    {
        var repo = new InMemoryCustomers();
        var customer = POSCustomer.Create(
            PosOrganizationId.From(Org),
            "Kizy Bakery",
            Utc,
            linkedBuyerOrganizationId: BuyerOrg,
            linkedBuyerPublicOrganizationId: "ORG622085");
        await repo.AddAsync(customer);

        var (service, actor) = PartyBranchAccessTestSupport.Create();
        var queries = new POSCustomerQueryService(repo, service, actor, new EmptyRelationships());

        var result = await queries.SearchForCheckoutAsync(Org, search: null, page: 1, pageSize: 20, kind: "Business");

        Assert.True(result.IsSuccess, result.ErrorMessage);
        var hit = Assert.Single(result.Value!.Items);
        Assert.Equal(CheckoutCustomerSearchItemDto.KindCustomer, hit.Kind);
        Assert.Equal("Kizy Bakery", hit.DisplayName);
        Assert.Equal(customer.Id.Value, hit.CustomerId);
        Assert.Equal(BuyerOrg, hit.BuyerOrganizationId);
        Assert.Equal("ORG622085", hit.BuyerPublicOrganizationId);
        Assert.Equal(nameof(CustomerPartyKind.Business), hit.PartyKind);
    }

    [Fact]
    public async Task Business_kind_blank_search_includes_active_connection()
    {
        var connectionId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        var relationship = ConnectedSupplierRelationship.Rehydrate(
            ConnectedSupplierRelationshipId.From(connectionId),
            PosOrganizationId.From(BuyerOrg),
            PosOrganizationId.From(Org),
            ConnectedSupplierRelationshipStatus.Active,
            Utc,
            null,
            null,
            null,
            null,
            Utc,
            Utc,
            "Kizy Bakery",
            "ORG622085",
            "Seller Co",
            "ORG000001");

        var (service, actor) = PartyBranchAccessTestSupport.Create();
        var queries = new POSCustomerQueryService(
            new InMemoryCustomers(),
            service,
            actor,
            new FakeRelationships(relationship));

        var result = await queries.SearchForCheckoutAsync(Org, search: " ", page: 1, pageSize: 20, kind: "Business");

        Assert.True(result.IsSuccess, result.ErrorMessage);
        var hit = Assert.Single(result.Value!.Items);
        Assert.Equal(CheckoutCustomerSearchItemDto.KindBusiness, hit.Kind);
        Assert.Equal(connectionId, hit.ConnectionId);
        Assert.Equal(BuyerOrg, hit.BuyerOrganizationId);
    }

    private sealed class InMemoryCustomers : IPOSCustomerRepository
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
            CancellationToken cancellationToken = default) =>
            Task.FromResult<POSCustomer?>(null);

        public Task<POSCustomer?> FindByLinkedBuyerOrganizationIdAsync(
            PosOrganizationId organizationId,
            Guid linkedBuyerOrganizationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(c =>
                c.OrganizationId == organizationId
                && c.LinkedBuyerOrganizationId == linkedBuyerOrganizationId));

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
            CancellationToken cancellationToken = default)
        {
            var query = _items.Where(c => c.OrganizationId == organizationId);
            if (status is not null)
            {
                query = query.Where(c => c.Status == status);
            }

            var list = query.OrderBy(c => c.DisplayName).ToList();
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                list = list
                    .Where(c =>
                        c.DisplayName.Contains(term, StringComparison.OrdinalIgnoreCase)
                        || (c.LinkedBuyerPublicOrganizationId?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false))
                    .ToList();
            }

            return Task.FromResult(((IReadOnlyList<POSCustomer>)list.Skip(skip).Take(take).ToList(), list.Count));
        }

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
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<POSCustomer>>(
                _items.Where(c => c.OrganizationId == organizationId && customerIds.Contains(c.Id)).ToList());

        public Task UpdateAsync(POSCustomer customer, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class EmptyRelationships : IConnectedSupplierRelationshipRepository
    {
        public Task<ConnectedSupplierRelationship?> GetAsync(
            ConnectedSupplierRelationshipId id,
            CancellationToken ct = default) =>
            Task.FromResult<ConnectedSupplierRelationship?>(null);

        public Task<ConnectedSupplierRelationship?> FindOpenAsync(
            PosOrganizationId buyer,
            PosOrganizationId supplier,
            CancellationToken ct = default) =>
            Task.FromResult<ConnectedSupplierRelationship?>(null);

        public Task<IReadOnlyList<ConnectedSupplierRelationship>> ListAsync(
            PosOrganizationId organizationId,
            bool supplierView,
            CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<ConnectedSupplierRelationship>>([]);

        public Task AddAsync(ConnectedSupplierRelationship relationship, CancellationToken ct = default) =>
            Task.CompletedTask;

        public Task UpdateAsync(ConnectedSupplierRelationship relationship, CancellationToken ct = default) =>
            Task.CompletedTask;
    }

    private sealed class FakeRelationships(params ConnectedSupplierRelationship[] rows)
        : IConnectedSupplierRelationshipRepository
    {
        private readonly Dictionary<Guid, ConnectedSupplierRelationship> _byId =
            rows.ToDictionary(r => r.Id.Value);

        public Task<ConnectedSupplierRelationship?> GetAsync(
            ConnectedSupplierRelationshipId id,
            CancellationToken ct = default) =>
            Task.FromResult(_byId.TryGetValue(id.Value, out var row) ? row : null);

        public Task<ConnectedSupplierRelationship?> FindOpenAsync(
            PosOrganizationId buyer,
            PosOrganizationId supplier,
            CancellationToken ct = default) =>
            Task.FromResult(
                _byId.Values.FirstOrDefault(r =>
                    r.BuyerOrganizationId == buyer && r.SupplierOrganizationId == supplier));

        public Task<IReadOnlyList<ConnectedSupplierRelationship>> ListAsync(
            PosOrganizationId organizationId,
            bool supplierView,
            CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<ConnectedSupplierRelationship>>(
                _byId.Values
                    .Where(r =>
                        supplierView
                            ? r.SupplierOrganizationId == organizationId
                            : r.BuyerOrganizationId == organizationId)
                    .ToList());

        public Task AddAsync(ConnectedSupplierRelationship relationship, CancellationToken ct = default) =>
            Task.CompletedTask;

        public Task UpdateAsync(ConnectedSupplierRelationship relationship, CancellationToken ct = default) =>
            Task.CompletedTask;
    }
}
