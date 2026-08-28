using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Identity;
using ExItS.Platform.Application.Personal;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Personal;

namespace ExItS.Platform.Application.Organizations;

public sealed record StaffInviteTargetDto(
    string PublicUserId,
    string DisplayName,
    Guid UserIdentityId);

/// <summary>Resolve Personal EX-ID / Personal QR for Organization staff invitation (not ownership transfer).</summary>
public sealed class ResolveStaffInviteTarget
{
    private readonly IPlatformUserRepository _users;

    public ResolveStaffInviteTarget(IPlatformUserRepository users) => _users = users;

    public async Task<ApplicationResult<StaffInviteTargetDto>> ExecuteAsync(
        string input,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return ApplicationResult<StaffInviteTargetDto>.Failure(
                DomainErrorCodes.InvalidInvitationToken,
                "Enter an ExItS ID (EX-####-####) or scan a Personal QR.");
        }

        var trimmed = input.Trim();
        if (LooksLikeQrEnvelope(trimmed))
        {
            try
            {
                var parsed = ExItsQrEnvelope.Parse(trimmed);
                if (parsed.Purpose == ExItsQrPurpose.Organization)
                {
                    return ApplicationResult<StaffInviteTargetDto>.Failure(
                        DomainErrorCodes.InvalidInvitationToken,
                        "This is a Business QR. Scan their Personal QR instead.");
                }

                if (parsed.Purpose == ExItsQrPurpose.PosDeviceRegistration)
                {
                    return ApplicationResult<StaffInviteTargetDto>.Failure(
                        DomainErrorCodes.InvalidInvitationToken,
                        "This code is for registering a POS device.");
                }
            }
            catch (DomainException)
            {
                // Fall through to PublicUserIdRules.
            }
        }

        string publicUserId;
        try
        {
            publicUserId = PublicUserIdRules.Normalize(trimmed);
        }
        catch (DomainException)
        {
            return ApplicationResult<StaffInviteTargetDto>.Failure(
                DomainErrorCodes.InvalidInvitationToken,
                "Enter an ExItS ID (EX-####-####) or scan a Personal QR.");
        }

        var user = await _users.GetByPublicUserIdAsync(publicUserId, cancellationToken).ConfigureAwait(false);
        if (user is null || user.Status != AccountStatus.Active)
        {
            return ApplicationResult<StaffInviteTargetDto>.Failure(
                ApplicationErrorCodes.UserNotFound,
                "We couldn't find this ExItS account.");
        }

        if (user.IsOrganizationScopedStaff)
        {
            return ApplicationResult<StaffInviteTargetDto>.Failure(
                DomainErrorCodes.InvalidInvitationToken,
                "Invite a Personal ExItS account, not an organization staff login.");
        }

        return ApplicationResult<StaffInviteTargetDto>.Success(
            new StaffInviteTargetDto(user.PublicUserId!, user.DisplayName, user.Id.Value));
    }

    private static bool LooksLikeQrEnvelope(string value) =>
        value.StartsWith("exits://", StringComparison.OrdinalIgnoreCase);
}

public sealed class CreateOrganizationInvitationForPersonal
{
    private readonly IPlatformOrganizationRepository _organizations;
    private readonly IOrganizationInvitationRepository _invitations;
    private readonly IOrganizationMembershipRepository _memberships;
    private readonly IPlatformUserRepository _users;
    private readonly IPublicOrganizationIdGenerator _publicOrganizationIds;
    private readonly ResolveStaffInviteTarget _resolveTarget;
    private readonly IPersonalInAppNotificationRepository? _personalNotifications;
    private readonly IPersonalAccountSettingsRepository? _personalSettings;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public CreateOrganizationInvitationForPersonal(
        IPlatformOrganizationRepository organizations,
        IOrganizationInvitationRepository invitations,
        IOrganizationMembershipRepository memberships,
        IPlatformUserRepository users,
        IPublicOrganizationIdGenerator publicOrganizationIds,
        ResolveStaffInviteTarget resolveTarget,
        IPlatformUnitOfWork unitOfWork,
        IClock clock,
        IPersonalInAppNotificationRepository? personalNotifications = null,
        IPersonalAccountSettingsRepository? personalSettings = null)
    {
        _organizations = organizations;
        _invitations = invitations;
        _memberships = memberships;
        _users = users;
        _publicOrganizationIds = publicOrganizationIds;
        _resolveTarget = resolveTarget;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _personalNotifications = personalNotifications;
        _personalSettings = personalSettings;
    }

    public async Task<ApplicationResult<OrganizationInvitationDto>> ExecuteAsync(
        PlatformOrganizationId organizationId,
        string publicUserIdOrQrPayload,
        PlatformUserId? invitedByUserId,
        string? productRole = null,
        string? branch = null,
        CancellationToken cancellationToken = default)
    {
        var organization = await _organizations.GetByIdAsync(organizationId, cancellationToken).ConfigureAwait(false);
        if (organization is null)
        {
            return ApplicationResult<OrganizationInvitationDto>.Failure(
                ApplicationErrorCodes.OrganizationNotFound,
                "Platform Organization was not found.");
        }

        if (organization.Status != OrganizationStatus.Active)
        {
            return ApplicationResult<OrganizationInvitationDto>.Failure(
                DomainErrorCodes.OrganizationNotActive,
                "Invitations can only be created for an active Platform Organization.");
        }

        var resolved = await _resolveTarget.ExecuteAsync(publicUserIdOrQrPayload, cancellationToken)
            .ConfigureAwait(false);
        if (!resolved.IsSuccess)
        {
            return ApplicationResult<OrganizationInvitationDto>.Failure(
                resolved.ErrorCode!,
                resolved.ErrorMessage!);
        }

        var target = await _users
            .GetByIdAsync(PlatformUserId.From(resolved.Value!.UserIdentityId), cancellationToken)
            .ConfigureAwait(false);
        if (target is null || target.Status != AccountStatus.Active || target.IsOrganizationScopedStaff)
        {
            return ApplicationResult<OrganizationInvitationDto>.Failure(
                ApplicationErrorCodes.UserNotFound,
                "We couldn't find this ExItS account.");
        }

        if (invitedByUserId is not null && target.Id == invitedByUserId)
        {
            return ApplicationResult<OrganizationInvitationDto>.Failure(
                DomainErrorCodes.AuthorizationDenied,
                "You're already the owner of this business.");
        }

        var existingMembership = await _memberships
            .FindActiveByUserAndOrganizationAsync(target.Id, organizationId, cancellationToken)
            .ConfigureAwait(false);
        if (existingMembership is not null)
        {
            return ApplicationResult<OrganizationInvitationDto>.Failure(
                ApplicationErrorCodes.MembershipConflict,
                "This person is already part of your business.");
        }

        var existingStaff = await _users
            .FindActiveStaffByHomeOrgAndContactEmailAsync(
                organizationId,
                target.NormalizedContactEmail ?? target.NormalizedEmail,
                cancellationToken)
            .ConfigureAwait(false);
        if (existingStaff is not null)
        {
            return ApplicationResult<OrganizationInvitationDto>.Failure(
                ApplicationErrorCodes.MembershipConflict,
                "This person is already part of your business.");
        }

        var utcNow = _clock.UtcNow;
        var pending = await _invitations
            .FindPendingByOrganizationAndTargetUserAsync(organizationId, target.Id, cancellationToken)
            .ConfigureAwait(false);
        if (pending is not null)
        {
            if (pending.IsExpired(utcNow))
            {
                pending.MarkExpired(utcNow);
                await _invitations.UpdateAsync(pending, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                return ApplicationResult<OrganizationInvitationDto>.Failure(
                    ApplicationErrorCodes.InvitationConflict,
                    "Invitation already sent.");
            }
        }

        if (string.IsNullOrWhiteSpace(organization.PublicOrganizationId))
        {
            var publicOrgId = await _publicOrganizationIds
                .GenerateUniqueAsync(cancellationToken)
                .ConfigureAwait(false);
            organization.AssignPublicOrganizationId(publicOrgId, utcNow);
            await _organizations.UpdateAsync(organization, cancellationToken).ConfigureAwait(false);
        }

        var contactEmail = target.NormalizedContactEmail ?? target.NormalizedEmail;
        try
        {
            var (invitation, acceptToken) = OrganizationInvitation.Create(
                organizationId,
                contactEmail,
                OrganizationRole.OrganizationMember,
                utcNow,
                invitedByUserId,
                inviteeDisplayName: target.DisplayName,
                firstName: target.FirstName,
                lastName: target.LastName,
                branch: branch,
                productRole: productRole,
                targetPersonalUserId: target.Id,
                targetPublicUserId: target.PublicUserId);

            await _invitations.AddAsync(invitation, cancellationToken).ConfigureAwait(false);
            await TryCreatePersonalNotificationAsync(invitation, organization, cancellationToken)
                .ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return ApplicationResult<OrganizationInvitationDto>.Success(
                OrganizationInvitationQueryService.Map(
                    invitation,
                    acceptToken: null,
                    effectiveNow: utcNow));
        }
        catch (DomainException ex)
        {
            return ApplicationResult<OrganizationInvitationDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }

    private async Task TryCreatePersonalNotificationAsync(
        OrganizationInvitation invitation,
        PlatformOrganization organization,
        CancellationToken cancellationToken)
    {
        if (_personalNotifications is null || invitation.TargetPersonalUserId is null)
        {
            return;
        }

        if (_personalSettings is not null)
        {
            var settings = await _personalSettings
                .GetByUserAsync(invitation.TargetPersonalUserId, cancellationToken)
                .ConfigureAwait(false);
            if (settings is not null && !settings.InAppNotificationsEnabled)
            {
                return;
            }
        }

        var relatedId = invitation.Id.Value.ToString("D");
        var existing = await _personalNotifications
            .FindByRecipientRelatedAsync(
                invitation.TargetPersonalUserId,
                OrganizationStaffInvitationNotificationTypes.PersonalPendingInvite,
                relatedId,
                cancellationToken)
            .ConfigureAwait(false);
        if (existing is not null)
        {
            return;
        }

        var orgName = string.IsNullOrWhiteSpace(organization.DisplayName)
            ? "a business"
            : organization.DisplayName;
        var roleLabel = string.IsNullOrWhiteSpace(invitation.ProductRole)
            ? "Staff"
            : ProductRoleDisplay.ToDisplayLabel(invitation.ProductRole);
        var notification = PersonalInAppNotification.Create(
            invitation.TargetPersonalUserId,
            title: "Staff invitation",
            preview: $"{orgName} invited you to join their team as {roleLabel}.",
            relatedType: OrganizationStaffInvitationNotificationTypes.PersonalPendingInvite,
            utcNow: _clock.UtcNow,
            relatedId: relatedId);
        await _personalNotifications.AddAsync(notification, cancellationToken).ConfigureAwait(false);
    }
}

public sealed class DeclineOrganizationInvitationForPersonal
{
    private readonly IOrganizationInvitationRepository _invitations;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public DeclineOrganizationInvitationForPersonal(
        IOrganizationInvitationRepository invitations,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _invitations = invitations;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<OrganizationInvitationDto>> ExecuteAsync(
        OrganizationInvitationId invitationId,
        PlatformUserId personalUserId,
        CancellationToken cancellationToken = default)
    {
        var invitation = await _invitations.GetByIdAsync(invitationId, cancellationToken).ConfigureAwait(false);
        if (invitation is null)
        {
            return ApplicationResult<OrganizationInvitationDto>.Failure(
                ApplicationErrorCodes.InvitationNotFound,
                "Invitation was not found.");
        }

        try
        {
            if (invitation.IsExpired(_clock.UtcNow))
            {
                invitation.MarkExpired(_clock.UtcNow);
                await _invitations.UpdateAsync(invitation, cancellationToken).ConfigureAwait(false);
                await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                return ApplicationResult<OrganizationInvitationDto>.Failure(
                    DomainErrorCodes.InvitationExpired,
                    "Invitation has expired.");
            }

            invitation.Decline(personalUserId, _clock.UtcNow);
            await _invitations.UpdateAsync(invitation, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<OrganizationInvitationDto>.Success(
                OrganizationInvitationQueryService.Map(invitation, effectiveNow: _clock.UtcNow));
        }
        catch (DomainException ex)
        {
            return ApplicationResult<OrganizationInvitationDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class ListPendingOrganizationInvitationsForPersonalUser
{
    private readonly IOrganizationInvitationRepository _invitations;
    private readonly IPlatformOrganizationRepository _organizations;
    private readonly IClock _clock;

    public ListPendingOrganizationInvitationsForPersonalUser(
        IOrganizationInvitationRepository invitations,
        IPlatformOrganizationRepository organizations,
        IClock clock)
    {
        _invitations = invitations;
        _organizations = organizations;
        _clock = clock;
    }

    public async Task<ApplicationResult<IReadOnlyList<OrganizationInvitationDto>>> ExecuteAsync(
        PlatformUserId personalUserId,
        CancellationToken cancellationToken = default)
    {
        var pending = await _invitations
            .ListPendingByTargetPersonalUserIdAsync(personalUserId, cancellationToken)
            .ConfigureAwait(false);
        var now = _clock.UtcNow;
        var result = new List<OrganizationInvitationDto>();
        foreach (var invitation in pending)
        {
            if (invitation.IsExpired(now))
            {
                try
                {
                    invitation.MarkExpired(now);
                    await _invitations.UpdateAsync(invitation, cancellationToken).ConfigureAwait(false);
                }
                catch (DomainException)
                {
                    // Already transitioned.
                }

                continue;
            }

            var org = await _organizations.GetByIdAsync(invitation.OrganizationId, cancellationToken)
                .ConfigureAwait(false);
            var mapped = OrganizationInvitationQueryService.Map(invitation, effectiveNow: now) with
            {
                OrganizationDisplayName = org?.DisplayName,
                TargetPublicUserId = invitation.TargetPublicUserId,
                TargetPersonalUserId = invitation.TargetPersonalUserId?.Value
            };
            result.Add(mapped);
        }

        return ApplicationResult<IReadOnlyList<OrganizationInvitationDto>>.Success(result);
    }
}
