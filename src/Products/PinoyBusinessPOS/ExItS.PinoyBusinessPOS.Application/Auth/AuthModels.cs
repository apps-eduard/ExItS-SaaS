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

    /// <summary>
    /// Durable installation DeviceId. Not a session key — must survive logout and ClearAllSessionKeysAsync.
    /// </summary>
    public const string DeviceId = "pos.device.id";

    /// <summary>
    /// AES-GCM key for offline queue payloads. Survives logout; never stored in SQLite.
    /// </summary>
    public const string LocalPayloadEncryptionKey = "pos.local.payload.key";

    /// <summary>
    /// Offline operate grant (no tokens/passwords). Kept across Sign out for PIN unlock;
    /// cleared on hard revoke (server denial) / remove-from-device; replaced on online bind.
    /// </summary>
    public const string OfflineOperatingGrant = "pos.offline.operatingGrant";

    /// <summary>
    /// Salted PIN verifier for unlocking an existing offline grant. Survives Sign out.
    /// </summary>
    public const string OfflinePinVerifier = "pos.offline.pinVerifier";
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
    bool OrganizationContextLocked = false);

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

public sealed record AuthResult(
    bool Succeeded,
    AuthFailureReason FailureReason,
    AuthSession? Session = null,
    string? SafeMessageKey = null);
