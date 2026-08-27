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
    public async Task Connected_purchase_order_types_are_publishable_and_idempotent()
    {
        var source = PlatformOrganizationId.From(Guid.NewGuid());
        var recipient = PlatformOrganizationId.From(Guid.NewGuid());
        var owner = PlatformUserId.From(Guid.NewGuid());
        var now = DateTimeOffset.UtcNow;
        var memberships = new InMemoryOrganizationMembershipRepository();
        await memberships.AddAsync(OrganizationMembership.Create(recipient, owner, OrganizationRole.OrganizationOwner, now));
        var notifications = new CustomerLinkCompletenessTests.InMemoryOrganizationInAppNotificationRepository();
        var uow = new FakeUow();
        var useCase = new PublishOrganizationBusinessNotification(memberships, notifications, uow, new FixedClock(now));
        var relatedId = Guid.NewGuid().ToString("D");

        var first = await useCase.ExecuteAsync(
            source,
            new PublishOrganizationBusinessNotificationRequest(
                recipient.Value,
                ConnectedPurchaseOrderNotificationTypes.Submitted,
                relatedId,
                "New purchase order",
                "Mica Store submitted PO PO-00123."));
        var retry = await useCase.ExecuteAsync(
            source,
            new PublishOrganizationBusinessNotificationRequest(
                recipient.Value,
                ConnectedPurchaseOrderNotificationTypes.Submitted,
                relatedId,
                "New purchase order",
                "Mica Store submitted PO PO-00123."));

        Assert.True(first.IsSuccess, first.ErrorMessage);
        Assert.True(retry.IsSuccess);
        Assert.Equal(1, first.Value!.CreatedCount);
        Assert.Equal(0, retry.Value!.CreatedCount);
        Assert.Equal(1, retry.Value.SkippedExistingCount);
        Assert.Single(notifications.Items);
        Assert.Equal(recipient, notifications.Items[0].OrganizationId);
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

    [Fact]
    public async Task Customer_order_submitted_may_publish_to_same_seller_organization()
    {
        var org = PlatformOrganizationId.From(Guid.NewGuid());
        var owner = PlatformUserId.From(Guid.NewGuid());
        var now = DateTimeOffset.UtcNow;
        var memberships = new InMemoryOrganizationMembershipRepository();
        await memberships.AddAsync(OrganizationMembership.Create(org, owner, OrganizationRole.OrganizationOwner, now));
        var notifications = new CustomerLinkCompletenessTests.InMemoryOrganizationInAppNotificationRepository();
        var useCase = new PublishOrganizationBusinessNotification(
            memberships, notifications, new FakeUow(), new FixedClock(now));
        var orderId = Guid.NewGuid().ToString("D");

        var first = await useCase.ExecuteAsync(
            org,
            new PublishOrganizationBusinessNotificationRequest(
                org.Value,
                CustomerOrderNotificationTypes.Submitted,
                orderId,
                "New customer order",
                "CO-1 · Buyer · 100.00"));
        var retry = await useCase.ExecuteAsync(
            org,
            new PublishOrganizationBusinessNotificationRequest(
                org.Value,
                CustomerOrderNotificationTypes.Submitted,
                orderId,
                "New customer order",
                "CO-1 · Buyer · 100.00"));

        Assert.True(first.IsSuccess, $"{first.ErrorCode}: {first.ErrorMessage}");
        Assert.True(retry.IsSuccess);
        Assert.Equal(1, first.Value!.CreatedCount);
        Assert.Equal(0, retry.Value!.CreatedCount);
        Assert.Equal(1, retry.Value.SkippedExistingCount);
        Assert.Single(notifications.Items);
        Assert.Equal(CustomerOrderNotificationTypes.Submitted, notifications.Items[0].RelatedType);
        Assert.Equal(orderId, notifications.Items[0].RelatedId);
    }

    [Fact]
    public async Task Personal_customer_order_status_publish_requires_active_link_and_is_idempotent()
    {
        var source = PlatformOrganizationId.From(Guid.NewGuid());
        var recipient = PlatformUserId.From(Guid.NewGuid());
        var now = DateTimeOffset.UtcNow;
        var links = new CustomerLinkCompletenessTests.InMemoryLinkedCustomerAppUserRepository();
        await links.AddAsync(LinkedCustomerAppUser.CreateFromAcceptedLink(
            source,
            BusinessCustomerId.New(),
            recipient,
            CustomerLinkRequestId.New(),
            now));
        var notifications = new CustomerLinkCompletenessTests.InMemoryPersonalInAppNotificationRepository();
        var settings = new CustomerLinkCompletenessTests.InMemoryPersonalAccountSettingsRepository();
        var useCase = new PublishPersonalBusinessNotification(
            links, notifications, settings, new FakeUow(), new FixedClock(now));
        var orderId = Guid.NewGuid().ToString("D");
        var request = new PublishPersonalBusinessNotificationRequest(
            recipient.Value,
            CustomerOrderNotificationTypes.Accepted,
            orderId,
            "Order accepted",
            "CO-9 · Accepted");

        var first = await useCase.ExecuteAsync(source, request);
        var retry = await useCase.ExecuteAsync(source, request);

        Assert.True(first.IsSuccess, first.ErrorMessage);
        Assert.True(retry.IsSuccess);
        Assert.True(first.Value!.Created);
        Assert.False(retry.Value!.Created);
        Assert.True(retry.Value.SkippedExisting);
        Assert.Single(notifications.Items);
        Assert.Equal(recipient, notifications.Items[0].RecipientUserIdentityId);
        Assert.Equal(CustomerOrderNotificationTypes.Accepted, notifications.Items[0].RelatedType);
        Assert.Equal(orderId, notifications.Items[0].RelatedId);
    }

    [Fact]
    public async Task Personal_publish_denies_unrelated_personal_user_in_same_source_org_context()
    {
        var source = PlatformOrganizationId.From(Guid.NewGuid());
        var linkedBuyer = PlatformUserId.From(Guid.NewGuid());
        var unrelated = PlatformUserId.From(Guid.NewGuid());
        var now = DateTimeOffset.UtcNow;
        var links = new CustomerLinkCompletenessTests.InMemoryLinkedCustomerAppUserRepository();
        await links.AddAsync(LinkedCustomerAppUser.CreateFromAcceptedLink(
            source,
            BusinessCustomerId.New(),
            linkedBuyer,
            CustomerLinkRequestId.New(),
            now));
        var useCase = new PublishPersonalBusinessNotification(
            links,
            new CustomerLinkCompletenessTests.InMemoryPersonalInAppNotificationRepository(),
            new CustomerLinkCompletenessTests.InMemoryPersonalAccountSettingsRepository(),
            new FakeUow(),
            new FixedClock(now));

        var result = await useCase.ExecuteAsync(
            source,
            new PublishPersonalBusinessNotificationRequest(
                unrelated.Value,
                CustomerOrderNotificationTypes.Ready,
                Guid.NewGuid().ToString("D"),
                "Ready",
                "CO-1 · Ready"));

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.LinkedCustomerAppUserNotFound, result.ErrorCode);
    }

    [Fact]
    public async Task Personal_publish_denies_recipient_linked_only_to_other_organization()
    {
        var orgA = PlatformOrganizationId.From(Guid.NewGuid());
        var orgB = PlatformOrganizationId.From(Guid.NewGuid());
        var buyerOfB = PlatformUserId.From(Guid.NewGuid());
        var now = DateTimeOffset.UtcNow;
        var links = new CustomerLinkCompletenessTests.InMemoryLinkedCustomerAppUserRepository();
        await links.AddAsync(LinkedCustomerAppUser.CreateFromAcceptedLink(
            orgB,
            BusinessCustomerId.New(),
            buyerOfB,
            CustomerLinkRequestId.New(),
            now));
        var notifications = new CustomerLinkCompletenessTests.InMemoryPersonalInAppNotificationRepository();
        var useCase = new PublishPersonalBusinessNotification(
            links,
            notifications,
            new CustomerLinkCompletenessTests.InMemoryPersonalAccountSettingsRepository(),
            new FakeUow(),
            new FixedClock(now));

        var result = await useCase.ExecuteAsync(
            orgA,
            new PublishPersonalBusinessNotificationRequest(
                buyerOfB.Value,
                CustomerOrderNotificationTypes.Delivered,
                Guid.NewGuid().ToString("D"),
                "Delivered",
                "CO-2 · Delivered"));

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.LinkedCustomerAppUserNotFound, result.ErrorCode);
        Assert.Empty(notifications.Items);
    }

    [Fact]
    public async Task Personal_publish_denies_unapproved_related_type()
    {
        var source = PlatformOrganizationId.From(Guid.NewGuid());
        var recipient = PlatformUserId.From(Guid.NewGuid());
        var now = DateTimeOffset.UtcNow;
        var links = new CustomerLinkCompletenessTests.InMemoryLinkedCustomerAppUserRepository();
        await links.AddAsync(LinkedCustomerAppUser.CreateFromAcceptedLink(
            source,
            BusinessCustomerId.New(),
            recipient,
            CustomerLinkRequestId.New(),
            now));
        var useCase = new PublishPersonalBusinessNotification(
            links,
            new CustomerLinkCompletenessTests.InMemoryPersonalInAppNotificationRepository(),
            new CustomerLinkCompletenessTests.InMemoryPersonalAccountSettingsRepository(),
            new FakeUow(),
            new FixedClock(now));

        var result = await useCase.ExecuteAsync(
            source,
            new PublishPersonalBusinessNotificationRequest(
                recipient.Value,
                "MarketingBlast",
                Guid.NewGuid().ToString("D"),
                "Promo",
                "Buy more"));

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.DomainViolation, result.ErrorCode);
    }

    [Fact]
    public async Task Personal_publish_allows_legitimate_linked_buyer()
    {
        var source = PlatformOrganizationId.From(Guid.NewGuid());
        var buyer = PlatformUserId.From(Guid.NewGuid());
        var now = DateTimeOffset.UtcNow;
        var links = new CustomerLinkCompletenessTests.InMemoryLinkedCustomerAppUserRepository();
        await links.AddAsync(LinkedCustomerAppUser.CreateFromAcceptedLink(
            source,
            BusinessCustomerId.New(),
            buyer,
            CustomerLinkRequestId.New(),
            now));
        var notifications = new CustomerLinkCompletenessTests.InMemoryPersonalInAppNotificationRepository();
        var useCase = new PublishPersonalBusinessNotification(
            links,
            notifications,
            new CustomerLinkCompletenessTests.InMemoryPersonalAccountSettingsRepository(),
            new FakeUow(),
            new FixedClock(now));
        var orderId = Guid.NewGuid().ToString("D");

        var result = await useCase.ExecuteAsync(
            source,
            new PublishPersonalBusinessNotificationRequest(
                buyer.Value,
                CustomerOrderNotificationTypes.OutForDelivery,
                orderId,
                "Out for delivery",
                "CO-3 · Out for delivery"));

        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.True(result.Value!.Created);
        Assert.Single(notifications.Items);
        Assert.Equal(orderId, notifications.Items[0].RelatedId);
        Assert.Equal(CustomerOrderNotificationTypes.OutForDelivery, notifications.Items[0].RelatedType);
    }
}
