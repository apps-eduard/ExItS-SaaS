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

public sealed record UpdatePlatformOrganizationRequest(string DisplayName, string? Slug = null);

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

public sealed record PlatformMembershipLifecycleRequest(string? Reason = null, string? ActorReference = null);

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

public sealed record RegisterPersonalAccountRequest(string DisplayName, string Email);

public sealed record ActivatePersonalAccountRequest(string Token, string Password);

public sealed record PersonalRegistrationAckDto(string Message, string? DebugToken, DateTimeOffset? ExpiresAtUtc);

public sealed record PlatformLoginRequest(string UsernameOrEmail, string Password);

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
    Guid? AccountProfileId = null,
    string? AccountClass = null,
    string? AllowedScope = null);

public sealed record SelectAccountProfileRequest(Guid AccountProfileId);

public sealed record StartBusinessRequest(
    string DisplayName,
    string Slug,
    string? ProductCode = null,
    string? PlanKey = null,
    string? BillingCycle = null,
    bool StartAsTrial = true,
    bool PayNow = false,
    bool ActivatePosEntitlement = true,
    bool ActivateProductAccess = true,
    bool AssignPosOwnerRole = true);

public sealed record StartBusinessResultDto(
    Guid OrganizationId,
    Guid MembershipId,
    Guid OrganizationAccountProfileId,
    string SessionToken,
    Guid SessionId,
    string AccountClass,
    string AllowedScope,
    Guid? SelectedOrganizationId,
    Guid? SubscriptionId,
    int? EntitlementSnapshotVersion,
    Guid? ProductAccessAssignmentId,
    Guid? ProductLocalRoleGrantId,
    string? ProductLocalRoleCode,
    bool OrganizationOwnerGranted,
    bool PosEntitlementActivated,
    bool PosOwnerRoleGranted,
    string ProductCode);

public sealed record CreateInvitationRequest(
    string Email,
    string Role,
    string? DisplayName = null,
    string? FirstName = null,
    string? LastName = null,
    string? ProductRole = null,
    bool RequireEmailVerification = true);

public sealed record OrganizationInvitationDto(
    Guid Id,
    Guid OrganizationId,
    string InvitationType,
    string Email,
    string Role,
    string Status,
    Guid? InvitedByUserId,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    DateTimeOffset ExpiresAtUtc,
    DateTimeOffset? AcceptedAtUtc = null,
    DateTimeOffset? RevokedAtUtc = null,
    Guid? AcceptedByUserId = null,
    string? ProductRole = null,
    string? InviteeDisplayName = null);

public sealed record ProductLocalRoleGrantDto(
    Guid Id,
    Guid OrganizationId,
    Guid UserIdentityId,
    string ProductCode,
    string RoleCode,
    string MappedPosRoleCode,
    string Status,
    DateTimeOffset GrantedAtUtc,
    Guid GrantedByUserIdentityId,
    string Source,
    DateTimeOffset? RevokedAtUtc = null,
    Guid? RevokedByUserIdentityId = null,
    string? Reason = null,
    string? UserDisplayName = null,
    string? RoleDisplay = null);

public sealed record AssignProductLocalRoleRequest(
    Guid UserIdentityId,
    string ProductCode,
    string RoleCode,
    string? Reason = null);

public sealed record RevokeProductLocalRoleRequest(string? Reason = null);

public sealed record PlatformSubscriptionDto(
    Guid Id,
    Guid OrganizationId,
    string ProductCode,
    string Status,
    DateTimeOffset? TrialEndUtc = null,
    DateTimeOffset? CurrentPeriodEndUtc = null,
    string? PlanKey = null,
    string? PlanDisplayName = null,
    string? BillingCycle = null);

public sealed record PlatformEntitlementSnapshotDto(
    Guid Id,
    Guid OrganizationId,
    string ProductCode,
    Guid SubscriptionId,
    string PlanCode,
    int SnapshotVersion,
    string SubscriptionStatus,
    DateTimeOffset GeneratedAtUtc,
    DateTimeOffset? ExpiresAtUtc = null);

public sealed record SetOrganizationContextRequest(Guid? OrganizationId);

/// <summary>Local Validation Quick Login identity (Platform API, non-Production).</summary>
public sealed record LocalValidationQuickLoginIdentityDto(
    string Key,
    string Username,
    string DisplayName,
    string Email,
    Guid UserId,
    Guid AccountProfileId,
    string AccountClass,
    Guid? OrganizationId,
    string? OrganizationName,
    string? OrganizationRole,
    string ListLabel,
    string ScopeLabel);

/// <summary>Typed Platform identity/access reads used by POS authentication and Mobile Org Owner essentials.</summary>
public interface IPlatformAccessClient
{
    Task<ApiResult<PlatformUserDto>> GetUserAsync(Guid userId, CancellationToken ct = default);
    Task<ApiResult<PlatformOrganizationDto>> GetOrganizationAsync(Guid organizationId, CancellationToken ct = default);
    Task<ApiResult<PlatformOrganizationDto>> UpdateOrganizationAsync(
        Guid organizationId,
        UpdatePlatformOrganizationRequest request,
        CancellationToken ct = default);
    Task<ApiResult<PlatformPagedResult<PlatformMembershipDto>>> GetUserMembershipsAsync(Guid userId, CancellationToken ct = default);
    Task<ApiResult<PlatformPagedResult<PlatformMembershipDto>>> GetOrganizationMembersAsync(
        Guid organizationId,
        int page = 1,
        int pageSize = 50,
        string? status = null,
        CancellationToken ct = default);
    Task<ApiResult<PlatformMembershipDto>> SuspendMembershipAsync(
        Guid membershipId,
        PlatformMembershipLifecycleRequest request,
        CancellationToken ct = default);
    Task<ApiResult<PlatformMembershipDto>> RevokeMembershipAsync(
        Guid membershipId,
        PlatformMembershipLifecycleRequest request,
        CancellationToken ct = default);
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

    Task<ApiResult<object>> RevokeAccessTokenAsync(CancellationToken ct = default);

    Task<ApiResult<IReadOnlyList<PlatformAuthEligibleOrganizationDto>>> GetAuthEligibleOrganizationsAsync(
        CancellationToken ct = default);

    Task<ApiResult<PersonalRegistrationAckDto>> RegisterPersonalAccountAsync(
        RegisterPersonalAccountRequest request,
        CancellationToken ct = default);

    Task<ApiResult<object>> ActivatePersonalAccountAsync(
        ActivatePersonalAccountRequest request,
        CancellationToken ct = default);

    Task<ApiResult<PlatformLoginResultDto>> LoginAsync(
        PlatformLoginRequest request,
        CancellationToken ct = default);

    Task<ApiResult<object>> LogoutSessionAsync(CancellationToken ct = default);

    Task<ApiResult<PlatformLoginResultDto>> SelectAccountProfileAsync(
        SelectAccountProfileRequest request,
        CancellationToken ct = default);

    Task<ApiResult<StartBusinessResultDto>> StartBusinessAsync(
        StartBusinessRequest request,
        CancellationToken ct = default);

    Task<ApiResult<OrganizationInvitationDto>> CreateOrganizationInvitationAsync(
        Guid organizationId,
        CreateInvitationRequest request,
        CancellationToken ct = default);

    Task<ApiResult<IReadOnlyList<ProductLocalRoleGrantDto>>> GetProductLocalRolesAsync(
        Guid organizationId,
        string? status = null,
        CancellationToken ct = default);

    Task<ApiResult<ProductLocalRoleGrantDto>> AssignProductLocalRoleAsync(
        Guid organizationId,
        AssignProductLocalRoleRequest request,
        CancellationToken ct = default);

    Task<ApiResult<ProductLocalRoleGrantDto>> RevokeProductLocalRoleAsync(
        Guid organizationId,
        Guid grantId,
        RevokeProductLocalRoleRequest request,
        CancellationToken ct = default);

    Task<ApiResult<PlatformSubscriptionDto>> GetCurrentSubscriptionAsync(
        Guid organizationId,
        string productCode,
        CancellationToken ct = default);

    Task<ApiResult<PlatformEntitlementSnapshotDto>> GetLatestEntitlementAsync(
        Guid organizationId,
        string productCode,
        CancellationToken ct = default);

    Task<ApiResult<object>> SetOrganizationContextAsync(
        SetOrganizationContextRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Local Validation Quick Login directory (anonymous, non-Production). Empty/failure when unavailable.
    /// </summary>
    Task<ApiResult<IReadOnlyList<LocalValidationQuickLoginIdentityDto>>> GetLocalValidationQuickLoginIdentitiesAsync(
        CancellationToken ct = default);
}
