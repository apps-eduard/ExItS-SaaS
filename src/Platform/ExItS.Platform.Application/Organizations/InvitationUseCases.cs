using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Identity;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.Application.Organizations;

public sealed record OrganizationInvitationDto(
    Guid Id,
    Guid OrganizationId,
    string InvitationType,
    string Email,
    string Role,
    string Status,
    Guid? InvitedByUserId,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    DateTimeOffset ExpiresAtUtc,
    DateTimeOffset? AcceptedAtUtc,
    DateTimeOffset? RevokedAtUtc,
    Guid? AcceptedByUserId,
    string? AcceptToken = null,
    string? RoleDisplay = null,
    string? InviteeDisplayName = null,
    string? FirstName = null,
    string? LastName = null,
    string? Branch = null,
    string? ProductRole = null,
    string? ProductRoleDisplay = null,
    string? InvitationStatus = null);

public sealed class OrganizationInvitationQueryService
{
    private readonly IOrganizationInvitationRepository _invitations;
    private readonly IPlatformUserRepository _users;
    private readonly IClock _clock;

    public OrganizationInvitationQueryService(
        IOrganizationInvitationRepository invitations,
        IPlatformUserRepository users,
        IClock clock)
    {
        _invitations = invitations;
        _users = users;
        _clock = clock;
    }

    public async Task<OrganizationInvitationDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var invitation = await _invitations.GetByIdAsync(OrganizationInvitationId.From(id), cancellationToken)
            .ConfigureAwait(false);
        if (invitation is null)
        {
            return null;
        }

        var user = await _users.GetByNormalizedEmailAsync(invitation.NormalizedEmail, cancellationToken)
            .ConfigureAwait(false);
        return Map(invitation, acceptToken: null, effectiveNow: _clock.UtcNow, user);
    }

    public async Task<PagedResult<OrganizationInvitationDto>> ListByOrganizationAsync(
        Guid organizationId,
        InvitationStatus? status,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken = default)
    {
        var (skip, take) = CatalogPagination.Normalize(page, pageSize);
        var (items, total) = await _invitations
            .ListByOrganizationAsync(PlatformOrganizationId.From(organizationId), status, skip, take, cancellationToken)
            .ConfigureAwait(false);

        var now = _clock.UtcNow;
        var mapped = new List<OrganizationInvitationDto>(items.Count);
        foreach (var invitation in items)
        {
            var user = await _users.GetByNormalizedEmailAsync(invitation.NormalizedEmail, cancellationToken)
                .ConfigureAwait(false);
            mapped.Add(Map(invitation, acceptToken: null, effectiveNow: now, user));
        }

        return new PagedResult<OrganizationInvitationDto>(
            mapped,
            total,
            Math.Max(page ?? 1, 1),
            take);
    }

    public static OrganizationInvitationDto Map(
        OrganizationInvitation invitation,
        string? acceptToken = null,
        DateTimeOffset? effectiveNow = null,
        PlatformUser? invitee = null)
    {
        var status = effectiveNow is not null && invitation.IsExpired(effectiveNow.Value)
            ? nameof(InvitationStatus.Expired)
            : invitation.Status.ToString();
        // Pending invitations are shown as Sent once created (email/token issued).
        // Status keeps the domain value for API filters; InvitationStatus is user-facing.
        var invitationStatus = status == nameof(InvitationStatus.Pending) ? "Sent" : status;
        var inviteeDisplayName = invitation.InviteeDisplayName
            ?? invitee?.DisplayName;
        var firstName = invitation.FirstName ?? invitee?.FirstName;
        var lastName = invitation.LastName ?? invitee?.LastName;
        var productRole = invitation.ProductRole;
        return new(
            invitation.Id.Value,
            invitation.OrganizationId.Value,
            OrganizationInvitation.InvitationType,
            invitation.NormalizedEmail,
            invitation.Role.ToString(),
            status,
            invitation.InvitedByUserId?.Value,
            invitation.CreatedAtUtc,
            invitation.UpdatedAtUtc,
            invitation.ExpiresAtUtc,
            invitation.AcceptedAtUtc,
            invitation.RevokedAtUtc,
            invitation.AcceptedByUserId?.Value,
            acceptToken,
            OrganizationRoleDisplay.ToDisplayLabel(invitation.Role),
            inviteeDisplayName,
            firstName,
            lastName,
            invitation.Branch,
            productRole,
            string.IsNullOrWhiteSpace(productRole) ? null : ProductRoleDisplay.ToDisplayLabel(productRole),
            invitationStatus);
    }
}

public sealed class CreateOrganizationInvitation
{
    private readonly IPlatformOrganizationRepository _organizations;
    private readonly IOrganizationInvitationRepository _invitations;
    private readonly IOrganizationMembershipRepository _memberships;
    private readonly IPlatformUserRepository _users;
    private readonly EnsureOrganizationStaffIdentity _ensureStaffIdentity;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public CreateOrganizationInvitation(
        IPlatformOrganizationRepository organizations,
        IOrganizationInvitationRepository invitations,
        IOrganizationMembershipRepository memberships,
        IPlatformUserRepository users,
        EnsureOrganizationStaffIdentity ensureStaffIdentity,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _organizations = organizations;
        _invitations = invitations;
        _memberships = memberships;
        _users = users;
        _ensureStaffIdentity = ensureStaffIdentity;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<OrganizationInvitationDto>> ExecuteAsync(
        PlatformOrganizationId organizationId,
        string email,
        OrganizationRole role,
        PlatformUserId? invitedByUserId,
        OrganizationRole? actorMembershipRole,
        bool actorHasPlatformManageMemberships,
        string? displayName = null,
        string? firstName = null,
        string? lastName = null,
        string? phone = null,
        string? employeeCode = null,
        string? branch = null,
        string? productRole = null,
        CancellationToken cancellationToken = default)
    {
        if (!OrganizationRoleDisplay.IsAssignableOrganizationStaffRole(role))
        {
            return ApplicationResult<OrganizationInvitationDto>.Failure(
                DomainErrorCodes.InvalidOrganizationRole,
                "Organization staff roles are Owner and Staff only.");
        }

        if (!actorHasPlatformManageMemberships
            && actorMembershipRole != OrganizationRole.OrganizationOwner
            && role == OrganizationRole.OrganizationOwner)
        {
            return ApplicationResult<OrganizationInvitationDto>.Failure(
                DomainErrorCodes.OrganizationOwnerAssignmentDenied,
                "Only Organization Owners can invite an Owner.");
        }

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

        try
        {
            var normalizedEmail = PlatformUser.NormalizeEmail(email);
            var resolvedDisplayName = ResolveInviteDisplayName(displayName, firstName, lastName, normalizedEmail);
            var staffIdentity = await _ensureStaffIdentity
                .ExecuteAsync(normalizedEmail, displayNameHint: resolvedDisplayName, cancellationToken)
                .ConfigureAwait(false);
            if (!staffIdentity.IsSuccess || staffIdentity.Value is null)
            {
                return ApplicationResult<OrganizationInvitationDto>.Failure(
                    staffIdentity.ErrorCode ?? ApplicationErrorCodes.DomainViolation,
                    staffIdentity.ErrorMessage ?? "Unable to provision Organization staff identity.");
            }

            var existingUser = staffIdentity.Value;
            var current = await _memberships
                .FindCurrentByUserAndOrganizationAsync(existingUser.Id, organizationId, cancellationToken)
                .ConfigureAwait(false);
            if (current is not null)
            {
                return ApplicationResult<OrganizationInvitationDto>.Failure(
                    ApplicationErrorCodes.MembershipConflict,
                    "A current membership already exists for this user and organization.");
            }

            var pending = await _invitations
                .FindPendingByOrganizationAndEmailAsync(organizationId, normalizedEmail, cancellationToken)
                .ConfigureAwait(false);
            if (pending is not null && !pending.IsExpired(_clock.UtcNow))
            {
                return ApplicationResult<OrganizationInvitationDto>.Failure(
                    ApplicationErrorCodes.InvitationConflict,
                    "A pending invitation already exists for this email in the organization.");
            }

            if (pending is not null && pending.IsExpired(_clock.UtcNow))
            {
                pending.MarkExpired(_clock.UtcNow);
                await _invitations.UpdateAsync(pending, cancellationToken).ConfigureAwait(false);
            }

            existingUser.UpdateStaffProfile(
                firstName,
                lastName,
                resolvedDisplayName,
                normalizedEmail,
                _clock.UtcNow,
                phone,
                employeeCode);
            await _users.UpdateAsync(existingUser, cancellationToken).ConfigureAwait(false);

            var (invitation, acceptToken) = OrganizationInvitation.Create(
                organizationId,
                normalizedEmail,
                role,
                _clock.UtcNow,
                invitedByUserId,
                inviteeDisplayName: resolvedDisplayName,
                firstName: firstName,
                lastName: lastName,
                branch: branch,
                productRole: productRole);
            await _invitations.AddAsync(invitation, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<OrganizationInvitationDto>.Success(
                OrganizationInvitationQueryService.Map(
                    invitation,
                    acceptToken,
                    effectiveNow: _clock.UtcNow,
                    invitee: existingUser));
        }
        catch (DomainException ex)
        {
            return ApplicationResult<OrganizationInvitationDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }

    private static string ResolveInviteDisplayName(
        string? displayName,
        string? firstName,
        string? lastName,
        string normalizedEmail)
    {
        if (!string.IsNullOrWhiteSpace(displayName))
        {
            return displayName.Trim();
        }

        var composed = string.Join(
            ' ',
            new[] { firstName?.Trim(), lastName?.Trim() }.Where(static s => !string.IsNullOrWhiteSpace(s)));
        if (!string.IsNullOrWhiteSpace(composed))
        {
            return composed;
        }

        return PlatformUsernameDerivation.DeriveFromEmail(normalizedEmail);
    }
}

public sealed class ResendOrganizationInvitation
{
    private readonly IOrganizationInvitationRepository _invitations;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public ResendOrganizationInvitation(
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
                    "Invitation has expired. Create a new invitation.");
            }

            var acceptToken = invitation.Resend(_clock.UtcNow);
            await _invitations.UpdateAsync(invitation, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<OrganizationInvitationDto>.Success(
                OrganizationInvitationQueryService.Map(invitation, acceptToken));
        }
        catch (DomainException ex)
        {
            return ApplicationResult<OrganizationInvitationDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class RevokeOrganizationInvitation
{
    private readonly IOrganizationInvitationRepository _invitations;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public RevokeOrganizationInvitation(
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
            invitation.Revoke(_clock.UtcNow);
            await _invitations.UpdateAsync(invitation, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<OrganizationInvitationDto>.Success(
                OrganizationInvitationQueryService.Map(invitation));
        }
        catch (DomainException ex)
        {
            return ApplicationResult<OrganizationInvitationDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class AcceptOrganizationInvitation
{
    private readonly IOrganizationInvitationRepository _invitations;
    private readonly IPlatformUserRepository _users;
    private readonly AddOrganizationMembership _addMembership;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public AcceptOrganizationInvitation(
        IOrganizationInvitationRepository invitations,
        IPlatformUserRepository users,
        AddOrganizationMembership addMembership,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _invitations = invitations;
        _users = users;
        _addMembership = addMembership;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<OrganizationMembership>> ExecuteAsync(
        string acceptToken,
        PlatformUserId acceptingUserId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(acceptToken))
        {
            return ApplicationResult<OrganizationMembership>.Failure(
                DomainErrorCodes.InvalidInvitationToken,
                "Invitation token is required.");
        }

        var user = await _users.GetByIdAsync(acceptingUserId, cancellationToken).ConfigureAwait(false);
        if (user is null || user.Status != AccountStatus.Active)
        {
            return ApplicationResult<OrganizationMembership>.Failure(
                DomainErrorCodes.UserNotActive,
                "Accepting invitations requires an active Platform User.");
        }

        var hash = OrganizationInvitation.HashToken(acceptToken);
        var invitation = await _invitations.FindPendingByTokenHashAsync(hash, cancellationToken)
            .ConfigureAwait(false);
        if (invitation is null)
        {
            return ApplicationResult<OrganizationMembership>.Failure(
                ApplicationErrorCodes.InvitationNotFound,
                "Invitation was not found or is no longer pending.");
        }

        try
        {
            if (invitation.IsExpired(_clock.UtcNow))
            {
                invitation.MarkExpired(_clock.UtcNow);
                await _invitations.UpdateAsync(invitation, cancellationToken).ConfigureAwait(false);
                await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                return ApplicationResult<OrganizationMembership>.Failure(
                    DomainErrorCodes.InvitationExpired,
                    "Invitation has expired.");
            }

            if (!string.Equals(user.NormalizedEmail, invitation.NormalizedEmail, StringComparison.Ordinal))
            {
                return ApplicationResult<OrganizationMembership>.Failure(
                    DomainErrorCodes.InvitationEmailMismatch,
                    "Invitation email does not match the accepting user.");
            }

            var membershipResult = await _addMembership
                .ExecuteAsync(invitation.OrganizationId, acceptingUserId, invitation.Role, cancellationToken)
                .ConfigureAwait(false);
            if (!membershipResult.IsSuccess)
            {
                return membershipResult;
            }

            invitation.Accept(acceptingUserId, user.NormalizedEmail, _clock.UtcNow);
            await _invitations.UpdateAsync(invitation, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return membershipResult;
        }
        catch (DomainException ex)
        {
            return ApplicationResult<OrganizationMembership>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}
