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
    public decimal BalanceAfter { get; }
    public string? Notes { get; }
    public DateTimeOffset? DueDateUtc { get; }
    public PlatformUserId CreatedByUserIdentityId { get; }
    public DateTimeOffset CreatedAtUtc { get; }

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
        DateTimeOffset createdAtUtc)
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
        string? notes,
        DateTimeOffset? dueDateUtc)
    {
        ArgumentNullException.ThrowIfNull(relationshipId);
        ArgumentNullException.ThrowIfNull(createdByUserIdentityId);
        EnsureUtc(utcNow);

        notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim()[..Math.Min(notes.Trim().Length, 512)];
        if (dueDateUtc is not null)
        {
            EnsureUtc(dueDateUtc.Value);
        }

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
            utcNow);
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
        DateTimeOffset createdAtUtc) =>
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
            createdAtUtc);

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
