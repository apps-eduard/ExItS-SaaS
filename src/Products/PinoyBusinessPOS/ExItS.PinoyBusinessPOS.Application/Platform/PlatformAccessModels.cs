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

public sealed record PlatformOrganizationBrandingDto(
    string? BrandDisplayName = null,
    string? LogoUrl = null,
    string? PrimaryColor = null,
    string? AccentColor = null);

public sealed record PlatformOrganizationDto(
    Guid Id,
    string DisplayName,
    string Slug,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    PlatformOrganizationBrandingDto? Branding = null,
    Guid? PrimaryBusinessTypeId = null);

public sealed record OrganizationBranchDto(Guid Id, Guid OrganizationId, string Code, string Name,
    bool IsPrimary, string Status, string? AddressLine1 = null, string? AddressLine2 = null,
    string? City = null, string? Region = null, string? PostalCode = null, string? CountryCode = null);
public sealed record BranchCapacityDto(int Used, int Allowed);
public sealed record CreateBranchRequest(string Code, string Name, string? AddressLine1 = null, string? AddressLine2 = null,
    string? City = null, string? Region = null, string? PostalCode = null, string? CountryCode = null);
public sealed record PosDeviceDto(Guid Id, Guid OrganizationId, Guid BranchId, string InstallationDeviceId, string FriendlyName,
    string? Platform, string? Model, string? AppVersion, string Status, DateTimeOffset RegisteredAtUtc, DateTimeOffset LastSeenAtUtc,
    DateTimeOffset? RevokedAtUtc = null);
public sealed record PosDeviceCapacityDto(int Used, int Allowed);
public sealed record RegisterPosDeviceRequest(Guid BranchId, string InstallationDeviceId, string FriendlyName,
    string? Platform = null, string? Model = null, string? AppVersion = null);
public sealed record AuthorizePosDeviceRequest(string InstallationDeviceId, Guid? BranchId = null);
public sealed record PosDeviceAuthorizationDto(Guid PosDeviceId, Guid BranchId, string InstallationDeviceId);

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
    string? ActorReference,
    string? Username = null,
    string? DisplayName = null,
    string? Email = null);

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

public sealed record ForgotPasswordRequest(string UsernameOrEmail);

public sealed record CredentialWorkflowAckDto(
    string Message,
    string? DebugToken,
    DateTimeOffset? ExpiresAtUtc);

/// <summary>GET /api/v1/platform/auth/me — session identity without re-issuing the opaque token.</summary>
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
    Guid? AccountProfileId = null,
    string? AccountClass = null,
    string? AllowedScope = null);

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
    string? AllowedScope = null,
    Guid? HomeOrganizationId = null,
    bool OrganizationContextLocked = false);

public sealed record SelectAccountProfileRequest(Guid AccountProfileId);

public sealed record StartBusinessRequest(
    string DisplayName,
    string Slug,
    Guid PrimaryBusinessTypeId,
    string? ProductCode = null,
    string? PlanKey = null,
    string? BillingCycle = null,
    bool StartAsTrial = true,
    bool PayNow = false,
    bool ActivatePosEntitlement = true,
    bool ActivateProductAccess = true,
    bool AssignPosOwnerRole = true);

/// <summary>Active commercial plan from GET /api/v1/commercial/plans (authoritative Platform catalog).</summary>
public sealed record CommercialPlanDto(
    Guid Id,
    string ProductCode,
    string Code,
    string DisplayName,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    Guid? ProductId = null,
    string? ProductDisplayName = null,
    string? PlanKey = null,
    string? Description = null,
    int MaxBranches = 1,
    int MaxActiveStaff = 3,
    int MaxActivePosDevices = 1,
    bool CustomerCreditEnabled = false,
    bool AdvancedReportsEnabled = false,
    bool ExportEnabled = false,
    bool TrialAllowed = true,
    int DefaultTrialDays = 14,
    int SortOrder = 100,
    decimal MonthlyPrice = 0m,
    decimal AnnualPrice = 0m,
    string CurrencyCode = "PHP");

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
    string ProductCode,
    Guid? PrimaryBusinessTypeId = null,
    Guid? PrimaryBranchId = null);

public sealed record BusinessTypeDto(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    string Status,
    int SortOrder);

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

public sealed record PlatformAccountProfileDto(
    Guid Id,
    Guid UserIdentityId,
    string AccountClass,
    string AllowedScope,
    string Status);

public sealed record PendingOrganizationInvitationDto(
    Guid Id,
    Guid OrganizationId,
    string OrganizationDisplayName,
    string Role,
    string? ProductRole,
    DateTimeOffset ExpiresAtUtc,
    string Status);

public sealed record AcceptOrganizationInvitationRequest(string Token, string Password);

public sealed record AcceptOrganizationInvitationResultDto(
    Guid UserId,
    string StaffLogin,
    string ContactEmail,
    string OrganizationDisplayName,
    Guid OrganizationId,
    Guid MembershipId,
    string Role);

public sealed record PersonalDashboardDto(
    Guid UserIdentityId,
    Guid AccountProfileId,
    string AccountClass,
    bool UtangAvailable,
    int ContactCount,
    int ActiveRelationshipCount,
    decimal TotalLentBalance,
    decimal TotalBorrowedBalance);

public sealed record PersonalProfileDto(
    Guid UserIdentityId,
    Guid AccountProfileId,
    string Username,
    string DisplayName,
    string Email,
    string AccountClass,
    string Status,
    string? PublicUserId = null,
    string? QrPayload = null);

public sealed record PublicIdentityDto(
    string PublicUserId,
    string QrPayload,
    string DisplayName,
    string Status);

public sealed record ResolvePublicUserIdRequest(
    string PublicUserIdOrQrPayload,
    string? Purpose = null);

public sealed record ResolvedPublicUserDto(
    string PublicUserId,
    Guid UserIdentityId,
    string DisplayName,
    string? MaskedEmail,
    string Status,
    bool IsSelf);

public sealed record PersonalAccountSettingsDto(
    Guid UserIdentityId,
    bool EmailNotificationsEnabled,
    bool PushNotificationsEnabled,
    bool InAppNotificationsEnabled,
    bool ReminderNotificationsEnabled,
    int Version,
    DateTimeOffset UpdatedAtUtc);

public sealed record UpdatePersonalAccountSettingsRequest(
    bool EmailNotificationsEnabled,
    bool PushNotificationsEnabled,
    bool InAppNotificationsEnabled,
    bool ReminderNotificationsEnabled,
    int? ExpectedVersion);

public sealed record PersonalContactDto(
    Guid Id,
    string DisplayName,
    string? Phone,
    string? Email,
    Guid? LinkedUserIdentityId,
    string Status,
    DateTimeOffset CreatedAtUtc);

public sealed record CreatePersonalContactRequest(string DisplayName, string? Phone, string? Email);

public sealed record PersonalDebtRelationshipSummaryDto(
    Guid Id,
    string Perspective,
    Guid? CreditorUserIdentityId,
    Guid? CreditorContactId,
    Guid? DebtorUserIdentityId,
    Guid? DebtorContactId,
    string CurrencyCode,
    decimal CurrentBalance,
    DateTimeOffset? DueDateUtc,
    string Status,
    int Version,
    DateTimeOffset UpdatedAtUtc);

public sealed record CreatePersonalDebtRelationshipRequest(
    Guid? CreditorUserIdentityId,
    Guid? CreditorContactId,
    Guid? DebtorUserIdentityId,
    Guid? DebtorContactId,
    string? CurrencyCode,
    DateTimeOffset? DueDateUtc,
    decimal? InitialLoanAmount,
    string? InitialLoanNotes);

public sealed record RecordPersonalUtangEntryRequest(
    string EntryType,
    decimal Amount,
    decimal? AdjustmentDelta,
    int? ExpectedVersion,
    string? Notes,
    DateTimeOffset? DueDateUtc);

public sealed record PersonalUtangEntryDto(
    Guid Id,
    Guid RelationshipId,
    string EntryType,
    decimal Amount,
    decimal SignedDelta,
    decimal BalanceAfter,
    string? Notes,
    DateTimeOffset? DueDateUtc,
    Guid CreatedByUserIdentityId,
    DateTimeOffset CreatedAtUtc);

public sealed record PersonalUtangBalanceDto(
    Guid RelationshipId,
    decimal CurrentBalance,
    string CurrencyCode,
    int Version,
    DateTimeOffset UpdatedAtUtc);

public sealed record PersonalUtangInvitationDto(
    Guid Id,
    Guid DebtRelationshipId,
    Guid InviteeContactId,
    Guid InvitedByUserIdentityId,
    string? InviteTargetEmailMasked,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    DateTimeOffset ExpiresAtUtc,
    DateTimeOffset? AcceptedAtUtc,
    DateTimeOffset? DeclinedAtUtc,
    DateTimeOffset? RevokedAtUtc,
    Guid? AcceptedByUserIdentityId,
    string? AcceptToken);

public sealed record CreatePersonalUtangInvitationRequest(Guid InviteeContactId);

public sealed record AcceptPersonalUtangInvitationRequest(string Token);

public sealed record PersonalUtangInvitationAcceptResultDto(
    Guid InvitationId,
    Guid DebtRelationshipId,
    Guid LinkedContactId,
    Guid LinkedUserIdentityId,
    bool CreatedOrganizationMembership,
    bool GrantedProductRole);

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
    Task<ApiResult<IReadOnlyList<OrganizationBranchDto>>> GetBranchesAsync(Guid organizationId, CancellationToken ct = default);
    Task<ApiResult<BranchCapacityDto>> GetBranchCapacityAsync(Guid organizationId, CancellationToken ct = default);
    Task<ApiResult<OrganizationBranchDto>> CreateBranchAsync(Guid organizationId, CreateBranchRequest request, CancellationToken ct = default);
    Task<ApiResult<IReadOnlyList<PosDeviceDto>>> GetPosDevicesAsync(Guid organizationId, CancellationToken ct = default);
    Task<ApiResult<PosDeviceCapacityDto>> GetPosDeviceCapacityAsync(Guid organizationId, CancellationToken ct = default);
    Task<ApiResult<PosDeviceDto>> RegisterCurrentDeviceAsync(Guid organizationId, RegisterPosDeviceRequest request, CancellationToken ct = default);
    Task<ApiResult<PosDeviceDto>> RenamePosDeviceAsync(Guid organizationId, Guid deviceId, string friendlyName, CancellationToken ct = default);
    Task<ApiResult<PosDeviceDto>> RevokePosDeviceAsync(Guid organizationId, Guid deviceId, CancellationToken ct = default);
    Task<ApiResult<PosDeviceAuthorizationDto>> AuthorizePosDeviceAsync(Guid organizationId, AuthorizePosDeviceRequest request, CancellationToken ct = default);
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

    Task<ApiResult<PlatformAuthSessionInfoDto>> GetAuthMeAsync(CancellationToken ct = default);

    Task<ApiResult<CredentialWorkflowAckDto>> ForgotPasswordAsync(
        ForgotPasswordRequest request,
        CancellationToken ct = default);

    Task<ApiResult<object>> LogoutSessionAsync(CancellationToken ct = default);

    Task<ApiResult<PlatformLoginResultDto>> SelectAccountProfileAsync(
        SelectAccountProfileRequest request,
        CancellationToken ct = default);

    Task<ApiResult<StartBusinessResultDto>> StartBusinessAsync(
        StartBusinessRequest request,
        CancellationToken ct = default);

    Task<ApiResult<IReadOnlyList<BusinessTypeDto>>> GetActiveBusinessTypesAsync(
        CancellationToken ct = default);

    Task<ApiResult<IReadOnlyList<CommercialPlanDto>>> GetCommercialPlansAsync(
        string? productCode = null,
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

    Task<ApiResult<IReadOnlyList<PlatformAccountProfileDto>>> GetAccountProfilesAsync(
        CancellationToken ct = default);

    Task<ApiResult<IReadOnlyList<PendingOrganizationInvitationDto>>> GetPendingOrganizationInvitationsAsync(
        CancellationToken ct = default);

    Task<ApiResult<AcceptOrganizationInvitationResultDto>> AcceptOrganizationInvitationAsync(
        string token,
        string password,
        CancellationToken ct = default);

    Task<ApiResult<PlatformMembershipDto>> AcceptOrganizationInvitationByIdAsync(
        Guid invitationId,
        CancellationToken ct = default);

    Task<ApiResult<PersonalDashboardDto>> GetPersonalDashboardAsync(CancellationToken ct = default);
    Task<ApiResult<PersonalProfileDto>> GetPersonalProfileAsync(CancellationToken ct = default);
    Task<ApiResult<PublicIdentityDto>> GetMyPublicIdentityAsync(CancellationToken ct = default);
    Task<ApiResult<ResolvedPublicUserDto>> ResolvePublicUserIdAsync(
        ResolvePublicUserIdRequest request,
        CancellationToken ct = default);
    Task<ApiResult<PersonalAccountSettingsDto>> GetPersonalSettingsAsync(CancellationToken ct = default);
    Task<ApiResult<PersonalAccountSettingsDto>> UpdatePersonalSettingsAsync(
        UpdatePersonalAccountSettingsRequest request,
        CancellationToken ct = default);
    Task<ApiResult<IReadOnlyList<PersonalContactDto>>> GetPersonalContactsAsync(CancellationToken ct = default);
    Task<ApiResult<PersonalContactDto>> CreatePersonalContactAsync(
        CreatePersonalContactRequest request,
        CancellationToken ct = default);
    Task<ApiResult<IReadOnlyList<PersonalDebtRelationshipSummaryDto>>> GetPersonalUtangLentAsync(
        CancellationToken ct = default);
    Task<ApiResult<IReadOnlyList<PersonalDebtRelationshipSummaryDto>>> GetPersonalUtangBorrowedAsync(
        CancellationToken ct = default);
    Task<ApiResult<PersonalDebtRelationshipSummaryDto>> CreatePersonalDebtRelationshipAsync(
        CreatePersonalDebtRelationshipRequest request,
        CancellationToken ct = default);
    Task<ApiResult<PersonalDebtRelationshipSummaryDto>> GetPersonalDebtRelationshipAsync(
        Guid relationshipId,
        CancellationToken ct = default);
    Task<ApiResult<PersonalUtangBalanceDto>> GetPersonalUtangBalanceAsync(
        Guid relationshipId,
        CancellationToken ct = default);
    Task<ApiResult<IReadOnlyList<PersonalUtangEntryDto>>> GetPersonalUtangHistoryAsync(
        Guid relationshipId,
        CancellationToken ct = default);
    Task<ApiResult<PersonalUtangEntryDto>> RecordPersonalUtangEntryAsync(
        Guid relationshipId,
        RecordPersonalUtangEntryRequest request,
        CancellationToken ct = default);
    Task<ApiResult<IReadOnlyList<PersonalUtangInvitationDto>>> GetPersonalUtangInvitationsAsync(
        CancellationToken ct = default);
    Task<ApiResult<PersonalUtangInvitationDto>> CreatePersonalUtangInvitationAsync(
        Guid relationshipId,
        CreatePersonalUtangInvitationRequest request,
        CancellationToken ct = default);
    Task<ApiResult<PersonalUtangInvitationAcceptResultDto>> AcceptPersonalUtangInvitationAsync(
        string token,
        CancellationToken ct = default);
    Task<ApiResult<PersonalUtangInvitationDto>> DeclinePersonalUtangInvitationAsync(
        string token,
        CancellationToken ct = default);

    /// <summary>
    /// Local Validation Quick Login directory (anonymous, non-Production). Empty/failure when unavailable.
    /// </summary>
    Task<ApiResult<IReadOnlyList<LocalValidationQuickLoginIdentityDto>>> GetLocalValidationQuickLoginIdentitiesAsync(
        CancellationToken ct = default);
}
