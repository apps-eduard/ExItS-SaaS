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
    DateTimeOffset EvaluatedAtUtc);

/// <summary>Typed Platform identity/access reads used by POS authentication and commercial-access evaluation.</summary>
public interface IPlatformAccessClient
{
    Task<ApiResult<PlatformUserDto>> GetUserAsync(Guid userId, CancellationToken ct = default);
    Task<ApiResult<PlatformOrganizationDto>> GetOrganizationAsync(Guid organizationId, CancellationToken ct = default);
    Task<ApiResult<PlatformPagedResult<PlatformMembershipDto>>> GetUserMembershipsAsync(Guid userId, CancellationToken ct = default);
    Task<ApiResult<EffectiveAccessDto>> EvaluateAccessAsync(Guid userId, Guid organizationId, string productCode, CancellationToken ct = default);
}
