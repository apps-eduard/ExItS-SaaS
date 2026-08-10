using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;

namespace ExItS.Platform.Domain.Organizations;

public enum PosDeviceStatus
{
    Active,
    Revoked
}

public sealed class PosDevice
{
    public PosDeviceId Id { get; }
    public PlatformOrganizationId OrganizationId { get; }
    public OrganizationBranchId BranchId { get; }
    public string InstallationDeviceId { get; }
    public string FriendlyName { get; private set; }
    public string? Platform { get; private set; }
    public string? Model { get; private set; }
    public string? AppVersion { get; private set; }
    public PosDeviceStatus Status { get; private set; }
    public DateTimeOffset RegisteredAtUtc { get; }
    public DateTimeOffset LastSeenAtUtc { get; private set; }
    public DateTimeOffset? RevokedAtUtc { get; private set; }
    public PlatformUserId? RevokedByUserId { get; private set; }

    private PosDevice(PosDeviceId id, PlatformOrganizationId organizationId, OrganizationBranchId branchId, string installationDeviceId,
        string friendlyName, string? platform, string? model, string? appVersion, PosDeviceStatus status, DateTimeOffset registeredAtUtc,
        DateTimeOffset lastSeenAtUtc, DateTimeOffset? revokedAtUtc, PlatformUserId? revokedByUserId)
    {
        Id = id; OrganizationId = organizationId; BranchId = branchId; InstallationDeviceId = installationDeviceId; FriendlyName = friendlyName;
        Platform = platform; Model = model; AppVersion = appVersion; Status = status; RegisteredAtUtc = registeredAtUtc; LastSeenAtUtc = lastSeenAtUtc;
        RevokedAtUtc = revokedAtUtc; RevokedByUserId = revokedByUserId;
    }

    public static PosDevice Register(PlatformOrganizationId organizationId, OrganizationBranchId branchId, string installationDeviceId, string friendlyName,
        DateTimeOffset utcNow, string? platform = null, string? model = null, string? appVersion = null, PosDeviceId? id = null)
    {
        ArgumentNullException.ThrowIfNull(organizationId); ArgumentNullException.ThrowIfNull(branchId); DomainTime.EnsureUtc(utcNow);
        return new(id ?? PosDeviceId.New(), organizationId, branchId, NormalizeInstallationDeviceId(installationDeviceId), NormalizeRequired(friendlyName, 128),
            NormalizeOptional(platform, 64), NormalizeOptional(model, 128), NormalizeOptional(appVersion, 64), PosDeviceStatus.Active, utcNow, utcNow, null, null);
    }

    internal static PosDevice Rehydrate(PosDeviceId id, PlatformOrganizationId organizationId, OrganizationBranchId branchId, string installationDeviceId,
        string friendlyName, string? platform, string? model, string? appVersion, PosDeviceStatus status, DateTimeOffset registeredAtUtc,
        DateTimeOffset lastSeenAtUtc, DateTimeOffset? revokedAtUtc, PlatformUserId? revokedByUserId) =>
        new(id, organizationId, branchId, NormalizeInstallationDeviceId(installationDeviceId), NormalizeRequired(friendlyName, 128), NormalizeOptional(platform, 64),
            NormalizeOptional(model, 128), NormalizeOptional(appVersion, 64), status, registeredAtUtc, lastSeenAtUtc, revokedAtUtc, revokedByUserId);

    public void Rename(string friendlyName, DateTimeOffset utcNow) { EnsureActive(); DomainTime.EnsureUtc(utcNow); FriendlyName = NormalizeRequired(friendlyName, 128); }
    public void TouchLastSeen(DateTimeOffset utcNow) { EnsureActive(); DomainTime.EnsureUtc(utcNow); LastSeenAtUtc = utcNow; }
    public void Revoke(PlatformUserId userId, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(userId); DomainTime.EnsureUtc(utcNow);
        if (Status == PosDeviceStatus.Revoked) return;
        Status = PosDeviceStatus.Revoked; RevokedAtUtc = utcNow; RevokedByUserId = userId;
    }
    public void Reactivate(DateTimeOffset utcNow)
    {
        DomainTime.EnsureUtc(utcNow);
        if (Status == PosDeviceStatus.Active)
        {
            TouchLastSeen(utcNow);
            return;
        }

        Status = PosDeviceStatus.Active;
        RevokedAtUtc = null;
        RevokedByUserId = null;
        LastSeenAtUtc = utcNow;
    }
    public void EnsureActive() { if (Status != PosDeviceStatus.Active) throw new DomainException(DomainErrorCodes.PosDeviceNotActive, "POS device is not active."); }
    public static string NormalizeInstallationDeviceId(string value) =>
        string.IsNullOrWhiteSpace(value) ? throw new DomainException(DomainErrorCodes.InvalidPosDeviceInstallationId, "Installation device ID cannot be blank.") : value.Trim();
    private static string NormalizeRequired(string value, int maximum) => string.IsNullOrWhiteSpace(value) ? throw new DomainException(DomainErrorCodes.InvalidPosDeviceInstallationId, "Device name cannot be blank.") : value.Trim()[..Math.Min(value.Trim().Length, maximum)];
    private static string? NormalizeOptional(string? value, int maximum) => string.IsNullOrWhiteSpace(value) ? null : value.Trim()[..Math.Min(value.Trim().Length, maximum)];
}
