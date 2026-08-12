using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Personal;
using ExItS.Platform.Domain.Catalog;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;
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

    public async Task<PersonalContact?> FindActiveByOwnerAndNormalizedEmailAsync(
        PlatformUserId ownerUserIdentityId,
        string normalizedEmail,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(normalizedEmail);
        var record = await db.PersonalContacts.AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.OwnerUserIdentityId == ownerUserIdentityId.Value
                     && x.Email == normalizedEmail
                     && x.Status == nameof(PersonalContactStatus.Active),
                cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : ToDomain(record);
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
        record.DestinationOrganizationId = relationship.DestinationOrganizationId;
        record.DestinationCreditCustomerId = relationship.DestinationCreditCustomerId;
        record.MigrationBatchId = relationship.MigrationBatchId;
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
            record.AggregateVersion,
            record.DestinationOrganizationId,
            record.DestinationCreditCustomerId,
            record.MigrationBatchId);

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
            DestinationOrganizationId = relationship.DestinationOrganizationId,
            DestinationCreditCustomerId = relationship.DestinationCreditCustomerId,
            MigrationBatchId = relationship.MigrationBatchId,
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

internal sealed class PersonalUtangMigrationBatchRepository(PlatformDbContext db) : IPersonalUtangMigrationBatchRepository
{
    public async Task<PersonalUtangMigrationBatch?> GetByIdAsync(
        PersonalUtangMigrationBatchId id,
        CancellationToken cancellationToken = default)
    {
        var record = await db.PersonalUtangMigrationBatches.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id.Value, cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : ToDomain(record);
    }

    public async Task<PersonalUtangMigrationBatch?> FindByOwnerAndIdempotencyKeyAsync(
        PlatformUserId ownerUserIdentityId,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        var key = idempotencyKey.Trim();
        var record = await db.PersonalUtangMigrationBatches.AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.OwnerUserIdentityId == ownerUserIdentityId.Value && x.IdempotencyKey == key,
                cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : ToDomain(record);
    }

    public Task AddAsync(PersonalUtangMigrationBatch batch, CancellationToken cancellationToken = default)
    {
        db.PersonalUtangMigrationBatches.Add(ToRecord(batch));
        return Task.CompletedTask;
    }

    public async Task UpdateAsync(PersonalUtangMigrationBatch batch, CancellationToken cancellationToken = default)
    {
        var record = await db.PersonalUtangMigrationBatches
            .FirstOrDefaultAsync(x => x.Id == batch.Id.Value, cancellationToken)
            .ConfigureAwait(false);
        if (record is null)
        {
            return;
        }

        record.IdempotencyKey = batch.IdempotencyKey;
        record.Status = batch.Status.ToString();
        record.ExecutedAtUtc = batch.ExecutedAtUtc;
    }

    private static PersonalUtangMigrationBatch ToDomain(PersonalUtangMigrationBatchRecord record) =>
        PersonalUtangMigrationBatch.Rehydrate(
            PersonalUtangMigrationBatchId.From(record.Id),
            PlatformUserId.From(record.OwnerUserIdentityId),
            PlatformOrganizationId.From(record.DestinationOrganizationId),
            record.DestinationProductCode,
            record.IdempotencyKey,
            Enum.Parse<PersonalUtangMigrationBatchStatus>(record.Status, ignoreCase: true),
            record.EffectiveMigrationDateUtc,
            record.IncludeContact,
            record.IncludeOpeningBalance,
            record.IncludeSelectedHistory,
            record.IncludeDueDatesAndNotes,
            Enum.Parse<PersonalUtangSourceDisposition>(record.SourceDisposition, ignoreCase: true),
            record.LinkedParticipantConsentAcknowledged,
            record.ConfirmationToken,
            record.PreviewedAtUtc,
            record.ExecutedAtUtc,
            record.CreatedAtUtc);

    private static PersonalUtangMigrationBatchRecord ToRecord(PersonalUtangMigrationBatch batch) =>
        new()
        {
            Id = batch.Id.Value,
            OwnerUserIdentityId = batch.OwnerUserIdentityId.Value,
            DestinationOrganizationId = batch.DestinationOrganizationId.Value,
            DestinationProductCode = batch.DestinationProductCode,
            IdempotencyKey = batch.IdempotencyKey,
            Status = batch.Status.ToString(),
            EffectiveMigrationDateUtc = batch.EffectiveMigrationDateUtc,
            IncludeContact = batch.IncludeContact,
            IncludeOpeningBalance = batch.IncludeOpeningBalance,
            IncludeSelectedHistory = batch.IncludeSelectedHistory,
            IncludeDueDatesAndNotes = batch.IncludeDueDatesAndNotes,
            SourceDisposition = batch.SourceDisposition.ToString(),
            LinkedParticipantConsentAcknowledged = batch.LinkedParticipantConsentAcknowledged,
            ConfirmationToken = batch.ConfirmationToken,
            PreviewedAtUtc = batch.PreviewedAtUtc,
            ExecutedAtUtc = batch.ExecutedAtUtc,
            CreatedAtUtc = batch.CreatedAtUtc
        };
}

internal sealed class PersonalUtangMigrationItemRepository(PlatformDbContext db) : IPersonalUtangMigrationItemRepository
{
    public async Task<IReadOnlyList<PersonalUtangMigrationItem>> ListByBatchAsync(
        PersonalUtangMigrationBatchId batchId,
        CancellationToken cancellationToken = default)
    {
        var records = await db.PersonalUtangMigrationItems.AsNoTracking()
            .Where(x => x.BatchId == batchId.Value)
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return records.Select(ToDomain).ToList();
    }

    public async Task<PersonalUtangMigrationItem?> FindMigratedByDestinationAndSourceAsync(
        PlatformOrganizationId destinationOrganizationId,
        PersonalUtangMigrationSourceType sourceType,
        Guid sourceRecordId,
        CancellationToken cancellationToken = default)
    {
        var sourceTypeName = sourceType.ToString();
        var migrated = nameof(PersonalUtangMigrationItemStatus.Migrated);
        var record = await (
                from item in db.PersonalUtangMigrationItems.AsNoTracking()
                join batch in db.PersonalUtangMigrationBatches.AsNoTracking() on item.BatchId equals batch.Id
                where batch.DestinationOrganizationId == destinationOrganizationId.Value
                      && item.SourceType == sourceTypeName
                      && item.SourceRecordId == sourceRecordId
                      && item.Status == migrated
                select item)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : ToDomain(record);
    }

    public Task AddAsync(PersonalUtangMigrationItem item, CancellationToken cancellationToken = default)
    {
        db.PersonalUtangMigrationItems.Add(ToRecord(item));
        return Task.CompletedTask;
    }

    public async Task UpdateAsync(PersonalUtangMigrationItem item, CancellationToken cancellationToken = default)
    {
        var record = await db.PersonalUtangMigrationItems
            .FirstOrDefaultAsync(x => x.Id == item.Id.Value, cancellationToken)
            .ConfigureAwait(false);
        if (record is null)
        {
            return;
        }

        record.DestinationType = item.DestinationType?.ToString();
        record.DestinationRecordId = item.DestinationRecordId;
        record.Status = item.Status.ToString();
        record.BlockedReason = item.BlockedReason;
    }

    private static PersonalUtangMigrationItem ToDomain(PersonalUtangMigrationItemRecord record) =>
        PersonalUtangMigrationItem.Rehydrate(
            PersonalUtangMigrationItemId.From(record.Id),
            PersonalUtangMigrationBatchId.From(record.BatchId),
            Enum.Parse<PersonalUtangMigrationSourceType>(record.SourceType, ignoreCase: true),
            record.SourceRecordId,
            string.IsNullOrWhiteSpace(record.DestinationType)
                ? null
                : Enum.Parse<PersonalUtangMigrationDestinationType>(record.DestinationType, ignoreCase: true),
            record.DestinationRecordId,
            record.OpeningBalanceAmount,
            record.CurrencyCode,
            record.NotesSnapshot,
            record.DueDateUtc,
            record.HistoryEntryIdsCsv,
            Enum.Parse<PersonalUtangMigrationItemStatus>(record.Status, ignoreCase: true),
            record.BlockedReason);

    private static PersonalUtangMigrationItemRecord ToRecord(PersonalUtangMigrationItem item) =>
        new()
        {
            Id = item.Id.Value,
            BatchId = item.BatchId.Value,
            SourceType = item.SourceType.ToString(),
            SourceRecordId = item.SourceRecordId,
            DestinationType = item.DestinationType?.ToString(),
            DestinationRecordId = item.DestinationRecordId,
            OpeningBalanceAmount = item.OpeningBalanceAmount,
            CurrencyCode = item.CurrencyCode,
            NotesSnapshot = item.NotesSnapshot,
            DueDateUtc = item.DueDateUtc,
            HistoryEntryIdsCsv = item.HistoryEntryIdsCsv,
            Status = item.Status.ToString(),
            BlockedReason = item.BlockedReason
        };
}

internal sealed class PersonalFeatureDefinitionRepository(PlatformDbContext db) : IPersonalFeatureDefinitionRepository
{
    public async Task<PersonalFeatureDefinition?> GetByCodeAsync(
        FeatureCode featureCode,
        CancellationToken cancellationToken = default)
    {
        var record = await db.PersonalFeatureDefinitions.AsNoTracking()
            .FirstOrDefaultAsync(x => x.FeatureCode == featureCode.Value, cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : ToDomain(record);
    }

    public Task AddAsync(PersonalFeatureDefinition definition, CancellationToken cancellationToken = default)
    {
        db.PersonalFeatureDefinitions.Add(ToRecord(definition));
        return Task.CompletedTask;
    }

    public async Task UpdateAsync(PersonalFeatureDefinition definition, CancellationToken cancellationToken = default)
    {
        var record = await db.PersonalFeatureDefinitions
            .FirstOrDefaultAsync(x => x.FeatureCode == definition.FeatureCode.Value, cancellationToken)
            .ConfigureAwait(false);
        if (record is null)
        {
            return;
        }

        record.DisplayName = definition.DisplayName;
        record.IsActive = definition.IsActive;
        record.RewardPointsPrice = definition.RewardPointsPrice;
        record.UpdatedAtUtc = definition.UpdatedAtUtc;
    }

    private static PersonalFeatureDefinition ToDomain(PersonalFeatureDefinitionRecord record) =>
        PersonalFeatureDefinition.Rehydrate(
            FeatureCode.Create(record.FeatureCode),
            record.DisplayName,
            record.IsActive,
            record.RewardPointsPrice,
            record.CreatedAtUtc,
            record.UpdatedAtUtc);

    private static PersonalFeatureDefinitionRecord ToRecord(PersonalFeatureDefinition definition) =>
        new()
        {
            FeatureCode = definition.FeatureCode.Value,
            DisplayName = definition.DisplayName,
            IsActive = definition.IsActive,
            RewardPointsPrice = definition.RewardPointsPrice,
            CreatedAtUtc = definition.CreatedAtUtc,
            UpdatedAtUtc = definition.UpdatedAtUtc
        };
}

internal sealed class PersonalFeatureEntitlementRepository(PlatformDbContext db) : IPersonalFeatureEntitlementRepository
{
    public async Task<PersonalFeatureEntitlement?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var record = await db.PersonalFeatureEntitlements.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : ToDomain(record);
    }

    public async Task<IReadOnlyList<PersonalFeatureEntitlement>> ListByUserAndFeatureAsync(
        PlatformUserId personalUserId,
        FeatureCode featureCode,
        CancellationToken cancellationToken = default)
    {
        var rows = await db.PersonalFeatureEntitlements.AsNoTracking()
            .Where(x => x.PersonalUserId == personalUserId.Value && x.FeatureCode == featureCode.Value)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return rows.Select(ToDomain).ToList();
    }

    public Task AddAsync(PersonalFeatureEntitlement entitlement, CancellationToken cancellationToken = default)
    {
        db.PersonalFeatureEntitlements.Add(ToRecord(entitlement));
        return Task.CompletedTask;
    }

    public async Task UpdateAsync(PersonalFeatureEntitlement entitlement, CancellationToken cancellationToken = default)
    {
        var record = await db.PersonalFeatureEntitlements
            .FirstOrDefaultAsync(x => x.Id == entitlement.Id, cancellationToken)
            .ConfigureAwait(false);
        if (record is null)
        {
            return;
        }

        record.EndsAtUtc = entitlement.EndsAtUtc;
        record.Status = entitlement.Status.ToString();
        record.RevokedAtUtc = entitlement.RevokedAtUtc;
        record.RevocationReason = entitlement.RevocationReason;
    }

    private static PersonalFeatureEntitlement ToDomain(PersonalFeatureEntitlementRecord record) =>
        PersonalFeatureEntitlement.Rehydrate(
            record.Id,
            PlatformUserId.From(record.PersonalUserId),
            FeatureCode.Create(record.FeatureCode),
            record.StartsAtUtc,
            record.EndsAtUtc,
            Enum.Parse<PersonalFeatureEntitlementStatus>(record.Status, ignoreCase: true),
            Enum.Parse<PersonalFeatureGrantSource>(record.GrantSource, ignoreCase: true),
            record.CreatedAtUtc,
            record.RevokedAtUtc,
            record.RevocationReason);

    private static PersonalFeatureEntitlementRecord ToRecord(PersonalFeatureEntitlement entitlement) =>
        new()
        {
            Id = entitlement.Id,
            PersonalUserId = entitlement.PersonalUserId.Value,
            FeatureCode = entitlement.FeatureCode.Value,
            StartsAtUtc = entitlement.StartsAtUtc,
            EndsAtUtc = entitlement.EndsAtUtc,
            Status = entitlement.Status.ToString(),
            GrantSource = entitlement.GrantSource.ToString(),
            CreatedAtUtc = entitlement.CreatedAtUtc,
            RevokedAtUtc = entitlement.RevokedAtUtc,
            RevocationReason = entitlement.RevocationReason
        };
}

internal sealed class PersonalRewardBalanceRepository(PlatformDbContext db) : IPersonalRewardBalanceRepository
{
    public async Task<PersonalRewardBalance?> GetByUserAsync(
        PlatformUserId personalUserId,
        CancellationToken cancellationToken = default)
    {
        var record = await db.PersonalRewardBalances.AsNoTracking()
            .FirstOrDefaultAsync(x => x.PersonalUserId == personalUserId.Value, cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : ToDomain(record);
    }

    public Task AddAsync(PersonalRewardBalance balance, CancellationToken cancellationToken = default)
    {
        db.PersonalRewardBalances.Add(ToRecord(balance));
        return Task.CompletedTask;
    }

    public async Task UpdateAsync(
        PersonalRewardBalance balance,
        int expectedVersion,
        CancellationToken cancellationToken = default)
    {
        var record = await db.PersonalRewardBalances
            .FirstOrDefaultAsync(x => x.PersonalUserId == balance.PersonalUserId.Value, cancellationToken)
            .ConfigureAwait(false);
        if (record is null)
        {
            throw new PersistenceConflictException(
                ApplicationErrorCodes.PersonalRewardBalanceConflict,
                "Personal reward balance was not found.");
        }

        if (record.Version != expectedVersion)
        {
            throw new PersistenceConflictException(
                ApplicationErrorCodes.PersonalRewardBalanceConflict,
                "Personal reward balance was modified concurrently.");
        }

        record.AvailablePoints = balance.AvailablePoints;
        record.UpdatedAtUtc = balance.UpdatedAtUtc;
        record.Version = balance.Version;
    }

    private static PersonalRewardBalance ToDomain(PersonalRewardBalanceRecord record) =>
        PersonalRewardBalance.Rehydrate(
            PlatformUserId.From(record.PersonalUserId),
            record.AvailablePoints,
            record.CreatedAtUtc,
            record.UpdatedAtUtc,
            record.Version);

    private static PersonalRewardBalanceRecord ToRecord(PersonalRewardBalance balance) =>
        new()
        {
            PersonalUserId = balance.PersonalUserId.Value,
            AvailablePoints = balance.AvailablePoints,
            CreatedAtUtc = balance.CreatedAtUtc,
            UpdatedAtUtc = balance.UpdatedAtUtc,
            Version = balance.Version
        };
}

internal sealed class PersonalRewardTransactionRepository(PlatformDbContext db) : IPersonalRewardTransactionRepository
{
    public Task AddAsync(PersonalRewardTransaction transaction, CancellationToken cancellationToken = default)
    {
        db.PersonalRewardTransactions.Add(ToRecord(transaction));
        return Task.CompletedTask;
    }

    public async Task<PersonalRewardTransaction?> FindByIdempotencyKeyAsync(
        PlatformUserId personalUserId,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        var record = await db.PersonalRewardTransactions.AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.PersonalUserId == personalUserId.Value && x.IdempotencyKey == idempotencyKey,
                cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : ToDomain(record);
    }

    public async Task<(IReadOnlyList<PersonalRewardTransaction> Items, int TotalCount)> ListByUserDescendingAsync(
        PlatformUserId personalUserId,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = db.PersonalRewardTransactions.AsNoTracking()
            .Where(x => x.PersonalUserId == personalUserId.Value);

        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var rows = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .ThenByDescending(x => x.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return (rows.Select(ToDomain).ToList(), total);
    }

    private static PersonalRewardTransaction ToDomain(PersonalRewardTransactionRecord record) =>
        PersonalRewardTransaction.Rehydrate(
            record.Id,
            PlatformUserId.From(record.PersonalUserId),
            Enum.Parse<PersonalRewardTransactionType>(record.TransactionType, ignoreCase: true),
            record.Points,
            record.SignedDelta,
            record.BalanceAfter,
            record.Source,
            record.Reason,
            record.ReferenceId,
            record.IdempotencyKey,
            record.CreatedAtUtc);

    private static PersonalRewardTransactionRecord ToRecord(PersonalRewardTransaction transaction) =>
        new()
        {
            Id = transaction.Id,
            PersonalUserId = transaction.PersonalUserId.Value,
            TransactionType = transaction.TransactionType.ToString(),
            Points = transaction.Points,
            SignedDelta = transaction.SignedDelta,
            BalanceAfter = transaction.BalanceAfter,
            Source = transaction.Source,
            Reason = transaction.Reason,
            ReferenceId = transaction.ReferenceId,
            IdempotencyKey = transaction.IdempotencyKey,
            CreatedAtUtc = transaction.CreatedAtUtc
        };
}

internal sealed class PersonalRewardClaimRepository(PlatformDbContext db) : IPersonalRewardClaimRepository
{
    public async Task<PersonalRewardClaim?> FindByUserTypeAndKeyAsync(
        PlatformUserId personalUserId,
        string claimType,
        string claimKey,
        CancellationToken cancellationToken = default)
    {
        var record = await db.PersonalRewardClaims.AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.PersonalUserId == personalUserId.Value
                    && x.ClaimType == claimType
                    && x.ClaimKey == claimKey,
                cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : ToDomain(record);
    }

    public Task AddAsync(PersonalRewardClaim claim, CancellationToken cancellationToken = default)
    {
        db.PersonalRewardClaims.Add(ToRecord(claim));
        return Task.CompletedTask;
    }

    private static PersonalRewardClaim ToDomain(PersonalRewardClaimRecord record) =>
        PersonalRewardClaim.Rehydrate(
            record.Id,
            PlatformUserId.From(record.PersonalUserId),
            record.ClaimType,
            record.ClaimKey,
            record.PointsAwarded,
            record.RewardTransactionId,
            record.ClaimedAtUtc);

    private static PersonalRewardClaimRecord ToRecord(PersonalRewardClaim claim) =>
        new()
        {
            Id = claim.Id,
            PersonalUserId = claim.PersonalUserId.Value,
            ClaimType = claim.ClaimType,
            ClaimKey = claim.ClaimKey,
            PointsAwarded = claim.PointsAwarded,
            RewardTransactionId = claim.RewardTransactionId,
            ClaimedAtUtc = claim.ClaimedAtUtc
        };
}
