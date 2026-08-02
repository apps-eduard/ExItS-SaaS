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

internal sealed class PersonalContactRepository(PlatformDbContext db) : IPersonalContactRepository
{
    public async Task<PersonalContact?> GetByIdAsync(PersonalContactId id, CancellationToken cancellationToken = default)
    {
        var record = await db.PersonalContacts.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id.Value, cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : ToDomain(record);
    }

    public async Task<IReadOnlyList<PersonalContact>> ListByOwnerAsync(
        PlatformUserId ownerUserIdentityId,
        CancellationToken cancellationToken = default)
    {
        var records = await db.PersonalContacts.AsNoTracking()
            .Where(x => x.OwnerUserIdentityId == ownerUserIdentityId.Value)
            .OrderBy(x => x.DisplayName)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return records.Select(ToDomain).ToList();
    }

    public Task AddAsync(PersonalContact contact, CancellationToken cancellationToken = default)
    {
        db.PersonalContacts.Add(ToRecord(contact));
        return Task.CompletedTask;
    }

    public async Task UpdateAsync(PersonalContact contact, CancellationToken cancellationToken = default)
    {
        var record = await db.PersonalContacts
            .FirstOrDefaultAsync(x => x.Id == contact.Id.Value, cancellationToken)
            .ConfigureAwait(false);
        if (record is null)
        {
            return;
        }

        record.DisplayName = contact.DisplayName;
        record.Phone = contact.Phone;
        record.Email = contact.Email;
        record.LinkedUserIdentityId = contact.LinkedUserIdentityId?.Value;
        record.Status = contact.Status.ToString();
        record.UpdatedAtUtc = contact.UpdatedAtUtc;
    }

    private static PersonalContact ToDomain(PersonalContactRecord record) =>
        PersonalContact.Rehydrate(
            PersonalContactId.From(record.Id),
            PlatformUserId.From(record.OwnerUserIdentityId),
            record.DisplayName,
            record.Phone,
            record.Email,
            record.LinkedUserIdentityId is Guid linked ? PlatformUserId.From(linked) : null,
            Enum.Parse<PersonalContactStatus>(record.Status, ignoreCase: true),
            record.CreatedAtUtc,
            record.UpdatedAtUtc);

    private static PersonalContactRecord ToRecord(PersonalContact contact) =>
        new()
        {
            Id = contact.Id.Value,
            OwnerUserIdentityId = contact.OwnerUserIdentityId.Value,
            DisplayName = contact.DisplayName,
            Phone = contact.Phone,
            Email = contact.Email,
            LinkedUserIdentityId = contact.LinkedUserIdentityId?.Value,
            Status = contact.Status.ToString(),
            CreatedAtUtc = contact.CreatedAtUtc,
            UpdatedAtUtc = contact.UpdatedAtUtc
        };
}

internal sealed class PersonalDebtRelationshipRepository(PlatformDbContext db) : IPersonalDebtRelationshipRepository
{
    public async Task<PersonalDebtRelationship?> GetByIdAsync(
        PersonalDebtRelationshipId id,
        CancellationToken cancellationToken = default)
    {
        var record = await db.PersonalDebtRelationships.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id.Value, cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : ToDomain(record);
    }

    public async Task<IReadOnlyList<PersonalDebtRelationship>> ListForUserAsync(
        PlatformUserId userIdentityId,
        CancellationToken cancellationToken = default)
    {
        var userId = userIdentityId.Value;
        var ownedContactIds = await db.PersonalContacts.AsNoTracking()
            .Where(c => c.OwnerUserIdentityId == userId)
            .Select(c => c.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var records = await db.PersonalDebtRelationships.AsNoTracking()
            .Where(r =>
                r.CreditorUserIdentityId == userId
                || r.DebtorUserIdentityId == userId
                || (r.CreditorContactId != null && ownedContactIds.Contains(r.CreditorContactId.Value))
                || (r.DebtorContactId != null && ownedContactIds.Contains(r.DebtorContactId.Value)))
            .OrderByDescending(r => r.UpdatedAtUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return records.Select(ToDomain).ToList();
    }

    public Task AddAsync(PersonalDebtRelationship relationship, CancellationToken cancellationToken = default)
    {
        db.PersonalDebtRelationships.Add(ToRecord(relationship));
        return Task.CompletedTask;
    }

    public async Task UpdateAsync(PersonalDebtRelationship relationship, CancellationToken cancellationToken = default)
    {
        var record = await db.PersonalDebtRelationships
            .FirstOrDefaultAsync(x => x.Id == relationship.Id.Value, cancellationToken)
            .ConfigureAwait(false);
        if (record is null)
        {
            return;
        }

        record.CreditorUserIdentityId = relationship.CreditorUserIdentityId?.Value;
        record.CreditorContactId = relationship.CreditorContactId?.Value;
        record.DebtorUserIdentityId = relationship.DebtorUserIdentityId?.Value;
        record.DebtorContactId = relationship.DebtorContactId?.Value;
        record.CurrentBalance = relationship.CurrentBalance;
        record.DueDateUtc = relationship.DueDateUtc;
        record.Status = relationship.Status.ToString();
        record.UpdatedAtUtc = relationship.UpdatedAtUtc;
        record.AggregateVersion = relationship.Version;
    }

    private static PersonalDebtRelationship ToDomain(PersonalDebtRelationshipRecord record) =>
        PersonalDebtRelationship.Rehydrate(
            PersonalDebtRelationshipId.From(record.Id),
            record.CreditorUserIdentityId is Guid cu ? PlatformUserId.From(cu) : null,
            record.CreditorContactId is Guid cc ? PersonalContactId.From(cc) : null,
            record.DebtorUserIdentityId is Guid du ? PlatformUserId.From(du) : null,
            record.DebtorContactId is Guid dc ? PersonalContactId.From(dc) : null,
            record.CurrencyCode,
            record.CurrentBalance,
            record.DueDateUtc,
            Enum.Parse<PersonalDebtRelationshipStatus>(record.Status, ignoreCase: true),
            record.CreatedAtUtc,
            record.UpdatedAtUtc,
            record.AggregateVersion);

    private static PersonalDebtRelationshipRecord ToRecord(PersonalDebtRelationship relationship) =>
        new()
        {
            Id = relationship.Id.Value,
            CreditorUserIdentityId = relationship.CreditorUserIdentityId?.Value,
            CreditorContactId = relationship.CreditorContactId?.Value,
            DebtorUserIdentityId = relationship.DebtorUserIdentityId?.Value,
            DebtorContactId = relationship.DebtorContactId?.Value,
            CurrencyCode = relationship.CurrencyCode,
            CurrentBalance = relationship.CurrentBalance,
            DueDateUtc = relationship.DueDateUtc,
            Status = relationship.Status.ToString(),
            CreatedAtUtc = relationship.CreatedAtUtc,
            UpdatedAtUtc = relationship.UpdatedAtUtc,
            AggregateVersion = relationship.Version
        };
}

internal sealed class PersonalUtangEntryRepository(PlatformDbContext db) : IPersonalUtangEntryRepository
{
    public async Task<IReadOnlyList<PersonalUtangEntry>> ListByRelationshipAsync(
        PersonalDebtRelationshipId relationshipId,
        CancellationToken cancellationToken = default)
    {
        var records = await db.PersonalUtangEntries.AsNoTracking()
            .Where(x => x.RelationshipId == relationshipId.Value)
            .OrderBy(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return records.Select(ToDomain).ToList();
    }

    public Task AddAsync(PersonalUtangEntry entry, CancellationToken cancellationToken = default)
    {
        db.PersonalUtangEntries.Add(ToRecord(entry));
        return Task.CompletedTask;
    }

    private static PersonalUtangEntry ToDomain(PersonalUtangEntryRecord record) =>
        PersonalUtangEntry.Rehydrate(
            PersonalUtangEntryId.From(record.Id),
            PersonalDebtRelationshipId.From(record.RelationshipId),
            Enum.Parse<PersonalUtangEntryType>(record.EntryType, ignoreCase: true),
            record.Amount,
            record.SignedDelta,
            record.BalanceAfter,
            record.Notes,
            record.DueDateUtc,
            PlatformUserId.From(record.CreatedByUserIdentityId),
            record.CreatedAtUtc);

    private static PersonalUtangEntryRecord ToRecord(PersonalUtangEntry entry) =>
        new()
        {
            Id = entry.Id.Value,
            RelationshipId = entry.RelationshipId.Value,
            EntryType = entry.EntryType.ToString(),
            Amount = entry.Amount,
            SignedDelta = entry.SignedDelta,
            BalanceAfter = entry.BalanceAfter,
            Notes = entry.Notes,
            DueDateUtc = entry.DueDateUtc,
            CreatedByUserIdentityId = entry.CreatedByUserIdentityId.Value,
            CreatedAtUtc = entry.CreatedAtUtc
        };
}

internal sealed class PersonalUtangInvitationRepository(PlatformDbContext db) : IPersonalUtangInvitationRepository
{
    public async Task<PersonalUtangInvitation?> GetByIdAsync(
        PersonalUtangInvitationId id,
        CancellationToken cancellationToken = default)
    {
        var record = await db.PersonalUtangInvitations.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id.Value, cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : ToDomain(record);
    }

    public async Task<PersonalUtangInvitation?> FindPendingByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default)
    {
        var record = await db.PersonalUtangInvitations.AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.TokenHash == tokenHash && x.Status == nameof(PersonalUtangInvitationStatus.Pending),
                cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : ToDomain(record);
    }

    public async Task<PersonalUtangInvitation?> FindPendingByRelationshipAndContactAsync(
        PersonalDebtRelationshipId relationshipId,
        PersonalContactId contactId,
        CancellationToken cancellationToken = default)
    {
        var record = await db.PersonalUtangInvitations.AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.DebtRelationshipId == relationshipId.Value
                    && x.InviteeContactId == contactId.Value
                    && x.Status == nameof(PersonalUtangInvitationStatus.Pending),
                cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : ToDomain(record);
    }

    public async Task<IReadOnlyList<PersonalUtangInvitation>> ListSentByUserAsync(
        PlatformUserId invitedByUserIdentityId,
        CancellationToken cancellationToken = default)
    {
        var records = await db.PersonalUtangInvitations.AsNoTracking()
            .Where(x => x.InvitedByUserIdentityId == invitedByUserIdentityId.Value)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return records.Select(ToDomain).ToList();
    }

    public async Task<IReadOnlyList<PersonalUtangInvitation>> ListPendingForEmailAsync(
        string normalizedEmail,
        CancellationToken cancellationToken = default)
    {
        var records = await db.PersonalUtangInvitations.AsNoTracking()
            .Where(x =>
                x.Status == nameof(PersonalUtangInvitationStatus.Pending)
                && x.InviteTargetNormalizedEmail == normalizedEmail)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return records.Select(ToDomain).ToList();
    }

    public Task AddAsync(PersonalUtangInvitation invitation, CancellationToken cancellationToken = default)
    {
        db.PersonalUtangInvitations.Add(ToRecord(invitation));
        return Task.CompletedTask;
    }

    public async Task UpdateAsync(PersonalUtangInvitation invitation, CancellationToken cancellationToken = default)
    {
        var record = await db.PersonalUtangInvitations
            .FirstOrDefaultAsync(x => x.Id == invitation.Id.Value, cancellationToken)
            .ConfigureAwait(false);
        if (record is null)
        {
            return;
        }

        record.InviteTargetNormalizedEmail = invitation.InviteTargetNormalizedEmail;
        record.InviteTargetPhone = invitation.InviteTargetPhone;
        record.Status = invitation.Status.ToString();
        record.TokenHash = invitation.TokenHash;
        record.UpdatedAtUtc = invitation.UpdatedAtUtc;
        record.ExpiresAtUtc = invitation.ExpiresAtUtc;
        record.AcceptedAtUtc = invitation.AcceptedAtUtc;
        record.DeclinedAtUtc = invitation.DeclinedAtUtc;
        record.RevokedAtUtc = invitation.RevokedAtUtc;
        record.AcceptedByUserIdentityId = invitation.AcceptedByUserIdentityId?.Value;
    }

    private static PersonalUtangInvitation ToDomain(PersonalUtangInvitationRecord record) =>
        PersonalUtangInvitation.Rehydrate(
            PersonalUtangInvitationId.From(record.Id),
            PersonalDebtRelationshipId.From(record.DebtRelationshipId),
            PersonalContactId.From(record.InviteeContactId),
            PlatformUserId.From(record.InvitedByUserIdentityId),
            record.InviteTargetNormalizedEmail,
            record.InviteTargetPhone,
            Enum.Parse<PersonalUtangInvitationStatus>(record.Status, ignoreCase: true),
            record.TokenHash,
            record.CreatedAtUtc,
            record.UpdatedAtUtc,
            record.ExpiresAtUtc,
            record.AcceptedAtUtc,
            record.DeclinedAtUtc,
            record.RevokedAtUtc,
            record.AcceptedByUserIdentityId is Guid a ? PlatformUserId.From(a) : null);

    private static PersonalUtangInvitationRecord ToRecord(PersonalUtangInvitation invitation) =>
        new()
        {
            Id = invitation.Id.Value,
            DebtRelationshipId = invitation.DebtRelationshipId.Value,
            InviteeContactId = invitation.InviteeContactId.Value,
            InvitedByUserIdentityId = invitation.InvitedByUserIdentityId.Value,
            InviteTargetNormalizedEmail = invitation.InviteTargetNormalizedEmail,
            InviteTargetPhone = invitation.InviteTargetPhone,
            Status = invitation.Status.ToString(),
            TokenHash = invitation.TokenHash,
            CreatedAtUtc = invitation.CreatedAtUtc,
            UpdatedAtUtc = invitation.UpdatedAtUtc,
            ExpiresAtUtc = invitation.ExpiresAtUtc,
            AcceptedAtUtc = invitation.AcceptedAtUtc,
            DeclinedAtUtc = invitation.DeclinedAtUtc,
            RevokedAtUtc = invitation.RevokedAtUtc,
            AcceptedByUserIdentityId = invitation.AcceptedByUserIdentityId?.Value
        };
}

internal sealed class PersonalReminderRepository(PlatformDbContext db) : IPersonalReminderRepository
{
    public async Task<PersonalReminder?> GetByIdAsync(PersonalReminderId id, CancellationToken cancellationToken = default)
    {
        var record = await db.PersonalReminders.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id.Value, cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : ToDomain(record);
    }

    public async Task<IReadOnlyList<PersonalReminder>> ListByRelationshipAsync(
        PersonalDebtRelationshipId relationshipId,
        CancellationToken cancellationToken = default)
    {
        var records = await db.PersonalReminders.AsNoTracking()
            .Where(x => x.DebtRelationshipId == relationshipId.Value)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return records.Select(ToDomain).ToList();
    }

    public async Task<IReadOnlyList<PersonalReminder>> ListDueAsync(
        DateTimeOffset asOfUtc,
        int take,
        CancellationToken cancellationToken = default)
    {
        var records = await db.PersonalReminders.AsNoTracking()
            .Where(x =>
                x.Status == nameof(PersonalReminderStatus.Scheduled)
                && x.NextDeliveryAtUtc != null
                && x.NextDeliveryAtUtc <= asOfUtc)
            .OrderBy(x => x.NextDeliveryAtUtc)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return records.Select(ToDomain).ToList();
    }

    public async Task<int> CountDeliveriesSinceAsync(
        PersonalDebtRelationshipId relationshipId,
        DateTimeOffset sinceUtc,
        CancellationToken cancellationToken = default) =>
        await db.PersonalReminders.AsNoTracking()
            .CountAsync(
                x => x.DebtRelationshipId == relationshipId.Value
                    && x.DeliveredAtUtc != null
                    && x.DeliveredAtUtc >= sinceUtc,
                cancellationToken)
            .ConfigureAwait(false);

    public async Task<DateTimeOffset?> GetLastDeliveryAtAsync(
        PersonalDebtRelationshipId relationshipId,
        CancellationToken cancellationToken = default) =>
        await db.PersonalReminders.AsNoTracking()
            .Where(x => x.DebtRelationshipId == relationshipId.Value && x.DeliveredAtUtc != null)
            .MaxAsync(x => (DateTimeOffset?)x.DeliveredAtUtc, cancellationToken)
            .ConfigureAwait(false);

    public Task AddAsync(PersonalReminder reminder, CancellationToken cancellationToken = default)
    {
        db.PersonalReminders.Add(ToRecord(reminder));
        return Task.CompletedTask;
    }

    public async Task UpdateAsync(PersonalReminder reminder, CancellationToken cancellationToken = default)
    {
        var record = await db.PersonalReminders
            .FirstOrDefaultAsync(x => x.Id == reminder.Id.Value, cancellationToken)
            .ConfigureAwait(false);
        if (record is null)
        {
            return;
        }

        record.Message = reminder.Message;
        record.ScheduledForUtc = reminder.ScheduledForUtc;
        record.NextDeliveryAtUtc = reminder.NextDeliveryAtUtc;
        record.Status = reminder.Status.ToString();
        record.UpdatedAtUtc = reminder.UpdatedAtUtc;
        record.DeliveredAtUtc = reminder.DeliveredAtUtc;
        record.DeliveryAttemptCount = reminder.DeliveryAttemptCount;
    }

    private static PersonalReminder ToDomain(PersonalReminderRecord record) =>
        PersonalReminder.Rehydrate(
            PersonalReminderId.From(record.Id),
            PersonalDebtRelationshipId.From(record.DebtRelationshipId),
            PlatformUserId.From(record.CreatedByUserIdentityId),
            Enum.Parse<PersonalReminderScheduleType>(record.ScheduleType, ignoreCase: true),
            record.Message,
            record.ScheduledForUtc,
            record.NextDeliveryAtUtc,
            Enum.Parse<PersonalReminderStatus>(record.Status, ignoreCase: true),
            record.CreatedAtUtc,
            record.UpdatedAtUtc,
            record.DeliveredAtUtc,
            record.DeliveryAttemptCount);

    private static PersonalReminderRecord ToRecord(PersonalReminder reminder) =>
        new()
        {
            Id = reminder.Id.Value,
            DebtRelationshipId = reminder.DebtRelationshipId.Value,
            CreatedByUserIdentityId = reminder.CreatedByUserIdentityId.Value,
            ScheduleType = reminder.ScheduleType.ToString(),
            Message = reminder.Message,
            ScheduledForUtc = reminder.ScheduledForUtc,
            NextDeliveryAtUtc = reminder.NextDeliveryAtUtc,
            Status = reminder.Status.ToString(),
            CreatedAtUtc = reminder.CreatedAtUtc,
            UpdatedAtUtc = reminder.UpdatedAtUtc,
            DeliveredAtUtc = reminder.DeliveredAtUtc,
            DeliveryAttemptCount = reminder.DeliveryAttemptCount
        };
}

internal sealed class PersonalInAppNotificationRepository(PlatformDbContext db) : IPersonalInAppNotificationRepository
{
    public async Task<PersonalInAppNotification?> GetByIdAsync(
        PersonalInAppNotificationId id,
        CancellationToken cancellationToken = default)
    {
        var record = await db.PersonalInAppNotifications.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id.Value, cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : ToDomain(record);
    }

    public async Task<IReadOnlyList<PersonalInAppNotification>> ListForUserAsync(
        PlatformUserId recipientUserIdentityId,
        int take,
        CancellationToken cancellationToken = default)
    {
        var records = await db.PersonalInAppNotifications.AsNoTracking()
            .Where(x => x.RecipientUserIdentityId == recipientUserIdentityId.Value)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return records.Select(ToDomain).ToList();
    }

    public Task AddAsync(PersonalInAppNotification notification, CancellationToken cancellationToken = default)
    {
        db.PersonalInAppNotifications.Add(ToRecord(notification));
        return Task.CompletedTask;
    }

    public async Task UpdateAsync(PersonalInAppNotification notification, CancellationToken cancellationToken = default)
    {
        var record = await db.PersonalInAppNotifications
            .FirstOrDefaultAsync(x => x.Id == notification.Id.Value, cancellationToken)
            .ConfigureAwait(false);
        if (record is null)
        {
            return;
        }

        record.IsRead = notification.IsRead;
        record.ReadAtUtc = notification.ReadAtUtc;
    }

    private static PersonalInAppNotification ToDomain(PersonalInAppNotificationRecord record) =>
        PersonalInAppNotification.Rehydrate(
            PersonalInAppNotificationId.From(record.Id),
            PlatformUserId.From(record.RecipientUserIdentityId),
            record.Title,
            record.Preview,
            record.RelatedType,
            record.RelatedId,
            record.IsRead,
            record.CreatedAtUtc,
            record.ReadAtUtc);

    private static PersonalInAppNotificationRecord ToRecord(PersonalInAppNotification notification) =>
        new()
        {
            Id = notification.Id.Value,
            RecipientUserIdentityId = notification.RecipientUserIdentityId.Value,
            Title = notification.Title,
            Preview = notification.Preview,
            RelatedType = notification.RelatedType,
            RelatedId = notification.RelatedId,
            IsRead = notification.IsRead,
            CreatedAtUtc = notification.CreatedAtUtc,
            ReadAtUtc = notification.ReadAtUtc
        };
}

internal sealed class PersonalNotificationDeliveryRepository(PlatformDbContext db)
    : IPersonalNotificationDeliveryRepository
{
    public async Task<IReadOnlyList<PersonalNotificationDelivery>> ListByReminderAsync(
        PersonalReminderId reminderId,
        CancellationToken cancellationToken = default)
    {
        var records = await db.PersonalNotificationDeliveries.AsNoTracking()
            .Where(x => x.ReminderId == reminderId.Value)
            .OrderByDescending(x => x.AttemptedAtUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return records.Select(ToDomain).ToList();
    }

    public async Task<IReadOnlyList<PersonalNotificationDelivery>> ListForRecipientAsync(
        PlatformUserId recipientUserIdentityId,
        int take,
        CancellationToken cancellationToken = default)
    {
        var records = await db.PersonalNotificationDeliveries.AsNoTracking()
            .Where(x => x.RecipientUserIdentityId == recipientUserIdentityId.Value)
            .OrderByDescending(x => x.AttemptedAtUtc)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return records.Select(ToDomain).ToList();
    }

    public Task AddAsync(PersonalNotificationDelivery delivery, CancellationToken cancellationToken = default)
    {
        db.PersonalNotificationDeliveries.Add(ToRecord(delivery));
        return Task.CompletedTask;
    }

    public async Task UpdateAsync(PersonalNotificationDelivery delivery, CancellationToken cancellationToken = default)
    {
        var record = await db.PersonalNotificationDeliveries
            .FirstOrDefaultAsync(x => x.Id == delivery.Id.Value, cancellationToken)
            .ConfigureAwait(false);
        if (record is null)
        {
            return;
        }

        record.Status = delivery.Status.ToString();
        record.DeliveredAtUtc = delivery.DeliveredAtUtc;
        record.FailureReason = delivery.FailureReason;
    }

    private static PersonalNotificationDelivery ToDomain(PersonalNotificationDeliveryRecord record) =>
        PersonalNotificationDelivery.Rehydrate(
            PersonalNotificationDeliveryId.From(record.Id),
            record.ReminderId is Guid r ? PersonalReminderId.From(r) : null,
            record.NotificationId is Guid n ? PersonalInAppNotificationId.From(n) : null,
            PlatformUserId.From(record.RecipientUserIdentityId),
            Enum.Parse<PersonalNotificationChannel>(record.Channel, ignoreCase: true),
            Enum.Parse<PersonalNotificationDeliveryStatus>(record.Status, ignoreCase: true),
            record.PreviewText,
            record.AttemptedAtUtc,
            record.DeliveredAtUtc,
            record.FailureReason);

    private static PersonalNotificationDeliveryRecord ToRecord(PersonalNotificationDelivery delivery) =>
        new()
        {
            Id = delivery.Id.Value,
            ReminderId = delivery.ReminderId?.Value,
            NotificationId = delivery.NotificationId?.Value,
            RecipientUserIdentityId = delivery.RecipientUserIdentityId.Value,
            Channel = delivery.Channel.ToString(),
            Status = delivery.Status.ToString(),
            PreviewText = delivery.PreviewText,
            AttemptedAtUtc = delivery.AttemptedAtUtc,
            DeliveredAtUtc = delivery.DeliveredAtUtc,
            FailureReason = delivery.FailureReason
        };
}
