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
    string? PublicUserId,
    string Status,
    DateTimeOffset CreatedAtUtc);

public sealed record CreatePersonalContactRequest(
    string DisplayName,
    string? Phone,
    string? Email,
    Guid? LinkedUserIdentityId = null,
    string? PublicUserId = null);

public sealed record UpdatePersonalContactRequest(string DisplayName, string? Phone, string? Email);

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
    string? InitialLoanNotes);

public sealed record RecordPersonalUtangEntryRequest(
    string EntryType,
    decimal Amount,
    decimal? AdjustmentDelta,
    int? ExpectedVersion,
    string? Notes,
    DateTimeOffset? DueDateUtc);

public sealed record ConfirmPersonalUtangEntryRequest(int? ExpectedVersion);

public sealed record DisputePersonalUtangEntryRequest(int? ExpectedVersion, string? Reason);

public sealed record CancelPersonalUtangEntryRequest(int? ExpectedVersion);

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
    bool IsSharedLedger);

public sealed record PersonalUtangBalanceDto(
    Guid RelationshipId,
    decimal CurrentBalance,
    string CurrencyCode,
    int Version,
    DateTimeOffset UpdatedAtUtc);

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

public sealed class CreatePersonalContact
{
    private readonly IPersonalContactRepository _contacts;
    private readonly IPersonalDebtRelationshipRepository _relationships;
    private readonly IPlatformUserRepository _users;
    private readonly IPersonalAccountSettingsRepository _settings;
    private readonly IPersonalInAppNotificationRepository _notifications;
    private readonly IAuditWriter _auditWriter;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public CreatePersonalContact(
        IPersonalContactRepository contacts,
        IPersonalDebtRelationshipRepository relationships,
        IPlatformUserRepository users,
        IPersonalAccountSettingsRepository settings,
        IPersonalInAppNotificationRepository notifications,
        IAuditWriter auditWriter,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _contacts = contacts;
        _relationships = relationships;
        _users = users;
        _settings = settings;
        _notifications = notifications;
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
            var linkTarget = await ResolveOptionalLinkTargetAsync(
                ownerUserIdentityId,
                request,
                cancellationToken).ConfigureAwait(false);
            if (!linkTarget.IsSuccess)
            {
                return ApplicationResult<PersonalContactDto>.Failure(
                    linkTarget.ErrorCode!,
                    linkTarget.ErrorMessage!);
            }

            var linkedUser = linkTarget.Value;
            if (linkedUser is not null)
            {
                var existingLinked = await _contacts
                    .FindActiveByOwnerAndLinkedUserAsync(ownerUserIdentityId, linkedUser.Id, cancellationToken)
                    .ConfigureAwait(false);
                if (existingLinked is not null)
                {
                    return ApplicationResult<PersonalContactDto>.Success(
                        ToDto(existingLinked, linkedUser.PublicUserId));
                }

                // Repair prior Add-by-ExItS-ID that saved name only without linking.
                var owned = await _contacts.ListByOwnerAsync(ownerUserIdentityId, cancellationToken)
                    .ConfigureAwait(false);
                var orphan = owned.FirstOrDefault(c =>
                    c.Status == PersonalContactStatus.Active
                    && !c.IsLinked
                    && string.Equals(c.DisplayName, linkedUser.DisplayName, StringComparison.OrdinalIgnoreCase));
                if (orphan is not null)
                {
                    orphan.LinkUser(linkedUser.Id, _clock.UtcNow);
                    await _contacts.UpdateAsync(orphan, cancellationToken).ConfigureAwait(false);
                    await PersonalContactLinkSupport.PromoteRelationshipsForLinkedContactAsync(
                        ownerUserIdentityId,
                        orphan,
                        linkedUser.Id,
                        _relationships,
                        _auditWriter,
                        _clock,
                        cancellationToken).ConfigureAwait(false);
                    await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

                    await _auditWriter.WriteAsync(
                        $"platform-user:{ownerUserIdentityId.Value:D}",
                        AuditActorType.PlatformUser,
                        PlatformAuditActions.PersonalContactLinked,
                        nameof(PersonalContact),
                        orphan.Id.Value.ToString("D"),
                        AuditOutcome.Succeeded,
                        summary: $"Personal contact '{orphan.DisplayName}' linked via ExItS ID.",
                        cancellationToken: cancellationToken).ConfigureAwait(false);

                    await NotifyLinkedPersonAsync(
                        ownerUserIdentityId,
                        linkedUser.Id,
                        orphan,
                        cancellationToken).ConfigureAwait(false);

                    return ApplicationResult<PersonalContactDto>.Success(
                        ToDto(orphan, linkedUser.PublicUserId));
                }
            }

            var displayName = linkedUser?.DisplayName ?? request.DisplayName;
            var contact = PersonalContact.Create(
                ownerUserIdentityId,
                displayName,
                request.Phone,
                request.Email,
                _clock.UtcNow);

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

            if (linkedUser is not null)
            {
                contact.LinkUser(linkedUser.Id, _clock.UtcNow);
            }

            await _contacts.AddAsync(contact, cancellationToken).ConfigureAwait(false);
            if (linkedUser is not null)
            {
                await PersonalContactLinkSupport.PromoteRelationshipsForLinkedContactAsync(
                    ownerUserIdentityId,
                    contact,
                    linkedUser.Id,
                    _relationships,
                    _auditWriter,
                    _clock,
                    cancellationToken).ConfigureAwait(false);
            }
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            await _auditWriter.WriteAsync(
                $"platform-user:{ownerUserIdentityId.Value:D}",
                AuditActorType.PlatformUser,
                PlatformAuditActions.PersonalContactCreated,
                nameof(PersonalContact),
                contact.Id.Value.ToString("D"),
                AuditOutcome.Succeeded,
                summary: linkedUser is null
                    ? $"Personal contact '{contact.DisplayName}' created."
                    : $"Personal contact '{contact.DisplayName}' created and linked via ExItS ID.",
                cancellationToken: cancellationToken).ConfigureAwait(false);

            if (linkedUser is not null)
            {
                await _auditWriter.WriteAsync(
                    $"platform-user:{ownerUserIdentityId.Value:D}",
                    AuditActorType.PlatformUser,
                    PlatformAuditActions.PersonalContactLinked,
                    nameof(PersonalContact),
                    contact.Id.Value.ToString("D"),
                    AuditOutcome.Succeeded,
                    summary: $"Personal contact linked to user {linkedUser.Id.Value:D}.",
                    cancellationToken: cancellationToken).ConfigureAwait(false);

                await NotifyLinkedPersonAsync(
                    ownerUserIdentityId,
                    linkedUser.Id,
                    contact,
                    cancellationToken).ConfigureAwait(false);
            }

            return ApplicationResult<PersonalContactDto>.Success(
                ToDto(contact, linkedUser?.PublicUserId));
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<PersonalContactDto>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (DomainException ex)
        {
            var code = ex.ErrorCode switch
            {
                DomainErrorCodes.PersonalContactAlreadyLinked => ApplicationErrorCodes.PersonalContactLinkConflict,
                DomainErrorCodes.PersonalContactLinkInvalid => ApplicationErrorCodes.PersonalContactLinkInvalid,
                _ => ex.ErrorCode
            };
            return ApplicationResult<PersonalContactDto>.Failure(code, ex.Message);
        }
    }

    private async Task<ApplicationResult<PlatformUser?>> ResolveOptionalLinkTargetAsync(
        PlatformUserId ownerUserIdentityId,
        CreatePersonalContactRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.PublicUserId) && request.LinkedUserIdentityId is null)
        {
            return ApplicationResult<PlatformUser?>.Success(null);
        }

        var resolved = await PersonalContactLinkSupport.ResolvePersonalLinkTargetAsync(
            ownerUserIdentityId,
            request.PublicUserId,
            request.LinkedUserIdentityId,
            _users,
            cancellationToken).ConfigureAwait(false);
        if (!resolved.IsSuccess)
        {
            return ApplicationResult<PlatformUser?>.Failure(resolved.ErrorCode!, resolved.ErrorMessage!);
        }

        return ApplicationResult<PlatformUser?>.Success(resolved.Value);
    }

    private async Task NotifyLinkedPersonAsync(
        PlatformUserId ownerUserIdentityId,
        PlatformUserId linkedUserIdentityId,
        PersonalContact contact,
        CancellationToken cancellationToken)
    {
        var owner = await _users.GetByIdAsync(ownerUserIdentityId, cancellationToken).ConfigureAwait(false);
        var ownerLabel = string.IsNullOrWhiteSpace(owner?.DisplayName) ? "Someone" : owner!.DisplayName.Trim();
        // Privacy-minimized: no email/phone/GUID; name is intentional for People-add awareness.
        var title = "Added to People";
        var preview = $"{ownerLabel} added you to their People list.";
        if (preview.Length > 200)
        {
            preview = preview[..200];
        }

        var settings = await _settings.GetByUserAsync(linkedUserIdentityId, cancellationToken)
            .ConfigureAwait(false);
        if (settings is not null && !settings.InAppNotificationsEnabled)
        {
            return;
        }

        var notification = PersonalInAppNotification.Create(
            linkedUserIdentityId,
            title,
            preview,
            relatedType: "personal_contact",
            utcNow: _clock.UtcNow,
            relatedId: contact.Id.Value.ToString("D"));
        await _notifications.AddAsync(notification, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    internal static PersonalContactDto ToDto(PersonalContact contact, string? publicUserId = null) =>
        new(
            contact.Id.Value,
            contact.DisplayName,
            contact.Phone,
            contact.Email,
            contact.LinkedUserIdentityId?.Value,
            publicUserId,
            contact.Status.ToString(),
            contact.CreatedAtUtc);
}

public sealed record LinkPersonalContactRequest(
    Guid? LinkedUserIdentityId = null,
    string? PublicUserId = null);

/// <summary>
/// Explicitly link an existing owned PersonalContact to a resolved Personal ExItS ID.
/// Prefer this over guessing by name when repairing orphans.
/// </summary>
public sealed class LinkPersonalContact
{
    private readonly IPersonalContactRepository _contacts;
    private readonly IPersonalDebtRelationshipRepository _relationships;
    private readonly IPlatformUserRepository _users;
    private readonly IPersonalAccountSettingsRepository _settings;
    private readonly IPersonalInAppNotificationRepository _notifications;
    private readonly IAuditWriter _auditWriter;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public LinkPersonalContact(
        IPersonalContactRepository contacts,
        IPersonalDebtRelationshipRepository relationships,
        IPlatformUserRepository users,
        IPersonalAccountSettingsRepository settings,
        IPersonalInAppNotificationRepository notifications,
        IAuditWriter auditWriter,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _contacts = contacts;
        _relationships = relationships;
        _users = users;
        _settings = settings;
        _notifications = notifications;
        _auditWriter = auditWriter;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<PersonalContactDto>> ExecuteAsync(
        PlatformUserId ownerUserIdentityId,
        Guid contactId,
        LinkPersonalContactRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var owned = await PersonalUtangAccess.RequireOwnedContactAsync(
                ownerUserIdentityId,
                PersonalContactId.From(contactId),
                _contacts,
                cancellationToken).ConfigureAwait(false);
            if (!owned.IsSuccess)
            {
                return ApplicationResult<PersonalContactDto>.Failure(owned.ErrorCode!, owned.ErrorMessage!);
            }

            var contact = owned.Value!;
            if (contact.IsLinked)
            {
                if (contact.LinkedUserIdentityId is PlatformUserId already
                    && request.LinkedUserIdentityId is Guid provided
                    && already.Value != provided)
                {
                    return ApplicationResult<PersonalContactDto>.Failure(
                        ApplicationErrorCodes.PersonalContactLinkConflict,
                        "Personal contact is already linked to a different ExItS account.");
                }

                var existingLinked = await _users.GetByIdAsync(contact.LinkedUserIdentityId!, cancellationToken)
                    .ConfigureAwait(false);
                return ApplicationResult<PersonalContactDto>.Success(
                    CreatePersonalContact.ToDto(contact, existingLinked?.PublicUserId));
            }

            var target = await PersonalContactLinkSupport.ResolvePersonalLinkTargetAsync(
                ownerUserIdentityId,
                request.PublicUserId,
                request.LinkedUserIdentityId,
                _users,
                cancellationToken).ConfigureAwait(false);
            if (!target.IsSuccess)
            {
                return ApplicationResult<PersonalContactDto>.Failure(target.ErrorCode!, target.ErrorMessage!);
            }

            var linkedUser = target.Value!;
            var duplicate = await _contacts
                .FindActiveByOwnerAndLinkedUserAsync(ownerUserIdentityId, linkedUser.Id, cancellationToken)
                .ConfigureAwait(false);
            if (duplicate is not null && duplicate.Id != contact.Id)
            {
                return ApplicationResult<PersonalContactDto>.Failure(
                    ApplicationErrorCodes.PersonalContactLinkConflict,
                    "Another People contact is already linked to that ExItS account.");
            }

            contact.LinkUser(linkedUser.Id, _clock.UtcNow);
            await _contacts.UpdateAsync(contact, cancellationToken).ConfigureAwait(false);
            await PersonalContactLinkSupport.PromoteRelationshipsForLinkedContactAsync(
                ownerUserIdentityId,
                contact,
                linkedUser.Id,
                _relationships,
                _auditWriter,
                _clock,
                cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            await _auditWriter.WriteAsync(
                $"platform-user:{ownerUserIdentityId.Value:D}",
                AuditActorType.PlatformUser,
                PlatformAuditActions.PersonalContactLinked,
                nameof(PersonalContact),
                contact.Id.Value.ToString("D"),
                AuditOutcome.Succeeded,
                summary: $"Personal contact '{contact.DisplayName}' linked via ExItS ID.",
                cancellationToken: cancellationToken).ConfigureAwait(false);

            var owner = await _users.GetByIdAsync(ownerUserIdentityId, cancellationToken).ConfigureAwait(false);
            var ownerLabel = string.IsNullOrWhiteSpace(owner?.DisplayName) ? "Someone" : owner!.DisplayName.Trim();
            var settings = await _settings.GetByUserAsync(linkedUser.Id, cancellationToken).ConfigureAwait(false);
            if (settings is null || settings.InAppNotificationsEnabled)
            {
                var preview = $"{ownerLabel} added you to their People list.";
                if (preview.Length > 200)
                {
                    preview = preview[..200];
                }

                var notification = PersonalInAppNotification.Create(
                    linkedUser.Id,
                    "Added to People",
                    preview,
                    relatedType: "personal_contact",
                    utcNow: _clock.UtcNow,
                    relatedId: contact.Id.Value.ToString("D"));
                await _notifications.AddAsync(notification, cancellationToken).ConfigureAwait(false);
                await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }

            return ApplicationResult<PersonalContactDto>.Success(
                CreatePersonalContact.ToDto(contact, linkedUser.PublicUserId));
        }
        catch (DomainException ex)
        {
            var code = ex.ErrorCode switch
            {
                DomainErrorCodes.PersonalContactAlreadyLinked => ApplicationErrorCodes.PersonalContactLinkConflict,
                DomainErrorCodes.PersonalContactLinkInvalid => ApplicationErrorCodes.PersonalContactLinkInvalid,
                _ => ex.ErrorCode
            };
            return ApplicationResult<PersonalContactDto>.Failure(code, ex.Message);
        }
    }
}

public sealed class UpdatePersonalContact
{
    private readonly IPersonalContactRepository _contacts;
    private readonly IAuditWriter _auditWriter;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public UpdatePersonalContact(
        IPersonalContactRepository contacts,
        IAuditWriter auditWriter,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _contacts = contacts;
        _auditWriter = auditWriter;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<PersonalContactDto>> ExecuteAsync(
        PlatformUserId ownerUserIdentityId,
        Guid contactId,
        UpdatePersonalContactRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var contact = await _contacts
                .GetByIdAsync(PersonalContactId.From(contactId), cancellationToken)
                .ConfigureAwait(false);
            if (contact is null || !contact.IsOwnedBy(ownerUserIdentityId))
            {
                return ApplicationResult<PersonalContactDto>.Failure(
                    ApplicationErrorCodes.PersonalContactNotFound,
                    "Personal contact was not found.");
            }

            contact.UpdateDetails(request.DisplayName, request.Phone, request.Email, _clock.UtcNow);

            if (contact.Email is not null)
            {
                var existing = await _contacts
                    .FindActiveByOwnerAndNormalizedEmailAsync(
                        ownerUserIdentityId,
                        contact.Email,
                        cancellationToken)
                    .ConfigureAwait(false);
                if (existing is not null && existing.Id != contact.Id)
                {
                    return ApplicationResult<PersonalContactDto>.Failure(
                        ApplicationErrorCodes.PersonalContactEmailConflict,
                        "An active personal contact with this email already exists.");
                }
            }

            await _contacts.UpdateAsync(contact, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            await _auditWriter.WriteAsync(
                $"platform-user:{ownerUserIdentityId.Value:D}",
                AuditActorType.PlatformUser,
                PlatformAuditActions.PersonalContactUpdated,
                nameof(PersonalContact),
                contact.Id.Value.ToString("D"),
                AuditOutcome.Succeeded,
                summary: $"Personal contact '{contact.DisplayName}' updated.",
                cancellationToken: cancellationToken).ConfigureAwait(false);

            return ApplicationResult<PersonalContactDto>.Success(CreatePersonalContact.ToDto(contact));
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
}

public sealed class ListPersonalContacts
{
    private readonly IPersonalContactRepository _contacts;
    private readonly IPlatformUserRepository _users;

    public ListPersonalContacts(IPersonalContactRepository contacts, IPlatformUserRepository users)
    {
        _contacts = contacts;
        _users = users;
    }

    public async Task<IReadOnlyList<PersonalContactDto>> ExecuteAsync(
        PlatformUserId ownerUserIdentityId,
        CancellationToken cancellationToken = default)
    {
        var list = await _contacts.ListByOwnerAsync(ownerUserIdentityId, cancellationToken).ConfigureAwait(false);
        var results = new List<PersonalContactDto>(list.Count);
        foreach (var contact in list)
        {
            string? publicUserId = null;
            if (contact.LinkedUserIdentityId is not null)
            {
                var linked = await _users.GetByIdAsync(contact.LinkedUserIdentityId, cancellationToken)
                    .ConfigureAwait(false);
                publicUserId = linked?.PublicUserId;
            }

            results.Add(CreatePersonalContact.ToDto(contact, publicUserId));
        }

        return results;
    }
}

public sealed class CreatePersonalDebtRelationship
{
    private readonly IPersonalContactRepository _contacts;
    private readonly IPersonalDebtRelationshipRepository _relationships;
    private readonly IPersonalUtangEntryRepository _entries;
    private readonly IPersonalAccountSettingsRepository _settings;
    private readonly IPersonalInAppNotificationRepository _notifications;
    private readonly IAuditWriter _auditWriter;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public CreatePersonalDebtRelationship(
        IPersonalContactRepository contacts,
        IPersonalDebtRelationshipRepository relationships,
        IPersonalUtangEntryRepository entries,
        IPersonalAccountSettingsRepository settings,
        IPersonalInAppNotificationRepository notifications,
        IAuditWriter auditWriter,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _contacts = contacts;
        _relationships = relationships;
        _entries = entries;
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

        try
        {
            var relationship = PersonalDebtRelationship.Create(
                actingUserIdentityId,
                creditorUser,
                creditorContact,
                debtorUser,
                debtorContact,
                request.CurrencyCode ?? "PHP",
                _clock.UtcNow,
                request.DueDateUtc);

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
                    request.DueDateUtc);
            }

            await _relationships.AddAsync(relationship, cancellationToken).ConfigureAwait(false);
            if (initialEntry is not null)
            {
                await _entries.AddAsync(initialEntry, cancellationToken).ConfigureAwait(false);
                if (initialEntry.Status is PersonalUtangEntryStatus.Pending)
                {
                    await PersonalUtangEntryNotifications.NotifyCounterpartyPendingAsync(
                        relationship,
                        initialEntry,
                        actingUserIdentityId,
                        _settings,
                        _notifications,
                        _clock,
                        cancellationToken).ConfigureAwait(false);
                }
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

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
                var initialAuditAction = initialEntry.Status is PersonalUtangEntryStatus.Pending
                    ? PlatformAuditActions.PersonalUtangEntryProposed
                    : PlatformAuditActions.PersonalUtangEntryRecorded;
                var initialSummary = initialEntry.Status is PersonalUtangEntryStatus.Pending
                    ? "Initial loan entry proposed for counterparty confirmation."
                    : "Initial loan entry recorded.";
                await _auditWriter.WriteAsync(
                    $"platform-user:{actingUserIdentityId.Value:D}",
                    AuditActorType.PlatformUser,
                    initialAuditAction,
                    nameof(PersonalUtangEntry),
                    initialEntry.Id.Value.ToString("D"),
                    AuditOutcome.Succeeded,
                    summary: initialSummary,
                    cancellationToken: cancellationToken).ConfigureAwait(false);
            }

            return ApplicationResult<PersonalDebtRelationshipSummaryDto>.Success(
                ToSummary(relationship, actingUserIdentityId));
        }
        catch (DomainException ex)
        {
            return ApplicationResult<PersonalDebtRelationshipSummaryDto>.Failure(
                PersonalUtangEntryErrors.Map(ex),
                ex.Message);
        }
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
    private readonly IPersonalDebtRelationshipRepository _relationships;
    private readonly IPersonalContactRepository _contacts;
    private readonly IPersonalUtangEntryRepository _entries;

    public ListPersonalUtangHistory(
        IPersonalDebtRelationshipRepository relationships,
        IPersonalContactRepository contacts,
        IPersonalUtangEntryRepository entries)
    {
        _relationships = relationships;
        _contacts = contacts;
        _entries = entries;
    }

    public async Task<ApplicationResult<IReadOnlyList<PersonalUtangEntryDto>>> ExecuteAsync(
        PlatformUserId userIdentityId,
        Guid relationshipId,
        CancellationToken cancellationToken = default)
    {
        var relationship = await _relationships
            .GetByIdAsync(PersonalDebtRelationshipId.From(relationshipId), cancellationToken)
            .ConfigureAwait(false);
        if (relationship is null)
        {
            return ApplicationResult<IReadOnlyList<PersonalUtangEntryDto>>.Failure(
                ApplicationErrorCodes.PersonalUtangRelationshipNotFound,
                "Personal debt relationship was not found.");
        }

        if (!await PersonalUtangAccess.CanViewAsync(relationship, userIdentityId, _contacts, cancellationToken)
                .ConfigureAwait(false))
        {
            return ApplicationResult<IReadOnlyList<PersonalUtangEntryDto>>.Failure(
                ApplicationErrorCodes.PersonalUtangUnauthorized,
                "Personal debt relationship is not visible to this account.");
        }

        var list = await _entries
            .ListByRelationshipAsync(PersonalDebtRelationshipId.From(relationshipId), cancellationToken)
            .ConfigureAwait(false);

        return ApplicationResult<IReadOnlyList<PersonalUtangEntryDto>>.Success(
            list.Select(e => RecordPersonalUtangEntry.ToDto(e, userIdentityId, relationship.IsSharedLinked))
                .ToList());
    }
}

public sealed class RecordPersonalUtangEntry
{
    private readonly IPersonalDebtRelationshipRepository _relationships;
    private readonly IPersonalUtangEntryRepository _entries;
    private readonly IPersonalContactRepository _contacts;
    private readonly IPersonalAccountSettingsRepository _settings;
    private readonly IPersonalInAppNotificationRepository _notifications;
    private readonly IAuditWriter _auditWriter;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public RecordPersonalUtangEntry(
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

        try
        {
            var signedDelta = PersonalDebtRelationship.ComputeSignedDelta(
                entryType,
                request.Amount,
                request.AdjustmentDelta);
            var entry = relationship.RecordEntry(
                actingUserIdentityId,
                entryType,
                request.Amount,
                signedDelta,
                _clock.UtcNow,
                request.ExpectedVersion,
                request.Notes,
                request.DueDateUtc);

            await _relationships.UpdateAsync(relationship, cancellationToken).ConfigureAwait(false);
            await _entries.AddAsync(entry, cancellationToken).ConfigureAwait(false);

            if (entry.Status is PersonalUtangEntryStatus.Pending)
            {
                await PersonalUtangEntryNotifications.NotifyCounterpartyPendingAsync(
                    relationship,
                    entry,
                    actingUserIdentityId,
                    _settings,
                    _notifications,
                    _clock,
                    cancellationToken).ConfigureAwait(false);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            var auditAction = entry.Status is PersonalUtangEntryStatus.Pending
                ? PlatformAuditActions.PersonalUtangEntryProposed
                : PlatformAuditActions.PersonalUtangEntryRecorded;
            var summary = entry.Status is PersonalUtangEntryStatus.Pending
                ? $"{entryType} entry proposed for counterparty confirmation."
                : $"{entryType} entry recorded.";
            await _auditWriter.WriteAsync(
                $"platform-user:{actingUserIdentityId.Value:D}",
                AuditActorType.PlatformUser,
                auditAction,
                nameof(PersonalUtangEntry),
                entry.Id.Value.ToString("D"),
                AuditOutcome.Succeeded,
                summary: summary,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            return ApplicationResult<PersonalUtangEntryDto>.Success(
                ToDto(entry, actingUserIdentityId, relationship.IsSharedLinked));
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

    internal static PersonalUtangEntryDto ToDto(
        PersonalUtangEntry entry,
        PlatformUserId viewerUserIdentityId,
        bool isSharedLedger)
    {
        var isPending = entry.Status is PersonalUtangEntryStatus.Pending;
        var isProposer = entry.CreatedByUserIdentityId == viewerUserIdentityId;
        var canResolve = isPending && isSharedLedger && !isProposer;
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
            IsSharedLedger: isSharedLedger);
    }
}

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
            title: "Personal Utang entry proposed",
            preview: "A counterparty proposed a Personal Utang entry for your review.",
            entry,
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
            entry,
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
            entry,
            settings,
            notifications,
            clock,
            cancellationToken).ConfigureAwait(false);
    }

    private static async Task TryNotifyAsync(
        PlatformUserId recipient,
        string title,
        string preview,
        PersonalUtangEntry entry,
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
            relatedType: "PersonalUtangEntry",
            clock.UtcNow,
            relatedId: entry.Id.Value.ToString("D"));
        await notifications.AddAsync(notification, cancellationToken).ConfigureAwait(false);
    }
}
