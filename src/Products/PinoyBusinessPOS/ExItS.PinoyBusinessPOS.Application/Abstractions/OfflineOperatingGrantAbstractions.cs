using ExItS.PinoyBusinessPOS.Application.Auth;
using ExItS.PinoyBusinessPOS.Application.Offline;

namespace ExItS.PinoyBusinessPOS.Application.Abstractions;

/// <summary>
/// Persists per-user offline operate grants + PIN verifiers in platform secure storage.
/// Legacy single-slot keys are migrated once when attributable to a stable user id.
/// </summary>
public interface IOfflineOperatingGrantStore
{
    /// <summary>Ensures legacy single-slot grant/PIN are migrated into per-user keys (idempotent).</summary>
    Task EnsureMigratedAsync(CancellationToken ct = default);

    Task<IReadOnlyList<OfflineEnrolledUserSummary>> GetEnrolledUsersAsync(CancellationToken ct = default);

    Task<OfflineOperatingGrant?> LoadGrantAsync(Guid userId, CancellationToken ct = default);

    Task SaveGrantAsync(OfflineOperatingGrant grant, CancellationToken ct = default);

    Task ClearGrantAsync(Guid userId, CancellationToken ct = default);

    Task<OfflinePinVerifier?> LoadPinVerifierAsync(Guid userId, CancellationToken ct = default);

    Task SavePinVerifierAsync(Guid userId, OfflinePinVerifier verifier, CancellationToken ct = default);

    Task ClearPinVerifierAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Removes grant, PIN, and directory entry for one enrolled user.</summary>
    Task RemoveUserAsync(Guid userId, CancellationToken ct = default);
}

/// <summary>
/// Establishes and unlocks offline operate grants. PIN never creates or extends authorization.
/// Multiple cashiers may be enrolled on one authorized device.
/// </summary>
public interface IOfflineOperatingGrantService
{
    Task EstablishFromOnlineSessionAsync(
        AuthSession session,
        string deviceId,
        string? roleCode,
        CancellationToken ct = default);

    /// <summary>Hard-clear the current active / last unlocked user's grant (PIN retained).</summary>
    Task ClearAsync(CancellationToken ct = default);

    /// <summary>Hard-clear one enrolled user's grant (PIN retained unless removed).</summary>
    Task ClearUserGrantAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Remove offline access for one user (grant + PIN + directory).</summary>
    Task RemoveEnrolledUserAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Ends the current unlocked process session without clearing durable grants or PINs.
    /// </summary>
    void LockThisProcess();

    Task<IReadOnlyList<OfflineEnrolledUserSummary>> GetEnrolledUsersAsync(CancellationToken ct = default);

    Task<bool> HasPinConfiguredAsync(CancellationToken ct = default);

    Task<bool> HasPinConfiguredAsync(Guid userId, CancellationToken ct = default);

    Task<OfflinePinSetupResult> SetPinAsync(string pin, CancellationToken ct = default);

    Task<OfflineColdStartOffer> EvaluateColdStartOfferAsync(CancellationToken ct = default);

    /// <summary>Unlock when exactly one unlockable enrolled user exists; otherwise requires userId.</summary>
    Task<OfflinePinUnlockResult> UnlockWithPinAsync(string pin, CancellationToken ct = default);

    Task<OfflinePinUnlockResult> UnlockWithPinAsync(Guid userId, string pin, CancellationToken ct = default);

    /// <summary>
    /// Development/testing only: force-expire one user's grant. No-op unless options allow it.
    /// </summary>
    Task<bool> ForceExpireGrantForDevelopmentAsync(Guid userId, CancellationToken ct = default);

    bool IsUnlockedThisProcess { get; }

    OfflineOperatingGrant? ActiveUnlockedGrant { get; }

    /// <summary>
    /// Loads the durable grant for the active unlocked user, else the sole enrolled grant, for safe metadata.
    /// </summary>
    Task<OfflineOperatingGrant?> PeekStoredGrantAsync(CancellationToken ct = default);

    Task<OfflineOperatingGrant?> PeekStoredGrantAsync(Guid userId, CancellationToken ct = default);
}
