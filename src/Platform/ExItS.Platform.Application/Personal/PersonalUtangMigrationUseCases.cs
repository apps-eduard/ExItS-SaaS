using ExItS.Platform.Application.Audit;
using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Audit;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Personal;
using ExItS.Platform.Domain.Products;

namespace ExItS.Platform.Application.Personal;

public sealed record UtangMigrationSelectionRequest(
    Guid RelationshipId,
    Guid? ContactId = null,
    IReadOnlyList<Guid>? HistoryEntryIds = null);

public sealed record PreviewUtangMigrationRequest(
    string? DestinationProductCode,
    DateTimeOffset? EffectiveMigrationDateUtc,
    bool IncludeContact = true,
    bool IncludeOpeningBalance = true,
    bool IncludeSelectedHistory = false,
    bool IncludeDueDatesAndNotes = true,
    string SourceDisposition = nameof(PersonalUtangSourceDisposition.Archive),
    bool LinkedParticipantConsentAcknowledged = false,
    IReadOnlyList<UtangMigrationSelectionRequest>? Selections = null);

public sealed record UtangMigrationPreviewItemDto(
    Guid ItemId,
    string SourceType,
    Guid SourceRecordId,
    string Status,
    string? BlockedReason,
    string? ContactDisplayName,
    decimal? ProposedOpeningBalance,
    string? CurrencyCode,
    DateTimeOffset? DueDateUtc,
    string? NotesSnapshot,
    int HistoryEntryCount,
    bool RequiresLinkedParticipantConsent);

public sealed record UtangMigrationPreviewDto(
    Guid BatchId,
    Guid ConfirmationToken,
    Guid DestinationOrganizationId,
    string DestinationProductCode,
    DateTimeOffset EffectiveMigrationDateUtc,
    string SourceDisposition,
    bool IncludeContact,
    bool IncludeOpeningBalance,
    bool IncludeSelectedHistory,
    bool IncludeDueDatesAndNotes,
    bool LinkedParticipantConsentAcknowledged,
    IReadOnlyList<UtangMigrationPreviewItemDto> Items,
    int MigratableItemCount,
    int BlockedItemCount);

public sealed record ExecuteUtangMigrationRequest(
    Guid BatchId,
    Guid ConfirmationToken,
    string IdempotencyKey);

public sealed record UtangMigrationExecuteItemDto(
    Guid ItemId,
    string SourceType,
    Guid SourceRecordId,
    string Status,
    Guid? BusinessCustomerId,
    Guid? CreditCustomerId,
    Guid? OpeningBalanceId,
    decimal? OpeningBalanceAmount);

public sealed record UtangMigrationExecuteDto(
    Guid BatchId,
    Guid DestinationOrganizationId,
    string DestinationProductCode,
    string Status,
    bool IdempotentReplay,
    IReadOnlyList<UtangMigrationExecuteItemDto> Items);

public sealed class PreviewPersonalUtangMigration
{
    private readonly IOrganizationMembershipRepository _memberships;
    private readonly IPersonalContactRepository _contacts;
    private readonly IPersonalDebtRelationshipRepository _relationships;
    private readonly IPersonalUtangEntryRepository _entries;
    private readonly IPersonalUtangMigrationBatchRepository _batches;
    private readonly IPersonalUtangMigrationItemRepository _items;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IAuditWriter _auditWriter;
    private readonly IClock _clock;

    public PreviewPersonalUtangMigration(
        IOrganizationMembershipRepository memberships,
        IPersonalContactRepository contacts,
        IPersonalDebtRelationshipRepository relationships,
        IPersonalUtangEntryRepository entries,
        IPersonalUtangMigrationBatchRepository batches,
        IPersonalUtangMigrationItemRepository items,
        IPlatformUnitOfWork unitOfWork,
        IAuditWriter auditWriter,
        IClock clock)
    {
        _memberships = memberships;
        _contacts = contacts;
        _relationships = relationships;
        _entries = entries;
        _batches = batches;
        _items = items;
        _unitOfWork = unitOfWork;
        _auditWriter = auditWriter;
        _clock = clock;
    }

    public async Task<ApplicationResult<UtangMigrationPreviewDto>> ExecuteAsync(
        PlatformUserId actingUserId,
        PlatformOrganizationId organizationId,
        PreviewUtangMigrationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actingUserId);
        ArgumentNullException.ThrowIfNull(organizationId);
        ArgumentNullException.ThrowIfNull(request);

        var ownerCheck = await EnsureOrganizationOwnerAsync(actingUserId, organizationId, cancellationToken)
            .ConfigureAwait(false);
        if (ownerCheck is not null)
        {
            return ownerCheck;
        }

        if (request.Selections is null || request.Selections.Count == 0)
        {
            return ApplicationResult<UtangMigrationPreviewDto>.Failure(
                ApplicationErrorCodes.UtangMigrationSelectionRequired,
                "Explicit migration selection is required; silent full dump is not allowed.");
        }

        if (!request.IncludeContact && !request.IncludeOpeningBalance
            && !request.IncludeSelectedHistory && !request.IncludeDueDatesAndNotes)
        {
            return ApplicationResult<UtangMigrationPreviewDto>.Failure(
                ApplicationErrorCodes.UtangMigrationSelectionRequired,
                "At least one migration include option must be selected.");
        }

        if (!Enum.TryParse<PersonalUtangSourceDisposition>(request.SourceDisposition, ignoreCase: true, out var disposition))
        {
            return ApplicationResult<UtangMigrationPreviewDto>.Failure(
                ApplicationErrorCodes.MigrationBatchInvalid,
                "Source disposition is invalid.");
        }

        var productCode = string.IsNullOrWhiteSpace(request.DestinationProductCode)
            ? ProductCode.PinoyBusinessPos
            : request.DestinationProductCode.Trim().ToLowerInvariant();
        var effectiveDate = request.EffectiveMigrationDateUtc ?? _clock.UtcNow;
        if (effectiveDate.Offset != TimeSpan.Zero)
        {
            effectiveDate = effectiveDate.ToUniversalTime();
        }

        try
        {
            var batch = PersonalUtangMigrationBatch.CreatePreview(
                actingUserId,
                organizationId,
                productCode,
                effectiveDate,
                request.IncludeContact,
                request.IncludeOpeningBalance,
                request.IncludeSelectedHistory,
                request.IncludeDueDatesAndNotes,
                disposition,
                request.LinkedParticipantConsentAcknowledged,
                _clock.UtcNow);

            var ownedContacts = (await _contacts.ListByOwnerAsync(actingUserId, cancellationToken).ConfigureAwait(false))
                .ToDictionary(c => c.Id.Value);
            var previewItems = new List<UtangMigrationPreviewItemDto>();

            foreach (var selection in request.Selections)
            {
                var relationship = await _relationships
                    .GetByIdAsync(PersonalDebtRelationshipId.From(selection.RelationshipId), cancellationToken)
                    .ConfigureAwait(false);
                if (relationship is null)
                {
                    return ApplicationResult<UtangMigrationPreviewDto>.Failure(
                        ApplicationErrorCodes.PersonalUtangRelationshipNotFound,
                        "Selected Personal Utang relationship was not found.");
                }

                if (!await CanAccessRelationshipAsync(actingUserId, relationship, ownedContacts, cancellationToken)
                        .ConfigureAwait(false))
                {
                    return ApplicationResult<UtangMigrationPreviewDto>.Failure(
                        ApplicationErrorCodes.PersonalUtangUnauthorized,
                        "Selected Personal Utang relationship is not owned by the authenticated user.");
                }

                var already = await _items
                    .FindMigratedByDestinationAndSourceAsync(
                        organizationId,
                        PersonalUtangMigrationSourceType.PersonalDebtRelationship,
                        relationship.Id.Value,
                        cancellationToken)
                    .ConfigureAwait(false);

                PersonalContact? contact = null;
                if (selection.ContactId is Guid contactId)
                {
                    ownedContacts.TryGetValue(contactId, out contact);
                    if (contact is null)
                    {
                        return ApplicationResult<UtangMigrationPreviewDto>.Failure(
                            ApplicationErrorCodes.PersonalContactNotFound,
                            "Selected Personal contact was not found or not owned.");
                    }
                }
                else
                {
                    contact = ResolveCounterpartyContact(relationship, ownedContacts);
                }

                var history = await _entries.ListByRelationshipAsync(relationship.Id, cancellationToken)
                    .ConfigureAwait(false);
                var selectedHistory = selection.HistoryEntryIds is { Count: > 0 }
                    ? history.Where(e => selection.HistoryEntryIds.Contains(e.Id.Value)).ToList()
                    : [];
                if (request.IncludeSelectedHistory && selection.HistoryEntryIds is { Count: > 0 }
                    && selectedHistory.Count != selection.HistoryEntryIds.Count)
                {
                    return ApplicationResult<UtangMigrationPreviewDto>.Failure(
                        ApplicationErrorCodes.MigrationBatchInvalid,
                        "One or more selected history entry ids are invalid for the relationship.");
                }

                var requiresConsent = contact?.IsLinked == true
                    || (relationship.CreditorUserIdentityId is not null
                        && relationship.DebtorUserIdentityId is not null
                        && (relationship.CreditorUserIdentityId == actingUserId
                            || relationship.DebtorUserIdentityId == actingUserId)
                        && relationship.CreditorUserIdentityId != relationship.DebtorUserIdentityId
                        && (relationship.CreditorUserIdentityId != actingUserId
                            || relationship.DebtorUserIdentityId != actingUserId));

                // Linked registered participant on the counterparty side requires consent for relationship transfer.
                var counterpartyLinked = false;
                if (relationship.CreditorUserIdentityId is not null
                    && relationship.CreditorUserIdentityId != actingUserId)
                {
                    counterpartyLinked = true;
                }
                else if (relationship.DebtorUserIdentityId is not null
                         && relationship.DebtorUserIdentityId != actingUserId)
                {
                    counterpartyLinked = true;
                }
                else if (contact?.IsLinked == true)
                {
                    counterpartyLinked = true;
                }

                requiresConsent = counterpartyLinked;

                string? blocked = null;
                var itemStatus = PersonalUtangMigrationItemStatus.Previewed;
                if (already is not null)
                {
                    blocked = "Source relationship already migrated into this organization.";
                    itemStatus = PersonalUtangMigrationItemStatus.Blocked;
                }
                else if (requiresConsent && !request.LinkedParticipantConsentAcknowledged)
                {
                    blocked = "Linked participant consent is required before transferring relationship data.";
                    itemStatus = PersonalUtangMigrationItemStatus.Blocked;
                }

                var historyCsv = request.IncludeSelectedHistory && selectedHistory.Count > 0
                    ? string.Join(',', selectedHistory.Select(e => e.Id.Value.ToString("D")))
                    : null;
                var notes = request.IncludeDueDatesAndNotes
                    ? BuildNotesSnapshot(selectedHistory, relationship)
                    : null;
                var due = request.IncludeDueDatesAndNotes ? relationship.DueDateUtc : null;
                decimal? opening = request.IncludeOpeningBalance ? relationship.CurrentBalance : null;

                var item = PersonalUtangMigrationItem.CreatePreview(
                    batch.Id,
                    PersonalUtangMigrationSourceType.PersonalDebtRelationship,
                    relationship.Id.Value,
                    opening,
                    relationship.CurrencyCode,
                    notes,
                    due,
                    historyCsv,
                    itemStatus,
                    blocked);
                if (itemStatus == PersonalUtangMigrationItemStatus.Blocked && blocked is not null)
                {
                    item.MarkBlocked(blocked);
                }

                await _items.AddAsync(item, cancellationToken).ConfigureAwait(false);
                previewItems.Add(new UtangMigrationPreviewItemDto(
                    item.Id.Value,
                    item.SourceType.ToString(),
                    item.SourceRecordId,
                    item.Status.ToString(),
                    item.BlockedReason,
                    contact?.DisplayName,
                    opening,
                    relationship.CurrencyCode,
                    due,
                    notes,
                    selectedHistory.Count,
                    requiresConsent));
            }

            await _batches.AddAsync(batch, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            await _auditWriter.WriteAsync(
                $"platform-user:{actingUserId.Value:D}",
                AuditActorType.PlatformUser,
                PlatformAuditActions.UtangMigrationPreviewed,
                nameof(PersonalUtangMigrationBatch),
                batch.Id.Value.ToString("D"),
                AuditOutcome.Succeeded,
                organizationId: organizationId,
                productCode: ProductCode.Create(productCode),
                summary: $"Migration preview created with {previewItems.Count} selected item(s).",
                cancellationToken: cancellationToken).ConfigureAwait(false);

            return ApplicationResult<UtangMigrationPreviewDto>.Success(new UtangMigrationPreviewDto(
                batch.Id.Value,
                batch.ConfirmationToken,
                organizationId.Value,
                productCode,
                effectiveDate,
                disposition.ToString(),
                batch.IncludeContact,
                batch.IncludeOpeningBalance,
                batch.IncludeSelectedHistory,
                batch.IncludeDueDatesAndNotes,
                batch.LinkedParticipantConsentAcknowledged,
                previewItems,
                previewItems.Count(i => i.Status == nameof(PersonalUtangMigrationItemStatus.Previewed)),
                previewItems.Count(i => i.Status == nameof(PersonalUtangMigrationItemStatus.Blocked))));
        }
        catch (DomainException ex)
        {
            return ApplicationResult<UtangMigrationPreviewDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }

    private async Task<ApplicationResult<UtangMigrationPreviewDto>?> EnsureOrganizationOwnerAsync(
        PlatformUserId userId,
        PlatformOrganizationId organizationId,
        CancellationToken cancellationToken)
    {
        var membership = await _memberships
            .FindActiveByUserAndOrganizationAsync(userId, organizationId, cancellationToken)
            .ConfigureAwait(false);
        if (membership is null || membership.Role != OrganizationRole.OrganizationOwner)
        {
            return ApplicationResult<UtangMigrationPreviewDto>.Failure(
                ApplicationErrorCodes.StartBusinessOwnerRequired,
                "Only the Organization Owner may migrate Personal Utang into this organization.");
        }

        return null;
    }

    private static async Task<bool> CanAccessRelationshipAsync(
        PlatformUserId userId,
        PersonalDebtRelationship relationship,
        Dictionary<Guid, PersonalContact> ownedContacts,
        CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);
        if (relationship.CanBeViewedBy(userId))
        {
            return true;
        }

        if (relationship.CreditorContactId is PersonalContactId cc
            && ownedContacts.TryGetValue(cc.Value, out var creditorContact)
            && relationship.CanBeViewedByContactOwner(userId, creditorContact))
        {
            return true;
        }

        if (relationship.DebtorContactId is PersonalContactId dc
            && ownedContacts.TryGetValue(dc.Value, out var debtorContact)
            && relationship.CanBeViewedByContactOwner(userId, debtorContact))
        {
            return true;
        }

        return false;
    }

    private static PersonalContact? ResolveCounterpartyContact(
        PersonalDebtRelationship relationship,
        Dictionary<Guid, PersonalContact> ownedContacts)
    {
        if (relationship.CreditorContactId is PersonalContactId cc
            && ownedContacts.TryGetValue(cc.Value, out var creditor))
        {
            return creditor;
        }

        if (relationship.DebtorContactId is PersonalContactId dc
            && ownedContacts.TryGetValue(dc.Value, out var debtor))
        {
            return debtor;
        }

        return null;
    }

    private static string? BuildNotesSnapshot(
        IReadOnlyList<PersonalUtangEntry> selectedHistory,
        PersonalDebtRelationship relationship)
    {
        var notes = selectedHistory
            .Where(e => !string.IsNullOrWhiteSpace(e.Notes))
            .Select(e => e.Notes!.Trim())
            .ToList();
        if (notes.Count == 0 && relationship.DueDateUtc is null)
        {
            return null;
        }

        var due = relationship.DueDateUtc is DateTimeOffset d
            ? $"Due:{d:yyyy-MM-dd}; "
            : string.Empty;
        var joined = string.Join(" | ", notes);
        var combined = (due + joined).Trim();
        return combined.Length > 512 ? combined[..512] : combined;
    }
}

public sealed class ExecutePersonalUtangMigration
{
    private readonly IOrganizationMembershipRepository _memberships;
    private readonly IPersonalContactRepository _contacts;
    private readonly IPersonalDebtRelationshipRepository _relationships;
    private readonly IPersonalUtangMigrationBatchRepository _batches;
    private readonly IPersonalUtangMigrationItemRepository _items;
    private readonly IBusinessCustomerRepository _businessCustomers;
    private readonly ICreditCustomerRepository _creditCustomers;
    private readonly IBusinessCreditOpeningBalanceRepository _openingBalances;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IAuditWriter _auditWriter;
    private readonly IClock _clock;

    public ExecutePersonalUtangMigration(
        IOrganizationMembershipRepository memberships,
        IPersonalContactRepository contacts,
        IPersonalDebtRelationshipRepository relationships,
        IPersonalUtangMigrationBatchRepository batches,
        IPersonalUtangMigrationItemRepository items,
        IBusinessCustomerRepository businessCustomers,
        ICreditCustomerRepository creditCustomers,
        IBusinessCreditOpeningBalanceRepository openingBalances,
        IPlatformUnitOfWork unitOfWork,
        IAuditWriter auditWriter,
        IClock clock)
    {
        _memberships = memberships;
        _contacts = contacts;
        _relationships = relationships;
        _batches = batches;
        _items = items;
        _businessCustomers = businessCustomers;
        _creditCustomers = creditCustomers;
        _openingBalances = openingBalances;
        _unitOfWork = unitOfWork;
        _auditWriter = auditWriter;
        _clock = clock;
    }

    public async Task<ApplicationResult<UtangMigrationExecuteDto>> ExecuteAsync(
        PlatformUserId actingUserId,
        PlatformOrganizationId organizationId,
        ExecuteUtangMigrationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actingUserId);
        ArgumentNullException.ThrowIfNull(organizationId);
        ArgumentNullException.ThrowIfNull(request);

        var membership = await _memberships
            .FindActiveByUserAndOrganizationAsync(actingUserId, organizationId, cancellationToken)
            .ConfigureAwait(false);
        if (membership is null || membership.Role != OrganizationRole.OrganizationOwner)
        {
            return ApplicationResult<UtangMigrationExecuteDto>.Failure(
                ApplicationErrorCodes.StartBusinessOwnerRequired,
                "Only the Organization Owner may execute Personal Utang migration into this organization.");
        }

        if (string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            return ApplicationResult<UtangMigrationExecuteDto>.Failure(
                ApplicationErrorCodes.MigrationBatchInvalid,
                "Idempotency key is required.");
        }

        var existingByKey = await _batches
            .FindByOwnerAndIdempotencyKeyAsync(actingUserId, request.IdempotencyKey.Trim(), cancellationToken)
            .ConfigureAwait(false);
        if (existingByKey is not null && existingByKey.Status == PersonalUtangMigrationBatchStatus.Executed)
        {
            var priorItems = await _items.ListByBatchAsync(existingByKey.Id, cancellationToken).ConfigureAwait(false);
            return ApplicationResult<UtangMigrationExecuteDto>.Success(
                await MapExecuteDtoAsync(existingByKey, priorItems, idempotentReplay: true, cancellationToken)
                    .ConfigureAwait(false));
        }

        var batch = await _batches
            .GetByIdAsync(PersonalUtangMigrationBatchId.From(request.BatchId), cancellationToken)
            .ConfigureAwait(false);
        if (batch is null || batch.OwnerUserIdentityId != actingUserId)
        {
            return ApplicationResult<UtangMigrationExecuteDto>.Failure(
                ApplicationErrorCodes.UtangMigrationBatchNotFound,
                "Migration batch was not found.");
        }

        if (batch.DestinationOrganizationId != organizationId)
        {
            return ApplicationResult<UtangMigrationExecuteDto>.Failure(
                ApplicationErrorCodes.CrossOrganizationMismatch,
                "Migration destination organization does not match the request organization.");
        }

        if (batch.ConfirmationToken != request.ConfirmationToken)
        {
            return ApplicationResult<UtangMigrationExecuteDto>.Failure(
                ApplicationErrorCodes.UtangMigrationConfirmationMismatch,
                "Confirmation token does not match the previewed migration batch.");
        }

        if (batch.Status == PersonalUtangMigrationBatchStatus.Executed)
        {
            var priorItems = await _items.ListByBatchAsync(batch.Id, cancellationToken).ConfigureAwait(false);
            return ApplicationResult<UtangMigrationExecuteDto>.Success(
                await MapExecuteDtoAsync(batch, priorItems, idempotentReplay: true, cancellationToken)
                    .ConfigureAwait(false));
        }

        if (batch.Status != PersonalUtangMigrationBatchStatus.Previewed)
        {
            return ApplicationResult<UtangMigrationExecuteDto>.Failure(
                ApplicationErrorCodes.UtangMigrationPreviewRequired,
                "Migration must be previewed before confirmation.");
        }

        var items = (await _items.ListByBatchAsync(batch.Id, cancellationToken).ConfigureAwait(false)).ToList();
        if (items.Count == 0)
        {
            return ApplicationResult<UtangMigrationExecuteDto>.Failure(
                ApplicationErrorCodes.UtangMigrationSelectionRequired,
                "Migration batch has no selected items.");
        }

        if (items.All(i => i.Status == PersonalUtangMigrationItemStatus.Blocked))
        {
            var alreadyMigrated = items.Any(i =>
                i.BlockedReason is not null
                && i.BlockedReason.Contains("already migrated", StringComparison.OrdinalIgnoreCase));
            return ApplicationResult<UtangMigrationExecuteDto>.Failure(
                alreadyMigrated
                    ? ApplicationErrorCodes.UtangMigrationAlreadyMigrated
                    : ApplicationErrorCodes.UtangMigrationConsentRequired,
                alreadyMigrated
                    ? "Selected source records were already migrated into this organization."
                    : "All selected items are blocked; resolve consent or duplicate selection before execute.");
        }

        var ownedContacts = (await _contacts.ListByOwnerAsync(actingUserId, cancellationToken).ConfigureAwait(false))
            .ToDictionary(c => c.Id.Value);
        var results = new List<UtangMigrationExecuteItemDto>();

        try
        {
            batch.BindIdempotencyKey(request.IdempotencyKey);

            foreach (var item in items.Where(i => i.Status == PersonalUtangMigrationItemStatus.Previewed))
            {
                var relationship = await _relationships
                    .GetByIdAsync(PersonalDebtRelationshipId.From(item.SourceRecordId), cancellationToken)
                    .ConfigureAwait(false);
                if (relationship is null)
                {
                    return ApplicationResult<UtangMigrationExecuteDto>.Failure(
                        ApplicationErrorCodes.PersonalUtangRelationshipNotFound,
                        "Source relationship disappeared before execute.");
                }

                var duplicate = await _items
                    .FindMigratedByDestinationAndSourceAsync(
                        organizationId,
                        PersonalUtangMigrationSourceType.PersonalDebtRelationship,
                        relationship.Id.Value,
                        cancellationToken)
                    .ConfigureAwait(false);
                if (duplicate is not null)
                {
                    return ApplicationResult<UtangMigrationExecuteDto>.Failure(
                        ApplicationErrorCodes.UtangMigrationAlreadyMigrated,
                        "Source relationship was already migrated into this organization.");
                }

                // Consent re-check at execute time.
                var contact = ResolveCounterpartyContact(relationship, ownedContacts);
                var counterpartyLinked = contact?.IsLinked == true
                    || (relationship.CreditorUserIdentityId is not null
                        && relationship.CreditorUserIdentityId != actingUserId)
                    || (relationship.DebtorUserIdentityId is not null
                        && relationship.DebtorUserIdentityId != actingUserId);
                if (counterpartyLinked && !batch.LinkedParticipantConsentAcknowledged)
                {
                    return ApplicationResult<UtangMigrationExecuteDto>.Failure(
                        ApplicationErrorCodes.UtangMigrationConsentRequired,
                        "Linked participant consent is required before transferring relationship data.");
                }

                var displayName = contact?.DisplayName ?? "Migrated Customer";
                var notes = batch.IncludeDueDatesAndNotes ? item.NotesSnapshot : null;
                BusinessCustomer? customer = null;
                CreditCustomer? credit = null;
                BusinessCreditOpeningBalance? opening = null;

                if (batch.IncludeContact || batch.IncludeOpeningBalance)
                {
                    customer = BusinessCustomer.Create(
                        organizationId,
                        displayName,
                        _clock.UtcNow,
                        email: contact?.Email,
                        phone: contact?.Phone,
                        notes: notes,
                        owningProductCode: batch.DestinationProductCode);
                    // Never invent org staff; never auto-link Personal Account without separate Customer Link.
                    await _businessCustomers.AddAsync(customer, cancellationToken).ConfigureAwait(false);
                }

                if (batch.IncludeOpeningBalance && customer is not null)
                {
                    credit = CreditCustomer.Create(
                        organizationId,
                        customer.Id,
                        _clock.UtcNow,
                        relationship.CurrencyCode);
                    await _creditCustomers.AddAsync(credit, cancellationToken).ConfigureAwait(false);

                    opening = BusinessCreditOpeningBalance.Create(
                        organizationId,
                        credit.Id,
                        customer.Id,
                        item.OpeningBalanceAmount ?? relationship.CurrentBalance,
                        relationship.CurrencyCode,
                        batch.EffectiveMigrationDateUtc,
                        PersonalUtangMigrationSourceType.PersonalDebtRelationship,
                        relationship.Id.Value,
                        batch.Id,
                        actingUserId,
                        _clock.UtcNow,
                        batch.DestinationProductCode);
                    await _openingBalances.AddAsync(opening, cancellationToken).ConfigureAwait(false);
                }

                if (customer is null)
                {
                    return ApplicationResult<UtangMigrationExecuteDto>.Failure(
                        ApplicationErrorCodes.UtangMigrationSelectionRequired,
                        "Migration execute requires contact and/or opening-balance include options.");
                }

                item.MarkMigrated(PersonalUtangMigrationDestinationType.BusinessCustomer, customer.Id.Value);

                await _items.UpdateAsync(item, cancellationToken).ConfigureAwait(false);

                switch (batch.SourceDisposition)
                {
                    case PersonalUtangSourceDisposition.Archive:
                        relationship.Archive(_clock.UtcNow);
                        if (contact is not null)
                        {
                            contact.Archive(_clock.UtcNow);
                            await _contacts.UpdateAsync(contact, cancellationToken).ConfigureAwait(false);
                        }

                        break;
                    case PersonalUtangSourceDisposition.MarkTransferred:
                        if (credit is null)
                        {
                            return ApplicationResult<UtangMigrationExecuteDto>.Failure(
                                ApplicationErrorCodes.MigrationBatchInvalid,
                                "MarkTransferred requires opening-balance / credit customer creation.");
                        }

                        relationship.MarkTransferred(
                            organizationId.Value,
                            credit.Id.Value,
                            batch.Id.Value,
                            _clock.UtcNow);
                        break;
                    case PersonalUtangSourceDisposition.Retain:
                        break;
                }

                await _relationships.UpdateAsync(relationship, cancellationToken).ConfigureAwait(false);

                results.Add(new UtangMigrationExecuteItemDto(
                    item.Id.Value,
                    item.SourceType.ToString(),
                    item.SourceRecordId,
                    item.Status.ToString(),
                    customer?.Id.Value,
                    credit?.Id.Value,
                    opening?.Id.Value,
                    opening?.Amount));
            }

            batch.MarkExecuted(_clock.UtcNow);
            await _batches.UpdateAsync(batch, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            await _auditWriter.WriteAsync(
                $"platform-user:{actingUserId.Value:D}",
                AuditActorType.PlatformUser,
                PlatformAuditActions.UtangMigrationExecuted,
                nameof(PersonalUtangMigrationBatch),
                batch.Id.Value.ToString("D"),
                AuditOutcome.Succeeded,
                organizationId: organizationId,
                productCode: ProductCode.Create(batch.DestinationProductCode),
                summary: $"Migration executed for {results.Count} item(s). No continuous sync. No staff invented from contacts.",
                cancellationToken: cancellationToken).ConfigureAwait(false);

            return ApplicationResult<UtangMigrationExecuteDto>.Success(new UtangMigrationExecuteDto(
                batch.Id.Value,
                organizationId.Value,
                batch.DestinationProductCode,
                batch.Status.ToString(),
                IdempotentReplay: false,
                results));
        }
        catch (DomainException ex)
        {
            return ApplicationResult<UtangMigrationExecuteDto>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<UtangMigrationExecuteDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }

    private async Task<UtangMigrationExecuteDto> MapExecuteDtoAsync(
        PersonalUtangMigrationBatch batch,
        IReadOnlyList<PersonalUtangMigrationItem> items,
        bool idempotentReplay,
        CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);
        return new UtangMigrationExecuteDto(
            batch.Id.Value,
            batch.DestinationOrganizationId.Value,
            batch.DestinationProductCode,
            batch.Status.ToString(),
            idempotentReplay,
            items.Select(i => new UtangMigrationExecuteItemDto(
                i.Id.Value,
                i.SourceType.ToString(),
                i.SourceRecordId,
                i.Status.ToString(),
                i.DestinationType == PersonalUtangMigrationDestinationType.BusinessCustomer
                    ? i.DestinationRecordId
                    : null,
                null,
                null,
                i.OpeningBalanceAmount)).ToList());
    }

    private static PersonalContact? ResolveCounterpartyContact(
        PersonalDebtRelationship relationship,
        Dictionary<Guid, PersonalContact> ownedContacts)
    {
        if (relationship.CreditorContactId is PersonalContactId cc
            && ownedContacts.TryGetValue(cc.Value, out var creditor))
        {
            return creditor;
        }

        if (relationship.DebtorContactId is PersonalContactId dc
            && ownedContacts.TryGetValue(dc.Value, out var debtor))
        {
            return debtor;
        }

        return null;
    }
}
