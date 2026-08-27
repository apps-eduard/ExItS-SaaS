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

public sealed record PersonalContactDto(
    Guid Id,
    string DisplayName,
    string? Phone,
    string? Email,
    Guid? LinkedUserIdentityId,
    Guid? ResolvedUserIdentityId,
    string? ResolvedPublicUserId,
    string? PublicUserId,
    DateTimeOffset? ConnectedAtUtc,
    DateTimeOffset? BlockedAtUtc,
    string Status,
    DateTimeOffset CreatedAtUtc);

public sealed record CreatePersonalContactRequest(
    string DisplayName,
    string? Phone,
    string? Email,
    Guid? ResolvedUserIdentityId = null,
    string? ResolvedPublicUserId = null,
    // Legacy POS JSON aliases — resolve identity only; do not auto-link/connect.
    Guid? LinkedUserIdentityId = null,
    string? PublicUserId = null,
    /// <summary>Optional client-stable identity for offline replay / ambiguous-outcome reconciliation.</summary>
    Guid? ContactId = null);

public sealed record PersonalDebtRelationshipSummaryDto(
    Guid Id,
    string Perspective,
    Guid? CreditorUserIdentityId,
    Guid? CreditorContactId,
    Guid? DebtorUserIdentityId,
    Guid? DebtorContactId,
    string CurrencyCode,
    decimal CurrentBalance,
    DateTimeOffset? DueDateUtc,
    string Status,
    int Version,
    DateTimeOffset UpdatedAtUtc,
    bool IsSharedLedger,
    bool IsPrivate);

public sealed record CreatePersonalDebtRelationshipRequest(
    Guid? CreditorUserIdentityId,
    Guid? CreditorContactId,
    Guid? DebtorUserIdentityId,
    Guid? DebtorContactId,
    string? CurrencyCode,
    DateTimeOffset? DueDateUtc,
    decimal? InitialLoanAmount,
    string? InitialLoanNotes,
    /// <summary>Optional client-stable identity for offline replay / ambiguous-outcome reconciliation.</summary>
    Guid? RelationshipId = null,
    /// <summary>Optional client-stable id for the initial loan entry when <see cref="InitialLoanAmount"/> is set.</summary>
    Guid? InitialLoanEntryId = null);

public sealed record RecordPersonalUtangEntryRequest(
    string EntryType,
    decimal Amount,
    decimal? AdjustmentDelta,
    int? ExpectedVersion,
    string? Notes,
    DateTimeOffset? DueDateUtc,
    /// <summary>Optional client-stable identity for offline replay / ambiguous-outcome reconciliation.</summary>
    Guid? EntryId = null);

public sealed record ConfirmPersonalUtangEntryRequest(int? ExpectedVersion);

public sealed record DisputePersonalUtangEntryRequest(int? ExpectedVersion, string? Reason);

public sealed record CancelPersonalUtangEntryRequest(int? ExpectedVersion);

public sealed record UpdatePersonalContactRequest(string DisplayName, string? Phone, string? Email);

public sealed record LinkPersonalContactRequest(
    Guid? LinkedUserIdentityId = null,
    string? PublicUserId = null);

public sealed record PersonalUtangEntryDto(
    Guid Id,
    Guid RelationshipId,
    string EntryType,
    decimal Amount,
    decimal SignedDelta,
    decimal BalanceAfter,
    string? Notes,
    DateTimeOffset? DueDateUtc,
    Guid CreatedByUserIdentityId,
    DateTimeOffset CreatedAtUtc,
    string Status,
    Guid? ResolvedByUserIdentityId,
    DateTimeOffset? ResolvedAtUtc,
    string? DisputeReason,
    bool CanConfirm,
    bool CanDispute,
    bool CanCancel,
    bool AffectsBalance,
    bool IsSharedLedger,
    string Intent,
    decimal? SettlementBalanceSnapshot,
    bool IsSettlement);

public sealed record PersonalUtangBalanceDto(
    Guid RelationshipId,
    decimal CurrentBalance,
    string CurrencyCode,
    int Version,
    DateTimeOffset UpdatedAtUtc);

internal static class PersonalUtangIdempotency
{
    public static bool ContactMatches(PersonalContact existing, CreatePersonalContactRequest request)
    {
        var phone = NormalizeOptional(request.Phone);
        var email = NormalizeOptionalEmail(request.Email);
        return string.Equals(existing.DisplayName, NormalizeDisplayName(request.DisplayName), StringComparison.Ordinal)
            && string.Equals(existing.Phone, phone, StringComparison.Ordinal)
            && string.Equals(existing.Email, email, StringComparison.OrdinalIgnoreCase);
    }

    public static bool RelationshipMatches(
        PersonalDebtRelationship existing,
        PlatformUserId? creditorUser,
        PersonalContactId? creditorContact,
        PlatformUserId? debtorUser,
        PersonalContactId? debtorContact,
        string currencyCode)
    {
        return existing.CreditorUserIdentityId == creditorUser
            && existing.CreditorContactId == creditorContact
            && existing.DebtorUserIdentityId == debtorUser
            && existing.DebtorContactId == debtorContact
            && string.Equals(existing.CurrencyCode, NormalizeCurrency(currencyCode), StringComparison.OrdinalIgnoreCase);
    }

    public static bool EntryMatches(
        PersonalUtangEntry existing,
        PersonalDebtRelationshipId relationshipId,
        PersonalUtangEntryType entryType,
        decimal amount,
        decimal? adjustmentDelta,
        PersonalUtangEntryIntent intent = PersonalUtangEntryIntent.Regular)
    {
        if (existing.RelationshipId != relationshipId || existing.EntryType != entryType)
        {
            return false;
        }

        if (existing.Intent != intent)
        {
            return false;
        }

        if (intent is PersonalUtangEntryIntent.Settlement || existing.IsSettlement)
        {
            return existing.IsSettlement
                && existing.EntryType is PersonalUtangEntryType.Payment
                && existing.Amount == amount;
        }

        if (entryType is PersonalUtangEntryType.Adjustment)
        {
            var expectedSigned = adjustmentDelta ?? 0m;
            return existing.Amount == amount && existing.SignedDelta == expectedSigned;
        }

        return existing.Amount == amount;
    }

    private static string NormalizeDisplayName(string displayName) =>
        displayName.Trim();

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? NormalizeOptionalEmail(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string NormalizeCurrency(string currencyCode) =>
        string.IsNullOrWhiteSpace(currencyCode) ? "PHP" : currencyCode.Trim().ToUpperInvariant();
}

internal static class PersonalUtangAccess
{
    public static async Task<bool> CanViewAsync(
        PersonalDebtRelationship relationship,
        PlatformUserId userIdentityId,
        IPersonalContactRepository contacts,
        CancellationToken cancellationToken)
    {
        if (relationship.CanBeViewedBy(userIdentityId))
        {
            return true;
        }

        if (relationship.CreditorContactId is not null)
        {
            var contact = await contacts.GetByIdAsync(relationship.CreditorContactId, cancellationToken)
                .ConfigureAwait(false);
            if (contact is not null && relationship.CanBeViewedByContactOwner(userIdentityId, contact))
            {
                return true;
            }
        }

        if (relationship.DebtorContactId is not null)
        {
            var contact = await contacts.GetByIdAsync(relationship.DebtorContactId, cancellationToken)
                .ConfigureAwait(false);
            if (contact is not null && relationship.CanBeViewedByContactOwner(userIdentityId, contact))
            {
                return true;
            }
        }

        return false;
    }

    public static async Task<ApplicationResult<PersonalContact>> RequireOwnedContactAsync(
        PlatformUserId ownerUserIdentityId,
        PersonalContactId contactId,
        IPersonalContactRepository contacts,
        CancellationToken cancellationToken)
    {
        var contact = await contacts.GetByIdAsync(contactId, cancellationToken).ConfigureAwait(false);
        if (contact is null)
        {
            return ApplicationResult<PersonalContact>.Failure(
                ApplicationErrorCodes.PersonalContactNotFound,
                "Personal contact was not found.");
        }

        if (!contact.IsOwnedBy(ownerUserIdentityId))
        {
            return ApplicationResult<PersonalContact>.Failure(
                ApplicationErrorCodes.PersonalUtangUnauthorized,
                "Personal contact is not owned by this account.");
        }

        return ApplicationResult<PersonalContact>.Success(contact);
    }
}

internal static class PersonalContactIdentityVerification
{
    internal static async Task<ApplicationResult<(PlatformUserId UserId, string PublicUserId)>> ResolveVerifiedIdentityAsync(
        PlatformUserId ownerUserIdentityId,
        string publicUserIdInput,
        Guid? clientSuppliedUserIdentityId,
        IPlatformUserRepository users,
        CancellationToken cancellationToken)
    {
        string normalized;
        try
        {
            normalized = PublicUserIdRules.TryExtractFromQrPayload(publicUserIdInput);
        }
        catch (DomainException)
        {
            return ApplicationResult<(PlatformUserId, string)>.Failure(
                DomainErrorCodes.InvalidPublicUserId,
                "ExItS ID format is invalid.");
        }

        var target = await users.GetByPublicUserIdAsync(normalized, cancellationToken).ConfigureAwait(false);
        if (target is null || target.Status is not AccountStatus.Active)
        {
            return ApplicationResult<(PlatformUserId, string)>.Failure(
                ApplicationErrorCodes.UserNotFound,
                "No active user matched that ExItS ID.");
        }

        if (target.Id == ownerUserIdentityId)
        {
            return ApplicationResult<(PlatformUserId, string)>.Failure(
                ApplicationErrorCodes.PersonalContactSelfNotAllowed,
                "You cannot add yourself as a contact.");
        }

        if (clientSuppliedUserIdentityId is Guid supplied
            && supplied != Guid.Empty
            && supplied != target.Id.Value)
        {
            return ApplicationResult<(PlatformUserId, string)>.Failure(
                ApplicationErrorCodes.PersonalContactIdentityMismatch,
                "ExItS ID does not match the supplied user identity.");
        }

        return ApplicationResult<(PlatformUserId, string)>.Success((target.Id, target.PublicUserId!));
    }
}

public sealed class CreatePersonalContact
{
    private readonly IPersonalContactRepository _contacts;
    private readonly IPlatformUserRepository _users;
    private readonly IAuditWriter _auditWriter;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public CreatePersonalContact(
        IPersonalContactRepository contacts,
        IPlatformUserRepository users,
        IAuditWriter auditWriter,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _contacts = contacts;
        _users = users;
        _auditWriter = auditWriter;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<PersonalContactDto>> ExecuteAsync(
        PlatformUserId ownerUserIdentityId,
        CreatePersonalContactRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (request.ContactId is Guid clientContactId && clientContactId != Guid.Empty)
            {
                var existingById = await _contacts
                    .GetByIdAsync(PersonalContactId.From(clientContactId), cancellationToken)
                    .ConfigureAwait(false);
                if (existingById is not null)
                {
                    if (existingById.OwnerUserIdentityId != ownerUserIdentityId)
                    {
                        return ApplicationResult<PersonalContactDto>.Failure(
                            ApplicationErrorCodes.PersonalContactNotFound,
                            "Personal contact was not found.");
                    }

                    if (!PersonalUtangIdempotency.ContactMatches(existingById, request))
                    {
                        return ApplicationResult<PersonalContactDto>.Failure(
                            ApplicationErrorCodes.PersonalUtangIdempotencyConflict,
                            "Contact identity was reused with a conflicting payload.");
                    }

                    return ApplicationResult<PersonalContactDto>.Success(ToDto(existingById));
                }
            }

            PersonalContactId? clientId = request.ContactId is Guid cid && cid != Guid.Empty
                ? PersonalContactId.From(cid)
                : null;

            var contact = PersonalContact.Create(
                ownerUserIdentityId,
                request.DisplayName,
                request.Phone,
                request.Email,
                _clock.UtcNow,
                clientId);

            var resolvedPublicInput = request.ResolvedPublicUserId ?? request.PublicUserId;
            var resolvedUserInput = request.ResolvedUserIdentityId ?? request.LinkedUserIdentityId;
            if (!string.IsNullOrWhiteSpace(resolvedPublicInput)
                || (resolvedUserInput is Guid suppliedId && suppliedId != Guid.Empty))
            {
                if (string.IsNullOrWhiteSpace(resolvedPublicInput))
                {
                    return ApplicationResult<PersonalContactDto>.Failure(
                        ApplicationErrorCodes.PersonalContactNotFound,
                        "Resolved public user id is required when resolving identity.");
                }

                var verified = await PersonalContactIdentityVerification.ResolveVerifiedIdentityAsync(
                    ownerUserIdentityId,
                    resolvedPublicInput,
                    resolvedUserInput,
                    _users,
                    cancellationToken).ConfigureAwait(false);
                if (!verified.IsSuccess)
                {
                    return ApplicationResult<PersonalContactDto>.Failure(
                        verified.ErrorCode!,
                        verified.ErrorMessage!);
                }

                var (resolvedUserId, verifiedPublicUserId) = verified.Value;

                var duplicate = await _contacts
                    .FindActiveByOwnerAndResolvedUserAsync(
                        ownerUserIdentityId,
                        resolvedUserId,
                        cancellationToken)
                    .ConfigureAwait(false);
                if (duplicate is not null)
                {
                    return ApplicationResult<PersonalContactDto>.Failure(
                        ApplicationErrorCodes.PersonalContactIdentityConflict,
                        "This person is already in your People list.");
                }

                contact.ResolveIdentity(
                    resolvedUserId,
                    verifiedPublicUserId,
                    _clock.UtcNow);
            }

            if (contact.Email is not null)
            {
                var existing = await _contacts
                    .FindActiveByOwnerAndNormalizedEmailAsync(
                        ownerUserIdentityId,
                        contact.Email,
                        cancellationToken)
                    .ConfigureAwait(false);
                if (existing is not null)
                {
                    return ApplicationResult<PersonalContactDto>.Failure(
                        ApplicationErrorCodes.PersonalContactEmailConflict,
                        "An active personal contact with this email already exists.");
                }
            }

            await _contacts.AddAsync(contact, cancellationToken).ConfigureAwait(false);
            try
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (PersistenceConflictException) when (clientId is not null)
            {
                var raced = await _contacts
                    .GetByIdAsync(clientId, cancellationToken)
                    .ConfigureAwait(false);
                if (raced is not null
                    && raced.OwnerUserIdentityId == ownerUserIdentityId
                    && PersonalUtangIdempotency.ContactMatches(raced, request))
                {
                    return ApplicationResult<PersonalContactDto>.Success(ToDto(raced));
                }

                throw;
            }

            await _auditWriter.WriteAsync(
                $"platform-user:{ownerUserIdentityId.Value:D}",
                AuditActorType.PlatformUser,
                PlatformAuditActions.PersonalContactCreated,
                nameof(PersonalContact),
                contact.Id.Value.ToString("D"),
                AuditOutcome.Succeeded,
                summary: $"Personal contact '{contact.DisplayName}' created.",
                cancellationToken: cancellationToken).ConfigureAwait(false);

            return ApplicationResult<PersonalContactDto>.Success(ToDto(contact));
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<PersonalContactDto>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<PersonalContactDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }

    internal static PersonalContactDto ToDto(PersonalContact contact) =>
        new(
            contact.Id.Value,
            contact.DisplayName,
            contact.Phone,
            contact.Email,
            contact.LinkedUserIdentityId?.Value,
            contact.ResolvedUserIdentityId?.Value,
            contact.ResolvedPublicUserId,
            contact.ResolvedPublicUserId,
            contact.ConnectedAtUtc,
            contact.BlockedAtUtc,
            contact.Status.ToString(),
            contact.CreatedAtUtc);
}

public sealed class ListPersonalContacts
{
    private readonly IPersonalContactRepository _contacts;

    public ListPersonalContacts(IPersonalContactRepository contacts) => _contacts = contacts;

    public async Task<IReadOnlyList<PersonalContactDto>> ExecuteAsync(
        PlatformUserId ownerUserIdentityId,
        CancellationToken cancellationToken = default)
    {
        var list = await _contacts.ListByOwnerAsync(ownerUserIdentityId, cancellationToken).ConfigureAwait(false);
        return list.Select(CreatePersonalContact.ToDto).ToList();
    }
}

public sealed class GetPersonalContact
{
    private readonly IPersonalContactRepository _contacts;

    public GetPersonalContact(IPersonalContactRepository contacts) => _contacts = contacts;

    public async Task<ApplicationResult<PersonalContactDto>> ExecuteAsync(
        PlatformUserId ownerUserIdentityId,
        Guid contactId,
        CancellationToken cancellationToken = default)
    {
        var contact = await _contacts
            .GetByIdAsync(PersonalContactId.From(contactId), cancellationToken)
            .ConfigureAwait(false);
        if (contact is null || contact.OwnerUserIdentityId != ownerUserIdentityId)
        {
            return ApplicationResult<PersonalContactDto>.Failure(
                ApplicationErrorCodes.PersonalContactNotFound,
                "Personal contact was not found.");
        }

        return ApplicationResult<PersonalContactDto>.Success(CreatePersonalContact.ToDto(contact));
    }
}

public sealed class CreatePersonalDebtRelationship
{
    private readonly IPersonalContactRepository _contacts;
    private readonly IPersonalDebtRelationshipRepository _relationships;
    private readonly IPersonalUtangEntryRepository _entries;
    private readonly IPlatformUserRepository _users;
    private readonly IPersonalAccountSettingsRepository _settings;
    private readonly IPersonalInAppNotificationRepository _notifications;
    private readonly IAuditWriter _auditWriter;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public CreatePersonalDebtRelationship(
        IPersonalContactRepository contacts,
        IPersonalDebtRelationshipRepository relationships,
        IPersonalUtangEntryRepository entries,
        IPlatformUserRepository users,
        IPersonalAccountSettingsRepository settings,
        IPersonalInAppNotificationRepository notifications,
        IAuditWriter auditWriter,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _contacts = contacts;
        _relationships = relationships;
        _entries = entries;
        _users = users;
        _settings = settings;
        _notifications = notifications;
        _auditWriter = auditWriter;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<PersonalDebtRelationshipSummaryDto>> ExecuteAsync(
        PlatformUserId actingUserIdentityId,
        CreatePersonalDebtRelationshipRequest request,
        CancellationToken cancellationToken = default)
    {
        PlatformUserId? creditorUser = request.CreditorUserIdentityId is Guid cu
            ? PlatformUserId.From(cu)
            : null;
        PersonalContactId? creditorContact = request.CreditorContactId is Guid cc
            ? PersonalContactId.From(cc)
            : null;
        PlatformUserId? debtorUser = request.DebtorUserIdentityId is Guid du
            ? PlatformUserId.From(du)
            : null;
        PersonalContactId? debtorContact = request.DebtorContactId is Guid dc
            ? PersonalContactId.From(dc)
            : null;

        if (creditorContact is not null)
        {
            var owned = await PersonalUtangAccess.RequireOwnedContactAsync(
                actingUserIdentityId, creditorContact, _contacts, cancellationToken).ConfigureAwait(false);
            if (!owned.IsSuccess)
            {
                return ApplicationResult<PersonalDebtRelationshipSummaryDto>.Failure(
                    owned.ErrorCode!, owned.ErrorMessage!);
            }

            // Linked People contact → canonicalize to Personal user participant (shared ledger).
            if (owned.Value!.LinkedUserIdentityId is PlatformUserId linkedCreditor)
            {
                creditorUser = linkedCreditor;
                creditorContact = null;
            }
        }

        if (debtorContact is not null)
        {
            var owned = await PersonalUtangAccess.RequireOwnedContactAsync(
                actingUserIdentityId, debtorContact, _contacts, cancellationToken).ConfigureAwait(false);
            if (!owned.IsSuccess)
            {
                return ApplicationResult<PersonalDebtRelationshipSummaryDto>.Failure(
                    owned.ErrorCode!, owned.ErrorMessage!);
            }

            if (owned.Value!.LinkedUserIdentityId is PlatformUserId linkedDebtor)
            {
                debtorUser = linkedDebtor;
                debtorContact = null;
            }
        }

        var actingOnUserSide = actingUserIdentityId == creditorUser || actingUserIdentityId == debtorUser;
        var actingOnContactSide = creditorContact is not null || debtorContact is not null;
        if (!actingOnUserSide && !actingOnContactSide)
        {
            return ApplicationResult<PersonalDebtRelationshipSummaryDto>.Failure(
                ApplicationErrorCodes.PersonalUtangUnauthorized,
                "At least one relationship participant must belong to the authenticated Personal account.");
        }

        if (creditorUser is not null && creditorUser != actingUserIdentityId
            && debtorUser is not null && debtorUser != actingUserIdentityId)
        {
            return ApplicationResult<PersonalDebtRelationshipSummaryDto>.Failure(
                ApplicationErrorCodes.PersonalUtangUnauthorized,
                "Cannot create a relationship between two other users.");
        }

        if (request.RelationshipId is Guid clientRelationshipId && clientRelationshipId != Guid.Empty)
        {
            var existingRelationship = await _relationships
                .GetByIdAsync(PersonalDebtRelationshipId.From(clientRelationshipId), cancellationToken)
                .ConfigureAwait(false);
            if (existingRelationship is not null)
            {
                if (!await PersonalUtangAccess.CanViewAsync(
                        existingRelationship, actingUserIdentityId, _contacts, cancellationToken)
                        .ConfigureAwait(false))
                {
                    return ApplicationResult<PersonalDebtRelationshipSummaryDto>.Failure(
                        ApplicationErrorCodes.PersonalUtangUnauthorized,
                        "Personal debt relationship is not visible to this account.");
                }

                var currency = request.CurrencyCode ?? "PHP";
                if (!PersonalUtangIdempotency.RelationshipMatches(
                        existingRelationship,
                        creditorUser,
                        creditorContact,
                        debtorUser,
                        debtorContact,
                        currency))
                {
                    return ApplicationResult<PersonalDebtRelationshipSummaryDto>.Failure(
                        ApplicationErrorCodes.PersonalUtangIdempotencyConflict,
                        "Relationship identity was reused with a conflicting payload.");
                }

                if (request.InitialLoanAmount is > 0)
                {
                    var history = await _entries
                        .ListByRelationshipAsync(existingRelationship.Id, cancellationToken)
                        .ConfigureAwait(false);
                    PersonalUtangEntry? initialLoan = null;
                    if (request.InitialLoanEntryId is Guid loanEntryId && loanEntryId != Guid.Empty)
                    {
                        initialLoan = history.FirstOrDefault(e => e.Id.Value == loanEntryId);
                    }
                    else
                    {
                        initialLoan = history
                            .Where(e => e.EntryType is PersonalUtangEntryType.Loan)
                            .OrderBy(e => e.CreatedAtUtc)
                            .FirstOrDefault();
                    }

                    if (initialLoan is null
                        || !PersonalUtangIdempotency.EntryMatches(
                            initialLoan,
                            existingRelationship.Id,
                            PersonalUtangEntryType.Loan,
                            request.InitialLoanAmount.Value,
                            adjustmentDelta: null))
                    {
                        return ApplicationResult<PersonalDebtRelationshipSummaryDto>.Failure(
                            ApplicationErrorCodes.PersonalUtangIdempotencyConflict,
                            "Relationship identity was reused with a conflicting payload.");
                    }
                }

                return ApplicationResult<PersonalDebtRelationshipSummaryDto>.Success(
                    ToSummary(existingRelationship, actingUserIdentityId));
            }
        }

        try
        {
            var sharedInitialLoan = creditorUser is not null
                && debtorUser is not null
                && request.InitialLoanAmount is > 0;

            if (sharedInitialLoan)
            {
                var counterparty = actingUserIdentityId == creditorUser ? debtorUser! : creditorUser!;
                PersonalUtangProposalAntiSpam.GateFailure? gateFailure = null;
                PersonalDebtRelationshipSummaryDto? summary = null;

                await _unitOfWork.ExecuteWithAdvisoryLockAsync(
                    actingUserIdentityId.Value,
                    counterparty.Value,
                    async ct =>
                    {
                        var gate = await PersonalUtangProposalAntiSpam.EnsureSharedLoanProposalAllowedAsync(
                            actingUserIdentityId,
                            counterparty,
                            request.InitialLoanAmount!.Value,
                            request.InitialLoanNotes,
                            _entries,
                            _contacts,
                            _clock,
                            ct).ConfigureAwait(false);
                        if (gate is not null)
                        {
                            gateFailure = gate;
                            return;
                        }

                        summary = await CreateRelationshipCoreAsync(
                            actingUserIdentityId,
                            creditorUser,
                            creditorContact,
                            debtorUser,
                            debtorContact,
                            request,
                            ct).ConfigureAwait(false);
                    },
                    cancellationToken).ConfigureAwait(false);

                if (gateFailure is not null)
                {
                    return ApplicationResult<PersonalDebtRelationshipSummaryDto>.Failure(
                        gateFailure.ErrorCode,
                        gateFailure.ErrorMessage);
                }

                return ApplicationResult<PersonalDebtRelationshipSummaryDto>.Success(summary!);
            }

            var created = await CreateRelationshipCoreAsync(
                actingUserIdentityId,
                creditorUser,
                creditorContact,
                debtorUser,
                debtorContact,
                request,
                cancellationToken).ConfigureAwait(false);
            return ApplicationResult<PersonalDebtRelationshipSummaryDto>.Success(created);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<PersonalDebtRelationshipSummaryDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }

    private async Task<PersonalDebtRelationshipSummaryDto> CreateRelationshipCoreAsync(
        PlatformUserId actingUserIdentityId,
        PlatformUserId? creditorUser,
        PersonalContactId? creditorContact,
        PlatformUserId? debtorUser,
        PersonalContactId? debtorContact,
        CreatePersonalDebtRelationshipRequest request,
        CancellationToken cancellationToken)
    {
        var relationship = PersonalDebtRelationship.Create(
            actingUserIdentityId,
            creditorUser,
            creditorContact,
            debtorUser,
            debtorContact,
            request.CurrencyCode ?? "PHP",
            _clock.UtcNow,
            request.DueDateUtc,
            request.RelationshipId is Guid rid && rid != Guid.Empty
                ? PersonalDebtRelationshipId.From(rid)
                : null);

        PersonalUtangEntry? initialEntry = null;
        if (request.InitialLoanAmount is > 0)
        {
            var signedDelta = PersonalDebtRelationship.ComputeSignedDelta(
                PersonalUtangEntryType.Loan,
                request.InitialLoanAmount.Value);
            initialEntry = relationship.RecordEntry(
                actingUserIdentityId,
                PersonalUtangEntryType.Loan,
                request.InitialLoanAmount.Value,
                signedDelta,
                _clock.UtcNow,
                expectedVersion: null,
                request.InitialLoanNotes,
                request.DueDateUtc,
                request.InitialLoanEntryId is Guid eid && eid != Guid.Empty
                    ? PersonalUtangEntryId.From(eid)
                    : null);
        }

        await _relationships.AddAsync(relationship, cancellationToken).ConfigureAwait(false);
        if (initialEntry is not null)
        {
            await _entries.AddAsync(initialEntry, cancellationToken).ConfigureAwait(false);
        }

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (PersistenceConflictException) when (
            request.RelationshipId is Guid conflictId && conflictId != Guid.Empty)
        {
            var raced = await _relationships
                .GetByIdAsync(PersonalDebtRelationshipId.From(conflictId), cancellationToken)
                .ConfigureAwait(false);
            if (raced is not null
                && await PersonalUtangAccess.CanViewAsync(raced, actingUserIdentityId, _contacts, cancellationToken)
                    .ConfigureAwait(false)
                && PersonalUtangIdempotency.RelationshipMatches(
                    raced,
                    creditorUser,
                    creditorContact,
                    debtorUser,
                    debtorContact,
                    request.CurrencyCode ?? "PHP"))
            {
                return ToSummary(raced, actingUserIdentityId);
            }

            throw;
        }

        if (initialEntry is not null && initialEntry.Status is PersonalUtangEntryStatus.Pending)
        {
            await PersonalUtangProposalAntiSpam.NotifyOrAggregatePendingAsync(
                relationship,
                actingUserIdentityId,
                _users,
                _settings,
                _notifications,
                _entries,
                _clock,
                cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        await _auditWriter.WriteAsync(
            $"platform-user:{actingUserIdentityId.Value:D}",
            AuditActorType.PlatformUser,
            PlatformAuditActions.PersonalUtangRelationshipCreated,
            nameof(PersonalDebtRelationship),
            relationship.Id.Value.ToString("D"),
            AuditOutcome.Succeeded,
            summary: "Personal debt relationship created.",
            cancellationToken: cancellationToken).ConfigureAwait(false);

        if (initialEntry is not null)
        {
            await _auditWriter.WriteAsync(
                $"platform-user:{actingUserIdentityId.Value:D}",
                AuditActorType.PlatformUser,
                PlatformAuditActions.PersonalUtangEntryRecorded,
                nameof(PersonalUtangEntry),
                initialEntry.Id.Value.ToString("D"),
                AuditOutcome.Succeeded,
                summary: "Initial loan entry recorded.",
                cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        return ToSummary(relationship, actingUserIdentityId);
    }

    internal static PersonalDebtRelationshipSummaryDto ToSummary(
        PersonalDebtRelationship relationship,
        PlatformUserId viewerUserIdentityId)
    {
        var perspective = relationship.DebtorUserIdentityId == viewerUserIdentityId ? "Borrowed" : "Lent";
        var isShared = relationship.IsSharedLinked;
        return new PersonalDebtRelationshipSummaryDto(
            relationship.Id.Value,
            perspective,
            relationship.CreditorUserIdentityId?.Value,
            relationship.CreditorContactId?.Value,
            relationship.DebtorUserIdentityId?.Value,
            relationship.DebtorContactId?.Value,
            relationship.CurrencyCode,
            relationship.CurrentBalance,
            relationship.DueDateUtc,
            relationship.Status.ToString(),
            relationship.Version,
            relationship.UpdatedAtUtc,
            IsSharedLedger: isShared,
            IsPrivate: !isShared);
    }
}

public sealed class ListPersonalUtangRelationships
{
    private readonly IPersonalDebtRelationshipRepository _relationships;
    private readonly IPersonalContactRepository _contacts;

    public ListPersonalUtangRelationships(
        IPersonalDebtRelationshipRepository relationships,
        IPersonalContactRepository contacts)
    {
        _relationships = relationships;
        _contacts = contacts;
    }

    public async Task<IReadOnlyList<PersonalDebtRelationshipSummaryDto>> ExecuteAsync(
        PlatformUserId userIdentityId,
        string perspective,
        CancellationToken cancellationToken = default)
    {
        var all = await _relationships.ListForUserAsync(userIdentityId, cancellationToken).ConfigureAwait(false);
        var ownedContactIds = (await _contacts.ListByOwnerAsync(userIdentityId, cancellationToken).ConfigureAwait(false))
            .Select(c => c.Id)
            .ToHashSet();
        var filtered = new List<PersonalDebtRelationship>();
        foreach (var relationship in all)
        {
            if (!await PersonalUtangAccess.CanViewAsync(relationship, userIdentityId, _contacts, cancellationToken)
                    .ConfigureAwait(false))
            {
                continue;
            }

            // User-side role wins; owned-contact role applies only when the viewer is not a user participant.
            var isLent = relationship.CreditorUserIdentityId == userIdentityId
                || (relationship.CreditorUserIdentityId is null
                    && relationship.DebtorUserIdentityId != userIdentityId
                    && relationship.CreditorContactId is not null
                    && ownedContactIds.Contains(relationship.CreditorContactId));
            var isBorrowed = relationship.DebtorUserIdentityId == userIdentityId
                || (relationship.DebtorUserIdentityId is null
                    && relationship.CreditorUserIdentityId != userIdentityId
                    && relationship.DebtorContactId is not null
                    && ownedContactIds.Contains(relationship.DebtorContactId));
            if (string.Equals(perspective, "lent", StringComparison.OrdinalIgnoreCase) && isLent)
            {
                filtered.Add(relationship);
            }
            else if (string.Equals(perspective, "borrowed", StringComparison.OrdinalIgnoreCase) && isBorrowed)
            {
                filtered.Add(relationship);
            }
        }

        return filtered
            .Select(r => CreatePersonalDebtRelationship.ToSummary(r, userIdentityId))
            .ToList();
    }
}

public sealed class GetPersonalUtangRelationship
{
    private readonly IPersonalDebtRelationshipRepository _relationships;
    private readonly IPersonalContactRepository _contacts;

    public GetPersonalUtangRelationship(
        IPersonalDebtRelationshipRepository relationships,
        IPersonalContactRepository contacts)
    {
        _relationships = relationships;
        _contacts = contacts;
    }

    public async Task<ApplicationResult<PersonalDebtRelationshipSummaryDto>> ExecuteAsync(
        PlatformUserId userIdentityId,
        Guid relationshipId,
        CancellationToken cancellationToken = default)
    {
        var relationship = await _relationships
            .GetByIdAsync(PersonalDebtRelationshipId.From(relationshipId), cancellationToken)
            .ConfigureAwait(false);
        if (relationship is null)
        {
            return ApplicationResult<PersonalDebtRelationshipSummaryDto>.Failure(
                ApplicationErrorCodes.PersonalUtangRelationshipNotFound,
                "Personal debt relationship was not found.");
        }

        if (!await PersonalUtangAccess.CanViewAsync(relationship, userIdentityId, _contacts, cancellationToken)
                .ConfigureAwait(false))
        {
            return ApplicationResult<PersonalDebtRelationshipSummaryDto>.Failure(
                ApplicationErrorCodes.PersonalUtangUnauthorized,
                "Personal debt relationship is not visible to this account.");
        }

        return ApplicationResult<PersonalDebtRelationshipSummaryDto>.Success(
            CreatePersonalDebtRelationship.ToSummary(relationship, userIdentityId));
    }
}

public sealed class GetPersonalUtangBalance
{
    private readonly GetPersonalUtangRelationship _getRelationship;

    public GetPersonalUtangBalance(GetPersonalUtangRelationship getRelationship) =>
        _getRelationship = getRelationship;

    public async Task<ApplicationResult<PersonalUtangBalanceDto>> ExecuteAsync(
        PlatformUserId userIdentityId,
        Guid relationshipId,
        CancellationToken cancellationToken = default)
    {
        var result = await _getRelationship.ExecuteAsync(userIdentityId, relationshipId, cancellationToken)
            .ConfigureAwait(false);
        if (!result.IsSuccess || result.Value is null)
        {
            return ApplicationResult<PersonalUtangBalanceDto>.Failure(result.ErrorCode!, result.ErrorMessage!);
        }

        var summary = result.Value;
        return ApplicationResult<PersonalUtangBalanceDto>.Success(new PersonalUtangBalanceDto(
            summary.Id,
            summary.CurrentBalance,
            summary.CurrencyCode,
            summary.Version,
            summary.UpdatedAtUtc));
    }
}

public sealed class ListPersonalUtangHistory
{
    private readonly GetPersonalUtangRelationship _getRelationship;
    private readonly IPersonalUtangEntryRepository _entries;

    public ListPersonalUtangHistory(
        GetPersonalUtangRelationship getRelationship,
        IPersonalUtangEntryRepository entries)
    {
        _getRelationship = getRelationship;
        _entries = entries;
    }

    public async Task<ApplicationResult<IReadOnlyList<PersonalUtangEntryDto>>> ExecuteAsync(
        PlatformUserId userIdentityId,
        Guid relationshipId,
        CancellationToken cancellationToken = default)
    {
        var access = await _getRelationship.ExecuteAsync(userIdentityId, relationshipId, cancellationToken)
            .ConfigureAwait(false);
        if (!access.IsSuccess)
        {
            return ApplicationResult<IReadOnlyList<PersonalUtangEntryDto>>.Failure(
                access.ErrorCode!,
                access.ErrorMessage!);
        }

        var list = await _entries
            .ListByRelationshipAsync(PersonalDebtRelationshipId.From(relationshipId), cancellationToken)
            .ConfigureAwait(false);
        var isShared = access.Value!.IsSharedLedger;
        return ApplicationResult<IReadOnlyList<PersonalUtangEntryDto>>.Success(
            list
                .Select(e => RecordPersonalUtangEntry.ToDto(e, userIdentityId, isShared))
                .ToList());
    }
}

public sealed class GetPersonalUtangEntry
{
    private readonly IPersonalUtangEntryRepository _entries;
    private readonly IPersonalDebtRelationshipRepository _relationships;
    private readonly IPersonalContactRepository _contacts;

    public GetPersonalUtangEntry(
        IPersonalUtangEntryRepository entries,
        IPersonalDebtRelationshipRepository relationships,
        IPersonalContactRepository contacts)
    {
        _entries = entries;
        _relationships = relationships;
        _contacts = contacts;
    }

    public async Task<ApplicationResult<PersonalUtangEntryDto>> ExecuteAsync(
        PlatformUserId userIdentityId,
        Guid entryId,
        CancellationToken cancellationToken = default)
    {
        var entry = await _entries
            .GetByIdAsync(PersonalUtangEntryId.From(entryId), cancellationToken)
            .ConfigureAwait(false);
        if (entry is null)
        {
            return ApplicationResult<PersonalUtangEntryDto>.Failure(
                ApplicationErrorCodes.PersonalUtangEntryNotFound,
                "Personal utang entry was not found.");
        }

        var relationship = await _relationships
            .GetByIdAsync(entry.RelationshipId, cancellationToken)
            .ConfigureAwait(false);
        if (relationship is null
            || !await PersonalUtangAccess.CanViewAsync(relationship, userIdentityId, _contacts, cancellationToken)
                .ConfigureAwait(false))
        {
            return ApplicationResult<PersonalUtangEntryDto>.Failure(
                ApplicationErrorCodes.PersonalUtangUnauthorized,
                "Personal debt relationship is not visible to this account.");
        }

        return ApplicationResult<PersonalUtangEntryDto>.Success(
            RecordPersonalUtangEntry.ToDto(entry, userIdentityId, relationship.IsSharedLinked));
    }
}

public sealed class RecordPersonalUtangEntry
{
    private readonly IPersonalDebtRelationshipRepository _relationships;
    private readonly IPersonalUtangEntryRepository _entries;
    private readonly IPersonalContactRepository _contacts;
    private readonly IPlatformUserRepository _users;
    private readonly IPersonalAccountSettingsRepository _settings;
    private readonly IPersonalInAppNotificationRepository _notifications;
    private readonly IAuditWriter _auditWriter;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public RecordPersonalUtangEntry(
        IPersonalDebtRelationshipRepository relationships,
        IPersonalUtangEntryRepository entries,
        IPersonalContactRepository contacts,
        IPlatformUserRepository users,
        IPersonalAccountSettingsRepository settings,
        IPersonalInAppNotificationRepository notifications,
        IAuditWriter auditWriter,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _relationships = relationships;
        _entries = entries;
        _contacts = contacts;
        _users = users;
        _settings = settings;
        _notifications = notifications;
        _auditWriter = auditWriter;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<PersonalUtangEntryDto>> ExecuteAsync(
        PlatformUserId actingUserIdentityId,
        Guid relationshipId,
        RecordPersonalUtangEntryRequest request,
        CancellationToken cancellationToken = default)
    {
        var relationship = await _relationships
            .GetByIdAsync(PersonalDebtRelationshipId.From(relationshipId), cancellationToken)
            .ConfigureAwait(false);
        if (relationship is null)
        {
            return ApplicationResult<PersonalUtangEntryDto>.Failure(
                ApplicationErrorCodes.PersonalUtangRelationshipNotFound,
                "Personal debt relationship was not found.");
        }

        if (!await PersonalUtangAccess.CanViewAsync(relationship, actingUserIdentityId, _contacts, cancellationToken)
                .ConfigureAwait(false))
        {
            return ApplicationResult<PersonalUtangEntryDto>.Failure(
                ApplicationErrorCodes.PersonalUtangUnauthorized,
                "Personal debt relationship is not visible to this account.");
        }

        if (!Enum.TryParse<PersonalUtangEntryType>(request.EntryType, ignoreCase: true, out var entryType))
        {
            return ApplicationResult<PersonalUtangEntryDto>.Failure(
                ApplicationErrorCodes.PersonalUtangEntryInvalid,
                "Entry type must be Loan, Payment, or Adjustment.");
        }

        if (request.EntryId is Guid clientEntryId && clientEntryId != Guid.Empty)
        {
            var existingEntry = await _entries
                .GetByIdAsync(PersonalUtangEntryId.From(clientEntryId), cancellationToken)
                .ConfigureAwait(false);
            if (existingEntry is not null)
            {
                if (!PersonalUtangIdempotency.EntryMatches(
                        existingEntry,
                        relationship.Id,
                        entryType,
                        request.Amount,
                        request.AdjustmentDelta))
                {
                    return ApplicationResult<PersonalUtangEntryDto>.Failure(
                        ApplicationErrorCodes.PersonalUtangIdempotencyConflict,
                        "Entry identity was reused with a conflicting payload.");
                }

                return ApplicationResult<PersonalUtangEntryDto>.Success(
                    ToDto(existingEntry, actingUserIdentityId, relationship.IsSharedLinked));
            }
        }

        PersonalUtangEntryId? clientId = request.EntryId is Guid eid && eid != Guid.Empty
            ? PersonalUtangEntryId.From(eid)
            : null;

        try
        {
            if (entryType is PersonalUtangEntryType.Loan && relationship.IsSharedLinked)
            {
                var counterparty = relationship.GetCounterpartyUserIdentityId(actingUserIdentityId);
                if (counterparty is null)
                {
                    return ApplicationResult<PersonalUtangEntryDto>.Failure(
                        ApplicationErrorCodes.PersonalUtangUnauthorized,
                        "Personal debt relationship is not visible to this account.");
                }

                PersonalUtangProposalAntiSpam.GateFailure? gateFailure = null;
                PersonalUtangEntry? createdEntry = null;

                await _unitOfWork.ExecuteWithAdvisoryLockAsync(
                    actingUserIdentityId.Value,
                    counterparty.Value,
                    async ct =>
                    {
                        if (clientId is not null)
                        {
                            var raced = await _entries.GetByIdAsync(clientId, ct).ConfigureAwait(false);
                            if (raced is not null)
                            {
                                if (!PersonalUtangIdempotency.EntryMatches(
                                        raced,
                                        relationship.Id,
                                        entryType,
                                        request.Amount,
                                        request.AdjustmentDelta))
                                {
                                    gateFailure = new PersonalUtangProposalAntiSpam.GateFailure(
                                        ApplicationErrorCodes.PersonalUtangIdempotencyConflict,
                                        "Entry identity was reused with a conflicting payload.");
                                    return;
                                }

                                createdEntry = raced;
                                return;
                            }
                        }

                        var gate = await PersonalUtangProposalAntiSpam.EnsureSharedLoanProposalAllowedAsync(
                            actingUserIdentityId,
                            counterparty,
                            request.Amount,
                            request.Notes,
                            _entries,
                            _contacts,
                            _clock,
                            ct).ConfigureAwait(false);
                        if (gate is not null)
                        {
                            gateFailure = gate;
                            return;
                        }

                        var signedDelta = PersonalDebtRelationship.ComputeSignedDelta(
                            entryType,
                            request.Amount,
                            request.AdjustmentDelta);
                        createdEntry = relationship.RecordEntry(
                            actingUserIdentityId,
                            entryType,
                            request.Amount,
                            signedDelta,
                            _clock.UtcNow,
                            request.ExpectedVersion,
                            request.Notes,
                            request.DueDateUtc,
                            clientId);

                        await _relationships.UpdateAsync(relationship, ct).ConfigureAwait(false);
                        await _entries.AddAsync(createdEntry, ct).ConfigureAwait(false);
                        await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

                        await PersonalUtangProposalAntiSpam.NotifyOrAggregatePendingAsync(
                            relationship,
                            actingUserIdentityId,
                            _users,
                            _settings,
                            _notifications,
                            _entries,
                            _clock,
                            ct).ConfigureAwait(false);

                        await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
                    },
                    cancellationToken).ConfigureAwait(false);

                if (gateFailure is not null)
                {
                    return ApplicationResult<PersonalUtangEntryDto>.Failure(
                        gateFailure.ErrorCode,
                        gateFailure.ErrorMessage);
                }

                await _auditWriter.WriteAsync(
                    $"platform-user:{actingUserIdentityId.Value:D}",
                    AuditActorType.PlatformUser,
                    PlatformAuditActions.PersonalUtangEntryRecorded,
                    nameof(PersonalUtangEntry),
                    createdEntry!.Id.Value.ToString("D"),
                    AuditOutcome.Succeeded,
                    summary: $"{entryType} entry recorded.",
                    cancellationToken: cancellationToken).ConfigureAwait(false);

                return ApplicationResult<PersonalUtangEntryDto>.Success(
                    ToDto(createdEntry, actingUserIdentityId, relationship.IsSharedLinked));
            }

            var nonSharedSignedDelta = PersonalDebtRelationship.ComputeSignedDelta(
                entryType,
                request.Amount,
                request.AdjustmentDelta);
            var entry = relationship.RecordEntry(
                actingUserIdentityId,
                entryType,
                request.Amount,
                nonSharedSignedDelta,
                _clock.UtcNow,
                request.ExpectedVersion,
                request.Notes,
                request.DueDateUtc,
                clientId);

            await _relationships.UpdateAsync(relationship, cancellationToken).ConfigureAwait(false);
            await _entries.AddAsync(entry, cancellationToken).ConfigureAwait(false);
            try
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (PersistenceConflictException) when (clientId is not null)
            {
                var raced = await _entries.GetByIdAsync(clientId, cancellationToken).ConfigureAwait(false);
                if (raced is not null
                    && PersonalUtangIdempotency.EntryMatches(
                        raced,
                        relationship.Id,
                        entryType,
                        request.Amount,
                        request.AdjustmentDelta))
                {
                    return ApplicationResult<PersonalUtangEntryDto>.Success(
                        ToDto(raced, actingUserIdentityId, relationship.IsSharedLinked));
                }

                throw;
            }

            if (entry.Status is PersonalUtangEntryStatus.Pending)
            {
                await PersonalUtangProposalAntiSpam.NotifyOrAggregatePendingAsync(
                    relationship,
                    actingUserIdentityId,
                    _users,
                    _settings,
                    _notifications,
                    _entries,
                    _clock,
                    cancellationToken).ConfigureAwait(false);
                await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }

            await _auditWriter.WriteAsync(
                $"platform-user:{actingUserIdentityId.Value:D}",
                AuditActorType.PlatformUser,
                PlatformAuditActions.PersonalUtangEntryRecorded,
                nameof(PersonalUtangEntry),
                entry.Id.Value.ToString("D"),
                AuditOutcome.Succeeded,
                summary: $"{entryType} entry recorded.",
                cancellationToken: cancellationToken).ConfigureAwait(false);

            return ApplicationResult<PersonalUtangEntryDto>.Success(
                ToDto(entry, actingUserIdentityId, relationship.IsSharedLinked));
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<PersonalUtangEntryDto>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (DomainException ex) when (ex.ErrorCode == DomainErrorCodes.PersonalUtangConcurrencyConflict)
        {
            return ApplicationResult<PersonalUtangEntryDto>.Failure(
                ApplicationErrorCodes.ConcurrencyConflict,
                ex.Message);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<PersonalUtangEntryDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }

    internal static PersonalUtangEntryDto ToDto(PersonalUtangEntry entry) =>
        ToDto(entry, viewerUserIdentityId: null, isSharedLedger: false);

    internal static PersonalUtangEntryDto ToDto(
        PersonalUtangEntry entry,
        PlatformUserId? viewerUserIdentityId,
        bool isSharedLedger)
    {
        var isPending = entry.Status is PersonalUtangEntryStatus.Pending;
        var isProposer = viewerUserIdentityId is not null
            && entry.CreatedByUserIdentityId == viewerUserIdentityId;
        var canResolve = isPending && isSharedLedger && viewerUserIdentityId is not null && !isProposer;
        var canCancel = isPending && isProposer;

        return new PersonalUtangEntryDto(
            entry.Id.Value,
            entry.RelationshipId.Value,
            entry.EntryType.ToString(),
            entry.Amount,
            entry.SignedDelta,
            entry.BalanceAfter,
            entry.Notes,
            entry.DueDateUtc,
            entry.CreatedByUserIdentityId.Value,
            entry.CreatedAtUtc,
            entry.Status.ToString(),
            entry.ResolvedByUserIdentityId?.Value,
            entry.ResolvedAtUtc,
            entry.DisputeReason,
            CanConfirm: canResolve,
            CanDispute: canResolve,
            CanCancel: canCancel,
            AffectsBalance: entry.Status is PersonalUtangEntryStatus.Confirmed,
            IsSharedLedger: isSharedLedger,
            Intent: entry.Intent.ToString(),
            SettlementBalanceSnapshot: entry.SettlementBalanceSnapshot,
            IsSettlement: entry.IsSettlement);
    }
}
