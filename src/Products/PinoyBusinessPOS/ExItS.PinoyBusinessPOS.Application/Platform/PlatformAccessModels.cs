using ExItS.PinoyBusinessPOS.Application.Common;

namespace ExItS.PinoyBusinessPOS.Application.Platform;

public sealed record PlatformUserDto(
    Guid Id,
    string Username,
    string DisplayName,
    string Email,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    DateTimeOffset? SuspendedAtUtc,
    string? SuspensionReason);

public sealed record PlatformOrganizationDto(
    Guid Id,
    string DisplayName,
    string Slug,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record PlatformMembershipDto(
    Guid Id,
    Guid OrganizationId,
    Guid UserId,
    string Role,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    DateTimeOffset? SuspendedAtUtc,
    DateTimeOffset? RemovedAtUtc,
    string? Reason,
    string? ActorReference);

public sealed record PlatformPagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize);

public sealed record EffectiveAccessDto(
    bool Allowed,
    string ReasonCode,
    Guid UserId,
    Guid OrganizationId,
    string ProductCode,
    Guid? MembershipId,
    Guid? AssignmentId,
    Guid? SubscriptionId,
    Guid? SnapshotId,
    DateTimeOffset EvaluatedAtUtc,
    string? SubscriptionStatus = null,
    IReadOnlyList<string>? EnabledFeatureCodes = null);

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
    string? ProductAccessReasonCode);

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
    IReadOnlyList<string>? EnabledFeatureCodes);

public sealed record PlatformAuthEligibleOrganizationDto(
    Guid OrganizationId,
    string DisplayName,
    string Slug,
    string MembershipRole,
    Guid MembershipId);

public sealed record IssuePlatformAccessTokenRequest(
    string? GrantType,
    string? UsernameOrEmail,
    string? Password,
    Guid? OrganizationId,
    string? ProductCode);

public sealed record BindPlatformAccessTokenRequest(
    string? AccessToken,
    Guid OrganizationId,
    string? ProductCode);

/// <summary>Typed Platform identity/access reads used by POS authentication and commercial-access evaluation.</summary>
public interface IPlatformAccessClient
{
    Task<ApiResult<PlatformUserDto>> GetUserAsync(Guid userId, CancellationToken ct = default);
    Task<ApiResult<PlatformOrganizationDto>> GetOrganizationAsync(Guid organizationId, CancellationToken ct = default);
    Task<ApiResult<PlatformPagedResult<PlatformMembershipDto>>> GetUserMembershipsAsync(Guid userId, CancellationToken ct = default);
    Task<ApiResult<EffectiveAccessDto>> EvaluateAccessAsync(Guid userId, Guid organizationId, string productCode, CancellationToken ct = default);

    Task<ApiResult<PlatformAccessTokenIssueDto>> IssueTokenAsync(
        IssuePlatformAccessTokenRequest request,
        CancellationToken ct = default);

    Task<ApiResult<PlatformAccessTokenIssueDto>> BindTokenAsync(
        BindPlatformAccessTokenRequest request,
        CancellationToken ct = default);

    Task<ApiResult<PlatformAccessTokenIntrospectionDto>> IntrospectTokenAsync(
        string? token = null,
        CancellationToken ct = default);

    Task<ApiResult<IReadOnlyList<PlatformAuthEligibleOrganizationDto>>> GetAuthEligibleOrganizationsAsync(
        CancellationToken ct = default);
}
