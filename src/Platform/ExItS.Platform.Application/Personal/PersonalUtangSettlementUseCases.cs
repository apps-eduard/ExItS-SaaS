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

public sealed record SettlePersonalDebtRelationshipRequest(
    int? ExpectedVersion,
    Guid? SettlementEntryId = null,
    string? Notes = null);

public sealed record ClosePersonalDebtRelationshipRequest(int? ExpectedVersion);

public sealed record SettlePersonalDebtRelationshipResultDto(
    string Outcome,
    PersonalDebtRelationshipSummaryDto Relationship,
    PersonalUtangEntryDto? SettlementEntry);

public sealed record ClosePersonalDebtRelationshipResultDto(
    string Outcome,
    PersonalDebtRelationshipSummaryDto Relationship);

public sealed class SettlePersonalDebtRelationship
{
    private readonly IPersonalDebtRelationshipRepository _relationships;
    private readonly IPersonalUtangEntryRepository _entries;
    private readonly IPersonalContactRepository _contacts;
    private readonly IPersonalReminderRepository _reminders;
    private readonly IAuditWriter _auditWriter;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public SettlePersonalDebtRelationship(
        IPersonalDebtRelationshipRepository relationships,
        IPersonalUtangEntryRepository entries,
        IPersonalContactRepository contacts,
        IPersonalReminderRepository reminders,
        IAuditWriter auditWriter,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _relationships = relationships;
        _entries = entries;
        _contacts = contacts;
        _reminders = reminders;
        _auditWriter = auditWriter;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<SettlePersonalDebtRelationshipResultDto>> ExecuteAsync(
        PlatformUserId actingUserIdentityId,
        Guid relationshipId,
        SettlePersonalDebtRelationshipRequest request,
        CancellationToken cancellationToken = default)
    {
        var relationship = await _relationships
            .GetByIdAsync(PersonalDebtRelationshipId.From(relationshipId), cancellationToken)
            .ConfigureAwait(false);
        if (relationship is null)
        {
            return ApplicationResult<SettlePersonalDebtRelationshipResultDto>.Failure(
                ApplicationErrorCodes.PersonalUtangRelationshipNotFound,
                "Personal debt relationship was not found.");
        }

        if (!await PersonalUtangAccess.CanViewAsync(relationship, actingUserIdentityId, _contacts, cancellationToken)
                .ConfigureAwait(false))
        {
            return ApplicationResult<SettlePersonalDebtRelationshipResultDto>.Failure(
                ApplicationErrorCodes.PersonalUtangUnauthorized,
                "Personal debt relationship is not visible to this account.");
        }

        if (relationship.Status is PersonalDebtRelationshipStatus.Archived
            or PersonalDebtRelationshipStatus.Transferred)
        {
            return ApplicationResult<SettlePersonalDebtRelationshipResultDto>.Failure(
                ApplicationErrorCodes.PersonalUtangSettlementInvalid,
                $"Cannot settle a relationship in status {relationship.Status}.");
        }

        if (request.SettlementEntryId is Guid clientEntryId && clientEntryId != Guid.Empty)
        {
            var existing = await _entries
                .GetByIdAsync(PersonalUtangEntryId.From(clientEntryId), cancellationToken)
                .ConfigureAwait(false);
            if (existing is not null)
            {
                if (!PersonalUtangIdempotency.EntryMatches(
                        existing,
                        relationship.Id,
                        PersonalUtangEntryType.Payment,
                        existing.Amount,
                        adjustmentDelta: null,
                        PersonalUtangEntryIntent.Settlement)
                    || !existing.IsSettlement)
                {
                    return ApplicationResult<SettlePersonalDebtRelationshipResultDto>.Failure(
                        ApplicationErrorCodes.PersonalUtangIdempotencyConflict,
                        "Settlement entry identity was reused with a conflicting payload.");
                }

                return ApplicationResult<SettlePersonalDebtRelationshipResultDto>.Success(
                    BuildSettleOutcome(relationship, existing, actingUserIdentityId));
            }
        }

        if (relationship.Status is PersonalDebtRelationshipStatus.Closed)
        {
            return ApplicationResult<SettlePersonalDebtRelationshipResultDto>.Success(
                new SettlePersonalDebtRelationshipResultDto(
                    "AlreadySettled",
                    CreatePersonalDebtRelationship.ToSummary(relationship, actingUserIdentityId),
                    SettlementEntry: null));
        }

        var history = await _entries.ListByRelationshipAsync(relationship.Id, cancellationToken)
            .ConfigureAwait(false);
        if (history.Any(e => e.Status is PersonalUtangEntryStatus.Pending))
        {
            return ApplicationResult<SettlePersonalDebtRelationshipResultDto>.Failure(
                ApplicationErrorCodes.PersonalUtangPendingBlocksSettlement,
                "Unresolved pending entries must be resolved before settling.");
        }

        PersonalUtangEntryId? stickyId = request.SettlementEntryId is Guid sid && sid != Guid.Empty
            ? PersonalUtangEntryId.From(sid)
            : null;

        try
        {
            var settlementAmount = relationship.CurrentBalance;
            var entry = relationship.RecordSettlementPayment(
                actingUserIdentityId,
                _clock.UtcNow,
                request.ExpectedVersion,
                stickyId,
                request.Notes);

            if (!PersonalUtangIdempotency.EntryMatches(
                    entry,
                    relationship.Id,
                    PersonalUtangEntryType.Payment,
                    settlementAmount,
                    adjustmentDelta: null,
                    PersonalUtangEntryIntent.Settlement))
            {
                return ApplicationResult<SettlePersonalDebtRelationshipResultDto>.Failure(
                    ApplicationErrorCodes.PersonalUtangIdempotencyConflict,
                    "Settlement entry identity was reused with a conflicting payload.");
            }

            await _relationships.UpdateAsync(relationship, cancellationToken).ConfigureAwait(false);
            await _entries.AddAsync(entry, cancellationToken).ConfigureAwait(false);

            if (entry.Status is PersonalUtangEntryStatus.Confirmed && relationship.CurrentBalance == 0m)
            {
                relationship.CloseAsSettled(_clock.UtcNow, expectedVersion: null, hasUnresolvedPending: false);
                await _relationships.UpdateAsync(relationship, cancellationToken).ConfigureAwait(false);
                await CancelScheduledRemindersAsync(relationship.Id, cancellationToken).ConfigureAwait(false);
            }

            try
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (PersistenceConflictException) when (stickyId is not null)
            {
                var raced = await _entries.GetByIdAsync(stickyId, cancellationToken).ConfigureAwait(false);
                if (raced is not null
                    && PersonalUtangIdempotency.EntryMatches(
                        raced,
                        relationship.Id,
                        PersonalUtangEntryType.Payment,
                        raced.Amount,
                        adjustmentDelta: null,
                        PersonalUtangEntryIntent.Settlement))
                {
                    var fresh = await _relationships.GetByIdAsync(relationship.Id, cancellationToken)
                        .ConfigureAwait(false);
                    return ApplicationResult<SettlePersonalDebtRelationshipResultDto>.Success(
                        BuildSettleOutcome(fresh ?? relationship, raced, actingUserIdentityId));
                }

                throw;
            }

            await _auditWriter.WriteAsync(
                $"platform-user:{actingUserIdentityId.Value:D}",
                AuditActorType.PlatformUser,
                PlatformAuditActions.PersonalUtangSettled,
                nameof(PersonalDebtRelationship),
                relationship.Id.Value.ToString("D"),
                AuditOutcome.Succeeded,
                summary: entry.Status is PersonalUtangEntryStatus.Pending
                    ? "Personal Utang settlement proposed."
                    : "Personal Utang settled and closed.",
                cancellationToken: cancellationToken).ConfigureAwait(false);

            return ApplicationResult<SettlePersonalDebtRelationshipResultDto>.Success(
                BuildSettleOutcome(relationship, entry, actingUserIdentityId));
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<SettlePersonalDebtRelationshipResultDto>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<SettlePersonalDebtRelationshipResultDto>.Failure(
                PersonalUtangSettlementErrors.Map(ex),
                ex.Message);
        }
    }

    private static SettlePersonalDebtRelationshipResultDto BuildSettleOutcome(
        PersonalDebtRelationship relationship,
        PersonalUtangEntry entry,
        PlatformUserId actingUserIdentityId)
    {
        var outcome = entry.Status is PersonalUtangEntryStatus.Pending
            ? "AwaitingCounterpartyConfirmation"
            : "Completed";

        return new SettlePersonalDebtRelationshipResultDto(
            outcome,
            CreatePersonalDebtRelationship.ToSummary(relationship, actingUserIdentityId),
            RecordPersonalUtangEntry.ToDto(entry, actingUserIdentityId, relationship.IsSharedLinked));
    }

    private async Task CancelScheduledRemindersAsync(
        PersonalDebtRelationshipId relationshipId,
        CancellationToken cancellationToken)
    {
        var reminders = await _reminders.ListByRelationshipAsync(relationshipId, cancellationToken)
            .ConfigureAwait(false);
        foreach (var reminder in reminders.Where(r => r.Status is PersonalReminderStatus.Scheduled))
        {
            reminder.Cancel(_clock.UtcNow);
            await _reminders.UpdateAsync(reminder, cancellationToken).ConfigureAwait(false);
        }
    }
}

public sealed class ClosePersonalDebtRelationship
{
    private readonly IPersonalDebtRelationshipRepository _relationships;
    private readonly IPersonalUtangEntryRepository _entries;
    private readonly IPersonalContactRepository _contacts;
    private readonly IPersonalReminderRepository _reminders;
    private readonly IAuditWriter _auditWriter;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public ClosePersonalDebtRelationship(
        IPersonalDebtRelationshipRepository relationships,
        IPersonalUtangEntryRepository entries,
        IPersonalContactRepository contacts,
        IPersonalReminderRepository reminders,
        IAuditWriter auditWriter,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _relationships = relationships;
        _entries = entries;
        _contacts = contacts;
        _reminders = reminders;
        _auditWriter = auditWriter;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<ClosePersonalDebtRelationshipResultDto>> ExecuteAsync(
        PlatformUserId actingUserIdentityId,
        Guid relationshipId,
        ClosePersonalDebtRelationshipRequest request,
        CancellationToken cancellationToken = default)
    {
        var relationship = await _relationships
            .GetByIdAsync(PersonalDebtRelationshipId.From(relationshipId), cancellationToken)
            .ConfigureAwait(false);
        if (relationship is null)
        {
            return ApplicationResult<ClosePersonalDebtRelationshipResultDto>.Failure(
                ApplicationErrorCodes.PersonalUtangRelationshipNotFound,
                "Personal debt relationship was not found.");
        }

        if (!await PersonalUtangAccess.CanViewAsync(relationship, actingUserIdentityId, _contacts, cancellationToken)
                .ConfigureAwait(false))
        {
            return ApplicationResult<ClosePersonalDebtRelationshipResultDto>.Failure(
                ApplicationErrorCodes.PersonalUtangUnauthorized,
                "Personal debt relationship is not visible to this account.");
        }

        if (relationship.Status is PersonalDebtRelationshipStatus.Archived
            or PersonalDebtRelationshipStatus.Transferred)
        {
            return ApplicationResult<ClosePersonalDebtRelationshipResultDto>.Failure(
                ApplicationErrorCodes.PersonalUtangCloseInvalid,
                $"Cannot close a relationship in status {relationship.Status}.");
        }

        if (relationship.Status is PersonalDebtRelationshipStatus.Closed)
        {
            return ApplicationResult<ClosePersonalDebtRelationshipResultDto>.Success(
                new ClosePersonalDebtRelationshipResultDto(
                    "AlreadySettled",
                    CreatePersonalDebtRelationship.ToSummary(relationship, actingUserIdentityId)));
        }

        var history = await _entries.ListByRelationshipAsync(relationship.Id, cancellationToken)
            .ConfigureAwait(false);
        var hasPending = history.Any(e => e.Status is PersonalUtangEntryStatus.Pending);

        try
        {
            relationship.CloseAsSettled(_clock.UtcNow, request.ExpectedVersion, hasPending);
            await _relationships.UpdateAsync(relationship, cancellationToken).ConfigureAwait(false);

            var reminders = await _reminders.ListByRelationshipAsync(relationship.Id, cancellationToken)
                .ConfigureAwait(false);
            foreach (var reminder in reminders.Where(r => r.Status is PersonalReminderStatus.Scheduled))
            {
                reminder.Cancel(_clock.UtcNow);
                await _reminders.UpdateAsync(reminder, cancellationToken).ConfigureAwait(false);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            await _auditWriter.WriteAsync(
                $"platform-user:{actingUserIdentityId.Value:D}",
                AuditActorType.PlatformUser,
                PlatformAuditActions.PersonalUtangClosed,
                nameof(PersonalDebtRelationship),
                relationship.Id.Value.ToString("D"),
                AuditOutcome.Succeeded,
                summary: "Personal Utang relationship closed as settled.",
                cancellationToken: cancellationToken).ConfigureAwait(false);

            return ApplicationResult<ClosePersonalDebtRelationshipResultDto>.Success(
                new ClosePersonalDebtRelationshipResultDto(
                    "Closed",
                    CreatePersonalDebtRelationship.ToSummary(relationship, actingUserIdentityId)));
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<ClosePersonalDebtRelationshipResultDto>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<ClosePersonalDebtRelationshipResultDto>.Failure(
                PersonalUtangSettlementErrors.Map(ex),
                ex.Message);
        }
    }
}

internal static class PersonalUtangSettlementErrors
{
    public static string Map(DomainException ex) => ex.ErrorCode switch
    {
        DomainErrorCodes.PersonalUtangConcurrencyConflict => ApplicationErrorCodes.ConcurrencyConflict,
        DomainErrorCodes.PersonalUtangSettlementInvalid => ApplicationErrorCodes.PersonalUtangSettlementInvalid,
        DomainErrorCodes.PersonalUtangSettlementStale => ApplicationErrorCodes.PersonalUtangSettlementStale,
        DomainErrorCodes.PersonalUtangPendingBlocksSettlement =>
            ApplicationErrorCodes.PersonalUtangPendingBlocksSettlement,
        DomainErrorCodes.PersonalUtangCloseInvalid => ApplicationErrorCodes.PersonalUtangCloseInvalid,
        DomainErrorCodes.PersonalUtangUnauthorized => ApplicationErrorCodes.PersonalUtangUnauthorized,
        _ => ex.ErrorCode
    };
}
