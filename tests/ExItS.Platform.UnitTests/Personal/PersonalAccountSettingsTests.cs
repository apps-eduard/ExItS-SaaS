using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Personal;

namespace ExItS.Platform.UnitTests.Personal;

public sealed class PersonalAccountSettingsTests
{
    private static readonly DateTimeOffset T0 = DateTimeOffset.Parse("2026-08-02T00:00:00Z");

    [Fact]
    public void CreateDefaults_enables_all_notification_channels()
    {
        var userId = PlatformUserId.From(Guid.Parse("11111111-1111-1111-1111-111111111111"));
        var settings = PersonalAccountSettings.CreateDefaults(userId, T0);

        Assert.Equal(userId, settings.UserIdentityId);
        Assert.True(settings.EmailNotificationsEnabled);
        Assert.True(settings.PushNotificationsEnabled);
        Assert.True(settings.InAppNotificationsEnabled);
        Assert.True(settings.ReminderNotificationsEnabled);
        Assert.Equal(1, settings.Version);
    }

    [Fact]
    public void UpdateNotificationPreferences_increments_version()
    {
        var userId = PlatformUserId.From(Guid.Parse("22222222-2222-2222-2222-222222222222"));
        var settings = PersonalAccountSettings.CreateDefaults(userId, T0);

        settings.UpdateNotificationPreferences(
            emailNotificationsEnabled: false,
            pushNotificationsEnabled: true,
            inAppNotificationsEnabled: false,
            reminderNotificationsEnabled: true,
            utcNow: T0.AddMinutes(1),
            expectedVersion: 1);

        Assert.False(settings.EmailNotificationsEnabled);
        Assert.False(settings.InAppNotificationsEnabled);
        Assert.Equal(2, settings.Version);
    }

    [Fact]
    public void UpdateNotificationPreferences_rejects_stale_expected_version()
    {
        var userId = PlatformUserId.From(Guid.Parse("33333333-3333-3333-3333-333333333333"));
        var settings = PersonalAccountSettings.CreateDefaults(userId, T0);

        var ex = Assert.Throws<DomainException>(() =>
            settings.UpdateNotificationPreferences(
                emailNotificationsEnabled: false,
                pushNotificationsEnabled: false,
                inAppNotificationsEnabled: false,
                reminderNotificationsEnabled: false,
                utcNow: T0.AddMinutes(1),
                expectedVersion: 0));

        Assert.Equal(DomainErrorCodes.PersonalAccountSettingsConcurrencyConflict, ex.ErrorCode);
        Assert.Equal(1, settings.Version);
    }
}
