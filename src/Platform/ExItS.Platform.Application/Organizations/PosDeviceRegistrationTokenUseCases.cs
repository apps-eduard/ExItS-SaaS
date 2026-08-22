using ExItS.Platform.Application.Audit;
using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Identity;
using ExItS.Platform.Application.Subscriptions;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Audit;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.Application.Organizations;

public sealed record PosDeviceRegistrationTokenDto(
    Guid Id,
    Guid OrganizationId,
    string Token,
    string QrPayload,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset ExpiresAtUtc,
    string Status,
    int ExpiresInMinutes);

public sealed record PosDeviceRegistrationTokenMetadataDto(
    Guid Id,
    Guid OrganizationId,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset ExpiresAtUtc,
    string Status,
    int ExpiresInMinutes);

public sealed record RedeemPosDeviceRegistrationTokenCommand(
    string Token,
    Guid BranchId,
    string InstallationDeviceId,
    string FriendlyName,
    string? Platform = null,
    string? Model = null,
    string? AppVersion = null);

/// <summary>
/// Creates an opaque org-scoped device registration token (QR) for another device to redeem.
/// Same manage-device authorization as <see cref="RegisterCurrentDevice"/>.
/// </summary>
public sealed class CreatePosDeviceRegistrationToken(
    IPosDeviceRegistrationTokenRepository tokens,
    IPosDeviceRepository devices,
    ISubscriptionRepository subscriptions,
    IPlanRepository plans,
    IPlatformSessionTokenService tokenService,
    IPlatformUnitOfWork unitOfWork,
    IClock clock,
    IAuditWriter audit)
{
    public async Task<ApplicationResult<PosDeviceRegistrationTokenDto>> ExecuteAsync(
        PlatformOrganizationId organizationId,
        PlatformUserId createdByUserId,
        CancellationToken cancellationToken = default)
    {
        var limit = await PosOrganizationPlanLimits
            .ResolveAsync(organizationId, subscriptions, plans, cancellationToken)
            .ConfigureAwait(false);
        if (!limit.IsSuccess || limit.Value is null)
        {
            return ApplicationResult<PosDeviceRegistrationTokenDto>.Failure(limit.ErrorCode!, limit.ErrorMessage!);
        }

        if (await devices.CountActiveAsync(organizationId, cancellationToken).ConfigureAwait(false)
            >= limit.Value.MaxActivePosDevices)
        {
            return ApplicationResult<PosDeviceRegistrationTokenDto>.Failure(
                ApplicationErrorCodes.PosDeviceCapacityExceeded,
                "The active POS plan device limit has been reached.");
        }

        var opaque = tokenService.CreateOpaqueToken();
        PosDeviceRegistrationToken entity;
        try
        {
            entity = PosDeviceRegistrationToken.Create(organizationId, createdByUserId, opaque, clock.UtcNow);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<PosDeviceRegistrationTokenDto>.Failure(ex.ErrorCode, ex.Message);
        }

        await tokens.AddAsync(entity, cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await audit.WriteAsync(
            $"platform-user:{createdByUserId.Value:D}",
            AuditActorType.PlatformUser,
            PlatformAuditActions.PosDeviceRegistrationTokenCreated,
            nameof(PosDeviceRegistrationToken),
            entity.Id.Value.ToString("D"),
            AuditOutcome.Succeeded,
            organizationId: organizationId,
            summary: "POS device registration token created.",
            cancellationToken: cancellationToken).ConfigureAwait(false);

        var remaining = entity.ExpiresAtUtc - clock.UtcNow;
        var minutes = Math.Max(0, (int)Math.Ceiling(remaining.TotalMinutes));
        return ApplicationResult<PosDeviceRegistrationTokenDto>.Success(new PosDeviceRegistrationTokenDto(
            entity.Id.Value,
            organizationId.Value,
            opaque,
            ExItsQrEnvelope.Build(ExItsQrPurpose.PosDeviceRegistration, opaque),
            entity.CreatedAtUtc,
            entity.ExpiresAtUtc,
            entity.Status.ToString(),
            minutes));
    }
}

/// <summary>
/// Redeems a registration token into a PosDevice. Requires an authenticated Platform user who is
/// an active member of the token's organization (route org must match token org).
/// </summary>
public sealed class RedeemPosDeviceRegistrationToken(
    IPosDeviceRegistrationTokenRepository tokens,
    IPosDeviceRepository devices,
    IOrganizationBranchRepository branches,
    IOrganizationMembershipRepository memberships,
    IOrganizationBranchAccessService branchAccess,
    ISubscriptionRepository subscriptions,
    IPlanRepository plans,
    IPlatformUnitOfWork unitOfWork,
    IClock clock,
    IAuditWriter audit)
{
    public async Task<ApplicationResult<PosDeviceDto>> ExecuteAsync(
        PlatformOrganizationId organizationId,
        PlatformUserId actorUserId,
        RedeemPosDeviceRegistrationTokenCommand command,
        CancellationToken cancellationToken = default)
    {
        var membership = await memberships
            .FindActiveByUserAndOrganizationAsync(actorUserId, organizationId, cancellationToken)
            .ConfigureAwait(false);
        if (membership is null)
        {
            return ApplicationResult<PosDeviceDto>.Failure(
                ApplicationErrorCodes.MembershipNotFound,
                "You must be an active member of this organization to redeem a device registration token.");
        }

        string opaque;
        try
        {
            // Accept raw token or full QR envelope.
            if (ExItsQrEnvelope.TryParse(command.Token, out var parsed) && parsed is not null)
            {
                if (parsed.Purpose != ExItsQrPurpose.PosDeviceRegistration)
                {
                    return ApplicationResult<PosDeviceDto>.Failure(
                        ApplicationErrorCodes.QrPurposeMismatch,
                        "qr_purpose_mismatch");
                }

                opaque = parsed.Subject;
            }
            else
            {
                opaque = ExItsQrEnvelope.NormalizeOpaqueToken(command.Token);
            }
        }
        catch (DomainException ex)
        {
            return ApplicationResult<PosDeviceDto>.Failure(
                ApplicationErrorCodes.PosDeviceRegistrationTokenNotFound,
                ex.Message);
        }

        var hash = PosDeviceRegistrationToken.HashToken(opaque);
        var token = await tokens.GetByTokenHashAsync(hash, cancellationToken).ConfigureAwait(false);
        if (token is null)
        {
            return ApplicationResult<PosDeviceDto>.Failure(
                ApplicationErrorCodes.PosDeviceRegistrationTokenNotFound,
                "Registration token was not found.");
        }

        try
        {
            token.EnsureRedeemable(clock.UtcNow, organizationId);
        }
        catch (DomainException ex)
        {
            var code = ex.ErrorCode switch
            {
                DomainErrorCodes.PosDeviceRegistrationTokenExpired =>
                    ApplicationErrorCodes.PosDeviceRegistrationTokenExpired,
                DomainErrorCodes.PosDeviceRegistrationTokenAlreadyRedeemed =>
                    ApplicationErrorCodes.PosDeviceRegistrationTokenAlreadyRedeemed,
                DomainErrorCodes.PosDeviceRegistrationTokenOrganizationMismatch =>
                    ApplicationErrorCodes.PosDeviceRegistrationTokenOrganizationMismatch,
                _ => ex.ErrorCode
            };
            return ApplicationResult<PosDeviceDto>.Failure(code, ex.Message);
        }

        var branch = await branches
            .GetByIdAsync(OrganizationBranchId.From(command.BranchId), cancellationToken)
            .ConfigureAwait(false);
        if (branch is null || branch.OrganizationId != organizationId)
        {
            return ApplicationResult<PosDeviceDto>.Failure(
                ApplicationErrorCodes.BranchNotFound,
                "The selected branch was not found.");
        }

        try
        {
            branch.EnsureActive();
        }
        catch (DomainException ex)
        {
            return ApplicationResult<PosDeviceDto>.Failure(ex.ErrorCode, ex.Message);
        }

        // Staff must only redeem into an Active branch they are authorized to access.
        if (!await branchAccess
                .CanAccessBranchAsync(actorUserId, organizationId, branch.Id, cancellationToken)
                .ConfigureAwait(false))
        {
            return ApplicationResult<PosDeviceDto>.Failure(
                ApplicationErrorCodes.BranchAccessDenied,
                "You are not authorized to register a POS device for the selected branch.");
        }

        ApplicationResult<PosDeviceDto>? outcome = null;
        try
        {
            await unitOfWork.ExecuteWithOrganizationLockAsync(
                organizationId.Value,
                async ct =>
                {
                    outcome = await RedeemLockedAsync(
                        organizationId,
                        actorUserId,
                        branch,
                        token,
                        command,
                        ct).ConfigureAwait(false);
                },
                cancellationToken).ConfigureAwait(false);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<PosDeviceDto>.Failure(ex.ErrorCode, ex.Message);
        }

        return outcome ?? ApplicationResult<PosDeviceDto>.Failure(
            ApplicationErrorCodes.PosDeviceNotAuthorized,
            "POS device registration did not complete.");
    }

    private async Task<ApplicationResult<PosDeviceDto>> RedeemLockedAsync(
        PlatformOrganizationId organizationId,
        PlatformUserId actorUserId,
        OrganizationBranch branch,
        PosDeviceRegistrationToken token,
        RedeemPosDeviceRegistrationTokenCommand command,
        CancellationToken cancellationToken)
    {
        PosDevice? existing;
        try
        {
            existing = await devices
                .GetByInstallationDeviceIdAsync(organizationId, command.InstallationDeviceId, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<PosDeviceDto>.Failure(ex.ErrorCode, ex.Message);
        }

        if (existing is not null && existing.Status == PosDeviceStatus.Active)
        {
            if (existing.BranchId != branch.Id)
            {
                // Do not consume the one-time token when the installation cannot move.
                return ApplicationResult<PosDeviceDto>.Failure(
                    ApplicationErrorCodes.PosDeviceBranchConflict,
                    "This POS installation is already registered to another branch. It cannot be moved silently.");
            }

            try
            {
                existing.TouchLastSeen(clock.UtcNow);
                token.Redeem(existing.Id, command.InstallationDeviceId, clock.UtcNow, organizationId);
            }
            catch (DomainException ex)
            {
                return ApplicationResult<PosDeviceDto>.Failure(ex.ErrorCode, ex.Message);
            }

            await devices.UpdateAsync(existing, cancellationToken).ConfigureAwait(false);
            await tokens.UpdateAsync(token, cancellationToken).ConfigureAwait(false);
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<PosDeviceDto>.Success(DeviceMapper.ToDto(existing));
        }

        var limit = await PosOrganizationPlanLimits
            .ResolveAsync(organizationId, subscriptions, plans, cancellationToken)
            .ConfigureAwait(false);
        if (!limit.IsSuccess || limit.Value is null)
        {
            return ApplicationResult<PosDeviceDto>.Failure(limit.ErrorCode!, limit.ErrorMessage!);
        }

        if (await devices.CountActiveAsync(organizationId, cancellationToken).ConfigureAwait(false)
            >= limit.Value.MaxActivePosDevices)
        {
            return ApplicationResult<PosDeviceDto>.Failure(
                ApplicationErrorCodes.PosDeviceCapacityExceeded,
                "The active POS plan device limit has been reached.");
        }

        PosDevice device;
        try
        {
            if (existing is not null)
            {
                if (existing.BranchId != branch.Id)
                {
                    return ApplicationResult<PosDeviceDto>.Failure(
                        ApplicationErrorCodes.PosDeviceBranchConflict,
                        "This POS installation is already registered to another branch. It cannot be moved silently.");
                }

                existing.Reactivate(clock.UtcNow);
                device = existing;
                await devices.UpdateAsync(device, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                device = PosDevice.Register(
                    organizationId,
                    branch.Id,
                    command.InstallationDeviceId,
                    command.FriendlyName,
                    clock.UtcNow,
                    command.Platform,
                    command.Model,
                    command.AppVersion);
                await devices.AddAsync(device, cancellationToken).ConfigureAwait(false);
            }

            token.Redeem(device.Id, command.InstallationDeviceId, clock.UtcNow, organizationId);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<PosDeviceDto>.Failure(ex.ErrorCode, ex.Message);
        }

        await tokens.UpdateAsync(token, cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await audit.WriteAsync(
            $"platform-user:{actorUserId.Value:D}",
            AuditActorType.PlatformUser,
            PlatformAuditActions.PosDeviceRegistrationTokenRedeemed,
            nameof(PosDeviceRegistrationToken),
            token.Id.Value.ToString("D"),
            AuditOutcome.Succeeded,
            organizationId: organizationId,
            summary: $"posDeviceId={device.Id.Value:D}",
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return ApplicationResult<PosDeviceDto>.Success(DeviceMapper.ToDto(device));
    }
}

public sealed class GetPosDeviceRegistrationTokenMetadata(
    IPosDeviceRegistrationTokenRepository tokens,
    IClock clock)
{
    public async Task<ApplicationResult<PosDeviceRegistrationTokenMetadataDto>> ExecuteAsync(
        PlatformOrganizationId organizationId,
        PosDeviceRegistrationTokenId tokenId,
        CancellationToken cancellationToken = default)
    {
        var token = await tokens.GetByIdAsync(tokenId, cancellationToken).ConfigureAwait(false);
        if (token is null || token.OrganizationId != organizationId)
        {
            return ApplicationResult<PosDeviceRegistrationTokenMetadataDto>.Failure(
                ApplicationErrorCodes.PosDeviceRegistrationTokenNotFound,
                "Registration token was not found.");
        }

        token.RefreshExpired(clock.UtcNow);
        var remaining = token.ExpiresAtUtc - clock.UtcNow;
        var minutes = Math.Max(0, (int)Math.Ceiling(remaining.TotalMinutes));
        return ApplicationResult<PosDeviceRegistrationTokenMetadataDto>.Success(
            new PosDeviceRegistrationTokenMetadataDto(
                token.Id.Value,
                token.OrganizationId.Value,
                token.CreatedAtUtc,
                token.ExpiresAtUtc,
                token.Status.ToString(),
                minutes));
    }
}
