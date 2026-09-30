using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Personal;

namespace ExItS.Platform.UnitTests.Personal;

public sealed class PersonalUtangSettlementDomainTests
{
    [Fact]
    public void Private_settle_confirms_payment_zeros_balance_and_closes()
    {
        var (relationship, owner, _) = CreatePrivateRelationshipWithBalance(500m);

        var settlement = relationship.RecordSettlementPayment(owner, UtcNow(), relationship.Version);
        Assert.Equal(PersonalUtangEntryType.Payment, settlement.EntryType);
        Assert.True(settlement.IsSettlement);
        Assert.Equal(PersonalUtangEntryStatus.Confirmed, settlement.Status);
        Assert.Equal(500m, settlement.Amount);
        Assert.Equal(500m, settlement.SettlementBalanceSnapshot);
        Assert.Equal(0m, relationship.CurrentBalance);

        relationship.CloseAsSettled(owner, UtcNow(), expectedVersion: null);
        Assert.Equal(PersonalDebtRelationshipStatus.Closed, relationship.Status);
    }

    [Fact]
    public void Shared_owner_settle_confirms_immediately_then_closes()
    {
        var (relationship, creditor, debtor) = CreateSharedRelationshipWithBalance(1000m);

        var nonOwnerEx = Assert.Throws<DomainException>(() =>
            relationship.RecordSettlementPayment(debtor, UtcNow(), relationship.Version));
        Assert.Equal(DomainErrorCodes.PersonalUtangNotLedgerOwner, nonOwnerEx.ErrorCode);

        var settlement = relationship.RecordSettlementPayment(creditor, UtcNow(), relationship.Version);
        Assert.Equal(PersonalUtangEntryStatus.Confirmed, settlement.Status);
        Assert.Equal(0m, relationship.CurrentBalance);

        relationship.CloseAsSettled(creditor, UtcNow(), expectedVersion: null);
        Assert.Equal(PersonalDebtRelationshipStatus.Closed, relationship.Status);
    }

    [Fact]
    public void Close_blocked_when_unresolved_pending_flag_set()
    {
        var (relationship, owner, _) = CreatePrivateRelationshipWithBalance(100m);
        relationship.RecordEntry(owner, PersonalUtangEntryType.Payment, 100m, -100m, UtcNow(), relationship.Version);

        var closeEx = Assert.Throws<DomainException>(() =>
            relationship.CloseAsSettled(owner, UtcNow(), expectedVersion: null, hasUnresolvedPending: true));
        Assert.Equal(DomainErrorCodes.PersonalUtangPendingBlocksSettlement, closeEx.ErrorCode);
    }

    [Fact]
    public void Close_when_balance_zero_succeeds()
    {
        var (relationship, owner, _) = CreatePrivateRelationshipWithBalance(100m);
        relationship.RecordEntry(owner, PersonalUtangEntryType.Payment, 100m, -100m, UtcNow(), relationship.Version);
        Assert.Equal(0m, relationship.CurrentBalance);

        relationship.CloseAsSettled(owner, UtcNow(), relationship.Version);
        Assert.Equal(PersonalDebtRelationshipStatus.Closed, relationship.Status);
    }

    [Fact]
    public void Close_blocked_when_balance_greater_than_zero()
    {
        var (relationship, owner, _) = CreatePrivateRelationshipWithBalance(75m);
        var ex = Assert.Throws<DomainException>(() =>
            relationship.CloseAsSettled(owner, UtcNow(), relationship.Version));
        Assert.Equal(DomainErrorCodes.PersonalUtangCloseInvalid, ex.ErrorCode);
    }

    [Fact]
    public void Non_owner_cannot_close()
    {
        var (relationship, creditor, debtor) = CreateSharedRelationshipWithBalance(50m);
        relationship.RecordEntry(creditor, PersonalUtangEntryType.Payment, 50m, -50m, UtcNow(), relationship.Version);

        var ex = Assert.Throws<DomainException>(() =>
            relationship.CloseAsSettled(debtor, UtcNow(), relationship.Version));
        Assert.Equal(DomainErrorCodes.PersonalUtangNotLedgerOwner, ex.ErrorCode);
    }

    [Fact]
    public void Closed_blocks_new_loan_or_payment()
    {
        var (relationship, owner, _) = CreatePrivateRelationshipWithBalance(50m);
        relationship.RecordEntry(owner, PersonalUtangEntryType.Payment, 50m, -50m, UtcNow(), relationship.Version);
        relationship.CloseAsSettled(owner, UtcNow(), relationship.Version);

        var loanEx = Assert.Throws<DomainException>(() =>
            relationship.RecordEntry(
                owner, PersonalUtangEntryType.Loan, 10m, 10m, UtcNow(), relationship.Version, notes: "Nope"));
        Assert.Equal(DomainErrorCodes.InvalidPersonalDebtRelationship, loanEx.ErrorCode);

        var payEx = Assert.Throws<DomainException>(() =>
            relationship.RecordEntry(
                owner, PersonalUtangEntryType.Payment, 10m, -10m, UtcNow(), relationship.Version));
        Assert.Equal(DomainErrorCodes.InvalidPersonalDebtRelationship, payEx.ErrorCode);
    }

    [Fact]
    public void Close_as_settled_is_idempotent_when_already_closed()
    {
        var (relationship, owner, _) = CreatePrivateRelationshipWithBalance(20m);
        relationship.RecordEntry(owner, PersonalUtangEntryType.Payment, 20m, -20m, UtcNow(), relationship.Version);
        relationship.CloseAsSettled(owner, UtcNow(), relationship.Version);
        var version = relationship.Version;

        relationship.CloseAsSettled(owner, UtcNow(), expectedVersion: null);
        Assert.Equal(PersonalDebtRelationshipStatus.Closed, relationship.Status);
        Assert.Equal(version, relationship.Version);
    }

    [Fact]
    public void Archived_settle_is_denied()
    {
        var (relationship, owner, _) = CreatePrivateRelationshipWithBalance(40m);
        relationship.Archive(UtcNow(), relationship.Version);

        var ex = Assert.Throws<DomainException>(() =>
            relationship.RecordSettlementPayment(owner, UtcNow(), relationship.Version));
        Assert.Equal(DomainErrorCodes.PersonalUtangSettlementInvalid, ex.ErrorCode);
    }

    [Fact]
    public void Zero_balance_settle_suggests_close()
    {
        var (relationship, owner, _) = CreatePrivateRelationshipWithBalance(10m);
        relationship.RecordEntry(owner, PersonalUtangEntryType.Payment, 10m, -10m, UtcNow(), relationship.Version);

        var ex = Assert.Throws<DomainException>(() =>
            relationship.RecordSettlementPayment(owner, UtcNow(), relationship.Version));
        Assert.Equal(DomainErrorCodes.PersonalUtangSettlementInvalid, ex.ErrorCode);
        Assert.Contains("close", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static DateTimeOffset UtcNow() => DateTimeOffset.UtcNow;

    private static (PersonalDebtRelationship Relationship, PlatformUserId Owner, PersonalContact Contact)
        CreatePrivateRelationshipWithBalance(decimal balance)
    {
        var owner = PlatformUserId.New();
        var contact = PersonalContact.Create(owner, "Friend", null, null, UtcNow());
        var relationship = PersonalDebtRelationship.Create(
            owner, owner, null, null, contact.Id, "PHP", UtcNow());
        relationship.RecordEntry(
            owner, PersonalUtangEntryType.Loan, balance, balance, UtcNow(), null, notes: "Seed");
        return (relationship, owner, contact);
    }

    private static (PersonalDebtRelationship Relationship, PlatformUserId Creditor, PlatformUserId Debtor)
        CreateSharedRelationshipWithBalance(decimal balance)
    {
        var creditor = PlatformUserId.New();
        var debtor = PlatformUserId.New();
        var relationship = PersonalDebtRelationship.Create(
            creditor, creditor, null, debtor, null, "PHP", UtcNow());
        relationship.RecordEntry(
            creditor, PersonalUtangEntryType.Loan, balance, balance, UtcNow(), null, notes: "Seed");
        return (relationship, creditor, debtor);
    }
}
