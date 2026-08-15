using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.UnitTests.Support;

namespace ExItS.Platform.UnitTests.Organizations;

public sealed class OrganizationBusinessNotificationTests
{
    private sealed class FixedClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
    }

    private sealed class FakeUow : IPlatformUnitOfWork
    {
        public int SaveCount { get; private set; }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveCount++;
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task SupplierRequestCreatesOrganizationNotification_for_owner_and_administrator()
    {
        var source = PlatformOrganizationId.From(Guid.NewGuid());
        var recipient = PlatformOrganizationId.From(Guid.NewGuid());
        var owner = PlatformUserId.From(Guid.NewGuid());
        var admin = PlatformUserId.From(Guid.NewGuid());
        var cashier = PlatformUserId.From(Guid.NewGuid());
        var now = DateTimeOffset.UtcNow;

        var memberships = new InMemoryOrganizationMembershipRepository();
        await memberships.AddAsync(OrganizationMembership.Create(recipient, owner, OrganizationRole.OrganizationOwner, now));
        await memberships.AddAsync(OrganizationMembership.Create(recipient, admin, OrganizationRole.OrganizationAdministrator, now));
        await memberships.AddAsync(OrganizationMembership.Create(recipient, cashier, OrganizationRole.OrganizationMember, now));

        var notifications = new CustomerLinkCompletenessTests.InMemoryOrganizationInAppNotificationRepository();
        var uow = new FakeUow();
        var useCase = new PublishOrganizationBusinessNotification(memberships, notifications, uow, new FixedClock(now));

        var relatedId = Guid.NewGuid().ToString("D");
        var result = await useCase.ExecuteAsync(
            source,
            new PublishOrganizationBusinessNotificationRequest(
                recipient.Value,
                SupplierConnectionNotificationTypes.Requested,
                relatedId,
                "Supplier connection request",
                "Mica Store wants to connect with your business as a supplier."));

        Assert.True(result.IsSuccess, $"{result.ErrorCode}: {result.ErrorMessage}");
        Assert.Equal(2, result.Value!.CreatedCount);
        Assert.Equal(0, result.Value.SkippedExistingCount);
        Assert.Equal(2, notifications.Items.Count);
        Assert.All(notifications.Items, n =>
        {
            Assert.Equal(recipient, n.OrganizationId);
            Assert.Equal(SupplierConnectionNotificationTypes.Requested, n.RelatedType);
            Assert.Equal(relatedId, n.RelatedId);
            Assert.False(n.IsRead);
        });
        Assert.DoesNotContain(notifications.Items, n => n.RecipientUserIdentityId == cashier);
        Assert.Equal(1, uow.SaveCount);
    }

    [Fact]
    public async Task Publish_is_idempotent_per_recipient_related()
    {
        var source = PlatformOrganizationId.From(Guid.NewGuid());
        var recipient = PlatformOrganizationId.From(Guid.NewGuid());
        var owner = PlatformUserId.From(Guid.NewGuid());
        var now = DateTimeOffset.UtcNow;
        var memberships = new InMemoryOrganizationMembershipRepository();
        await memberships.AddAsync(OrganizationMembership.Create(recipient, owner, OrganizationRole.OrganizationOwner, now));
        var notifications = new CustomerLinkCompletenessTests.InMemoryOrganizationInAppNotificationRepository();
        var useCase = new PublishOrganizationBusinessNotification(
            memberships, notifications, new FakeUow(), new FixedClock(now));
        var relatedId = Guid.NewGuid().ToString("D");
        var request = new PublishOrganizationBusinessNotificationRequest(
            recipient.Value,
            SupplierConnectionNotificationTypes.Requested,
            relatedId,
            "Supplier connection request",
            "Mica Store wants to connect with your business as a supplier.");

        Assert.True((await useCase.ExecuteAsync(source, request)).IsSuccess);
        var second = await useCase.ExecuteAsync(source, request);

        Assert.True(second.IsSuccess);
        Assert.Equal(0, second.Value!.CreatedCount);
        Assert.Equal(1, second.Value.SkippedExistingCount);
        Assert.Single(notifications.Items);
    }

    [Fact]
    public async Task MarkRelated_marks_unread_and_keeps_history()
    {
        var org = PlatformOrganizationId.From(Guid.NewGuid());
        var owner = PlatformUserId.From(Guid.NewGuid());
        var now = DateTimeOffset.UtcNow;
        var notifications = new CustomerLinkCompletenessTests.InMemoryOrganizationInAppNotificationRepository();
        var relatedId = Guid.NewGuid().ToString("D");
        var note = OrganizationInAppNotification.Create(
            org,
            owner,
            "Supplier connection request",
            "Mica Store wants to connect with your business as a supplier.",
            SupplierConnectionNotificationTypes.Requested,
            now,
            relatedId);
        await notifications.AddAsync(note);

        var useCase = new MarkRelatedOrganizationNotificationsRead(
            notifications, new FakeUow(), new FixedClock(now.AddMinutes(1)));
        var result = await useCase.ExecuteAsync(
            org,
            new MarkRelatedOrganizationNotificationsReadRequest(
                SupplierConnectionNotificationTypes.Requested,
                relatedId));

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value!.MarkedCount);
        Assert.True(notifications.Items[0].IsRead);
        Assert.Single(notifications.Items);
    }

    [Fact]
    public async Task OpeningNotificationMarksRead_and_is_idempotent()
    {
        var org = PlatformOrganizationId.From(Guid.NewGuid());
        var otherOrg = PlatformOrganizationId.From(Guid.NewGuid());
        var owner = PlatformUserId.From(Guid.NewGuid());
        var otherOwner = PlatformUserId.From(Guid.NewGuid());
        var now = DateTimeOffset.UtcNow;
        var notifications = new CustomerLinkCompletenessTests.InMemoryOrganizationInAppNotificationRepository();
        var note = OrganizationInAppNotification.Create(
            org,
            owner,
            "Supplier connection accepted",
            "Paul Distribution accepted your supplier connection request.",
            SupplierConnectionNotificationTypes.Accepted,
            now,
            Guid.NewGuid().ToString("D"));
        var otherNote = OrganizationInAppNotification.Create(
            otherOrg,
            otherOwner,
            "Other",
            "Other preview",
            SupplierConnectionNotificationTypes.Accepted,
            now,
            Guid.NewGuid().ToString("D"));
        await notifications.AddAsync(note);
        await notifications.AddAsync(otherNote);

        var useCase = new MarkOrganizationInAppNotificationRead(
            notifications, new FakeUow(), new FixedClock(now.AddMinutes(1)));

        var first = await useCase.ExecuteAsync(org, owner, note.Id.Value);
        Assert.True(first.IsSuccess);
        Assert.True(first.Value!.IsRead);
        Assert.True(note.IsRead);

        var second = await useCase.ExecuteAsync(org, owner, note.Id.Value);
        Assert.True(second.IsSuccess);
        Assert.True(second.Value!.IsRead);

        var scoped = await useCase.ExecuteAsync(otherOrg, owner, note.Id.Value);
        Assert.False(scoped.IsSuccess);
        Assert.False(otherNote.IsRead);
    }

    [Fact]
    public async Task UnreadCount_excludes_read_notifications_for_organization()
    {
        var org = PlatformOrganizationId.From(Guid.NewGuid());
        var owner = PlatformUserId.From(Guid.NewGuid());
        var now = DateTimeOffset.UtcNow;
        var notifications = new CustomerLinkCompletenessTests.InMemoryOrganizationInAppNotificationRepository();
        var unread = OrganizationInAppNotification.Create(
            org, owner, "A", "a", SupplierConnectionNotificationTypes.Accepted, now, Guid.NewGuid().ToString("D"));
        var read = OrganizationInAppNotification.Create(
            org, owner, "B", "b", SupplierConnectionNotificationTypes.Declined, now, Guid.NewGuid().ToString("D"));
        read.MarkRead(now.AddMinutes(1));
        await notifications.AddAsync(unread);
        await notifications.AddAsync(read);

        var listed = await notifications.ListForRecipientInOrganizationAsync(org, owner, take: 50);
        Assert.Equal(1, listed.Count(n => !n.IsRead));
        Assert.Equal(2, listed.Count);
        Assert.Contains(listed, n => n.IsRead);
    }

    [Fact]
    public async Task Rejects_unknown_related_type_and_same_org()
    {
        var org = PlatformOrganizationId.From(Guid.NewGuid());
        var useCase = new PublishOrganizationBusinessNotification(
            new InMemoryOrganizationMembershipRepository(),
            new CustomerLinkCompletenessTests.InMemoryOrganizationInAppNotificationRepository(),
            new FakeUow(),
            new FixedClock(DateTimeOffset.UtcNow));

        var badType = await useCase.ExecuteAsync(
            org,
            new PublishOrganizationBusinessNotificationRequest(
                Guid.NewGuid(),
                "NotARealType",
                Guid.NewGuid().ToString("D"),
                "Title",
                "Preview"));
        Assert.False(badType.IsSuccess);

        var sameOrg = await useCase.ExecuteAsync(
            org,
            new PublishOrganizationBusinessNotificationRequest(
                org.Value,
                SupplierConnectionNotificationTypes.Requested,
                Guid.NewGuid().ToString("D"),
                "Title",
                "Preview"));
        Assert.False(sameOrg.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.CrossOrganizationMismatch, sameOrg.ErrorCode);
    }

    [Fact]
    public async Task Local_activity_confirmation_may_publish_to_same_organization()
    {
        var org = PlatformOrganizationId.From(Guid.NewGuid());
        var owner = PlatformUserId.From(Guid.NewGuid());
        var now = DateTimeOffset.UtcNow;
        var memberships = new InMemoryOrganizationMembershipRepository();
        await memberships.AddAsync(OrganizationMembership.Create(org, owner, OrganizationRole.OrganizationOwner, now));
        var notifications = new CustomerLinkCompletenessTests.InMemoryOrganizationInAppNotificationRepository();
        var useCase = new PublishOrganizationBusinessNotification(
            memberships, notifications, new FakeUow(), new FixedClock(now));

        var result = await useCase.ExecuteAsync(
            org,
            new PublishOrganizationBusinessNotificationRequest(
                org.Value,
                SupplierConnectionNotificationTypes.AcceptedConfirmation,
                Guid.NewGuid().ToString("D"),
                "Connection accepted",
                "Mica Store is now a connected buyer."));

        Assert.True(result.IsSuccess, $"{result.ErrorCode}: {result.ErrorMessage}");
        Assert.Equal(1, result.Value!.CreatedCount);
        Assert.Single(notifications.Items);
        Assert.Equal(SupplierConnectionNotificationTypes.AcceptedConfirmation, notifications.Items[0].RelatedType);
        Assert.Equal(org, notifications.Items[0].OrganizationId);
    }
}
