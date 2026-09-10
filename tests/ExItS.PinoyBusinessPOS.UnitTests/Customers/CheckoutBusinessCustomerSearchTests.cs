using ExItS.PinoyBusinessPOS.Application.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Application.Credit;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Application.Payments;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Credit;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Payments;
using ExItS.PinoyBusinessPOS.UnitTests.Parties;

namespace ExItS.PinoyBusinessPOS.UnitTests.Customers;

public sealed class CheckoutBusinessCustomerSearchTests
{
    private static readonly Guid Org = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid BuyerOrg = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid Actor = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
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

        var queries = CreateQueries(repo);

        var result = await queries.SearchForCheckoutAsync(Org, search: null, page: 1, pageSize: 20, kind: "Business");

        Assert.True(result.IsSuccess, result.ErrorMessage);
        var hit = Assert.Single(result.Value!.Items);
        Assert.Equal(CheckoutCustomerSearchItemDto.KindCustomer, hit.Kind);
        Assert.Equal("Kizy Bakery", hit.DisplayName);
        Assert.Equal(customer.Id.Value, hit.CustomerId);
        Assert.Equal(BuyerOrg, hit.BuyerOrganizationId);
        Assert.Equal("ORG622085", hit.BuyerPublicOrganizationId);
        Assert.Equal(nameof(CustomerPartyKind.Business), hit.PartyKind);
        Assert.Null(hit.CreditStatus);
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

        var queries = CreateQueries(new InMemoryCustomers(), new FakeRelationships(relationship));

        var result = await queries.SearchForCheckoutAsync(Org, search: " ", page: 1, pageSize: 20, kind: "Business");

        Assert.True(result.IsSuccess, result.ErrorMessage);
        var hit = Assert.Single(result.Value!.Items);
        Assert.Equal(CheckoutCustomerSearchItemDto.KindBusiness, hit.Kind);
        Assert.Equal(connectionId, hit.ConnectionId);
        Assert.Equal(BuyerOrg, hit.BuyerOrganizationId);
        Assert.Null(hit.CreditStatus);
    }

    [Fact]
    public async Task All_kind_blank_search_includes_active_people_and_businesses()
    {
        var repo = new InMemoryCustomers();
        var person = POSCustomer.Create(PosOrganizationId.From(Org), "Rosa Santos", Utc);
        var business = POSCustomer.Create(
            PosOrganizationId.From(Org),
            "Kizy Bakery",
            Utc,
            linkedBuyerOrganizationId: BuyerOrg,
            linkedBuyerPublicOrganizationId: "ORG622085");
        await repo.AddAsync(person);
        await repo.AddAsync(business);

        var queries = CreateQueries(repo);

        var result = await queries.SearchForCheckoutAsync(Org, search: null, page: 1, pageSize: 20, kind: "All");

        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.Equal(2, result.Value!.Items.Count);
        Assert.Contains(result.Value.Items, i =>
            i.Kind == CheckoutCustomerSearchItemDto.KindCustomer
            && i.CustomerId == person.Id.Value
            && i.DisplayName == "Rosa Santos");
        Assert.Contains(result.Value.Items, i =>
            i.Kind == CheckoutCustomerSearchItemDto.KindCustomer
            && i.CustomerId == business.Id.Value
            && i.PartyKind == nameof(CustomerPartyKind.Business));
    }

    [Fact]
    public async Task Customer_kind_blank_search_projects_credit_fields_for_person_rows()
    {
        var repo = new InMemoryCustomers();
        var person = POSCustomer.Create(PosOrganizationId.From(Org), "Rosa Santos", Utc);
        await repo.AddAsync(person);

        var policies = new InMemoryCreditPolicies();
        var (policy, change) = CustomerCreditPolicy.Configure(
            PosOrganizationId.From(Org),
            person.Id,
            creditLimit: 1_000m,
            defaultTermDays: 30,
            Actor,
            reason: null,
            Utc);
        await policies.AddAsync(policy);
        await policies.AddChangeAsync(change);
        var approve = policy.Approve(Actor, "ok", Utc.AddSeconds(1));
        await policies.UpdateAsync(policy);
        await policies.AddChangeAsync(approve);

        var outstanding = new OutstandingBalanceService(
            new EmptyCredits(),
            new EmptyRepayments(),
            new InMemoryWriteOffRepository(),
            new FixedClock(Utc));
        var queries = CreateQueries(repo, policies: policies, outstanding: outstanding);

        var result = await queries.SearchForCheckoutAsync(Org, search: null, page: 1, pageSize: 20, kind: "Customer");

        Assert.True(result.IsSuccess, result.ErrorMessage);
        var hit = Assert.Single(result.Value!.Items);
        Assert.Equal(nameof(CustomerCreditPolicyStatus.Approved), hit.CreditStatus);
        Assert.Equal(1_000m, hit.CreditLimit);
        Assert.Equal(0m, hit.OutstandingAmount);
        Assert.Equal(1_000m, hit.AvailableCredit);
        Assert.Equal(30, hit.DefaultTermDays);
    }

    [Fact]
    public async Task Customer_kind_blank_search_projects_not_configured_when_no_policy()
    {
        var repo = new InMemoryCustomers();
        var person = POSCustomer.Create(PosOrganizationId.From(Org), "Rosa Santos", Utc);
        await repo.AddAsync(person);

        var queries = CreateQueries(repo);

        var result = await queries.SearchForCheckoutAsync(Org, search: " ", page: 1, pageSize: 20, kind: "Customer");

        Assert.True(result.IsSuccess, result.ErrorMessage);
        var hit = Assert.Single(result.Value!.Items);
        Assert.Equal(nameof(CustomerCreditPolicyStatus.NotConfigured), hit.CreditStatus);
        Assert.Null(hit.CreditLimit);
        Assert.Equal(0m, hit.OutstandingAmount);
        Assert.Equal(0m, hit.AvailableCredit);
        Assert.Null(hit.DefaultTermDays);
    }

    private static POSCustomerQueryService CreateQueries(
        IPOSCustomerRepository customers,
        IConnectedSupplierRelationshipRepository? relationships = null,
        ICustomerCreditPolicyRepository? policies = null,
        IOutstandingBalanceService? outstanding = null)
    {
        var (service, actor) = PartyBranchAccessTestSupport.Create();
        outstanding ??= new OutstandingBalanceService(
            new EmptyCredits(),
            new EmptyRepayments(),
            new InMemoryWriteOffRepository(),
            new FixedClock(Utc));
        return new POSCustomerQueryService(
            customers,
            service,
            actor,
            relationships ?? new EmptyRelationships(),
            policies ?? new InMemoryCreditPolicies(),
            outstanding);
    }

    private sealed class FixedClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
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
            bool peopleOnly = false,
            CancellationToken cancellationToken = default)
        {
            var query = _items.Where(c => c.OrganizationId == organizationId);
            if (status is not null)
            {
                query = query.Where(c => c.Status == status);
            }

            if (peopleOnly)
            {
                query = query.Where(c =>
                    c.LinkedBuyerOrganizationId is null
                    && c.PartyKind != CustomerPartyKind.Business);
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

    private sealed class InMemoryCreditPolicies : ICustomerCreditPolicyRepository
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

    private sealed class EmptyCredits : ICreditEntryRepository
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

        public Task<(IReadOnlyList<CreditEntry> Items, int TotalCount)> ListCreatedSinceAsync(
            PosOrganizationId organizationId,
            DateTimeOffset? sinceUtc,
            int skip,
            int take,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(((IReadOnlyList<CreditEntry>)Array.Empty<CreditEntry>(), 0));

        public Task<IReadOnlyList<CreditEntry>> ListActiveByOrganizationAsync(
            PosOrganizationId organizationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CreditEntry>>([]);

        public Task<IReadOnlyList<CreditEntry>> ListRecordedInRangeAsync(
            PosOrganizationId organizationId,
            DateOnly fromDateUtc,
            DateOnly toDateUtc,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CreditEntry>>([]);

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

        public Task AddAsync(CreditEntry entry, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task UpdateAsync(CreditEntry entry, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class EmptyRepayments : IRepaymentRepository
    {
        public Task<Repayment?> GetByIdAsync(
            PosOrganizationId organizationId,
            RepaymentId repaymentId,
            CancellationToken cancellationToken = default) =>
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
            Task.FromResult<IReadOnlyList<Repayment>>([]);

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
