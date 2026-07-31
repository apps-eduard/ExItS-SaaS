using ExItS.Platform.Domain.Identity;
using Microsoft.Extensions.Options;

namespace ExItS.Platform.Application.Identity;

/// <summary>
/// MFA readiness options. Enrollment and enforcement remain disabled until a later authorized WP.
/// Production must keep both flags false (startup validation).
/// </summary>
public sealed class PlatformMfaOptions
{
    public const string SectionName = "PlatformAuthentication:Mfa";

    /// <summary>When true, enrollment APIs may be enabled. Not implemented in P13-WP07.</summary>
    public bool EnrollmentEnabled { get; set; }

    /// <summary>When true, login/token issuance may require MFA challenge. Not implemented in P13-WP07.</summary>
    public bool EnforcementEnabled { get; set; }
}

/// <summary>Non-enforcing MFA readiness snapshot for identity/session/token contracts.</summary>
public sealed record PlatformMfaReadinessDto(
    bool MfaEnabled,
    bool EnrollmentAvailable,
    bool EnforcementRequired,
    bool ChallengeRequired,
    int RegisteredFactorCount,
    string ReadinessState);

/// <summary>Reserved factor kinds for a future MFA enrollment WP (no secrets stored in WP07).</summary>
public static class PlatformMfaFactorKinds
{
    public const string Totp = "totp";
    public const string RecoveryCode = "recovery_code";
}

/// <summary>
/// Extension point for future MFA factor persistence. WP07 ships a no-op store only —
/// no secrets, no enrollment, no challenge.
/// </summary>
public interface IPlatformMfaFactorStore
{
    Task<int> CountRegisteredFactorsAsync(PlatformUserId userId, CancellationToken cancellationToken = default);
}

/// <summary>Evaluates MFA readiness signals without performing challenges.</summary>
public interface IPlatformMfaReadinessService
{
    Task<PlatformMfaReadinessDto> GetForUserAsync(
        PlatformUserId userId,
        CancellationToken cancellationToken = default);
}

public sealed class NullPlatformMfaFactorStore : IPlatformMfaFactorStore
{
    public Task<int> CountRegisteredFactorsAsync(
        PlatformUserId userId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(0);
}

public sealed class PlatformMfaReadinessService(
    IPlatformMfaFactorStore factorStore,
    IOptions<PlatformMfaOptions> options) : IPlatformMfaReadinessService
{
    public const string StateNotEnrolled = "NotEnrolled";
    public const string StateReadyDeferred = "ReadyDeferred";

    public async Task<PlatformMfaReadinessDto> GetForUserAsync(
        PlatformUserId userId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(userId);
        var cfg = options.Value;
        var factorCount = await factorStore
            .CountRegisteredFactorsAsync(userId, cancellationToken)
            .ConfigureAwait(false);

        // Enforcement/enrollment flags are configuration-only until a later WP implements flows.
        var enforcementRequired = cfg.EnforcementEnabled && factorCount > 0;
        var challengeRequired = false; // Never challenge in WP07.
        var mfaEnabled = factorCount > 0;
        var readiness = factorCount > 0 || cfg.EnrollmentEnabled || cfg.EnforcementEnabled
            ? StateReadyDeferred
            : StateNotEnrolled;

        return new PlatformMfaReadinessDto(
            mfaEnabled,
            cfg.EnrollmentEnabled,
            enforcementRequired,
            challengeRequired,
            factorCount,
            readiness);
    }
}
