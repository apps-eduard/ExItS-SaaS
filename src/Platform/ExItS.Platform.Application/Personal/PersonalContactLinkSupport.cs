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
    /// Private Personal debts stay owner-private after ExItS ID link.
    /// Sharing is opt-in per new debt (Share with …) or via explicit Utang invitation accept —
    /// never by silently promoting historical private ledgers.
    /// </summary>
    public static Task PromoteRelationshipsForLinkedContactAsync(
        PlatformUserId ownerUserIdentityId,
        PersonalContact contact,
        PlatformUserId linkedUserIdentityId,
        IPersonalDebtRelationshipRepository relationships,
        IAuditWriter auditWriter,
        IClock clock,
        CancellationToken cancellationToken) =>
        Task.CompletedTask;
}
