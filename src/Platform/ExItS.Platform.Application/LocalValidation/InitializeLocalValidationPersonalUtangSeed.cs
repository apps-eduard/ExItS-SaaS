using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Identity;
using ExItS.Platform.Application.Personal;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Personal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ExItS.Platform.Application.LocalValidation;

/// <summary>
/// Deterministic Personal Utang ledger seed for Local Validation Personal accounts (Luis ↔ Sofia).
/// Idempotent. Never runs unless LocalValidation:Enabled.
/// </summary>
public sealed class InitializeLocalValidationPersonalUtangSeed
{
    private readonly LocalValidationOptions _options;
    private readonly IPlatformUserRepository _users;
    private readonly CreatePersonalDebtRelationship _createRelationship;
    private readonly RecordPersonalUtangEntry _recordEntry;
    private readonly ConfirmPersonalUtangEntry _confirmEntry;
    private readonly CreatePersonalReminder _createReminder;
    private readonly IPersonalDebtRelationshipRepository _relationships;
    private readonly IPersonalUtangEntryRepository _entries;
    private readonly IPersonalReminderRepository _reminders;
    private readonly IClock _clock;
    private readonly ILogger<InitializeLocalValidationPersonalUtangSeed> _logger;

    public InitializeLocalValidationPersonalUtangSeed(
        IOptions<LocalValidationOptions> options,
        IPlatformUserRepository users,
        CreatePersonalDebtRelationship createRelationship,
        RecordPersonalUtangEntry recordEntry,
        ConfirmPersonalUtangEntry confirmEntry,
        CreatePersonalReminder createReminder,
        IPersonalDebtRelationshipRepository relationships,
        IPersonalUtangEntryRepository entries,
        IPersonalReminderRepository reminders,
        IClock clock,
        ILogger<InitializeLocalValidationPersonalUtangSeed> logger)
    {
        _options = options.Value;
        _users = users;
        _createRelationship = createRelationship;
        _recordEntry = recordEntry;
        _confirmEntry = confirmEntry;
        _createReminder = createReminder;
        _relationships = relationships;
        _entries = entries;
        _reminders = reminders;
        _clock = clock;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            return;
        }

        var luis = await RequireUserAsync("luis.navarro", cancellationToken).ConfigureAwait(false);
        var sofia = await RequireUserAsync("sofia.ramos", cancellationToken).ConfigureAwait(false);

        var dueLuisToSofia = _clock.UtcNow.AddDays(14);
        var dueSofiaToLuis = _clock.UtcNow.AddDays(21);

        var luisToSofia = await EnsureRelationshipAsync(
            actingUserId: luis.Id,
            creditorUserId: luis.Id,
            debtorUserId: sofia.Id,
            dueDateUtc: dueLuisToSofia,
            initialLoan: LocalValidationPersonalUtangSeedMarkers.LuisToSofiaLoan,
            notes: LocalValidationPersonalUtangSeedMarkers.LuisToSofiaNotes,
            cancellationToken).ConfigureAwait(false);

        await EnsurePaymentAsync(
            actingUserId: sofia.Id,
            relationshipId: luisToSofia.Id.Value,
            amount: LocalValidationPersonalUtangSeedMarkers.LuisToSofiaPayment,
            notes: LocalValidationPersonalUtangSeedMarkers.LuisToSofiaPaymentNotes,
            cancellationToken).ConfigureAwait(false);

        await EnsureReminderAsync(
            actingUserId: luis.Id,
            relationshipId: luisToSofia.Id.Value,
            scheduledForUtc: dueLuisToSofia,
            message: "Local Validation: collect remaining balance from Sofia Ramos.",
            cancellationToken).ConfigureAwait(false);

        var sofiaToLuis = await EnsureRelationshipAsync(
            actingUserId: sofia.Id,
            creditorUserId: sofia.Id,
            debtorUserId: luis.Id,
            dueDateUtc: dueSofiaToLuis,
            initialLoan: LocalValidationPersonalUtangSeedMarkers.SofiaToLuisLoan,
            notes: LocalValidationPersonalUtangSeedMarkers.SofiaToLuisNotes,
            cancellationToken).ConfigureAwait(false);

        await EnsureReminderAsync(
            actingUserId: sofia.Id,
            relationshipId: sofiaToLuis.Id.Value,
            scheduledForUtc: dueSofiaToLuis,
            message: "Local Validation: Sofia ↔ Luis loan reminder-ready.",
            cancellationToken).ConfigureAwait(false);

        var luisToSofiaBalance = (await _relationships.GetByIdAsync(luisToSofia.Id, cancellationToken).ConfigureAwait(false))
            ?.CurrentBalance;
        var sofiaToLuisBalance = (await _relationships.GetByIdAsync(sofiaToLuis.Id, cancellationToken).ConfigureAwait(false))
            ?.CurrentBalance;

        _logger.LogInformation(
            "Local Validation Personal Utang seed ready. Luis→Sofia balance={LuisToSofia} Sofia→Luis balance={SofiaToLuis} (ledger-derived).",
            luisToSofiaBalance,
            sofiaToLuisBalance);
    }

    private async Task<PlatformUser> RequireUserAsync(string username, CancellationToken ct)
    {
        var (_, normalized) = PlatformUser.NormalizeUsername(username);
        var user = await _users.GetByNormalizedUsernameAsync(normalized, ct).ConfigureAwait(false);
        if (user is null)
        {
            throw new InvalidOperationException(
                $"Local Validation Personal Utang seed requires identity '{username}' to exist.");
        }

        return user;
    }

    private async Task<PersonalDebtRelationship> EnsureRelationshipAsync(
        PlatformUserId actingUserId,
        PlatformUserId creditorUserId,
        PlatformUserId debtorUserId,
        DateTimeOffset dueDateUtc,
        decimal initialLoan,
        string notes,
        CancellationToken ct)
    {
        var existingForActor = await _relationships.ListForUserAsync(actingUserId, ct).ConfigureAwait(false);
        var existing = existingForActor.FirstOrDefault(r =>
            r.Status == PersonalDebtRelationshipStatus.Active
            && r.CreditorUserIdentityId == creditorUserId
            && r.DebtorUserIdentityId == debtorUserId);

        if (existing is not null)
        {
            var entries = await _entries.ListByRelationshipAsync(existing.Id, ct).ConfigureAwait(false);
            if (entries.Any(e =>
                    e.EntryType == PersonalUtangEntryType.Loan
                    && string.Equals(e.Notes, notes, StringComparison.Ordinal)))
            {
                await ConfirmPendingByNotesAsync(debtorUserId, existing.Id.Value, notes, ct)
                    .ConfigureAwait(false);
                return (await _relationships.GetByIdAsync(existing.Id, ct).ConfigureAwait(false)) ?? existing;
            }
        }

        var created = await _createRelationship
            .ExecuteAsync(
                actingUserId,
                new CreatePersonalDebtRelationshipRequest(
                    CreditorUserIdentityId: creditorUserId.Value,
                    CreditorContactId: null,
                    DebtorUserIdentityId: debtorUserId.Value,
                    DebtorContactId: null,
                    CurrencyCode: "PHP",
                    DueDateUtc: dueDateUtc,
                    InitialLoanAmount: initialLoan,
                    InitialLoanNotes: notes),
                ct)
            .ConfigureAwait(false);

        if (!created.IsSuccess || created.Value is null)
        {
            throw new InvalidOperationException(
                $"Local Validation Personal Utang relationship seed failed: {created.ErrorCode} {created.ErrorMessage}");
        }

        var reloaded = await _relationships
            .GetByIdAsync(PersonalDebtRelationshipId.From(created.Value.Id), ct)
            .ConfigureAwait(false);
        if (reloaded is null)
        {
            throw new InvalidOperationException("Local Validation Personal Utang relationship was created but not found.");
        }

        // Shared ledger: initial loan starts Pending — counterparty confirms for deterministic seed balances.
        await ConfirmPendingByNotesAsync(
            confirmerUserId: debtorUserId,
            relationshipId: reloaded.Id.Value,
            notes: notes,
            ct).ConfigureAwait(false);

        reloaded = await _relationships.GetByIdAsync(reloaded.Id, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Local Validation Personal Utang relationship missing after confirm.");

        return reloaded;
    }

    private async Task EnsurePaymentAsync(
        PlatformUserId actingUserId,
        Guid relationshipId,
        decimal amount,
        string notes,
        CancellationToken ct)
    {
        var entries = await _entries
            .ListByRelationshipAsync(PersonalDebtRelationshipId.From(relationshipId), ct)
            .ConfigureAwait(false);
        if (entries.Any(e =>
                e.EntryType == PersonalUtangEntryType.Payment
                && string.Equals(e.Notes, notes, StringComparison.Ordinal)
                && e.Status == PersonalUtangEntryStatus.Confirmed))
        {
            return;
        }

        var relationship = await _relationships
            .GetByIdAsync(PersonalDebtRelationshipId.From(relationshipId), ct)
            .ConfigureAwait(false);
        if (relationship is null)
        {
            throw new InvalidOperationException("Local Validation Personal Utang payment target relationship missing.");
        }

        var pendingExisting = entries.FirstOrDefault(e =>
            e.EntryType == PersonalUtangEntryType.Payment
            && string.Equals(e.Notes, notes, StringComparison.Ordinal)
            && e.Status == PersonalUtangEntryStatus.Pending);
        if (pendingExisting is not null)
        {
            var counterparty = relationship.GetCounterpartyUserIdentityId(actingUserId)
                ?? relationship.CreditorUserIdentityId
                ?? throw new InvalidOperationException("Local Validation payment confirm requires counterparty.");
            await ConfirmEntryAsync(counterparty, relationshipId, pendingExisting.Id.Value, relationship.Version, ct)
                .ConfigureAwait(false);
            return;
        }

        var recorded = await _recordEntry
            .ExecuteAsync(
                actingUserId,
                relationshipId,
                new RecordPersonalUtangEntryRequest(
                    EntryType: nameof(PersonalUtangEntryType.Payment),
                    Amount: amount,
                    AdjustmentDelta: null,
                    ExpectedVersion: relationship.Version,
                    Notes: notes,
                    DueDateUtc: null),
                ct)
            .ConfigureAwait(false);

        if (!recorded.IsSuccess || recorded.Value is null)
        {
            throw new InvalidOperationException(
                $"Local Validation Personal Utang payment seed failed: {recorded.ErrorCode} {recorded.ErrorMessage}");
        }

        relationship = await _relationships
            .GetByIdAsync(PersonalDebtRelationshipId.From(relationshipId), ct)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException("Local Validation Personal Utang payment target relationship missing.");

        var confirmer = relationship.GetCounterpartyUserIdentityId(actingUserId)
            ?? throw new InvalidOperationException("Local Validation payment confirm requires counterparty.");
        await ConfirmEntryAsync(confirmer, relationshipId, recorded.Value.Id, relationship.Version, ct)
            .ConfigureAwait(false);
    }

    private async Task ConfirmPendingByNotesAsync(
        PlatformUserId confirmerUserId,
        Guid relationshipId,
        string notes,
        CancellationToken ct)
    {
        var relationship = await _relationships
            .GetByIdAsync(PersonalDebtRelationshipId.From(relationshipId), ct)
            .ConfigureAwait(false);
        if (relationship is null)
        {
            return;
        }

        var entries = await _entries.ListByRelationshipAsync(relationship.Id, ct).ConfigureAwait(false);
        var pending = entries.FirstOrDefault(e =>
            e.Status == PersonalUtangEntryStatus.Pending
            && string.Equals(e.Notes, notes, StringComparison.Ordinal));
        if (pending is null)
        {
            return;
        }

        await ConfirmEntryAsync(confirmerUserId, relationshipId, pending.Id.Value, relationship.Version, ct)
            .ConfigureAwait(false);
    }

    private async Task ConfirmEntryAsync(
        PlatformUserId confirmerUserId,
        Guid relationshipId,
        Guid entryId,
        int expectedVersion,
        CancellationToken ct)
    {
        var confirmed = await _confirmEntry
            .ExecuteAsync(
                confirmerUserId,
                relationshipId,
                entryId,
                new ConfirmPersonalUtangEntryRequest(ExpectedVersion: expectedVersion),
                ct)
            .ConfigureAwait(false);
        if (!confirmed.IsSuccess)
        {
            throw new InvalidOperationException(
                $"Local Validation Personal Utang confirm seed failed: {confirmed.ErrorCode} {confirmed.ErrorMessage}");
        }
    }

    private async Task EnsureReminderAsync(
        PlatformUserId actingUserId,
        Guid relationshipId,
        DateTimeOffset scheduledForUtc,
        string message,
        CancellationToken ct)
    {
        var existing = await _reminders.ListByRelationshipAsync(
            PersonalDebtRelationshipId.From(relationshipId),
            ct).ConfigureAwait(false);
        if (existing.Any(r => string.Equals(r.Message, message, StringComparison.Ordinal)))
        {
            return;
        }

        var created = await _createReminder
            .ExecuteAsync(
                actingUserId,
                relationshipId,
                new CreatePersonalReminderRequest(
                    ScheduleType: nameof(PersonalReminderScheduleType.OnDueDate),
                    ScheduledForUtc: scheduledForUtc,
                    Message: message),
                ct)
            .ConfigureAwait(false);

        if (!created.IsSuccess)
        {
            throw new InvalidOperationException(
                $"Local Validation Personal Utang reminder seed failed: {created.ErrorCode} {created.ErrorMessage}");
        }
    }
}
