using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Application.Personal;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Personal;
using ExItS.Platform.UnitTests.Support;

namespace ExItS.Platform.UnitTests.Organizations;

public sealed class CustomerLinkConsentFlowTests
{
    private static readonly DateTimeOffset T0 = new(2026, 8, 12, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Create_with_target_personal_user_creates_pending_request_and_personal_notification()
    {
        var harness = await Harness.CreateAsync();

        var created = await harness.CreateRequest.ExecuteAsync(
            harness.Org.Id,
            harness.Customer.Id,
            email: null,
            harness.Inviter.Id,
            harness.Personal.Id,
            harness.Personal.PublicUserId);

        Assert.True(created.IsSuccess, created.ErrorMessage);
        Assert.Equal(nameof(CustomerLinkRequestStatus.Pending), created.Value!.Status);
        Assert.Equal(harness.Personal.Id.Value, created.Value.TargetUserIdentityId);
        Assert.Equal(harness.Personal.PublicUserId, created.Value.TargetPublicUserId);
        Assert.Equal(harness.Inviter.Id.Value, created.Value.InvitedByUserId);

        Assert.Single(harness.PersonalNotifications.Items);
        var notification = harness.PersonalNotifications.Items[0];
        Assert.Equal(harness.Personal.Id, notification.RecipientUserIdentityId);
        Assert.Equal(CustomerLinkNotificationTypes.PersonalPendingRequest, notification.RelatedType);
        Assert.Equal(created.Value.Id.ToString("D"), notification.RelatedId);
        Assert.Contains("Corner Store", notification.Preview, StringComparison.Ordinal);

        var listed = await harness.ListPending.ExecuteAsync(harness.Personal.Id, AccountClass.Personal);
        Assert.True(listed.IsSuccess, listed.ErrorMessage);
        Assert.Single(listed.Value!);
        Assert.Equal("Corner Store", listed.Value![0].OrganizationDisplayName);
        Assert.Equal(created.Value.Id, listed.Value[0].Id);
    }

    [Fact]
    public async Task Idempotent_create_retry_does_not_duplicate_pending_request_or_notification()
    {
        var harness = await Harness.CreateAsync();

        var first = await harness.CreateTargetedPendingAsync();
        Assert.True(first.IsSuccess, first.ErrorMessage);

        var second = await harness.CreateRequest.ExecuteAsync(
            harness.Org.Id,
            harness.Customer.Id,
            email: null,
            harness.Inviter.Id,
            harness.Personal.Id,
            harness.Personal.PublicUserId);

        Assert.True(second.IsSuccess, second.ErrorMessage);
        Assert.Equal(first.Value!.Id, second.Value!.Id);
        Assert.Null(second.Value.AcceptToken);

        var (_, total) = await harness.Requests.ListByOrganizationAsync(harness.Org.Id, null, 0, 50);
        Assert.Equal(1, total);
        Assert.Single(harness.PersonalNotifications.Items);
    }

    [Fact]
    public async Task Wrong_personal_user_cannot_list_or_accept_by_id()
    {
        var harness = await Harness.CreateAsync();
        var created = await harness.CreateTargetedPendingAsync();
        Assert.True(created.IsSuccess, created.ErrorMessage);

        var other = PlatformUser.Create("other.personal", "Other Personal", "other@example.com", T0);
        other.AssignPublicUserId("EX-9999-8888", T0);
        await harness.Users.AddAsync(other);

        var listed = await harness.ListPending.ExecuteAsync(other.Id, AccountClass.Personal);
        Assert.True(listed.IsSuccess, listed.ErrorMessage);
        Assert.Empty(listed.Value!);

        var accepted = await harness.Accept.ExecuteByIdAsync(
            CustomerLinkRequestId.From(created.Value!.Id),
            other.Id,
            AccountClass.Personal);
        Assert.False(accepted.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.CustomerLinkRequestNotFound, accepted.ErrorCode);
        Assert.Equal(0, (await harness.Links.ListByOrganizationAsync(harness.Org.Id, 0, 20)).TotalCount);
    }

    [Fact]
    public async Task Target_accept_by_id_creates_link_without_membership_or_roles()
    {
        var harness = await Harness.CreateAsync();
        var created = await harness.CreateTargetedPendingAsync();
        Assert.True(created.IsSuccess, created.ErrorMessage);

        var accepted = await harness.Accept.ExecuteByIdAsync(
            CustomerLinkRequestId.From(created.Value!.Id),
            harness.Personal.Id,
            AccountClass.Personal);

        Assert.True(accepted.IsSuccess, accepted.ErrorMessage);
        Assert.False(accepted.Value!.CreatedOrganizationMembership);
        Assert.False(accepted.Value.GrantedStaffRole);
        Assert.False(accepted.Value.GrantedProductRole);
        Assert.Equal(0, harness.Memberships.AddCount);

        var link = await harness.Links.GetByIdAsync(
            LinkedCustomerAppUserId.From(accepted.Value.LinkedCustomerAppUserId));
        Assert.NotNull(link);
        Assert.Equal(LinkedCustomerAppUserStatus.Active, link!.Status);
        Assert.Equal(harness.Personal.Id, link.UserIdentityId);
        Assert.Equal(harness.Customer.Id, link.BusinessCustomerId);
    }

    [Fact]
    public async Task Accept_notifies_inviter_only_not_other_orgs_or_users()
    {
        var harness = await Harness.CreateAsync();
        var otherOrg = PlatformOrganization.Create("Other Shop", "other-shop", T0);
        await harness.Orgs.AddAsync(otherOrg);
        var otherStaff = PlatformUser.Create("other.staff.user", "Other Staff", "staff2@example.com", T0);
        await harness.Users.AddAsync(otherStaff);

        var created = await harness.CreateTargetedPendingAsync();
        Assert.True(created.IsSuccess, created.ErrorMessage);

        var accepted = await harness.Accept.ExecuteByIdAsync(
            CustomerLinkRequestId.From(created.Value!.Id),
            harness.Personal.Id,
            AccountClass.Personal);
        Assert.True(accepted.IsSuccess, accepted.ErrorMessage);

        Assert.Single(harness.OrgNotifications.Items);
        var notification = harness.OrgNotifications.Items[0];
        Assert.Equal(harness.Org.Id, notification.OrganizationId);
        Assert.Equal(harness.Inviter.Id, notification.RecipientUserIdentityId);
        Assert.Equal(CustomerLinkNotificationTypes.OrganizationAccepted, notification.RelatedType);
        Assert.Equal(created.Value.Id.ToString("D"), notification.RelatedId);

        var inviterInbox = await harness.ListOrgNotifications.ExecuteAsync(
            harness.Org.Id,
            harness.Inviter.Id);
        Assert.Single(inviterInbox);

        var otherUserInbox = await harness.ListOrgNotifications.ExecuteAsync(
            harness.Org.Id,
            otherStaff.Id);
        Assert.Empty(otherUserInbox);

        var otherOrgInbox = await harness.ListOrgNotifications.ExecuteAsync(
            otherOrg.Id,
            harness.Inviter.Id);
        Assert.Empty(otherOrgInbox);
    }

    [Fact]
    public async Task Decline_by_id_keeps_customer_notifies_inviter_and_retains_history()
    {
        var harness = await Harness.CreateAsync();
        var created = await harness.CreateTargetedPendingAsync();
        Assert.True(created.IsSuccess, created.ErrorMessage);

        var declined = await harness.Decline.ExecuteByIdAsync(
            CustomerLinkRequestId.From(created.Value!.Id),
            harness.Personal.Id,
            AccountClass.Personal);

        Assert.True(declined.IsSuccess, declined.ErrorMessage);
        Assert.Equal(nameof(CustomerLinkRequestStatus.Declined), declined.Value!.Status);

        Assert.Equal(0, (await harness.Links.ListByOrganizationAsync(harness.Org.Id, 0, 20)).TotalCount);
        var customer = await harness.Customers.GetByIdAsync(harness.Customer.Id);
        Assert.NotNull(customer);
        Assert.Null(customer!.LinkedUserIdentityId);

        Assert.Single(harness.OrgNotifications.Items);
        Assert.Equal(CustomerLinkNotificationTypes.OrganizationDeclined, harness.OrgNotifications.Items[0].RelatedType);
        Assert.Equal(harness.Inviter.Id, harness.OrgNotifications.Items[0].RecipientUserIdentityId);

        var history = await harness.Requests.ListByBusinessCustomerAsync(harness.Customer.Id);
        Assert.Single(history);
        Assert.Equal(CustomerLinkRequestStatus.Declined, history[0].Status);
    }

    [Fact]
    public async Task Duplicate_accept_and_decline_do_not_duplicate_org_notifications()
    {
        var acceptHarness = await Harness.CreateAsync();
        var acceptCreated = await acceptHarness.CreateTargetedPendingAsync();
        Assert.True(acceptCreated.IsSuccess, acceptCreated.ErrorMessage);
        var requestId = CustomerLinkRequestId.From(acceptCreated.Value!.Id);

        Assert.True((await acceptHarness.Accept.ExecuteByIdAsync(
            requestId,
            acceptHarness.Personal.Id,
            AccountClass.Personal)).IsSuccess);
        var duplicateAccept = await acceptHarness.Accept.ExecuteByIdAsync(
            requestId,
            acceptHarness.Personal.Id,
            AccountClass.Personal);
        Assert.False(duplicateAccept.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.CustomerLinkRequestNotFound, duplicateAccept.ErrorCode);
        Assert.Single(
            acceptHarness.OrgNotifications.Items,
            n => n.RelatedType == CustomerLinkNotificationTypes.OrganizationAccepted);

        var declineHarness = await Harness.CreateAsync();
        var declineCreated = await declineHarness.CreateTargetedPendingAsync();
        Assert.True(declineCreated.IsSuccess, declineCreated.ErrorMessage);
        var declineId = CustomerLinkRequestId.From(declineCreated.Value!.Id);

        Assert.True((await declineHarness.Decline.ExecuteByIdAsync(
            declineId,
            declineHarness.Personal.Id,
            AccountClass.Personal)).IsSuccess);
        var duplicateDecline = await declineHarness.Decline.ExecuteByIdAsync(
            declineId,
            declineHarness.Personal.Id,
            AccountClass.Personal);
        Assert.False(duplicateDecline.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.CustomerLinkRequestNotFound, duplicateDecline.ErrorCode);
        Assert.Single(
            declineHarness.OrgNotifications.Items,
            n => n.RelatedType == CustomerLinkNotificationTypes.OrganizationDeclined);
    }

    [Fact]
    public async Task Expired_request_cannot_be_accepted()
    {
        var harness = await Harness.CreateAsync();
        var created = await harness.CreateTargetedPendingAsync();
        Assert.True(created.IsSuccess, created.ErrorMessage);

        harness.Clock.UtcNow = T0.AddDays(8);

        var accepted = await harness.Accept.ExecuteByIdAsync(
            CustomerLinkRequestId.From(created.Value!.Id),
            harness.Personal.Id,
            AccountClass.Personal);

        Assert.False(accepted.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.CustomerLinkRequestNotFound, accepted.ErrorCode);
        Assert.Equal(0, (await harness.Links.ListByOrganizationAsync(harness.Org.Id, 0, 20)).TotalCount);
    }

    [Fact]
    public async Task Revoked_request_cannot_be_accepted()
    {
        var harness = await Harness.CreateAsync();
        var created = await harness.CreateTargetedPendingAsync();
        Assert.True(created.IsSuccess, created.ErrorMessage);

        var revoked = await harness.RevokePending.ExecuteAsync(
            CustomerLinkRequestId.From(created.Value!.Id),
            harness.Org.Id);
        Assert.True(revoked.IsSuccess, revoked.ErrorMessage);

        var accepted = await harness.Accept.ExecuteByIdAsync(
            CustomerLinkRequestId.From(created.Value.Id),
            harness.Personal.Id,
            AccountClass.Personal);
        Assert.False(accepted.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.CustomerLinkRequestNotFound, accepted.ErrorCode);
    }

    [Fact]
    public async Task Organization_session_and_staff_identity_cannot_accept_by_id()
    {
        var harness = await Harness.CreateAsync();
        var created = await harness.CreateTargetedPendingAsync();
        Assert.True(created.IsSuccess, created.ErrorMessage);
        var requestId = CustomerLinkRequestId.From(created.Value!.Id);

        var orgSession = await harness.Accept.ExecuteByIdAsync(
            requestId,
            harness.Personal.Id,
            AccountClass.Organization);
        Assert.False(orgSession.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.AccountScopeDenied, orgSession.ErrorCode);

        var staff = PlatformUser.CreateOrganizationStaff(
            "maria_org001842",
            "maria@ORG001842",
            harness.Personal.NormalizedEmail,
            harness.Org.Id,
            "Maria Staff",
            T0);
        await harness.Users.AddAsync(staff);

        var staffAccept = await harness.Accept.ExecuteByIdAsync(
            requestId,
            staff.Id,
            AccountClass.Personal);
        Assert.False(staffAccept.IsSuccess);
        Assert.Equal(DomainErrorCodes.CustomerLinkPersonalIdentityRequired, staffAccept.ErrorCode);
        Assert.Equal(0, harness.Memberships.AddCount);
    }

    [Fact]
    public async Task Email_invitation_path_still_works_with_token_accept()
    {
        var harness = await Harness.CreateAsync();

        var created = await harness.CreateRequest.ExecuteAsync(
            harness.Org.Id,
            harness.Customer.Id,
            harness.Personal.NormalizedEmail,
            harness.Inviter.Id);
        Assert.True(created.IsSuccess, created.ErrorMessage);
        Assert.NotNull(created.Value!.AcceptToken);
        Assert.Null(created.Value.TargetUserIdentityId);
        Assert.Empty(harness.PersonalNotifications.Items);

        var accepted = await harness.Accept.ExecuteAsync(
            created.Value.AcceptToken!,
            harness.Personal.Id,
            AccountClass.Personal);
        Assert.True(accepted.IsSuccess, accepted.ErrorMessage);
        Assert.False(accepted.Value!.CreatedOrganizationMembership);
        Assert.Single(harness.OrgNotifications.Items);
        Assert.Equal(CustomerLinkNotificationTypes.OrganizationAccepted, harness.OrgNotifications.Items[0].RelatedType);
    }

    [Fact]
    public async Task Stats_count_pending_active_declined_and_expired()
    {
        var harness = await Harness.CreateAsync();

        var pendingCustomer = BusinessCustomer.Create(harness.Org.Id, "Pending Customer", T0, email: "pending@example.com");
        await harness.Customers.AddAsync(pendingCustomer);
        var pending = await harness.CreateRequest.ExecuteAsync(
            harness.Org.Id,
            pendingCustomer.Id,
            email: null,
            harness.Inviter.Id,
            harness.Personal.Id,
            harness.Personal.PublicUserId);
        Assert.True(pending.IsSuccess, pending.ErrorMessage);

        var acceptCustomer = BusinessCustomer.Create(harness.Org.Id, "Accept Customer", T0, email: "accept@example.com");
        await harness.Customers.AddAsync(acceptCustomer);
        var acceptInvite = await harness.CreateRequest.ExecuteAsync(
            harness.Org.Id,
            acceptCustomer.Id,
            email: null,
            harness.Inviter.Id,
            harness.Personal.Id,
            harness.Personal.PublicUserId);
        Assert.True(acceptInvite.IsSuccess, acceptInvite.ErrorMessage);
        Assert.True((await harness.Accept.ExecuteByIdAsync(
            CustomerLinkRequestId.From(acceptInvite.Value!.Id),
            harness.Personal.Id,
            AccountClass.Personal)).IsSuccess);

        var declineCustomer = BusinessCustomer.Create(harness.Org.Id, "Decline Customer", T0, email: "decline@example.com");
        await harness.Customers.AddAsync(declineCustomer);
        var declineInvite = await harness.CreateRequest.ExecuteAsync(
            harness.Org.Id,
            declineCustomer.Id,
            email: null,
            harness.Inviter.Id,
            harness.Personal.Id,
            harness.Personal.PublicUserId);
        Assert.True(declineInvite.IsSuccess, declineInvite.ErrorMessage);
        Assert.True((await harness.Decline.ExecuteByIdAsync(
            CustomerLinkRequestId.From(declineInvite.Value!.Id),
            harness.Personal.Id,
            AccountClass.Personal)).IsSuccess);

        var expireCustomer = BusinessCustomer.Create(harness.Org.Id, "Expire Customer", T0, email: "expire@example.com");
        await harness.Customers.AddAsync(expireCustomer);
        var (expireRequest, _) = CustomerLinkRequest.Create(
            harness.Org.Id,
            expireCustomer.Id,
            harness.Personal.NormalizedEmail,
            T0,
            harness.Inviter.Id,
            lifetime: TimeSpan.FromHours(1),
            targetUserIdentityId: harness.Personal.Id,
            targetPublicUserId: harness.Personal.PublicUserId);
        await harness.Requests.AddAsync(expireRequest);

        // Only the short-lifetime invite expires; default 7-day pending remains Pending.
        harness.Clock.UtcNow = T0.AddHours(2);
        var stats = await harness.Stats.CountByOrganizationAsync(harness.Org.Id);

        Assert.Equal(1, stats.CountsByStatus[nameof(CustomerLinkRequestStatus.Pending)]);
        Assert.Equal(1, stats.CountsByStatus[nameof(CustomerLinkRequestStatus.Active)]);
        Assert.Equal(1, stats.CountsByStatus[nameof(CustomerLinkRequestStatus.Declined)]);
        Assert.Equal(1, stats.CountsByStatus[nameof(CustomerLinkRequestStatus.Expired)]);
    }

    [Fact]
    public async Task Authorize_denied_before_accept_and_allowed_after()
    {
        var harness = await Harness.CreateAsync();
        var created = await harness.CreateTargetedPendingAsync();
        Assert.True(created.IsSuccess, created.ErrorMessage);

        var before = await harness.Authorize.ExecuteAsync(
            harness.Personal.Id,
            AccountClass.Personal,
            harness.Org.Id,
            harness.Customer.Id);
        Assert.False(before.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.LinkedCustomerAppUserNotFound, before.ErrorCode);

        var accepted = await harness.Accept.ExecuteByIdAsync(
            CustomerLinkRequestId.From(created.Value!.Id),
            harness.Personal.Id,
            AccountClass.Personal);
        Assert.True(accepted.IsSuccess, accepted.ErrorMessage);

        var after = await harness.Authorize.ExecuteAsync(
            harness.Personal.Id,
            AccountClass.Personal,
            harness.Org.Id,
            harness.Customer.Id);
        Assert.True(after.IsSuccess, after.ErrorMessage);
        Assert.Equal(accepted.Value!.LinkedCustomerAppUserId, after.Value!.LinkedCustomerAppUserId);
        Assert.Equal(harness.Personal.Id.Value, after.Value.PersonalUserId);
        Assert.Equal(harness.Customer.Id.Value, after.Value.PlatformBusinessCustomerId);
    }

    internal sealed class Harness
    {
        private Harness(
            FixedClock clock,
            PlatformOrganization org,
            PlatformUser personal,
            PlatformUser inviter,
            BusinessCustomer customer,
            InMemoryPlatformUserRepository users,
            InMemoryOrganizationMembershipRepository memberships,
            CustomerLinkCompletenessTests.InMemoryBusinessCustomerRepository customers,
            CustomerLinkCompletenessTests.InMemoryLinkedCustomerAppUserRepository links,
            CustomerLinkCompletenessTests.InMemoryCustomerLinkRequestRepository requests,
            InMemoryPlatformOrganizationRepository orgs,
            CustomerLinkCompletenessTests.InMemoryPersonalInAppNotificationRepository personalNotifications,
            CustomerLinkCompletenessTests.InMemoryOrganizationInAppNotificationRepository orgNotifications,
            CreateCustomerLinkRequest createRequest,
            AcceptCustomerLinkRequest accept,
            DeclineCustomerLinkRequest decline,
            RevokeCustomerLinkRequest revokePending,
            ListPendingCustomerLinkRequestsForPersonalUser listPending,
            ListOrganizationInAppNotifications listOrgNotifications,
            CustomerLinkRequestStatsQuery stats,
            AuthorizeLinkedCustomerAccess authorize)
        {
            Clock = clock;
            Org = org;
            Personal = personal;
            Inviter = inviter;
            Customer = customer;
            Users = users;
            Memberships = memberships;
            Customers = customers;
            Links = links;
            Requests = requests;
            Orgs = orgs;
            PersonalNotifications = personalNotifications;
            OrgNotifications = orgNotifications;
            CreateRequest = createRequest;
            Accept = accept;
            Decline = decline;
            RevokePending = revokePending;
            ListPending = listPending;
            ListOrgNotifications = listOrgNotifications;
            Stats = stats;
            Authorize = authorize;
        }

        public FixedClock Clock { get; }
        public PlatformOrganization Org { get; }
        public PlatformUser Personal { get; }
        public PlatformUser Inviter { get; }
        public BusinessCustomer Customer { get; }
        public InMemoryPlatformUserRepository Users { get; }
        public InMemoryOrganizationMembershipRepository Memberships { get; }
        public CustomerLinkCompletenessTests.InMemoryBusinessCustomerRepository Customers { get; }
        public CustomerLinkCompletenessTests.InMemoryLinkedCustomerAppUserRepository Links { get; }
        public CustomerLinkCompletenessTests.InMemoryCustomerLinkRequestRepository Requests { get; }
        public InMemoryPlatformOrganizationRepository Orgs { get; }
        public CustomerLinkCompletenessTests.InMemoryPersonalInAppNotificationRepository PersonalNotifications { get; }
        public CustomerLinkCompletenessTests.InMemoryOrganizationInAppNotificationRepository OrgNotifications { get; }
        public CreateCustomerLinkRequest CreateRequest { get; }
        public AcceptCustomerLinkRequest Accept { get; }
        public DeclineCustomerLinkRequest Decline { get; }
        public RevokeCustomerLinkRequest RevokePending { get; }
        public ListPendingCustomerLinkRequestsForPersonalUser ListPending { get; }
        public ListOrganizationInAppNotifications ListOrgNotifications { get; }
        public CustomerLinkRequestStatsQuery Stats { get; }
        public AuthorizeLinkedCustomerAccess Authorize { get; }

        public Task<ApplicationResult<CustomerLinkRequestDto>> CreateTargetedPendingAsync() =>
            CreateRequest.ExecuteAsync(
                Org.Id,
                Customer.Id,
                email: null,
                Inviter.Id,
                Personal.Id,
                Personal.PublicUserId);

        public static async Task<Harness> CreateAsync()
        {
            var clock = new FixedClock(T0);
            var uow = new NoOpUnitOfWork();
            var users = new InMemoryPlatformUserRepository();
            var memberships = new InMemoryOrganizationMembershipRepository();
            var orgs = new InMemoryPlatformOrganizationRepository();
            var customers = new CustomerLinkCompletenessTests.InMemoryBusinessCustomerRepository();
            var requests = new CustomerLinkCompletenessTests.InMemoryCustomerLinkRequestRepository(clock);
            var links = new CustomerLinkCompletenessTests.InMemoryLinkedCustomerAppUserRepository();
            var personalSettings = new CustomerLinkCompletenessTests.InMemoryPersonalAccountSettingsRepository();
            var personalNotifications = new CustomerLinkCompletenessTests.InMemoryPersonalInAppNotificationRepository();
            var orgNotifications = new CustomerLinkCompletenessTests.InMemoryOrganizationInAppNotificationRepository();

            var org = PlatformOrganization.Create("Corner Store", "corner-store", T0);
            await orgs.AddAsync(org);

            var personal = PlatformUser.Create("rosa.personal", "Rosa Personal", "rosa@example.com", T0);
            personal.AssignPublicUserId("EX-4827-1936", T0);
            await users.AddAsync(personal);
            await personalSettings.AddAsync(PersonalAccountSettings.CreateDefaults(personal.Id, T0));

            var inviter = PlatformUser.Create("owner.user", "Owner User", "owner@example.com", T0);
            await users.AddAsync(inviter);

            var customer = BusinessCustomer.Create(org.Id, "Store Customer", T0, email: "rosa@example.com");
            await customers.AddAsync(customer);

            var createRequest = new CreateCustomerLinkRequest(
                customers,
                requests,
                uow,
                clock,
                users,
                orgs,
                personalSettings,
                personalNotifications);
            var accept = new AcceptCustomerLinkRequest(
                requests,
                customers,
                links,
                memberships,
                users,
                uow,
                clock,
                orgNotifications);
            var decline = new DeclineCustomerLinkRequest(requests, uow, clock, orgNotifications, users);

            return new Harness(
                clock,
                org,
                personal,
                inviter,
                customer,
                users,
                memberships,
                customers,
                links,
                requests,
                orgs,
                personalNotifications,
                orgNotifications,
                createRequest,
                accept,
                decline,
                new RevokeCustomerLinkRequest(requests, uow, clock),
                new ListPendingCustomerLinkRequestsForPersonalUser(requests, users, orgs, clock),
                new ListOrganizationInAppNotifications(orgNotifications),
                new CustomerLinkRequestStatsQuery(requests),
                new AuthorizeLinkedCustomerAccess(users, links, customers));
        }
    }
}
