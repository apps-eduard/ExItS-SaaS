using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;

namespace ExItS.Platform.Domain.Personal;

/// <summary>Notification and preference foundation for a Personal Account (User Identity).</summary>
public sealed class PersonalAccountSettings
{
    public PlatformUserId UserIdentityId { get; }
    public bool EmailNotificationsEnabled { get; private set; }
    public bool PushNotificationsEnabled { get; private set; }
    public bool InAppNotificationsEnabled { get; private set; }
    public bool ReminderNotificationsEnabled { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public int Version { get; private set; }

    private PersonalAccountSettings(
        PlatformUserId userIdentityId,
        bool emailNotificationsEnabled,
        bool pushNotificationsEnabled,
        bool inAppNotificationsEnabled,
        bool reminderNotificationsEnabled,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc,
        int version)
    {
        UserIdentityId = userIdentityId;
        EmailNotificationsEnabled = emailNotificationsEnabled;
        PushNotificationsEnabled = pushNotificationsEnabled;
        InAppNotificationsEnabled = inAppNotificationsEnabled;
        ReminderNotificationsEnabled = reminderNotificationsEnabled;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
        Version = version;
    }

    public static PersonalAccountSettings CreateDefaults(PlatformUserId userIdentityId, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(userIdentityId);
        EnsureUtc(utcNow);
        return new PersonalAccountSettings(
            userIdentityId,
            emailNotificationsEnabled: true,
            pushNotificationsEnabled: true,
            inAppNotificationsEnabled: true,
            reminderNotificationsEnabled: true,
            utcNow,
            utcNow,
            version: 1);
    }

    public static PersonalAccountSettings Rehydrate(
        PlatformUserId userIdentityId,
        bool emailNotificationsEnabled,
        bool pushNotificationsEnabled,
        bool inAppNotificationsEnabled,
        bool reminderNotificationsEnabled,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc,
        int version) =>
        new(
            userIdentityId,
            emailNotificationsEnabled,
            pushNotificationsEnabled,
            inAppNotificationsEnabled,
            reminderNotificationsEnabled,
            createdAtUtc,
            updatedAtUtc,
            version);

    public void UpdateNotificationPreferences(
        bool emailNotificationsEnabled,
        bool pushNotificationsEnabled,
        bool inAppNotificationsEnabled,
        bool reminderNotificationsEnabled,
        DateTimeOffset utcNow,
        int? expectedVersion)
    {
        EnsureUtc(utcNow);
        EnsureVersion(expectedVersion);

        EmailNotificationsEnabled = emailNotificationsEnabled;
        PushNotificationsEnabled = pushNotificationsEnabled;
        InAppNotificationsEnabled = inAppNotificationsEnabled;
        ReminderNotificationsEnabled = reminderNotificationsEnabled;
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
                DomainErrorCodes.PersonalAccountSettingsConcurrencyConflict,
                "Personal account settings were modified by another request.");
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
