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
    string? AcceptToken = null);

public sealed class OrganizationInvitationQueryService
{
    private readonly IOrganizationInvitationRepository _invitations;
    private readonly IClock _clock;

    public OrganizationInvitationQueryService(IOrganizationInvitationRepository invitations, IClock clock)
    {
        _invitations = invitations;
        _clock = clock;
    }

    public async Task<OrganizationInvitationDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var invitation = await _invitations.GetByIdAsync(OrganizationInvitationId.From(id), cancellationToken)
            .ConfigureAwait(false);
        return invitation is null ? null : Map(invitation, effectiveNow: _clock.UtcNow);
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
        var mapped = items
            .Select(i => Map(i, effectiveNow: now))
            .Where(i => status is null || string.Equals(i.Status, status.Value.ToString(), StringComparison.Ordinal))
            .ToList();

        return new PagedResult<OrganizationInvitationDto>(
            mapped,
            total,
            Math.Max(page ?? 1, 1),
            take);
    }

    public static OrganizationInvitationDto Map(
        OrganizationInvitation invitation,
        string? acceptToken = null,
        DateTimeOffset? effectiveNow = null) =>
        new(
            invitation.Id.Value,
            invitation.OrganizationId.Value,
            OrganizationInvitation.InvitationType,
            invitation.NormalizedEmail,
            invitation.Role.ToString(),
            effectiveNow is not null && invitation.IsExpired(effectiveNow.Value)
                ? nameof(InvitationStatus.Expired)
                : invitation.Status.ToString(),
            invitation.InvitedByUserId?.Value,
            invitation.CreatedAtUtc,
            invitation.UpdatedAtUtc,
            invitation.ExpiresAtUtc,
            invitation.AcceptedAtUtc,
            invitation.RevokedAtUtc,
            invitation.AcceptedByUserId?.Value,
            acceptToken);
}

public sealed class CreateOrganizationInvitation
{
    private readonly IPlatformOrganizationRepository _organizations;
    private readonly IOrganizationInvitationRepository _invitations;
    private readonly IOrganizationMembershipRepository _memberships;
    private readonly IPlatformUserRepository _users;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public CreateOrganizationInvitation(
        IPlatformOrganizationRepository organizations,
        IOrganizationInvitationRepository invitations,
        IOrganizationMembershipRepository memberships,
        IPlatformUserRepository users,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _organizations = organizations;
        _invitations = invitations;
        _memberships = memberships;
        _users = users;
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
        CancellationToken cancellationToken = default)
    {
        if (!actorHasPlatformManageMemberships
            && actorMembershipRole == OrganizationRole.OrganizationAdministrator
            && role == OrganizationRole.OrganizationOwner)
        {
            return ApplicationResult<OrganizationInvitationDto>.Failure(
                DomainErrorCodes.OrganizationOwnerAssignmentDenied,
                "Organization Administrators cannot invite OrganizationOwner.");
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
            var existingUser = await _users.GetByNormalizedEmailAsync(normalizedEmail, cancellationToken)
                .ConfigureAwait(false);
            if (existingUser is not null)
            {
                var current = await _memberships
                    .FindCurrentByUserAndOrganizationAsync(existingUser.Id, organizationId, cancellationToken)
                    .ConfigureAwait(false);
                if (current is not null)
                {
                    return ApplicationResult<OrganizationInvitationDto>.Failure(
                        ApplicationErrorCodes.MembershipConflict,
                        "A current membership already exists for this user and organization.");
                }
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

            var (invitation, acceptToken) = OrganizationInvitation.Create(
                organizationId,
                normalizedEmail,
                role,
                _clock.UtcNow,
                invitedByUserId);
            await _invitations.AddAsync(invitation, cancellationToken).ConfigureAwait(false);
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
