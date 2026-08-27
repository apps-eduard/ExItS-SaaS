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

        relationship.CloseAsSettled(UtcNow(), expectedVersion: null);
        Assert.Equal(PersonalDebtRelationshipStatus.Closed, relationship.Status);
    }

    [Fact]
    public void Linked_settle_stays_pending_until_confirm_then_closes()
    {
        var (relationship, creditor, debtor) = CreateSharedRelationshipWithBalance(1000m);

        var settlement = relationship.RecordSettlementPayment(debtor, UtcNow(), relationship.Version);
        Assert.Equal(PersonalUtangEntryStatus.Pending, settlement.Status);
        Assert.Equal(1000m, relationship.CurrentBalance);
        Assert.Equal(PersonalDebtRelationshipStatus.Active, relationship.Status);

        relationship.ConfirmEntry(settlement, creditor, UtcNow(), relationship.Version);
        Assert.Equal(PersonalUtangEntryStatus.Confirmed, settlement.Status);
        Assert.Equal(0m, relationship.CurrentBalance);
        Assert.Equal(PersonalDebtRelationshipStatus.Closed, relationship.Status);
    }

    [Fact]
    public void Proposer_cannot_self_confirm_settlement()
    {
        var (relationship, creditor, debtor) = CreateSharedRelationshipWithBalance(200m);
        var settlement = relationship.RecordSettlementPayment(debtor, UtcNow(), relationship.Version);

        var ex = Assert.Throws<DomainException>(() =>
            relationship.ConfirmEntry(settlement, debtor, UtcNow(), relationship.Version));
        Assert.Equal(DomainErrorCodes.PersonalUtangUnauthorized, ex.ErrorCode);
        Assert.Equal(PersonalDebtRelationshipStatus.Active, relationship.Status);
        Assert.Equal(200m, relationship.CurrentBalance);
    }

    [Fact]
    public void Dispute_settlement_leaves_balance_and_status_unchanged()
    {
        var (relationship, creditor, debtor) = CreateSharedRelationshipWithBalance(300m);
        var settlement = relationship.RecordSettlementPayment(debtor, UtcNow(), relationship.Version);

        relationship.DisputeEntry(settlement, creditor, UtcNow(), relationship.Version, "Not settled.");
        Assert.Equal(PersonalUtangEntryStatus.Disputed, settlement.Status);
        Assert.Equal(300m, relationship.CurrentBalance);
        Assert.Equal(PersonalDebtRelationshipStatus.Active, relationship.Status);
    }

    [Fact]
    public void Close_blocked_when_unresolved_pending_flag_set()
    {
        var (relationship, owner, _) = CreatePrivateRelationshipWithBalance(100m);
        relationship.RecordEntry(owner, PersonalUtangEntryType.Payment, 100m, -100m, UtcNow(), relationship.Version);

        var closeEx = Assert.Throws<DomainException>(() =>
            relationship.CloseAsSettled(UtcNow(), expectedVersion: null, hasUnresolvedPending: true));
        Assert.Equal(DomainErrorCodes.PersonalUtangPendingBlocksSettlement, closeEx.ErrorCode);
    }

    [Fact]
    public void Stale_settlement_confirm_throws_settlement_stale()
    {
        var (relationship, creditor, debtor) = CreateSharedRelationshipWithBalance(500m);
        var settlement = relationship.RecordSettlementPayment(debtor, UtcNow(), relationship.Version);
        Assert.Equal(500m, settlement.SettlementBalanceSnapshot);

        // Confirmed partial payment changes balance after settlement was proposed.
        var partial = relationship.RecordEntry(
            debtor, PersonalUtangEntryType.Payment, 100m, -100m, UtcNow(), relationship.Version);
        relationship.ConfirmEntry(partial, creditor, UtcNow(), relationship.Version);
        Assert.Equal(400m, relationship.CurrentBalance);

        var ex = Assert.Throws<DomainException>(() =>
            relationship.ConfirmEntry(settlement, creditor, UtcNow(), relationship.Version));
        Assert.Equal(DomainErrorCodes.PersonalUtangSettlementStale, ex.ErrorCode);
        Assert.Equal(400m, relationship.CurrentBalance);
        Assert.Equal(PersonalDebtRelationshipStatus.Active, relationship.Status);
    }

    [Fact]
    public void Close_when_balance_zero_succeeds()
    {
        var (relationship, owner, _) = CreatePrivateRelationshipWithBalance(100m);
        relationship.RecordEntry(owner, PersonalUtangEntryType.Payment, 100m, -100m, UtcNow(), relationship.Version);
        Assert.Equal(0m, relationship.CurrentBalance);

        relationship.CloseAsSettled(UtcNow(), relationship.Version);
        Assert.Equal(PersonalDebtRelationshipStatus.Closed, relationship.Status);
    }

    [Fact]
    public void Close_blocked_when_balance_greater_than_zero()
    {
        var (relationship, _, _) = CreatePrivateRelationshipWithBalance(75m);
        var ex = Assert.Throws<DomainException>(() =>
            relationship.CloseAsSettled(UtcNow(), relationship.Version));
        Assert.Equal(DomainErrorCodes.PersonalUtangCloseInvalid, ex.ErrorCode);
    }

    [Fact]
    public void Closed_blocks_new_loan_or_payment()
    {
        var (relationship, owner, _) = CreatePrivateRelationshipWithBalance(50m);
        relationship.RecordEntry(owner, PersonalUtangEntryType.Payment, 50m, -50m, UtcNow(), relationship.Version);
        relationship.CloseAsSettled(UtcNow(), relationship.Version);

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
        relationship.CloseAsSettled(UtcNow(), relationship.Version);
        var version = relationship.Version;

        relationship.CloseAsSettled(UtcNow(), expectedVersion: null);
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

    [Fact]
    public void Confirm_settlement_idempotent_after_close()
    {
        var (relationship, creditor, debtor) = CreateSharedRelationshipWithBalance(80m);
        var settlement = relationship.RecordSettlementPayment(debtor, UtcNow(), relationship.Version);
        relationship.ConfirmEntry(settlement, creditor, UtcNow(), relationship.Version);
        Assert.Equal(PersonalDebtRelationshipStatus.Closed, relationship.Status);

        relationship.ConfirmEntry(settlement, creditor, UtcNow(), expectedVersion: null);
        Assert.Equal(0m, relationship.CurrentBalance);
        Assert.Equal(PersonalDebtRelationshipStatus.Closed, relationship.Status);
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
        var loan = relationship.RecordEntry(
            creditor, PersonalUtangEntryType.Loan, balance, balance, UtcNow(), null, notes: "Seed");
        relationship.ConfirmEntry(loan, debtor, UtcNow(), relationship.Version);
        return (relationship, creditor, debtor);
    }
}
