using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;

namespace ExItS.Platform.Domain.Personal;

/// <summary>
/// Peer debt relationship between a creditor and debtor participant.
/// At least one participant must belong to the authenticated Personal account owner.
/// </summary>
public sealed class PersonalDebtRelationship
{
    public PersonalDebtRelationshipId Id { get; }
    public PlatformUserId? CreditorUserIdentityId { get; private set; }
    public PersonalContactId? CreditorContactId { get; private set; }
    public PlatformUserId? DebtorUserIdentityId { get; private set; }
    public PersonalContactId? DebtorContactId { get; private set; }
    public string CurrencyCode { get; }
    public decimal CurrentBalance { get; private set; }
    public DateTimeOffset? DueDateUtc { get; private set; }
    public PersonalDebtRelationshipStatus Status { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public int Version { get; private set; }

    private PersonalDebtRelationship(
        PersonalDebtRelationshipId id,
        PlatformUserId? creditorUserIdentityId,
        PersonalContactId? creditorContactId,
        PlatformUserId? debtorUserIdentityId,
        PersonalContactId? debtorContactId,
        string currencyCode,
        decimal currentBalance,
        DateTimeOffset? dueDateUtc,
        PersonalDebtRelationshipStatus status,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc,
        int version)
    {
        Id = id;
        CreditorUserIdentityId = creditorUserIdentityId;
        CreditorContactId = creditorContactId;
        DebtorUserIdentityId = debtorUserIdentityId;
        DebtorContactId = debtorContactId;
        CurrencyCode = currencyCode;
        CurrentBalance = currentBalance;
        DueDateUtc = dueDateUtc;
        Status = status;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
        Version = version;
    }

    public static PersonalDebtRelationship Create(
        PlatformUserId actingUserIdentityId,
        PlatformUserId? creditorUserIdentityId,
        PersonalContactId? creditorContactId,
        PlatformUserId? debtorUserIdentityId,
        PersonalContactId? debtorContactId,
        string currencyCode,
        DateTimeOffset utcNow,
        DateTimeOffset? dueDateUtc = null,
        PersonalDebtRelationshipId? id = null)
    {
        ArgumentNullException.ThrowIfNull(actingUserIdentityId);
        EnsureUtc(utcNow);
        ValidateParticipantSide(creditorUserIdentityId, creditorContactId, "Creditor");
        ValidateParticipantSide(debtorUserIdentityId, debtorContactId, "Debtor");

        if (creditorUserIdentityId is not null && debtorUserIdentityId is not null
            && creditorUserIdentityId == debtorUserIdentityId)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPersonalDebtRelationship,
                "Creditor and debtor cannot be the same user.");
        }

        if (actingUserIdentityId != creditorUserIdentityId && actingUserIdentityId != debtorUserIdentityId
            && creditorContactId is null && debtorContactId is null)
        {
            throw new DomainException(
                DomainErrorCodes.PersonalUtangUnauthorized,
                "At least one relationship participant must belong to the authenticated Personal account.");
        }

        currencyCode = NormalizeCurrency(currencyCode);
        if (dueDateUtc is not null)
        {
            EnsureUtc(dueDateUtc.Value);
        }

        return new PersonalDebtRelationship(
            id ?? PersonalDebtRelationshipId.New(),
            creditorUserIdentityId,
            creditorContactId,
            debtorUserIdentityId,
            debtorContactId,
            currencyCode,
            currentBalance: 0m,
            dueDateUtc,
            PersonalDebtRelationshipStatus.Active,
            utcNow,
            utcNow,
            version: 1);
    }

    public static PersonalDebtRelationship Rehydrate(
        PersonalDebtRelationshipId id,
        PlatformUserId? creditorUserIdentityId,
        PersonalContactId? creditorContactId,
        PlatformUserId? debtorUserIdentityId,
        PersonalContactId? debtorContactId,
        string currencyCode,
        decimal currentBalance,
        DateTimeOffset? dueDateUtc,
        PersonalDebtRelationshipStatus status,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc,
        int version) =>
        new(
            id,
            creditorUserIdentityId,
            creditorContactId,
            debtorUserIdentityId,
            debtorContactId,
            currencyCode,
            currentBalance,
            dueDateUtc,
            status,
            createdAtUtc,
            updatedAtUtc,
            version);

    public bool CanBeViewedBy(PlatformUserId userIdentityId) =>
        CreditorUserIdentityId == userIdentityId
        || DebtorUserIdentityId == userIdentityId;

    public bool CanBeViewedByContactOwner(PlatformUserId ownerUserIdentityId, PersonalContact contact) =>
        (CreditorContactId is not null && contact.Id == CreditorContactId && contact.IsOwnedBy(ownerUserIdentityId))
        || (DebtorContactId is not null && contact.Id == DebtorContactId && contact.IsOwnedBy(ownerUserIdentityId));

    /// <summary>
    /// After explicit invitation acceptance, promote a contact participant to the linked user.
    /// Does not create Organization membership or product roles.
    /// </summary>
    public void AuthorizeLinkedParticipant(
        PersonalContactId contactId,
        PlatformUserId linkedUserIdentityId,
        DateTimeOffset utcNow,
        int? expectedVersion = null)
    {
        ArgumentNullException.ThrowIfNull(contactId);
        ArgumentNullException.ThrowIfNull(linkedUserIdentityId);
        EnsureUtc(utcNow);
        EnsureVersion(expectedVersion);

        if (Status is not PersonalDebtRelationshipStatus.Active)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPersonalDebtRelationship,
                "Cannot authorize participants on a closed relationship.");
        }

        if (CreditorContactId == contactId)
        {
            if (DebtorUserIdentityId == linkedUserIdentityId)
            {
                throw new DomainException(
                    DomainErrorCodes.InvalidPersonalDebtRelationship,
                    "Creditor and debtor cannot be the same user.");
            }

            CreditorUserIdentityId = linkedUserIdentityId;
            CreditorContactId = null;
        }
        else if (DebtorContactId == contactId)
        {
            if (CreditorUserIdentityId == linkedUserIdentityId)
            {
                throw new DomainException(
                    DomainErrorCodes.InvalidPersonalDebtRelationship,
                    "Creditor and debtor cannot be the same user.");
            }

            DebtorUserIdentityId = linkedUserIdentityId;
            DebtorContactId = null;
        }
        else
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPersonalDebtRelationship,
                "Contact is not a participant on this relationship.");
        }

        UpdatedAtUtc = utcNow;
        Version++;
    }

    public PersonalUtangEntry RecordEntry(
        PlatformUserId actingUserIdentityId,
        PersonalUtangEntryType entryType,
        decimal amount,
        decimal signedDelta,
        DateTimeOffset utcNow,
        int? expectedVersion,
        string? notes = null,
        DateTimeOffset? dueDateUtc = null,
        PersonalUtangEntryId? entryId = null)
    {
        EnsureUtc(utcNow);
        EnsureVersion(expectedVersion);

        if (Status is not PersonalDebtRelationshipStatus.Active)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPersonalDebtRelationship,
                "Cannot record entries on a closed relationship.");
        }

        if (amount <= 0)
        {
            throw new DomainException(DomainErrorCodes.PersonalUtangAmountInvalid, "Entry amount must be positive.");
        }

        if (dueDateUtc is not null)
        {
            EnsureUtc(dueDateUtc.Value);
        }

        var newBalance = CurrentBalance + signedDelta;
        CurrentBalance = newBalance;
        if (dueDateUtc is not null && entryType is PersonalUtangEntryType.Loan)
        {
            DueDateUtc = dueDateUtc;
        }

        UpdatedAtUtc = utcNow;
        Version++;

        return PersonalUtangEntry.Create(
            entryId,
            Id,
            entryType,
            amount,
            signedDelta,
            newBalance,
            actingUserIdentityId,
            utcNow,
            notes,
            dueDateUtc);
    }

    public static decimal ComputeSignedDelta(PersonalUtangEntryType entryType, decimal amount, decimal? adjustmentDelta = null) =>
        entryType switch
        {
            PersonalUtangEntryType.Loan => amount,
            PersonalUtangEntryType.Payment => -amount,
            PersonalUtangEntryType.Adjustment when adjustmentDelta is null =>
                throw new DomainException(DomainErrorCodes.PersonalUtangAmountInvalid, "Adjustment delta is required."),
            PersonalUtangEntryType.Adjustment => adjustmentDelta!.Value,
            _ => throw new DomainException(DomainErrorCodes.InvalidPersonalUtangEntryType, "Entry type is invalid.")
        };

    private void EnsureVersion(int? expectedVersion)
    {
        if (expectedVersion is null)
        {
            return;
        }

        if (expectedVersion.Value != Version)
        {
            throw new DomainException(
                DomainErrorCodes.PersonalUtangConcurrencyConflict,
                "The debt relationship was modified by another request.");
        }
    }

    private static void ValidateParticipantSide(
        PlatformUserId? userIdentityId,
        PersonalContactId? contactId,
        string sideName)
    {
        var hasUser = userIdentityId is not null;
        var hasContact = contactId is not null;
        if (hasUser == hasContact)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPersonalDebtRelationship,
                $"{sideName} must reference exactly one user or contact.");
        }
    }

    private static string NormalizeCurrency(string currencyCode)
    {
        if (string.IsNullOrWhiteSpace(currencyCode))
        {
            return "PHP";
        }

        var normalized = currencyCode.Trim().ToUpperInvariant();
        if (normalized.Length != 3)
        {
            throw new DomainException(DomainErrorCodes.PersonalUtangAmountInvalid, "Currency code must be three letters.");
        }

        return normalized;
    }

    private static void EnsureUtc(DateTimeOffset value)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new DomainException(DomainErrorCodes.InvalidUtcTimestamp, "Timestamps must be UTC.");
        }
    }
}

public sealed class PersonalDebtRelationshipId : IEquatable<PersonalDebtRelationshipId>
{
    public Guid Value { get; }

    private PersonalDebtRelationshipId(Guid value) => Value = value;

    public static PersonalDebtRelationshipId New() => new(Guid.NewGuid());

    public static PersonalDebtRelationshipId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(DomainErrorCodes.InvalidPersonalDebtRelationshipId, "Debt relationship id is required.");
        }

        return new PersonalDebtRelationshipId(value);
    }

    public bool Equals(PersonalDebtRelationshipId? other) =>
        other is not null && Value.Equals(other.Value);

    public override bool Equals(object? obj) =>
        obj is PersonalDebtRelationshipId other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString("D");

    public static bool operator ==(PersonalDebtRelationshipId? left, PersonalDebtRelationshipId? right) => Equals(left, right);

    public static bool operator !=(PersonalDebtRelationshipId? left, PersonalDebtRelationshipId? right) => !Equals(left, right);
}
