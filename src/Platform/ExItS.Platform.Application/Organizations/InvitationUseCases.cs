using ExItS.Platform.Application.Access;
using ExItS.Platform.Application.Audit;
using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Identity;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Audit;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Products;
using Microsoft.Extensions.Options;

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
    string? InvitationStatus = null,
    Guid? TargetPersonalUserId = null,
    string? TargetPublicUserId = null,
    DateTimeOffset? DeclinedAtUtc = null,
    string? OrganizationDisplayName = null);

public sealed record AcceptOrganizationInvitationResultDto(
    Guid UserId,
    string StaffLogin,
    string ContactEmail,
    string OrganizationDisplayName,
    Guid OrganizationId,
    Guid MembershipId,
    string Role,
    Guid? LinkedPersonalUserId = null);

public sealed class OrganizationInvitationQueryService
{
    private readonly IOrganizationInvitationRepository _invitations;
    private readonly IClock _clock;

    public OrganizationInvitationQueryService(
        IOrganizationInvitationRepository invitations,
        IClock clock)
    {
        _invitations = invitations;
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

        return Map(invitation, acceptToken: null, effectiveNow: _clock.UtcNow);
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
            .Select(invitation => Map(invitation, acceptToken: null, effectiveNow: now))
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
            invitationStatus,
            invitation.TargetPersonalUserId?.Value,
            invitation.TargetPublicUserId,
            invitation.DeclinedAtUtc);
    }
}

public sealed class CreateOrganizationInvitation
{
    private readonly IPlatformOrganizationRepository _organizations;
    private readonly IOrganizationInvitationRepository _invitations;
    private readonly IPlatformUserRepository _users;
    private readonly IPublicOrganizationIdGenerator _publicOrganizationIds;
    private readonly IPlatformAuthOutboundMessageSink _messages;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public CreateOrganizationInvitation(
        IPlatformOrganizationRepository organizations,
        IOrganizationInvitationRepository invitations,
        IPlatformUserRepository users,
        IPublicOrganizationIdGenerator publicOrganizationIds,
        IPlatformAuthOutboundMessageSink messages,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _organizations = organizations;
        _invitations = invitations;
        _users = users;
        _publicOrganizationIds = publicOrganizationIds;
        _messages = messages;
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
        bool requireEmailVerification = true,
        CancellationToken cancellationToken = default)
    {
        _ = phone;
        _ = employeeCode;
        _ = requireEmailVerification;
        _ = actorMembershipRole;
        _ = actorHasPlatformManageMemberships;

        if (!OrganizationRoleDisplay.IsAssignableOrganizationStaffRole(role))
        {
            return ApplicationResult<OrganizationInvitationDto>.Failure(
                DomainErrorCodes.InvalidOrganizationRole,
                "Organization staff role is Staff only. MVP supports a single Organization Owner created at Start a Business.");
        }

        if (role == OrganizationRole.OrganizationOwner)
        {
            return ApplicationResult<OrganizationInvitationDto>.Failure(
                DomainErrorCodes.OrganizationOwnerUniqueViolation,
                "MVP allows only one Organization Owner per organization.");
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
            var utcNow = _clock.UtcNow;
            var normalizedContactEmail = PlatformUser.NormalizeEmail(email);
            var resolvedDisplayName = ResolveInviteDisplayName(displayName, firstName, lastName, normalizedContactEmail);

            if (string.IsNullOrWhiteSpace(organization.PublicOrganizationId))
            {
                var publicOrgId = await _publicOrganizationIds
                    .GenerateUniqueAsync(cancellationToken)
                    .ConfigureAwait(false);
                organization.AssignPublicOrganizationId(publicOrgId, utcNow);
                await _organizations.UpdateAsync(organization, cancellationToken).ConfigureAwait(false);
            }

            var pending = await _invitations
                .FindPendingByOrganizationAndEmailAsync(organizationId, normalizedContactEmail, cancellationToken)
                .ConfigureAwait(false);
            if (pending is not null && !pending.IsExpired(utcNow))
            {
                return ApplicationResult<OrganizationInvitationDto>.Failure(
                    ApplicationErrorCodes.InvitationConflict,
                    "A pending invitation already exists for this email in the organization.");
            }

            if (pending is not null && pending.IsExpired(utcNow))
            {
                pending.MarkExpired(utcNow);
                await _invitations.UpdateAsync(pending, cancellationToken).ConfigureAwait(false);
            }

            var existingStaff = await _users
                .FindActiveStaffByHomeOrgAndContactEmailAsync(
                    organizationId,
                    normalizedContactEmail,
                    cancellationToken)
                .ConfigureAwait(false);
            if (existingStaff is not null)
            {
                return ApplicationResult<OrganizationInvitationDto>.Failure(
                    ApplicationErrorCodes.MembershipConflict,
                    "An active organization staff identity already exists for this contact email.");
            }

            var (invitation, acceptToken) = OrganizationInvitation.Create(
                organizationId,
                normalizedContactEmail,
                role,
                utcNow,
                invitedByUserId,
                inviteeDisplayName: resolvedDisplayName,
                firstName: firstName,
                lastName: lastName,
                branch: branch,
                productRole: productRole);
            await _invitations.AddAsync(invitation, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            await _messages.PublishAsync(
                new PlatformAuthOutboundMessage(
                    PlatformAuthOutboundMessageKinds.OrganizationStaffInvitation,
                    Guid.Empty,
                    normalizedContactEmail,
                    acceptToken,
                    invitation.ExpiresAtUtc,
                    OrganizationName: organization.DisplayName,
                    RoleDisplay: OrganizationRoleDisplay.ToDisplayLabel(role),
                    ContactEmail: normalizedContactEmail),
                cancellationToken).ConfigureAwait(false);

            return ApplicationResult<OrganizationInvitationDto>.Success(
                OrganizationInvitationQueryService.Map(
                    invitation,
                    acceptToken,
                    effectiveNow: utcNow));
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
    private readonly IPlatformOrganizationRepository _organizations;
    private readonly IPlatformAuthOutboundMessageSink _messages;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public ResendOrganizationInvitation(
        IOrganizationInvitationRepository invitations,
        IPlatformOrganizationRepository organizations,
        IPlatformAuthOutboundMessageSink messages,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _invitations = invitations;
        _organizations = organizations;
        _messages = messages;
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

            var organization = await _organizations
                .GetByIdAsync(invitation.OrganizationId, cancellationToken)
                .ConfigureAwait(false);

            await _messages.PublishAsync(
                new PlatformAuthOutboundMessage(
                    PlatformAuthOutboundMessageKinds.OrganizationStaffInvitation,
                    Guid.Empty,
                    invitation.NormalizedEmail,
                    acceptToken,
                    invitation.ExpiresAtUtc,
                    OrganizationName: organization?.DisplayName,
                    RoleDisplay: OrganizationRoleDisplay.ToDisplayLabel(invitation.Role),
                    ContactEmail: invitation.NormalizedEmail),
                cancellationToken).ConfigureAwait(false);

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

/// <summary>
/// Accepts an organization staff invitation: anonymous (no Personal) or authenticated Personal (formal person-link).
/// Durable mutations run under <see cref="IPlatformUnitOfWork.ExecuteWithOrganizationLockAsync"/>.
/// </summary>
public sealed class AcceptOrganizationInvitation
{
    private readonly IOrganizationInvitationRepository _invitations;
    private readonly IPlatformOrganizationRepository _organizations;
    private readonly IPlatformUserRepository _users;
    private readonly IPlatformUserCredentialRepository _credentials;
    private readonly IAccountProfileRepository _profiles;
    private readonly IStaffLoginNameAllocator _staffLoginNames;
    private readonly IPublicOrganizationIdGenerator _publicOrganizationIds;
    private readonly IPlatformPasswordHasher _hasher;
    private readonly EnsureAccountProfilesForUser _ensureProfiles;
    private readonly AddOrganizationMembership _addMembership;
    private readonly AssignProductLocalRole _assignProductLocalRole;
    private readonly IPlatformAuthOutboundMessageSink _messages;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IAuditWriter _audit;
    private readonly PlatformPasswordOptions _passwordOptions;

    public AcceptOrganizationInvitation(
        IOrganizationInvitationRepository invitations,
        IPlatformOrganizationRepository organizations,
        IPlatformUserRepository users,
        IPlatformUserCredentialRepository credentials,
        IAccountProfileRepository profiles,
        IStaffLoginNameAllocator staffLoginNames,
        IPublicOrganizationIdGenerator publicOrganizationIds,
        IPlatformPasswordHasher hasher,
        EnsureAccountProfilesForUser ensureProfiles,
        AddOrganizationMembership addMembership,
        AssignProductLocalRole assignProductLocalRole,
        IPlatformAuthOutboundMessageSink messages,
        IPlatformUnitOfWork unitOfWork,
        IClock clock,
        IAuditWriter audit,
        IOptions<PlatformPasswordOptions> passwordOptions)
    {
        _invitations = invitations;
        _organizations = organizations;
        _users = users;
        _credentials = credentials;
        _profiles = profiles;
        _staffLoginNames = staffLoginNames;
        _publicOrganizationIds = publicOrganizationIds;
        _hasher = hasher;
        _ensureProfiles = ensureProfiles;
        _addMembership = addMembership;
        _assignProductLocalRole = assignProductLocalRole;
        _messages = messages;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _audit = audit;
        _passwordOptions = passwordOptions.Value;
    }

    public Task<ApplicationResult<AcceptOrganizationInvitationResultDto>> ExecuteAsync(
        string acceptToken,
        string password,
        string? displayName = null,
        string? firstName = null,
        string? lastName = null,
        CancellationToken cancellationToken = default) =>
        CompleteAsync(
            acceptToken,
            password,
            authenticatedPersonalUserId: null,
            displayName,
            firstName,
            lastName,
            cancellationToken);

    public Task<ApplicationResult<AcceptOrganizationInvitationResultDto>> ExecuteForAuthenticatedPersonalAsync(
        PlatformUserId authenticatedPersonalUserId,
        string acceptToken,
        string password,
        string? displayName = null,
        string? firstName = null,
        string? lastName = null,
        CancellationToken cancellationToken = default) =>
        CompleteAsync(
            acceptToken,
            password,
            authenticatedPersonalUserId,
            displayName,
            firstName,
            lastName,
            cancellationToken);

    public async Task<ApplicationResult<AcceptOrganizationInvitationResultDto>> ExecuteAcceptByIdForPersonalAsync(
        PlatformUserId authenticatedPersonalUserId,
        OrganizationInvitationId invitationId,
        string password,
        string? displayName = null,
        string? firstName = null,
        string? lastName = null,
        CancellationToken cancellationToken = default)
    {
        var invitation = await _invitations.GetByIdAsync(invitationId, cancellationToken).ConfigureAwait(false);
        if (invitation is null
            || invitation.Status != InvitationStatus.Pending
            || invitation.TargetPersonalUserId is null
            || invitation.TargetPersonalUserId != authenticatedPersonalUserId)
        {
            return ApplicationResult<AcceptOrganizationInvitationResultDto>.Failure(
                ApplicationErrorCodes.InvitationNotFound,
                "Invitation was not found or is no longer pending.");
        }

        return await CompleteWithTokenHashAsync(
                invitation.TokenHash,
                password,
                authenticatedPersonalUserId,
                displayName,
                firstName,
                lastName,
                cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<ApplicationResult<AcceptOrganizationInvitationResultDto>> CompleteAsync(
        string acceptToken,
        string password,
        PlatformUserId? authenticatedPersonalUserId,
        string? displayName,
        string? firstName,
        string? lastName,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(acceptToken))
        {
            return ApplicationResult<AcceptOrganizationInvitationResultDto>.Failure(
                DomainErrorCodes.InvalidInvitationToken,
                "Invitation token is required.");
        }

        return await CompleteWithTokenHashAsync(
                OrganizationInvitation.HashToken(acceptToken),
                password,
                authenticatedPersonalUserId,
                displayName,
                firstName,
                lastName,
                cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<ApplicationResult<AcceptOrganizationInvitationResultDto>> CompleteWithTokenHashAsync(
        string tokenHash,
        string password,
        PlatformUserId? authenticatedPersonalUserId,
        string? displayName,
        string? firstName,
        string? lastName,
        CancellationToken cancellationToken)
    {
        var policyError = PlatformPasswordPolicy.Validate(password, _passwordOptions);
        if (policyError is not null)
        {
            return ApplicationResult<AcceptOrganizationInvitationResultDto>.Failure(
                ApplicationErrorCodes.PasswordInvalid,
                policyError);
        }

        var preliminary = await _invitations.FindPendingByTokenHashAsync(tokenHash, cancellationToken)
            .ConfigureAwait(false);
        if (preliminary is null)
        {
            return ApplicationResult<AcceptOrganizationInvitationResultDto>.Failure(
                ApplicationErrorCodes.InvitationNotFound,
                "Invitation was not found or is no longer pending.");
        }

        ApplicationResult<AcceptOrganizationInvitationResultDto>? outcome = null;
        PlatformAuthOutboundMessage? outbound = null;

        try
        {
            await _unitOfWork
                .ExecuteWithOrganizationLockAsync(
                    preliminary.OrganizationId.Value,
                    async ct =>
                    {
                        var locked = await ExecuteLockedAsync(
                                tokenHash,
                                password,
                                authenticatedPersonalUserId,
                                displayName,
                                firstName,
                                lastName,
                                ct)
                            .ConfigureAwait(false);
                        outcome = locked.Result;
                        outbound = locked.Outbound;
                    },
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<AcceptOrganizationInvitationResultDto>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<AcceptOrganizationInvitationResultDto>.Failure(ex.ErrorCode, ex.Message);
        }

        if (outcome is null)
        {
            return ApplicationResult<AcceptOrganizationInvitationResultDto>.Failure(
                ApplicationErrorCodes.DomainViolation,
                "Invitation acceptance could not be completed.");
        }

        if (outcome.IsSuccess && outbound is not null)
        {
            await _messages.PublishAsync(outbound, cancellationToken).ConfigureAwait(false);
        }

        return outcome;
    }

    private sealed record LockedAcceptOutcome(
        ApplicationResult<AcceptOrganizationInvitationResultDto> Result,
        PlatformAuthOutboundMessage? Outbound);

    private async Task<LockedAcceptOutcome> ExecuteLockedAsync(
        string tokenHash,
        string password,
        PlatformUserId? authenticatedPersonalUserId,
        string? displayName,
        string? firstName,
        string? lastName,
        CancellationToken cancellationToken)
    {
        var invitation = await _invitations.FindPendingByTokenHashAsync(tokenHash, cancellationToken)
            .ConfigureAwait(false);
        if (invitation is null)
        {
            return new LockedAcceptOutcome(
                ApplicationResult<AcceptOrganizationInvitationResultDto>.Failure(
                    ApplicationErrorCodes.InvitationNotFound,
                    "Invitation was not found or is no longer pending."),
                Outbound: null);
        }

        var utcNow = _clock.UtcNow;
        if (invitation.IsExpired(utcNow))
        {
            invitation.MarkExpired(utcNow);
            await _invitations.UpdateAsync(invitation, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return new LockedAcceptOutcome(
                ApplicationResult<AcceptOrganizationInvitationResultDto>.Failure(
                    DomainErrorCodes.InvitationExpired,
                    "Invitation has expired."),
                Outbound: null);
        }

        var organization = await _organizations
            .GetByIdAsync(invitation.OrganizationId, cancellationToken)
            .ConfigureAwait(false);
        if (organization is null || organization.Status != OrganizationStatus.Active)
        {
            return new LockedAcceptOutcome(
                ApplicationResult<AcceptOrganizationInvitationResultDto>.Failure(
                    ApplicationErrorCodes.OrganizationNotFound,
                    "Platform Organization was not found or is not active."),
                Outbound: null);
        }

        if (string.IsNullOrWhiteSpace(organization.PublicOrganizationId))
        {
            var publicOrgId = await _publicOrganizationIds
                .GenerateUniqueAsync(cancellationToken)
                .ConfigureAwait(false);
            organization.AssignPublicOrganizationId(publicOrgId, utcNow);
            await _organizations.UpdateAsync(organization, cancellationToken).ConfigureAwait(false);
        }

        var contactEmail = invitation.NormalizedEmail;
        var existingStaff = await _users
            .FindActiveStaffByHomeOrgAndContactEmailAsync(
                invitation.OrganizationId,
                contactEmail,
                cancellationToken)
            .ConfigureAwait(false);
        if (existingStaff is not null)
        {
            return new LockedAcceptOutcome(
                ApplicationResult<AcceptOrganizationInvitationResultDto>.Failure(
                    ApplicationErrorCodes.MembershipConflict,
                    "An active organization staff identity already exists for this contact email."),
                Outbound: null);
        }

        var existingLoginPrincipal = await _users
            .GetByNormalizedEmailAsync(contactEmail, cancellationToken)
            .ConfigureAwait(false);
        PlatformUserId? linkedPersonalUserId = null;
        if (authenticatedPersonalUserId is not null)
        {
            var personalProof = await ProveEligiblePersonalForInvitationAsync(
                    authenticatedPersonalUserId,
                    invitation,
                    cancellationToken)
                .ConfigureAwait(false);
            if (personalProof.Failure is not null)
            {
                return new LockedAcceptOutcome(personalProof.Failure, Outbound: null);
            }

            linkedPersonalUserId = personalProof.PersonalUserId;
        }
        else if (invitation.IsExItsNativePersonalInvite)
        {
            return new LockedAcceptOutcome(
                ApplicationResult<AcceptOrganizationInvitationResultDto>.Failure(
                    ApplicationErrorCodes.InvitationRequiresAuthenticatedPersonal,
                    "Sign in with your Personal account to accept this invitation."),
                Outbound: null);
        }
        else if (existingLoginPrincipal is not null
                 && await HasActivePersonalAccountProfileAsync(existingLoginPrincipal.Id, cancellationToken)
                     .ConfigureAwait(false))
        {
            return new LockedAcceptOutcome(
                ApplicationResult<AcceptOrganizationInvitationResultDto>.Failure(
                    ApplicationErrorCodes.InvitationRequiresAuthenticatedPersonal,
                    "Sign in with your Personal account to accept this invitation."),
                Outbound: null);
        }

        var staffLogin = await _staffLoginNames
            .AllocateAsync(contactEmail, organization.PublicOrganizationId!, cancellationToken)
            .ConfigureAwait(false);
        var username = await AllocateUniqueUsernameAsync(
                StaffLoginNameRules.DeriveUsername(staffLogin),
                cancellationToken)
            .ConfigureAwait(false);

        var resolvedDisplayName = !string.IsNullOrWhiteSpace(displayName)
            ? displayName.Trim()
            : invitation.InviteeDisplayName
              ?? ResolveInviteDisplayName(invitation.FirstName, invitation.LastName, contactEmail);
        var resolvedFirstName = firstName ?? invitation.FirstName;
        var resolvedLastName = lastName ?? invitation.LastName;

        var staffUser = PlatformUser.CreateOrganizationStaff(
            username,
            staffLogin,
            contactEmail,
            invitation.OrganizationId,
            resolvedDisplayName,
            utcNow,
            resolvedFirstName,
            resolvedLastName,
            createdByUserId: invitation.InvitedByUserId,
            linkedPersonalUserId: linkedPersonalUserId);

        await _users.AddAsync(staffUser, cancellationToken).ConfigureAwait(false);

        var passwordHash = _hasher.HashPassword(password);
        var credential = PlatformUserCredential.Create(
            staffUser.Id,
            passwordHash,
            _hasher.Algorithm,
            utcNow);
        // Contact email was delivered the invite; treat as verified for staff login activation.
        credential.MarkEmailVerified(utcNow);
        await _credentials.AddAsync(credential, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await _ensureProfiles
            .ExecuteAsync(
                staffUser.Id,
                AccountClass.Organization,
                exclusivePreferredClass: true,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        var membershipResult = await _addMembership
            .ExecuteAsync(
                invitation.OrganizationId,
                staffUser.Id,
                invitation.Role,
                exclusiveOrganizationProfile: true,
                cancellationToken)
            .ConfigureAwait(false);
        if (!membershipResult.IsSuccess)
        {
            // Abort org-lock transaction — do not leave orphan staff/credential.
            throw new DomainException(
                membershipResult.ErrorCode!,
                membershipResult.ErrorMessage!);
        }

        if (!string.IsNullOrWhiteSpace(invitation.ProductRole))
        {
            await _assignProductLocalRole
                .ExecuteAsync(
                    invitation.OrganizationId,
                    staffUser.Id,
                    ProductCode.PinoyBusinessPos,
                    invitation.ProductRole,
                    invitation.InvitedByUserId ?? staffUser.Id,
                    reason: "organization staff invitation product role",
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }

        if (invitation.IsExItsNativePersonalInvite && linkedPersonalUserId is not null)
        {
            invitation.AcceptForPersonalTarget(staffUser.Id, linkedPersonalUserId, utcNow);
        }
        else
        {
            invitation.Accept(staffUser.Id, contactEmail, utcNow);
        }
        await _invitations.UpdateAsync(invitation, cancellationToken).ConfigureAwait(false);

        var staffLoginDisplay = StaffLoginNameRules.FormatForDisplay(staffUser.NormalizedEmail);
        var actorIdentifier = linkedPersonalUserId is not null
            ? $"platform-user:{linkedPersonalUserId.Value:D}"
            : $"platform-user:{staffUser.Id.Value:D}";

        await _audit.WriteAsync(
            actorIdentifier,
            AuditActorType.PlatformUser,
            PlatformAuditActions.InvitationAccepted,
            nameof(OrganizationInvitation),
            invitation.Id.Value.ToString("D"),
            AuditOutcome.Succeeded,
            organizationId: invitation.OrganizationId,
            summary: linkedPersonalUserId is null
                ? $"Organization staff invitation accepted. StaffUserId={staffUser.Id.Value:D}."
                : $"Organization staff invitation accepted by Personal principal. StaffUserId={staffUser.Id.Value:D}; LinkedPersonalUserId={linkedPersonalUserId.Value:D}.",
            cancellationToken: cancellationToken).ConfigureAwait(false);

        if (linkedPersonalUserId is not null)
        {
            await _audit.WriteAsync(
                $"platform-user:{linkedPersonalUserId.Value:D}",
                AuditActorType.PlatformUser,
                PlatformAuditActions.PersonLinkEstablished,
                nameof(PlatformUser),
                staffUser.Id.Value.ToString("D"),
                AuditOutcome.Succeeded,
                organizationId: invitation.OrganizationId,
                summary: $"Formal same-human person-link established. StaffUserId={staffUser.Id.Value:D}; LinkedPersonalUserId={linkedPersonalUserId.Value:D}. Correlation only; not authorization.",
                cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var outbound = new PlatformAuthOutboundMessage(
            PlatformAuthOutboundMessageKinds.OrganizationStaffInvitationAccepted,
            staffUser.Id.Value,
            contactEmail,
            OpaqueToken: string.Empty,
            ExpiresAtUtc: utcNow,
            OrganizationName: organization.DisplayName,
            RoleDisplay: OrganizationRoleDisplay.ToDisplayLabel(invitation.Role),
            ContactEmail: contactEmail,
            StaffLogin: staffLoginDisplay);

        return new LockedAcceptOutcome(
            ApplicationResult<AcceptOrganizationInvitationResultDto>.Success(
                new AcceptOrganizationInvitationResultDto(
                    staffUser.Id.Value,
                    staffLoginDisplay,
                    contactEmail,
                    organization.DisplayName,
                    organization.Id.Value,
                    membershipResult.Value!.Id.Value,
                    invitation.Role.ToString(),
                    linkedPersonalUserId?.Value)),
            outbound);
    }

    private async Task<(ApplicationResult<AcceptOrganizationInvitationResultDto>? Failure, PlatformUserId? PersonalUserId)>
        ProveEligiblePersonalForInvitationAsync(
            PlatformUserId authenticatedPersonalUserId,
            OrganizationInvitation invitation,
            CancellationToken cancellationToken)
    {
        var personal = await _users
            .GetByIdAsync(authenticatedPersonalUserId, cancellationToken)
            .ConfigureAwait(false);
        if (personal is null
            || personal.IsOrganizationScopedStaff
            || personal.Status != AccountStatus.Active
            || !await HasActivePersonalAccountProfileAsync(personal.Id, cancellationToken).ConfigureAwait(false))
        {
            return (
                ApplicationResult<AcceptOrganizationInvitationResultDto>.Failure(
                    ApplicationErrorCodes.InvitationNotFound,
                    "Invitation was not found or is no longer pending."),
                null);
        }

        if (invitation.TargetPersonalUserId is not null)
        {
            if (personal.Id != invitation.TargetPersonalUserId)
            {
                return (
                    ApplicationResult<AcceptOrganizationInvitationResultDto>.Failure(
                        ApplicationErrorCodes.InvitationNotFound,
                        "Invitation was not found or is no longer pending."),
                    null);
            }
        }
        else if (!string.Equals(personal.NormalizedEmail, invitation.NormalizedEmail, StringComparison.Ordinal))
        {
            return (
                ApplicationResult<AcceptOrganizationInvitationResultDto>.Failure(
                    ApplicationErrorCodes.InvitationNotFound,
                    "Invitation was not found or is no longer pending."),
                null);
        }

        var personalCredential = await _credentials
            .GetByUserIdAsync(personal.Id, cancellationToken)
            .ConfigureAwait(false);
        if (personalCredential?.EmailVerifiedAtUtc is null)
        {
            return (
                ApplicationResult<AcceptOrganizationInvitationResultDto>.Failure(
                    ApplicationErrorCodes.InvitationPersonalEmailUnverified,
                    "Verify your Personal email before accepting this invitation."),
                null);
        }

        return (null, personal.Id);
    }

    private async Task<bool> HasActivePersonalAccountProfileAsync(
        PlatformUserId userId,
        CancellationToken cancellationToken)
    {
        var profile = await _profiles
            .GetByUserAndClassAsync(userId, AccountClass.Personal, cancellationToken)
            .ConfigureAwait(false);
        return profile is not null && profile.IsActive;
    }

    private async Task<string> AllocateUniqueUsernameAsync(
        string usernameBase,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 8; attempt++)
        {
            var username = attempt == 0 ? usernameBase : $"{usernameBase}{attempt + 1}";
            if (username.Length < 3)
            {
                username = $"st{attempt + 1}{username}";
            }

            if (username.Length > 64)
            {
                username = username[..64];
            }

            var (_, normalized) = PlatformUser.NormalizeUsername(username);
            if (await _users.GetByNormalizedUsernameAsync(normalized, cancellationToken).ConfigureAwait(false) is null)
            {
                return username;
            }
        }

        throw new DomainException(
            DomainErrorCodes.InvalidUsername,
            "Unable to allocate a unique username for the organization staff identity.");
    }

    private static string ResolveInviteDisplayName(string? firstName, string? lastName, string normalizedEmail)
    {
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

/// <summary>Pending Organization Staff invitations for an org-scoped staff identity (membership repair surface).</summary>
public sealed record PendingOrganizationInvitationForUserDto(
    Guid Id,
    Guid OrganizationId,
    string OrganizationDisplayName,
    string Role,
    string? ProductRole,
    DateTimeOffset ExpiresAtUtc,
    string Status);

public sealed class ListPendingOrganizationInvitationsForUser
{
    private readonly IOrganizationInvitationRepository _invitations;
    private readonly IPlatformUserRepository _users;
    private readonly IPlatformOrganizationRepository _organizations;
    private readonly IClock _clock;

    public ListPendingOrganizationInvitationsForUser(
        IOrganizationInvitationRepository invitations,
        IPlatformUserRepository users,
        IPlatformOrganizationRepository organizations,
        IClock clock)
    {
        _invitations = invitations;
        _users = users;
        _organizations = organizations;
        _clock = clock;
    }

    public async Task<ApplicationResult<IReadOnlyList<PendingOrganizationInvitationForUserDto>>> ExecuteAsync(
        PlatformUserId userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByIdAsync(userId, cancellationToken).ConfigureAwait(false);
        if (user is null || user.Status != AccountStatus.Active)
        {
            return ApplicationResult<IReadOnlyList<PendingOrganizationInvitationForUserDto>>.Failure(
                DomainErrorCodes.UserNotActive,
                "Listing invitations requires an active Platform User.");
        }

        // Personal identities never list/attach org staff invites — accept is token + password only.
        if (!user.IsOrganizationScopedStaff
            || user.HomeOrganizationId is null
            || string.IsNullOrWhiteSpace(user.NormalizedContactEmail))
        {
            return ApplicationResult<IReadOnlyList<PendingOrganizationInvitationForUserDto>>.Success(
                Array.Empty<PendingOrganizationInvitationForUserDto>());
        }

        var pending = await _invitations
            .ListPendingByNormalizedEmailAsync(user.NormalizedContactEmail, cancellationToken)
            .ConfigureAwait(false);
        var now = _clock.UtcNow;
        var list = new List<PendingOrganizationInvitationForUserDto>();
        foreach (var invitation in pending)
        {
            if (invitation.OrganizationId != user.HomeOrganizationId)
            {
                continue;
            }

            if (invitation.IsExpired(now))
            {
                invitation.MarkExpired(now);
                await _invitations.UpdateAsync(invitation, cancellationToken).ConfigureAwait(false);
                continue;
            }

            var org = await _organizations
                .GetByIdAsync(invitation.OrganizationId, cancellationToken)
                .ConfigureAwait(false);
            list.Add(new PendingOrganizationInvitationForUserDto(
                invitation.Id.Value,
                invitation.OrganizationId.Value,
                org?.DisplayName ?? invitation.OrganizationId.Value.ToString("D"),
                invitation.Role.ToString(),
                invitation.ProductRole,
                invitation.ExpiresAtUtc,
                nameof(InvitationStatus.Pending)));
        }

        return ApplicationResult<IReadOnlyList<PendingOrganizationInvitationForUserDto>>.Success(list);
    }
}

/// <summary>
/// Membership repair for an already-created org-scoped staff identity.
/// Personal identities must accept via the invite token + password flow.
/// </summary>
public sealed class AcceptOrganizationInvitationByIdForInvitee
{
    private readonly IOrganizationInvitationRepository _invitations;
    private readonly IPlatformUserRepository _users;
    private readonly AddOrganizationMembership _addMembership;
    private readonly AssignProductLocalRole _assignProductLocalRole;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public AcceptOrganizationInvitationByIdForInvitee(
        IOrganizationInvitationRepository invitations,
        IPlatformUserRepository users,
        AddOrganizationMembership addMembership,
        AssignProductLocalRole assignProductLocalRole,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _invitations = invitations;
        _users = users;
        _addMembership = addMembership;
        _assignProductLocalRole = assignProductLocalRole;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<OrganizationMembership>> ExecuteAsync(
        Guid invitationId,
        PlatformUserId acceptingUserId,
        CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByIdAsync(acceptingUserId, cancellationToken).ConfigureAwait(false);
        if (user is null || user.Status != AccountStatus.Active)
        {
            return ApplicationResult<OrganizationMembership>.Failure(
                DomainErrorCodes.UserNotActive,
                "Accepting invitations requires an active Platform User.");
        }

        var invitation = await _invitations
            .GetByIdAsync(OrganizationInvitationId.From(invitationId), cancellationToken)
            .ConfigureAwait(false);
        if (invitation is null || invitation.Status != InvitationStatus.Pending)
        {
            return ApplicationResult<OrganizationMembership>.Failure(
                ApplicationErrorCodes.InvitationNotFound,
                "Invitation was not found or is no longer pending.");
        }

        if (!user.IsOrganizationScopedStaff
            || user.HomeOrganizationId != invitation.OrganizationId
            || !string.Equals(user.NormalizedContactEmail, invitation.NormalizedEmail, StringComparison.Ordinal))
        {
            return ApplicationResult<OrganizationMembership>.Failure(
                DomainErrorCodes.InvitationEmailMismatch,
                "Organization staff must accept invitations via the invite token and password flow.");
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

            var membershipResult = await _addMembership
                .ExecuteAsync(
                    invitation.OrganizationId,
                    acceptingUserId,
                    invitation.Role,
                    exclusiveOrganizationProfile: true,
                    cancellationToken)
                .ConfigureAwait(false);
            if (!membershipResult.IsSuccess)
            {
                return membershipResult;
            }

            if (!string.IsNullOrWhiteSpace(invitation.ProductRole))
            {
                await _assignProductLocalRole
                    .ExecuteAsync(
                        invitation.OrganizationId,
                        acceptingUserId,
                        ProductCode.PinoyBusinessPos,
                        invitation.ProductRole,
                        invitation.InvitedByUserId ?? acceptingUserId,
                        reason: "organization staff invitation product role",
                        cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
            }

            invitation.Accept(acceptingUserId, invitation.NormalizedEmail, _clock.UtcNow);
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
