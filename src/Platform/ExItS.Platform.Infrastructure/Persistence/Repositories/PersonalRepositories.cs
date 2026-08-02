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
