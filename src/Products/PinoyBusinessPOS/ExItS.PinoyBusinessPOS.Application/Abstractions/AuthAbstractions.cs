using ExItS.PinoyBusinessPOS.Application.Auth;
using ExItS.PinoyBusinessPOS.Application.Offline;
using ExItS.PinoyBusinessPOS.Application.Platform;

namespace ExItS.PinoyBusinessPOS.Application.Abstractions;

/// <summary>
/// Platform-secure key/value store for authentication session secrets (e.g. MAUI SecureStorage).
/// Never store passwords. Never use Preferences/localStorage for tokens.
/// </summary>
public interface ISecureTokenStore
{
    Task<string?> GetAsync(string key, CancellationToken ct = default);
    Task SetAsync(string key, string value, CancellationToken ct = default);
    Task ClearAsync(string key, CancellationToken ct = default);
    Task ClearAllSessionKeysAsync(CancellationToken ct = default);
}

/// <summary>Non-secret preference store for onboarding progress and selected organization.</summary>
public interface IOnboardingPreferenceStore
{
    Task<bool> GetOnboardingCompletedAsync(CancellationToken ct = default);
    Task SetOnboardingCompletedAsync(bool completed, CancellationToken ct = default);
    Task<string?> GetOnboardingStepAsync(CancellationToken ct = default);
    Task SetOnboardingStepAsync(string step, CancellationToken ct = default);
    Task<Guid?> GetSelectedOrganizationIdAsync(CancellationToken ct = default);
    Task SetSelectedOrganizationIdAsync(Guid? organizationId, CancellationToken ct = default);
    Task<bool> GetDevEnvironmentConfirmedAsync(CancellationToken ct = default);
    Task SetDevEnvironmentConfirmedAsync(bool confirmed, CancellationToken ct = default);
    Task ClearOrganizationPreferenceAsync(CancellationToken ct = default);

    /// <summary>
    /// True when Start Business / trial should offer a one-time skippable business-template prompt
    /// for this organization before landing on the POS home.
    /// </summary>
    Task<bool> GetBusinessTemplatePromptPendingAsync(Guid organizationId, CancellationToken ct = default);

    Task SetBusinessTemplatePromptPendingAsync(Guid organizationId, bool pending, CancellationToken ct = default);

    /// <summary>
    /// True when Start Business should offer optional additional Business Type activation
    /// (Growth/Pro capacity) before device registration.
    /// </summary>
    Task<bool> GetBusinessTypeActivationPromptPendingAsync(Guid organizationId, CancellationToken ct = default);

    Task SetBusinessTypeActivationPromptPendingAsync(Guid organizationId, bool pending, CancellationToken ct = default);
}

public interface ISessionStore
{
    Task SaveAsync(AuthSession session, string sessionMarker, CancellationToken ct = default);
    Task<(AuthSession? Session, string? Marker)> LoadAsync(CancellationToken ct = default);
    Task ClearAsync(CancellationToken ct = default);
}

public interface ICurrentUserContext
{
    AuthSession? Session { get; }
    bool IsAuthenticated { get; }
    bool HasPosAccess { get; }
    void Set(AuthSession? session);
    void Clear();
    event Func<Task>? Changed;
}

public interface IProductAccessResolver
{
    Task<AuthResult> EvaluateAsync(Guid userId, Guid organizationId, CancellationToken ct = default);
    Task<IReadOnlyList<EligibleOrganization>> ListEligibleOrganizationsAsync(Guid userId, CancellationToken ct = default);
}

public interface IAuthenticationService
{
    /// <summary>True only in Development/Testing when the approved non-production identity mechanism is enabled.</summary>
    bool IsDevelopmentAuthenticationEnabled { get; }

    Task<AuthResult> SignInAsync(SignInRequest request, CancellationToken ct = default);

    /// <summary>
    /// Completes sign-in from a Platform opaque session token (e.g. Google external login callback).
    /// Hydrates identity via /auth/me and issues a POS bearer token via the session grant when possible.
    /// </summary>
    Task<AuthResult> SignInWithPlatformSessionTokenAsync(string sessionToken, CancellationToken ct = default);

    Task<AuthResult> RestoreSessionAsync(CancellationToken ct = default);
    Task<AuthResult> RefreshSessionAsync(CancellationToken ct = default);
    Task LogoutAsync(CancellationToken ct = default);

    /// <summary>
    /// Locks the device into PIN unlock while preserving the durable offline operate grant.
    /// Unlike <see cref="LogoutAsync"/>, Lock does not clear local offline trust.
    /// </summary>
    Task LockAsync(CancellationToken ct = default);

    Task<AuthResult> SelectOrganizationAsync(Guid organizationId, CancellationToken ct = default);

    /// <summary>
    /// Leaves organization/POS context and returns to Personal without signing out.
    /// Clears organization-scoped local state and process validation; keeps the Platform session.
    /// </summary>
    Task<AuthResult> SwitchToPersonalAsync(CancellationToken ct = default);

    /// <summary>
    /// When the session is still Personal after membership exists (invite accept / Start Business),
    /// selects the Organization account profile so org-bound APIs and POS bind can proceed.
    /// </summary>
    Task<AuthResult> EnsureOrganizationAccountProfileAsync(CancellationToken ct = default);

    /// <summary>
    /// Selects the Personal account profile so `/api/v1/personal/*` Utang APIs are in scope.
    /// Clears organization/POS local context without signing out.
    /// </summary>
    Task<AuthResult> EnsurePersonalAccountProfileAsync(CancellationToken ct = default);

    /// <summary>
    /// Applies a rotated Platform session after Start a Business and binds the new organization for POS when entitled.
    /// </summary>
    Task<AuthResult> ContinueAfterStartBusinessAsync(
        StartBusinessResultDto result,
        CancellationToken ct = default);

    /// <summary>
    /// Verifies the local PIN for an enrolled identity, then revalidates an online session
    /// when the server is reachable. Wrong PIN never contacts the server.
    /// <see cref="AuthResult.ServerOutcome"/> distinguishes ValidatedOnline, LocalOffline,
    /// TransientUnavailable, and ExplicitlyRevoked.
    /// When multiple cashiers are enrolled, use <see cref="UnlockOfflineWithPinAsync(Guid, string, CancellationToken)"/>.
    /// </summary>
    Task<AuthResult> UnlockOfflineWithPinAsync(string pin, CancellationToken ct = default);

    /// <summary>PIN sign-in for a specific enrolled user on this device. Never reuses another user's tokens.</summary>
    Task<AuthResult> UnlockOfflineWithPinAsync(Guid userId, string pin, CancellationToken ct = default);

    /// <summary>Safe list of cashiers enrolled for offline unlock on this device (no secrets).</summary>
    Task<IReadOnlyList<OfflineEnrolledUserSummary>> GetEnrolledOfflineUsersAsync(
        CancellationToken ct = default);

    /// <summary>Sets or replaces the local offline PIN while an online-validated grant exists.</summary>
    Task<OfflinePinSetupResult> SetOfflinePinAsync(string pin, CancellationToken ct = default);

    Task<bool> HasOfflinePinConfiguredAsync(CancellationToken ct = default);

    Task<OfflineColdStartOffer> EvaluateOfflineColdStartOfferAsync(CancellationToken ct = default);

    /// <summary>
    /// After online auth, ensure the current session's device/grant then evaluate complete
    /// offline PIN setup (identity + grant + matching device + verifier). Not a cold-start offer.
    /// </summary>
    Task<OfflineColdStartOffer> EvaluateCurrentUserOfflinePinReadinessAsync(CancellationToken ct = default);

    /// <summary>
    /// Ensures a durable offline operate grant exists for the current online session
    /// (Organization POS or Personal) so mandatory PIN enrollment can succeed.
    /// </summary>
    Task EnsureOfflineOperateGrantAsync(CancellationToken ct = default);

    /// <summary>Removes one enrolled offline cashier from this device (grant + PIN).</summary>
    Task RemoveEnrolledOfflineUserAsync(Guid userId, CancellationToken ct = default);
}

/// <summary>
/// Per-user device recovery credential used to obtain a fresh access token after local PIN verification.
/// Never stores passwords, PINs, or access tokens.
/// </summary>
public interface IDeviceRecoveryCredentialStore
{
    Task SaveAsync(
        Guid userId,
        string deviceId,
        string recoveryCredential,
        DateTimeOffset idleExpiresAtUtc,
        DateTimeOffset absoluteExpiresAtUtc,
        CancellationToken ct = default);

    Task<StoredDeviceRecoveryCredential?> LoadAsync(Guid userId, CancellationToken ct = default);

    Task ClearAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Legacy Platform session handle for one-time migration while online.</summary>
    Task<string?> LoadLegacySessionHandleAsync(Guid userId, CancellationToken ct = default);

    Task ClearLegacySessionHandleAsync(Guid userId, CancellationToken ct = default);
}

/// <summary>
/// Obsolete transitional store. Prefer <see cref="IDeviceRecoveryCredentialStore"/>.
/// </summary>
[Obsolete("Use IDeviceRecoveryCredentialStore.")]
public interface IPinRecoverySessionStore
{
    Task SaveAsync(Guid userId, string platformSessionToken, CancellationToken ct = default);

    Task<string?> LoadAsync(Guid userId, CancellationToken ct = default);

    Task ClearAsync(Guid userId, CancellationToken ct = default);
}

/// <summary>Local security-event sink. Does not replace Platform audit authority.</summary>
public interface IAuthEventSink
{
    void Record(string eventName, IReadOnlyDictionary<string, string?> safeProperties);
}
