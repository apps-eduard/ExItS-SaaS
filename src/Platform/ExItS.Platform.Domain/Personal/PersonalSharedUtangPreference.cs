using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;

namespace ExItS.Platform.Domain.Personal;

/// <summary>
/// Recipient-owned Personal↔Personal Utang sharing preferences for one connected counterparty.
/// Keyed by (OwnerUserIdentityId = recipient, CounterpartyUserIdentityId = sender).
/// </summary>
public sealed class PersonalSharedUtangPreference
{
    public PersonalSharedUtangPreferenceId Id { get; }
    public PlatformUserId OwnerUserIdentityId { get; }
    public PlatformUserId CounterpartyUserIdentityId { get; }
    public bool ReceiveSharedUtang { get; private set; }
    public bool AutoAcceptSharedUtang { get; private set; }
    public bool SharedUtangNotifications { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public int Version { get; private set; }

    private PersonalSharedUtangPreference(
        PersonalSharedUtangPreferenceId id,
        PlatformUserId ownerUserIdentityId,
        PlatformUserId counterpartyUserIdentityId,
        bool receiveSharedUtang,
        bool autoAcceptSharedUtang,
        bool sharedUtangNotifications,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc,
        int version)
    {
        Id = id;
        OwnerUserIdentityId = ownerUserIdentityId;
        CounterpartyUserIdentityId = counterpartyUserIdentityId;
        ReceiveSharedUtang = receiveSharedUtang;
        AutoAcceptSharedUtang = autoAcceptSharedUtang;
        SharedUtangNotifications = sharedUtangNotifications;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
        Version = version;
    }

    /// <summary>Defaults when no preference row exists yet.</summary>
    public static PersonalSharedUtangPreference CreateDefaults(
        PlatformUserId ownerUserIdentityId,
        PlatformUserId counterpartyUserIdentityId,
        DateTimeOffset utcNow,
        PersonalSharedUtangPreferenceId? id = null)
    {
        ArgumentNullException.ThrowIfNull(ownerUserIdentityId);
        ArgumentNullException.ThrowIfNull(counterpartyUserIdentityId);
        EnsureUtc(utcNow);

        if (ownerUserIdentityId == counterpartyUserIdentityId)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPersonalSharedUtangPreference,
                "Cannot create shared Utang preferences for yourself.");
        }

        return new PersonalSharedUtangPreference(
            id ?? PersonalSharedUtangPreferenceId.New(),
            ownerUserIdentityId,
            counterpartyUserIdentityId,
            receiveSharedUtang: true,
            autoAcceptSharedUtang: false,
            sharedUtangNotifications: true,
            utcNow,
            utcNow,
            version: 1);
    }

    public static PersonalSharedUtangPreference Rehydrate(
        PersonalSharedUtangPreferenceId id,
        PlatformUserId ownerUserIdentityId,
        PlatformUserId counterpartyUserIdentityId,
        bool receiveSharedUtang,
        bool autoAcceptSharedUtang,
        bool sharedUtangNotifications,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc,
        int version) =>
        new(
            id,
            ownerUserIdentityId,
            counterpartyUserIdentityId,
            receiveSharedUtang,
            autoAcceptSharedUtang,
            sharedUtangNotifications,
            createdAtUtc,
            updatedAtUtc,
            version);

    public void Update(
        bool receiveSharedUtang,
        bool autoAcceptSharedUtang,
        bool sharedUtangNotifications,
        DateTimeOffset utcNow,
        int? expectedVersion = null)
    {
        EnsureUtc(utcNow);
        EnsureVersion(expectedVersion);

        // Auto-accept is meaningless when receive is off; keep storage coherent.
        if (!receiveSharedUtang)
        {
            autoAcceptSharedUtang = false;
        }

        ReceiveSharedUtang = receiveSharedUtang;
        AutoAcceptSharedUtang = autoAcceptSharedUtang;
        SharedUtangNotifications = sharedUtangNotifications;
        UpdatedAtUtc = utcNow;
        Version++;
    }

    private void EnsureVersion(int? expectedVersion)
    {
        if (expectedVersion is null)
        {
            return;
        }

        if (expectedVersion.Value != Version)
        {
            throw new DomainException(
                DomainErrorCodes.PersonalSharedUtangPreferenceConcurrencyConflict,
                "Shared Utang preferences were modified by another request.");
        }
    }

    private static void EnsureUtc(DateTimeOffset value)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new DomainException(DomainErrorCodes.InvalidUtcTimestamp, "Timestamps must be UTC.");
        }
    }
}

public sealed class PersonalSharedUtangPreferenceId : IEquatable<PersonalSharedUtangPreferenceId>
{
    public Guid Value { get; }

    private PersonalSharedUtangPreferenceId(Guid value) => Value = value;

    public static PersonalSharedUtangPreferenceId New() => new(Guid.NewGuid());

    public static PersonalSharedUtangPreferenceId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPersonalSharedUtangPreferenceId,
                "Shared Utang preference id is required.");
        }

        return new PersonalSharedUtangPreferenceId(value);
    }

    public bool Equals(PersonalSharedUtangPreferenceId? other) =>
        other is not null && Value == other.Value;

    public override bool Equals(object? obj) =>
        obj is PersonalSharedUtangPreferenceId other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public static bool operator ==(PersonalSharedUtangPreferenceId? left, PersonalSharedUtangPreferenceId? right) =>
        Equals(left, right);

    public static bool operator !=(PersonalSharedUtangPreferenceId? left, PersonalSharedUtangPreferenceId? right) =>
        !Equals(left, right);
}
