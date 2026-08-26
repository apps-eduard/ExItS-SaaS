using ExItS.Platform.Application.Audit;
using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Identity;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Audit;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Personal;

namespace ExItS.Platform.Application.Personal;

public sealed class ConfirmPersonalUtangEntry
{
    private readonly IPersonalDebtRelationshipRepository _relationships;
    private readonly IPersonalUtangEntryRepository _entries;
    private readonly IPersonalContactRepository _contacts;
    private readonly IPersonalAccountSettingsRepository _settings;
    private readonly IPersonalInAppNotificationRepository _notifications;
    private readonly IAuditWriter _auditWriter;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public ConfirmPersonalUtangEntry(
        IPersonalDebtRelationshipRepository relationships,
        IPersonalUtangEntryRepository entries,
        IPersonalContactRepository contacts,
        IPersonalAccountSettingsRepository settings,
        IPersonalInAppNotificationRepository notifications,
        IAuditWriter auditWriter,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _relationships = relationships;
        _entries = entries;
        _contacts = contacts;
        _settings = settings;
        _notifications = notifications;
        _auditWriter = auditWriter;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<PersonalUtangEntryDto>> ExecuteAsync(
        PlatformUserId actingUserIdentityId,
        Guid relationshipId,
        Guid entryId,
        ConfirmPersonalUtangEntryRequest request,
        CancellationToken cancellationToken = default)
    {
        var loaded = await PersonalUtangEntryAccess.LoadForMutationAsync(
            actingUserIdentityId,
            relationshipId,
            entryId,
            _relationships,
            _entries,
            _contacts,
            cancellationToken).ConfigureAwait(false);
        if (!loaded.IsSuccess)
        {
            return ApplicationResult<PersonalUtangEntryDto>.Failure(loaded.ErrorCode!, loaded.ErrorMessage!);
        }

        var (relationship, entry) = loaded.Value!;

        try
        {
            var priorStatus = entry.Status;
            relationship.ConfirmEntry(entry, actingUserIdentityId, _clock.UtcNow, request.ExpectedVersion);
            await _relationships.UpdateAsync(relationship, cancellationToken).ConfigureAwait(false);
            await _entries.UpdateAsync(entry, cancellationToken).ConfigureAwait(false);
            if (priorStatus is PersonalUtangEntryStatus.Pending)
            {
                await PersonalUtangEntryNotifications.NotifyProposerResolvedAsync(
                    entry,
                    title: "Personal Utang entry confirmed",
                    preview: "Your Personal Utang entry was confirmed.",
                    _settings,
                    _notifications,
                    _clock,
                    cancellationToken).ConfigureAwait(false);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            if (priorStatus is PersonalUtangEntryStatus.Pending)
            {
                await _auditWriter.WriteAsync(
                    $"platform-user:{actingUserIdentityId.Value:D}",
                    AuditActorType.PlatformUser,
                    PlatformAuditActions.PersonalUtangEntryConfirmed,
                    nameof(PersonalUtangEntry),
                    entry.Id.Value.ToString("D"),
                    AuditOutcome.Succeeded,
                    summary: "Personal Utang entry confirmed.",
                    cancellationToken: cancellationToken).ConfigureAwait(false);
            }

            return ApplicationResult<PersonalUtangEntryDto>.Success(
                RecordPersonalUtangEntry.ToDto(entry, actingUserIdentityId, relationship.IsSharedLinked));
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<PersonalUtangEntryDto>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<PersonalUtangEntryDto>.Failure(
                PersonalUtangEntryErrors.Map(ex),
                ex.Message);
        }
    }
}

public sealed class DisputePersonalUtangEntry
{
    private readonly IPersonalDebtRelationshipRepository _relationships;
    private readonly IPersonalUtangEntryRepository _entries;
    private readonly IPersonalContactRepository _contacts;
    private readonly IPersonalAccountSettingsRepository _settings;
    private readonly IPersonalInAppNotificationRepository _notifications;
    private readonly IAuditWriter _auditWriter;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public DisputePersonalUtangEntry(
        IPersonalDebtRelationshipRepository relationships,
        IPersonalUtangEntryRepository entries,
        IPersonalContactRepository contacts,
        IPersonalAccountSettingsRepository settings,
        IPersonalInAppNotificationRepository notifications,
        IAuditWriter auditWriter,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _relationships = relationships;
        _entries = entries;
        _contacts = contacts;
        _settings = settings;
        _notifications = notifications;
        _auditWriter = auditWriter;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<PersonalUtangEntryDto>> ExecuteAsync(
        PlatformUserId actingUserIdentityId,
        Guid relationshipId,
        Guid entryId,
        DisputePersonalUtangEntryRequest request,
        CancellationToken cancellationToken = default)
    {
        var loaded = await PersonalUtangEntryAccess.LoadForMutationAsync(
            actingUserIdentityId,
            relationshipId,
            entryId,
            _relationships,
            _entries,
            _contacts,
            cancellationToken).ConfigureAwait(false);
        if (!loaded.IsSuccess)
        {
            return ApplicationResult<PersonalUtangEntryDto>.Failure(loaded.ErrorCode!, loaded.ErrorMessage!);
        }

        var (relationship, entry) = loaded.Value!;

        try
        {
            var priorStatus = entry.Status;
            relationship.DisputeEntry(
                entry,
                actingUserIdentityId,
                _clock.UtcNow,
                request.ExpectedVersion,
                request.Reason);
            await _relationships.UpdateAsync(relationship, cancellationToken).ConfigureAwait(false);
            await _entries.UpdateAsync(entry, cancellationToken).ConfigureAwait(false);
            if (priorStatus is PersonalUtangEntryStatus.Pending)
            {
                await PersonalUtangEntryNotifications.NotifyProposerResolvedAsync(
                    entry,
                    title: "Personal Utang entry disputed",
                    preview: "Your Personal Utang entry was disputed.",
                    _settings,
                    _notifications,
                    _clock,
                    cancellationToken).ConfigureAwait(false);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            if (priorStatus is PersonalUtangEntryStatus.Pending)
            {
                await _auditWriter.WriteAsync(
                    $"platform-user:{actingUserIdentityId.Value:D}",
                    AuditActorType.PlatformUser,
                    PlatformAuditActions.PersonalUtangEntryDisputed,
                    nameof(PersonalUtangEntry),
                    entry.Id.Value.ToString("D"),
                    AuditOutcome.Succeeded,
                    summary: "Personal Utang entry disputed.",
                    cancellationToken: cancellationToken).ConfigureAwait(false);
            }

            return ApplicationResult<PersonalUtangEntryDto>.Success(
                RecordPersonalUtangEntry.ToDto(entry, actingUserIdentityId, relationship.IsSharedLinked));
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<PersonalUtangEntryDto>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<PersonalUtangEntryDto>.Failure(
                PersonalUtangEntryErrors.Map(ex),
                ex.Message);
        }
    }
}

public sealed class CancelPersonalUtangEntry
{
    private readonly IPersonalDebtRelationshipRepository _relationships;
    private readonly IPersonalUtangEntryRepository _entries;
    private readonly IPersonalContactRepository _contacts;
    private readonly IPersonalAccountSettingsRepository _settings;
    private readonly IPersonalInAppNotificationRepository _notifications;
    private readonly IAuditWriter _auditWriter;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public CancelPersonalUtangEntry(
        IPersonalDebtRelationshipRepository relationships,
        IPersonalUtangEntryRepository entries,
        IPersonalContactRepository contacts,
        IPersonalAccountSettingsRepository settings,
        IPersonalInAppNotificationRepository notifications,
        IAuditWriter auditWriter,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _relationships = relationships;
        _entries = entries;
        _contacts = contacts;
        _settings = settings;
        _notifications = notifications;
        _auditWriter = auditWriter;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<PersonalUtangEntryDto>> ExecuteAsync(
        PlatformUserId actingUserIdentityId,
        Guid relationshipId,
        Guid entryId,
        CancelPersonalUtangEntryRequest request,
        CancellationToken cancellationToken = default)
    {
        var loaded = await PersonalUtangEntryAccess.LoadForMutationAsync(
            actingUserIdentityId,
            relationshipId,
            entryId,
            _relationships,
            _entries,
            _contacts,
            cancellationToken).ConfigureAwait(false);
        if (!loaded.IsSuccess)
        {
            return ApplicationResult<PersonalUtangEntryDto>.Failure(loaded.ErrorCode!, loaded.ErrorMessage!);
        }

        var (relationship, entry) = loaded.Value!;

        try
        {
            var priorStatus = entry.Status;
            relationship.CancelPendingEntry(entry, actingUserIdentityId, _clock.UtcNow, request.ExpectedVersion);
            await _relationships.UpdateAsync(relationship, cancellationToken).ConfigureAwait(false);
            await _entries.UpdateAsync(entry, cancellationToken).ConfigureAwait(false);
            if (priorStatus is PersonalUtangEntryStatus.Pending)
            {
                await PersonalUtangEntryNotifications.NotifyCounterpartyCancelledAsync(
                    relationship,
                    entry,
                    actingUserIdentityId,
                    _settings,
                    _notifications,
                    _clock,
                    cancellationToken).ConfigureAwait(false);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            if (priorStatus is PersonalUtangEntryStatus.Pending)
            {
                await _auditWriter.WriteAsync(
                    $"platform-user:{actingUserIdentityId.Value:D}",
                    AuditActorType.PlatformUser,
                    PlatformAuditActions.PersonalUtangEntryCancelled,
                    nameof(PersonalUtangEntry),
                    entry.Id.Value.ToString("D"),
                    AuditOutcome.Succeeded,
                    summary: "Pending Personal Utang entry cancelled.",
                    cancellationToken: cancellationToken).ConfigureAwait(false);
            }

            return ApplicationResult<PersonalUtangEntryDto>.Success(
                RecordPersonalUtangEntry.ToDto(entry, actingUserIdentityId, relationship.IsSharedLinked));
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<PersonalUtangEntryDto>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<PersonalUtangEntryDto>.Failure(
                PersonalUtangEntryErrors.Map(ex),
                ex.Message);
        }
    }
}

internal static class PersonalUtangEntryErrors
{
    public static string Map(DomainException ex) => ex.ErrorCode switch
    {
        DomainErrorCodes.PersonalUtangConcurrencyConflict => ApplicationErrorCodes.ConcurrencyConflict,
        DomainErrorCodes.PersonalUtangEntryInvalid => ApplicationErrorCodes.PersonalUtangEntryInvalid,
        DomainErrorCodes.PersonalUtangUnauthorized => ApplicationErrorCodes.PersonalUtangUnauthorized,
        _ => ex.ErrorCode
    };
}

internal static class PersonalUtangEntryAccess
{
    public static async Task<ApplicationResult<(PersonalDebtRelationship Relationship, PersonalUtangEntry Entry)>>
        LoadForMutationAsync(
            PlatformUserId actingUserIdentityId,
            Guid relationshipId,
            Guid entryId,
            IPersonalDebtRelationshipRepository relationships,
            IPersonalUtangEntryRepository entries,
            IPersonalContactRepository contacts,
            CancellationToken cancellationToken)
    {
        var relationship = await relationships
            .GetByIdAsync(PersonalDebtRelationshipId.From(relationshipId), cancellationToken)
            .ConfigureAwait(false);
        if (relationship is null)
        {
            return ApplicationResult<(PersonalDebtRelationship, PersonalUtangEntry)>.Failure(
                ApplicationErrorCodes.PersonalUtangRelationshipNotFound,
                "Personal debt relationship was not found.");
        }

        if (!await PersonalUtangAccess.CanViewAsync(relationship, actingUserIdentityId, contacts, cancellationToken)
                .ConfigureAwait(false))
        {
            return ApplicationResult<(PersonalDebtRelationship, PersonalUtangEntry)>.Failure(
                ApplicationErrorCodes.PersonalUtangUnauthorized,
                "Personal debt relationship is not visible to this account.");
        }

        var entry = await entries
            .GetByIdAsync(PersonalUtangEntryId.From(entryId), cancellationToken)
            .ConfigureAwait(false);
        if (entry is null || entry.RelationshipId != relationship.Id)
        {
            return ApplicationResult<(PersonalDebtRelationship, PersonalUtangEntry)>.Failure(
                ApplicationErrorCodes.PersonalUtangEntryInvalid,
                "Personal Utang entry was not found on this relationship.");
        }

        return ApplicationResult<(PersonalDebtRelationship, PersonalUtangEntry)>.Success((relationship, entry));
    }
}

internal static class PersonalUtangEntryNotifications
{
    public static async Task NotifyCounterpartyPendingAsync(
        PersonalDebtRelationship relationship,
        PersonalUtangEntry entry,
        PlatformUserId proposerUserIdentityId,
        IPlatformUserRepository users,
        IPersonalAccountSettingsRepository settings,
        IPersonalInAppNotificationRepository notifications,
        IClock clock,
        CancellationToken cancellationToken)
    {
        var counterparty = relationship.GetCounterpartyUserIdentityId(proposerUserIdentityId);
        if (counterparty is null)
        {
            return;
        }

        var proposer = await users.GetByIdAsync(proposerUserIdentityId, cancellationToken).ConfigureAwait(false);
        var proposerName = string.IsNullOrWhiteSpace(proposer?.DisplayName)
            ? "Someone"
            : proposer!.DisplayName.Trim();

        await TryNotifyAsync(
            counterparty,
            title: "Utang entry to review",
            preview: $"{proposerName} recorded an Utang entry for your review.",
            relatedType: "PersonalDebtRelationship",
            relatedId: relationship.Id.Value.ToString("D"),
            settings,
            notifications,
            clock,
            cancellationToken).ConfigureAwait(false);
    }

    public static async Task NotifyProposerResolvedAsync(
        PersonalUtangEntry entry,
        string title,
        string preview,
        IPersonalAccountSettingsRepository settings,
        IPersonalInAppNotificationRepository notifications,
        IClock clock,
        CancellationToken cancellationToken) =>
        await TryNotifyAsync(
            entry.CreatedByUserIdentityId,
            title,
            preview,
            relatedType: "PersonalDebtRelationship",
            relatedId: entry.RelationshipId.Value.ToString("D"),
            settings,
            notifications,
            clock,
            cancellationToken).ConfigureAwait(false);

    public static async Task NotifyCounterpartyCancelledAsync(
        PersonalDebtRelationship relationship,
        PersonalUtangEntry entry,
        PlatformUserId proposerUserIdentityId,
        IPersonalAccountSettingsRepository settings,
        IPersonalInAppNotificationRepository notifications,
        IClock clock,
        CancellationToken cancellationToken)
    {
        var counterparty = relationship.GetCounterpartyUserIdentityId(proposerUserIdentityId);
        if (counterparty is null)
        {
            return;
        }

        await TryNotifyAsync(
            counterparty,
            title: "Personal Utang entry cancelled",
            preview: "A pending Personal Utang entry was cancelled.",
            relatedType: "PersonalDebtRelationship",
            relatedId: relationship.Id.Value.ToString("D"),
            settings,
            notifications,
            clock,
            cancellationToken).ConfigureAwait(false);
    }

    private static async Task TryNotifyAsync(
        PlatformUserId recipient,
        string title,
        string preview,
        string relatedType,
        string relatedId,
        IPersonalAccountSettingsRepository settings,
        IPersonalInAppNotificationRepository notifications,
        IClock clock,
        CancellationToken cancellationToken)
    {
        var prefs = await settings.GetByUserAsync(recipient, cancellationToken).ConfigureAwait(false)
            ?? PersonalAccountSettings.CreateDefaults(recipient, clock.UtcNow);
        if (!prefs.InAppNotificationsEnabled)
        {
            return;
        }

        var notification = PersonalInAppNotification.Create(
            recipient,
            title,
            preview,
            relatedType,
            clock.UtcNow,
            relatedId);
        await notifications.AddAsync(notification, cancellationToken).ConfigureAwait(false);
    }
}
