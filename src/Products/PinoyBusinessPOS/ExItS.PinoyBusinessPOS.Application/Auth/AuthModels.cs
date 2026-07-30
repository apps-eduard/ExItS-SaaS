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
}

public static class PreferenceKeys
{
    public const string OnboardingCompleted = "exits-pos-onboarding-completed";
    public const string OnboardingStep = "exits-pos-onboarding-step";
    public const string SelectedOrganizationId = "exits-pos-selected-org";
    public const string DevEnvironmentConfirmed = "exits-pos-dev-env-confirmed";
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
    string? AccessReasonCode);

public sealed record EligibleOrganization(
    Guid OrganizationId,
    string DisplayName,
    Guid MembershipId,
    string MembershipStatus,
    bool AccessAllowed,
    string AccessReasonCode);

public sealed record SignInRequest(Guid PlatformUserId);

public sealed record AuthResult(
    bool Succeeded,
    AuthFailureReason FailureReason,
    AuthSession? Session = null,
    string? SafeMessageKey = null);
