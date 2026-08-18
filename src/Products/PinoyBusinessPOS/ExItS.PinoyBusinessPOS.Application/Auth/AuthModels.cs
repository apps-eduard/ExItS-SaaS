namespace ExItS.PinoyBusinessPOS.Application.Auth;

/// <summary>PinoyBusinessPOS commercial product code used for Platform access evaluation.</summary>
public static class PosProductCodes
{
    public const string PinoyBusinessPos = "pinoy-business-pos";
}

public static class SecureTokenKeys
{
    public const string UserId = "pos.session.userId";
    public const string SessionMarker = "pos.session.marker";
    public const string IssuedAtUtc = "pos.session.issuedAtUtc";
    public const string ExpiresAtUtc = "pos.session.expiresAtUtc";
    public const string SubscriptionStatus = "pos.session.subscriptionStatus";
    public const string FeatureGrants = "pos.session.featureGrants";
    public const string AccessToken = "pos.session.accessToken";
    public const string PlatformSessionToken = "pos.session.platformSessionToken";
    public const string AccountClass = "pos.session.accountClass";
    public const string AccountProfileId = "pos.session.accountProfileId";
    public const string OrganizationContextLocked = "pos.session.organizationContextLocked";
    public const string BranchId = "pos.session.branchId";
    public const string PosDeviceId = "pos.session.posDeviceId";

    /// <summary>
    /// Durable installation DeviceId. Not a session key — must survive logout and ClearAllSessionKeysAsync.
    /// </summary>
    public const string DeviceId = "pos.device.id";

    /// <summary>
    /// AES-GCM key for offline queue payloads. Survives logout; never stored in SQLite.
    /// </summary>
    public const string LocalPayloadEncryptionKey = "pos.local.payload.key";

    /// <summary>
    /// Legacy single-slot offline operate grant (pre multi-cashier). Migrated to
    /// <see cref="OfflineOperatingGrantFor"/> then removed.
    /// </summary>
    public const string OfflineOperatingGrant = "pos.offline.operatingGrant";

    /// <summary>
    /// Legacy single-slot PIN verifier (pre multi-cashier). Migrated to
    /// <see cref="OfflinePinVerifierFor"/> then removed.
    /// </summary>
    public const string OfflinePinVerifier = "pos.offline.pinVerifier";

    /// <summary>Non-secret directory of enrolled offline users on this device.</summary>
    public const string OfflineEnrolledUsers = "pos.offline.enrolledUsers";

    public static string OfflineOperatingGrantFor(Guid userId) =>
        $"pos.offline.grant.{userId:D}";

    public static string OfflinePinVerifierFor(Guid userId) =>
        $"pos.offline.pin.{userId:D}";

    /// <summary>
    /// Per-user Platform session handle for PIN sign-in recovery. Not an AccessToken, password, or PIN.
    /// Survives MAUI session-key clear on logout (not a session slot).
    /// </summary>
    public static string PinRecoveryPlatformSessionFor(Guid userId) =>
        $"pos.pin.recovery.session.{userId:D}";

    /// <summary>
    /// Per-user rotating device recovery credential for online PIN re-entry.
    /// Survives logout; never stores PINs or passwords.
    /// </summary>
    public static string DeviceRecoveryCredentialFor(Guid userId) =>
        $"pos.pin.recovery.credential.{userId:D}";
}

public static class PreferenceKeys
{
    public const string OnboardingCompleted = "exits-pos-onboarding-completed";
    public const string OnboardingStep = "exits-pos-onboarding-step";
    public const string SelectedOrganizationId = "exits-pos-selected-org";
    public const string DevEnvironmentConfirmed = "exits-pos-dev-env-confirmed";
    public const string RememberMe = "exits-pos-remember-me";
    public const string RememberedUsername = "exits-pos-remembered-username";

    /// <summary>Prefix for per-organization one-time business template onboarding prompt flags.</summary>
    public const string BusinessTemplatePromptPendingPrefix = "exits-pos-template-prompt-pending-";

    /// <summary>Prefix for per-organization optional Business Type activation onboarding prompt flags.</summary>
    public const string BusinessTypeActivationPromptPendingPrefix = "exits-pos-bt-activation-prompt-pending-";
}

/// <summary>MAUI WebAuthenticator callback for Platform external login redirects.</summary>
public static class PosExternalAuth
{
    public const string CallbackUrl = "exitspos://auth/callback";
}

public enum OnboardingStep
{
    Welcome = 0,
    Language = 1,
    Theme = 2,
    Density = 3,
    DevEnvironment = 4,
    SignIn = 5,
    OrganizationSelect = 6,
    AccessConfirm = 7,
    Complete = 8
}

public enum AuthFailureReason
{
    None = 0,
    ProductionAuthUnavailable,
    InvalidCredentials,
    UserInactive,
    Offline,
    ApiUnavailable,
    Timeout,
    RateLimited,
    SecureStorageFailure,
    SessionExpired,
    RefreshFailed,
    AccessDenied,
    Cancelled,
    Unknown
}

public sealed record AuthSession(
    Guid UserId,
    string DisplayName,
    string Username,
    string Email,
    Guid? OrganizationId,
    string? OrganizationDisplayName,
    DateTimeOffset IssuedAtUtc,
    DateTimeOffset ExpiresAtUtc,
    bool HasPosAccess,
    string? AccessReasonCode,
    string? SubscriptionStatus = null,
    IReadOnlyList<string>? EnabledFeatureCodes = null,
    string? AccessToken = null,
    string? PlatformSessionToken = null,
    string? AccountClass = null,
    Guid? AccountProfileId = null,
    bool OrganizationContextLocked = false,
    Guid? BranchId = null,
    Guid? PosDeviceId = null);

public sealed record EligibleOrganization(
    Guid OrganizationId,
    string DisplayName,
    Guid MembershipId,
    string MembershipStatus,
    bool AccessAllowed,
    string AccessReasonCode,
    string? MembershipRole = null);

/// <summary>
/// Workspace routing helpers for Mobile post-login / cold-start destinations.
/// </summary>
public static class AuthSessionWorkspace
{
    /// <summary>
    /// Personal default workspace: Personal account class, not org-locked staff.
    /// Stale/forged OrganizationId must not convert this into an Organization session.
    /// </summary>
    public static bool IsPersonalDefault(AuthSession? session) =>
        session is not null
        && !session.OrganizationContextLocked
        && string.Equals(session.AccountClass, "Personal", StringComparison.OrdinalIgnoreCase);
}

/// <summary>
/// Platform membership roles used for Mobile post-login routing.
/// Organization Owners keep Personal + Org choice; staff go straight into org/role UI.
/// </summary>
public static class OrganizationMembershipRoles
{
    public const string Owner = "OrganizationOwner";
    public const string Administrator = "OrganizationAdministrator";
    public const string Member = "OrganizationMember";

    public static bool IsOwnerRole(string? role) =>
        string.Equals(role, Owner, StringComparison.OrdinalIgnoreCase)
        || string.Equals(role, Administrator, StringComparison.OrdinalIgnoreCase);

    public static bool HasOrganizationOwner(IEnumerable<EligibleOrganization> organizations) =>
        organizations.Any(o => IsOwnerRole(o.MembershipRole));
}

public sealed record SignInRequest(
    string? UsernameOrEmail,
    string? Password,
    Guid? PlatformUserId = null,
    Guid? AccountProfileId = null);

/// <summary>
/// Server follow-up after a locally verified PIN. Never collapse this to a boolean
/// "server accepted" — transient failure is not revocation.
/// </summary>
public enum PinSignInServerOutcome
{
    /// <summary>PIN failed locally, or recovery was not attempted.</summary>
    NotAttempted = 0,

    /// <summary>PIN valid; device is offline or no recoverable Platform session exists.</summary>
    LocalOffline = 1,

    /// <summary>PIN valid; Platform session revalidated for the same user.</summary>
    ValidatedOnline = 2,

    /// <summary>PIN valid; server unreachable/timeout/5xx. Local grant retained.</summary>
    TransientUnavailable = 3,

    /// <summary>PIN valid; server explicitly denied this identity/device/assignment.</summary>
    ExplicitlyRevoked = 4,

    /// <summary>
    /// PIN valid and server reachable, but the device recovery credential is missing or expired.
    /// Requires a normal full sign-in once to re-enroll PIN quick sign-in.
    /// </summary>
    OnlineVerificationRequired = 5
}

public sealed record AuthResult(
    bool Succeeded,
    AuthFailureReason FailureReason,
    AuthSession? Session = null,
    string? SafeMessageKey = null,
    PinSignInServerOutcome ServerOutcome = PinSignInServerOutcome.NotAttempted);
