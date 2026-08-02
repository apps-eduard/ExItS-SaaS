using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Personal;

namespace ExItS.Platform.UnitTests.Personal;

public sealed class PersonalUtangDomainTests
{
    [Fact]
    public void Loan_payment_sequence_reconciles_balance()
    {
        var user = PlatformUserId.New();
        var contact = PersonalContact.Create(user, "Friend", null, null, DateTimeOffset.UtcNow);
        var relationship = PersonalDebtRelationship.Create(
            user,
            creditorUserIdentityId: user,
            creditorContactId: null,
            debtorUserIdentityId: null,
            debtorContactId: contact.Id,
            "PHP",
            DateTimeOffset.UtcNow);

        var loan = relationship.RecordEntry(
            user,
            PersonalUtangEntryType.Loan,
            500m,
            500m,
            DateTimeOffset.UtcNow,
            expectedVersion: null);
        Assert.Equal(500m, loan.BalanceAfter);

        var payment = relationship.RecordEntry(
            user,
            PersonalUtangEntryType.Payment,
            200m,
            -200m,
            DateTimeOffset.UtcNow,
            expectedVersion: relationship.Version);
        Assert.Equal(300m, payment.BalanceAfter);
        Assert.Equal(300m, relationship.CurrentBalance);
    }

    [Fact]
    public void Stale_version_throws_concurrency_domain_error()
    {
        var user = PlatformUserId.New();
        var contact = PersonalContact.Create(user, "Friend", null, null, DateTimeOffset.UtcNow);
        var relationship = PersonalDebtRelationship.Create(
            user,
            user,
            null,
            null,
            contact.Id,
            "PHP",
            DateTimeOffset.UtcNow);

        var ex = Assert.Throws<DomainException>(() =>
            relationship.RecordEntry(
                user,
                PersonalUtangEntryType.Payment,
                10m,
                -10m,
                DateTimeOffset.UtcNow,
                expectedVersion: 99));

        Assert.Equal(DomainErrorCodes.PersonalUtangConcurrencyConflict, ex.ErrorCode);
    }

    [Fact]
    public void Personal_account_settings_update_increments_version()
    {
        var user = PlatformUserId.New();
        var settings = PersonalAccountSettings.CreateDefaults(user, DateTimeOffset.UtcNow);
        settings.UpdateNotificationPreferences(false, false, true, true, DateTimeOffset.UtcNow, expectedVersion: 1);
        Assert.Equal(2, settings.Version);
        Assert.False(settings.EmailNotificationsEnabled);
    }

    [Fact]
    public void Invitation_accept_links_contact_and_authorizes_participant()
    {
        var owner = PlatformUserId.New();
        var invitee = PlatformUserId.New();
        var now = DateTimeOffset.UtcNow;
        var contact = PersonalContact.Create(owner, "Friend", null, "friend@example.com", now);
        var relationship = PersonalDebtRelationship.Create(
            owner,
            creditorUserIdentityId: owner,
            creditorContactId: null,
            debtorUserIdentityId: null,
            debtorContactId: contact.Id,
            "PHP",
            now);

        var (invitation, token) = PersonalUtangInvitation.Create(
            relationship.Id,
            contact.Id,
            owner,
            now,
            inviteTargetEmail: "friend@example.com");

        Assert.Equal(PersonalUtangInvitationStatus.Pending, invitation.Status);
        Assert.Equal(PersonalUtangInvitation.HashToken(token), invitation.TokenHash);

        invitation.Accept(invitee, "friend@example.com", now);
        contact.LinkUser(invitee, now);
        relationship.AuthorizeLinkedParticipant(contact.Id, invitee, now);

        Assert.Equal(PersonalUtangInvitationStatus.Accepted, invitation.Status);
        Assert.Equal(invitee, contact.LinkedUserIdentityId);
        Assert.Equal(invitee, relationship.DebtorUserIdentityId);
        Assert.Null(relationship.DebtorContactId);
        Assert.True(relationship.CanBeViewedBy(invitee));
    }

    [Fact]
    public void Invitation_does_not_silently_match_without_token_acceptance()
    {
        var owner = PlatformUserId.New();
        var stranger = PlatformUserId.New();
        var now = DateTimeOffset.UtcNow;
        var contact = PersonalContact.Create(owner, "Same Name", "+639170000099", "same@example.com", now);

        Assert.Null(contact.LinkedUserIdentityId);
        Assert.Throws<DomainException>(() => contact.LinkUser(owner, now));
        _ = stranger;
    }

    [Fact]
    public void Reminder_rate_limit_blocks_repeated_deliveries()
    {
        var now = DateTimeOffset.UtcNow;
        var ex = Assert.Throws<DomainException>(() =>
            PersonalReminder.EnsureDeliveryAllowed(
                deliveriesInLast24Hours: 3,
                lastDeliveryAtUtc: now.AddMinutes(-30),
                utcNow: now));
        Assert.Equal(DomainErrorCodes.PersonalReminderRateLimited, ex.ErrorCode);

        var intervalEx = Assert.Throws<DomainException>(() =>
            PersonalReminder.EnsureDeliveryAllowed(
                deliveriesInLast24Hours: 1,
                lastDeliveryAtUtc: now.AddMinutes(-10),
                utcNow: now));
        Assert.Equal(DomainErrorCodes.PersonalReminderRateLimited, intervalEx.ErrorCode);
    }

    [Fact]
    public void Reminder_preview_minimizes_sensitive_values()
    {
        var preview = PersonalReminder.BuildMinimizedPreview("Payment due soon");
        Assert.DoesNotContain("₱", preview, StringComparison.Ordinal);
        Assert.DoesNotContain("1000", preview, StringComparison.Ordinal);
        Assert.Equal("Payment due soon", preview);
    }

    [Fact]
    public void Invitation_decline_and_revoke_lifecycle()
    {
        var owner = PlatformUserId.New();
        var contactId = PersonalContactId.New();
        var relationshipId = PersonalDebtRelationshipId.New();
        var now = DateTimeOffset.UtcNow;
        var (invitation, _) = PersonalUtangInvitation.Create(relationshipId, contactId, owner, now);
        invitation.Decline(now);
        Assert.Equal(PersonalUtangInvitationStatus.Declined, invitation.Status);

        var (pending, _) = PersonalUtangInvitation.Create(relationshipId, contactId, owner, now);
        pending.Revoke(now);
        Assert.Equal(PersonalUtangInvitationStatus.Revoked, pending.Status);
    }
}
