namespace ExItS.Platform.Admin.Models;

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize);

public sealed record ProductDto(
    Guid Id,
    string Code,
    string DisplayName,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record CreateProductRequest(string Code, string DisplayName);
public sealed record CreatePlanRequest(
    string Code,
    string DisplayName,
    string? Description = null,
    int MaxBranches = 1,
    int MaxActiveStaff = 3,
    bool CustomerCreditEnabled = false,
    bool AdvancedReportsEnabled = false,
    bool ExportEnabled = false,
    bool TrialAllowed = true,
    int DefaultTrialDays = 14,
    int SortOrder = 100);

public sealed record UpdatePlanCommercialRequest(
    string DisplayName,
    string? Description,
    int MaxBranches,
    int MaxActiveStaff,
    bool CustomerCreditEnabled,
    bool AdvancedReportsEnabled,
    bool ExportEnabled,
    bool TrialAllowed,
    int DefaultTrialDays,
    int SortOrder,
    DateTimeOffset? ExpectedUpdatedAtUtc = null);
public sealed record RenameCatalogRequest(string DisplayName, DateTimeOffset? ExpectedUpdatedAtUtc);

public sealed record FeatureDefinitionDto(
    string ProductCode,
    string FeatureCode,
    string DisplayName,
    string ValueType,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record FeatureGrantDto(string FeatureCode, bool Enabled, int? NumericLimit);

public sealed record PlanDto(
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
    bool CustomerCreditEnabled = false,
    bool AdvancedReportsEnabled = false,
    bool ExportEnabled = false,
    bool TrialAllowed = true,
    int DefaultTrialDays = 14,
    int SortOrder = 100);

public sealed record PlanVersionDto(
    Guid Id,
    Guid PlanId,
    string ProductCode,
    int VersionNumber,
    DateTimeOffset EffectiveFromUtc,
    DateTimeOffset? EffectiveToUtc,
    string BillingPeriod,
    bool TrialEligible,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    IReadOnlyList<FeatureGrantDto> Grants);

public sealed record TrialDefinitionDto(
    Guid Id,
    string ProductCode,
    Guid? PlanId,
    string DisplayName,
    long DurationTicks,
    string DurationIso,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    IReadOnlyList<FeatureGrantDto> FeatureGrants,
    IReadOnlyList<FeatureGrantDto> PostExpiryFeatureGrants);

public sealed record ProductOverviewDto(
    ProductDto Product,
    IReadOnlyList<FeatureDefinitionDto> Features,
    IReadOnlyList<PlanDto> Plans,
    IReadOnlyList<PlanVersionDto> PublishedPlanVersions,
    IReadOnlyList<TrialDefinitionDto> Trials);

public sealed record OrganizationProfileDto(
    string? LegalName,
    string? ContactEmail,
    string? ContactPhone,
    string? AddressLine1,
    string? AddressLine2,
    string? City,
    string? Region,
    string? PostalCode,
    string? CountryCode,
    string? TimeZoneId,
    string? Locale,
    string? CurrencyCode);

public sealed record OrganizationBrandingDto(
    string? BrandDisplayName,
    string? LogoUrl,
    string? PrimaryColor,
    string? AccentColor);

public sealed record OrganizationDto(
    Guid Id,
    string DisplayName,
    string Slug,
    string Status,
    OrganizationProfileDto? Profile,
    OrganizationBrandingDto? Branding,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record CreateOrganizationRequest(string DisplayName, string Slug);

public sealed record UpdateOrganizationRequest(
    string? DisplayName,
    string? Slug,
    string? LegalName,
    string? ContactEmail,
    string? ContactPhone,
    string? AddressLine1,
    string? AddressLine2,
    string? City,
    string? Region,
    string? PostalCode,
    string? CountryCode,
    string? TimeZoneId,
    string? Locale,
    string? CurrencyCode,
    DateTimeOffset? ExpectedUpdatedAtUtc);

public sealed record UpdateOrganizationBrandingRequest(
    string? BrandDisplayName,
    string? LogoUrl,
    string? PrimaryColor,
    string? AccentColor,
    DateTimeOffset? ExpectedUpdatedAtUtc);

public sealed record SubscriptionDto(
    Guid Id,
    Guid OrganizationId,
    string ProductCode,
    Guid PlanId,
    Guid PlanVersionId,
    Guid? TrialDefinitionId,
    string Status,
    DateTimeOffset? TrialStartUtc,
    DateTimeOffset? TrialEndUtc,
    DateTimeOffset? PaidPeriodStartUtc,
    DateTimeOffset? PaidPeriodEndUtc,
    DateTimeOffset? GracePeriodEndUtc,
    DateTimeOffset? SuspendedAtUtc,
    DateTimeOffset? PastDueAtUtc,
    DateTimeOffset? CancelledAtUtc,
    DateTimeOffset? ExpiredAtUtc,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    int Version,
    string? OrganizationDisplayName = null,
    string? ProductDisplayName = null,
    string? PlanDisplayName = null,
    string? PlanKey = null,
    DateTimeOffset? RenewalDateUtc = null);

public sealed record PaymentDto(
    Guid Id,
    Guid OrganizationId,
    string ProductCode,
    Guid? SubscriptionId,
    decimal Amount,
    string CurrencyCode,
    string Method,
    string ExternalReference,
    string Status,
    DateTimeOffset PaidAtUtc,
    DateTimeOffset? ConfirmedAtUtc,
    string? ConfirmedBy,
    DateTimeOffset? RejectedAtUtc,
    string? RejectedBy,
    string? RejectionReason,
    DateTimeOffset? VoidedAtUtc,
    string? VoidedBy,
    string? VoidReason,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    int Version);

public sealed record EntitlementGrantDto(
    string FeatureCode,
    bool Enabled,
    int? NumericLimit,
    string Source,
    DateTimeOffset EffectiveAtUtc,
    DateTimeOffset? ExpiresAtUtc);

public sealed record EntitlementSnapshotDto(
    Guid Id,
    Guid OrganizationId,
    string ProductCode,
    Guid SubscriptionId,
    string PlanCode,
    int PlanVersionNumber,
    int SnapshotVersion,
    int SchemaVersion,
    string SubscriptionStatus,
    bool InGracePeriod,
    DateTimeOffset GeneratedAtUtc,
    DateTimeOffset EffectiveAtUtc,
    DateTimeOffset RefreshByUtc,
    DateTimeOffset? ExpiresAtUtc,
    int SourceAggregateVersion,
    IReadOnlyList<EntitlementGrantDto> Grants);

public sealed record EntitlementLatestSummaryDto(
    Guid Id,
    Guid OrganizationId,
    string ProductCode,
    Guid SubscriptionId,
    string SubscriptionStatus,
    int SnapshotVersion,
    int SchemaVersion,
    DateTimeOffset GeneratedAtUtc,
    DateTimeOffset EffectiveAtUtc,
    DateTimeOffset RefreshByUtc,
    DateTimeOffset? ExpiresAtUtc,
    bool InGracePeriod);

public sealed record FeatureOverrideDto(
    Guid Id,
    Guid OrganizationId,
    string ProductCode,
    string FeatureCode,
    bool Enabled,
    int? NumericLimit,
    string Reason,
    DateTimeOffset EffectiveFromUtc,
    DateTimeOffset? ExpiresAtUtc,
    string Status,
    DateTimeOffset CreatedAtUtc,
    Guid CreatedByUserId,
    DateTimeOffset UpdatedAtUtc,
    DateTimeOffset? RevokedAtUtc,
    Guid? RevokedByUserId,
    string? RevocationReason);

public sealed record OrganizationCommercialSummaryDto(
    OrganizationDto Organization,
    IReadOnlyList<SubscriptionDto> Subscriptions,
    IReadOnlyList<PaymentDto> Payments,
    IReadOnlyList<EntitlementLatestSummaryDto> LatestEntitlements);

public sealed record PortfolioSummaryDto(
    int ActiveProductCount,
    int PublishedPlanVersionCount,
    int OrganizationCount,
    int TrialingSubscriptionCount,
    int ActiveSubscriptionCount,
    int GracePeriodSubscriptionCount,
    int PastDueSubscriptionCount,
    int SuspendedSubscriptionCount,
    int PendingManualPaymentCount,
    int LatestEntitlementSnapshotCount,
    IReadOnlyList<string> PartialFailures);

public sealed record PlatformUserOrganizationDirectoryItem(
    string Name,
    string Role,
    string RoleDisplay);

public sealed record PlatformUserDto(
    Guid Id,
    string Username,
    string DisplayName,
    string Email,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    DateTimeOffset? SuspendedAtUtc,
    string? SuspensionReason,
    IReadOnlyList<string>? AccountClasses = null,
    IReadOnlyList<string>? OrganizationNames = null,
    IReadOnlyList<PlatformUserOrganizationDirectoryItem>? Organizations = null,
    string? FirstName = null,
    string? LastName = null,
    string? Phone = null,
    string? EmployeeCode = null,
    string? StaffNumber = null,
    Guid? CreatedByUserId = null);

public sealed record PlatformCredentialStatusDto(
    Guid UserId,
    bool HasPassword,
    bool EmailVerified,
    DateTimeOffset? EmailVerifiedAtUtc,
    bool IsLockedOut,
    DateTimeOffset? LockoutEndUtc,
    int FailedAccessCount,
    DateTimeOffset? PasswordChangedAtUtc,
    string? PendingRecoveryEmail = null,
    string? RecoveryEmail = null,
    bool RecoveryEmailVerified = false,
    DateTimeOffset? RecoveryEmailVerifiedAtUtc = null,
    bool NeedsRecoveryEmailPrompt = false);

public sealed record CredentialWorkflowAckDto(
    string Message,
    string? DebugToken,
    DateTimeOffset? ExpiresAtUtc);

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);
public sealed record ForgotPasswordRequest(string UsernameOrEmail);
public sealed record ResetPasswordRequest(string Token, string NewPassword);
public sealed record RegisterPersonalAccountRequest(string DisplayName, string Email);
public sealed record ActivatePersonalAccountRequest(string Token, string Password);
public sealed record PersonalRegistrationAckDto(string Message, string? DebugToken, DateTimeOffset? ExpiresAtUtc);
public sealed record ConfirmEmailVerificationRequest(string Token);
public sealed record RequestRecoveryEmailRequest(string RecoveryEmail);
public sealed record ConfirmRecoveryEmailRequest(string Token);
public sealed record SetUserPasswordRequest(string Password);

public sealed record AuthSessionInfoDto(
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

public sealed record SetOrganizationContextRequest(Guid? OrganizationId);

public sealed record IssueAccessTokenRequest(
    string? GrantType,
    string? UsernameOrEmail,
    string? Password,
    Guid? OrganizationId,
    string? ProductCode);

public sealed record IntrospectAccessTokenRequest(string? Token);

public sealed record AccessTokenIssueDto(
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

public sealed record AccessTokenIntrospectionDto(
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

public sealed record OrganizationMembershipDto(
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
    string? Email = null,
    string? RoleDisplay = null,
    IReadOnlyList<string>? ProductRoles = null,
    string? AccountStatus = null,
    string? EmployeeCode = null,
    string? Branch = null);

public sealed record OrganizationInvitationDto(
    Guid Id,
    Guid OrganizationId,
    string Email,
    string Role,
    string Status,
    Guid? InvitedByUserId,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    DateTimeOffset ExpiresAtUtc,
    DateTimeOffset? AcceptedAtUtc,
    DateTimeOffset? RevokedAtUtc,
    Guid? AcceptedByUserId,
    string? AcceptToken = null,
    string? InvitationType = null,
    string? RoleDisplay = null,
    string? InviteeDisplayName = null,
    string? FirstName = null,
    string? LastName = null,
    string? Branch = null,
    string? ProductRole = null,
    string? ProductRoleDisplay = null,
    string? InvitationStatus = null);

public sealed record CreateInvitationRequest(
    string Email,
    string Role,
    string? FirstName = null,
    string? LastName = null,
    string? DisplayName = null,
    string? Phone = null,
    string? EmployeeCode = null,
    string? Branch = null,
    string? ProductRole = null,
    bool RequireEmailVerification = true);

public sealed record ProductAccessAssignmentDto(
    Guid Id,
    Guid UserId,
    Guid OrganizationId,
    Guid MembershipId,
    string ProductCode,
    string Status,
    DateTimeOffset GrantedAtUtc,
    string GrantedByActor,
    DateTimeOffset? RevokedAtUtc,
    string? RevokedByActor,
    string? Reason,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record EffectiveProductAccessResultDto(
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

public sealed record EnabledProductDto(
    string ProductCode,
    string DisplayName,
    bool EntitlementActive,
    bool ProductAccessAssigned,
    bool ProductLocalRoleGranted,
    bool CanLaunch,
    string? ProductLocalRoleCode,
    string? MappedPosRoleCode,
    string? SubscriptionStatus,
    string ReasonCode,
    Guid? ProductId = null,
    string? ProductKey = null,
    string? ProductDisplayName = null,
    string? EntitlementStatus = null,
    string? ProvisioningStatus = null,
    string? OrganizationRole = null,
    string? ProductRole = null,
    string? DenialReasonCode = null,
    string? DenialReasonDisplay = null);

public sealed record ProductAuthorizationResultDto(
    bool EntitlementAllowed,
    bool ProductAccessAssigned,
    bool ProductLocalRoleGranted,
    bool CanOperate,
    string ReasonCode,
    Guid UserId,
    Guid OrganizationId,
    string ProductCode,
    string? ProductLocalRoleCode,
    string? MappedPosRoleCode,
    Guid? MembershipId,
    Guid? AssignmentId,
    Guid? SubscriptionId,
    Guid? SnapshotId,
    Guid? ProductLocalRoleGrantId,
    DateTimeOffset EvaluatedAtUtc,
    string? SubscriptionStatus = null,
    IReadOnlyList<string>? EnabledFeatureCodes = null);

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
    DateTimeOffset? RevokedAtUtc,
    Guid? RevokedByUserIdentityId,
    string? Reason,
    string? UserDisplayName = null,
    string? ProductDisplayName = null,
    string? RoleDisplay = null,
    string? ProductKey = null);

public sealed record AssignProductLocalRoleRequest(
    Guid UserIdentityId,
    string ProductCode,
    string RoleCode,
    string? Reason);

public sealed record RevokeProductLocalRoleRequest(string? Reason);

public sealed record ProductLaunchResultDto(
    string ProductCode,
    bool CanOperate,
    string? ProductLocalRoleCode,
    string? MappedPosRoleCode,
    string LaunchPath,
    string ReasonCode);

public sealed record CreatePlatformUserRequest(
    string DisplayName,
    string Email,
    string PlatformRole,
    string? Username = null,
    string? FirstName = null,
    string? LastName = null,
    string? Phone = null,
    string? EmployeeCode = null,
    bool SendEmailVerification = false,
    bool RequireEmailVerification = false,
    string? InitialPassword = null,
    Guid? CreatedByUserId = null);
public sealed record LifecycleReasonRequest(
    string? Reason,
    bool Global = false,
    string? ActorPassword = null,
    string? MfaCode = null);
public sealed record ReactivatePlatformUserRequest(
    string? Reason = null,
    string? ActorPassword = null,
    string? MfaCode = null,
    bool Global = false);
public sealed record UpdatePlatformUserRequest(
    string DisplayName,
    string Email,
    string? FirstName = null,
    string? LastName = null,
    string? Phone = null,
    string? EmployeeCode = null);
public sealed record AddMemberRequest(Guid UserId, string Role, string? Reason = null);
public sealed record ChangeRoleRequest(string Role, string? ActorReference);
public sealed record MembershipLifecycleRequest(string? Reason, string? ActorReference);
public sealed record GrantProductAccessRequest(Guid UserId, string ProductCode, string GrantedByActor, string? Reason);
public sealed record RevokeProductAccessRequest(string RevokedByActor, string? Reason);

public sealed record StartTrialRequest(Guid PlanId, Guid PlanVersionId, Guid TrialDefinitionId);
public sealed record CreatePaidSubscriptionRequest(
    Guid PlanId,
    Guid PlanVersionId,
    DateTimeOffset PeriodStartUtc,
    DateTimeOffset PeriodEndUtc);
public sealed record ActivateSubscriptionRequest(
    DateTimeOffset PeriodStartUtc,
    DateTimeOffset PeriodEndUtc,
    int? ExpectedVersion = null);
public sealed record GracePeriodRequest(DateTimeOffset GracePeriodEndUtc, int? ExpectedVersion = null);
public sealed record ReactivateSubscriptionRequest(
    DateTimeOffset? PeriodStartUtc = null,
    DateTimeOffset? PeriodEndUtc = null,
    int? ExpectedVersion = null);
public sealed record SubscriptionLifecycleRequest(int? ExpectedVersion = null);
public sealed record CreateFeatureOverrideRequest(
    string FeatureCode,
    bool Enabled,
    string Reason,
    Guid CreatedByUserId,
    int? NumericLimit = null,
    DateTimeOffset? ExpiresAtUtc = null);
public sealed record RevokeFeatureOverrideRequest(string Reason, Guid RevokedByUserId);
public sealed record GenerateEntitlementSnapshotRequest(int? ExpectedNextVersion = null);
public sealed record ReconcileEntitlementSnapshotRequest(string? Reason = null);

public sealed record CreateManualPaymentRequest(
    Guid OrganizationId,
    string ProductCode,
    decimal Amount,
    string CurrencyCode,
    string Method,
    string ExternalReference,
    DateTimeOffset PaidAtUtc);

public sealed record ConfirmPaymentRequest(string ConfirmedBy);
public sealed record RejectPaymentRequest(string RejectedBy, string Reason);
public sealed record VoidPaymentRequest(string VoidedBy, string Reason);
public sealed record ActivateSubscriptionForPaymentRequest(
    string ConfirmedBy,
    Guid SubscriptionId,
    DateTimeOffset PeriodStartUtc,
    DateTimeOffset PeriodEndUtc);

public sealed record PaymentActivationResultDto(PaymentDto Payment, SubscriptionDto Subscription);

public sealed record AuditRecordDto(
    Guid Id,
    DateTimeOffset OccurredAtUtc,
    string ActorIdentifier,
    string ActorType,
    string ActionCode,
    string TargetType,
    string TargetId,
    Guid? OrganizationId,
    string? ProductCode,
    string? CorrelationId,
    string Outcome,
    string? Reason,
    string? Summary);

public sealed record AuditQuery(
    DateTimeOffset? OccurredFromUtc = null,
    DateTimeOffset? OccurredToUtc = null,
    string? ActorIdentifier = null,
    string? ActionCode = null,
    Guid? OrganizationId = null,
    string? ProductCode = null,
    string? Outcome = null,
    string? CorrelationId = null,
    int Page = 1,
    int PageSize = 25);

public sealed record ResolvedPermissionsDto(
    string ActorIdentifier,
    string ActorType,
    Guid? PlatformUserId,
    Guid? OrganizationId,
    IReadOnlyList<string> Permissions);

public sealed record PlatformRoleAssignmentDto(
    Guid Id,
    Guid PlatformUserId,
    string Role,
    Guid? OrganizationId,
    string Status,
    string GrantedByActor,
    DateTimeOffset GrantedAtUtc,
    string? Reason,
    string? RevokedByActor,
    DateTimeOffset? RevokedAtUtc,
    string? RevokeReason);

public sealed record PlatformRoleCatalogEntryDto(string Role, IReadOnlyList<string> Permissions);

public sealed record PermissionCatalogEntryDto(string Code, string Description, string Area);

public sealed record PlatformRoleDefinitionDto(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    string Kind,
    string Status,
    IReadOnlyList<string> Permissions,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    int Version);

public sealed record PlatformCustomRoleAssignmentDto(
    Guid Id,
    Guid PlatformUserId,
    Guid RoleDefinitionId,
    string Status,
    string GrantedByActor,
    DateTimeOffset GrantedAtUtc,
    string? Reason,
    string? RevokedByActor,
    DateTimeOffset? RevokedAtUtc,
    string? RevokeReason);

public sealed record OrganizationRoleDefinitionDto(
    Guid Id,
    Guid OrganizationId,
    string Code,
    string Name,
    string? Description,
    string Status,
    IReadOnlyList<string> Permissions,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    int Version);

public sealed record OrganizationCustomRoleAssignmentDto(
    Guid Id,
    Guid OrganizationId,
    Guid PlatformUserId,
    Guid RoleDefinitionId,
    string Status,
    string GrantedByActor,
    DateTimeOffset GrantedAtUtc,
    string? Reason,
    string? RevokedByActor,
    DateTimeOffset? RevokedAtUtc,
    string? RevokeReason);

public sealed record EffectivePlatformPermissionsDto(
    Guid PlatformUserId,
    IReadOnlyList<string> SystemRoles,
    IReadOnlyList<string> CustomRoles,
    IReadOnlyList<string> Permissions);

public sealed record EffectiveOrganizationPermissionsDto(
    Guid OrganizationId,
    Guid PlatformUserId,
    string? MembershipRole,
    string? MembershipStatus,
    IReadOnlyList<string> CustomRoles,
    IReadOnlyList<string> Permissions);

public sealed record CreateRoleDefinitionRequest(string Code, string Name, string? Description, IReadOnlyList<string> Permissions);
public sealed record UpdateRoleDefinitionRequest(string Name, string? Description, IReadOnlyList<string>? Permissions, int? ExpectedVersion);
public sealed record RoleLifecycleRequest(int? ExpectedVersion = null, string? Reason = null);
public sealed record AssignSystemRoleRequest(Guid PlatformUserId, string Role, Guid? OrganizationId = null, string? Reason = null);
public sealed record AssignCustomRoleRequest(Guid PlatformUserId, Guid RoleDefinitionId, string? Reason = null);
public sealed record AssignOrgCustomRoleRequest(Guid PlatformUserId, Guid RoleDefinitionId, string? Reason = null);
public sealed record RevokeRoleAssignmentRequest(string? Reason = null);

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
    string Status);

public sealed record PersonalAccountSettingsDto(
    Guid UserIdentityId,
    bool EmailNotificationsEnabled,
    bool PushNotificationsEnabled,
    bool InAppNotificationsEnabled,
    bool ReminderNotificationsEnabled,
    int Version,
    DateTimeOffset UpdatedAtUtc);

public sealed record PersonalContactDto(
    Guid Id,
    string DisplayName,
    string? Phone,
    string? Email,
    Guid? LinkedUserIdentityId,
    string Status,
    DateTimeOffset CreatedAtUtc);

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

public sealed record PersonalInAppNotificationDto(
    Guid Id,
    string Title,
    string Preview,
    string RelatedType,
    string? RelatedId,
    bool IsRead,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ReadAtUtc);
