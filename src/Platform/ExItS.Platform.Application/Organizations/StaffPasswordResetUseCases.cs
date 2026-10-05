using ExItS.Platform.Application.Audit;
using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Identity;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Audit;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;
using Microsoft.Extensions.Options;

namespace ExItS.Platform.Application.Organizations;

public sealed record StaffPasswordResetStatusDto(
    Guid Id,
    string Status,
    DateTimeOffset ExpiresAtUtc);

public sealed record OrganizationStaffPasswordResetDto(
    Guid Id,
    Guid StaffUserId,
    string StaffDisplayName,
    string StaffLogin,
    Guid MembershipId,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset ExpiresAtUtc);

public interface IStaffPasswordResetCoordinator
{
    /// <summary>
    /// Opens an organization approval request for an org-scoped staff identity.
    /// Does not issue a reset token or change the password.
    /// </summary>
    Task EnsurePendingForStaffAsync(PlatformUser staffUser, CancellationToken cancellationToken = default);
}

public sealed class StaffPasswordResetCoordinator : IStaffPasswordResetCoordinator
{
    private readonly IOrganizationMembershipRepository _memberships;
    private readonly IStaffPasswordResetRequestRepository _requests;
    private readonly IAuditWriter _auditWriter;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public StaffPasswordResetCoordinator(
        IOrganizationMembershipRepository memberships,
        IStaffPasswordResetRequestRepository requests,
        IAuditWriter auditWriter,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _memberships = memberships;
        _requests = requests;
        _auditWriter = auditWriter;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task EnsurePendingForStaffAsync(
        PlatformUser staffUser,
        CancellationToken cancellationToken = default)
    {
        if (!staffUser.IsOrganizationScopedStaff || staffUser.HomeOrganizationId is null)
        {
            return;
        }

        var membership = await _memberships
            .FindCurrentByUserAndOrganizationAsync(staffUser.Id, staffUser.HomeOrganizationId, cancellationToken)
            .ConfigureAwait(false);
        if (membership is null || membership.Status != MembershipStatus.Active)
        {
            return;
        }

        var requestedBy = staffUser.LinkedPersonalUserId ?? staffUser.Id;
        await StaffPasswordResetRequests
            .EnsurePendingAsync(
                _requests,
                _auditWriter,
                _unitOfWork,
                _clock,
                membership,
                staffUser,
                requestedBy,
                cancellationToken)
            .ConfigureAwait(false);
    }
}

public sealed class RequestStaffPasswordReset
{
    private readonly IPlatformUserRepository _users;
    private readonly IOrganizationMembershipRepository _memberships;
    private readonly IStaffPasswordResetRequestRepository _requests;
    private readonly IAuditWriter _auditWriter;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public RequestStaffPasswordReset(
        IPlatformUserRepository users,
        IOrganizationMembershipRepository memberships,
        IStaffPasswordResetRequestRepository requests,
        IAuditWriter auditWriter,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _users = users;
        _memberships = memberships;
        _requests = requests;
        _auditWriter = auditWriter;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<StaffPasswordResetStatusDto>> ExecuteAsync(
        PlatformUserId personalUserId,
        Guid membershipId,
        CancellationToken cancellationToken = default)
    {
        var linked = await StaffPasswordResetRequests
            .RequireLinkedWorkplaceAsync(_users, _memberships, personalUserId, membershipId, cancellationToken)
            .ConfigureAwait(false);
        if (!linked.IsSuccess)
        {
            return ApplicationResult<StaffPasswordResetStatusDto>.Failure(linked.ErrorCode!, linked.ErrorMessage!);
        }

        var (membership, staffUser) = linked.Value;
        var request = await StaffPasswordResetRequests
            .EnsurePendingAsync(
                _requests,
                _auditWriter,
                _unitOfWork,
                _clock,
                membership,
                staffUser,
                personalUserId,
                cancellationToken)
            .ConfigureAwait(false);
        return ApplicationResult<StaffPasswordResetStatusDto>.Success(StaffPasswordResetRequests.ToStatus(request));
    }
}

public sealed class CompleteStaffPasswordReset
{
    private readonly IPlatformUserRepository _users;
    private readonly IOrganizationMembershipRepository _memberships;
    private readonly IStaffPasswordResetRequestRepository _requests;
    private readonly IPlatformUserCredentialRepository _credentials;
    private readonly IPlatformCredentialTokenRepository _tokens;
    private readonly IPlatformAuthSessionRepository _sessions;
    private readonly IPlatformAccessTokenRepository _accessTokens;
    private readonly IPlatformDeviceRecoveryCredentialRepository _recoveryCredentials;
    private readonly IPlatformPasswordHasher _hasher;
    private readonly IAuditWriter _auditWriter;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly PlatformPasswordOptions _passwordOptions;

    public CompleteStaffPasswordReset(
        IPlatformUserRepository users,
        IOrganizationMembershipRepository memberships,
        IStaffPasswordResetRequestRepository requests,
        IPlatformUserCredentialRepository credentials,
        IPlatformCredentialTokenRepository tokens,
        IPlatformAuthSessionRepository sessions,
        IPlatformAccessTokenRepository accessTokens,
        IPlatformDeviceRecoveryCredentialRepository recoveryCredentials,
        IPlatformPasswordHasher hasher,
        IAuditWriter auditWriter,
        IPlatformUnitOfWork unitOfWork,
        IClock clock,
        IOptions<PlatformPasswordOptions> passwordOptions)
    {
        _users = users;
        _memberships = memberships;
        _requests = requests;
        _credentials = credentials;
        _tokens = tokens;
        _sessions = sessions;
        _accessTokens = accessTokens;
        _recoveryCredentials = recoveryCredentials;
        _hasher = hasher;
        _auditWriter = auditWriter;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _passwordOptions = passwordOptions.Value;
    }

    public async Task<ApplicationResult<StaffPasswordResetStatusDto>> ExecuteAsync(
        PlatformUserId personalUserId,
        Guid membershipId,
        Guid requestId,
        string? newPassword,
        CancellationToken cancellationToken = default)
    {
        var policyError = PlatformPasswordPolicy.Validate(newPassword, _passwordOptions);
        if (policyError is not null)
        {
            return ApplicationResult<StaffPasswordResetStatusDto>.Failure(
                ApplicationErrorCodes.PasswordInvalid,
                policyError);
        }

        var linked = await StaffPasswordResetRequests
            .RequireLinkedWorkplaceAsync(_users, _memberships, personalUserId, membershipId, cancellationToken)
            .ConfigureAwait(false);
        if (!linked.IsSuccess)
        {
            return ApplicationResult<StaffPasswordResetStatusDto>.Failure(linked.ErrorCode!, linked.ErrorMessage!);
        }

        var (membership, staffUser) = linked.Value;
        StaffPasswordResetRequestId resetId;
        try
        {
            resetId = StaffPasswordResetRequestId.From(requestId);
        }
        catch (DomainException)
        {
            return NotFound();
        }

        var request = await _requests.GetByIdAsync(resetId, cancellationToken).ConfigureAwait(false);
        if (request is null
            || !request.StaffUserId.Equals(staffUser.Id)
            || !request.MembershipId.Equals(membership.Id)
            || !request.OrganizationId.Equals(membership.OrganizationId))
        {
            return NotFound();
        }

        var utcNow = _clock.UtcNow;
        if (request.IsPastExpiry(utcNow))
        {
            request.MarkExpired(utcNow);
            await _requests.UpdateAsync(request, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<StaffPasswordResetStatusDto>.Failure(
                DomainErrorCodes.StaffPasswordResetExpired,
                "This password reset request has expired. Ask the organization again.");
        }

        if (request.Status != StaffPasswordResetRequestStatus.Approved)
        {
            return ApplicationResult<StaffPasswordResetStatusDto>.Failure(
                ApplicationErrorCodes.StaffPasswordResetNotApproved,
                "The organization has not approved a password reset yet.");
        }

        var credential = await _credentials.GetByUserIdAsync(staffUser.Id, cancellationToken).ConfigureAwait(false);
        if (credential is null)
        {
            return ApplicationResult<StaffPasswordResetStatusDto>.Failure(
                ApplicationErrorCodes.CredentialNotFound,
                "This workplace has no password to replace.");
        }

        try
        {
            request.Complete(utcNow);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<StaffPasswordResetStatusDto>.Failure(ex.ErrorCode, ex.Message);
        }

        credential.ReplacePasswordHash(_hasher.HashPassword(newPassword!), _hasher.Algorithm, utcNow);
        await _requests.UpdateAsync(request, cancellationToken).ConfigureAwait(false);
        await _credentials.UpdateAsync(credential, cancellationToken).ConfigureAwait(false);
        await _tokens.InvalidateActiveForUserAsync(
            staffUser.Id,
            PlatformCredentialTokenPurpose.PasswordReset,
            utcNow,
            cancellationToken).ConfigureAwait(false);
        await CredentialSessionInvalidation.RevokeAllAsync(
            _sessions,
            _accessTokens,
            _recoveryCredentials,
            _auditWriter,
            staffUser.Id,
            utcNow,
            "Staff workplace sessions revoked after an approved password reset.",
            cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await _auditWriter.WriteAsync(
            $"platform-user:{personalUserId.Value:D}",
            AuditActorType.PlatformUser,
            PlatformAuditActions.StaffPasswordResetCompleted,
            nameof(StaffPasswordResetRequest),
            request.Id.Value.ToString("D"),
            AuditOutcome.Succeeded,
            organizationId: request.OrganizationId,
            summary: "Approved staff password reset completed (password not recorded).",
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return ApplicationResult<StaffPasswordResetStatusDto>.Success(StaffPasswordResetRequests.ToStatus(request));
    }

    private static ApplicationResult<StaffPasswordResetStatusDto> NotFound() =>
        ApplicationResult<StaffPasswordResetStatusDto>.Failure(
            ApplicationErrorCodes.StaffPasswordResetNotFound,
            "Password reset request was not found.");
}

public sealed class ListOrganizationStaffPasswordResets
{
    private readonly IStaffPasswordResetRequestRepository _requests;
    private readonly IPlatformUserRepository _users;
    private readonly IClock _clock;

    public ListOrganizationStaffPasswordResets(
        IStaffPasswordResetRequestRepository requests,
        IPlatformUserRepository users,
        IClock clock)
    {
        _requests = requests;
        _users = users;
        _clock = clock;
    }

    public async Task<ApplicationResult<IReadOnlyList<OrganizationStaffPasswordResetDto>>> ExecuteAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        PlatformOrganizationId orgId;
        try
        {
            orgId = PlatformOrganizationId.From(organizationId);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<IReadOnlyList<OrganizationStaffPasswordResetDto>>.Failure(ex.ErrorCode, ex.Message);
        }

        var utcNow = _clock.UtcNow;
        var open = await _requests.ListOpenByOrganizationAsync(orgId, cancellationToken).ConfigureAwait(false);
        var items = new List<OrganizationStaffPasswordResetDto>();
        foreach (var request in open)
        {
            if (request.IsPastExpiry(utcNow))
            {
                continue;
            }

            var staff = await _users.GetByIdAsync(request.StaffUserId, cancellationToken).ConfigureAwait(false);
            if (staff is null)
            {
                continue;
            }

            items.Add(new OrganizationStaffPasswordResetDto(
                request.Id.Value,
                staff.Id.Value,
                staff.DisplayName,
                StaffLoginNameRules.FormatForDisplay(staff.NormalizedEmail),
                request.MembershipId.Value,
                request.Status.ToString(),
                request.CreatedAtUtc,
                request.ExpiresAtUtc));
        }

        return ApplicationResult<IReadOnlyList<OrganizationStaffPasswordResetDto>>.Success(items);
    }
}

public sealed class DecideStaffPasswordReset
{
    private readonly IStaffPasswordResetRequestRepository _requests;
    private readonly IPlatformUserRepository _users;
    private readonly IPlatformUserCredentialRepository _credentials;
    private readonly IPlatformCredentialTokenRepository _tokens;
    private readonly IPlatformSessionTokenService _tokenService;
    private readonly IPlatformAuthOutboundMessageSink _messages;
    private readonly IAuditWriter _auditWriter;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly PlatformCredentialLifecycleOptions _lifecycle;

    public DecideStaffPasswordReset(
        IStaffPasswordResetRequestRepository requests,
        IPlatformUserRepository users,
        IPlatformUserCredentialRepository credentials,
        IPlatformCredentialTokenRepository tokens,
        IPlatformSessionTokenService tokenService,
        IPlatformAuthOutboundMessageSink messages,
        IAuditWriter auditWriter,
        IPlatformUnitOfWork unitOfWork,
        IClock clock,
        IOptions<PlatformCredentialLifecycleOptions> lifecycle)
    {
        _requests = requests;
        _users = users;
        _credentials = credentials;
        _tokens = tokens;
        _tokenService = tokenService;
        _messages = messages;
        _auditWriter = auditWriter;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _lifecycle = lifecycle.Value;
    }

    public async Task<ApplicationResult<StaffPasswordResetStatusDto>> ExecuteAsync(
        Guid organizationId,
        Guid requestId,
        PlatformUserId actorUserId,
        bool approve,
        CancellationToken cancellationToken = default)
    {
        PlatformOrganizationId orgId;
        StaffPasswordResetRequestId resetId;
        try
        {
            orgId = PlatformOrganizationId.From(organizationId);
            resetId = StaffPasswordResetRequestId.From(requestId);
        }
        catch (DomainException)
        {
            return NotFound();
        }

        var request = await _requests.GetByIdAsync(resetId, cancellationToken).ConfigureAwait(false);
        if (request is null || !request.OrganizationId.Equals(orgId))
        {
            return NotFound();
        }

        var utcNow = _clock.UtcNow;
        if (request.IsPastExpiry(utcNow))
        {
            request.MarkExpired(utcNow);
            await _requests.UpdateAsync(request, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<StaffPasswordResetStatusDto>.Failure(
                DomainErrorCodes.StaffPasswordResetExpired,
                "This password reset request has expired.");
        }

        try
        {
            if (approve)
            {
                request.Approve(actorUserId, utcNow);
            }
            else
            {
                request.Deny(actorUserId, utcNow);
            }
        }
        catch (DomainException ex)
        {
            return ApplicationResult<StaffPasswordResetStatusDto>.Failure(ex.ErrorCode, ex.Message);
        }

        await _requests.UpdateAsync(request, cancellationToken).ConfigureAwait(false);
        if (approve)
        {
            await EmailUnlinkedStaffAsync(request, utcNow, cancellationToken).ConfigureAwait(false);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await _auditWriter.WriteAsync(
            $"platform-user:{actorUserId.Value:D}",
            AuditActorType.PlatformUser,
            approve
                ? PlatformAuditActions.StaffPasswordResetApproved
                : PlatformAuditActions.StaffPasswordResetDenied,
            nameof(StaffPasswordResetRequest),
            request.Id.Value.ToString("D"),
            AuditOutcome.Succeeded,
            organizationId: request.OrganizationId,
            summary: approve
                ? "Organization approved a staff password reset (password not recorded)."
                : "Organization denied a staff password reset.",
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return ApplicationResult<StaffPasswordResetStatusDto>.Success(StaffPasswordResetRequests.ToStatus(request));
    }

    private async Task EmailUnlinkedStaffAsync(
        StaffPasswordResetRequest request,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        var staff = await _users.GetByIdAsync(request.StaffUserId, cancellationToken).ConfigureAwait(false);
        if (staff is null || staff.LinkedPersonalUserId is not null || staff.Status != AccountStatus.Active)
        {
            return;
        }

        var deliveryEmail = staff.NormalizedContactEmail;
        if (string.IsNullOrWhiteSpace(deliveryEmail))
        {
            return;
        }

        var credential = await _credentials.GetByUserIdAsync(staff.Id, cancellationToken).ConfigureAwait(false);
        if (credential is null)
        {
            return;
        }

        await _tokens.InvalidateActiveForUserAsync(
            staff.Id,
            PlatformCredentialTokenPurpose.PasswordReset,
            utcNow,
            cancellationToken).ConfigureAwait(false);
        var opaque = _tokenService.CreateOpaqueToken();
        var lifetime = TimeSpan.FromMinutes(Math.Max(5, _lifecycle.PasswordResetTokenLifetimeMinutes));
        var token = PlatformCredentialToken.Create(
            staff.Id,
            PlatformCredentialTokenPurpose.PasswordReset,
            _tokenService.HashToken(opaque),
            utcNow,
            lifetime);
        await _tokens.AddAsync(token, cancellationToken).ConfigureAwait(false);
        await _messages.PublishAsync(
            new PlatformAuthOutboundMessage(
                PlatformAuthOutboundMessageKinds.PasswordReset,
                staff.Id.Value,
                deliveryEmail,
                opaque,
                token.ExpiresAtUtc,
                PublicSurface: PlatformAuthPublicSurfaces.PinoyBusinessPos),
            cancellationToken).ConfigureAwait(false);
    }

    private static ApplicationResult<StaffPasswordResetStatusDto> NotFound() =>
        ApplicationResult<StaffPasswordResetStatusDto>.Failure(
            ApplicationErrorCodes.StaffPasswordResetNotFound,
            "Password reset request was not found.");
}

internal static class StaffPasswordResetRequests
{
    public static async Task<ApplicationResult<(OrganizationMembership Membership, PlatformUser StaffUser)>> RequireLinkedWorkplaceAsync(
        IPlatformUserRepository users,
        IOrganizationMembershipRepository memberships,
        PlatformUserId personalUserId,
        Guid membershipId,
        CancellationToken cancellationToken)
    {
        OrganizationMembershipId id;
        try
        {
            id = OrganizationMembershipId.From(membershipId);
        }
        catch (DomainException)
        {
            return NotFound();
        }

        var membership = await memberships.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (membership is null || membership.Status != MembershipStatus.Active)
        {
            return NotFound();
        }

        var staffUser = await users.GetByIdAsync(membership.UserId, cancellationToken).ConfigureAwait(false);
        if (staffUser is null
            || !staffUser.IsOrganizationScopedStaff
            || staffUser.HomeOrganizationId is null
            || !staffUser.HomeOrganizationId.Equals(membership.OrganizationId)
            || staffUser.LinkedPersonalUserId is null
            || !staffUser.LinkedPersonalUserId.Equals(personalUserId)
            || staffUser.Status != AccountStatus.Active)
        {
            return NotFound();
        }

        return ApplicationResult<(OrganizationMembership, PlatformUser)>.Success((membership, staffUser));
    }

    public static async Task<StaffPasswordResetRequest> EnsurePendingAsync(
        IStaffPasswordResetRequestRepository requests,
        IAuditWriter auditWriter,
        IPlatformUnitOfWork unitOfWork,
        IClock clock,
        OrganizationMembership membership,
        PlatformUser staffUser,
        PlatformUserId requestedByUserId,
        CancellationToken cancellationToken)
    {
        var utcNow = clock.UtcNow;
        var open = await requests.FindOpenByStaffUserAsync(staffUser.Id, cancellationToken).ConfigureAwait(false);
        if (open is not null)
        {
            if (open.IsPastExpiry(utcNow))
            {
                open.MarkExpired(utcNow);
                await requests.UpdateAsync(open, cancellationToken).ConfigureAwait(false);
                await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            else
            {
                return open;
            }
        }

        var created = StaffPasswordResetRequest.Create(
            membership.OrganizationId,
            staffUser.Id,
            membership.Id,
            requestedByUserId,
            utcNow);
        await requests.AddAsync(created, cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await auditWriter.WriteAsync(
            $"platform-user:{requestedByUserId.Value:D}",
            AuditActorType.PlatformUser,
            PlatformAuditActions.StaffPasswordResetRequested,
            nameof(StaffPasswordResetRequest),
            created.Id.Value.ToString("D"),
            AuditOutcome.Succeeded,
            organizationId: created.OrganizationId,
            summary: "Staff password reset requested. Organization approval is required (password not recorded).",
            cancellationToken: cancellationToken).ConfigureAwait(false);
        return created;
    }

    public static StaffPasswordResetStatusDto ToStatus(StaffPasswordResetRequest request) =>
        new(request.Id.Value, request.Status.ToString(), request.ExpiresAtUtc);

    private static ApplicationResult<(OrganizationMembership, PlatformUser)> NotFound() =>
        ApplicationResult<(OrganizationMembership, PlatformUser)>.Failure(
            ApplicationErrorCodes.StaffPasswordResetNotFound,
            "Workplace was not found.");
}
