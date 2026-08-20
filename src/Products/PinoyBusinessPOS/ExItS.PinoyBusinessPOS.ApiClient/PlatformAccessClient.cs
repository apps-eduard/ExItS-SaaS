using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Platform;

namespace ExItS.PinoyBusinessPOS.ApiClient;

public sealed class PlatformAccessClient(IPosApiClient api) : IPlatformAccessClient
{
    public Task<ApiResult<PlatformUserDto>> GetUserAsync(Guid userId, CancellationToken ct = default) =>
        api.GetAsync<PlatformUserDto>($"/api/v1/platform/users/{userId:D}", ct);

    public Task<ApiResult<PlatformOrganizationDto>> GetOrganizationAsync(Guid organizationId, CancellationToken ct = default) =>
        api.GetAsync<PlatformOrganizationDto>($"/api/v1/platform/organizations/{organizationId:D}", ct);

    public Task<ApiResult<PlatformOrganizationDto>> UpdateOrganizationAsync(
        Guid organizationId,
        UpdatePlatformOrganizationRequest request,
        CancellationToken ct = default) =>
        api.SendAsync<PlatformOrganizationDto>(HttpMethod.Put, $"/api/v1/platform/organizations/{organizationId:D}", request, ct);

    public Task<ApiResult<IReadOnlyList<OrganizationBranchDto>>> GetBranchesAsync(Guid organizationId, CancellationToken ct = default) =>
        api.GetAsync<IReadOnlyList<OrganizationBranchDto>>($"/api/v1/platform/organizations/{organizationId:D}/branches", ct);

    public Task<ApiResult<PlatformPagedResult<PlatformGovernanceAuditRecordDto>>> GetOrganizationAuditAsync(
        Guid organizationId,
        int page = 1,
        int pageSize = 20,
        DateTimeOffset? fromUtc = null,
        DateTimeOffset? toUtc = null,
        string? action = null,
        string? actor = null,
        string? outcome = null,
        Guid? branchId = null,
        CancellationToken ct = default)
    {
        var query = new List<string>
        {
            $"page={page}",
            $"pageSize={pageSize}",
            "outcome=Succeeded"
        };
        if (fromUtc is not null) query.Add($"fromUtc={Uri.EscapeDataString(fromUtc.Value.ToString("O"))}");
        if (toUtc is not null) query.Add($"toUtc={Uri.EscapeDataString(toUtc.Value.ToString("O"))}");
        if (!string.IsNullOrWhiteSpace(action)) query.Add($"action={Uri.EscapeDataString(action)}");
        if (!string.IsNullOrWhiteSpace(actor)) query.Add($"actor={Uri.EscapeDataString(actor)}");
        if (!string.IsNullOrWhiteSpace(outcome)) query[^1] = $"outcome={Uri.EscapeDataString(outcome)}";
        if (branchId is not null) query.Add($"branchId={branchId.Value:D}");

        var url = $"/api/v1/platform/organizations/{organizationId:D}/audit?{string.Join('&', query)}";
        return api.GetAsync<PlatformPagedResult<PlatformGovernanceAuditRecordDto>>(url, ct);
    }

    public Task<ApiResult<OrganizationBranchContextDto>> SelectBranchContextAsync(
        Guid organizationId,
        SelectBranchContextRequest request,
        CancellationToken ct = default) =>
        api.SendAsync<OrganizationBranchContextDto>(
            HttpMethod.Put,
            $"/api/v1/platform/organizations/{organizationId:D}/branch-context",
            request,
            ct);

    public Task<ApiResult<BranchCapacityDto>> GetBranchCapacityAsync(Guid organizationId, CancellationToken ct = default) =>
        api.GetAsync<BranchCapacityDto>($"/api/v1/platform/organizations/{organizationId:D}/branches/capacity", ct);

    public Task<ApiResult<OrganizationBranchDto>> CreateBranchAsync(Guid organizationId, CreateBranchRequest request, CancellationToken ct = default) =>
        api.SendAsync<OrganizationBranchDto>(HttpMethod.Post, $"/api/v1/platform/organizations/{organizationId:D}/branches", request, ct);

    public Task<ApiResult<OrganizationBranchDto>> UpdateBranchAsync(
        Guid organizationId,
        Guid branchId,
        UpdateBranchRequest request,
        CancellationToken ct = default) =>
        api.SendAsync<OrganizationBranchDto>(
            HttpMethod.Put,
            $"/api/v1/platform/organizations/{organizationId:D}/branches/{branchId:D}",
            request,
            ct);

    public Task<ApiResult<GovernanceStepUpTokenDto>> IssueGovernanceStepUpAsync(
        Guid organizationId,
        IssueGovernanceStepUpRequest request,
        CancellationToken ct = default) =>
        api.SendAsync<GovernanceStepUpTokenDto>(
            HttpMethod.Post,
            $"/api/v1/platform/organizations/{organizationId:D}/governance/step-up",
            request,
            ct);

    public Task<ApiResult<OrganizationBranchDto>> SuspendBranchAsync(
        Guid organizationId,
        Guid branchId,
        GovernanceCriticalActionRequest request,
        CancellationToken ct = default) =>
        api.SendAsync<OrganizationBranchDto>(
            HttpMethod.Post,
            $"/api/v1/platform/organizations/{organizationId:D}/branches/{branchId:D}/suspend",
            request,
            ct);

    public Task<ApiResult<OrganizationBranchDto>> ReactivateBranchAsync(
        Guid organizationId,
        Guid branchId,
        GovernanceCriticalActionRequest request,
        CancellationToken ct = default) =>
        api.SendAsync<OrganizationBranchDto>(
            HttpMethod.Post,
            $"/api/v1/platform/organizations/{organizationId:D}/branches/{branchId:D}/reactivate",
            request,
            ct);

    public Task<ApiResult<OrganizationBranchDto>> ArchiveBranchAsync(
        Guid organizationId,
        Guid branchId,
        GovernanceCriticalActionRequest request,
        CancellationToken ct = default) =>
        api.SendAsync<OrganizationBranchDto>(
            HttpMethod.Post,
            $"/api/v1/platform/organizations/{organizationId:D}/branches/{branchId:D}/archive",
            request,
            ct);

    public Task<ApiResult<BranchDeliveryPolicyDto>> UpsertBranchDeliveryPolicyAsync(
        Guid organizationId,
        Guid branchId,
        UpsertBranchDeliveryPolicyRequest request,
        CancellationToken ct = default) =>
        api.SendAsync<BranchDeliveryPolicyDto>(
            HttpMethod.Put,
            $"/api/v1/platform/organizations/{organizationId:D}/branches/{branchId:D}/delivery-policy",
            request,
            ct);

    public Task<ApiResult<BranchFulfillmentReadinessDto>> GetBranchFulfillmentReadinessAsync(
        Guid organizationId,
        Guid branchId,
        CancellationToken ct = default) =>
        api.GetAsync<BranchFulfillmentReadinessDto>(
            $"/api/v1/platform/organizations/{organizationId:D}/branches/{branchId:D}/fulfillment-readiness",
            ct);

    public Task<ApiResult<IReadOnlyList<BranchOperatingHoursDayDto>>> GetBranchOperatingHoursAsync(
        Guid organizationId,
        Guid branchId,
        CancellationToken ct = default) =>
        api.GetAsync<IReadOnlyList<BranchOperatingHoursDayDto>>(
            $"/api/v1/platform/organizations/{organizationId:D}/branches/{branchId:D}/operating-hours",
            ct);

    public Task<ApiResult<BranchFulfillmentReadinessDto>> UpsertBranchOperatingHoursAsync(
        Guid organizationId,
        Guid branchId,
        UpsertBranchOperatingHoursRequest request,
        CancellationToken ct = default) =>
        api.SendAsync<BranchFulfillmentReadinessDto>(
            HttpMethod.Put,
            $"/api/v1/platform/organizations/{organizationId:D}/branches/{branchId:D}/operating-hours",
            request,
            ct);

    public Task<ApiResult<BranchFulfillmentReadinessDto>> UpdateBranchFulfillmentSettingsAsync(
        Guid organizationId,
        Guid branchId,
        UpdateBranchFulfillmentSettingsRequest request,
        CancellationToken ct = default) =>
        api.SendAsync<BranchFulfillmentReadinessDto>(
            HttpMethod.Put,
            $"/api/v1/platform/organizations/{organizationId:D}/branches/{branchId:D}/fulfillment-settings",
            request,
            ct);

    public Task<ApiResult<BranchFulfillmentReadinessDto>> SetBranchOnlineOrdersPausedAsync(
        Guid organizationId,
        Guid branchId,
        SetBranchOnlineOrdersPausedRequest request,
        CancellationToken ct = default) =>
        api.SendAsync<BranchFulfillmentReadinessDto>(
            HttpMethod.Post,
            $"/api/v1/platform/organizations/{organizationId:D}/branches/{branchId:D}/online-orders-pause",
            request,
            ct);

    public Task<ApiResult<DeliveryFeePreviewDto>> PreviewBranchDeliveryFeeAsync(
        Guid organizationId,
        Guid branchId,
        DeliveryFeePreviewRequest request,
        CancellationToken ct = default) =>
        api.SendAsync<DeliveryFeePreviewDto>(
            HttpMethod.Post,
            $"/api/v1/platform/organizations/{organizationId:D}/branches/{branchId:D}/delivery-fee-preview",
            request,
            ct);

    public Task<ApiResult<IReadOnlyList<PosDeviceDto>>> GetPosDevicesAsync(Guid organizationId, CancellationToken ct = default) =>
        api.GetAsync<IReadOnlyList<PosDeviceDto>>($"/api/v1/platform/organizations/{organizationId:D}/pos-devices", ct);

    public Task<ApiResult<PosDeviceCapacityDto>> GetPosDeviceCapacityAsync(Guid organizationId, CancellationToken ct = default) =>
        api.GetAsync<PosDeviceCapacityDto>($"/api/v1/platform/organizations/{organizationId:D}/pos-devices/capacity", ct);

    public Task<ApiResult<PosDeviceDto>> RegisterCurrentDeviceAsync(Guid organizationId, RegisterPosDeviceRequest request, CancellationToken ct = default) =>
        api.SendAsync<PosDeviceDto>(HttpMethod.Post, $"/api/v1/platform/organizations/{organizationId:D}/pos-devices/register", request, ct);

    public Task<ApiResult<PosDeviceDto>> RenamePosDeviceAsync(Guid organizationId, Guid deviceId, string friendlyName, CancellationToken ct = default) =>
        api.SendAsync<PosDeviceDto>(HttpMethod.Put, $"/api/v1/platform/organizations/{organizationId:D}/pos-devices/{deviceId:D}", new { friendlyName }, ct);

    public Task<ApiResult<PosDeviceDto>> RevokePosDeviceAsync(
        Guid organizationId,
        Guid deviceId,
        GovernanceCriticalActionRequest request,
        CancellationToken ct = default) =>
        api.SendAsync<PosDeviceDto>(
            HttpMethod.Post,
            $"/api/v1/platform/organizations/{organizationId:D}/pos-devices/{deviceId:D}/revoke",
            request,
            ct);

    public Task<ApiResult<PosDeviceAuthorizationDto>> AuthorizePosDeviceAsync(Guid organizationId, AuthorizePosDeviceRequest request, CancellationToken ct = default) =>
        api.SendAsync<PosDeviceAuthorizationDto>(HttpMethod.Post, $"/api/v1/platform/organizations/{organizationId:D}/pos-devices/authorize", request, ct);

    public Task<ApiResult<PlatformPagedResult<PlatformMembershipDto>>> GetUserMembershipsAsync(Guid userId, CancellationToken ct = default) =>
        api.GetAsync<PlatformPagedResult<PlatformMembershipDto>>(
            $"/api/v1/platform/users/{userId:D}/memberships?page=1&pageSize=100&status=Active",
            ct);

    public Task<ApiResult<PlatformPagedResult<PlatformMembershipDto>>> GetOrganizationMembersAsync(
        Guid organizationId,
        int page = 1,
        int pageSize = 50,
        string? status = null,
        CancellationToken ct = default)
    {
        var path =
            $"/api/v1/platform/organizations/{organizationId:D}/members?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(status))
        {
            path += $"&status={Uri.EscapeDataString(status.Trim())}";
        }

        return api.GetAsync<PlatformPagedResult<PlatformMembershipDto>>(path, ct);
    }

    public Task<ApiResult<PlatformMembershipDto>> SuspendMembershipAsync(
        Guid membershipId,
        PlatformMembershipLifecycleRequest request,
        CancellationToken ct = default) =>
        api.SendAsync<PlatformMembershipDto>(
            HttpMethod.Post,
            $"/api/v1/platform/memberships/{membershipId:D}/suspend",
            request,
            ct);

    public Task<ApiResult<PlatformMembershipDto>> RevokeMembershipAsync(
        Guid membershipId,
        PlatformMembershipLifecycleRequest request,
        CancellationToken ct = default) =>
        api.SendAsync<PlatformMembershipDto>(
            HttpMethod.Post,
            $"/api/v1/platform/memberships/{membershipId:D}/revoke",
            request,
            ct);

    public Task<ApiResult<IReadOnlyList<MembershipBranchAssignmentDto>>> GetMembershipBranchAssignmentsAsync(
        Guid organizationId,
        Guid membershipId,
        CancellationToken ct = default) =>
        api.GetAsync<IReadOnlyList<MembershipBranchAssignmentDto>>(
            $"/api/v1/platform/organizations/{organizationId:D}/members/{membershipId:D}/branch-assignments",
            ct);

    public Task<ApiResult<IReadOnlyList<MembershipBranchAssignmentDto>>> SetMembershipBranchAssignmentsAsync(
        Guid organizationId,
        Guid membershipId,
        SetMembershipBranchAssignmentsRequest request,
        CancellationToken ct = default) =>
        api.SendAsync<IReadOnlyList<MembershipBranchAssignmentDto>>(
            HttpMethod.Put,
            $"/api/v1/platform/organizations/{organizationId:D}/members/{membershipId:D}/branch-assignments",
            request,
            ct);

    public Task<ApiResult<EffectiveAccessDto>> EvaluateAccessAsync(
        Guid userId,
        Guid organizationId,
        string productCode,
        CancellationToken ct = default) =>
        api.GetAsync<EffectiveAccessDto>(
            $"/api/v1/platform/access/evaluate?userId={userId:D}&organizationId={organizationId:D}&productCode={Uri.EscapeDataString(productCode)}",
            ct);

    public Task<ApiResult<PlatformAccessTokenIssueDto>> IssueTokenAsync(
        IssuePlatformAccessTokenRequest request,
        CancellationToken ct = default) =>
        api.SendAsync<PlatformAccessTokenIssueDto>(HttpMethod.Post, "/api/v1/platform/auth/token", request, ct);

    public Task<ApiResult<DeviceRecoveryCredentialEnrollDto>> EnrollDeviceRecoveryCredentialAsync(
        CancellationToken ct = default) =>
        api.SendAsync<DeviceRecoveryCredentialEnrollDto>(
            HttpMethod.Post,
            "/api/v1/platform/auth/recovery/enroll",
            null,
            ct);

    public Task<ApiResult<DeviceRecoveryCredentialExchangeDto>> ExchangeDeviceRecoveryCredentialAsync(
        DeviceRecoveryCredentialExchangeRequest request,
        CancellationToken ct = default) =>
        api.SendAsync<DeviceRecoveryCredentialExchangeDto>(
            HttpMethod.Post,
            "/api/v1/platform/auth/recovery/exchange",
            request,
            ct);

    public Task<ApiResult<object>> RevokeDeviceRecoveryCredentialAsync(CancellationToken ct = default) =>
        api.SendAsync<object>(HttpMethod.Post, "/api/v1/platform/auth/recovery/revoke", null, ct);

    public Task<ApiResult<PlatformAccessTokenIssueDto>> BindTokenAsync(
        BindPlatformAccessTokenRequest request,
        CancellationToken ct = default) =>
        api.SendAsync<PlatformAccessTokenIssueDto>(HttpMethod.Post, "/api/v1/platform/auth/token/bind", request, ct);

    public Task<ApiResult<PlatformAccessTokenIntrospectionDto>> IntrospectTokenAsync(
        string? token = null,
        CancellationToken ct = default) =>
        api.SendAsync<PlatformAccessTokenIntrospectionDto>(
            HttpMethod.Post,
            "/api/v1/platform/auth/introspect",
            new { token },
            ct);

    public Task<ApiResult<object>> RevokeAccessTokenAsync(CancellationToken ct = default) =>
        api.SendAsync<object>(HttpMethod.Post, "/api/v1/platform/auth/token/revoke", null, ct);

    public Task<ApiResult<IReadOnlyList<PlatformAuthEligibleOrganizationDto>>> GetAuthEligibleOrganizationsAsync(
        CancellationToken ct = default) =>
        api.GetAsync<IReadOnlyList<PlatformAuthEligibleOrganizationDto>>(
            "/api/v1/platform/auth/organizations",
            ct);

    public Task<ApiResult<PersonalRegistrationAckDto>> RegisterPersonalAccountAsync(
        RegisterPersonalAccountRequest request,
        CancellationToken ct = default) =>
        api.SendAsync<PersonalRegistrationAckDto>(HttpMethod.Post, "/api/v1/platform/auth/register", request, ct);

    public Task<ApiResult<object>> ActivatePersonalAccountAsync(
        ActivatePersonalAccountRequest request,
        CancellationToken ct = default) =>
        api.SendAsync<object>(HttpMethod.Post, "/api/v1/platform/auth/activate-account", request, ct);

    public Task<ApiResult<PlatformLoginResultDto>> LoginAsync(
        PlatformLoginRequest request,
        CancellationToken ct = default) =>
        api.SendAsync<PlatformLoginResultDto>(HttpMethod.Post, "/api/v1/platform/auth/login", request, ct);

    public Task<ApiResult<PlatformAuthSessionInfoDto>> GetAuthMeAsync(CancellationToken ct = default) =>
        api.GetAsync<PlatformAuthSessionInfoDto>("/api/v1/platform/auth/me", ct);

    public Task<ApiResult<CredentialWorkflowAckDto>> ForgotPasswordAsync(
        ForgotPasswordRequest request,
        CancellationToken ct = default) =>
        api.SendAsync<CredentialWorkflowAckDto>(HttpMethod.Post, "/api/v1/platform/auth/forgot-password", request, ct);

    public Task<ApiResult<object>> LogoutSessionAsync(CancellationToken ct = default) =>
        api.SendAsync<object>(HttpMethod.Post, "/api/v1/platform/auth/logout", null, ct);

    public Task<ApiResult<PlatformLoginResultDto>> SelectAccountProfileAsync(
        SelectAccountProfileRequest request,
        CancellationToken ct = default) =>
        api.SendAsync<PlatformLoginResultDto>(HttpMethod.Post, "/api/v1/platform/auth/account-profiles/select", request, ct);

    public Task<ApiResult<PlatformAccountProfileDto>> EnsureAccountProfileAsync(
        EnsureAccountProfileRequest request,
        CancellationToken ct = default) =>
        api.SendAsync<PlatformAccountProfileDto>(HttpMethod.Post, "/api/v1/platform/auth/account-profiles/ensure", request, ct);

    public Task<ApiResult<StartBusinessResultDto>> StartBusinessAsync(
        StartBusinessRequest request,
        CancellationToken ct = default) =>
        api.SendAsync<StartBusinessResultDto>(HttpMethod.Post, "/api/v1/personal/start-business", request, ct);

    public Task<ApiResult<IReadOnlyList<BusinessTypeDto>>> GetOnboardingBusinessTypesAsync(
        CancellationToken ct = default) =>
        api.GetAsync<IReadOnlyList<BusinessTypeDto>>("/api/v1/personal/onboarding/business-types", ct);

    public Task<ApiResult<IReadOnlyList<BusinessTypeDto>>> GetActiveBusinessTypesAsync(
        CancellationToken ct = default) =>
        api.GetAsync<IReadOnlyList<BusinessTypeDto>>("/api/v1/catalog/business-types", ct);

    public Task<ApiResult<IReadOnlyList<CommercialPlanDto>>> GetCommercialPlansAsync(
        string? productCode = null,
        CancellationToken ct = default)
    {
        var path = "/api/v1/commercial/plans";
        if (!string.IsNullOrWhiteSpace(productCode))
        {
            path += $"?productCode={Uri.EscapeDataString(productCode.Trim())}";
        }

        return api.GetAsync<IReadOnlyList<CommercialPlanDto>>(path, ct);
    }

    public Task<ApiResult<OrganizationBusinessTypeEntitlementDto>> GetOrganizationBusinessTypeEntitlementsAsync(
        Guid organizationId,
        string? productCode = null,
        CancellationToken ct = default)
    {
        var path = $"/api/v1/platform/organizations/{organizationId:D}/business-type-entitlements";
        if (!string.IsNullOrWhiteSpace(productCode))
        {
            path += $"?productCode={Uri.EscapeDataString(productCode.Trim())}";
        }

        return api.GetAsync<OrganizationBusinessTypeEntitlementDto>(path, ct);
    }

    public Task<ApiResult<OrganizationBusinessTypeActivationDto>> ActivateOrganizationBusinessTypeAsync(
        Guid organizationId,
        ActivateOrganizationBusinessTypeRequest request,
        CancellationToken ct = default) =>
        api.SendAsync<OrganizationBusinessTypeActivationDto>(
            HttpMethod.Post,
            $"/api/v1/platform/organizations/{organizationId:D}/business-type-activations",
            request,
            ct);

    public Task<ApiResult<object>> DeactivateOrganizationBusinessTypeAsync(
        Guid organizationId,
        Guid businessTypeId,
        CancellationToken ct = default) =>
        api.SendAsync<object>(
            HttpMethod.Delete,
            $"/api/v1/platform/organizations/{organizationId:D}/business-type-activations/{businessTypeId:D}",
            null,
            ct);

    public Task<ApiResult<OrganizationInvitationDto>> CreateOrganizationInvitationAsync(
        Guid organizationId,
        CreateInvitationRequest request,
        CancellationToken ct = default) =>
        api.SendAsync<OrganizationInvitationDto>(
            HttpMethod.Post,
            $"/api/v1/platform/organizations/{organizationId:D}/invitations",
            request,
            ct);

    public Task<ApiResult<PlatformPagedResult<OrganizationInvitationDto>>> GetOrganizationInvitationsAsync(
        Guid organizationId,
        string? status = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        var path =
            $"/api/v1/platform/organizations/{organizationId:D}/invitations?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(status))
        {
            path += $"&status={Uri.EscapeDataString(status.Trim())}";
        }

        return api.GetAsync<PlatformPagedResult<OrganizationInvitationDto>>(path, ct);
    }

    public Task<ApiResult<OrganizationInvitationDto>> ResendOrganizationInvitationAsync(
        Guid invitationId,
        CancellationToken ct = default) =>
        api.SendAsync<OrganizationInvitationDto>(
            HttpMethod.Post,
            $"/api/v1/platform/invitations/{invitationId:D}/resend",
            null,
            ct);

    public Task<ApiResult<OrganizationInvitationDto>> RevokeOrganizationInvitationAsync(
        Guid invitationId,
        CancellationToken ct = default) =>
        api.SendAsync<OrganizationInvitationDto>(
            HttpMethod.Post,
            $"/api/v1/platform/invitations/{invitationId:D}/revoke",
            null,
            ct);

    public Task<ApiResult<PlatformMembershipDto>> ChangeMembershipRoleAsync(
        Guid membershipId,
        string role,
        CancellationToken ct = default) =>
        api.SendAsync<PlatformMembershipDto>(
            HttpMethod.Put,
            $"/api/v1/platform/memberships/{membershipId:D}/role",
            new { role },
            ct);

    public Task<ApiResult<PlatformMembershipDto>> ReactivateMembershipAsync(
        Guid membershipId,
        PlatformMembershipLifecycleRequest request,
        CancellationToken ct = default) =>
        api.SendAsync<PlatformMembershipDto>(
            HttpMethod.Post,
            $"/api/v1/platform/memberships/{membershipId:D}/reactivate",
            request,
            ct);

    public Task<ApiResult<IReadOnlyList<ProductLocalRoleGrantDto>>> GetProductLocalRolesAsync(
        Guid organizationId,
        string? status = null,
        CancellationToken ct = default)
    {
        var path = $"/api/v1/organizations/{organizationId:D}/product-local-roles";
        if (!string.IsNullOrWhiteSpace(status))
        {
            path += $"?status={Uri.EscapeDataString(status.Trim())}";
        }

        return api.GetAsync<IReadOnlyList<ProductLocalRoleGrantDto>>(path, ct);
    }

    public Task<ApiResult<ProductLocalRoleGrantDto>> AssignProductLocalRoleAsync(
        Guid organizationId,
        AssignProductLocalRoleRequest request,
        CancellationToken ct = default) =>
        api.SendAsync<ProductLocalRoleGrantDto>(
            HttpMethod.Post,
            $"/api/v1/organizations/{organizationId:D}/product-local-roles",
            request,
            ct);

    public Task<ApiResult<ProductLocalRoleGrantDto>> RevokeProductLocalRoleAsync(
        Guid organizationId,
        Guid grantId,
        RevokeProductLocalRoleRequest request,
        CancellationToken ct = default) =>
        api.SendAsync<ProductLocalRoleGrantDto>(
            HttpMethod.Post,
            $"/api/v1/organizations/{organizationId:D}/product-local-roles/{grantId:D}/revoke",
            request,
            ct);

    public Task<ApiResult<PlatformSubscriptionDto>> GetCurrentSubscriptionAsync(
        Guid organizationId,
        string productCode,
        CancellationToken ct = default) =>
        api.GetAsync<PlatformSubscriptionDto>(
            $"/api/v1/platform/organizations/{organizationId:D}/subscriptions/current?productCode={Uri.EscapeDataString(productCode)}",
            ct);

    public Task<ApiResult<PlatformEntitlementSnapshotDto>> GetLatestEntitlementAsync(
        Guid organizationId,
        string productCode,
        CancellationToken ct = default) =>
        api.GetAsync<PlatformEntitlementSnapshotDto>(
            $"/api/v1/platform/organizations/{organizationId:D}/products/{Uri.EscapeDataString(productCode)}/entitlements/snapshots/latest",
            ct);

    public Task<ApiResult<object>> SetOrganizationContextAsync(
        SetOrganizationContextRequest request,
        CancellationToken ct = default) =>
        api.SendAsync<object>(HttpMethod.Put, "/api/v1/platform/auth/organization-context", request, ct);

    public Task<ApiResult<IReadOnlyList<PlatformAccountProfileDto>>> GetAccountProfilesAsync(
        CancellationToken ct = default) =>
        api.GetAsync<IReadOnlyList<PlatformAccountProfileDto>>(
            "/api/v1/platform/auth/account-profiles",
            ct);

    public Task<ApiResult<IReadOnlyList<PendingOrganizationInvitationDto>>> GetPendingOrganizationInvitationsAsync(
        CancellationToken ct = default) =>
        api.GetAsync<IReadOnlyList<PendingOrganizationInvitationDto>>(
            "/api/v1/platform/auth/organization-invitations/pending",
            ct);

    public Task<ApiResult<AcceptOrganizationInvitationResultDto>> AcceptOrganizationInvitationAsync(
        string token,
        string password,
        CancellationToken ct = default) =>
        api.SendAsync<AcceptOrganizationInvitationResultDto>(
            HttpMethod.Post,
            "/api/v1/platform/auth/organization-invitations/accept",
            new AcceptOrganizationInvitationRequest(token, password),
            ct);

    public Task<ApiResult<AcceptOrganizationInvitationResultDto>> AcceptOrganizationInvitationAsPersonalAsync(
        string token,
        string password,
        CancellationToken ct = default) =>
        api.SendAsync<AcceptOrganizationInvitationResultDto>(
            HttpMethod.Post,
            "/api/v1/platform/auth/organization-invitations/accept-as-personal",
            new AcceptOrganizationInvitationRequest(token, password),
            ct);

    public Task<ApiResult<PlatformMembershipDto>> AcceptOrganizationInvitationByIdAsync(
        Guid invitationId,
        CancellationToken ct = default) =>
        api.SendAsync<PlatformMembershipDto>(
            HttpMethod.Post,
            $"/api/v1/platform/auth/organization-invitations/{invitationId:D}/accept",
            null,
            ct);

    public Task<ApiResult<PersonalDashboardDto>> GetPersonalDashboardAsync(CancellationToken ct = default) =>
        api.GetAsync<PersonalDashboardDto>("/api/v1/personal/dashboard", ct);

    public Task<ApiResult<PersonalProfileDto>> GetPersonalProfileAsync(CancellationToken ct = default) =>
        api.GetAsync<PersonalProfileDto>("/api/v1/personal/profile", ct);

    public Task<ApiResult<PublicIdentityDto>> GetMyPublicIdentityAsync(CancellationToken ct = default) =>
        api.GetAsync<PublicIdentityDto>("/api/v1/me/public-identity", ct);

    public Task<ApiResult<ResolvedPublicUserDto>> ResolvePublicUserIdAsync(
        ResolvePublicUserIdRequest request,
        CancellationToken ct = default) =>
        api.SendAsync<ResolvedPublicUserDto>(HttpMethod.Post, "/api/v1/users/resolve-public-id", request, ct);

    public Task<ApiResult<OrganizationPublicIdentityDto>> GetOrganizationPublicIdentityAsync(
        Guid organizationId,
        CancellationToken ct = default) =>
        api.GetAsync<OrganizationPublicIdentityDto>(
            $"/api/v1/organizations/{organizationId:D}/public-identity",
            ct);

    public Task<ApiResult<ResolvedPublicOrganizationDto>> ResolveOrganizationPublicIdAsync(
        ResolvePublicOrganizationIdRequest request,
        CancellationToken ct = default) =>
        api.SendAsync<ResolvedPublicOrganizationDto>(
            HttpMethod.Post,
            "/api/v1/organizations/resolve-public-id",
            request,
            ct);

    public Task<ApiResult<ResolvedExItsQrDto>> ResolveQrAsync(
        ResolveExItsQrRequest request,
        CancellationToken ct = default) =>
        api.SendAsync<ResolvedExItsQrDto>(HttpMethod.Post, "/api/v1/qr/resolve", request, ct);

    public Task<ApiResult<PosDeviceRegistrationTokenDto>> CreatePosDeviceRegistrationTokenAsync(
        Guid organizationId,
        CancellationToken ct = default) =>
        api.SendAsync<PosDeviceRegistrationTokenDto>(
            HttpMethod.Post,
            $"/api/v1/platform/organizations/{organizationId:D}/pos-devices/registration-tokens",
            null,
            ct);

    public Task<ApiResult<PosDeviceDto>> RedeemPosDeviceRegistrationTokenAsync(
        Guid organizationId,
        RedeemPosDeviceRegistrationTokenRequest request,
        CancellationToken ct = default) =>
        api.SendAsync<PosDeviceDto>(
            HttpMethod.Post,
            $"/api/v1/platform/organizations/{organizationId:D}/pos-devices/registration-tokens/redeem",
            request,
            ct);

    public Task<ApiResult<PosDeviceRegistrationTokenMetadataDto>> GetPosDeviceRegistrationTokenAsync(
        Guid organizationId,
        Guid tokenId,
        CancellationToken ct = default) =>
        api.GetAsync<PosDeviceRegistrationTokenMetadataDto>(
            $"/api/v1/platform/organizations/{organizationId:D}/pos-devices/registration-tokens/{tokenId:D}",
            ct);

    public Task<ApiResult<PersonalAccountSettingsDto>> GetPersonalSettingsAsync(CancellationToken ct = default) =>
        api.GetAsync<PersonalAccountSettingsDto>("/api/v1/personal/settings", ct);

    public Task<ApiResult<PersonalAccountSettingsDto>> UpdatePersonalSettingsAsync(
        UpdatePersonalAccountSettingsRequest request,
        CancellationToken ct = default) =>
        api.SendAsync<PersonalAccountSettingsDto>(HttpMethod.Put, "/api/v1/personal/settings", request, ct);

    public Task<ApiResult<IReadOnlyList<PersonalContactDto>>> GetPersonalContactsAsync(CancellationToken ct = default) =>
        api.GetAsync<IReadOnlyList<PersonalContactDto>>("/api/v1/personal/utang/contacts", ct);

    public Task<ApiResult<PersonalContactDto>> CreatePersonalContactAsync(
        CreatePersonalContactRequest request,
        CancellationToken ct = default) =>
        api.SendAsync<PersonalContactDto>(HttpMethod.Post, "/api/v1/personal/utang/contacts", request, ct);

    public Task<ApiResult<IReadOnlyList<PersonalDebtRelationshipSummaryDto>>> GetPersonalUtangLentAsync(
        CancellationToken ct = default) =>
        api.GetAsync<IReadOnlyList<PersonalDebtRelationshipSummaryDto>>(
            "/api/v1/personal/utang/relationships/lent",
            ct);

    public Task<ApiResult<IReadOnlyList<PersonalDebtRelationshipSummaryDto>>> GetPersonalUtangBorrowedAsync(
        CancellationToken ct = default) =>
        api.GetAsync<IReadOnlyList<PersonalDebtRelationshipSummaryDto>>(
            "/api/v1/personal/utang/relationships/borrowed",
            ct);

    public Task<ApiResult<PersonalDebtRelationshipSummaryDto>> CreatePersonalDebtRelationshipAsync(
        CreatePersonalDebtRelationshipRequest request,
        CancellationToken ct = default) =>
        api.SendAsync<PersonalDebtRelationshipSummaryDto>(
            HttpMethod.Post,
            "/api/v1/personal/utang/relationships",
            request,
            ct);

    public Task<ApiResult<PersonalDebtRelationshipSummaryDto>> GetPersonalDebtRelationshipAsync(
        Guid relationshipId,
        CancellationToken ct = default) =>
        api.GetAsync<PersonalDebtRelationshipSummaryDto>(
            $"/api/v1/personal/utang/relationships/{relationshipId:D}",
            ct);

    public Task<ApiResult<PersonalUtangBalanceDto>> GetPersonalUtangBalanceAsync(
        Guid relationshipId,
        CancellationToken ct = default) =>
        api.GetAsync<PersonalUtangBalanceDto>(
            $"/api/v1/personal/utang/relationships/{relationshipId:D}/balance",
            ct);

    public Task<ApiResult<IReadOnlyList<PersonalUtangEntryDto>>> GetPersonalUtangHistoryAsync(
        Guid relationshipId,
        CancellationToken ct = default) =>
        api.GetAsync<IReadOnlyList<PersonalUtangEntryDto>>(
            $"/api/v1/personal/utang/relationships/{relationshipId:D}/history",
            ct);

    public Task<ApiResult<PersonalUtangEntryDto>> RecordPersonalUtangEntryAsync(
        Guid relationshipId,
        RecordPersonalUtangEntryRequest request,
        CancellationToken ct = default) =>
        api.SendAsync<PersonalUtangEntryDto>(
            HttpMethod.Post,
            $"/api/v1/personal/utang/relationships/{relationshipId:D}/entries",
            request,
            ct);

    public Task<ApiResult<IReadOnlyList<PersonalUtangInvitationDto>>> GetPersonalUtangInvitationsAsync(
        CancellationToken ct = default) =>
        api.GetAsync<IReadOnlyList<PersonalUtangInvitationDto>>("/api/v1/personal/utang/invitations", ct);

    public Task<ApiResult<PersonalUtangInvitationDto>> CreatePersonalUtangInvitationAsync(
        Guid relationshipId,
        CreatePersonalUtangInvitationRequest request,
        CancellationToken ct = default) =>
        api.SendAsync<PersonalUtangInvitationDto>(
            HttpMethod.Post,
            $"/api/v1/personal/utang/relationships/{relationshipId:D}/invitations",
            request,
            ct);

    public Task<ApiResult<PersonalUtangInvitationAcceptResultDto>> AcceptPersonalUtangInvitationAsync(
        string token,
        CancellationToken ct = default) =>
        api.SendAsync<PersonalUtangInvitationAcceptResultDto>(
            HttpMethod.Post,
            "/api/v1/personal/utang/invitations/accept",
            new AcceptPersonalUtangInvitationRequest(token),
            ct);

    public Task<ApiResult<PersonalUtangInvitationDto>> DeclinePersonalUtangInvitationAsync(
        string token,
        CancellationToken ct = default) =>
        api.SendAsync<PersonalUtangInvitationDto>(
            HttpMethod.Post,
            "/api/v1/personal/utang/invitations/decline",
            new AcceptPersonalUtangInvitationRequest(token),
            ct);

    public Task<ApiResult<PlatformPagedResult<LinkedMerchantDto>>> GetLinkedMerchantsAsync(
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default) =>
        api.GetAsync<PlatformPagedResult<LinkedMerchantDto>>(
            $"/api/v1/personal/linked-merchants?page={page}&pageSize={pageSize}",
            ct);

    public Task<ApiResult<LinkedMerchantOrderingCapabilityDto>> GetLinkedMerchantOrderingCapabilityAsync(
        Guid organizationId,
        CancellationToken ct = default) =>
        api.GetAsync<LinkedMerchantOrderingCapabilityDto>(
            $"/api/v1/personal/linked-merchants/{organizationId:D}/ordering-capability",
            ct);

    public Task<ApiResult<CreateBusinessCustomerWithPersonalLinkResultDto>> CreateBusinessCustomerWithPersonalLinkAsync(
        Guid organizationId,
        CreateBusinessCustomerWithPersonalLinkRequest request,
        CancellationToken ct = default) =>
        api.SendAsync<CreateBusinessCustomerWithPersonalLinkResultDto>(
            HttpMethod.Post,
            $"/api/v1/organizations/{organizationId:D}/customers/with-personal-link",
            request,
            ct);

    public Task<ApiResult<PlatformCustomerLinkStatusDto>> GetCustomerLinkStatusAsync(
        Guid organizationId,
        Guid businessCustomerId,
        CancellationToken ct = default) =>
        api.GetAsync<PlatformCustomerLinkStatusDto>(
            $"/api/v1/organizations/{organizationId:D}/customers/{businessCustomerId:D}/link-status",
            ct);

    public Task<ApiResult<IReadOnlyList<PlatformCustomerLinkRequestDto>>> GetCustomerLinkRequestsAsync(
        Guid organizationId,
        Guid businessCustomerId,
        CancellationToken ct = default) =>
        api.GetAsync<IReadOnlyList<PlatformCustomerLinkRequestDto>>(
            $"/api/v1/organizations/{organizationId:D}/customers/{businessCustomerId:D}/link-requests",
            ct);

    public Task<ApiResult<PlatformCustomerLinkRequestStatsDto>> GetCustomerLinkRequestStatsAsync(
        Guid organizationId,
        CancellationToken ct = default) =>
        api.GetAsync<PlatformCustomerLinkRequestStatsDto>(
            $"/api/v1/organizations/{organizationId:D}/customer-link-requests/stats",
            ct);

    public Task<ApiResult<IReadOnlyList<OrganizationInAppNotificationDto>>> GetOrganizationNotificationsAsync(
        Guid organizationId,
        CancellationToken ct = default) =>
        api.GetAsync<IReadOnlyList<OrganizationInAppNotificationDto>>(
            $"/api/v1/organizations/{organizationId:D}/notifications",
            ct);

    public Task<ApiResult<OrganizationInAppNotificationDto>> MarkOrganizationNotificationReadAsync(
        Guid organizationId,
        Guid notificationId,
        CancellationToken ct = default) =>
        api.SendAsync<OrganizationInAppNotificationDto>(
            HttpMethod.Post,
            $"/api/v1/organizations/{organizationId:D}/notifications/{notificationId:D}/read",
            null,
            ct);

    public Task<ApiResult<MarkRelatedOrganizationNotificationsReadResultDto>> MarkRelatedOrganizationNotificationsReadAsync(
        Guid organizationId,
        string relatedType,
        string relatedId,
        CancellationToken ct = default) =>
        api.SendAsync<MarkRelatedOrganizationNotificationsReadResultDto>(
            HttpMethod.Post,
            $"/api/v1/organizations/{organizationId:D}/notifications/related/read",
            new { RelatedType = relatedType, RelatedId = relatedId },
            ct);

    public Task<ApiResult<IReadOnlyList<PersonalPendingCustomerLinkRequestDto>>> GetPersonalCustomerLinkRequestsAsync(
        CancellationToken ct = default) =>
        api.GetAsync<IReadOnlyList<PersonalPendingCustomerLinkRequestDto>>(
            "/api/v1/personal/customer-link-requests",
            ct);

    public Task<ApiResult<AcceptCustomerLinkResultDto>> AcceptPersonalCustomerLinkRequestAsync(
        Guid requestId,
        CancellationToken ct = default) =>
        api.SendAsync<AcceptCustomerLinkResultDto>(
            HttpMethod.Post,
            $"/api/v1/personal/customer-link-requests/{requestId:D}/accept",
            null,
            ct);

    public Task<ApiResult<PlatformCustomerLinkRequestDto>> DeclinePersonalCustomerLinkRequestAsync(
        Guid requestId,
        CancellationToken ct = default) =>
        api.SendAsync<PlatformCustomerLinkRequestDto>(
            HttpMethod.Post,
            $"/api/v1/personal/customer-link-requests/{requestId:D}/decline",
            null,
            ct);

    public Task<ApiResult<IReadOnlyList<PersonalInAppNotificationDto>>> GetPersonalNotificationsAsync(
        CancellationToken ct = default) =>
        api.GetAsync<IReadOnlyList<PersonalInAppNotificationDto>>(
            "/api/v1/personal/notifications",
            ct);

    public Task<ApiResult<PersonalInAppNotificationDto>> MarkPersonalNotificationReadAsync(
        Guid notificationId,
        CancellationToken ct = default) =>
        api.SendAsync<PersonalInAppNotificationDto>(
            HttpMethod.Post,
            $"/api/v1/personal/notifications/{notificationId:D}/read",
            null,
            ct);

    public Task<ApiResult<PersonalRewardBalanceDto>> GetPersonalRewardBalanceAsync(CancellationToken ct = default) =>
        api.GetAsync<PersonalRewardBalanceDto>("/api/v1/personal/reward-points/balance", ct);

    public Task<ApiResult<PersonalRewardActivityPageDto>> GetPersonalRewardActivityAsync(
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default) =>
        api.GetAsync<PersonalRewardActivityPageDto>(
            $"/api/v1/personal/reward-points/activity?page={page}&pageSize={pageSize}",
            ct);

    public Task<ApiResult<PersonalFeatureActiveDto>> GetPersonalFeatureActiveAsync(
        string featureCode,
        CancellationToken ct = default) =>
        api.GetAsync<PersonalFeatureActiveDto>(
            $"/api/v1/personal/features/{Uri.EscapeDataString(featureCode)}/active",
            ct);

    public Task<ApiResult<RedeemPersonalFeatureResultDto>> RedeemPersonalFeatureAsync(
        string featureCode,
        CancellationToken ct = default) =>
        api.SendAsync<RedeemPersonalFeatureResultDto>(
            HttpMethod.Post,
            $"/api/v1/personal/features/{Uri.EscapeDataString(featureCode)}/redeem",
            null,
            ct);

    public Task<ApiResult<PersonalAdEligibilityDto>> GetPersonalAdEligibilityAsync(CancellationToken ct = default) =>
        api.GetAsync<PersonalAdEligibilityDto>("/api/v1/personal/ads/eligibility", ct);

    public Task<ApiResult<IReadOnlyList<LocalValidationQuickLoginIdentityDto>>> GetLocalValidationQuickLoginIdentitiesAsync(
        CancellationToken ct = default) =>
        api.GetAsync<IReadOnlyList<LocalValidationQuickLoginIdentityDto>>(
            "/api/v1/platform/local-validation/quick-login-identities",
            ct);

    public Task<ApiResult<OwnershipTransferTargetDto>> ResolveOwnershipTransferTargetAsync(
        Guid organizationId,
        string input,
        CancellationToken ct = default) =>
        api.SendAsync<OwnershipTransferTargetDto>(
            HttpMethod.Post,
            $"/api/v1/platform/organizations/{organizationId:D}/ownership-transfer/resolve-target",
            new ResolveOwnershipTransferTargetRequest(input),
            ct);

    public Task<ApiResult<OrganizationOwnershipTransferDto>> RequestOwnershipTransferAsync(
        Guid organizationId,
        string targetInput,
        CancellationToken ct = default) =>
        api.SendAsync<OrganizationOwnershipTransferDto>(
            HttpMethod.Post,
            $"/api/v1/platform/organizations/{organizationId:D}/ownership-transfer/request",
            new RequestOwnershipTransferRequest(targetInput),
            ct);

    public Task<ApiResult<OrganizationOwnershipTransferDto?>> GetPendingOwnershipTransferAsync(
        Guid organizationId,
        CancellationToken ct = default) =>
        api.GetAsync<OrganizationOwnershipTransferDto?>(
            $"/api/v1/platform/organizations/{organizationId:D}/ownership-transfer/pending",
            ct);

    public Task<ApiResult<OrganizationOwnershipTransferDto>> CancelOwnershipTransferAsync(
        Guid transferId,
        CancellationToken ct = default) =>
        api.SendAsync<OrganizationOwnershipTransferDto>(
            HttpMethod.Post,
            $"/api/v1/platform/ownership-transfers/{transferId:D}/cancel",
            null,
            ct);

    public Task<ApiResult<OrganizationOwnershipTransferDto>> AcceptOwnershipTransferAsync(
        Guid transferId,
        CancellationToken ct = default) =>
        api.SendAsync<OrganizationOwnershipTransferDto>(
            HttpMethod.Post,
            $"/api/v1/platform/ownership-transfers/{transferId:D}/accept",
            null,
            ct);

    public Task<ApiResult<OrganizationOwnershipTransferDto>> DeclineOwnershipTransferAsync(
        Guid transferId,
        CancellationToken ct = default) =>
        api.SendAsync<OrganizationOwnershipTransferDto>(
            HttpMethod.Post,
            $"/api/v1/platform/ownership-transfers/{transferId:D}/decline",
            null,
            ct);

    public Task<ApiResult<IReadOnlyList<OrganizationOwnershipTransferDto>>> GetMyPendingOwnershipTransfersAsync(
        CancellationToken ct = default) =>
        api.GetAsync<IReadOnlyList<OrganizationOwnershipTransferDto>>(
            "/api/v1/platform/ownership-transfers/my-pending",
            ct);

    public Task<ApiResult<OrganizationSalesDocumentEducationStatusDto>> GetSalesDocumentEducationStatusAsync(
        Guid organizationId,
        CancellationToken ct = default) =>
        api.GetAsync<OrganizationSalesDocumentEducationStatusDto>(
            $"/api/v1/platform/organizations/{organizationId:D}/sales-document-education",
            ct);

    public Task<ApiResult<OrganizationSalesDocumentEducationStatusDto>> AcknowledgeSalesDocumentEducationAsync(
        Guid organizationId,
        CancellationToken ct = default) =>
        api.SendAsync<OrganizationSalesDocumentEducationStatusDto>(
            HttpMethod.Post,
            $"/api/v1/platform/organizations/{organizationId:D}/sales-document-education/acknowledge",
            null,
            ct);

    public Task<ApiResult<OrganizationComplianceStatusDto>> GetOrganizationComplianceStatusAsync(
        Guid organizationId,
        CancellationToken ct = default) =>
        api.GetAsync<OrganizationComplianceStatusDto>(
            $"/api/v1/platform/organizations/{organizationId:D}/compliance-status",
            ct);

    public Task<ApiResult<OrganizationComplianceStatusDto>> RequestOrganizationComplianceReviewAsync(
        Guid organizationId,
        CancellationToken ct = default) =>
        api.SendAsync<OrganizationComplianceStatusDto>(
            HttpMethod.Post,
            $"/api/v1/platform/organizations/{organizationId:D}/compliance/request",
            null,
            ct);

    public Task<ApiResult<OrganizationComplianceProfileDto>> GetOrganizationComplianceProfileAsync(
        Guid organizationId,
        CancellationToken ct = default) =>
        api.GetAsync<OrganizationComplianceProfileDto>(
            $"/api/v1/platform/organizations/{organizationId:D}/compliance-profile",
            ct);

    public Task<ApiResult<OrganizationComplianceProfileDto>> UpdateRegisteredTaxpayerAsync(
        Guid organizationId,
        UpdateRegisteredTaxpayerRequest request,
        CancellationToken ct = default) =>
        api.SendAsync<OrganizationComplianceProfileDto>(
            HttpMethod.Put,
            $"/api/v1/platform/organizations/{organizationId:D}/compliance-profile/registered-taxpayer",
            request,
            ct);

    public Task<ApiResult<ComplianceActivationReadinessDto>> GetComplianceActivationReadinessAsync(
        Guid organizationId,
        CancellationToken ct = default) =>
        api.GetAsync<ComplianceActivationReadinessDto>(
            $"/api/v1/platform/organizations/{organizationId:D}/compliance/readiness",
            ct);

    public Task<ApiResult<ComplianceActivationReadinessDto>> SubmitComplianceReadinessForReviewAsync(
        Guid organizationId,
        CancellationToken ct = default) =>
        api.SendAsync<ComplianceActivationReadinessDto>(
            HttpMethod.Post,
            $"/api/v1/platform/organizations/{organizationId:D}/compliance/readiness/submit",
            null,
            ct);

    public Task<ApiResult<IReadOnlyList<BranchComplianceProfileDto>>> ListBranchComplianceProfilesAsync(
        Guid organizationId,
        CancellationToken ct = default) =>
        api.GetAsync<IReadOnlyList<BranchComplianceProfileDto>>(
            $"/api/v1/platform/organizations/{organizationId:D}/compliance/branch-profiles",
            ct);

    public Task<ApiResult<BranchComplianceProfileDto>> UpsertBranchComplianceProfileAsync(
        Guid organizationId,
        Guid branchId,
        UpsertBranchComplianceProfileRequest request,
        CancellationToken ct = default) =>
        api.SendAsync<BranchComplianceProfileDto>(
            HttpMethod.Put,
            $"/api/v1/platform/organizations/{organizationId:D}/branches/{branchId:D}/compliance-profile",
            request,
            ct);

    public Task<ApiResult<IReadOnlyList<ComplianceRegistrationRecordDto>>> ListComplianceRegistrationRecordsAsync(
        Guid organizationId,
        CancellationToken ct = default) =>
        api.GetAsync<IReadOnlyList<ComplianceRegistrationRecordDto>>(
            $"/api/v1/platform/organizations/{organizationId:D}/compliance/registration-records",
            ct);

    public Task<ApiResult<ComplianceRegistrationRecordDto>> AddComplianceRegistrationRecordAsync(
        Guid organizationId,
        CreateComplianceRegistrationRequest request,
        CancellationToken ct = default) =>
        api.SendAsync<ComplianceRegistrationRecordDto>(
            HttpMethod.Post,
            $"/api/v1/platform/organizations/{organizationId:D}/compliance/registration-records",
            request,
            ct);
}
