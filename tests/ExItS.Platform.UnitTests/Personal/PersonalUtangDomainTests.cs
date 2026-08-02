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
}
