namespace ExItS.PinoyBusinessPOS.Application.Offline;

/// <summary>Scope of a durable offline operate grant.</summary>
public enum OfflineGrantScopeKind
{
    Organization = 0,
    Personal = 1
}

/// <summary>
/// Durable offline operate grant established only after successful online validation.
/// Does not contain passwords or access tokens. PIN unlock cannot create or extend this grant.
/// Schema v1 grants (OrganizationId required, no ScopeKind) are accepted as Organization scope.
/// </summary>
public sealed record OfflineOperatingGrant(
    int SchemaVersion,
    Guid UserId,
    Guid? OrganizationId,
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
    DateTimeOffset ExpiresAtUtc,
    OfflineGrantScopeKind ScopeKind = OfflineGrantScopeKind.Organization)
{
    public const int LegacySchemaVersion = 1;
    public const int CurrentSchemaVersion = 2;

    public bool IsExpired(DateTimeOffset utcNow) => utcNow >= ExpiresAtUtc;

    public bool IsOrganizationScope =>
        ScopeKind == OfflineGrantScopeKind.Organization && OrganizationId is not null;

    public bool IsPersonalScope =>
        ScopeKind == OfflineGrantScopeKind.Personal && OrganizationId is null;

    /// <summary>Normalizes legacy v1 grants (missing ScopeKind → Organization) for cold-start evaluation.</summary>
    public OfflineOperatingGrant NormalizeForEvaluation()
    {
        if (SchemaVersion == LegacySchemaVersion)
        {
            return this with
            {
                SchemaVersion = CurrentSchemaVersion,
                ScopeKind = OfflineGrantScopeKind.Organization
            };
        }

        return this;
    }

    public static bool IsSupportedSchemaVersion(int schemaVersion) =>
        schemaVersion is LegacySchemaVersion or CurrentSchemaVersion;
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
    InvalidPinFormat = 9,
    ScopeMismatch = 10
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
