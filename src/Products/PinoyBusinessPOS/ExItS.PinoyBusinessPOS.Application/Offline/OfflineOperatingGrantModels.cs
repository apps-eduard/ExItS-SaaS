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
/// Schema v1/v2 grants are retained for deserialization but cannot unlock POS after device binding
/// became mandatory in v3.
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
    OfflineGrantScopeKind ScopeKind = OfflineGrantScopeKind.Organization,
    Guid? BranchId = null,
    Guid? PosDeviceId = null)
{
    public const int LegacySchemaVersion = 1;
    public const int PreviousSchemaVersion = 2;
    public const int CurrentSchemaVersion = 3;

    public bool IsExpired(DateTimeOffset utcNow) => utcNow >= ExpiresAtUtc;

    public bool IsOrganizationScope =>
        ScopeKind == OfflineGrantScopeKind.Organization && OrganizationId is not null
        && BranchId is not null && PosDeviceId is not null;

    public bool IsPersonalScope =>
        ScopeKind == OfflineGrantScopeKind.Personal && OrganizationId is null;

    /// <summary>Normalizes legacy v1 grants (missing ScopeKind → Organization) for inspection only.</summary>
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
        schemaVersion is LegacySchemaVersion or PreviousSchemaVersion or CurrentSchemaVersion;
}

public sealed record OfflinePinVerifier(
    string Algorithm,
    int Iterations,
    string SaltBase64,
    string HashBase64,
    int FailedAttempts,
    DateTimeOffset? LockedUntilUtc,
    /// <summary>Owner of this PIN. Null on legacy device verifiers written before user binding.</summary>
    [property: System.Text.Json.Serialization.JsonPropertyName("userId")]
    Guid? UserId = null);

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
    ScopeMismatch = 10,
    UserSelectionRequired = 11
}

public sealed record OfflinePinUnlockResult(
    OfflinePinUnlockStatus Status,
    OfflineOperatingGrant? Grant = null,
    string? SafeMessageKey = null,
    DateTimeOffset? LockedUntilUtc = null);

public sealed record OfflinePinSetupResult(bool Succeeded, string? SafeMessageKey = null);

/// <summary>
/// Safe, non-secret summary of an enrolled offline cashier on this device.
/// Never includes PIN hashes or verifier material.
/// </summary>
public sealed record OfflineEnrolledUserSummary(
    Guid UserId,
    string DisplayName,
    string? Username,
    OfflineGrantScopeKind ScopeKind,
    string? OrganizationDisplayName,
    DateTimeOffset ExpiresAtUtc,
    bool HasPinConfigured);

/// <summary>Result of evaluating whether cold-start offline unlock can be offered.</summary>
public sealed record OfflineColdStartOffer(
    bool CanOfferPinUnlock,
    OfflineOperatingGrant? Grant,
    string? DenialReasonCode,
    IReadOnlyList<OfflineEnrolledUserSummary>? UnlockCandidates = null);

/// <summary>Directory document persisted in SecureStorage (no secrets).</summary>
public sealed record OfflineEnrolledUsersDirectory(
    int SchemaVersion,
    IReadOnlyList<OfflineEnrolledUserDirectoryEntry> Users)
{
    public const int CurrentSchemaVersion = 1;
}

public sealed record OfflineEnrolledUserDirectoryEntry(
    Guid UserId,
    string DisplayName,
    string? Username,
    OfflineGrantScopeKind ScopeKind,
    string? OrganizationDisplayName,
    DateTimeOffset ExpiresAtUtc);
