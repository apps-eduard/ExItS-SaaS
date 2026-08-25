using ExItS.Platform.Application.Audit;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Identity;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Audit;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Personal;

namespace ExItS.Platform.Application.Personal;

/// <summary>
/// Shared resolve + relationship promotion for Personal ExItS ID linking.
/// </summary>
internal static class PersonalContactLinkSupport
{
    public static async Task<ApplicationResult<PlatformUser>> ResolvePersonalLinkTargetAsync(
        PlatformUserId ownerUserIdentityId,
        string? publicUserId,
        Guid? linkedUserIdentityId,
        IPlatformUserRepository users,
        CancellationToken cancellationToken)
    {
        PlatformUser? target = null;
        if (!string.IsNullOrWhiteSpace(publicUserId))
        {
            string normalized;
            try
            {
                normalized = PublicUserIdRules.TryExtractFromQrPayload(publicUserId);
            }
            catch (DomainException)
            {
                return ApplicationResult<PlatformUser>.Failure(
                    DomainErrorCodes.InvalidPublicUserId,
                    "ExItS ID format is invalid.");
            }

            target = await users.GetByPublicUserIdAsync(normalized, cancellationToken).ConfigureAwait(false);
            if (target is null || target.Status is not AccountStatus.Active)
            {
                return ApplicationResult<PlatformUser>.Failure(
                    ApplicationErrorCodes.UserNotFound,
                    "No active user matched that ExItS ID.");
            }

            if (linkedUserIdentityId is Guid providedId && providedId != target.Id.Value)
            {
                return ApplicationResult<PlatformUser>.Failure(
                    ApplicationErrorCodes.PersonalContactLinkInvalid,
                    "ExItS ID does not match the confirmed person.");
            }
        }
        else if (linkedUserIdentityId is Guid linkedId)
        {
            target = await users.GetByIdAsync(PlatformUserId.From(linkedId), cancellationToken)
                .ConfigureAwait(false);
            if (target is null || target.Status is not AccountStatus.Active)
            {
                return ApplicationResult<PlatformUser>.Failure(
                    ApplicationErrorCodes.UserNotFound,
                    "No active user matched that ExItS ID.");
            }
        }
        else
        {
            return ApplicationResult<PlatformUser>.Failure(
                ApplicationErrorCodes.PersonalContactLinkInvalid,
                "A Personal ExItS ID is required to link this contact.");
        }

        if (target.Id == ownerUserIdentityId)
        {
            return ApplicationResult<PlatformUser>.Failure(
                ApplicationErrorCodes.PersonalContactLinkInvalid,
                "Cannot link a contact to yourself.");
        }

        if (target.IsOrganizationScopedStaff)
        {
            return ApplicationResult<PlatformUser>.Failure(
                ApplicationErrorCodes.PersonalContactLinkInvalid,
                "Only Personal ExItS accounts can be linked here.");
        }

        if (string.IsNullOrWhiteSpace(target.PublicUserId))
        {
            return ApplicationResult<PlatformUser>.Failure(
                ApplicationErrorCodes.PersonalContactLinkInvalid,
                "That person does not have an ExItS ID yet.");
        }

        return ApplicationResult<PlatformUser>.Success(target);
    }

    /// <summary>
    /// Promote active relationships that still reference this contact to the linked Personal user.
    /// Same relationship ID / history / balance — Contact participant → User participant.
    /// </summary>
    public static async Task PromoteRelationshipsForLinkedContactAsync(
        PlatformUserId ownerUserIdentityId,
        PersonalContact contact,
        PlatformUserId linkedUserIdentityId,
        IPersonalDebtRelationshipRepository relationships,
        IAuditWriter auditWriter,
        IClock clock,
        CancellationToken cancellationToken)
    {
        if (contact.LinkedUserIdentityId is null || contact.LinkedUserIdentityId != linkedUserIdentityId)
        {
            return;
        }

        var owned = await relationships.ListForUserAsync(ownerUserIdentityId, cancellationToken)
            .ConfigureAwait(false);
        foreach (var relationship in owned)
        {
            if (relationship.Status is not PersonalDebtRelationshipStatus.Active)
            {
                continue;
            }

            var isCreditorContact = relationship.CreditorContactId == contact.Id;
            var isDebtorContact = relationship.DebtorContactId == contact.Id;
            if (!isCreditorContact && !isDebtorContact)
            {
                continue;
            }

            if (relationship.IsSharedLinked)
            {
                continue;
            }

            relationship.AuthorizeLinkedParticipant(contact.Id, linkedUserIdentityId, clock.UtcNow);
            await relationships.UpdateAsync(relationship, cancellationToken).ConfigureAwait(false);

            await auditWriter.WriteAsync(
                $"platform-user:{ownerUserIdentityId.Value:D}",
                AuditActorType.PlatformUser,
                PlatformAuditActions.PersonalUtangParticipantAuthorized,
                nameof(PersonalDebtRelationship),
                relationship.Id.Value.ToString("D"),
                AuditOutcome.Succeeded,
                summary: "Personal Utang relationship promoted to linked Personal user after ExItS ID link.",
                cancellationToken: cancellationToken).ConfigureAwait(false);
        }
    }
}
