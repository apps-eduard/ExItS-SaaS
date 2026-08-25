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

        var amountPreview = PersonalReminder.BuildMinimizedPreview("Please pay ₱9,999.50 today");
        Assert.DoesNotContain("₱", amountPreview, StringComparison.Ordinal);
        Assert.DoesNotContain("9,999", amountPreview, StringComparison.Ordinal);
        Assert.DoesNotContain("9999", amountPreview, StringComparison.Ordinal);
        Assert.Contains("Please pay", amountPreview, StringComparison.Ordinal);
    }

    [Fact]
    public void Invitation_resend_is_rate_limited()
    {
        var owner = PlatformUserId.New();
        var contactId = PersonalContactId.New();
        var relationshipId = PersonalDebtRelationshipId.New();
        var now = DateTimeOffset.UtcNow;
        var (invitation, _) = PersonalUtangInvitation.Create(relationshipId, contactId, owner, now);

        var ex = Assert.Throws<DomainException>(() => invitation.Resend(now.AddMinutes(10)));
        Assert.Equal(DomainErrorCodes.PersonalUtangInvitationRateLimited, ex.ErrorCode);

        var token = invitation.Resend(now.AddHours(1));
        Assert.False(string.IsNullOrWhiteSpace(token));
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

    [Fact]
    public void Contact_email_is_normalized_uppercase()
    {
        var user = PlatformUserId.New();
        var contact = PersonalContact.Create(user, "Friend", null, "Friend@Example.com", DateTimeOffset.UtcNow);
        Assert.Equal("FRIEND@EXAMPLE.COM", contact.Email);
        Assert.Equal("FRIEND@EXAMPLE.COM", PersonalContact.NormalizeOptionalEmail(" friend@example.com "));
        Assert.Null(PersonalContact.NormalizeOptionalEmail("  "));
    }

    [Fact]
    public void Private_loan_and_payment_affect_balance_immediately_as_confirmed()
    {
        var (relationship, owner, _) = CreatePrivateRelationship();
        var loan = relationship.RecordEntry(owner, PersonalUtangEntryType.Loan, 1000m, 1000m, UtcNow(), null);
        Assert.Equal(PersonalUtangEntryStatus.Confirmed, loan.Status);
        Assert.Equal(1000m, relationship.CurrentBalance);

        var payment = relationship.RecordEntry(
            owner, PersonalUtangEntryType.Payment, 300m, -300m, UtcNow(), relationship.Version);
        Assert.Equal(PersonalUtangEntryStatus.Confirmed, payment.Status);
        Assert.Equal(700m, relationship.CurrentBalance);
    }

    [Fact]
    public void Private_adjustment_applies_immediately()
    {
        var (relationship, owner, _) = CreatePrivateRelationship();
        relationship.RecordEntry(owner, PersonalUtangEntryType.Loan, 500m, 500m, UtcNow(), null);
        var adjustment = relationship.RecordEntry(
            owner,
            PersonalUtangEntryType.Adjustment,
            amount: 50m,
            signedDelta: -50m,
            UtcNow(),
            relationship.Version);
        Assert.Equal(PersonalUtangEntryStatus.Confirmed, adjustment.Status);
        Assert.Equal(450m, relationship.CurrentBalance);
    }

    [Fact]
    public void Linked_loan_starts_pending_without_balance_effect_until_counterparty_confirms()
    {
        var (relationship, creditor, debtor) = CreateSharedRelationship();
        var loan = relationship.RecordEntry(creditor, PersonalUtangEntryType.Loan, 1000m, 1000m, UtcNow(), null);
        Assert.Equal(PersonalUtangEntryStatus.Pending, loan.Status);
        Assert.Equal(0m, relationship.CurrentBalance);
        Assert.Equal(0m, loan.BalanceAfter);

        relationship.ConfirmEntry(loan, debtor, UtcNow(), relationship.Version);
        Assert.Equal(PersonalUtangEntryStatus.Confirmed, loan.Status);
        Assert.Equal(1000m, relationship.CurrentBalance);
        Assert.Equal(1000m, loan.BalanceAfter);
    }

    [Fact]
    public void Proposer_cannot_self_confirm_or_dispute()
    {
        var (relationship, creditor, _) = CreateSharedRelationship();
        var loan = relationship.RecordEntry(creditor, PersonalUtangEntryType.Loan, 100m, 100m, UtcNow(), null);

        var confirmEx = Assert.Throws<DomainException>(() =>
            relationship.ConfirmEntry(loan, creditor, UtcNow(), relationship.Version));
        Assert.Equal(DomainErrorCodes.PersonalUtangUnauthorized, confirmEx.ErrorCode);

        var disputeEx = Assert.Throws<DomainException>(() =>
            relationship.DisputeEntry(loan, creditor, UtcNow(), relationship.Version));
        Assert.Equal(DomainErrorCodes.PersonalUtangUnauthorized, disputeEx.ErrorCode);
    }

    [Fact]
    public void Counterparty_dispute_leaves_balance_unchanged()
    {
        var (relationship, creditor, debtor) = CreateSharedRelationship();
        var established = relationship.RecordEntry(creditor, PersonalUtangEntryType.Loan, 700m, 700m, UtcNow(), null);
        relationship.ConfirmEntry(established, debtor, UtcNow(), relationship.Version);
        Assert.Equal(700m, relationship.CurrentBalance);

        var disputedLoan = relationship.RecordEntry(
            creditor, PersonalUtangEntryType.Loan, 500m, 500m, UtcNow(), relationship.Version);
        relationship.DisputeEntry(disputedLoan, debtor, UtcNow(), relationship.Version, "Amount is incorrect.");
        Assert.Equal(PersonalUtangEntryStatus.Disputed, disputedLoan.Status);
        Assert.Equal(700m, relationship.CurrentBalance);
    }

    [Fact]
    public void Linked_payment_requires_confirmation_before_reducing_balance()
    {
        var (relationship, creditor, debtor) = CreateSharedRelationship();
        var loan = relationship.RecordEntry(creditor, PersonalUtangEntryType.Loan, 1000m, 1000m, UtcNow(), null);
        relationship.ConfirmEntry(loan, debtor, UtcNow(), relationship.Version);

        var payment = relationship.RecordEntry(
            debtor, PersonalUtangEntryType.Payment, 300m, -300m, UtcNow(), relationship.Version);
        Assert.Equal(PersonalUtangEntryStatus.Pending, payment.Status);
        Assert.Equal(1000m, relationship.CurrentBalance);

        relationship.ConfirmEntry(payment, creditor, UtcNow(), relationship.Version);
        Assert.Equal(700m, relationship.CurrentBalance);
    }

    [Fact]
    public void Linked_adjustment_requires_confirmation()
    {
        var (relationship, creditor, debtor) = CreateSharedRelationship();
        var loan = relationship.RecordEntry(creditor, PersonalUtangEntryType.Loan, 1000m, 1000m, UtcNow(), null);
        relationship.ConfirmEntry(loan, debtor, UtcNow(), relationship.Version);

        var adjustment = relationship.RecordEntry(
            creditor,
            PersonalUtangEntryType.Adjustment,
            amount: 100m,
            signedDelta: -100m,
            UtcNow(),
            relationship.Version);
        Assert.Equal(PersonalUtangEntryStatus.Pending, adjustment.Status);
        Assert.Equal(1000m, relationship.CurrentBalance);

        relationship.ConfirmEntry(adjustment, debtor, UtcNow(), relationship.Version);
        Assert.Equal(900m, relationship.CurrentBalance);
    }

    [Fact]
    public void Confirm_is_idempotent_and_does_not_double_apply_balance()
    {
        var (relationship, creditor, debtor) = CreateSharedRelationship();
        var loan = relationship.RecordEntry(creditor, PersonalUtangEntryType.Loan, 1000m, 1000m, UtcNow(), null);
        relationship.ConfirmEntry(loan, debtor, UtcNow(), relationship.Version);
        Assert.Equal(1000m, relationship.CurrentBalance);

        relationship.ConfirmEntry(loan, debtor, UtcNow(), expectedVersion: null);
        Assert.Equal(1000m, relationship.CurrentBalance);
        Assert.Equal(PersonalUtangEntryStatus.Confirmed, loan.Status);
    }

    [Fact]
    public void Concurrent_confirm_then_dispute_leaves_confirmed_winner()
    {
        var (relationship, creditor, debtor) = CreateSharedRelationship();
        var loan = relationship.RecordEntry(creditor, PersonalUtangEntryType.Loan, 400m, 400m, UtcNow(), null);
        var versionAtPending = relationship.Version;
        relationship.ConfirmEntry(loan, debtor, UtcNow(), versionAtPending);

        var disputeEx = Assert.Throws<DomainException>(() =>
            relationship.DisputeEntry(loan, debtor, UtcNow(), versionAtPending));
        Assert.Equal(DomainErrorCodes.PersonalUtangConcurrencyConflict, disputeEx.ErrorCode);
        Assert.Equal(PersonalUtangEntryStatus.Confirmed, loan.Status);
        Assert.Equal(400m, relationship.CurrentBalance);
    }

    [Fact]
    public void Stale_version_on_confirm_throws_concurrency_conflict()
    {
        var (relationship, creditor, debtor) = CreateSharedRelationship();
        var loan = relationship.RecordEntry(creditor, PersonalUtangEntryType.Loan, 100m, 100m, UtcNow(), null);
        var ex = Assert.Throws<DomainException>(() =>
            relationship.ConfirmEntry(loan, debtor, UtcNow(), expectedVersion: 1));
        Assert.Equal(DomainErrorCodes.PersonalUtangConcurrencyConflict, ex.ErrorCode);
    }

    [Fact]
    public void Unrelated_user_cannot_confirm_or_dispute()
    {
        var (relationship, creditor, _) = CreateSharedRelationship();
        var stranger = PlatformUserId.New();
        var loan = relationship.RecordEntry(creditor, PersonalUtangEntryType.Loan, 50m, 50m, UtcNow(), null);

        var confirmEx = Assert.Throws<DomainException>(() =>
            relationship.ConfirmEntry(loan, stranger, UtcNow(), relationship.Version));
        Assert.Equal(DomainErrorCodes.PersonalUtangUnauthorized, confirmEx.ErrorCode);

        var disputeEx = Assert.Throws<DomainException>(() =>
            relationship.DisputeEntry(loan, stranger, UtcNow(), relationship.Version));
        Assert.Equal(DomainErrorCodes.PersonalUtangUnauthorized, disputeEx.ErrorCode);
    }

    [Fact]
    public void Invite_link_preserves_relationship_history_and_balance_then_new_entries_pending()
    {
        var owner = PlatformUserId.New();
        var invitee = PlatformUserId.New();
        var now = UtcNow();
        var contact = PersonalContact.Create(owner, "Juan", null, "juan@example.com", now);
        var relationship = PersonalDebtRelationship.Create(
            owner, owner, null, null, contact.Id, "PHP", now);

        var loan = relationship.RecordEntry(owner, PersonalUtangEntryType.Loan, 2000m, 2000m, now, null);
        var payment = relationship.RecordEntry(
            owner, PersonalUtangEntryType.Payment, 500m, -500m, now, relationship.Version);
        Assert.Equal(1500m, relationship.CurrentBalance);
        Assert.Equal(PersonalUtangEntryStatus.Confirmed, loan.Status);
        Assert.Equal(PersonalUtangEntryStatus.Confirmed, payment.Status);

        var relationshipId = relationship.Id;
        relationship.AuthorizeLinkedParticipant(contact.Id, invitee, now);
        Assert.Equal(relationshipId, relationship.Id);
        Assert.Equal(1500m, relationship.CurrentBalance);
        Assert.True(relationship.IsSharedLinked);

        var postLinkLoan = relationship.RecordEntry(
            owner, PersonalUtangEntryType.Loan, 400m, 400m, now, relationship.Version);
        Assert.Equal(PersonalUtangEntryStatus.Pending, postLinkLoan.Status);
        Assert.Equal(1500m, relationship.CurrentBalance);

        relationship.ConfirmEntry(postLinkLoan, invitee, now, relationship.Version);
        Assert.Equal(1900m, relationship.CurrentBalance);
    }

    [Fact]
    public void Legacy_rehydrated_entries_default_to_confirmed()
    {
        var entry = PersonalUtangEntry.Rehydrate(
            PersonalUtangEntryId.New(),
            PersonalDebtRelationshipId.New(),
            PersonalUtangEntryType.Loan,
            100m,
            100m,
            100m,
            notes: null,
            dueDateUtc: null,
            PlatformUserId.New(),
            UtcNow());
        Assert.Equal(PersonalUtangEntryStatus.Confirmed, entry.Status);
    }

    [Fact]
    public void Proposer_may_cancel_pending_entry()
    {
        var (relationship, creditor, debtor) = CreateSharedRelationship();
        var loan = relationship.RecordEntry(creditor, PersonalUtangEntryType.Loan, 200m, 200m, UtcNow(), null);
        relationship.CancelPendingEntry(loan, creditor, UtcNow(), relationship.Version);
        Assert.Equal(PersonalUtangEntryStatus.Cancelled, loan.Status);
        Assert.Equal(0m, relationship.CurrentBalance);

        var cancelByCounterparty = relationship.RecordEntry(
            creditor, PersonalUtangEntryType.Loan, 50m, 50m, UtcNow(), relationship.Version);
        var ex = Assert.Throws<DomainException>(() =>
            relationship.CancelPendingEntry(cancelByCounterparty, debtor, UtcNow(), relationship.Version));
        Assert.Equal(DomainErrorCodes.PersonalUtangUnauthorized, ex.ErrorCode);
    }

    private static DateTimeOffset UtcNow() => DateTimeOffset.UtcNow;

    private static (PersonalDebtRelationship Relationship, PlatformUserId Owner, PersonalContact Contact)
        CreatePrivateRelationship()
    {
        var owner = PlatformUserId.New();
        var contact = PersonalContact.Create(owner, "Friend", null, null, UtcNow());
        var relationship = PersonalDebtRelationship.Create(
            owner, owner, null, null, contact.Id, "PHP", UtcNow());
        return (relationship, owner, contact);
    }

    private static (PersonalDebtRelationship Relationship, PlatformUserId Creditor, PlatformUserId Debtor)
        CreateSharedRelationship()
    {
        var creditor = PlatformUserId.New();
        var debtor = PlatformUserId.New();
        var relationship = PersonalDebtRelationship.Create(
            creditor, creditor, null, debtor, null, "PHP", UtcNow());
        Assert.True(relationship.IsSharedLinked);
        return (relationship, creditor, debtor);
    }
}
