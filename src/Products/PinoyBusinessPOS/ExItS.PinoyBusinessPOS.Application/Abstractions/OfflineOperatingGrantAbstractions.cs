using ExItS.PinoyBusinessPOS.Application.Auth;
using ExItS.PinoyBusinessPOS.Application.Offline;

namespace ExItS.PinoyBusinessPOS.Application.Abstractions;

/// <summary>Persists offline operate grant + PIN verifier in platform secure storage.</summary>
public interface IOfflineOperatingGrantStore
{
    Task<OfflineOperatingGrant?> LoadGrantAsync(CancellationToken ct = default);

    Task SaveGrantAsync(OfflineOperatingGrant grant, CancellationToken ct = default);

    Task ClearGrantAsync(CancellationToken ct = default);

    Task<OfflinePinVerifier?> LoadPinVerifierAsync(CancellationToken ct = default);

    Task SavePinVerifierAsync(OfflinePinVerifier verifier, CancellationToken ct = default);

    Task ClearPinVerifierAsync(CancellationToken ct = default);
}

/// <summary>
/// Establishes and unlocks offline operate grants. PIN never creates or extends authorization.
/// </summary>
public interface IOfflineOperatingGrantService
{
    Task EstablishFromOnlineSessionAsync(
        AuthSession session,
        string deviceId,
        string? roleCode,
        CancellationToken ct = default);

    Task ClearAsync(CancellationToken ct = default);

    /// <summary>
    /// Ends the current unlocked process session without clearing the durable grant or PIN.
    /// Next access requires PIN unlock (or online revalidation).
    /// </summary>
    void LockThisProcess();

    Task<bool> HasPinConfiguredAsync(CancellationToken ct = default);

    Task<OfflinePinSetupResult> SetPinAsync(string pin, CancellationToken ct = default);

    Task<OfflineColdStartOffer> EvaluateColdStartOfferAsync(CancellationToken ct = default);

    Task<OfflinePinUnlockResult> UnlockWithPinAsync(string pin, CancellationToken ct = default);

    /// <summary>True after a successful PIN unlock in this process (until logout/clear).</summary>
    bool IsUnlockedThisProcess { get; }

    OfflineOperatingGrant? ActiveUnlockedGrant { get; }

    /// <summary>
    /// Loads the durable grant for safe metadata (status/expiry/scope) without unlocking or exposing PIN.
    /// </summary>
    Task<OfflineOperatingGrant?> PeekStoredGrantAsync(CancellationToken ct = default);
}
