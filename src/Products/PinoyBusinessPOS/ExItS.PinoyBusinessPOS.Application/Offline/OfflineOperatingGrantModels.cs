namespace ExItS.PinoyBusinessPOS.Application.Offline;

/// <summary>
/// Durable offline operate grant established only after successful online org/role validation.
/// Does not contain passwords or access tokens. PIN unlock cannot create or extend this grant.
/// </summary>
public sealed record OfflineOperatingGrant(
    int SchemaVersion,
    Guid UserId,
    Guid OrganizationId,
    string OrganizationDisplayName,
    string DeviceId,
    string? RoleCode,
    IReadOnlyList<string> EnabledFeatureCodes,
    string? SubscriptionStatus,
    string? DisplayName,
    string? Username,
    string? Email,
    DateTimeOffset IssuedAtUtc,
    DateTimeOffset LastOnlineValidatedAtUtc,
    DateTimeOffset ExpiresAtUtc)
{
    public const int CurrentSchemaVersion = 1;

    public bool IsExpired(DateTimeOffset utcNow) => utcNow >= ExpiresAtUtc;
}

public sealed record OfflinePinVerifier(
    string Algorithm,
    int Iterations,
    string SaltBase64,
    string HashBase64,
    int FailedAttempts,
    DateTimeOffset? LockedUntilUtc);

public enum OfflinePinUnlockStatus
{
    Succeeded = 0,
    WrongPin = 1,
    Locked = 2,
    GrantMissing = 3,
    GrantExpired = 4,
    DeviceMismatch = 5,
    OrgMismatch = 6,
    UserMismatch = 7,
    PinNotConfigured = 8,
    InvalidPinFormat = 9
}

public sealed record OfflinePinUnlockResult(
    OfflinePinUnlockStatus Status,
    OfflineOperatingGrant? Grant = null,
    string? SafeMessageKey = null,
    DateTimeOffset? LockedUntilUtc = null);

public sealed record OfflinePinSetupResult(bool Succeeded, string? SafeMessageKey = null);

/// <summary>Result of evaluating whether cold-start offline unlock can be offered.</summary>
public sealed record OfflineColdStartOffer(
    bool CanOfferPinUnlock,
    OfflineOperatingGrant? Grant,
    string? DenialReasonCode);
