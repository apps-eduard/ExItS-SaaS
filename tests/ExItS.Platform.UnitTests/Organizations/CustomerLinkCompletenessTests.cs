using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Application.Personal;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Personal;
using ExItS.Platform.UnitTests.Support;

namespace ExItS.Platform.UnitTests.Organizations;

public sealed class CustomerLinkCompletenessTests
{
    private static readonly DateTimeOffset T0 = new(2026, 8, 12, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Personal_user_accepts_valid_link_without_membership_or_roles()
    {
        var harness = await Harness.CreateAsync();
        var accepted = await harness.Accept.ExecuteAsync(
            harness.AcceptToken,
            harness.Personal.Id,
            AccountClass.Personal);

        Assert.True(accepted.IsSuccess, accepted.ErrorMessage);
        Assert.False(accepted.Value!.CreatedOrganizationMembership);
        Assert.False(accepted.Value.GrantedStaffRole);
        Assert.False(accepted.Value.GrantedProductRole);
        Assert.Equal(0, harness.Memberships.AddCount);

        var listed = await harness.List.ExecuteAsync(harness.Personal.Id, page: 1, pageSize: 20);
        Assert.Equal(1, listed.TotalCount);
        Assert.Equal(harness.Customer.Id.Value, listed.Items[0].BusinessCustomerId);
        Assert.Equal("Corner Store", listed.Items[0].OrganizationDisplayName);
        Assert.Equal("Store Customer", listed.Items[0].CustomerDisplayName);
        Assert.Equal(nameof(LinkedCustomerAppUserStatus.Active), listed.Items[0].LinkStatus);
    }

    [Fact]
    public async Task Organization_session_cannot_accept_as_personal()
    {
        var harness = await Harness.CreateAsync();
        var result = await harness.Accept.ExecuteAsync(
            harness.AcceptToken,
            harness.Personal.Id,
            AccountClass.Organization);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.AccountScopeDenied, result.ErrorCode);
    }

    [Fact]
    public async Task Platform_session_cannot_accept_as_personal()
    {
        var harness = await Harness.CreateAsync();
        var result = await harness.Accept.ExecuteAsync(
            harness.AcceptToken,
            harness.Personal.Id,
            AccountClass.Platform);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.AccountScopeDenied, result.ErrorCode);
    }

    [Fact]
    public async Task Organization_staff_identity_cannot_accept()
    {
        var harness = await Harness.CreateAsync();
        var staff = PlatformUser.CreateOrganizationStaff(
            "maria_org001842",
            "maria@ORG001842",
            harness.Personal.NormalizedEmail,
            harness.Org.Id,
            "Maria Staff",
            T0);
        await harness.Users.AddAsync(staff);

        var result = await harness.Accept.ExecuteAsync(
            harness.AcceptToken,
            staff.Id,
            AccountClass.Personal);

        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrorCodes.CustomerLinkPersonalIdentityRequired, result.ErrorCode);
        Assert.Equal(0, harness.Memberships.AddCount);
    }

    [Fact]
    public async Task Platform_admin_identity_cannot_accept()
    {
        var harness = await Harness.CreateAsync();
        var admin = PlatformUser.CreatePlatformStaff(
            "olivia.staff",
            "Olivia",
            "Staff",
            "Olivia Staff",
            harness.Personal.NormalizedEmail,
            "STF-000001",
            T0);
        await harness.Users.AddAsync(admin);

        var result = await harness.Accept.ExecuteAsync(
            harness.AcceptToken,
            admin.Id,
            AccountClass.Personal);

        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrorCodes.CustomerLinkPersonalIdentityRequired, result.ErrorCode);
    }

    [Fact]
    public async Task Unrelated_personal_user_is_denied()
    {
        var harness = await Harness.CreateAsync();
        var other = PlatformUser.Create("otheruser", "Other User", "other@example.com", T0);
        await harness.Users.AddAsync(other);

        var result = await harness.Accept.ExecuteAsync(
            harness.AcceptToken,
            other.Id,
            AccountClass.Personal);

        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrorCodes.CustomerLinkRequestEmailMismatch, result.ErrorCode);
    }

    [Fact]
    public async Task Inactive_personal_user_is_denied()
    {
        var harness = await Harness.CreateAsync();
        harness.Personal.Suspend(T0.AddMinutes(1), "test");
        await harness.Users.UpdateAsync(harness.Personal);

        var result = await harness.Accept.ExecuteAsync(
            harness.AcceptToken,
            harness.Personal.Id,
            AccountClass.Personal);

        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrorCodes.UserNotActive, result.ErrorCode);
    }

    [Fact]
    public async Task Linked_merchant_list_does_not_leak_across_users()
    {
        var harness = await Harness.CreateAsync();
        Assert.True((await harness.Accept.ExecuteAsync(
            harness.AcceptToken,
            harness.Personal.Id,
            AccountClass.Personal)).IsSuccess);

        var other = PlatformUser.Create("otheruser", "Other User", "other@example.com", T0);
        await harness.Users.AddAsync(other);

        var own = await harness.List.ExecuteAsync(harness.Personal.Id, 1, 20);
        var leaked = await harness.List.ExecuteAsync(other.Id, 1, 20);
        Assert.Equal(1, own.TotalCount);
        Assert.Equal(0, leaked.TotalCount);
        Assert.Empty(leaked.Items);
    }

    [Fact]
    public async Task Unlink_by_owner_stops_future_list_access_and_is_soft()
    {
        var harness = await Harness.CreateAsync();
        var accepted = await harness.Accept.ExecuteAsync(
            harness.AcceptToken,
            harness.Personal.Id,
            AccountClass.Personal);
        Assert.True(accepted.IsSuccess, accepted.ErrorMessage);

        var unlink = await harness.Unlink.ExecuteForOwnerAsync(
            LinkedCustomerAppUserId.From(accepted.Value!.LinkedCustomerAppUserId),
            harness.Personal.Id);
        Assert.True(unlink.IsSuccess, unlink.ErrorMessage);
        Assert.Equal(nameof(LinkedCustomerAppUserStatus.Revoked), unlink.Value!.Status);

        var listed = await harness.List.ExecuteAsync(harness.Personal.Id, 1, 20);
        Assert.Equal(0, listed.TotalCount);
        Assert.Empty(listed.Items);

        var customer = await harness.Customers.GetByIdAsync(harness.Customer.Id);
        Assert.Null(customer!.LinkedUserIdentityId);

        var again = await harness.Unlink.ExecuteForOwnerAsync(
            LinkedCustomerAppUserId.From(accepted.Value.LinkedCustomerAppUserId),
            harness.Personal.Id);
        Assert.True(again.IsSuccess);
    }

    [Fact]
    public async Task Unlink_guessing_and_cross_user_access_fail_closed()
    {
        var harness = await Harness.CreateAsync();
        var accepted = await harness.Accept.ExecuteAsync(
            harness.AcceptToken,
            harness.Personal.Id,
            AccountClass.Personal);
        Assert.True(accepted.IsSuccess);

        var other = PlatformUser.Create("otheruser", "Other User", "other@example.com", T0);
        await harness.Users.AddAsync(other);

        var guessed = await harness.Unlink.ExecuteForOwnerAsync(LinkedCustomerAppUserId.New(), other.Id);
        Assert.False(guessed.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.LinkedCustomerAppUserNotFound, guessed.ErrorCode);

        var stolen = await harness.Unlink.ExecuteForOwnerAsync(
            LinkedCustomerAppUserId.From(accepted.Value!.LinkedCustomerAppUserId),
            other.Id);
        Assert.False(stolen.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.LinkedCustomerAppUserNotFound, stolen.ErrorCode);
    }

    [Fact]
    public async Task Pending_invitation_revoke_still_blocks_accept()
    {
        var harness = await Harness.CreateAsync();
        var revoked = await harness.RevokePending.ExecuteAsync(
            harness.Request.Id,
            harness.Org.Id);
        Assert.True(revoked.IsSuccess, revoked.ErrorMessage);
        Assert.Equal(nameof(CustomerLinkRequestStatus.Revoked), revoked.Value!.Status);

        var accepted = await harness.Accept.ExecuteAsync(
            harness.AcceptToken,
            harness.Personal.Id,
            AccountClass.Personal);
        Assert.False(accepted.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.CustomerLinkRequestNotFound, accepted.ErrorCode);
    }

    [Fact]
    public async Task Relink_after_unlink_requires_a_new_invitation()
    {
        var harness = await Harness.CreateAsync();
        var accepted = await harness.Accept.ExecuteAsync(
            harness.AcceptToken,
            harness.Personal.Id,
            AccountClass.Personal);
        Assert.True(accepted.IsSuccess);

        Assert.True((await harness.Unlink.ExecuteForOwnerAsync(
            LinkedCustomerAppUserId.From(accepted.Value!.LinkedCustomerAppUserId),
            harness.Personal.Id)).IsSuccess);

        var reuse = await harness.Accept.ExecuteAsync(
            harness.AcceptToken,
            harness.Personal.Id,
            AccountClass.Personal);
        Assert.False(reuse.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.CustomerLinkRequestNotFound, reuse.ErrorCode);

        var created = await harness.CreateRequest.ExecuteAsync(
            harness.Org.Id,
            harness.Customer.Id,
            harness.Personal.NormalizedEmail,
            invitedByUserId: null);
        Assert.True(created.IsSuccess, created.ErrorMessage);
        var relinked = await harness.Accept.ExecuteAsync(
            created.Value!.AcceptToken!,
            harness.Personal.Id,
            AccountClass.Personal);
        Assert.True(relinked.IsSuccess, relinked.ErrorMessage);
        Assert.NotEqual(accepted.Value.LinkedCustomerAppUserId, relinked.Value!.LinkedCustomerAppUserId);
    }

    [Fact]
    public async Task Ordering_capability_requires_active_personal_link()
    {
        var harness = await Harness.CreateAsync();
        var unlinked = await harness.OrderingCapability.ExecuteAsync(
            harness.Personal.Id,
            harness.Org.Id.Value);
        Assert.False(unlinked.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.LinkedCustomerAppUserNotFound, unlinked.ErrorCode);

        var accepted = await harness.Accept.ExecuteAsync(
            harness.AcceptToken,
            harness.Personal.Id,
            AccountClass.Personal);
        Assert.True(accepted.IsSuccess, accepted.ErrorMessage);

        var linked = await harness.OrderingCapability.ExecuteAsync(
            harness.Personal.Id,
            harness.Org.Id.Value);
        Assert.True(linked.IsSuccess, linked.ErrorMessage);
        Assert.Equal(harness.Org.Id.Value, linked.Value!.OrganizationId);
        Assert.Equal("Corner Store", linked.Value.OrganizationDisplayName);

        Assert.True((await harness.Unlink.ExecuteForOwnerAsync(
            LinkedCustomerAppUserId.From(accepted.Value!.LinkedCustomerAppUserId),
            harness.Personal.Id)).IsSuccess);

        var revoked = await harness.OrderingCapability.ExecuteAsync(
            harness.Personal.Id,
            harness.Org.Id.Value);
        Assert.False(revoked.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.LinkedCustomerAppUserNotFound, revoked.ErrorCode);
    }

    internal sealed class Harness
    {
        private Harness(
            PlatformOrganization org,
            PlatformUser personal,
            BusinessCustomer customer,
            CustomerLinkRequest request,
            string acceptToken,
            InMemoryPlatformUserRepository users,
            InMemoryOrganizationMembershipRepository memberships,
            InMemoryBusinessCustomerRepository customers,
            InMemoryLinkedCustomerAppUserRepository links,
            InMemoryCustomerLinkRequestRepository requests,
            InMemoryPlatformOrganizationRepository orgs,
            AcceptCustomerLinkRequest accept,
            UnlinkAcceptedCustomerLink unlink,
            ListLinkedMerchantsForPersonalUser list,
            RevokeCustomerLinkRequest revokePending,
            CreateCustomerLinkRequest createRequest,
            DeclineCustomerLinkRequest decline,
            AuthorizeLinkedCustomerAccess authorize,
            GetLinkedMerchantOrderingCapability orderingCapability)
        {
            Org = org;
            Personal = personal;
            Customer = customer;
            Request = request;
            AcceptToken = acceptToken;
            Users = users;
            Memberships = memberships;
            Customers = customers;
            Links = links;
            Requests = requests;
            Orgs = orgs;
            Accept = accept;
            Unlink = unlink;
            List = list;
            RevokePending = revokePending;
            CreateRequest = createRequest;
            Decline = decline;
            Authorize = authorize;
            OrderingCapability = orderingCapability;
        }

        public PlatformOrganization Org { get; }
        public PlatformUser Personal { get; }
        public BusinessCustomer Customer { get; }
        public CustomerLinkRequest Request { get; }
        public string AcceptToken { get; }
        public InMemoryPlatformUserRepository Users { get; }
        public InMemoryOrganizationMembershipRepository Memberships { get; }
        public InMemoryBusinessCustomerRepository Customers { get; }
        public InMemoryLinkedCustomerAppUserRepository Links { get; }
        public InMemoryCustomerLinkRequestRepository Requests { get; }
        public InMemoryPlatformOrganizationRepository Orgs { get; }
        public AcceptCustomerLinkRequest Accept { get; }
        public UnlinkAcceptedCustomerLink Unlink { get; }
        public ListLinkedMerchantsForPersonalUser List { get; }
        public RevokeCustomerLinkRequest RevokePending { get; }
        public CreateCustomerLinkRequest CreateRequest { get; }
        public DeclineCustomerLinkRequest Decline { get; }
        public AuthorizeLinkedCustomerAccess Authorize { get; }
        public GetLinkedMerchantOrderingCapability OrderingCapability { get; }

        public static async Task<Harness> CreateAsync()
        {
            var clock = new FixedClock(T0);
            var uow = new NoOpUnitOfWork();
            var users = new InMemoryPlatformUserRepository();
            var memberships = new InMemoryOrganizationMembershipRepository();
            var orgs = new InMemoryPlatformOrganizationRepository();
            var customers = new InMemoryBusinessCustomerRepository();
            var requests = new InMemoryCustomerLinkRequestRepository(clock);
            var links = new InMemoryLinkedCustomerAppUserRepository();
            var personalSettings = new InMemoryPersonalAccountSettingsRepository();
            var personalNotifications = new InMemoryPersonalInAppNotificationRepository();
            var orgNotifications = new InMemoryOrganizationInAppNotificationRepository();
            var entitlements = new InMemoryEntitlementSnapshotRepository();

            var org = PlatformOrganization.Create("Corner Store", "corner-store", T0);
            await orgs.AddAsync(org);
            var personal = PlatformUser.Create("rosa.personal", "Rosa Personal", "rosa@example.com", T0);
            await users.AddAsync(personal);
            var customer = BusinessCustomer.Create(org.Id, "Store Customer", T0, email: "rosa@example.com");
            await customers.AddAsync(customer);
            var (request, token) = CustomerLinkRequest.Create(org.Id, customer.Id, "rosa@example.com", T0);
            await requests.AddAsync(request);

            return new Harness(
                org,
                personal,
                customer,
                request,
                token,
                users,
                memberships,
                customers,
                links,
                requests,
                orgs,
                new AcceptCustomerLinkRequest(requests, customers, links, memberships, users, uow, clock, orgNotifications),
                new UnlinkAcceptedCustomerLink(links, customers, uow, clock),
                new ListLinkedMerchantsForPersonalUser(links, customers, orgs, entitlements),
                new RevokeCustomerLinkRequest(requests, uow, clock),
                new CreateCustomerLinkRequest(
                    customers,
                    requests,
                    uow,
                    clock,
                    users,
                    orgs,
                    personalSettings,
                    personalNotifications,
                    blocks: null,
                    links: links,
                    eligibility: new EvaluateCustomerLinkEligibility(
                        users, memberships, requests, links, clock)),
                new DeclineCustomerLinkRequest(requests, uow, clock, orgNotifications, users),
                new AuthorizeLinkedCustomerAccess(users, links, customers),
                new GetLinkedMerchantOrderingCapability(links, entitlements, orgs));
        }
    }

    internal sealed class InMemoryBusinessCustomerRepository : IBusinessCustomerRepository
    {
        private readonly List<BusinessCustomer> _items = [];

        public Task<BusinessCustomer?> GetByIdAsync(BusinessCustomerId id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(c => c.Id == id));

        public Task<(IReadOnlyList<BusinessCustomer> Items, int TotalCount)> ListByOrganizationAsync(
            PlatformOrganizationId organizationId,
            string? owningProductCode,
            int skip,
            int take,
            CancellationToken cancellationToken = default)
        {
            var query = _items.Where(c => c.OrganizationId == organizationId);
            if (!string.IsNullOrWhiteSpace(owningProductCode))
            {
                query = query.Where(c => c.OwningProductCode == owningProductCode);
            }

            var list = query.ToList();
            return Task.FromResult(((IReadOnlyList<BusinessCustomer>)list.Skip(skip).Take(take).ToList(), list.Count));
        }

        public Task<IReadOnlyList<BusinessCustomer>> ListByIdsAsync(
            IReadOnlyCollection<BusinessCustomerId> ids,
            CancellationToken cancellationToken = default)
        {
            var values = ids.Select(i => i.Value).ToHashSet();
            return Task.FromResult<IReadOnlyList<BusinessCustomer>>(
                _items.Where(c => values.Contains(c.Id.Value)).ToList());
        }

        public Task AddAsync(BusinessCustomer customer, CancellationToken cancellationToken = default)
        {
            _items.Add(customer);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(BusinessCustomer customer, CancellationToken cancellationToken = default)
        {
            var index = _items.FindIndex(c => c.Id == customer.Id);
            if (index >= 0)
            {
                _items[index] = customer;
            }

            return Task.CompletedTask;
        }
    }

    internal sealed class InMemoryCustomerLinkRequestRepository : ICustomerLinkRequestRepository
    {
        private readonly List<CustomerLinkRequest> _items = [];
        private readonly IClock? _clock;

        public InMemoryCustomerLinkRequestRepository(IClock? clock = null) => _clock = clock;

        public Task<CustomerLinkRequest?> GetByIdAsync(
            CustomerLinkRequestId id,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(r => r.Id == id));

        public Task<CustomerLinkRequest?> FindPendingByTokenHashAsync(
            string tokenHash,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(r =>
                r.Status == CustomerLinkRequestStatus.Pending && r.TokenHash == tokenHash));

        public Task<CustomerLinkRequest?> FindPendingByBusinessCustomerAsync(
            BusinessCustomerId businessCustomerId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(r =>
                r.BusinessCustomerId == businessCustomerId && r.Status == CustomerLinkRequestStatus.Pending));

        public Task<CustomerLinkRequest?> FindPendingByOrganizationAndTargetUserAsync(
            PlatformOrganizationId organizationId,
            PlatformUserId targetUserIdentityId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(r =>
                r.OrganizationId == organizationId
                && r.TargetUserIdentityId == targetUserIdentityId
                && r.Status == CustomerLinkRequestStatus.Pending));

        public Task<(IReadOnlyList<CustomerLinkRequest> Items, int TotalCount)> ListByOrganizationAsync(
            PlatformOrganizationId organizationId,
            CustomerLinkRequestStatus? status,
            int skip,
            int take,
            CancellationToken cancellationToken = default)
        {
            var query = _items.Where(r => r.OrganizationId == organizationId);
            if (status is not null)
            {
                query = query.Where(r => r.Status == status);
            }

            var list = query.ToList();
            return Task.FromResult(((IReadOnlyList<CustomerLinkRequest>)list.Skip(skip).Take(take).ToList(), list.Count));
        }

        public Task<IReadOnlyList<CustomerLinkRequest>> ListPendingForTargetUserAsync(
            PlatformUserId targetUserIdentityId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CustomerLinkRequest>>(
                _items.Where(r =>
                        r.Status == CustomerLinkRequestStatus.Pending
                        && r.TargetUserIdentityId == targetUserIdentityId)
                    .ToList());

        public Task<IReadOnlyList<CustomerLinkRequest>> ListResolvedForTargetUserAsync(
            PlatformUserId targetUserIdentityId,
            int take,
            CancellationToken cancellationToken = default)
        {
            var limit = Math.Clamp(take, 1, 100);
            return Task.FromResult<IReadOnlyList<CustomerLinkRequest>>(
                _items
                    .Where(r =>
                        r.TargetUserIdentityId == targetUserIdentityId
                        && r.Status != CustomerLinkRequestStatus.Pending)
                    .OrderByDescending(r => r.CreatedAtUtc)
                    .Take(limit)
                    .ToList());
        }

        public Task<IReadOnlyList<CustomerLinkRequest>> ListByBusinessCustomerAsync(
            BusinessCustomerId businessCustomerId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CustomerLinkRequest>>(
                _items.Where(r => r.BusinessCustomerId == businessCustomerId)
                    .OrderByDescending(r => r.CreatedAtUtc)
                    .ToList());

        public Task<IReadOnlyDictionary<string, int>> CountByOrganizationGroupedAsync(
            PlatformOrganizationId organizationId,
            CancellationToken cancellationToken = default)
        {
            var now = _clock?.UtcNow ?? DateTimeOffset.UtcNow;
            var counts = _items
                .Where(r => r.OrganizationId == organizationId)
                .GroupBy(r =>
                    r.Status == CustomerLinkRequestStatus.Pending && r.IsExpired(now)
                        ? nameof(CustomerLinkRequestStatus.Expired)
                        : r.Status.ToString())
                .ToDictionary(g => g.Key, g => g.Count());
            return Task.FromResult((IReadOnlyDictionary<string, int>)counts);
        }

        public Task AddAsync(CustomerLinkRequest request, CancellationToken cancellationToken = default)
        {
            if (request.Status == CustomerLinkRequestStatus.Pending
                && request.TargetUserIdentityId is not null
                && _items.Any(r =>
                    r.OrganizationId == request.OrganizationId
                    && r.TargetUserIdentityId == request.TargetUserIdentityId
                    && r.Status == CustomerLinkRequestStatus.Pending
                    && r.Id != request.Id))
            {
                throw new PersistenceConflictException(
                    ApplicationErrorCodes.CustomerLinkPendingExists,
                    "An invitation has already been sent to this person.");
            }

            _items.Add(request);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(CustomerLinkRequest request, CancellationToken cancellationToken = default)
        {
            var index = _items.FindIndex(r => r.Id == request.Id);
            if (index >= 0)
            {
                _items[index] = request;
            }

            return Task.CompletedTask;
        }
    }

    internal sealed class InMemoryLinkedCustomerAppUserRepository : ILinkedCustomerAppUserRepository
    {
        private readonly List<LinkedCustomerAppUser> _items = [];

        public Task<LinkedCustomerAppUser?> GetByIdAsync(
            LinkedCustomerAppUserId id,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(l => l.Id == id));

        public Task<LinkedCustomerAppUser?> FindActiveByBusinessCustomerAsync(
            BusinessCustomerId businessCustomerId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(l =>
                l.BusinessCustomerId == businessCustomerId && l.Status == LinkedCustomerAppUserStatus.Active));

        public Task<LinkedCustomerAppUser?> FindActiveByUserAndOrganizationAsync(
            PlatformUserId userIdentityId,
            PlatformOrganizationId organizationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(l =>
                l.UserIdentityId == userIdentityId
                && l.OrganizationId == organizationId
                && l.Status == LinkedCustomerAppUserStatus.Active));

        public Task<LinkedCustomerAppUser?> FindActiveByUserOrganizationAndBusinessCustomerAsync(
            PlatformUserId userIdentityId,
            PlatformOrganizationId organizationId,
            BusinessCustomerId businessCustomerId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(l =>
                l.UserIdentityId == userIdentityId
                && l.OrganizationId == organizationId
                && l.BusinessCustomerId == businessCustomerId
                && l.Status == LinkedCustomerAppUserStatus.Active));

        public Task<(IReadOnlyList<LinkedCustomerAppUser> Items, int TotalCount)> ListActiveByUserAsync(
            PlatformUserId userIdentityId,
            int skip,
            int take,
            CancellationToken cancellationToken = default)
        {
            var list = _items
                .Where(l => l.UserIdentityId == userIdentityId && l.Status == LinkedCustomerAppUserStatus.Active)
                .OrderByDescending(l => l.LinkedAtUtc)
                .ThenBy(l => l.Id.Value)
                .ToList();
            return Task.FromResult(((IReadOnlyList<LinkedCustomerAppUser>)list.Skip(skip).Take(take).ToList(), list.Count));
        }

        public Task<(IReadOnlyList<LinkedCustomerAppUser> Items, int TotalCount)> ListByOrganizationAsync(
            PlatformOrganizationId organizationId,
            int skip,
            int take,
            CancellationToken cancellationToken = default)
        {
            var list = _items.Where(l => l.OrganizationId == organizationId).ToList();
            return Task.FromResult(((IReadOnlyList<LinkedCustomerAppUser>)list.Skip(skip).Take(take).ToList(), list.Count));
        }

        public Task<IReadOnlyList<LinkedCustomerAppUser>> ListActiveByUserAndOrganizationAsync(
            PlatformUserId userIdentityId,
            PlatformOrganizationId organizationId,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyList<LinkedCustomerAppUser> list = _items
                .Where(l =>
                    l.UserIdentityId == userIdentityId
                    && l.OrganizationId == organizationId
                    && l.Status == LinkedCustomerAppUserStatus.Active)
                .OrderByDescending(l => l.LinkedAtUtc)
                .ToList();
            return Task.FromResult(list);
        }

        public Task AddAsync(LinkedCustomerAppUser link, CancellationToken cancellationToken = default)
        {
            _items.Add(link);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(LinkedCustomerAppUser link, CancellationToken cancellationToken = default)
        {
            var index = _items.FindIndex(l => l.Id == link.Id);
            if (index >= 0)
            {
                _items[index] = link;
            }

            return Task.CompletedTask;
        }
    }

    internal sealed class InMemoryPersonalAccountSettingsRepository : IPersonalAccountSettingsRepository
    {
        private readonly Dictionary<Guid, PersonalAccountSettings> _byUser = new();

        public Task<PersonalAccountSettings?> GetByUserAsync(
            PlatformUserId userIdentityId,
            CancellationToken cancellationToken = default)
        {
            _byUser.TryGetValue(userIdentityId.Value, out var settings);
            return Task.FromResult(settings);
        }

        public Task AddAsync(PersonalAccountSettings settings, CancellationToken cancellationToken = default)
        {
            _byUser[settings.UserIdentityId.Value] = settings;
            return Task.CompletedTask;
        }

        public Task UpdateAsync(PersonalAccountSettings settings, CancellationToken cancellationToken = default)
        {
            _byUser[settings.UserIdentityId.Value] = settings;
            return Task.CompletedTask;
        }
    }

    internal sealed class InMemoryPersonalInAppNotificationRepository : IPersonalInAppNotificationRepository
    {
        private readonly List<PersonalInAppNotification> _items = [];

        public IReadOnlyList<PersonalInAppNotification> Items => _items;

        public Task<PersonalInAppNotification?> GetByIdAsync(
            PersonalInAppNotificationId id,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(n => n.Id == id));

        public Task<IReadOnlyList<PersonalInAppNotification>> ListForUserAsync(
            PlatformUserId recipientUserIdentityId,
            int take,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<PersonalInAppNotification>>(
                _items
                    .Where(n => n.RecipientUserIdentityId == recipientUserIdentityId)
                    .OrderByDescending(n => n.CreatedAtUtc)
                    .Take(take)
                    .ToList());

        public Task<(IReadOnlyList<PersonalInAppNotification> Items, int TotalCount)> ListForUserPagedAsync(
            PlatformUserId recipientUserIdentityId,
            DateTimeOffset? createdOnOrAfterUtc,
            DateTimeOffset? createdBeforeUtc,
            bool unreadOnly,
            int skip,
            int take,
            CancellationToken cancellationToken = default)
        {
            IEnumerable<PersonalInAppNotification> query = _items
                .Where(n => n.RecipientUserIdentityId == recipientUserIdentityId);
            if (createdOnOrAfterUtc is not null)
            {
                query = query.Where(n => n.CreatedAtUtc >= createdOnOrAfterUtc.Value);
            }

            if (createdBeforeUtc is not null)
            {
                query = query.Where(n => n.CreatedAtUtc < createdBeforeUtc.Value);
            }

            if (unreadOnly)
            {
                query = query.Where(n => !n.IsRead);
            }

            var filtered = query
                .OrderByDescending(n => n.CreatedAtUtc)
                .ThenByDescending(n => n.Id.Value)
                .ToList();
            var page = filtered.Skip(skip).Take(take).ToList();
            return Task.FromResult<(IReadOnlyList<PersonalInAppNotification>, int)>((page, filtered.Count));
        }

        public Task<int> CountUnreadForUserAsync(
            PlatformUserId recipientUserIdentityId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.Count(n =>
                n.RecipientUserIdentityId == recipientUserIdentityId && !n.IsRead));

        public Task<PersonalInAppNotification?> FindByRecipientRelatedAsync(
            PlatformUserId recipientUserIdentityId,
            string relatedType,
            string relatedId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(n =>
                n.RecipientUserIdentityId == recipientUserIdentityId
                && string.Equals(n.RelatedType, relatedType, StringComparison.Ordinal)
                && string.Equals(n.RelatedId, relatedId, StringComparison.Ordinal)));

        public Task AddAsync(PersonalInAppNotification notification, CancellationToken cancellationToken = default)
        {
            _items.Add(notification);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(PersonalInAppNotification notification, CancellationToken cancellationToken = default)
        {
            var index = _items.FindIndex(n => n.Id == notification.Id);
            if (index >= 0)
            {
                _items[index] = notification;
            }

            return Task.CompletedTask;
        }
    }

    internal sealed class InMemoryOrganizationInAppNotificationRepository : IOrganizationInAppNotificationRepository
    {
        private readonly List<OrganizationInAppNotification> _items = [];

        public IReadOnlyList<OrganizationInAppNotification> Items => _items;

        public Task<OrganizationInAppNotification?> GetByIdAsync(
            OrganizationInAppNotificationId id,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(n => n.Id == id));

        public Task<IReadOnlyList<OrganizationInAppNotification>> ListForRecipientInOrganizationAsync(
            PlatformOrganizationId organizationId,
            PlatformUserId recipientUserIdentityId,
            int take,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<OrganizationInAppNotification>>(
                _items
                    .Where(n =>
                        n.OrganizationId == organizationId
                        && n.RecipientUserIdentityId == recipientUserIdentityId)
                    .OrderByDescending(n => n.CreatedAtUtc)
                    .Take(take)
                    .ToList());

        public Task<OrganizationInAppNotification?> FindByRecipientRelatedAsync(
            PlatformUserId recipientUserIdentityId,
            string relatedType,
            string relatedId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(n =>
                n.RecipientUserIdentityId == recipientUserIdentityId
                && string.Equals(n.RelatedType, relatedType, StringComparison.Ordinal)
                && string.Equals(n.RelatedId, relatedId, StringComparison.Ordinal)));

        public Task<IReadOnlyList<OrganizationInAppNotification>> ListByOrganizationRelatedAsync(
            PlatformOrganizationId organizationId,
            string relatedType,
            string relatedId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<OrganizationInAppNotification>>(
                _items.Where(n =>
                        n.OrganizationId == organizationId
                        && string.Equals(n.RelatedType, relatedType, StringComparison.Ordinal)
                        && string.Equals(n.RelatedId, relatedId, StringComparison.Ordinal))
                    .ToList());

        public Task AddAsync(OrganizationInAppNotification notification, CancellationToken cancellationToken = default)
        {
            _items.Add(notification);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(OrganizationInAppNotification notification, CancellationToken cancellationToken = default)
        {
            var index = _items.FindIndex(n => n.Id == notification.Id);
            if (index >= 0)
            {
                _items[index] = notification;
            }

            return Task.CompletedTask;
        }
    }

    internal sealed class InMemoryPersonalOrganizationConnectionBlockRepository
        : IPersonalOrganizationConnectionBlockRepository
    {
        private readonly List<PersonalOrganizationConnectionBlock> _items = [];

        public Task<PersonalOrganizationConnectionBlock?> GetByIdAsync(
            PersonalOrganizationConnectionBlockId id,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(b => b.Id == id));

        public Task<PersonalOrganizationConnectionBlock?> FindByPersonalAndOrganizationAsync(
            PlatformUserId personalUserIdentityId,
            PlatformOrganizationId organizationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(b =>
                b.PersonalUserIdentityId == personalUserIdentityId
                && b.OrganizationId == organizationId));

        public Task<PersonalOrganizationConnectionBlock?> FindActiveByPersonalAndOrganizationAsync(
            PlatformUserId personalUserIdentityId,
            PlatformOrganizationId organizationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(b =>
                b.PersonalUserIdentityId == personalUserIdentityId
                && b.OrganizationId == organizationId
                && b.IsActive));

        public Task<IReadOnlyList<PersonalOrganizationConnectionBlock>> ListActiveByPersonalUserAsync(
            PlatformUserId personalUserIdentityId,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyList<PersonalOrganizationConnectionBlock> list = _items
                .Where(b => b.PersonalUserIdentityId == personalUserIdentityId && b.IsActive)
                .OrderByDescending(b => b.BlockedAtUtc)
                .ToList();
            return Task.FromResult(list);
        }

        public Task AddAsync(
            PersonalOrganizationConnectionBlock block,
            CancellationToken cancellationToken = default)
        {
            _items.Add(block);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(
            PersonalOrganizationConnectionBlock block,
            CancellationToken cancellationToken = default)
        {
            var index = _items.FindIndex(b => b.Id == block.Id);
            if (index >= 0)
            {
                _items[index] = block;
            }

            return Task.CompletedTask;
        }
    }
}
