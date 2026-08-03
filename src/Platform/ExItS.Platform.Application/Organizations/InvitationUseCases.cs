using ExItS.Platform.Application.Access;
using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Identity;
using ExItS.Platform.Domain.Abstractions;
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
    private readonly IPlatformUserCredentialRepository _credentials;
    private readonly IPlatformCredentialTokenRepository _tokens;
    private readonly IPlatformSessionTokenService _tokenService;
    private readonly EnsureAccountProfilesForUser _ensureProfiles;
    private readonly IPlatformAuthOutboundMessageSink _messages;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly PlatformCredentialLifecycleOptions _lifecycle;

    public CreateOrganizationInvitation(
        IPlatformOrganizationRepository organizations,
        IOrganizationInvitationRepository invitations,
        IOrganizationMembershipRepository memberships,
        IPlatformUserRepository users,
        IPlatformUserCredentialRepository credentials,
        IPlatformCredentialTokenRepository tokens,
        IPlatformSessionTokenService tokenService,
        EnsureAccountProfilesForUser ensureProfiles,
        IPlatformAuthOutboundMessageSink messages,
        IPlatformUnitOfWork unitOfWork,
        IClock clock,
        IOptions<PlatformCredentialLifecycleOptions> lifecycle)
    {
        _organizations = organizations;
        _invitations = invitations;
        _memberships = memberships;
        _users = users;
        _credentials = credentials;
        _tokens = tokens;
        _tokenService = tokenService;
        _ensureProfiles = ensureProfiles;
        _messages = messages;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _lifecycle = lifecycle.Value;
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
            var normalizedEmail = PlatformUser.NormalizeEmail(email);
            var resolvedDisplayName = ResolveInviteDisplayName(displayName, firstName, lastName, normalizedEmail);

            var pending = await _invitations
                .FindPendingByOrganizationAndEmailAsync(organizationId, normalizedEmail, cancellationToken)
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

            var existingUser = await _users.GetByNormalizedEmailAsync(normalizedEmail, cancellationToken)
                .ConfigureAwait(false);
            PlatformUser invitee;
            var createdNewIdentity = false;

            if (existingUser is null)
            {
                invitee = await CreateOrganizationInviteeIdentityAsync(
                    normalizedEmail,
                    resolvedDisplayName,
                    firstName,
                    lastName,
                    phone,
                    employeeCode,
                    requireEmailVerification,
                    utcNow,
                    cancellationToken).ConfigureAwait(false);
                createdNewIdentity = true;
            }
            else
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

                // Existing identity: invitation only — do not invent profiles or duplicate users.
                invitee = existingUser;
            }

            var (invitation, acceptToken) = OrganizationInvitation.Create(
                organizationId,
                normalizedEmail,
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

            await DeliverInviteOutboundAsync(
                invitee,
                acceptToken,
                createdNewIdentity && requireEmailVerification,
                cancellationToken).ConfigureAwait(false);

            return ApplicationResult<OrganizationInvitationDto>.Success(
                OrganizationInvitationQueryService.Map(
                    invitation,
                    acceptToken,
                    effectiveNow: utcNow,
                    invitee: invitee));
        }
        catch (DomainException ex)
        {
            return ApplicationResult<OrganizationInvitationDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }

    private async Task<PlatformUser> CreateOrganizationInviteeIdentityAsync(
        string normalizedEmail,
        string resolvedDisplayName,
        string? firstName,
        string? lastName,
        string? phone,
        string? employeeCode,
        bool requireEmailVerification,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        var username = await AllocateUsernameFromEmailAsync(normalizedEmail, cancellationToken).ConfigureAwait(false);
        var user = requireEmailVerification
            ? PlatformUser.CreatePendingVerification(username, resolvedDisplayName, normalizedEmail, utcNow)
            : PlatformUser.Create(username, resolvedDisplayName, normalizedEmail, utcNow);

        user.UpdateStaffProfile(
            firstName,
            lastName,
            resolvedDisplayName,
            normalizedEmail,
            utcNow,
            phone,
            employeeCode);

        await _users.AddAsync(user, cancellationToken).ConfigureAwait(false);
        var credential = PlatformUserCredential.CreateForExternalLogin(user.Id, utcNow, emailVerified: false);
        await _credentials.AddAsync(credential, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await _ensureProfiles
            .ExecuteAsync(
                user.Id,
                AccountClass.Organization,
                exclusivePreferredClass: true,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return user;
    }

    private async Task DeliverInviteOutboundAsync(
        PlatformUser invitee,
        string acceptToken,
        bool sendActivationEmail,
        CancellationToken cancellationToken)
    {
        if (sendActivationEmail)
        {
            var utcNow = _clock.UtcNow;
            await _tokens.InvalidateActiveForUserAsync(
                invitee.Id,
                PlatformCredentialTokenPurpose.EmailVerification,
                utcNow,
                cancellationToken).ConfigureAwait(false);

            var opaque = _tokenService.CreateOpaqueToken();
            var lifetime = TimeSpan.FromHours(Math.Max(1, _lifecycle.EmailVerificationTokenLifetimeHours));
            var token = PlatformCredentialToken.Create(
                invitee.Id,
                PlatformCredentialTokenPurpose.EmailVerification,
                _tokenService.HashToken(opaque),
                utcNow,
                lifetime);
            await _tokens.AddAsync(token, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            await _messages.PublishAsync(
                new PlatformAuthOutboundMessage(
                    PlatformAuthOutboundMessageKinds.EmailVerification,
                    invitee.Id.Value,
                    invitee.NormalizedEmail,
                    opaque,
                    token.ExpiresAtUtc),
                cancellationToken).ConfigureAwait(false);
            return;
        }

        // Existing/active invitees receive the organization accept token.
        await _messages.PublishAsync(
            new PlatformAuthOutboundMessage(
                PlatformAuthOutboundMessageKinds.OrganizationStaffInvitation,
                invitee.Id.Value,
                invitee.NormalizedEmail,
                acceptToken,
                _clock.UtcNow.AddHours(OrganizationInvitation.DefaultLifetimeHours)),
            cancellationToken).ConfigureAwait(false);
    }

    private async Task<string> AllocateUsernameFromEmailAsync(
        string normalizedEmail,
        CancellationToken cancellationToken)
    {
        var usernameBase = PlatformUsernameDerivation.DeriveFromEmail(normalizedEmail);
        for (var attempt = 0; attempt < 8; attempt++)
        {
            var username = attempt == 0 ? usernameBase : $"{usernameBase}{attempt + 1}";
            if (username.Length < 3)
            {
                username = $"user{attempt + 1}{username}";
            }

            var (_, normalized) = PlatformUser.NormalizeUsername(username);
            if (await _users.GetByNormalizedUsernameAsync(normalized, cancellationToken).ConfigureAwait(false) is null)
            {
                return username;
            }
        }

        throw new DomainException(
            DomainErrorCodes.InvalidUsername,
            "Unable to allocate a unique username for the invited staff identity.");
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
    private readonly IPlatformUserRepository _users;
    private readonly IPlatformCredentialTokenRepository _tokens;
    private readonly IPlatformSessionTokenService _tokenService;
    private readonly IPlatformAuthOutboundMessageSink _messages;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly PlatformCredentialLifecycleOptions _lifecycle;

    public ResendOrganizationInvitation(
        IOrganizationInvitationRepository invitations,
        IPlatformUserRepository users,
        IPlatformCredentialTokenRepository tokens,
        IPlatformSessionTokenService tokenService,
        IPlatformAuthOutboundMessageSink messages,
        IPlatformUnitOfWork unitOfWork,
        IClock clock,
        IOptions<PlatformCredentialLifecycleOptions> lifecycle)
    {
        _invitations = invitations;
        _users = users;
        _tokens = tokens;
        _tokenService = tokenService;
        _messages = messages;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _lifecycle = lifecycle.Value;
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

            var invitee = await _users.GetByNormalizedEmailAsync(invitation.NormalizedEmail, cancellationToken)
                .ConfigureAwait(false);
            if (invitee is not null)
            {
                if (invitee.Status == AccountStatus.PendingVerification)
                {
                    var utcNow = _clock.UtcNow;
                    await _tokens.InvalidateActiveForUserAsync(
                        invitee.Id,
                        PlatformCredentialTokenPurpose.EmailVerification,
                        utcNow,
                        cancellationToken).ConfigureAwait(false);
                    var opaque = _tokenService.CreateOpaqueToken();
                    var lifetime = TimeSpan.FromHours(Math.Max(1, _lifecycle.EmailVerificationTokenLifetimeHours));
                    var token = PlatformCredentialToken.Create(
                        invitee.Id,
                        PlatformCredentialTokenPurpose.EmailVerification,
                        _tokenService.HashToken(opaque),
                        utcNow,
                        lifetime);
                    await _tokens.AddAsync(token, cancellationToken).ConfigureAwait(false);
                    await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                    await _messages.PublishAsync(
                        new PlatformAuthOutboundMessage(
                            PlatformAuthOutboundMessageKinds.EmailVerification,
                            invitee.Id.Value,
                            invitee.NormalizedEmail,
                            opaque,
                            token.ExpiresAtUtc),
                        cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    await _messages.PublishAsync(
                        new PlatformAuthOutboundMessage(
                            PlatformAuthOutboundMessageKinds.OrganizationStaffInvitation,
                            invitee.Id.Value,
                            invitee.NormalizedEmail,
                            acceptToken,
                            invitation.ExpiresAtUtc),
                        cancellationToken).ConfigureAwait(false);
                }
            }

            return ApplicationResult<OrganizationInvitationDto>.Success(
                OrganizationInvitationQueryService.Map(invitation, acceptToken, invitee: invitee));
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
    private readonly AssignProductLocalRole _assignProductLocalRole;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public AcceptOrganizationInvitation(
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
                .ExecuteAsync(
                    invitation.OrganizationId,
                    acceptingUserId,
                    invitation.Role,
                    exclusiveOrganizationProfile: false,
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

/// <summary>
/// After account activation, accept any still-pending Organization Staff invitations for the email.
/// </summary>
public sealed class AcceptPendingOrganizationInvitationsForUser
{
    private readonly IOrganizationInvitationRepository _invitations;
    private readonly AddOrganizationMembership _addMembership;
    private readonly AssignProductLocalRole _assignProductLocalRole;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public AcceptPendingOrganizationInvitationsForUser(
        IOrganizationInvitationRepository invitations,
        AddOrganizationMembership addMembership,
        AssignProductLocalRole assignProductLocalRole,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _invitations = invitations;
        _addMembership = addMembership;
        _assignProductLocalRole = assignProductLocalRole;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task ExecuteAsync(PlatformUser user, CancellationToken cancellationToken = default)
    {
        if (user.Status != AccountStatus.Active)
        {
            return;
        }

        var pending = await _invitations
            .ListPendingByNormalizedEmailAsync(user.NormalizedEmail, cancellationToken)
            .ConfigureAwait(false);
        foreach (var invitation in pending)
        {
            if (invitation.IsExpired(_clock.UtcNow))
            {
                invitation.MarkExpired(_clock.UtcNow);
                await _invitations.UpdateAsync(invitation, cancellationToken).ConfigureAwait(false);
                continue;
            }

            try
            {
                var membershipResult = await _addMembership
                    .ExecuteAsync(
                        invitation.OrganizationId,
                        user.Id,
                        invitation.Role,
                        exclusiveOrganizationProfile: false,
                        cancellationToken)
                    .ConfigureAwait(false);
                if (!membershipResult.IsSuccess)
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(invitation.ProductRole))
                {
                    await _assignProductLocalRole
                        .ExecuteAsync(
                            invitation.OrganizationId,
                            user.Id,
                            ProductCode.PinoyBusinessPos,
                            invitation.ProductRole,
                            invitation.InvitedByUserId ?? user.Id,
                            reason: "organization staff invitation product role",
                            cancellationToken: cancellationToken)
                        .ConfigureAwait(false);
                }

                invitation.Accept(user.Id, user.NormalizedEmail, _clock.UtcNow);
                await _invitations.UpdateAsync(invitation, cancellationToken).ConfigureAwait(false);
                await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (DomainException)
            {
                // Best-effort; activation still succeeded.
            }
        }
    }
}
