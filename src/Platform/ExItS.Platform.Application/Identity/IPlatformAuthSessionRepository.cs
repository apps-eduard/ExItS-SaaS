using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.Application.Identity;

public interface IPlatformAuthSessionRepository
{
    Task<PlatformAuthSession?> GetByIdAsync(
        PlatformAuthSessionId sessionId,
        CancellationToken cancellationToken = default);

    Task<PlatformAuthSession?> GetByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default);

    Task AddAsync(PlatformAuthSession session, CancellationToken cancellationToken = default);

    Task UpdateAsync(PlatformAuthSession session, CancellationToken cancellationToken = default);

    Task<int> RevokeAllActiveForUserAsync(
        PlatformUserId userId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears selected organization context on active sessions for the user when membership/org eligibility ends.
    /// </summary>
    Task<int> ClearSelectedOrganizationAsync(
        PlatformUserId userId,
        PlatformOrganizationId organizationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears selected organization context on all active sessions currently bound to the organization.
    /// </summary>
    Task<int> ClearSelectedOrganizationForOrganizationAsync(
        PlatformOrganizationId organizationId,
        CancellationToken cancellationToken = default);
}

/// <summary>Creates opaque session tokens and hashes them for persistence (never store raw tokens).</summary>
public interface IPlatformSessionTokenService
{
    string CreateOpaqueToken();

    string HashToken(string opaqueToken);
}

/// <summary>Shared claim / scheme names for Platform browser sessions (Application boundary).</summary>
public static class PlatformSessionClaimTypes
{
    public const string AuthenticationScheme = "PlatformSession";
    public const string SessionId = "exits_session_id";
    public const string OrganizationId = "exits_organization_id";
    public const string AccountProfileId = "exits_account_profile_id";
    public const string AccountClass = "exits_account_class";
    public const string AllowedScope = "exits_allowed_scope";
    public const string RequestTokenItemKey = "PlatformSession:token";
}

/// <summary>Server-side organization selection state for an authenticated session.</summary>
public static class OrganizationSelectionStates
{
    public const string None = "None";
    public const string Selected = "Selected";
    public const string SelectionRequired = "SelectionRequired";
}

public sealed record PlatformAuthSessionInfoDto(
    Guid SessionId,
    Guid UserId,
    string Username,
    string DisplayName,
    string Email,
    DateTimeOffset ExpiresAtUtc,
    DateTimeOffset AbsoluteExpiresAtUtc,
    DateTimeOffset LastActivityAtUtc,
    Guid? SelectedOrganizationId,
    string? SelectedOrganizationDisplayName,
    string OrganizationSelectionState,
    int ActiveOrganizationCount,
    PlatformMfaReadinessDto? Mfa = null,
    Guid? AccountProfileId = null,
    string? AccountClass = null,
    string? AllowedScope = null,
    Guid? HomeOrganizationId = null,
    bool OrganizationContextLocked = false);

/// <summary>Returned only from login — includes the opaque session token once.</summary>
public sealed record PlatformLoginResultDto(
    string SessionToken,
    Guid SessionId,
    Guid UserId,
    string Username,
    string DisplayName,
    string Email,
    DateTimeOffset ExpiresAtUtc,
    DateTimeOffset AbsoluteExpiresAtUtc,
    Guid? SelectedOrganizationId,
    string? SelectedOrganizationDisplayName,
    string OrganizationSelectionState,
    int ActiveOrganizationCount,
    PlatformMfaReadinessDto? Mfa = null,
    Guid? AccountProfileId = null,
    string? AccountClass = null,
    string? AllowedScope = null,
    Guid? HomeOrganizationId = null,
    bool OrganizationContextLocked = false);

public sealed record EligibleOrganizationDto(
    Guid OrganizationId,
    string DisplayName,
    string Slug,
    string MembershipRole,
    Guid MembershipId);

public sealed record OrganizationContextResultDto(
    Guid? SelectedOrganizationId,
    string? SelectedOrganizationDisplayName,
    string OrganizationSelectionState,
    int ActiveOrganizationCount);
