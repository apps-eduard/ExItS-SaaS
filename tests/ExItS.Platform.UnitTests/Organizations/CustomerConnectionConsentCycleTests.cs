using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Application.Personal;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Personal;
using ExItS.Platform.UnitTests.Support;

namespace ExItS.Platform.UnitTests.Organizations;

public sealed class CustomerConnectionConsentCycleTests
{
    private static readonly DateTimeOffset T0 = new(2026, 8, 25, 4, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Remind_pending_increments_count_sets_last_at_and_creates_notification()
    {
        var h = await Harness.CreateAsync();
        var pending = await h.CreateTargetedPendingAsync();
        Assert.True(pending.IsSuccess, pending.ErrorMessage);

        var reminded = await h.Remind.ExecuteAsync(
            CustomerLinkRequestId.From(pending.Value!.Id),
            h.Org.Id);
        Assert.True(reminded.IsSuccess, reminded.ErrorMessage);
        Assert.Equal(1, reminded.Value!.ReminderCount);
        Assert.Equal(T0, reminded.Value.LastRemindedAtUtc);
        Assert.Equal(T0.AddHours(24), reminded.Value.NextReminderEligibleAtUtc);

        Assert.Contains(
            h.PersonalNotifications.Items,
            n => n.RelatedType == CustomerLinkNotificationTypes.PersonalCustomerLinkReminder
                 && n.RelatedId == $"{pending.Value.Id:D}:1");
    }

    [Fact]
    public async Task Remind_before_cooldown_is_denied()
    {
        var h = await Harness.CreateAsync();
        var pending = await h.CreateTargetedPendingAsync();
        Assert.True((await h.Remind.ExecuteAsync(CustomerLinkRequestId.From(pending.Value!.Id), h.Org.Id)).IsSuccess);

        h.Clock.UtcNow = T0.AddHours(1);
        var tooSoon = await h.Remind.ExecuteAsync(CustomerLinkRequestId.From(pending.Value.Id), h.Org.Id);
        Assert.False(tooSoon.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.CustomerLinkReminderTooSoon, tooSoon.ErrorCode);

        h.Clock.UtcNow = T0.AddHours(24);
        var after = await h.Remind.ExecuteAsync(CustomerLinkRequestId.From(pending.Value.Id), h.Org.Id);
        Assert.True(after.IsSuccess, after.ErrorMessage);
        Assert.Equal(2, after.Value!.ReminderCount);
    }

    [Fact]
    public async Task Remind_denied_for_declined_expired_revoked_and_linked()
    {
        var h = await Harness.CreateAsync();
        var pending = await h.CreateTargetedPendingAsync();
        Assert.True(pending.IsSuccess);

        await h.Decline.ExecuteByIdAsync(
            CustomerLinkRequestId.From(pending.Value!.Id),
            h.Personal.Id,
            AccountClass.Personal);
        var declinedRemind = await h.Remind.ExecuteAsync(CustomerLinkRequestId.From(pending.Value.Id), h.Org.Id);
        Assert.False(declinedRemind.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.CustomerLinkRequestNotPending, declinedRemind.ErrorCode);

        var h2 = await Harness.CreateAsync();
        var p2 = await h2.CreateTargetedPendingAsync();
        await h2.RevokePending.ExecuteAsync(CustomerLinkRequestId.From(p2.Value!.Id), h2.Org.Id);
        Assert.False((await h2.Remind.ExecuteAsync(CustomerLinkRequestId.From(p2.Value.Id), h2.Org.Id)).IsSuccess);

        var h3 = await Harness.CreateAsync();
        var p3 = await h3.CreateTargetedPendingAsync();
        h3.Clock.UtcNow = T0.AddDays(8);
        Assert.False((await h3.Remind.ExecuteAsync(CustomerLinkRequestId.From(p3.Value!.Id), h3.Org.Id)).IsSuccess);

        var h4 = await Harness.CreateAsync();
        var p4 = await h4.CreateTargetedPendingAsync();
        Assert.True((await h4.Accept.ExecuteByIdAsync(
            CustomerLinkRequestId.From(p4.Value!.Id),
            h4.Personal.Id,
            AccountClass.Personal)).IsSuccess);
        Assert.False((await h4.Remind.ExecuteAsync(CustomerLinkRequestId.From(p4.Value.Id), h4.Org.Id)).IsSuccess);
    }

    [Fact]
    public async Task Decline_does_not_create_block_and_invite_again_succeeds()
    {
        var h = await Harness.CreateAsync();
        var pending = await h.CreateTargetedPendingAsync();
        Assert.True(pending.IsSuccess);

        var declined = await h.Decline.ExecuteByIdAsync(
            CustomerLinkRequestId.From(pending.Value!.Id),
            h.Personal.Id,
            AccountClass.Personal);
        Assert.True(declined.IsSuccess, declined.ErrorMessage);
        Assert.Empty(await h.Blocks.ListActiveByPersonalUserAsync(h.Personal.Id));

        var again = await h.CreateTargetedPendingAsync();
        Assert.True(again.IsSuccess, again.ErrorMessage);
        Assert.NotEqual(pending.Value.Id, again.Value!.Id);
        Assert.Equal(nameof(CustomerLinkRequestStatus.Pending), again.Value.Status);
    }

    [Fact]
    public async Task Block_pending_activates_pair_block_and_denies_org_invite_remind_resend()
    {
        var h = await Harness.CreateAsync();
        var pending = await h.CreateTargetedPendingAsync();
        Assert.True(pending.IsSuccess);

        var blocked = await h.BlockFromRequest.ExecuteAsync(
            CustomerLinkRequestId.From(pending.Value!.Id),
            h.Personal.Id);
        Assert.True(blocked.IsSuccess, blocked.ErrorMessage);

        var active = await h.Blocks.FindActiveByPersonalAndOrganizationAsync(h.Personal.Id, h.Org.Id);
        Assert.NotNull(active);

        // Idempotent block
        Assert.True((await h.BlockFromRequest.ExecuteAsync(
            CustomerLinkRequestId.From(pending.Value.Id),
            h.Personal.Id)).IsSuccess);
        Assert.Single(await h.Blocks.ListActiveByPersonalUserAsync(h.Personal.Id));

        var invite = await h.CreateTargetedPendingAsync();
        Assert.False(invite.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.CustomerConnectionUnavailable, invite.ErrorCode);
        Assert.Equal(CustomerConnectionBlockSupport.OrgUnavailableMessage, invite.ErrorMessage);

        Assert.False((await h.Remind.ExecuteAsync(CustomerLinkRequestId.From(pending.Value.Id), h.Org.Id)).IsSuccess);
        Assert.False((await h.Resend.ExecuteAsync(CustomerLinkRequestId.From(pending.Value.Id), h.Org.Id)).IsSuccess);

        var status = await h.Status.ExecuteAsync(h.Org.Id, h.Customer.Id);
        Assert.True(status.IsSuccess);
        Assert.Equal("Unavailable", status.Value!.Status);
    }

    [Fact]
    public async Task Block_second_customer_bypass_denied_other_org_unaffected()
    {
        var h = await Harness.CreateAsync();
        var pending = await h.CreateTargetedPendingAsync();
        Assert.True((await h.BlockFromRequest.ExecuteAsync(
            CustomerLinkRequestId.From(pending.Value!.Id),
            h.Personal.Id)).IsSuccess);

        var customer2 = BusinessCustomer.Create(h.Org.Id, "Customer Dup", T0, email: "rosa@example.com");
        await h.Customers.AddAsync(customer2);
        var bypass = await h.CreateRequest.ExecuteAsync(
            h.Org.Id,
            customer2.Id,
            email: null,
            h.Inviter.Id,
            h.Personal.Id,
            h.Personal.PublicUserId);
        Assert.False(bypass.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.CustomerConnectionUnavailable, bypass.ErrorCode);

        var orgB = PlatformOrganization.Create("Other Org", "other-org", T0);
        await h.Orgs.AddAsync(orgB);
        var customerB = BusinessCustomer.Create(orgB.Id, "Other Customer", T0, email: "rosa@example.com");
        await h.Customers.AddAsync(customerB);
        var otherOrgInvite = await h.CreateRequest.ExecuteAsync(
            orgB.Id,
            customerB.Id,
            email: null,
            h.Inviter.Id,
            h.Personal.Id,
            h.Personal.PublicUserId);
        Assert.True(otherOrgInvite.IsSuccess, otherOrgInvite.ErrorMessage);
    }

    [Fact]
    public async Task Unblock_does_not_auto_connect_and_allows_new_invite()
    {
        var h = await Harness.CreateAsync();
        var pending = await h.CreateTargetedPendingAsync();
        Assert.True((await h.BlockFromRequest.ExecuteAsync(
            CustomerLinkRequestId.From(pending.Value!.Id),
            h.Personal.Id)).IsSuccess);

        Assert.True((await h.Unblock.ExecuteAsync(h.Personal.Id, h.Org.Id)).IsSuccess);
        Assert.Null(await h.Blocks.FindActiveByPersonalAndOrganizationAsync(h.Personal.Id, h.Org.Id));
        Assert.Equal(0, (await h.Links.ListByOrganizationAsync(h.Org.Id, 0, 20)).TotalCount);

        var again = await h.CreateTargetedPendingAsync();
        Assert.True(again.IsSuccess, again.ErrorMessage);
        Assert.Equal(nameof(CustomerLinkRequestStatus.Pending), again.Value!.Status);
    }

    [Fact]
    public async Task Disconnect_and_disconnect_block_preserve_customer_and_revoke_link()
    {
        var h = await Harness.CreateAsync();
        var pending = await h.CreateTargetedPendingAsync();
        Assert.True((await h.Accept.ExecuteByIdAsync(
            CustomerLinkRequestId.From(pending.Value!.Id),
            h.Personal.Id,
            AccountClass.Personal)).IsSuccess);

        var linked = await h.Customers.GetByIdAsync(h.Customer.Id);
        Assert.Equal(h.Personal.Id, linked!.LinkedUserIdentityId);

        var disconnect = await h.Disconnect.ExecuteAsync(h.Personal.Id, h.Org.Id);
        Assert.True(disconnect.IsSuccess, disconnect.ErrorMessage);
        Assert.Equal(1, disconnect.Value!.RevokedLinkCount);
        Assert.False(disconnect.Value.BlockActivated);

        var after = await h.Customers.GetByIdAsync(h.Customer.Id);
        Assert.NotNull(after);
        Assert.Null(after!.LinkedUserIdentityId);
        Assert.Equal(0, (await h.Links.ListActiveByUserAndOrganizationAsync(h.Personal.Id, h.Org.Id)).Count);

        // Re-link then disconnect+block
        var pending2 = await h.CreateTargetedPendingAsync();
        Assert.True((await h.Accept.ExecuteByIdAsync(
            CustomerLinkRequestId.From(pending2.Value!.Id),
            h.Personal.Id,
            AccountClass.Personal)).IsSuccess);

        var disconnectBlock = await h.Disconnect.ExecuteAndBlockAsync(h.Personal.Id, h.Org.Id);
        Assert.True(disconnectBlock.IsSuccess, disconnectBlock.ErrorMessage);
        Assert.True(disconnectBlock.Value!.BlockActivated);
        Assert.NotNull(await h.Blocks.FindActiveByPersonalAndOrganizationAsync(h.Personal.Id, h.Org.Id));
        Assert.False((await h.CreateTargetedPendingAsync()).IsSuccess);

        // Same BusinessCustomer still exists
        Assert.NotNull(await h.Customers.GetByIdAsync(h.Customer.Id));
    }

    private sealed class Harness
    {
        private Harness(
            FixedClock clock,
            PlatformOrganization org,
            PlatformUser personal,
            PlatformUser inviter,
            BusinessCustomer customer,
            CustomerLinkCompletenessTests.InMemoryBusinessCustomerRepository customers,
            CustomerLinkCompletenessTests.InMemoryLinkedCustomerAppUserRepository links,
            CustomerLinkCompletenessTests.InMemoryCustomerLinkRequestRepository requests,
            InMemoryPlatformOrganizationRepository orgs,
            CustomerLinkCompletenessTests.InMemoryPersonalInAppNotificationRepository personalNotifications,
            CustomerLinkCompletenessTests.InMemoryPersonalOrganizationConnectionBlockRepository blocks,
            CreateCustomerLinkRequest createRequest,
            AcceptCustomerLinkRequest accept,
            DeclineCustomerLinkRequest decline,
            RevokeCustomerLinkRequest revokePending,
            ResendCustomerLinkRequest resend,
            RemindCustomerLinkRequest remind,
            BlockBusinessFromCustomerLinkRequest blockFromRequest,
            UnblockPersonalOrganizationConnection unblock,
            DisconnectPersonalLinkedMerchant disconnect,
            GetCustomerLinkStatusForBusinessCustomer status)
        {
            Clock = clock;
            Org = org;
            Personal = personal;
            Inviter = inviter;
            Customer = customer;
            Customers = customers;
            Links = links;
            Requests = requests;
            Orgs = orgs;
            PersonalNotifications = personalNotifications;
            Blocks = blocks;
            CreateRequest = createRequest;
            Accept = accept;
            Decline = decline;
            RevokePending = revokePending;
            Resend = resend;
            Remind = remind;
            BlockFromRequest = blockFromRequest;
            Unblock = unblock;
            Disconnect = disconnect;
            Status = status;
        }

        public FixedClock Clock { get; }
        public PlatformOrganization Org { get; }
        public PlatformUser Personal { get; }
        public PlatformUser Inviter { get; }
        public BusinessCustomer Customer { get; }
        public CustomerLinkCompletenessTests.InMemoryBusinessCustomerRepository Customers { get; }
        public CustomerLinkCompletenessTests.InMemoryLinkedCustomerAppUserRepository Links { get; }
        public CustomerLinkCompletenessTests.InMemoryCustomerLinkRequestRepository Requests { get; }
        public InMemoryPlatformOrganizationRepository Orgs { get; }
        public CustomerLinkCompletenessTests.InMemoryPersonalInAppNotificationRepository PersonalNotifications { get; }
        public CustomerLinkCompletenessTests.InMemoryPersonalOrganizationConnectionBlockRepository Blocks { get; }
        public CreateCustomerLinkRequest CreateRequest { get; }
        public AcceptCustomerLinkRequest Accept { get; }
        public DeclineCustomerLinkRequest Decline { get; }
        public RevokeCustomerLinkRequest RevokePending { get; }
        public ResendCustomerLinkRequest Resend { get; }
        public RemindCustomerLinkRequest Remind { get; }
        public BlockBusinessFromCustomerLinkRequest BlockFromRequest { get; }
        public UnblockPersonalOrganizationConnection Unblock { get; }
        public DisconnectPersonalLinkedMerchant Disconnect { get; }
        public GetCustomerLinkStatusForBusinessCustomer Status { get; }

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
            var blocks = new CustomerLinkCompletenessTests.InMemoryPersonalOrganizationConnectionBlockRepository();

            var org = PlatformOrganization.Create("Kissystore", "kissystore", T0);
            await orgs.AddAsync(org);

            var personal = PlatformUser.Create("person.b", "Person B", "person.b@example.com", T0);
            personal.AssignPublicUserId("EX-2000-0001", T0);
            await users.AddAsync(personal);
            await personalSettings.AddAsync(PersonalAccountSettings.CreateDefaults(personal.Id, T0));

            var inviter = PlatformUser.Create("owner.a", "Owner A", "owner.a@example.com", T0);
            await users.AddAsync(inviter);

            var customer = BusinessCustomer.Create(org.Id, "Person B Customer", T0, email: "person.b@example.com");
            await customers.AddAsync(customer);

            var createRequest = new CreateCustomerLinkRequest(
                customers,
                requests,
                uow,
                clock,
                users,
                orgs,
                personalSettings,
                personalNotifications,
                blocks);

            return new Harness(
                clock,
                org,
                personal,
                inviter,
                customer,
                customers,
                links,
                requests,
                orgs,
                personalNotifications,
                blocks,
                createRequest,
                new AcceptCustomerLinkRequest(
                    requests,
                    customers,
                    links,
                    memberships,
                    users,
                    uow,
                    clock,
                    orgNotifications,
                    personalNotifications),
                new DeclineCustomerLinkRequest(requests, uow, clock, orgNotifications, users, personalNotifications),
                new RevokeCustomerLinkRequest(requests, uow, clock),
                new ResendCustomerLinkRequest(
                    requests,
                    uow,
                    clock,
                    customers,
                    users,
                    orgs,
                    personalSettings,
                    personalNotifications,
                    blocks),
                new RemindCustomerLinkRequest(
                    requests,
                    blocks,
                    orgs,
                    personalSettings,
                    personalNotifications,
                    uow,
                    clock),
                new BlockBusinessFromCustomerLinkRequest(requests, blocks, personalNotifications, uow, clock),
                new UnblockPersonalOrganizationConnection(blocks, uow, clock),
                new DisconnectPersonalLinkedMerchant(links, customers, blocks, uow, clock),
                new GetCustomerLinkStatusForBusinessCustomer(customers, requests, blocks, clock));
        }
    }
}
