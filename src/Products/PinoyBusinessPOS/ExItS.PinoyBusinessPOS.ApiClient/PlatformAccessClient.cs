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

    public Task<ApiResult<object>> LogoutSessionAsync(CancellationToken ct = default) =>
        api.SendAsync<object>(HttpMethod.Post, "/api/v1/platform/auth/logout", null, ct);

    public Task<ApiResult<PlatformLoginResultDto>> SelectAccountProfileAsync(
        SelectAccountProfileRequest request,
        CancellationToken ct = default) =>
        api.SendAsync<PlatformLoginResultDto>(HttpMethod.Post, "/api/v1/platform/auth/account-profiles/select", request, ct);

    public Task<ApiResult<StartBusinessResultDto>> StartBusinessAsync(
        StartBusinessRequest request,
        CancellationToken ct = default) =>
        api.SendAsync<StartBusinessResultDto>(HttpMethod.Post, "/api/v1/personal/start-business", request, ct);

    public Task<ApiResult<OrganizationInvitationDto>> CreateOrganizationInvitationAsync(
        Guid organizationId,
        CreateInvitationRequest request,
        CancellationToken ct = default) =>
        api.SendAsync<OrganizationInvitationDto>(
            HttpMethod.Post,
            $"/api/v1/platform/organizations/{organizationId:D}/invitations",
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

    public Task<ApiResult<PlatformMembershipDto>> AcceptOrganizationInvitationAsync(
        string token,
        CancellationToken ct = default) =>
        api.SendAsync<PlatformMembershipDto>(
            HttpMethod.Post,
            "/api/v1/platform/auth/organization-invitations/accept",
            new AcceptOrganizationInvitationRequest(token),
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

    public Task<ApiResult<IReadOnlyList<LocalValidationQuickLoginIdentityDto>>> GetLocalValidationQuickLoginIdentitiesAsync(
        CancellationToken ct = default) =>
        api.GetAsync<IReadOnlyList<LocalValidationQuickLoginIdentityDto>>(
            "/api/v1/platform/local-validation/quick-login-identities",
            ct);
}
