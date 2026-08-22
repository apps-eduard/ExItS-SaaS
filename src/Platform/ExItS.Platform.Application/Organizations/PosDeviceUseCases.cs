using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Subscriptions;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.Application.Organizations;

public sealed record PosDeviceDto(Guid Id, Guid OrganizationId, Guid BranchId, string InstallationDeviceId, string FriendlyName,
    string? Platform, string? Model, string? AppVersion, PosDeviceStatus Status, DateTimeOffset RegisteredAtUtc,
    DateTimeOffset LastSeenAtUtc, DateTimeOffset? RevokedAtUtc);
public sealed record PosDeviceCapacityDto(int Used, int Allowed);
public sealed record RegisterPosDeviceCommand(Guid BranchId, string InstallationDeviceId, string FriendlyName,
    string? Platform = null, string? Model = null, string? AppVersion = null);
public sealed record PosDeviceAuthorizationDto(Guid PosDeviceId, Guid BranchId, string InstallationDeviceId);

public sealed class RegisterCurrentDevice(
    IPosDeviceRepository devices, IOrganizationBranchRepository branches, ISubscriptionRepository subscriptions,
    IPlanRepository plans, IPlatformUnitOfWork unitOfWork, IClock clock)
{
    public async Task<ApplicationResult<PosDeviceDto>> ExecuteAsync(
        PlatformOrganizationId organizationId, RegisterPosDeviceCommand command, CancellationToken cancellationToken = default)
    {
        var branch = await branches.GetByIdAsync(OrganizationBranchId.From(command.BranchId), cancellationToken).ConfigureAwait(false);
        if (branch is null || branch.OrganizationId != organizationId)
            return ApplicationResult<PosDeviceDto>.Failure(ApplicationErrorCodes.BranchNotFound, "The selected branch was not found.");
        try { branch.EnsureActive(); } catch (DomainException ex) { return ApplicationResult<PosDeviceDto>.Failure(ex.ErrorCode, ex.Message); }

        ApplicationResult<PosDeviceDto>? outcome = null;
        try
        {
            // Capacity check + insert must be serialized per organization so two final-slot
            // registrations cannot both succeed (PostgreSQL advisory lock in PlatformUnitOfWork).
            await unitOfWork.ExecuteWithOrganizationLockAsync(
                organizationId.Value,
                async ct =>
                {
                    outcome = await ExecuteLockedAsync(organizationId, branch, command, ct).ConfigureAwait(false);
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

    private async Task<ApplicationResult<PosDeviceDto>> ExecuteLockedAsync(
        PlatformOrganizationId organizationId,
        OrganizationBranch branch,
        RegisterPosDeviceCommand command,
        CancellationToken cancellationToken)
    {
        PosDevice? existing;
        try { existing = await devices.GetByInstallationDeviceIdAsync(organizationId, command.InstallationDeviceId, cancellationToken).ConfigureAwait(false); }
        catch (DomainException ex) { return ApplicationResult<PosDeviceDto>.Failure(ex.ErrorCode, ex.Message); }

        if (existing is not null && existing.Status == PosDeviceStatus.Active)
        {
            if (existing.BranchId != branch.Id)
            {
                return ApplicationResult<PosDeviceDto>.Failure(
                    ApplicationErrorCodes.PosDeviceBranchConflict,
                    "This POS installation is already registered to another branch. It cannot be moved silently.");
            }

            try { existing.TouchLastSeen(clock.UtcNow); }
            catch (DomainException ex) { return ApplicationResult<PosDeviceDto>.Failure(ex.ErrorCode, ex.Message); }
            await devices.UpdateAsync(existing, cancellationToken).ConfigureAwait(false);
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<PosDeviceDto>.Success(DeviceMapper.ToDto(existing));
        }

        var limit = await PosOrganizationPlanLimits.ResolveAsync(organizationId, subscriptions, plans, cancellationToken).ConfigureAwait(false);
        if (!limit.IsSuccess || limit.Value is null)
            return ApplicationResult<PosDeviceDto>.Failure(limit.ErrorCode!, limit.ErrorMessage!);
        if (await devices.CountActiveAsync(organizationId, cancellationToken).ConfigureAwait(false) >= limit.Value.MaxActivePosDevices)
            return ApplicationResult<PosDeviceDto>.Failure(ApplicationErrorCodes.PosDeviceCapacityExceeded, "The active POS plan device limit has been reached.");

        PosDevice device;
        try
        {
            if (existing is not null)
            {
                // Revoked devices keep their original BranchId; refuse silent rebinding.
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
                device = PosDevice.Register(organizationId, branch.Id, command.InstallationDeviceId, command.FriendlyName, clock.UtcNow,
                    command.Platform, command.Model, command.AppVersion);
                await devices.AddAsync(device, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (DomainException ex) { return ApplicationResult<PosDeviceDto>.Failure(ex.ErrorCode, ex.Message); }

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return ApplicationResult<PosDeviceDto>.Success(DeviceMapper.ToDto(device));
    }
}

public sealed class ListDevices(IPosDeviceRepository devices)
{
    /// <summary>Customer Device Management — active POS devices only.</summary>
    public async Task<IReadOnlyList<PosDeviceDto>> ExecuteAsync(PlatformOrganizationId organizationId, CancellationToken cancellationToken = default) =>
        (await devices.ListActiveByOrganizationAsync(organizationId, cancellationToken).ConfigureAwait(false)).Select(DeviceMapper.ToDto).ToList();
}

/// <summary>Audit/support — all devices including revoked. Does not delete history.</summary>
public sealed class ListAllDevices(IPosDeviceRepository devices)
{
    public async Task<IReadOnlyList<PosDeviceDto>> ExecuteAsync(PlatformOrganizationId organizationId, CancellationToken cancellationToken = default) =>
        (await devices.ListByOrganizationAsync(organizationId, cancellationToken).ConfigureAwait(false)).Select(DeviceMapper.ToDto).ToList();
}

public sealed class RenameDevice(IPosDeviceRepository devices, IPlatformUnitOfWork unitOfWork, IClock clock)
{
    public async Task<ApplicationResult<PosDeviceDto>> ExecuteAsync(PlatformOrganizationId organizationId, PosDeviceId deviceId, string friendlyName, CancellationToken cancellationToken = default)
    {
        var device = await devices.GetByIdAsync(deviceId, cancellationToken).ConfigureAwait(false);
        if (device is null || device.OrganizationId != organizationId) return ApplicationResult<PosDeviceDto>.Failure(ApplicationErrorCodes.PosDeviceNotFound, "POS device was not found.");
        try { device.Rename(friendlyName, clock.UtcNow); } catch (DomainException ex) { return ApplicationResult<PosDeviceDto>.Failure(ex.ErrorCode, ex.Message); }
        await devices.UpdateAsync(device, cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return ApplicationResult<PosDeviceDto>.Success(DeviceMapper.ToDto(device));
    }
}

public sealed class RevokeDevice(IPosDeviceRepository devices, IPlatformUnitOfWork unitOfWork, IClock clock)
{
    public async Task<ApplicationResult<PosDeviceDto>> ExecuteAsync(PlatformOrganizationId organizationId, PosDeviceId deviceId, PlatformUserId revokedBy, CancellationToken cancellationToken = default)
    {
        var device = await devices.GetByIdAsync(deviceId, cancellationToken).ConfigureAwait(false);
        if (device is null || device.OrganizationId != organizationId) return ApplicationResult<PosDeviceDto>.Failure(ApplicationErrorCodes.PosDeviceNotFound, "POS device was not found.");
        device.Revoke(revokedBy, clock.UtcNow);
        await devices.UpdateAsync(device, cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return ApplicationResult<PosDeviceDto>.Success(DeviceMapper.ToDto(device));
    }
}

public sealed class GetDeviceCapacity(IPosDeviceRepository devices, ISubscriptionRepository subscriptions, IPlanRepository plans)
{
    public async Task<ApplicationResult<PosDeviceCapacityDto>> ExecuteAsync(PlatformOrganizationId organizationId, CancellationToken cancellationToken = default)
    {
        var limit = await PosOrganizationPlanLimits.ResolveAsync(organizationId, subscriptions, plans, cancellationToken).ConfigureAwait(false);
        if (!limit.IsSuccess || limit.Value is null) return ApplicationResult<PosDeviceCapacityDto>.Failure(limit.ErrorCode!, limit.ErrorMessage!);
        return ApplicationResult<PosDeviceCapacityDto>.Success(new(await devices.CountActiveAsync(organizationId, cancellationToken).ConfigureAwait(false), limit.Value.MaxActivePosDevices));
    }
}

public sealed class AuthorizeForTransactions(IPosDeviceRepository devices)
{
    public async Task<ApplicationResult<PosDeviceAuthorizationDto>> ExecuteAsync(
        PlatformOrganizationId organizationId, string installationDeviceId, OrganizationBranchId? expectedBranchId = null, CancellationToken cancellationToken = default)
    {
        PosDevice? device;
        try { device = await devices.GetByInstallationDeviceIdAsync(organizationId, installationDeviceId, cancellationToken).ConfigureAwait(false); }
        catch (DomainException ex) { return ApplicationResult<PosDeviceAuthorizationDto>.Failure(ApplicationErrorCodes.PosDeviceNotAuthorized, ex.Message); }
        if (device is null) return ApplicationResult<PosDeviceAuthorizationDto>.Failure(ApplicationErrorCodes.PosDeviceRegistrationRequired, "This POS installation is not registered for sales.");
        if (device.Status == PosDeviceStatus.Revoked) return ApplicationResult<PosDeviceAuthorizationDto>.Failure(ApplicationErrorCodes.PosDeviceRevoked, "This POS device has been revoked.");
        if (expectedBranchId is not null && device.BranchId != expectedBranchId)
            return ApplicationResult<PosDeviceAuthorizationDto>.Failure(ApplicationErrorCodes.PosDeviceNotAuthorized, "This device is not authorized for the selected branch.");
        return ApplicationResult<PosDeviceAuthorizationDto>.Success(new(device.Id.Value, device.BranchId.Value, device.InstallationDeviceId));
    }
}

internal static class DeviceMapper
{
    public static PosDeviceDto ToDto(PosDevice x) => new(x.Id.Value, x.OrganizationId.Value, x.BranchId.Value, x.InstallationDeviceId,
        x.FriendlyName, x.Platform, x.Model, x.AppVersion, x.Status, x.RegisteredAtUtc, x.LastSeenAtUtc, x.RevokedAtUtc);
}
