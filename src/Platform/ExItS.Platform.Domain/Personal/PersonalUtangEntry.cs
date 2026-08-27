using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;

namespace ExItS.Platform.Domain.Personal;

/// <summary>Append-oriented financial activity for a personal debt relationship.</summary>
public sealed class PersonalUtangEntry
{
    public PersonalUtangEntryId Id { get; }
    public PersonalDebtRelationshipId RelationshipId { get; }
    public PersonalUtangEntryType EntryType { get; }
    public decimal Amount { get; }
    public decimal SignedDelta { get; }
    public decimal BalanceAfter { get; private set; }
    public string? Notes { get; }
    public DateTimeOffset? DueDateUtc { get; }
    public PlatformUserId CreatedByUserIdentityId { get; }
    public DateTimeOffset CreatedAtUtc { get; }
    public PersonalUtangEntryStatus Status { get; private set; }
    public PlatformUserId? ResolvedByUserIdentityId { get; private set; }
    public DateTimeOffset? ResolvedAtUtc { get; private set; }
    public string? DisputeReason { get; private set; }
    public PersonalUtangEntryIntent Intent { get; }
    public decimal? SettlementBalanceSnapshot { get; }

    public bool IsSettlement => Intent == PersonalUtangEntryIntent.Settlement;

    /// <summary>Max stored length for Purpose / Note (and optional payment notes).</summary>
    public const int NotesMaxLength = 512;

    private PersonalUtangEntry(
        PersonalUtangEntryId id,
        PersonalDebtRelationshipId relationshipId,
        PersonalUtangEntryType entryType,
        decimal amount,
        decimal signedDelta,
        decimal balanceAfter,
        string? notes,
        DateTimeOffset? dueDateUtc,
        PlatformUserId createdByUserIdentityId,
        DateTimeOffset createdAtUtc,
        PersonalUtangEntryStatus status,
        PlatformUserId? resolvedByUserIdentityId,
        DateTimeOffset? resolvedAtUtc,
        string? disputeReason,
        PersonalUtangEntryIntent intent,
        decimal? settlementBalanceSnapshot)
    {
        Id = id;
        RelationshipId = relationshipId;
        EntryType = entryType;
        Amount = amount;
        SignedDelta = signedDelta;
        BalanceAfter = balanceAfter;
        Notes = notes;
        DueDateUtc = dueDateUtc;
        CreatedByUserIdentityId = createdByUserIdentityId;
        CreatedAtUtc = createdAtUtc;
        Status = status;
        ResolvedByUserIdentityId = resolvedByUserIdentityId;
        ResolvedAtUtc = resolvedAtUtc;
        DisputeReason = disputeReason;
        Intent = intent;
        SettlementBalanceSnapshot = settlementBalanceSnapshot;
    }

    internal static PersonalUtangEntry Create(
        PersonalUtangEntryId? id,
        PersonalDebtRelationshipId relationshipId,
        PersonalUtangEntryType entryType,
        decimal amount,
        decimal signedDelta,
        decimal balanceAfter,
        PlatformUserId createdByUserIdentityId,
        DateTimeOffset utcNow,
        PersonalUtangEntryStatus status,
        string? notes,
        DateTimeOffset? dueDateUtc,
        PersonalUtangEntryIntent intent = PersonalUtangEntryIntent.Regular,
        decimal? settlementBalanceSnapshot = null)
    {
        ArgumentNullException.ThrowIfNull(relationshipId);
        ArgumentNullException.ThrowIfNull(createdByUserIdentityId);
        EnsureUtc(utcNow);

        notes = NormalizeNotes(notes, entryType);
        if (dueDateUtc is not null)
        {
            EnsureUtc(dueDateUtc.Value);
        }

        ValidateSettlementIntent(entryType, intent, settlementBalanceSnapshot, amount);

        return new PersonalUtangEntry(
            id ?? PersonalUtangEntryId.New(),
            relationshipId,
            entryType,
            amount,
            signedDelta,
            balanceAfter,
            notes,
            dueDateUtc,
            createdByUserIdentityId,
            utcNow,
            status,
            resolvedByUserIdentityId: null,
            resolvedAtUtc: null,
            disputeReason: null,
            intent,
            settlementBalanceSnapshot);
    }

    /// <summary>
    /// Loan and Adjustment require a non-empty Purpose / Note (or reason). Payment notes stay optional.
    /// </summary>
    public static string? NormalizeNotes(string? notes, PersonalUtangEntryType entryType)
    {
        var trimmed = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        if (trimmed is not null && trimmed.Length > NotesMaxLength)
        {
            trimmed = trimmed[..NotesMaxLength];
        }

        var required = entryType is PersonalUtangEntryType.Loan or PersonalUtangEntryType.Adjustment;
        if (required && trimmed is null)
        {
            throw new DomainException(
                DomainErrorCodes.PersonalUtangNotesRequired,
                entryType is PersonalUtangEntryType.Adjustment
                    ? "A reason / note is required for adjustments."
                    : "A purpose / note is required for this Utang entry.");
        }

        return trimmed;
    }

    public static PersonalUtangEntry Rehydrate(
        PersonalUtangEntryId id,
        PersonalDebtRelationshipId relationshipId,
        PersonalUtangEntryType entryType,
        decimal amount,
        decimal signedDelta,
        decimal balanceAfter,
        string? notes,
        DateTimeOffset? dueDateUtc,
        PlatformUserId createdByUserIdentityId,
        DateTimeOffset createdAtUtc,
        PersonalUtangEntryStatus status = PersonalUtangEntryStatus.Confirmed,
        PlatformUserId? resolvedByUserIdentityId = null,
        DateTimeOffset? resolvedAtUtc = null,
        string? disputeReason = null,
        PersonalUtangEntryIntent intent = PersonalUtangEntryIntent.Regular,
        decimal? settlementBalanceSnapshot = null) =>
        new(
            id,
            relationshipId,
            entryType,
            amount,
            signedDelta,
            balanceAfter,
            notes,
            dueDateUtc,
            createdByUserIdentityId,
            createdAtUtc,
            status,
            resolvedByUserIdentityId,
            resolvedAtUtc,
            disputeReason,
            intent,
            settlementBalanceSnapshot);

    /// <summary>Applies confirmation metadata and the post-confirmation balance snapshot.</summary>
    internal void MarkConfirmed(PlatformUserId resolvedBy, DateTimeOffset utcNow, decimal balanceAfter)
    {
        ArgumentNullException.ThrowIfNull(resolvedBy);
        EnsureUtc(utcNow);
        Status = PersonalUtangEntryStatus.Confirmed;
        ResolvedByUserIdentityId = resolvedBy;
        ResolvedAtUtc = utcNow;
        DisputeReason = null;
        BalanceAfter = balanceAfter;
    }

    internal void MarkDisputed(PlatformUserId resolvedBy, DateTimeOffset utcNow, string? disputeReason)
    {
        ArgumentNullException.ThrowIfNull(resolvedBy);
        EnsureUtc(utcNow);
        Status = PersonalUtangEntryStatus.Disputed;
        ResolvedByUserIdentityId = resolvedBy;
        ResolvedAtUtc = utcNow;
        DisputeReason = NormalizeDisputeReason(disputeReason);
    }

    internal void MarkCancelled(PlatformUserId resolvedBy, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(resolvedBy);
        EnsureUtc(utcNow);
        Status = PersonalUtangEntryStatus.Cancelled;
        ResolvedByUserIdentityId = resolvedBy;
        ResolvedAtUtc = utcNow;
        DisputeReason = null;
    }

    private static void ValidateSettlementIntent(
        PersonalUtangEntryType entryType,
        PersonalUtangEntryIntent intent,
        decimal? settlementBalanceSnapshot,
        decimal amount)
    {
        if (intent is not PersonalUtangEntryIntent.Settlement)
        {
            if (settlementBalanceSnapshot is not null)
            {
                throw new DomainException(
                    DomainErrorCodes.PersonalUtangSettlementInvalid,
                    "Settlement balance snapshot is only valid for settlement intent.");
            }

            return;
        }

        if (entryType is not PersonalUtangEntryType.Payment)
        {
            throw new DomainException(
                DomainErrorCodes.PersonalUtangSettlementInvalid,
                "Settlement intent requires a Payment entry.");
        }

        if (settlementBalanceSnapshot is null || settlementBalanceSnapshot.Value <= 0)
        {
            throw new DomainException(
                DomainErrorCodes.PersonalUtangSettlementInvalid,
                "Settlement requires a positive balance snapshot.");
        }

        if (settlementBalanceSnapshot.Value != amount)
        {
            throw new DomainException(
                DomainErrorCodes.PersonalUtangSettlementInvalid,
                "Settlement payment amount must equal the balance snapshot.");
        }
    }

    private static string? NormalizeDisputeReason(string? disputeReason)
    {
        if (string.IsNullOrWhiteSpace(disputeReason))
        {
            return null;
        }

        var trimmed = disputeReason.Trim();
        return trimmed[..Math.Min(trimmed.Length, 256)];
    }

    private static void EnsureUtc(DateTimeOffset value)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new DomainException(DomainErrorCodes.InvalidUtcTimestamp, "Timestamps must be UTC.");
        }
    }
}

public sealed class PersonalUtangEntryId : IEquatable<PersonalUtangEntryId>
{
    public Guid Value { get; }

    private PersonalUtangEntryId(Guid value) => Value = value;

    public static PersonalUtangEntryId New() => new(Guid.NewGuid());

    public static PersonalUtangEntryId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(DomainErrorCodes.InvalidPersonalUtangEntryId, "Utang entry id is required.");
        }

        return new PersonalUtangEntryId(value);
    }

    public bool Equals(PersonalUtangEntryId? other) =>
        other is not null && Value.Equals(other.Value);

    public override bool Equals(object? obj) =>
        obj is PersonalUtangEntryId other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString("D");

    public static bool operator ==(PersonalUtangEntryId? left, PersonalUtangEntryId? right) => Equals(left, right);

    public static bool operator !=(PersonalUtangEntryId? left, PersonalUtangEntryId? right) => !Equals(left, right);
}
