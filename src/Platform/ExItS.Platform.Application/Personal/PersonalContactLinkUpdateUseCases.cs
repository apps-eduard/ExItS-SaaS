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
                    CreatePersonalContact.ToDto(contact));
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
            if (!string.IsNullOrWhiteSpace(linkedUser.PublicUserId))
            {
                contact.ResolveIdentity(linkedUser.Id, linkedUser.PublicUserId, _clock.UtcNow);
            }
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
                CreatePersonalContact.ToDto(contact));
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
    private readonly IPlatformUserRepository _users;
    private readonly IAuditWriter _auditWriter;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public UpdatePersonalContact(
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

            PlatformUser? linkedUser = null;
            if (contact.LinkedUserIdentityId is not null)
            {
                linkedUser = await _users.GetByIdAsync(contact.LinkedUserIdentityId, cancellationToken)
                    .ConfigureAwait(false);
            }

            if (contact.IsLinked)
            {
                // Linked account email is never editable. Phone is locked when the contact or
                // linked account already has a phone; otherwise a local phone may still be set.
                var lockPhone = !string.IsNullOrWhiteSpace(contact.Phone)
                    || !string.IsNullOrWhiteSpace(linkedUser?.Phone);
                var phone = lockPhone ? contact.Phone : request.Phone;
                contact.UpdateDetails(request.DisplayName, phone, contact.Email, _clock.UtcNow);
            }
            else
            {
                contact.UpdateDetails(request.DisplayName, request.Phone, request.Email, _clock.UtcNow);
            }

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

            return ApplicationResult<PersonalContactDto>.Success(
                CreatePersonalContact.ToDto(contact));
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
