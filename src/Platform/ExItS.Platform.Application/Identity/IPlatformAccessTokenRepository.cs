using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.Application.Identity;

public interface IPlatformAccessTokenRepository
{
    Task<PlatformAccessToken?> GetByIdAsync(
        PlatformAccessTokenId tokenId,
        CancellationToken cancellationToken = default);

    Task<PlatformAccessToken?> GetByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default);

    Task AddAsync(PlatformAccessToken token, CancellationToken cancellationToken = default);

    Task UpdateAsync(PlatformAccessToken token, CancellationToken cancellationToken = default);

    Task<int> RevokeAllActiveForUserAsync(
        PlatformUserId userId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken = default);

    Task<int> ClearOrganizationBindingAsync(
        PlatformUserId userId,
        PlatformOrganizationId organizationId,
        CancellationToken cancellationToken = default);

    Task<int> ClearOrganizationBindingForOrganizationAsync(
        PlatformOrganizationId organizationId,
        CancellationToken cancellationToken = default);
}

public sealed class PlatformAccessTokenOptions
{
    public const string SectionName = "PlatformAuthentication:AccessToken";

    public int LifetimeHours { get; set; } = 8;

    /// <summary>Upper bound applied when issuing tokens (Production validation forbids LifetimeHours above this).</summary>
    public int MaxLifetimeHours { get; set; } = 24;

    public int ResolveLifetimeHours() =>
        Math.Clamp(LifetimeHours, 1, Math.Max(1, MaxLifetimeHours));
}

public sealed record PlatformAccessTokenIssueDto(
    string AccessToken,
    string TokenType,
    Guid TokenId,
    Guid UserId,
    string Username,
    string DisplayName,
    string Email,
    DateTimeOffset ExpiresAtUtc,
    Guid? OrganizationId,
    string? OrganizationDisplayName,
    string? ProductCode,
    string OrganizationSelectionState,
    int ActiveOrganizationCount,
    bool? ProductAccessAllowed,
    string? ProductAccessReasonCode,
    PlatformMfaReadinessDto? Mfa = null,
    string? ProductLocalRoleCode = null,
    string? MappedPosRoleCode = null,
    string? MembershipRole = null,
    bool OrganizationManagementAuthority = false);

public sealed record PlatformAccessTokenIntrospectionDto(
    bool Active,
    Guid? TokenId,
    Guid? UserId,
    string? Username,
    string? DisplayName,
    Guid? OrganizationId,
    string? OrganizationDisplayName,
    string? ProductCode,
    DateTimeOffset? ExpiresAtUtc,
    bool? ProductAccessAllowed,
    string? ProductAccessReasonCode,
    string? SubscriptionStatus,
    IReadOnlyList<string>? EnabledFeatureCodes,
    PlatformMfaReadinessDto? Mfa = null,
    string? ProductLocalRoleCode = null,
    string? MappedPosRoleCode = null,
    string? MembershipRole = null,
    bool OrganizationManagementAuthority = false);
