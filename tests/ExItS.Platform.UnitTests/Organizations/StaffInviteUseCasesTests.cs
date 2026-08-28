using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Identity;
using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Application.Personal;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Personal;
using ExItS.Platform.UnitTests.Support;
using ExItS.Platform.UnitTests.TestSupport;

namespace ExItS.Platform.UnitTests.Organizations;

public sealed class StaffInviteUseCasesTests
{
    private static readonly DateTimeOffset T0 = new(2026, 8, 28, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ResolveStaffInviteTarget_rejects_unknown_and_org_qr()
    {
        var users = new InMemoryPlatformUserRepository();
        var resolve = new ResolveStaffInviteTarget(users);

        var unknown = await resolve.ExecuteAsync("EX-0000-0001");
        Assert.False(unknown.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.UserNotFound, unknown.ErrorCode);

        var businessQr = await resolve.ExecuteAsync(
            ExItsQrEnvelope.Build(ExItsQrPurpose.Organization, "ORG123456"));
        Assert.False(businessQr.IsSuccess);
        Assert.Contains("Business QR", businessQr.ErrorMessage!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Create_native_invite_notifies_personal_and_blocks_duplicate_pending()
    {
        var clock = new FixedClock(T0);
        var uow = new NoOpUnitOfWork();
        var users = new InMemoryPlatformUserRepository();
        var orgs = new InMemoryPlatformOrganizationRepository();
        var memberships = new InMemoryOrganizationMembershipRepository();
        var invitations = new InMemoryOrganizationInvitationRepository();
        var notifications = new StaffInvitePersonalNotificationRepository();

        var org = (await new CreatePlatformOrganization(orgs, new FakePublicOrganizationIdGenerator(), uow, clock)
            .ExecuteAsync("Kizy Store", "kizy")).Value!;

        var owner = PlatformUser.Create("owner", "Owner", "owner@example.com", T0);
        owner.AssignPublicUserId("EX-1111-1111", T0);
        var personal = PlatformUser.Create("maria", "Maria Santos", "maria@example.com", T0);
        personal.AssignPublicUserId("EX-1234-5678", T0);
        await users.AddAsync(owner);
        await users.AddAsync(personal);

        var create = new CreateOrganizationInvitationForPersonal(
            orgs,
            invitations,
            memberships,
            users,
            new FakePublicOrganizationIdGenerator(),
            new ResolveStaffInviteTarget(users),
            uow,
            clock,
            notifications);

        var first = await create.ExecuteAsync(org.Id, "EX-1234-5678", owner.Id, productRole: "Cashier");
        Assert.True(first.IsSuccess, first.ErrorMessage);
        Assert.Equal(InvitationStatus.Pending.ToString(), first.Value!.Status);
        Assert.Equal(personal.Id.Value, first.Value.TargetPersonalUserId);
        Assert.Null(first.Value.AcceptToken);

        Assert.Single(notifications.Items);
        Assert.Equal(
            OrganizationStaffInvitationNotificationTypes.PersonalPendingInvite,
            notifications.Items[0].RelatedType);
        Assert.Equal(personal.Id, notifications.Items[0].RecipientUserIdentityId);

        var duplicate = await create.ExecuteAsync(org.Id, "EX-1234-5678", owner.Id, productRole: "Cashier");
        Assert.False(duplicate.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.InvitationConflict, duplicate.ErrorCode);
        Assert.Contains("already sent", duplicate.ErrorMessage!, StringComparison.OrdinalIgnoreCase);

        var self = await create.ExecuteAsync(org.Id, "EX-1111-1111", owner.Id, productRole: "Cashier");
        Assert.False(self.IsSuccess);
        Assert.Contains("already the owner", self.ErrorMessage!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Decline_marks_declined_without_membership()
    {
        var clock = new FixedClock(T0);
        var uow = new NoOpUnitOfWork();
        var users = new InMemoryPlatformUserRepository();
        var orgs = new InMemoryPlatformOrganizationRepository();
        var memberships = new InMemoryOrganizationMembershipRepository();
        var invitations = new InMemoryOrganizationInvitationRepository();

        var org = (await new CreatePlatformOrganization(orgs, new FakePublicOrganizationIdGenerator(), uow, clock)
            .ExecuteAsync("Kizy Store", "kizy")).Value!;
        var personal = PlatformUser.Create("maria2", "Maria", "maria2@example.com", T0);
        personal.AssignPublicUserId("EX-2222-2222", T0);
        await users.AddAsync(personal);

        var create = new CreateOrganizationInvitationForPersonal(
            orgs,
            invitations,
            memberships,
            users,
            new FakePublicOrganizationIdGenerator(),
            new ResolveStaffInviteTarget(users),
            uow,
            clock);

        var created = await create.ExecuteAsync(org.Id, "EX-2222-2222", invitedByUserId: null, productRole: "Cashier");
        Assert.True(created.IsSuccess, created.ErrorMessage);

        var decline = new DeclineOrganizationInvitationForPersonal(invitations, uow, clock);
        var declined = await decline.ExecuteAsync(
            OrganizationInvitationId.From(created.Value!.Id),
            personal.Id);
        Assert.True(declined.IsSuccess, declined.ErrorMessage);
        Assert.Equal(InvitationStatus.Declined.ToString(), declined.Value!.Status);

        var (items, _) = await memberships.ListByOrganizationAsync(org.Id, null, 0, 20);
        Assert.Empty(items);
    }

    private sealed class StaffInvitePersonalNotificationRepository : IPersonalInAppNotificationRepository
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
                    .Take(take)
                    .ToList());

        public Task<(IReadOnlyList<PersonalInAppNotification> Items, int TotalCount)> ListForUserPagedAsync(
            PlatformUserId recipientUserIdentityId,
            DateTimeOffset? createdOnOrAfterUtc,
            DateTimeOffset? createdBeforeUtc,
            bool unreadOnly,
            int skip,
            int take,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<(IReadOnlyList<PersonalInAppNotification>, int)>(
                (_items.Where(n => n.RecipientUserIdentityId == recipientUserIdentityId).Skip(skip).Take(take).ToList(),
                    _items.Count(n => n.RecipientUserIdentityId == recipientUserIdentityId)));

        public Task<int> CountUnreadForUserAsync(
            PlatformUserId recipientUserIdentityId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.Count(n => n.RecipientUserIdentityId == recipientUserIdentityId && !n.IsRead));

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

        public Task UpdateAsync(PersonalInAppNotification notification, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
