using ExItS.Platform.Domain.Identity;

namespace ExItS.Platform.Application.Identity;

public interface IPlatformDeviceRecoveryCredentialRepository
{
    Task<PlatformDeviceRecoveryCredential?> GetByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default);

    Task<PlatformDeviceRecoveryCredential?> GetActiveByUserAndDeviceAsync(
        PlatformUserId userId,
        string installationDeviceId,
        CancellationToken cancellationToken = default);

    Task AddAsync(PlatformDeviceRecoveryCredential credential, CancellationToken cancellationToken = default);

    Task UpdateAsync(PlatformDeviceRecoveryCredential credential, CancellationToken cancellationToken = default);

    Task<int> RevokeActiveForUserAsync(
        PlatformUserId userId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken = default);

    Task<int> RevokeActiveForUserAndDeviceAsync(
        PlatformUserId userId,
        string installationDeviceId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken = default);
}

public sealed class PlatformDeviceRecoveryCredentialOptions
{
    public const string SectionName = "PlatformAuthentication:DeviceRecoveryCredential";

    public int IdleLifetimeDays { get; set; } = 30;

    public int AbsoluteLifetimeDays { get; set; } = 90;

    public TimeSpan ResolveIdleLifetime() =>
        TimeSpan.FromDays(Math.Clamp(IdleLifetimeDays, 1, AbsoluteLifetimeDays));

    public TimeSpan ResolveAbsoluteLifetime() =>
        TimeSpan.FromDays(Math.Clamp(AbsoluteLifetimeDays, IdleLifetimeDays, 365));
}

public sealed record DeviceRecoveryCredentialEnrollDto(
    string RecoveryCredential,
    DateTimeOffset IdleExpiresAtUtc,
    DateTimeOffset AbsoluteExpiresAtUtc);

public sealed record DeviceRecoveryCredentialExchangeDto(
    PlatformAccessTokenIssueDto AccessToken,
    string RecoveryCredential,
    DateTimeOffset IdleExpiresAtUtc,
    DateTimeOffset AbsoluteExpiresAtUtc);
