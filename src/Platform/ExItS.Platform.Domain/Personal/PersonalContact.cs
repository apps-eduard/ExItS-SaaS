using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;

namespace ExItS.Platform.Domain.Personal;

public sealed class PersonalContact
{
    public PersonalContactId Id { get; }
    public PlatformUserId OwnerUserIdentityId { get; }
    public string DisplayName { get; private set; }
    public string? Phone { get; private set; }
    public string? Email { get; private set; }
    public PlatformUserId? LinkedUserIdentityId { get; private set; }
    public PersonalContactStatus Status { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public bool IsLinked => LinkedUserIdentityId is not null;

    private PersonalContact(
        PersonalContactId id,
        PlatformUserId ownerUserIdentityId,
        string displayName,
        string? phone,
        string? email,
        PlatformUserId? linkedUserIdentityId,
        PersonalContactStatus status,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc)
    {
        Id = id;
        OwnerUserIdentityId = ownerUserIdentityId;
        DisplayName = displayName;
        Phone = phone;
        Email = email;
        LinkedUserIdentityId = linkedUserIdentityId;
        Status = status;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
    }

    public static PersonalContact Create(
        PlatformUserId ownerUserIdentityId,
        string displayName,
        string? phone,
        string? email,
        DateTimeOffset utcNow,
        PersonalContactId? id = null)
    {
        ArgumentNullException.ThrowIfNull(ownerUserIdentityId);
        EnsureUtc(utcNow);
        displayName = NormalizeDisplayName(displayName);
        phone = NormalizeOptional(phone, 32);
        email = NormalizeOptionalEmail(email);

        return new PersonalContact(
            id ?? PersonalContactId.New(),
            ownerUserIdentityId,
            displayName,
            phone,
            email,
            linkedUserIdentityId: null,
            PersonalContactStatus.Active,
            utcNow,
            utcNow);
    }

    public static PersonalContact Rehydrate(
        PersonalContactId id,
        PlatformUserId ownerUserIdentityId,
        string displayName,
        string? phone,
        string? email,
        PlatformUserId? linkedUserIdentityId,
        PersonalContactStatus status,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc) =>
        new(id, ownerUserIdentityId, displayName, phone, email, linkedUserIdentityId, status, createdAtUtc, updatedAtUtc);

    public bool IsOwnedBy(PlatformUserId userIdentityId) =>
        OwnerUserIdentityId == userIdentityId;

    /// <summary>
    /// Links this contact to a Platform User after explicit invitation acceptance.
    /// Never matches silently by name, email, or phone.
    /// </summary>
    public void LinkUser(PlatformUserId linkedUserIdentityId, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(linkedUserIdentityId);
        EnsureUtc(utcNow);

        if (LinkedUserIdentityId is not null)
        {
            throw new DomainException(
                DomainErrorCodes.PersonalContactAlreadyLinked,
                "Personal contact is already linked to a user.");
        }

        if (linkedUserIdentityId == OwnerUserIdentityId)
        {
            throw new DomainException(
                DomainErrorCodes.PersonalContactLinkInvalid,
                "Cannot link a contact to its owner.");
        }

        if (Status is not PersonalContactStatus.Active)
        {
            throw new DomainException(
                DomainErrorCodes.PersonalContactLinkInvalid,
                "Only active contacts can be linked.");
        }

        LinkedUserIdentityId = linkedUserIdentityId;
        UpdatedAtUtc = utcNow;
    }

    private static string NormalizeDisplayName(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new DomainException(DomainErrorCodes.InvalidPersonalContactDisplayName, "Contact display name is required.");
        }

        var trimmed = displayName.Trim();
        if (trimmed.Length > 100)
        {
            throw new DomainException(DomainErrorCodes.InvalidPersonalContactDisplayName, "Contact display name is too long.");
        }

        return trimmed;
    }

    private static string? NormalizeOptional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
        {
            throw new DomainException(DomainErrorCodes.InvalidPersonalContactDisplayName, "Contact field value is too long.");
        }

        return trimmed;
    }

    private static string? NormalizeOptionalEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        var trimmed = email.Trim();
        if (trimmed.Length > 320)
        {
            throw new DomainException(DomainErrorCodes.InvalidEmail, "Contact email is too long.");
        }

        return trimmed.ToUpperInvariant();
    }

    private static void EnsureUtc(DateTimeOffset value)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new DomainException(DomainErrorCodes.InvalidUtcTimestamp, "Timestamps must be UTC.");
        }
    }
}

public sealed class PersonalContactId : IEquatable<PersonalContactId>
{
    public Guid Value { get; }

    private PersonalContactId(Guid value) => Value = value;

    public static PersonalContactId New() => new(Guid.NewGuid());

    public static PersonalContactId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(DomainErrorCodes.InvalidPersonalContactId, "Personal contact id is required.");
        }

        return new PersonalContactId(value);
    }

    public bool Equals(PersonalContactId? other) =>
        other is not null && Value.Equals(other.Value);

    public override bool Equals(object? obj) =>
        obj is PersonalContactId other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString("D");

    public static bool operator ==(PersonalContactId? left, PersonalContactId? right) => Equals(left, right);

    public static bool operator !=(PersonalContactId? left, PersonalContactId? right) => !Equals(left, right);
}
