using ExItS.Platform.Application.Personal;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Personal;
using ExItS.Platform.Infrastructure.Persistence.Personal;
using Microsoft.EntityFrameworkCore;

namespace ExItS.Platform.Infrastructure.Persistence.Repositories;

internal sealed class PersonalAccountSettingsRepository(PlatformDbContext db) : IPersonalAccountSettingsRepository
{
    public async Task<PersonalAccountSettings?> GetByUserAsync(
        PlatformUserId userIdentityId,
        CancellationToken cancellationToken = default)
    {
        var record = await db.PersonalAccountSettings.AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserIdentityId == userIdentityId.Value, cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : ToDomain(record);
    }

    public Task AddAsync(PersonalAccountSettings settings, CancellationToken cancellationToken = default)
    {
        db.PersonalAccountSettings.Add(ToRecord(settings));
        return Task.CompletedTask;
    }

    public async Task UpdateAsync(PersonalAccountSettings settings, CancellationToken cancellationToken = default)
    {
        var record = await db.PersonalAccountSettings
            .FirstOrDefaultAsync(x => x.UserIdentityId == settings.UserIdentityId.Value, cancellationToken)
            .ConfigureAwait(false);
        if (record is null)
        {
            return;
        }

        record.EmailNotificationsEnabled = settings.EmailNotificationsEnabled;
        record.PushNotificationsEnabled = settings.PushNotificationsEnabled;
        record.InAppNotificationsEnabled = settings.InAppNotificationsEnabled;
        record.ReminderNotificationsEnabled = settings.ReminderNotificationsEnabled;
        record.UpdatedAtUtc = settings.UpdatedAtUtc;
        record.Version = settings.Version;
    }

    private static PersonalAccountSettings ToDomain(PersonalAccountSettingsRecord record) =>
        PersonalAccountSettings.Rehydrate(
            PlatformUserId.From(record.UserIdentityId),
            record.EmailNotificationsEnabled,
            record.PushNotificationsEnabled,
            record.InAppNotificationsEnabled,
            record.ReminderNotificationsEnabled,
            record.CreatedAtUtc,
            record.UpdatedAtUtc,
            record.Version);

    private static PersonalAccountSettingsRecord ToRecord(PersonalAccountSettings settings) =>
        new()
        {
            UserIdentityId = settings.UserIdentityId.Value,
            EmailNotificationsEnabled = settings.EmailNotificationsEnabled,
            PushNotificationsEnabled = settings.PushNotificationsEnabled,
            InAppNotificationsEnabled = settings.InAppNotificationsEnabled,
            ReminderNotificationsEnabled = settings.ReminderNotificationsEnabled,
            CreatedAtUtc = settings.CreatedAtUtc,
            UpdatedAtUtc = settings.UpdatedAtUtc,
            Version = settings.Version
        };
}
