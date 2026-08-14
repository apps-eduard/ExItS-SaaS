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

namespace ExItS.Platform.Application.Organizations;

public sealed record OrganizationOwnershipTransferDto(
    Guid Id,
    Guid OrganizationId,
    string? OrganizationDisplayName,
    string? PublicOrganizationId,
    Guid FromOwnerUserId,
    Guid ToUserId,
    string? ToDisplayName,
    string? ToPublicUserId,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset ExpiresAtUtc,
    DateTimeOffset? AcceptedAtUtc,
    DateTimeOffset? DeclinedAtUtc,
    DateTimeOffset? CancelledAtUtc,
    DateTimeOffset? CompletedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record OwnershipTransferTargetDto(
    string PublicUserId,
    string DisplayName);

public sealed class ResolveOwnershipTransferTarget
{
    private readonly IPlatformUserRepository _users;

    public ResolveOwnershipTransferTarget(IPlatformUserRepository users) => _users = users;

    public async Task<ApplicationResult<OwnershipTransferTargetDto>> ExecuteAsync(
        string input,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return ApplicationResult<OwnershipTransferTargetDto>.Failure(
                DomainErrorCodes.OwnershipTransferTargetInvalid,
                "Enter a Personal ExItS ID (EX-####-####) or scan a Personal QR.");
        }

        var trimmed = input.Trim();

        // Reject Business / Device QR before attempting personal normalize.
        if (LooksLikeQrEnvelope(trimmed))
        {
            try
            {
                var parsed = ExItsQrEnvelope.Parse(trimmed);
                if (parsed.Purpose == ExItsQrPurpose.Organization)
                {
                    return ApplicationResult<OwnershipTransferTargetDto>.Failure(
                        DomainErrorCodes.OwnershipTransferQrPurposeRejected,
                        "This is a Business QR. To transfer ownership, scan the new owner's Personal QR.");
                }

                if (parsed.Purpose == ExItsQrPurpose.PosDeviceRegistration)
                {
                    return ApplicationResult<OwnershipTransferTargetDto>.Failure(
                        DomainErrorCodes.OwnershipTransferQrPurposeRejected,
                        "This code is for registering a POS device.");
                }
            }
            catch (DomainException)
            {
                // Fall through to PublicUserIdRules for personal EX id / personal QR.
            }
        }

        string publicUserId;
        try
        {
            publicUserId = PublicUserIdRules.Normalize(trimmed);
        }
        catch (DomainException)
        {
            return ApplicationResult<OwnershipTransferTargetDto>.Failure(
                DomainErrorCodes.OwnershipTransferTargetInvalid,
                "Enter a Personal ExItS ID (EX-####-####) or scan a Personal QR.");
        }

        var user = await _users.GetByPublicUserIdAsync(publicUserId, cancellationToken).ConfigureAwait(false);
        if (user is null || user.Status != AccountStatus.Active)
        {
            return ApplicationResult<OwnershipTransferTargetDto>.Failure(
                ApplicationErrorCodes.UserNotFound,
                "No active Personal account matched that ExItS ID.");
        }

        if (user.IsOrganizationScopedStaff)
        {
            return ApplicationResult<OwnershipTransferTargetDto>.Failure(
                DomainErrorCodes.OwnershipTransferTargetInvalid,
                "Ownership can only transfer to a Personal account. Organization staff logins cannot become Owner.");
        }

        return ApplicationResult<OwnershipTransferTargetDto>.Success(
            new OwnershipTransferTargetDto(user.PublicUserId!, user.DisplayName));
    }

    private static bool LooksLikeQrEnvelope(string value) =>
        value.StartsWith("exits://", StringComparison.OrdinalIgnoreCase);
}

public sealed class RequestOwnershipTransfer
{
    private readonly IPlatformOrganizationRepository _organizations;
    private readonly IPlatformUserRepository _users;
    private readonly IOrganizationMembershipRepository _memberships;
    private readonly IOrganizationOwnershipTransferRepository _transfers;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IAuditWriter _audit;
    private readonly ResolveOwnershipTransferTarget _resolveTarget;

    public RequestOwnershipTransfer(
        IPlatformOrganizationRepository organizations,
        IPlatformUserRepository users,
        IOrganizationMembershipRepository memberships,
        IOrganizationOwnershipTransferRepository transfers,
        IPlatformUnitOfWork unitOfWork,
        IClock clock,
        IAuditWriter audit,
        ResolveOwnershipTransferTarget resolveTarget)
    {
        _organizations = organizations;
        _users = users;
        _memberships = memberships;
        _transfers = transfers;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _audit = audit;
        _resolveTarget = resolveTarget;
    }

    public async Task<ApplicationResult<OrganizationOwnershipTransferDto>> ExecuteAsync(
        PlatformOrganizationId organizationId,
        PlatformUserId actorUserId,
        string targetInput,
        CancellationToken cancellationToken = default)
    {
        var organization = await _organizations.GetByIdAsync(organizationId, cancellationToken).ConfigureAwait(false);
        if (organization is null)
        {
            return ApplicationResult<OrganizationOwnershipTransferDto>.Failure(
                ApplicationErrorCodes.OrganizationNotFound,
                "Organization was not found.");
        }

        if (organization.Status != OrganizationStatus.Active)
        {
            return ApplicationResult<OrganizationOwnershipTransferDto>.Failure(
                DomainErrorCodes.OrganizationNotActive,
                "Ownership can only be transferred for an active organization.");
        }

        var actorMembership = await _memberships
            .FindActiveByUserAndOrganizationAsync(actorUserId, organizationId, cancellationToken)
            .ConfigureAwait(false);
        if (actorMembership is null || actorMembership.Role != OrganizationRole.OrganizationOwner)
        {
            return ApplicationResult<OrganizationOwnershipTransferDto>.Failure(
                DomainErrorCodes.AuthorizationDenied,
                "Only the current Organization Owner can request an ownership transfer.");
        }

        var resolved = await _resolveTarget.ExecuteAsync(targetInput, cancellationToken).ConfigureAwait(false);
        if (!resolved.IsSuccess)
        {
            return ApplicationResult<OrganizationOwnershipTransferDto>.Failure(
                resolved.ErrorCode!,
                resolved.ErrorMessage!);
        }

        var target = await _users
            .GetByPublicUserIdAsync(resolved.Value!.PublicUserId, cancellationToken)
            .ConfigureAwait(false);
        if (target is null || target.Status != AccountStatus.Active || target.IsOrganizationScopedStaff)
        {
            return ApplicationResult<OrganizationOwnershipTransferDto>.Failure(
                DomainErrorCodes.OwnershipTransferTargetInvalid,
                "Ownership can only transfer to an active Personal account.");
        }

        if (target.Id == actorUserId)
        {
            return ApplicationResult<OrganizationOwnershipTransferDto>.Failure(
                DomainErrorCodes.OwnershipTransferSelfDenied,
                "You already own this business.");
        }

        var pending = await _transfers
            .FindPendingByOrganizationAsync(organizationId, cancellationToken)
            .ConfigureAwait(false);
        if (pending is not null)
        {
            if (pending.IsExpired(_clock.UtcNow))
            {
                try
                {
                    pending.MarkExpired(_clock.UtcNow);
                    await _transfers.UpdateAsync(pending, cancellationToken).ConfigureAwait(false);
                    await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (DomainException)
                {
                    // Ignore race; unique index still protects.
                }
            }
            else
            {
                return ApplicationResult<OrganizationOwnershipTransferDto>.Failure(
                    DomainErrorCodes.OwnershipTransferPendingConflict,
                    "This organization already has a pending ownership transfer. Cancel it first.");
            }
        }

        try
        {
            var transfer = OrganizationOwnershipTransfer.Create(
                organizationId,
                actorUserId,
                target.Id,
                _clock.UtcNow);
            await _transfers.AddAsync(transfer, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            await _audit.WriteAsync(
                $"platform-user:{actorUserId.Value:D}",
                AuditActorType.PlatformUser,
                PlatformAuditActions.OwnershipTransferRequested,
                nameof(OrganizationOwnershipTransfer),
                transfer.Id.Value.ToString("D"),
                AuditOutcome.Succeeded,
                organizationId: organizationId,
                summary: $"Ownership transfer requested to {resolved.Value.PublicUserId}.",
                cancellationToken: cancellationToken).ConfigureAwait(false);

            return ApplicationResult<OrganizationOwnershipTransferDto>.Success(
                await MapAsync(transfer, organization, target, cancellationToken).ConfigureAwait(false));
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<OrganizationOwnershipTransferDto>.Failure(
                DomainErrorCodes.OwnershipTransferPendingConflict,
                ex.Message.Contains("ownership", StringComparison.OrdinalIgnoreCase)
                    ? "This organization already has a pending ownership transfer. Cancel it first."
                    : ex.Message);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<OrganizationOwnershipTransferDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }

    private async Task<OrganizationOwnershipTransferDto> MapAsync(
        OrganizationOwnershipTransfer transfer,
        PlatformOrganization organization,
        PlatformUser? toUser,
        CancellationToken cancellationToken)
    {
        toUser ??= await _users.GetByIdAsync(transfer.ToUserId, cancellationToken).ConfigureAwait(false);
        return OwnershipTransferMapping.ToDto(transfer, organization, toUser);
    }
}

public sealed class CancelOwnershipTransfer
{
    private readonly IOrganizationOwnershipTransferRepository _transfers;
    private readonly IOrganizationMembershipRepository _memberships;
    private readonly IPlatformOrganizationRepository _organizations;
    private readonly IPlatformUserRepository _users;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IAuditWriter _audit;

    public CancelOwnershipTransfer(
        IOrganizationOwnershipTransferRepository transfers,
        IOrganizationMembershipRepository memberships,
        IPlatformOrganizationRepository organizations,
        IPlatformUserRepository users,
        IPlatformUnitOfWork unitOfWork,
        IClock clock,
        IAuditWriter audit)
    {
        _transfers = transfers;
        _memberships = memberships;
        _organizations = organizations;
        _users = users;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _audit = audit;
    }

    public async Task<ApplicationResult<OrganizationOwnershipTransferDto>> ExecuteAsync(
        OrganizationOwnershipTransferId transferId,
        PlatformUserId actorUserId,
        CancellationToken cancellationToken = default)
    {
        var transfer = await _transfers.GetByIdAsync(transferId, cancellationToken).ConfigureAwait(false);
        if (transfer is null)
        {
            return ApplicationResult<OrganizationOwnershipTransferDto>.Failure(
                ApplicationErrorCodes.OwnershipTransferNotFound,
                "Ownership transfer was not found.");
        }

        var actorMembership = await _memberships
            .FindActiveByUserAndOrganizationAsync(actorUserId, transfer.OrganizationId, cancellationToken)
            .ConfigureAwait(false);
        if (actorMembership is null
            || actorMembership.Role != OrganizationRole.OrganizationOwner
            || actorUserId != transfer.FromOwnerUserId)
        {
            return ApplicationResult<OrganizationOwnershipTransferDto>.Failure(
                DomainErrorCodes.AuthorizationDenied,
                "Only the current Organization Owner who requested the transfer can cancel it.");
        }

        try
        {
            transfer.Cancel(actorUserId, _clock.UtcNow);
            await _transfers.UpdateAsync(transfer, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            await _audit.WriteAsync(
                $"platform-user:{actorUserId.Value:D}",
                AuditActorType.PlatformUser,
                PlatformAuditActions.OwnershipTransferCancelled,
                nameof(OrganizationOwnershipTransfer),
                transfer.Id.Value.ToString("D"),
                AuditOutcome.Succeeded,
                organizationId: transfer.OrganizationId,
                summary: "Ownership transfer cancelled.",
                cancellationToken: cancellationToken).ConfigureAwait(false);

            return ApplicationResult<OrganizationOwnershipTransferDto>.Success(
                await OwnershipTransferMapping.MapAsync(transfer, _organizations, _users, cancellationToken)
                    .ConfigureAwait(false));
        }
        catch (DomainException ex)
        {
            return ApplicationResult<OrganizationOwnershipTransferDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class DeclineOwnershipTransfer
{
    private readonly IOrganizationOwnershipTransferRepository _transfers;
    private readonly IPlatformOrganizationRepository _organizations;
    private readonly IPlatformUserRepository _users;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IAuditWriter _audit;

    public DeclineOwnershipTransfer(
        IOrganizationOwnershipTransferRepository transfers,
        IPlatformOrganizationRepository organizations,
        IPlatformUserRepository users,
        IPlatformUnitOfWork unitOfWork,
        IClock clock,
        IAuditWriter audit)
    {
        _transfers = transfers;
        _organizations = organizations;
        _users = users;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _audit = audit;
    }

    public async Task<ApplicationResult<OrganizationOwnershipTransferDto>> ExecuteAsync(
        OrganizationOwnershipTransferId transferId,
        PlatformUserId actorUserId,
        CancellationToken cancellationToken = default)
    {
        var transfer = await _transfers.GetByIdAsync(transferId, cancellationToken).ConfigureAwait(false);
        if (transfer is null)
        {
            return ApplicationResult<OrganizationOwnershipTransferDto>.Failure(
                ApplicationErrorCodes.OwnershipTransferNotFound,
                "Ownership transfer was not found.");
        }

        try
        {
            transfer.Decline(actorUserId, _clock.UtcNow);
            await _transfers.UpdateAsync(transfer, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            await _audit.WriteAsync(
                $"platform-user:{actorUserId.Value:D}",
                AuditActorType.PlatformUser,
                PlatformAuditActions.OwnershipTransferDeclined,
                nameof(OrganizationOwnershipTransfer),
                transfer.Id.Value.ToString("D"),
                AuditOutcome.Succeeded,
                organizationId: transfer.OrganizationId,
                summary: "Ownership transfer declined.",
                cancellationToken: cancellationToken).ConfigureAwait(false);

            return ApplicationResult<OrganizationOwnershipTransferDto>.Success(
                await OwnershipTransferMapping.MapAsync(transfer, _organizations, _users, cancellationToken)
                    .ConfigureAwait(false));
        }
        catch (DomainException ex)
        {
            return ApplicationResult<OrganizationOwnershipTransferDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class AcceptOwnershipTransfer
{
    private readonly IOrganizationOwnershipTransferRepository _transfers;
    private readonly IOrganizationMembershipRepository _memberships;
    private readonly IPlatformOrganizationRepository _organizations;
    private readonly IPlatformUserRepository _users;
    private readonly IProductAccessAssignmentRepository _assignments;
    private readonly IPlatformAuthSessionRepository _sessions;
    private readonly IPlatformAccessTokenRepository _accessTokens;
    private readonly EnsureAccountProfilesForUser _ensureProfiles;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IAuditWriter _audit;

    public AcceptOwnershipTransfer(
        IOrganizationOwnershipTransferRepository transfers,
        IOrganizationMembershipRepository memberships,
        IPlatformOrganizationRepository organizations,
        IPlatformUserRepository users,
        IProductAccessAssignmentRepository assignments,
        IPlatformAuthSessionRepository sessions,
        IPlatformAccessTokenRepository accessTokens,
        EnsureAccountProfilesForUser ensureProfiles,
        IPlatformUnitOfWork unitOfWork,
        IClock clock,
        IAuditWriter audit)
    {
        _transfers = transfers;
        _memberships = memberships;
        _organizations = organizations;
        _users = users;
        _assignments = assignments;
        _sessions = sessions;
        _accessTokens = accessTokens;
        _ensureProfiles = ensureProfiles;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _audit = audit;
    }

    public async Task<ApplicationResult<OrganizationOwnershipTransferDto>> ExecuteAsync(
        OrganizationOwnershipTransferId transferId,
        PlatformUserId actorUserId,
        CancellationToken cancellationToken = default)
    {
        var transfer = await _transfers.GetByIdAsync(transferId, cancellationToken).ConfigureAwait(false);
        if (transfer is null)
        {
            return ApplicationResult<OrganizationOwnershipTransferDto>.Failure(
                ApplicationErrorCodes.OwnershipTransferNotFound,
                "Ownership transfer was not found.");
        }

        ApplicationResult<OrganizationOwnershipTransferDto>? outcome = null;

        try
        {
            await _unitOfWork
                .ExecuteWithOrganizationLockAsync(
                    transfer.OrganizationId.Value,
                    async ct =>
                    {
                        outcome = await ExecuteLockedAsync(transferId, actorUserId, ct).ConfigureAwait(false);
                    },
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<OrganizationOwnershipTransferDto>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<OrganizationOwnershipTransferDto>.Failure(ex.ErrorCode, ex.Message);
        }

        return outcome ?? ApplicationResult<OrganizationOwnershipTransferDto>.Failure(
            ApplicationErrorCodes.DomainViolation,
            "Ownership transfer could not be completed.");
    }

    private async Task<ApplicationResult<OrganizationOwnershipTransferDto>> ExecuteLockedAsync(
        OrganizationOwnershipTransferId transferId,
        PlatformUserId actorUserId,
        CancellationToken cancellationToken)
    {
        var transfer = await _transfers.GetByIdAsync(transferId, cancellationToken).ConfigureAwait(false);
        if (transfer is null)
        {
            return ApplicationResult<OrganizationOwnershipTransferDto>.Failure(
                ApplicationErrorCodes.OwnershipTransferNotFound,
                "Ownership transfer was not found.");
        }

        // Idempotent retry after successful accept by the same recipient.
        if (transfer.Status == OrganizationOwnershipTransferStatus.Accepted
            && transfer.ToUserId == actorUserId)
        {
            return ApplicationResult<OrganizationOwnershipTransferDto>.Success(
                await OwnershipTransferMapping.MapAsync(transfer, _organizations, _users, cancellationToken)
                    .ConfigureAwait(false));
        }

        var organization = await _organizations.GetByIdAsync(transfer.OrganizationId, cancellationToken)
            .ConfigureAwait(false);
        if (organization is null || organization.Status != OrganizationStatus.Active)
        {
            return ApplicationResult<OrganizationOwnershipTransferDto>.Failure(
                DomainErrorCodes.OrganizationNotActive,
                "Ownership can only be accepted for an active organization.");
        }

        var target = await _users.GetByIdAsync(transfer.ToUserId, cancellationToken).ConfigureAwait(false);
        if (target is null
            || target.Status != AccountStatus.Active
            || target.IsOrganizationScopedStaff
            || target.Id != actorUserId)
        {
            return ApplicationResult<OrganizationOwnershipTransferDto>.Failure(
                DomainErrorCodes.OwnershipTransferActorMismatch,
                "Only the Personal recipient can accept this ownership transfer.");
        }

        var fromOwnerMembership = await _memberships
            .FindActiveByUserAndOrganizationAsync(
                transfer.FromOwnerUserId,
                transfer.OrganizationId,
                cancellationToken)
            .ConfigureAwait(false);
        if (fromOwnerMembership is null
            || fromOwnerMembership.Role != OrganizationRole.OrganizationOwner)
        {
            return ApplicationResult<OrganizationOwnershipTransferDto>.Failure(
                DomainErrorCodes.OwnershipTransferOwnerInvariant,
                "The requesting owner is no longer the active Organization Owner.");
        }

        var ownerCount = await _memberships
            .CountActiveGoverningAdminsAsync(transfer.OrganizationId, cancellationToken)
            .ConfigureAwait(false);
        if (ownerCount != 1)
        {
            return ApplicationResult<OrganizationOwnershipTransferDto>.Failure(
                DomainErrorCodes.OwnershipTransferOwnerInvariant,
                "Organization Owner seat is in an unexpected state. Transfer cannot proceed.");
        }

        var utcNow = _clock.UtcNow;
        var actorRef = $"ownership-transfer:{transfer.Id.Value:D}";

        transfer.Accept(actorUserId, utcNow);

        // Promote or create the new Owner first (temporarily two owners in memory / same tx).
        var targetMembership = await _memberships
            .FindCurrentByUserAndOrganizationAsync(target.Id, transfer.OrganizationId, cancellationToken)
            .ConfigureAwait(false);
        if (targetMembership is not null)
        {
            if (targetMembership.Status == MembershipStatus.Suspended)
            {
                targetMembership.Reactivate(utcNow, actorRef, "Ownership transfer accept");
            }

            targetMembership.ChangeRole(OrganizationRole.OrganizationOwner, utcNow, actorRef);
            await _memberships.UpdateAsync(targetMembership, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            var created = OrganizationMembership.Create(
                transfer.OrganizationId,
                target.Id,
                OrganizationRole.OrganizationOwner,
                utcNow,
                actorReference: actorRef);
            await _memberships.AddAsync(created, cancellationToken).ConfigureAwait(false);
        }

        // Remove former owner (bypass last-owner guard — new owner already staged).
        fromOwnerMembership.Remove(utcNow, "Ownership transferred.", actorRef);
        await _memberships.UpdateAsync(fromOwnerMembership, cancellationToken).ConfigureAwait(false);

        var activeAssignments = await _assignments
            .ListActiveByMembershipAsync(fromOwnerMembership.Id, cancellationToken)
            .ConfigureAwait(false);
        foreach (var assignment in activeAssignments)
        {
            assignment.Revoke(actorRef, "Ownership transferred", utcNow);
            await _assignments.UpdateAsync(assignment, cancellationToken).ConfigureAwait(false);
        }

        await _sessions
            .ClearSelectedOrganizationAsync(
                transfer.FromOwnerUserId,
                transfer.OrganizationId,
                cancellationToken)
            .ConfigureAwait(false);
        await _accessTokens
            .ClearOrganizationBindingAsync(
                transfer.FromOwnerUserId,
                transfer.OrganizationId,
                cancellationToken)
            .ConfigureAwait(false);

        await _transfers.UpdateAsync(transfer, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // Post-condition: exactly one Owner (still inside org lock transaction — throw to roll back).
        var postOwners = await _memberships
            .CountActiveGoverningAdminsAsync(transfer.OrganizationId, cancellationToken)
            .ConfigureAwait(false);
        if (postOwners != 1)
        {
            throw new DomainException(
                DomainErrorCodes.OwnershipTransferOwnerInvariant,
                "Ownership transfer failed owner invariant (exactly one Owner required).");
        }

        var soleOwner = await _memberships
            .FindActiveByUserAndOrganizationAsync(target.Id, transfer.OrganizationId, cancellationToken)
            .ConfigureAwait(false);
        if (soleOwner is null
            || soleOwner.Role != OrganizationRole.OrganizationOwner
            || soleOwner.Status != MembershipStatus.Active)
        {
            throw new DomainException(
                DomainErrorCodes.OwnershipTransferOwnerInvariant,
                "Ownership transfer failed: recipient is not the sole Owner.");
        }

        await _ensureProfiles
            .ExecuteAsync(
                target.Id,
                AccountClass.Organization,
                exclusivePreferredClass: false,
                cancellationToken)
            .ConfigureAwait(false);

        await _audit.WriteAsync(
            $"platform-user:{actorUserId.Value:D}",
            AuditActorType.PlatformUser,
            PlatformAuditActions.OwnershipTransferAccepted,
            nameof(OrganizationOwnershipTransfer),
            transfer.Id.Value.ToString("D"),
            AuditOutcome.Succeeded,
            organizationId: transfer.OrganizationId,
            summary: "Ownership transfer accepted.",
            cancellationToken: cancellationToken).ConfigureAwait(false);

        await _audit.WriteAsync(
            $"platform-user:{actorUserId.Value:D}",
            AuditActorType.PlatformUser,
            PlatformAuditActions.OrganizationOwnerChanged,
            nameof(PlatformOrganization),
            transfer.OrganizationId.Value.ToString("D"),
            AuditOutcome.Succeeded,
            organizationId: transfer.OrganizationId,
            summary:
            $"Owner changed from {transfer.FromOwnerUserId.Value:D} to {transfer.ToUserId.Value:D}.",
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return ApplicationResult<OrganizationOwnershipTransferDto>.Success(
            OwnershipTransferMapping.ToDto(transfer, organization, target));
    }
}

public sealed class GetPendingOwnershipTransferForOrg
{
    private readonly IOrganizationOwnershipTransferRepository _transfers;
    private readonly IPlatformOrganizationRepository _organizations;
    private readonly IPlatformUserRepository _users;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public GetPendingOwnershipTransferForOrg(
        IOrganizationOwnershipTransferRepository transfers,
        IPlatformOrganizationRepository organizations,
        IPlatformUserRepository users,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _transfers = transfers;
        _organizations = organizations;
        _users = users;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<OrganizationOwnershipTransferDto?>> ExecuteAsync(
        PlatformOrganizationId organizationId,
        CancellationToken cancellationToken = default)
    {
        var transfer = await _transfers
            .FindPendingByOrganizationAsync(organizationId, cancellationToken)
            .ConfigureAwait(false);
        if (transfer is null)
        {
            return ApplicationResult<OrganizationOwnershipTransferDto?>.Success(null);
        }

        if (transfer.IsExpired(_clock.UtcNow))
        {
            try
            {
                transfer.MarkExpired(_clock.UtcNow);
                await _transfers.UpdateAsync(transfer, cancellationToken).ConfigureAwait(false);
                await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (DomainException)
            {
                // Already transitioned.
            }

            return ApplicationResult<OrganizationOwnershipTransferDto?>.Success(null);
        }

        return ApplicationResult<OrganizationOwnershipTransferDto?>.Success(
            await OwnershipTransferMapping.MapAsync(transfer, _organizations, _users, cancellationToken)
                .ConfigureAwait(false));
    }
}

public sealed class ListPendingOwnershipTransfersForRecipient
{
    private readonly IOrganizationOwnershipTransferRepository _transfers;
    private readonly IPlatformOrganizationRepository _organizations;
    private readonly IPlatformUserRepository _users;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public ListPendingOwnershipTransfersForRecipient(
        IOrganizationOwnershipTransferRepository transfers,
        IPlatformOrganizationRepository organizations,
        IPlatformUserRepository users,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _transfers = transfers;
        _organizations = organizations;
        _users = users;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<IReadOnlyList<OrganizationOwnershipTransferDto>>> ExecuteAsync(
        PlatformUserId recipientUserId,
        CancellationToken cancellationToken = default)
    {
        var pending = await _transfers
            .ListPendingByRecipientAsync(recipientUserId, cancellationToken)
            .ConfigureAwait(false);
        var result = new List<OrganizationOwnershipTransferDto>();
        foreach (var transfer in pending)
        {
            if (transfer.IsExpired(_clock.UtcNow))
            {
                try
                {
                    transfer.MarkExpired(_clock.UtcNow);
                    await _transfers.UpdateAsync(transfer, cancellationToken).ConfigureAwait(false);
                }
                catch (DomainException)
                {
                    // Already transitioned.
                }

                continue;
            }

            result.Add(
                await OwnershipTransferMapping.MapAsync(transfer, _organizations, _users, cancellationToken)
                    .ConfigureAwait(false));
        }

        if (pending.Any(t => t.Status == OrganizationOwnershipTransferStatus.Expired))
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return ApplicationResult<IReadOnlyList<OrganizationOwnershipTransferDto>>.Success(result);
    }
}

internal static class OwnershipTransferMapping
{
    public static OrganizationOwnershipTransferDto ToDto(
        OrganizationOwnershipTransfer transfer,
        PlatformOrganization? organization,
        PlatformUser? toUser) =>
        new(
            transfer.Id.Value,
            transfer.OrganizationId.Value,
            organization?.DisplayName,
            organization?.PublicOrganizationId,
            transfer.FromOwnerUserId.Value,
            transfer.ToUserId.Value,
            toUser?.DisplayName,
            toUser?.PublicUserId,
            transfer.Status.ToString(),
            transfer.CreatedAtUtc,
            transfer.ExpiresAtUtc,
            transfer.AcceptedAtUtc,
            transfer.DeclinedAtUtc,
            transfer.CancelledAtUtc,
            transfer.CompletedAtUtc,
            transfer.UpdatedAtUtc);

    public static async Task<OrganizationOwnershipTransferDto> MapAsync(
        OrganizationOwnershipTransfer transfer,
        IPlatformOrganizationRepository organizations,
        IPlatformUserRepository users,
        CancellationToken cancellationToken)
    {
        var organization = await organizations.GetByIdAsync(transfer.OrganizationId, cancellationToken)
            .ConfigureAwait(false);
        var toUser = await users.GetByIdAsync(transfer.ToUserId, cancellationToken).ConfigureAwait(false);
        return ToDto(transfer, organization, toUser);
    }
}
