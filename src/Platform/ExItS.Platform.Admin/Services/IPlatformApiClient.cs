using ExItS.Platform.Admin.Models;

namespace ExItS.Platform.Admin.Services;

public interface IPlatformApiClient
{
    Task<ApiCallResult<PortfolioSummaryDto>> GetPortfolioSummaryAsync(CancellationToken ct = default);
    Task<ApiCallResult<PagedResult<ProductDto>>> GetProductsAsync(
        int page = 1,
        int pageSize = 20,
        string? status = null,
        string? search = null,
        string? sortBy = null,
        bool? sortDesc = null,
        CancellationToken ct = default);
    Task<ApiCallResult<ProductDto>> GetProductAsync(Guid id, CancellationToken ct = default);
    Task<ApiCallResult<ProductDto>> CreateProductAsync(CreateProductRequest request, CancellationToken ct = default);
    Task<ApiCallResult<ProductDto>> RenameProductAsync(Guid id, RenameCatalogRequest request, CancellationToken ct = default);
    Task<ApiCallResult<ProductDto>> ActivateProductAsync(Guid id, CancellationToken ct = default);
    Task<ApiCallResult<ProductDto>> DeactivateProductAsync(Guid id, CancellationToken ct = default);
    Task<ApiCallResult<ProductDto>> RetireProductAsync(Guid id, CancellationToken ct = default);
    Task<ApiCallResult<ProductOverviewDto>> GetProductOverviewAsync(string productCode, CancellationToken ct = default);
    Task<ApiCallResult<PagedResult<PlanDto>>> GetPlansAsync(
        int page = 1,
        int pageSize = 20,
        string? productCode = null,
        string? status = null,
        string? search = null,
        string? sortBy = null,
        bool? sortDesc = null,
        CancellationToken ct = default);
    Task<ApiCallResult<IReadOnlyList<PlanDto>>> GetCommercialPlansAsync(string? productCode = null, CancellationToken ct = default);
    Task<ApiCallResult<PlanDto>> GetPlanAsync(Guid id, CancellationToken ct = default);
    Task<ApiCallResult<PlanDto>> CreatePlanAsync(string productCode, CreatePlanRequest request, CancellationToken ct = default);
    Task<ApiCallResult<PlanDto>> RenamePlanAsync(string productCode, Guid planId, RenameCatalogRequest request, CancellationToken ct = default);
    Task<ApiCallResult<PlanDto>> UpdatePlanCommercialAsync(string productCode, Guid planId, UpdatePlanCommercialRequest request, CancellationToken ct = default);
    Task<ApiCallResult<PlanDto>> ActivatePlanAsync(string productCode, Guid planId, CancellationToken ct = default);
    Task<ApiCallResult<PlanDto>> DeactivatePlanAsync(string productCode, Guid planId, CancellationToken ct = default);
    Task<ApiCallResult<PlanDto>> RetirePlanAsync(string productCode, Guid planId, CancellationToken ct = default);
    Task<ApiCallResult<IReadOnlyList<PlanVersionDto>>> GetPlanVersionsAsync(string productCode, Guid planId, CancellationToken ct = default);

    Task<ApiCallResult<IReadOnlyList<PersonalFeatureDefinitionDto>>> GetPersonalFeatureDefinitionsAsync(
        CancellationToken ct = default);
    Task<ApiCallResult<PersonalFeatureDefinitionDto>> GetPersonalFeatureDefinitionAsync(
        string featureCode,
        CancellationToken ct = default);
    Task<ApiCallResult<PersonalFeatureDefinitionDto>> UpdatePersonalFeatureDefinitionAsync(
        string featureCode,
        UpdatePersonalFeatureDefinitionRequest request,
        CancellationToken ct = default);

    Task<ApiCallResult<PagedResult<OrganizationDto>>> GetOrganizationsAsync(
        int page = 1,
        int pageSize = 20,
        string? status = null,
        string? search = null,
        string? sortBy = null,
        bool? sortDesc = null,
        CancellationToken ct = default);
    Task<ApiCallResult<OrganizationDto>> GetOrganizationAsync(Guid id, CancellationToken ct = default);
    Task<ApiCallResult<OrganizationCatalogVisibilityDto>> GetOrganizationCatalogAsync(
        Guid organizationId,
        int page = 1,
        int pageSize = 20,
        string? search = null,
        CancellationToken ct = default);
    Task<ApiCallResult<OrganizationDto>> CreateOrganizationAsync(CreateOrganizationRequest request, CancellationToken ct = default);
    Task<ApiCallResult<OrganizationDto>> UpdateOrganizationAsync(Guid id, UpdateOrganizationRequest request, CancellationToken ct = default);
    Task<ApiCallResult<OrganizationDto>> UpdateOrganizationBrandingAsync(Guid id, UpdateOrganizationBrandingRequest request, CancellationToken ct = default);
    Task<ApiCallResult<OrganizationDto>> SuspendOrganizationAsync(Guid id, CancellationToken ct = default);
    Task<ApiCallResult<OrganizationDto>> ReactivateOrganizationAsync(Guid id, CancellationToken ct = default);
    Task<ApiCallResult<OrganizationDto>> CloseOrganizationAsync(Guid id, CancellationToken ct = default);
    Task<ApiCallResult<OrganizationComplianceStatusDto>> GetOrganizationComplianceStatusAsync(Guid organizationId, CancellationToken ct = default);
    Task<ApiCallResult<OrganizationComplianceStatusDto>> TransitionOrganizationComplianceAsync(Guid organizationId, ComplianceTransitionRequest request, CancellationToken ct = default);
    Task<ApiCallResult<OrganizationComplianceStatusDto>> SetOrganizationTaxDocumentCapabilityAsync(Guid organizationId, TaxDocumentCapabilityRequest request, CancellationToken ct = default);
    Task<ApiCallResult<OrganizationComplianceStatusDto>> SetOrganizationTaxConfigurationCapabilityAsync(Guid organizationId, TaxConfigurationCapabilityRequest request, CancellationToken ct = default);
    Task<ApiCallResult<OrganizationCommercialSummaryDto>> GetOrganizationCommercialSummaryAsync(Guid id, CancellationToken ct = default);
    Task<ApiCallResult<OrganizationCurrentPlanDto>> GetOrganizationCurrentPlanAsync(Guid organizationId, string? productCode = null, CancellationToken ct = default);
    Task<ApiCallResult<PagedResult<SubscriptionDto>>> GetSubscriptionsAsync(
        string? status = null,
        string? productCode = null,
        Guid? organizationId = null,
        string? search = null,
        bool? isTrial = null,
        Guid? planId = null,
        string? sortBy = null,
        bool sortDesc = true,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default);
    Task<ApiCallResult<SubscriptionDto>> GetSubscriptionAsync(Guid id, CancellationToken ct = default);
    Task<ApiCallResult<PagedResult<PaymentDto>>> GetPaymentsAsync(string? status = null, string? productCode = null, Guid? organizationId = null, string? method = null, int page = 1, int pageSize = 20, CancellationToken ct = default);
    Task<ApiCallResult<PaymentDto>> GetPaymentAsync(Guid id, CancellationToken ct = default);
    Task<ApiCallResult<PagedResult<EntitlementLatestSummaryDto>>> GetLatestEntitlementsAsync(
        int page = 1,
        int pageSize = 20,
        string? sortBy = null,
        bool? sortDesc = null,
        CancellationToken ct = default);
    Task<ApiCallResult<PagedResult<EntitlementSnapshotDto>>> GetEntitlementHistoryAsync(Guid organizationId, string productCode, CancellationToken ct = default);
    Task<ApiCallResult<EntitlementSnapshotDto>> GetLatestEntitlementAsync(Guid organizationId, string productCode, CancellationToken ct = default);
    Task<ApiCallResult<EntitlementSnapshotDto>> GetEntitlementAsync(Guid id, CancellationToken ct = default);
    Task<ApiCallResult<PagedResult<FeatureOverrideDto>>> GetFeatureOverridesAsync(Guid organizationId, string productCode, CancellationToken ct = default);
    Task<ApiCallResult<EntitlementSnapshotDto>> GenerateEntitlementSnapshotAsync(Guid organizationId, string productCode, int? expectedNextVersion = null, CancellationToken ct = default);
    Task<ApiCallResult<EntitlementSnapshotDto>> ReconcileEntitlementSnapshotAsync(Guid organizationId, string productCode, string? reason = null, CancellationToken ct = default);
    Task<ApiCallResult<FeatureOverrideDto>> CreateFeatureOverrideAsync(Guid organizationId, string productCode, CreateFeatureOverrideRequest request, CancellationToken ct = default);
    Task<ApiCallResult<FeatureOverrideDto>> RevokeFeatureOverrideAsync(Guid overrideId, RevokeFeatureOverrideRequest request, CancellationToken ct = default);

    Task<ApiCallResult<PagedResult<PlatformUserDto>>> GetUsersAsync(int page = 1, int pageSize = 20, string? status = null, string? search = null, string? directory = null, string? sortBy = null, bool? sortDesc = null, CancellationToken ct = default);
    Task<ApiCallResult<PlatformUserDto>> GetUserAsync(Guid id, CancellationToken ct = default);
    Task<ApiCallResult<PlatformUserDto>> CreateUserAsync(CreatePlatformUserRequest request, CancellationToken ct = default);
    Task<ApiCallResult<PlatformUserDto>> UpdateUserAsync(Guid id, UpdatePlatformUserRequest request, CancellationToken ct = default);
    Task<ApiCallResult<PlatformUserDto>> SuspendUserAsync(Guid id, string? reason = null, bool global = false, CancellationToken ct = default);
    Task<ApiCallResult<PlatformUserDto>> ReactivateUserAsync(Guid id, ReactivatePlatformUserRequest? request = null, CancellationToken ct = default);
    Task<ApiCallResult<PlatformUserDto>> DeactivateUserAsync(
        Guid id,
        string reason,
        string? actorPassword = null,
        string? mfaCode = null,
        CancellationToken ct = default);
    Task<ApiCallResult<PlatformUserDto>> MoveUserToSuspendedAsync(
        Guid id,
        string reason,
        string? actorPassword = null,
        string? mfaCode = null,
        CancellationToken ct = default);
    Task<ApiCallResult<PlatformUserDto>> DisableUserAsync(Guid id, string reason, CancellationToken ct = default);

    Task<ApiCallResult<PagedResult<OrganizationMembershipDto>>> GetOrganizationMembersAsync(Guid organizationId, int page = 1, int pageSize = 20, string? status = null, CancellationToken ct = default);
    Task<ApiCallResult<PagedResult<OrganizationMembershipDto>>> GetUserMembershipsAsync(Guid userId, int page = 1, int pageSize = 20, string? status = null, CancellationToken ct = default);
    Task<ApiCallResult<OrganizationMembershipDto>> AddOrganizationMemberAsync(Guid organizationId, AddMemberRequest request, CancellationToken ct = default);
    Task<ApiCallResult<OrganizationMembershipDto>> ChangeMembershipRoleAsync(Guid membershipId, ChangeRoleRequest request, CancellationToken ct = default);
    Task<ApiCallResult<OrganizationMembershipDto>> SuspendMembershipAsync(Guid membershipId, MembershipLifecycleRequest request, CancellationToken ct = default);
    Task<ApiCallResult<OrganizationMembershipDto>> ReactivateMembershipAsync(Guid membershipId, MembershipLifecycleRequest request, CancellationToken ct = default);
    Task<ApiCallResult<OrganizationMembershipDto>> RevokeMembershipAsync(Guid membershipId, MembershipLifecycleRequest request, CancellationToken ct = default);

    Task<ApiCallResult<PagedResult<OrganizationInvitationDto>>> GetOrganizationInvitationsAsync(Guid organizationId, int page = 1, int pageSize = 20, string? status = null, CancellationToken ct = default);
    Task<ApiCallResult<OrganizationInvitationDto>> CreateOrganizationInvitationAsync(Guid organizationId, CreateInvitationRequest request, CancellationToken ct = default);
    Task<ApiCallResult<OrganizationInvitationDto>> ResendOrganizationInvitationAsync(Guid invitationId, CancellationToken ct = default);
    Task<ApiCallResult<OrganizationInvitationDto>> RevokeOrganizationInvitationAsync(Guid invitationId, CancellationToken ct = default);
    Task<ApiCallResult<AcceptOrganizationInvitationResultDto>> AcceptOrganizationInvitationAsync(
        string token,
        string password,
        string? displayName = null,
        string? firstName = null,
        string? lastName = null,
        CancellationToken ct = default);

    Task<ApiCallResult<PagedResult<ProductAccessAssignmentDto>>> GetOrganizationProductAccessAsync(Guid organizationId, int page = 1, int pageSize = 20, string? status = null, CancellationToken ct = default);
    Task<ApiCallResult<PagedResult<ProductAccessAssignmentDto>>> GetUserProductAccessAsync(Guid userId, int page = 1, int pageSize = 20, string? status = null, CancellationToken ct = default);
    Task<ApiCallResult<ProductAccessAssignmentDto>> GrantProductAccessAsync(Guid organizationId, GrantProductAccessRequest request, CancellationToken ct = default);
    Task<ApiCallResult<ProductAccessAssignmentDto>> RevokeProductAccessAsync(Guid assignmentId, RevokeProductAccessRequest request, CancellationToken ct = default);
    Task<ApiCallResult<EffectiveProductAccessResultDto>> EvaluateAccessAsync(Guid userId, Guid organizationId, string productCode, CancellationToken ct = default);
    Task<ApiCallResult<IReadOnlyList<EnabledProductDto>>> GetEnabledProductsAsync(Guid organizationId, CancellationToken ct = default);
    Task<ApiCallResult<ProductAuthorizationResultDto>> EvaluateProductAuthorizationAsync(Guid organizationId, string productCode, Guid? userId = null, CancellationToken ct = default);
    Task<ApiCallResult<IReadOnlyList<ProductLocalRoleGrantDto>>> GetProductLocalRolesAsync(Guid organizationId, string? status = null, CancellationToken ct = default);
    Task<ApiCallResult<ProductLocalRoleGrantDto>> AssignProductLocalRoleAsync(Guid organizationId, AssignProductLocalRoleRequest request, CancellationToken ct = default);
    Task<ApiCallResult<ProductLocalRoleGrantDto>> RevokeProductLocalRoleAsync(Guid organizationId, Guid grantId, RevokeProductLocalRoleRequest request, CancellationToken ct = default);
    Task<ApiCallResult<ProductLaunchResultDto>> LaunchProductAsync(Guid organizationId, string productCode, CancellationToken ct = default);

    Task<ApiCallResult<PagedResult<SubscriptionDto>>> GetOrganizationSubscriptionsAsync(Guid organizationId, string? status = null, int page = 1, int pageSize = 20, CancellationToken ct = default);
    Task<ApiCallResult<SubscriptionDto>> GetCurrentSubscriptionAsync(Guid organizationId, string productCode, CancellationToken ct = default);
    Task<ApiCallResult<SubscriptionDto>> StartTrialAsync(Guid organizationId, StartTrialRequest request, CancellationToken ct = default);
    Task<ApiCallResult<SubscriptionDto>> CreatePaidSubscriptionAsync(Guid organizationId, CreatePaidSubscriptionRequest request, CancellationToken ct = default);
    Task<ApiCallResult<SubscriptionDto>> ActivateSubscriptionAsync(Guid subscriptionId, ActivateSubscriptionRequest request, CancellationToken ct = default);
    Task<ApiCallResult<SubscriptionDto>> EnterGracePeriodAsync(Guid subscriptionId, GracePeriodRequest request, CancellationToken ct = default);
    Task<ApiCallResult<SubscriptionDto>> MarkPastDueAsync(Guid subscriptionId, int? expectedVersion = null, CancellationToken ct = default);
    Task<ApiCallResult<SubscriptionDto>> SuspendSubscriptionAsync(Guid subscriptionId, int? expectedVersion = null, CancellationToken ct = default);
    Task<ApiCallResult<SubscriptionDto>> ReactivateSubscriptionAsync(Guid subscriptionId, ReactivateSubscriptionRequest request, CancellationToken ct = default);
    Task<ApiCallResult<SubscriptionDto>> CancelSubscriptionAsync(Guid subscriptionId, int? expectedVersion = null, CancellationToken ct = default);
    Task<ApiCallResult<SubscriptionDto>> ExpireSubscriptionAsync(Guid subscriptionId, int? expectedVersion = null, CancellationToken ct = default);
    Task<ApiCallResult<SubscriptionDto>> UpgradeSubscriptionAsync(Guid organizationId, Guid subscriptionId, UpgradeSubscriptionRequest request, CancellationToken ct = default);
    Task<ApiCallResult<SubscriptionDto>> DowngradeSubscriptionAsync(Guid organizationId, Guid subscriptionId, DowngradeSubscriptionRequest request, CancellationToken ct = default);
    Task<ApiCallResult<SubscriptionDto>> ConvertTrialSubscriptionAsync(Guid organizationId, Guid subscriptionId, ConvertTrialSubscriptionRequest request, CancellationToken ct = default);
    Task<ApiCallResult<SubscriptionDto>> StartOrganizationCommercialSubscriptionAsync(Guid organizationId, StartOrganizationCommercialRequest request, CancellationToken ct = default);
    Task<ApiCallResult<PlanChangeImpactPreviewDto>> PreviewPlanChangeAsync(Guid organizationId, Guid subscriptionId, Guid? planId = null, string? planKey = null, int? activeBranchCount = null, CancellationToken ct = default);
    Task<ApiCallResult<SubscriptionDto>> ApplyPendingPlanChangeAsync(Guid organizationId, Guid subscriptionId, CancellationToken ct = default);

    Task<ApiCallResult<SimulateLocalValidationPaymentResultDto>> SimulateLocalValidationPaymentAsync(SimulateLocalValidationPaymentRequest request, CancellationToken ct = default);
    Task<ApiCallResult<StartBusinessResultDto>> StartBusinessAsync(StartBusinessRequest request, CancellationToken ct = default);
    Task<ApiCallResult<bool>> GetLocalValidationEnabledAsync(CancellationToken ct = default);

    Task<ApiCallResult<PaymentDto>> CreateManualPaymentAsync(CreateManualPaymentRequest request, CancellationToken ct = default);
    Task<ApiCallResult<PaymentDto>> ConfirmPaymentAsync(Guid paymentId, ConfirmPaymentRequest request, CancellationToken ct = default);
    Task<ApiCallResult<PaymentDto>> RejectPaymentAsync(Guid paymentId, RejectPaymentRequest request, CancellationToken ct = default);
    Task<ApiCallResult<PaymentDto>> VoidPaymentAsync(Guid paymentId, VoidPaymentRequest request, CancellationToken ct = default);
    Task<ApiCallResult<PaymentActivationResultDto>> ConfirmPaymentAndActivateAsync(Guid paymentId, ActivateSubscriptionForPaymentRequest request, CancellationToken ct = default);

    Task<ApiCallResult<PagedResult<AuditRecordDto>>> GetAuditRecordsAsync(AuditQuery query, CancellationToken ct = default);
    Task<ApiCallResult<AuditRecordDto>> GetAuditRecordAsync(Guid id, CancellationToken ct = default);
    Task<ApiCallResult<ResolvedPermissionsDto>> GetMyAuthorizationAsync(Guid? organizationId = null, CancellationToken ct = default);
    Task<ApiCallResult<IReadOnlyList<PlatformRoleCatalogEntryDto>>> GetAuthorizationRolesAsync(CancellationToken ct = default);
    Task<ApiCallResult<IReadOnlyList<PermissionCatalogEntryDto>>> GetPlatformPermissionsAsync(CancellationToken ct = default);
    Task<ApiCallResult<IReadOnlyList<PermissionCatalogEntryDto>>> GetOrganizationPermissionsCatalogAsync(CancellationToken ct = default);
    Task<ApiCallResult<PagedResult<PlatformRoleDefinitionDto>>> GetPlatformRoleDefinitionsAsync(int page = 1, int pageSize = 20, string? kind = null, string? status = null, string? search = null, CancellationToken ct = default);
    Task<ApiCallResult<PlatformRoleDefinitionDto>> GetPlatformRoleDefinitionAsync(Guid id, CancellationToken ct = default);
    Task<ApiCallResult<PlatformRoleDefinitionDto>> CreatePlatformRoleDefinitionAsync(CreateRoleDefinitionRequest request, CancellationToken ct = default);
    Task<ApiCallResult<PlatformRoleDefinitionDto>> UpdatePlatformRoleDefinitionAsync(Guid id, UpdateRoleDefinitionRequest request, CancellationToken ct = default);
    Task<ApiCallResult<PlatformRoleDefinitionDto>> ActivatePlatformRoleDefinitionAsync(Guid id, RoleLifecycleRequest? request = null, CancellationToken ct = default);
    Task<ApiCallResult<PlatformRoleDefinitionDto>> DeactivatePlatformRoleDefinitionAsync(Guid id, RoleLifecycleRequest? request = null, CancellationToken ct = default);
    Task<ApiCallResult<PlatformRoleDefinitionDto>> RetirePlatformRoleDefinitionAsync(Guid id, RoleLifecycleRequest? request = null, CancellationToken ct = default);
    Task<ApiCallResult<PagedResult<PlatformRoleAssignmentDto>>> GetPlatformRoleAssignmentsAsync(Guid? platformUserId = null, string? role = null, string? status = null, int page = 1, int pageSize = 20, CancellationToken ct = default);
    Task<ApiCallResult<PlatformRoleAssignmentDto>> AssignPlatformSystemRoleAsync(AssignSystemRoleRequest request, CancellationToken ct = default);
    Task<ApiCallResult<PlatformRoleAssignmentDto>> RevokePlatformSystemRoleAsync(Guid assignmentId, RevokeRoleAssignmentRequest? request = null, CancellationToken ct = default);
    Task<ApiCallResult<PagedResult<PlatformCustomRoleAssignmentDto>>> GetPlatformCustomRoleAssignmentsAsync(Guid? platformUserId = null, Guid? roleDefinitionId = null, string? status = null, int page = 1, int pageSize = 20, CancellationToken ct = default);
    Task<ApiCallResult<PlatformCustomRoleAssignmentDto>> AssignPlatformCustomRoleAsync(AssignCustomRoleRequest request, CancellationToken ct = default);
    Task<ApiCallResult<PlatformCustomRoleAssignmentDto>> RevokePlatformCustomRoleAsync(Guid assignmentId, RevokeRoleAssignmentRequest? request = null, CancellationToken ct = default);
    Task<ApiCallResult<EffectivePlatformPermissionsDto>> GetEffectivePlatformPermissionsAsync(Guid userId, Guid? organizationId = null, CancellationToken ct = default);
    Task<ApiCallResult<PagedResult<OrganizationRoleDefinitionDto>>> GetOrganizationRoleDefinitionsAsync(Guid organizationId, int page = 1, int pageSize = 20, string? status = null, string? search = null, CancellationToken ct = default);
    Task<ApiCallResult<OrganizationRoleDefinitionDto>> GetOrganizationRoleDefinitionAsync(Guid organizationId, Guid roleId, CancellationToken ct = default);
    Task<ApiCallResult<OrganizationRoleDefinitionDto>> CreateOrganizationRoleDefinitionAsync(Guid organizationId, CreateRoleDefinitionRequest request, CancellationToken ct = default);
    Task<ApiCallResult<OrganizationRoleDefinitionDto>> UpdateOrganizationRoleDefinitionAsync(Guid organizationId, Guid roleId, UpdateRoleDefinitionRequest request, CancellationToken ct = default);
    Task<ApiCallResult<OrganizationRoleDefinitionDto>> ActivateOrganizationRoleDefinitionAsync(Guid organizationId, Guid roleId, RoleLifecycleRequest? request = null, CancellationToken ct = default);
    Task<ApiCallResult<OrganizationRoleDefinitionDto>> DeactivateOrganizationRoleDefinitionAsync(Guid organizationId, Guid roleId, RoleLifecycleRequest? request = null, CancellationToken ct = default);
    Task<ApiCallResult<OrganizationRoleDefinitionDto>> RetireOrganizationRoleDefinitionAsync(Guid organizationId, Guid roleId, RoleLifecycleRequest? request = null, CancellationToken ct = default);
    Task<ApiCallResult<PagedResult<OrganizationCustomRoleAssignmentDto>>> GetOrganizationRoleAssignmentsAsync(Guid organizationId, Guid? platformUserId = null, Guid? roleDefinitionId = null, string? status = null, int page = 1, int pageSize = 20, CancellationToken ct = default);
    Task<ApiCallResult<OrganizationCustomRoleAssignmentDto>> AssignOrganizationCustomRoleAsync(Guid organizationId, AssignOrgCustomRoleRequest request, CancellationToken ct = default);
    Task<ApiCallResult<OrganizationCustomRoleAssignmentDto>> RevokeOrganizationCustomRoleAsync(Guid organizationId, Guid assignmentId, RevokeRoleAssignmentRequest? request = null, CancellationToken ct = default);
    Task<ApiCallResult<EffectiveOrganizationPermissionsDto>> GetEffectiveOrganizationPermissionsAsync(Guid organizationId, Guid userId, CancellationToken ct = default);

    Task<ApiCallResult<PlatformCredentialStatusDto>> GetUserCredentialsAsync(Guid userId, CancellationToken ct = default);
    Task<ApiCallResult<PlatformCredentialStatusDto>> SetUserPasswordAsync(Guid userId, SetUserPasswordRequest request, CancellationToken ct = default);
    Task<ApiCallResult<PlatformCredentialStatusDto>> UnlockUserCredentialAsync(Guid userId, CancellationToken ct = default);
    Task<ApiCallResult<PlatformCredentialStatusDto>> MarkUserEmailVerifiedAsync(Guid userId, CancellationToken ct = default);
    Task<ApiCallResult<PlatformCredentialStatusDto>> ChangePasswordAsync(ChangePasswordRequest request, CancellationToken ct = default);
    Task<ApiCallResult<CredentialWorkflowAckDto>> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken ct = default);
    Task<ApiCallResult<PlatformCredentialStatusDto>> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct = default);
    Task<ApiCallResult<PersonalRegistrationAckDto>> RegisterPersonalAccountAsync(RegisterPersonalAccountRequest request, CancellationToken ct = default);
    Task<ApiCallResult<PlatformCredentialStatusDto>> ActivatePersonalAccountAsync(ActivatePersonalAccountRequest request, CancellationToken ct = default);
    Task<ApiCallResult<PlatformCredentialStatusDto>> GetMyCredentialsAsync(CancellationToken ct = default);
    Task<ApiCallResult<CredentialWorkflowAckDto>> RequestEmailVerificationAsync(CancellationToken ct = default);
    Task<ApiCallResult<PlatformCredentialStatusDto>> ConfirmEmailVerificationAsync(ConfirmEmailVerificationRequest request, CancellationToken ct = default);
    Task<ApiCallResult<CredentialWorkflowAckDto>> RequestRecoveryEmailAsync(RequestRecoveryEmailRequest request, CancellationToken ct = default);
    Task<ApiCallResult<PlatformCredentialStatusDto>> ConfirmRecoveryEmailAsync(ConfirmRecoveryEmailRequest request, CancellationToken ct = default);
    Task<ApiCallResult<PlatformCredentialStatusDto>> SkipRecoveryEmailAsync(CancellationToken ct = default);
    Task<ApiCallResult<PlatformCredentialStatusDto>> ClearRecoveryEmailAsync(CancellationToken ct = default);

    Task<ApiCallResult<AuthSessionInfoDto>> GetAuthMeAsync(CancellationToken ct = default);
    Task<ApiCallResult<IReadOnlyList<EligibleOrganizationDto>>> GetEligibleOrganizationsAsync(CancellationToken ct = default);
    Task<ApiCallResult<OrganizationContextResultDto>> SetOrganizationContextAsync(SetOrganizationContextRequest request, CancellationToken ct = default);

    Task<ApiCallResult<PersonalDashboardDto>> GetPersonalDashboardAsync(CancellationToken ct = default);
    Task<ApiCallResult<PersonalProfileDto>> GetPersonalProfileAsync(CancellationToken ct = default);
    Task<ApiCallResult<PersonalAccountSettingsDto>> GetPersonalSettingsAsync(CancellationToken ct = default);
    Task<ApiCallResult<IReadOnlyList<PersonalContactDto>>> GetPersonalUtangContactsAsync(CancellationToken ct = default);
    Task<ApiCallResult<IReadOnlyList<PersonalDebtRelationshipSummaryDto>>> GetPersonalUtangLentAsync(CancellationToken ct = default);
    Task<ApiCallResult<IReadOnlyList<PersonalDebtRelationshipSummaryDto>>> GetPersonalUtangBorrowedAsync(CancellationToken ct = default);
    Task<ApiCallResult<IReadOnlyList<PersonalUtangInvitationDto>>> GetPersonalUtangInvitationsAsync(CancellationToken ct = default);
    Task<ApiCallResult<IReadOnlyList<PersonalInAppNotificationDto>>> GetPersonalNotificationsAsync(CancellationToken ct = default);

    Task<ApiCallResult<AccessTokenIssueDto>> IssueAccessTokenAsync(IssueAccessTokenRequest request, CancellationToken ct = default);
    Task<ApiCallResult<AccessTokenIntrospectionDto>> IntrospectAccessTokenAsync(IntrospectAccessTokenRequest request, CancellationToken ct = default);

    // Global merchandise catalog (Phase 20) — not commercial SaaS /api/v1/platform/catalog/*
    Task<ApiCallResult<PagedResult<BusinessTypeDto>>> GetBusinessTypesAsync(
        int page = 1,
        int pageSize = 50,
        string? status = null,
        string? search = null,
        string? sortBy = null,
        bool? sortDesc = null,
        CancellationToken ct = default);
    Task<ApiCallResult<BusinessTypeDto>> GetBusinessTypeAsync(Guid id, CancellationToken ct = default);
    Task<ApiCallResult<BusinessTypeDto>> CreateBusinessTypeAsync(CreateBusinessTypeRequest request, CancellationToken ct = default);
    Task<ApiCallResult<BusinessTypeDto>> UpdateBusinessTypeAsync(Guid id, UpdateBusinessTypeRequest request, CancellationToken ct = default);
    Task<ApiCallResult<BusinessTypeDto>> SetBusinessTypeStatusAsync(Guid id, SetBusinessTypeStatusRequest request, CancellationToken ct = default);

    Task<ApiCallResult<PagedResult<GlobalCategoryDto>>> GetGlobalCategoriesAsync(
        int page = 1,
        int pageSize = 50,
        string? status = null,
        Guid? parentId = null,
        string? businessTypeCode = null,
        Guid? businessTypeId = null,
        string? search = null,
        string? sortBy = null,
        bool? sortDesc = null,
        CancellationToken ct = default);
    Task<ApiCallResult<GlobalCategoryDto>> GetGlobalCategoryAsync(Guid id, CancellationToken ct = default);
    Task<ApiCallResult<GlobalCategoryDto>> CreateGlobalCategoryAsync(CreateGlobalCategoryRequest request, CancellationToken ct = default);
    Task<ApiCallResult<GlobalCategoryDto>> UpdateGlobalCategoryAsync(Guid id, UpdateGlobalCategoryRequest request, CancellationToken ct = default);
    Task<ApiCallResult<GlobalCategoryDto>> SetGlobalCategoryStatusAsync(Guid id, SetGlobalCategoryStatusRequest request, CancellationToken ct = default);
    Task<ApiCallResult<GlobalCategoryDto>> BulkAssignCategoryBusinessTypesAsync(
        Guid id,
        BulkAssignCategoryBusinessTypesRequest request,
        CancellationToken ct = default);

    Task<ApiCallResult<PagedResult<GlobalProductDto>>> GetGlobalProductsAsync(
        int page = 1,
        int pageSize = 20,
        string? status = null,
        Guid? categoryId = null,
        string? businessTypeCode = null,
        Guid? businessTypeId = null,
        string? search = null,
        string? barcode = null,
        string? sku = null,
        string? sortBy = null,
        bool? sortDesc = null,
        CancellationToken ct = default);
    Task<ApiCallResult<GlobalProductDto>> GetGlobalProductAsync(Guid id, CancellationToken ct = default);
    Task<ApiCallResult<GlobalProductDto>> CreateGlobalProductAsync(CreateGlobalProductRequest request, CancellationToken ct = default);
    Task<ApiCallResult<GlobalProductDto>> UpdateGlobalProductAsync(Guid id, UpdateGlobalProductRequest request, CancellationToken ct = default);
    Task<ApiCallResult<GlobalProductDto>> SetGlobalProductStatusAsync(Guid id, SetGlobalProductStatusRequest request, CancellationToken ct = default);

    Task<ApiCallResult<PagedResult<CatalogTemplateSummaryDto>>> GetCatalogTemplatesAsync(
        int page = 1,
        int pageSize = 20,
        string? status = null,
        string? primaryBusinessTypeCode = null,
        Guid? primaryBusinessTypeId = null,
        string? search = null,
        string? sortBy = null,
        bool? sortDesc = null,
        CancellationToken ct = default);
    Task<ApiCallResult<CatalogTemplateDto>> GetCatalogTemplateAsync(Guid id, CancellationToken ct = default);
    Task<ApiCallResult<CatalogTemplateDto>> CreateCatalogTemplateAsync(CreateCatalogTemplateRequest request, CancellationToken ct = default);
    Task<ApiCallResult<CatalogTemplateDto>> UpdateCatalogTemplateAsync(Guid id, UpdateCatalogTemplateRequest request, CancellationToken ct = default);
    Task<ApiCallResult<CatalogTemplateDto>> PublishCatalogTemplateAsync(Guid id, CatalogTemplateLifecycleRequest? request = null, CancellationToken ct = default);
    Task<ApiCallResult<CatalogTemplateDto>> UnpublishCatalogTemplateAsync(Guid id, CatalogTemplateLifecycleRequest? request = null, CancellationToken ct = default);
    Task<ApiCallResult<CatalogTemplateDto>> ArchiveCatalogTemplateAsync(Guid id, CatalogTemplateLifecycleRequest? request = null, CancellationToken ct = default);
    Task<ApiCallResult<CatalogTemplateDto>> AssignCatalogTemplateProductAsync(Guid id, AssignCatalogTemplateProductRequest request, CancellationToken ct = default);
    Task<ApiCallResult<CatalogTemplateDto>> BulkAssignCatalogTemplateProductsAsync(Guid id, BulkAssignCatalogTemplateProductsRequest request, CancellationToken ct = default);
    Task<ApiCallResult<CatalogTemplateDto>> BulkRemoveCatalogTemplateProductsAsync(Guid id, BulkRemoveCatalogTemplateProductsRequest request, CancellationToken ct = default);
    Task<ApiCallResult<PagedResult<GlobalProductDto>>> GetCatalogTemplateAvailableProductsAsync(
        Guid id,
        int page = 1,
        int pageSize = 20,
        string? status = null,
        Guid? categoryId = null,
        string? search = null,
        string? barcode = null,
        string? sku = null,
        string? sortBy = null,
        bool? sortDesc = null,
        CancellationToken ct = default);
    Task<ApiCallResult<CatalogTemplateDto>> ReorderCatalogTemplateProductsAsync(Guid id, ReorderCatalogTemplateProductsRequest request, CancellationToken ct = default);
    Task<ApiCallResult<CatalogTemplateDto>> UpdateCatalogTemplateProductFlagsAsync(Guid id, Guid productId, UpdateCatalogTemplateProductFlagsRequest request, CancellationToken ct = default);
    Task<ApiCallResult<CatalogTemplateDto>> RemoveCatalogTemplateProductAsync(Guid id, Guid productId, DateTimeOffset? expectedUpdatedAtUtc = null, CancellationToken ct = default);

    Task<ApiCallResult<PagedResult<CatalogImportJobDto>>> GetCatalogImportsAsync(
        int page = 1,
        int pageSize = 20,
        string? status = null,
        CancellationToken ct = default);
    Task<ApiCallResult<CatalogImportJobDto>> GetCatalogImportAsync(Guid jobId, CancellationToken ct = default);
    Task<ApiCallResult<CatalogImportJobDto>> UploadCatalogImportAsync(
        Stream content,
        string fileName,
        string? contentType,
        string? idempotencyKey = null,
        CancellationToken ct = default);
    Task<ApiCallResult<CatalogImportJobDto>> ConfirmCatalogImportAsync(
        Guid jobId,
        Guid? targetTemplateId = null,
        CancellationToken ct = default);
    Task<ApiCallResult<PagedResult<CatalogImportErrorDto>>> GetCatalogImportErrorsAsync(
        Guid jobId,
        int page = 1,
        int pageSize = 100,
        CancellationToken ct = default);
    Task<ApiCallResult<byte[]>> DownloadCatalogImportTemplateAsync(CancellationToken ct = default);

    Task<ApiCallResult<PrivacyComplianceOverviewDto>> GetPrivacyComplianceOverviewAsync(CancellationToken ct = default);
    Task<ApiCallResult<IReadOnlyList<ComplianceRequirementDto>>> ListPrivacyComplianceRequirementsAsync(string? category = null, CancellationToken ct = default);
    Task<ApiCallResult<ComplianceRequirementDto>> GetPrivacyComplianceRequirementAsync(Guid id, CancellationToken ct = default);
    Task<ApiCallResult<ComplianceRequirementDto>> UpdatePrivacyComplianceRequirementStatusAsync(Guid id, UpdateComplianceRequirementStatusRequest request, CancellationToken ct = default);
    Task<ApiCallResult<ComplianceRequirementDto>> UpdatePrivacyComplianceRequirementDetailsAsync(Guid id, UpdateComplianceRequirementDetailsRequest request, CancellationToken ct = default);
    Task<ApiCallResult<IReadOnlyList<ComplianceEvidenceDto>>> ListPrivacyComplianceEvidenceAsync(Guid requirementId, CancellationToken ct = default);
    Task<ApiCallResult<ComplianceEvidenceDto>> AddPrivacyComplianceEvidenceAsync(AddComplianceEvidenceRequest request, CancellationToken ct = default);
    Task<ApiCallResult<IReadOnlyList<ProcessingSystemDto>>> ListPrivacyComplianceSystemsAsync(CancellationToken ct = default);
    Task<ApiCallResult<EnsurePrivacyComplianceCatalogResultDto>> EnsurePrivacyComplianceCatalogAsync(CancellationToken ct = default);
    Task<ApiCallResult<byte[]>> ExportPrivacyComplianceRequirementPdfAsync(Guid requirementId, string? companyName = null, CancellationToken ct = default);
}
