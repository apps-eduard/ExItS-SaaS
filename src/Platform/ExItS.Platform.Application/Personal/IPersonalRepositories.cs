using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Personal;

namespace ExItS.Platform.Application.Personal;

public interface IPersonalAccountSettingsRepository
{
    Task<PersonalAccountSettings?> GetByUserAsync(PlatformUserId userIdentityId, CancellationToken cancellationToken = default);

    Task AddAsync(PersonalAccountSettings settings, CancellationToken cancellationToken = default);

    Task UpdateAsync(PersonalAccountSettings settings, CancellationToken cancellationToken = default);
}

public interface IPersonalContactRepository
{
    Task<PersonalContact?> GetByIdAsync(PersonalContactId id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PersonalContact>> ListByOwnerAsync(
        PlatformUserId ownerUserIdentityId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Active contact for this owner with the given already-normalized email, or null.
    /// </summary>
    Task<PersonalContact?> FindActiveByOwnerAndNormalizedEmailAsync(
        PlatformUserId ownerUserIdentityId,
        string normalizedEmail,
        CancellationToken cancellationToken = default);

    Task<PersonalContact?> FindActiveByOwnerAndResolvedUserAsync(
        PlatformUserId ownerUserIdentityId,
        PlatformUserId resolvedUserIdentityId,
        CancellationToken cancellationToken = default);

    Task<PersonalContact?> FindActiveBlockedByOwnerForUserAsync(
        PlatformUserId ownerUserIdentityId,
        PlatformUserId blockedUserIdentityId,
        CancellationToken cancellationToken = default);

    Task<PersonalContact?> FindActiveByOwnerAndLinkedUserAsync(
        PlatformUserId ownerUserIdentityId,
        PlatformUserId linkedUserIdentityId,
        CancellationToken cancellationToken = default);
    Task AddAsync(PersonalContact contact, CancellationToken cancellationToken = default);

    Task UpdateAsync(PersonalContact contact, CancellationToken cancellationToken = default);
}

public interface IPersonalConnectionRequestRepository
{
    Task<PersonalConnectionRequest?> GetByIdAsync(
        PersonalConnectionRequestId id,
        CancellationToken cancellationToken = default);

    Task<PersonalConnectionRequest?> FindPendingByRequesterAndTargetAsync(
        PlatformUserId requesterUserIdentityId,
        PlatformUserId targetUserIdentityId,
        CancellationToken cancellationToken = default);

    Task<PersonalConnectionRequest?> FindPendingBetweenUsersAsync(
        PlatformUserId userA,
        PlatformUserId userB,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PersonalConnectionRequest>> ListPendingBetweenUsersAsync(
        PlatformUserId userA,
        PlatformUserId userB,
        CancellationToken cancellationToken = default);

    Task<PersonalConnectionRequest?> FindPendingForContactAsync(
        PersonalContactId requesterContactId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PersonalConnectionRequest>> ListForUserAsync(
        PlatformUserId userIdentityId,
        CancellationToken cancellationToken = default);

    Task AddAsync(PersonalConnectionRequest request, CancellationToken cancellationToken = default);

    Task UpdateAsync(PersonalConnectionRequest request, CancellationToken cancellationToken = default);
}

public interface IPersonalDebtRelationshipRepository
{
    Task<PersonalDebtRelationship?> GetByIdAsync(PersonalDebtRelationshipId id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PersonalDebtRelationship>> ListForUserAsync(
        PlatformUserId userIdentityId,
        CancellationToken cancellationToken = default);

    Task AddAsync(PersonalDebtRelationship relationship, CancellationToken cancellationToken = default);

    Task UpdateAsync(PersonalDebtRelationship relationship, CancellationToken cancellationToken = default);
}

public interface IPersonalUtangEntryRepository
{
    Task<PersonalUtangEntry?> GetByIdAsync(
        PersonalUtangEntryId id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PersonalUtangEntry>> ListByRelationshipAsync(
        PersonalDebtRelationshipId relationshipId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts Pending entries across relationships where <paramref name="userIdentityId"/> is a linked
    /// participant but not the proposer (awaiting this user's confirmation).
    /// </summary>
    Task<int> CountPendingAwaitingConfirmationAsync(
        PlatformUserId userIdentityId,
        CancellationToken cancellationToken = default);

    Task AddAsync(PersonalUtangEntry entry, CancellationToken cancellationToken = default);

    Task UpdateAsync(PersonalUtangEntry entry, CancellationToken cancellationToken = default);
}

public interface IPersonalUtangInvitationRepository
{
    Task<PersonalUtangInvitation?> GetByIdAsync(PersonalUtangInvitationId id, CancellationToken cancellationToken = default);

    Task<PersonalUtangInvitation?> FindPendingByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    Task<PersonalUtangInvitation?> FindPendingByRelationshipAndContactAsync(
        PersonalDebtRelationshipId relationshipId,
        PersonalContactId contactId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PersonalUtangInvitation>> ListSentByUserAsync(
        PlatformUserId invitedByUserIdentityId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PersonalUtangInvitation>> ListPendingForEmailAsync(
        string normalizedEmail,
        CancellationToken cancellationToken = default);

    Task AddAsync(PersonalUtangInvitation invitation, CancellationToken cancellationToken = default);

    Task UpdateAsync(PersonalUtangInvitation invitation, CancellationToken cancellationToken = default);
}

public interface IPersonalTodoRepository
{
    Task<PersonalTodo?> GetByIdAsync(PersonalTodoId id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PersonalTodo>> ListByOwnerAsync(
        PlatformUserId ownerUserIdentityId,
        PersonalTodoStatus? status = null,
        CancellationToken cancellationToken = default);

    Task AddAsync(PersonalTodo todo, CancellationToken cancellationToken = default);

    Task UpdateAsync(PersonalTodo todo, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PersonalTodo>> ListDueRemindersAsync(
        DateTimeOffset asOfUtc,
        int take,
        CancellationToken cancellationToken = default);
}

public interface IPersonalReminderRepository
{
    Task<PersonalReminder?> GetByIdAsync(PersonalReminderId id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PersonalReminder>> ListByRelationshipAsync(
        PersonalDebtRelationshipId relationshipId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PersonalReminder>> ListDueAsync(
        DateTimeOffset asOfUtc,
        int take,
        CancellationToken cancellationToken = default);

    Task<int> CountDeliveriesSinceAsync(
        PersonalDebtRelationshipId relationshipId,
        DateTimeOffset sinceUtc,
        CancellationToken cancellationToken = default);

    Task<DateTimeOffset?> GetLastDeliveryAtAsync(
        PersonalDebtRelationshipId relationshipId,
        CancellationToken cancellationToken = default);

    Task AddAsync(PersonalReminder reminder, CancellationToken cancellationToken = default);

    Task UpdateAsync(PersonalReminder reminder, CancellationToken cancellationToken = default);
}

public interface IPersonalInAppNotificationRepository
{
    Task<PersonalInAppNotification?> GetByIdAsync(
        PersonalInAppNotificationId id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PersonalInAppNotification>> ListForUserAsync(
        PlatformUserId recipientUserIdentityId,
        int take,
        CancellationToken cancellationToken = default);

    Task<PersonalInAppNotification?> FindByRecipientRelatedAsync(
        PlatformUserId recipientUserIdentityId,
        string relatedType,
        string relatedId,
        CancellationToken cancellationToken = default);

    Task AddAsync(PersonalInAppNotification notification, CancellationToken cancellationToken = default);

    Task UpdateAsync(PersonalInAppNotification notification, CancellationToken cancellationToken = default);
}

public interface IPersonalNotificationDeliveryRepository
{
    Task<IReadOnlyList<PersonalNotificationDelivery>> ListByReminderAsync(
        PersonalReminderId reminderId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PersonalNotificationDelivery>> ListForRecipientAsync(
        PlatformUserId recipientUserIdentityId,
        int take,
        CancellationToken cancellationToken = default);

    Task AddAsync(PersonalNotificationDelivery delivery, CancellationToken cancellationToken = default);

    Task UpdateAsync(PersonalNotificationDelivery delivery, CancellationToken cancellationToken = default);
}

/// <summary>Pluggable push sink. Null implementation records audit without vendor delivery.</summary>
public interface IPersonalPushNotificationSink
{
    Task<bool> TryDeliverAsync(
        PlatformUserId recipientUserIdentityId,
        string title,
        string minimizedPreview,
        CancellationToken cancellationToken = default);
}

public interface IPersonalUtangMigrationBatchRepository
{
    Task<PersonalUtangMigrationBatch?> GetByIdAsync(
        PersonalUtangMigrationBatchId id,
        CancellationToken cancellationToken = default);

    Task<PersonalUtangMigrationBatch?> FindByOwnerAndIdempotencyKeyAsync(
        PlatformUserId ownerUserIdentityId,
        string idempotencyKey,
        CancellationToken cancellationToken = default);

    Task AddAsync(PersonalUtangMigrationBatch batch, CancellationToken cancellationToken = default);

    Task UpdateAsync(PersonalUtangMigrationBatch batch, CancellationToken cancellationToken = default);
}

public interface IPersonalUtangMigrationItemRepository
{
    Task<IReadOnlyList<PersonalUtangMigrationItem>> ListByBatchAsync(
        PersonalUtangMigrationBatchId batchId,
        CancellationToken cancellationToken = default);

    Task<PersonalUtangMigrationItem?> FindMigratedByDestinationAndSourceAsync(
        PlatformOrganizationId destinationOrganizationId,
        PersonalUtangMigrationSourceType sourceType,
        Guid sourceRecordId,
        CancellationToken cancellationToken = default);

    Task AddAsync(PersonalUtangMigrationItem item, CancellationToken cancellationToken = default);

    Task UpdateAsync(PersonalUtangMigrationItem item, CancellationToken cancellationToken = default);
}
