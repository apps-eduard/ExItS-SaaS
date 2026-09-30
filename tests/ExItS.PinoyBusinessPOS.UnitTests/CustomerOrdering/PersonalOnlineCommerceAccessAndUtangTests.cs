using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Credit;
using ExItS.PinoyBusinessPOS.Application.CustomerOrdering;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Application.Payments;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Credit;
using ExItS.PinoyBusinessPOS.Domain.CustomerOrdering;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Payments;

namespace ExItS.PinoyBusinessPOS.UnitTests.CustomerOrdering;

public sealed class PersonalOnlineCommerceAccessAndUtangTests
{
    private static readonly Guid Seller = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid Branch = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly Guid Actor = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
    private static readonly Guid ProductGuid = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid BusinessCustomer = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid LinkedId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly DateTimeOffset Utc = new(2026, 8, 17, 12, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(false, CustomerOnlineOrderingAccess.Default)]
    [InlineData(false, CustomerOnlineOrderingAccess.Allowed)]
    [InlineData(false, CustomerOnlineOrderingAccess.Blocked)]
    public async Task Master_off_denies_shopping_even_when_customer_allowed(
        bool masterOn,
        CustomerOnlineOrderingAccess access)
    {
        _ = masterOn;
        var auth = CreateShoppingAuth(canOrder: false, access);
        var result = await auth.AuthorizeShoppingAsync(Seller, Actor, BusinessCustomer);
        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.CustomerOrderOrderingUnavailable, result.ErrorCode);
        Assert.Equal(CustomerOnlineOrderingAccessRules.StoreNotAcceptingMessage, result.ErrorMessage);
    }

    [Theory]
    [InlineData(CustomerOnlineOrderingAccess.Default)]
    [InlineData(CustomerOnlineOrderingAccess.Allowed)]
    public async Task Master_on_default_or_allowed_with_active_link_allows(CustomerOnlineOrderingAccess access)
    {
        var auth = CreateShoppingAuth(canOrder: true, access);
        var result = await auth.AuthorizeShoppingAsync(Seller, Actor, BusinessCustomer);
        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.Equal(access, result.Value!.Access);
    }

    [Fact]
    public async Task Master_on_blocked_denies_shopping()
    {
        var auth = CreateShoppingAuth(canOrder: true, CustomerOnlineOrderingAccess.Blocked);
        var result = await auth.AuthorizeShoppingAsync(Seller, Actor, BusinessCustomer);
        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.CustomerOrderCustomerBlocked, result.ErrorCode);
    }

    [Fact]
    public async Task Revoked_link_denies_shopping()
    {
        var customers = new InMemoryCustomers();
        var pos = POSCustomer.Create(
            PosOrganizationId.From(Seller),
            "Ana",
            Utc,
            platformBusinessCustomerId: BusinessCustomer);
        await customers.AddAsync(pos);
        var auth = new PersonalOnlineCommerceAuthorization(
            new FixedCapability(false),
            new FixedLink(authorized: false),
            customers);
        var result = await auth.AuthorizeShoppingAsync(Seller, Actor, BusinessCustomer);
        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.LinkedCustomerNotFound, result.ErrorCode);
    }

    [Theory]
    [InlineData(CustomerCreditPolicyStatus.NotConfigured)]
    [InlineData(CustomerCreditPolicyStatus.PendingApproval)]
    [InlineData(CustomerCreditPolicyStatus.Disabled)]
    public async Task Online_utang_denied_when_policy_not_approved(CustomerCreditPolicyStatus status)
    {
        var customerId = POSCustomerId.New();
        var policies = new InMemoryCreditPolicies();
        if (status != CustomerCreditPolicyStatus.NotConfigured)
        {
            var (policy, _) = CustomerCreditPolicy.Configure(
                PosOrganizationId.From(Seller),
                customerId,
                5000m,
                30,
                Actor,
                reason: null,
                Utc);
            if (status == CustomerCreditPolicyStatus.Approved)
            {
                policy.Approve(Actor, "ok", Utc);
            }
            else if (status == CustomerCreditPolicyStatus.Disabled)
            {
                policy.Approve(Actor, "ok", Utc);
                policy.Disable(Actor, "pause", Utc);
            }

            await policies.AddAsync(policy);
        }

        var service = new CustomerCreditAuthorizationService(policies, new FixedOutstanding(0m));
        var result = await service.AuthorizeOnlineUtangAsync(
            PosOrganizationId.From(Seller),
            customerId,
            requestedCreditAmount: 100m,
            activeOnlineUtangCommitment: 0m,
            businessDate: DateOnly.FromDateTime(Utc.UtcDateTime));
        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.CustomerOrderOnlineUtangUnavailable, result.ErrorCode);
    }

    [Fact]
    public async Task Online_utang_allowed_within_available_after_outstanding_and_pending()
    {
        var customerId = POSCustomerId.New();
        var policies = new InMemoryCreditPolicies();
        var (policy, _) = CustomerCreditPolicy.Configure(
            PosOrganizationId.From(Seller),
            customerId,
            5000m,
            30,
            Actor,
            reason: null,
            Utc);
        policy.Approve(Actor, "ok", Utc);
        await policies.AddAsync(policy);

        var service = new CustomerCreditAuthorizationService(policies, new FixedOutstanding(2000m));
        var ok = await service.AuthorizeOnlineUtangAsync(
            PosOrganizationId.From(Seller),
            customerId,
            requestedCreditAmount: 2000m,
            activeOnlineUtangCommitment: 1000m,
            businessDate: DateOnly.FromDateTime(Utc.UtcDateTime));
        Assert.True(ok.IsSuccess, ok.ErrorMessage);
        Assert.Equal(2000m, ok.Value!.AvailableCredit);

        var over = await service.AuthorizeOnlineUtangAsync(
            PosOrganizationId.From(Seller),
            customerId,
            requestedCreditAmount: 2500m,
            activeOnlineUtangCommitment: 1000m,
            businessDate: DateOnly.FromDateTime(Utc.UtcDateTime));
        Assert.False(over.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.CustomerOrderOnlineUtangLimitExceeded, over.ErrorCode);
    }

    [Fact]
    public void Access_rules_are_pure_and_master_off_always_denies()
    {
        Assert.False(CustomerOnlineOrderingAccessRules.IsShoppingAllowed(false, CustomerOnlineOrderingAccess.Allowed));
        Assert.True(CustomerOnlineOrderingAccessRules.IsShoppingAllowed(true, CustomerOnlineOrderingAccess.Default));
        Assert.True(CustomerOnlineOrderingAccessRules.IsShoppingAllowed(true, CustomerOnlineOrderingAccess.Allowed));
        Assert.False(CustomerOnlineOrderingAccessRules.IsShoppingAllowed(true, CustomerOnlineOrderingAccess.Blocked));
    }

    [Fact]
    public void Pos_customer_set_online_ordering_access_records_actor_metadata()
    {
        var customer = POSCustomer.Create(PosOrganizationId.From(Seller), "Ana", Utc);
        customer.SetOnlineOrderingAccess(CustomerOnlineOrderingAccess.Blocked, Actor, Utc.AddMinutes(1));
        Assert.Equal(CustomerOnlineOrderingAccess.Blocked, customer.OnlineOrderingAccess);
        Assert.Equal(Actor, customer.OnlineOrderingAccessUpdatedByUserId);
        Assert.Equal(Utc.AddMinutes(1), customer.OnlineOrderingAccessUpdatedAtUtc);
    }

    private static PersonalOnlineCommerceAuthorization CreateShoppingAuth(
        bool canOrder,
        CustomerOnlineOrderingAccess access)
    {
        var customers = new InMemoryCustomers();
        var pos = POSCustomer.Create(
            PosOrganizationId.From(Seller),
            "Ana",
            Utc,
            platformBusinessCustomerId: BusinessCustomer);
        pos.SetOnlineOrderingAccess(access, Actor, Utc);
        customers.AddAsync(pos).GetAwaiter().GetResult();
        return new PersonalOnlineCommerceAuthorization(
            new FixedCapability(canOrder),
            new FixedLink(authorized: true),
            customers);
    }

    private sealed class FixedCapability(bool canOrder) : ISellerCustomerOrderingCapability
    {
        public Task<SellerCustomerOrderingCapability> ResolveAsync(
            Guid sellerOrganizationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new SellerCustomerOrderingCapability(sellerOrganizationId, canOrder, canOrder));
    }

    private sealed class FixedLink(bool authorized) : ILinkedCustomerPlatformAuthorization
    {
        public Task<LinkedCustomerPlatformAuthorizationResult> VerifyAsync(
            Guid organizationId,
            Guid platformBusinessCustomerId,
            CancellationToken cancellationToken = default)
        {
            if (!authorized
                || organizationId != Seller
                || platformBusinessCustomerId != BusinessCustomer)
            {
                return Task.FromResult(new LinkedCustomerPlatformAuthorizationResult(
                    LinkedCustomerPlatformAuthorizationOutcome.NotFound,
                    null));
            }

            return Task.FromResult(new LinkedCustomerPlatformAuthorizationResult(
                LinkedCustomerPlatformAuthorizationOutcome.Authorized,
                new LinkedCustomerPlatformAuthorizationProof(
                    Actor,
                    Seller,
                    BusinessCustomer,
                    LinkedId)));
        }
    }

    private sealed class FixedOutstanding(decimal amount) : IOutstandingBalanceService
    {
        public Task<decimal> GetOutstandingAsync(
            PosOrganizationId organizationId,
            POSCustomerId customerId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(amount);

        public Task<IReadOnlyDictionary<Guid, decimal>> GetOutstandingBatchAsync(
            PosOrganizationId organizationId,
            IReadOnlyCollection<Guid> customerIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<Guid, decimal>>(
                customerIds.ToDictionary(id => id, _ => amount));

        public Task<CustomerUtangSummaryDto> GetSummaryAsync(
            Guid organizationId,
            Guid customerId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new CustomerUtangSummaryDto(
                customerId,
                organizationId,
                amount,
                amount,
                0m,
                0m,
                0,
                0,
                0));
    }

    private sealed class InMemoryCreditPolicies : ICustomerCreditPolicyRepository
    {
        private readonly List<CustomerCreditPolicy> _policies = [];

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
                _policies.Where(p => p.OrganizationId == organizationId && customerIds.Contains(p.CustomerId.Value)).ToList());

        public Task AddAsync(CustomerCreditPolicy policy, CancellationToken cancellationToken = default)
        {
            _policies.Add(policy);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(CustomerCreditPolicy policy, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task AddChangeAsync(CustomerCreditPolicyChange change, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<(IReadOnlyList<CustomerCreditPolicyChange> Items, int TotalCount)> ListChangesAsync(
            PosOrganizationId organizationId,
            POSCustomerId customerId,
            int skip,
            int take,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<(IReadOnlyList<CustomerCreditPolicyChange>, int)>(([], 0));

        public Task AcquireCustomerCreditLockAsync(
            PosOrganizationId organizationId,
            POSCustomerId customerId,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class InMemoryCustomers : IPOSCustomerRepository
    {
        private readonly List<POSCustomer> _items = [];

        public Task AddAsync(POSCustomer customer, CancellationToken cancellationToken = default)
        {
            _items.Add(customer);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(POSCustomer customer, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<POSCustomer?> GetByIdAsync(
            PosOrganizationId organizationId,
            POSCustomerId customerId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(c => c.OrganizationId == organizationId && c.Id == customerId));

        public Task<POSCustomer?> FindByPlatformBusinessCustomerIdAsync(
            PosOrganizationId organizationId,
            Guid platformBusinessCustomerId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(c =>
                c.OrganizationId == organizationId
                && c.PlatformBusinessCustomerId == platformBusinessCustomerId));

        public Task<POSCustomer?> FindActiveByNormalizedMobileAsync(
            PosOrganizationId organizationId,
            string normalizedMobile,
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
            Task.FromResult(_items.Count(c =>
                c.OrganizationId == organizationId
                && c.PlatformBusinessCustomerId == platformBusinessCustomerId));

        public Task<(IReadOnlyList<POSCustomer> Items, int TotalCount)> ListAsync(
            PosOrganizationId organizationId,
            CustomerStatus? status,
            string? search,
            int skip,
            int take,
            IReadOnlyCollection<Guid>? restrictToCustomerIds = null,
            bool peopleOnly = false,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<(IReadOnlyList<POSCustomer>, int)>(([], 0));

        public Task<(IReadOnlyList<POSCustomer> Items, int TotalCount)> ListUpdatedSinceAsync(
            PosOrganizationId organizationId,
            DateTimeOffset? sinceUtc,
            int skip,
            int take,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<(IReadOnlyList<POSCustomer>, int)>(([], 0));

        public Task<IReadOnlyList<POSCustomer>> ListByIdsAsync(
            PosOrganizationId organizationId,
            IReadOnlyCollection<POSCustomerId> customerIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<POSCustomer>>(
                _items.Where(c => c.OrganizationId == organizationId && customerIds.Contains(c.Id)).ToList());
    }
}
