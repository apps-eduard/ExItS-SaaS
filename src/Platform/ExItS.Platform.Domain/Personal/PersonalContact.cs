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
    public PlatformUserId? ResolvedUserIdentityId { get; private set; }
    public string? ResolvedPublicUserId { get; private set; }
    public DateTimeOffset? ConnectedAtUtc { get; private set; }
    public DateTimeOffset? BlockedAtUtc { get; private set; }
    public PersonalContactStatus Status { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public bool IsLinked => LinkedUserIdentityId is not null;
    public bool IsBlocked => BlockedAtUtc is not null;
    public bool IsConnected => LinkedUserIdentityId is not null;
    public bool HasResolvedIdentity => ResolvedUserIdentityId is not null;

    private PersonalContact(
        PersonalContactId id,
        PlatformUserId ownerUserIdentityId,
        string displayName,
        string? phone,
        string? email,
        PlatformUserId? linkedUserIdentityId,
        PlatformUserId? resolvedUserIdentityId,
        string? resolvedPublicUserId,
        DateTimeOffset? connectedAtUtc,
        DateTimeOffset? blockedAtUtc,
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
        ResolvedUserIdentityId = resolvedUserIdentityId;
        ResolvedPublicUserId = resolvedPublicUserId;
        ConnectedAtUtc = connectedAtUtc;
        BlockedAtUtc = blockedAtUtc;
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
            resolvedUserIdentityId: null,
            resolvedPublicUserId: null,
            connectedAtUtc: null,
            blockedAtUtc: null,
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
        PlatformUserId? resolvedUserIdentityId,
        string? resolvedPublicUserId,
        DateTimeOffset? connectedAtUtc,
        DateTimeOffset? blockedAtUtc,
        PersonalContactStatus status,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc) =>
        new(
            id,
            ownerUserIdentityId,
            displayName,
            phone,
            email,
            linkedUserIdentityId,
            resolvedUserIdentityId,
            resolvedPublicUserId,
            connectedAtUtc,
            blockedAtUtc,
            status,
            createdAtUtc,
            updatedAtUtc);

    public bool IsOwnedBy(PlatformUserId userIdentityId) =>
        OwnerUserIdentityId == userIdentityId;

    public void ResolveIdentity(
        PlatformUserId resolvedUserIdentityId,
        string resolvedPublicUserId,
        DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(resolvedUserIdentityId);
        EnsureUtc(utcNow);
        EnsureActive();

        if (resolvedUserIdentityId == OwnerUserIdentityId)
        {
            throw new DomainException(
                DomainErrorCodes.PersonalContactLinkInvalid,
                "Cannot resolve a contact to its owner.");
        }

        resolvedPublicUserId = NormalizePublicUserId(resolvedPublicUserId);

        if (ResolvedUserIdentityId is not null && ResolvedUserIdentityId != resolvedUserIdentityId)
        {
            throw new DomainException(
                DomainErrorCodes.PersonalContactResolvedConflict,
                "Contact already resolves to a different ExItS identity.");
        }

        ResolvedUserIdentityId = resolvedUserIdentityId;
        ResolvedPublicUserId = resolvedPublicUserId;
        UpdatedAtUtc = utcNow;
    }

    public void LinkUser(PlatformUserId linkedUserIdentityId, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(linkedUserIdentityId);
        EnsureUtc(utcNow);
        EnsureActive();

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

        if (IsBlocked)
        {
            throw new DomainException(
                DomainErrorCodes.PersonalContactBlocked,
                "Blocked contacts cannot be linked.");
        }

        LinkedUserIdentityId = linkedUserIdentityId;
        ConnectedAtUtc = utcNow;
        UpdatedAtUtc = utcNow;
    }

    public void Unlink(DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        EnsureActive();

        if (LinkedUserIdentityId is null)
        {
            return;
        }

        LinkedUserIdentityId = null;
        ConnectedAtUtc = null;
        UpdatedAtUtc = utcNow;
    }

    public void Block(DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        EnsureActive();

        if (BlockedAtUtc is not null)
        {
            return;
        }

        Unlink(utcNow);
        BlockedAtUtc = utcNow;
        UpdatedAtUtc = utcNow;
    }

    public void Unblock(DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        EnsureActive();

        if (BlockedAtUtc is null)
        {
            return;
        }

        BlockedAtUtc = null;
        UpdatedAtUtc = utcNow;
    }

    public void UpdateDetails(string displayName, string? phone, string? email, DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        if (Status is not PersonalContactStatus.Active)
        {
            throw new DomainException(
                DomainErrorCodes.PersonalContactLinkInvalid,
                "Only active contacts can be updated.");
        }

        DisplayName = NormalizeDisplayName(displayName);
        Phone = NormalizeOptional(phone, 32);
        Email = NormalizeOptionalEmail(email);
        UpdatedAtUtc = utcNow;
    }

    public void Archive(DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        if (Status == PersonalContactStatus.Archived)
        {
            return;
        }

        Status = PersonalContactStatus.Archived;
        UpdatedAtUtc = utcNow;
    }

    private void EnsureActive()
    {
        if (Status is not PersonalContactStatus.Active)
        {
            throw new DomainException(
                DomainErrorCodes.PersonalContactLinkInvalid,
                "Only active contacts can be updated.");
        }
    }

    private static string NormalizePublicUserId(string publicUserId)
    {
        if (string.IsNullOrWhiteSpace(publicUserId))
        {
            throw new DomainException(
                DomainErrorCodes.PersonalContactNotResolved,
                "Resolved public user id is required.");
        }

        var trimmed = publicUserId.Trim();
        if (trimmed.Length > 32)
        {
            throw new DomainException(
                DomainErrorCodes.PersonalContactNotResolved,
                "Resolved public user id is too long.");
        }

        return trimmed;
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

    public static string? NormalizeOptionalEmail(string? email)
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
