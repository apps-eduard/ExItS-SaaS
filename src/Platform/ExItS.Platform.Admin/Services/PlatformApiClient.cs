using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using ExItS.Platform.Admin.Models;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;

namespace ExItS.Platform.Admin.Services;

public sealed class PlatformApiClient(
    HttpClient httpClient,
    IHttpContextAccessor httpContextAccessor,
    AuthenticationStateProvider authenticationStateProvider,
    PlatformCircuitSession circuitSession) : IPlatformApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private const string DevActor = "dev-admin";
    private const string SessionTokenHeader = "X-ExItS-Session-Token";

    public Task<ApiCallResult<PortfolioSummaryDto>> GetPortfolioSummaryAsync(CancellationToken ct = default) =>
        GetAsync<PortfolioSummaryDto>("/api/v1/platform/admin/portfolio-summary", ct);
    public Task<ApiCallResult<PagedResult<ProductDto>>> GetProductsAsync(int page = 1, int pageSize = 20, string? status = null, CancellationToken ct = default) =>
        GetAsync<PagedResult<ProductDto>>($"/api/v1/platform/catalog/products?{Query(("page", page), ("pageSize", pageSize), ("status", status))}", ct);
    public Task<ApiCallResult<ProductDto>> GetProductAsync(Guid id, CancellationToken ct = default) =>
        GetAsync<ProductDto>($"/api/v1/platform/catalog/products/{id}", ct);
    public Task<ApiCallResult<ProductOverviewDto>> GetProductOverviewAsync(string productCode, CancellationToken ct = default) =>
        GetAsync<ProductOverviewDto>($"/api/v1/platform/admin/products/{Escape(productCode)}/overview", ct);
    public Task<ApiCallResult<PagedResult<OrganizationDto>>> GetOrganizationsAsync(
        int page = 1,
        int pageSize = 20,
        string? status = null,
        string? search = null,
        string? sortBy = null,
        bool? sortDesc = null,
        CancellationToken ct = default) =>
        GetAsync<PagedResult<OrganizationDto>>(
            $"/api/v1/platform/organizations?{Query(("page", page), ("pageSize", pageSize), ("status", status), ("search", search), ("sortBy", sortBy), ("sortDesc", sortDesc))}",
            ct);
    public Task<ApiCallResult<OrganizationDto>> GetOrganizationAsync(Guid id, CancellationToken ct = default) =>
        GetAsync<OrganizationDto>($"/api/v1/platform/organizations/{id}", ct);
    public Task<ApiCallResult<OrganizationDto>> CreateOrganizationAsync(CreateOrganizationRequest request, CancellationToken ct = default) =>
        SendAsync<OrganizationDto>(HttpMethod.Post, "/api/v1/platform/organizations", request, ct);
    public Task<ApiCallResult<OrganizationDto>> UpdateOrganizationAsync(Guid id, UpdateOrganizationRequest request, CancellationToken ct = default) =>
        SendAsync<OrganizationDto>(HttpMethod.Put, $"/api/v1/platform/organizations/{id}", request, ct);
    public Task<ApiCallResult<OrganizationDto>> UpdateOrganizationBrandingAsync(Guid id, UpdateOrganizationBrandingRequest request, CancellationToken ct = default) =>
        SendAsync<OrganizationDto>(HttpMethod.Put, $"/api/v1/platform/organizations/{id}/branding", request, ct);
    public Task<ApiCallResult<OrganizationDto>> SuspendOrganizationAsync(Guid id, CancellationToken ct = default) =>
        SendAsync<OrganizationDto>(HttpMethod.Post, $"/api/v1/platform/organizations/{id}/suspend", null, ct);
    public Task<ApiCallResult<OrganizationDto>> ReactivateOrganizationAsync(Guid id, CancellationToken ct = default) =>
        SendAsync<OrganizationDto>(HttpMethod.Post, $"/api/v1/platform/organizations/{id}/reactivate", null, ct);
    public Task<ApiCallResult<OrganizationDto>> CloseOrganizationAsync(Guid id, CancellationToken ct = default) =>
        SendAsync<OrganizationDto>(HttpMethod.Post, $"/api/v1/platform/organizations/{id}/close", null, ct);
    public Task<ApiCallResult<OrganizationCommercialSummaryDto>> GetOrganizationCommercialSummaryAsync(Guid id, CancellationToken ct = default) =>
        GetAsync<OrganizationCommercialSummaryDto>($"/api/v1/platform/admin/organizations/{id}/commercial-summary", ct);
    public Task<ApiCallResult<PagedResult<SubscriptionDto>>> GetSubscriptionsAsync(string? status = null, string? productCode = null, int page = 1, int pageSize = 20, CancellationToken ct = default) =>
        GetAsync<PagedResult<SubscriptionDto>>($"/api/v1/platform/subscriptions?{Query(("status", status), ("productCode", productCode), ("page", page), ("pageSize", pageSize))}", ct);
    public Task<ApiCallResult<SubscriptionDto>> GetSubscriptionAsync(Guid id, CancellationToken ct = default) =>
        GetAsync<SubscriptionDto>($"/api/v1/platform/subscriptions/{id}", ct);
    public Task<ApiCallResult<PagedResult<PaymentDto>>> GetPaymentsAsync(string? status = null, string? productCode = null, Guid? organizationId = null, string? method = null, int page = 1, int pageSize = 20, CancellationToken ct = default) =>
        GetAsync<PagedResult<PaymentDto>>($"/api/v1/platform/payments?{Query(("status", status), ("productCode", productCode), ("organizationId", organizationId), ("method", method), ("page", page), ("pageSize", pageSize))}", ct);
    public Task<ApiCallResult<PaymentDto>> GetPaymentAsync(Guid id, CancellationToken ct = default) =>
        GetAsync<PaymentDto>($"/api/v1/platform/payments/{id}", ct);
    public Task<ApiCallResult<PagedResult<EntitlementLatestSummaryDto>>> GetLatestEntitlementsAsync(int page = 1, int pageSize = 20, CancellationToken ct = default) =>
        GetAsync<PagedResult<EntitlementLatestSummaryDto>>($"/api/v1/platform/admin/entitlements/latest?{Query(("page", page), ("pageSize", pageSize))}", ct);
    public Task<ApiCallResult<PagedResult<EntitlementSnapshotDto>>> GetEntitlementHistoryAsync(Guid organizationId, string productCode, CancellationToken ct = default) =>
        GetAsync<PagedResult<EntitlementSnapshotDto>>($"/api/v1/platform/organizations/{organizationId}/products/{Escape(productCode)}/entitlements/snapshots", ct);
    public Task<ApiCallResult<EntitlementSnapshotDto>> GetLatestEntitlementAsync(Guid organizationId, string productCode, CancellationToken ct = default) =>
        GetAsync<EntitlementSnapshotDto>($"/api/v1/platform/organizations/{organizationId}/products/{Escape(productCode)}/entitlements/snapshots/latest", ct);
    public Task<ApiCallResult<EntitlementSnapshotDto>> GetEntitlementAsync(Guid id, CancellationToken ct = default) =>
        GetAsync<EntitlementSnapshotDto>($"/api/v1/platform/entitlements/snapshots/{id}", ct);
    public Task<ApiCallResult<PagedResult<FeatureOverrideDto>>> GetFeatureOverridesAsync(Guid organizationId, string productCode, CancellationToken ct = default) =>
        GetAsync<PagedResult<FeatureOverrideDto>>($"/api/v1/platform/organizations/{organizationId}/products/{Escape(productCode)}/feature-overrides", ct);

    public Task<ApiCallResult<PagedResult<PlatformUserDto>>> GetUsersAsync(int page = 1, int pageSize = 20, string? status = null, string? search = null, CancellationToken ct = default) =>
        GetAsync<PagedResult<PlatformUserDto>>($"/api/v1/platform/users?{Query(("page", page), ("pageSize", pageSize), ("status", status), ("search", search))}", ct);
    public Task<ApiCallResult<PlatformUserDto>> GetUserAsync(Guid id, CancellationToken ct = default) =>
        GetAsync<PlatformUserDto>($"/api/v1/platform/users/{id}", ct);
    public Task<ApiCallResult<PlatformUserDto>> CreateUserAsync(CreatePlatformUserRequest request, CancellationToken ct = default) =>
        SendAsync<PlatformUserDto>(HttpMethod.Post, "/api/v1/platform/users", request, ct);
    public Task<ApiCallResult<PlatformUserDto>> UpdateUserAsync(Guid id, UpdatePlatformUserRequest request, CancellationToken ct = default) =>
        SendAsync<PlatformUserDto>(HttpMethod.Put, $"/api/v1/platform/users/{id}", request, ct);
    public Task<ApiCallResult<PlatformUserDto>> SuspendUserAsync(Guid id, string? reason = null, CancellationToken ct = default) =>
        SendAsync<PlatformUserDto>(HttpMethod.Post, $"/api/v1/platform/users/{id}/suspend", new LifecycleReasonRequest(reason), ct);
    public Task<ApiCallResult<PlatformUserDto>> ReactivateUserAsync(Guid id, CancellationToken ct = default) =>
        SendAsync<PlatformUserDto>(HttpMethod.Post, $"/api/v1/platform/users/{id}/reactivate", null, ct);
    public Task<ApiCallResult<PlatformUserDto>> DisableUserAsync(Guid id, CancellationToken ct = default) =>
        SendAsync<PlatformUserDto>(HttpMethod.Post, $"/api/v1/platform/users/{id}/disable", null, ct);

    public Task<ApiCallResult<PagedResult<OrganizationMembershipDto>>> GetOrganizationMembersAsync(Guid organizationId, int page = 1, int pageSize = 20, string? status = null, CancellationToken ct = default) =>
        GetAsync<PagedResult<OrganizationMembershipDto>>($"/api/v1/platform/organizations/{organizationId}/members?{Query(("page", page), ("pageSize", pageSize), ("status", status))}", ct);
    public Task<ApiCallResult<PagedResult<OrganizationMembershipDto>>> GetUserMembershipsAsync(Guid userId, int page = 1, int pageSize = 20, string? status = null, CancellationToken ct = default) =>
        GetAsync<PagedResult<OrganizationMembershipDto>>($"/api/v1/platform/users/{userId}/memberships?{Query(("page", page), ("pageSize", pageSize), ("status", status))}", ct);
    public Task<ApiCallResult<OrganizationMembershipDto>> AddOrganizationMemberAsync(Guid organizationId, AddMemberRequest request, CancellationToken ct = default) =>
        SendAsync<OrganizationMembershipDto>(HttpMethod.Post, $"/api/v1/platform/organizations/{organizationId}/members", request, ct);
    public Task<ApiCallResult<OrganizationMembershipDto>> ChangeMembershipRoleAsync(Guid membershipId, ChangeRoleRequest request, CancellationToken ct = default) =>
        SendAsync<OrganizationMembershipDto>(HttpMethod.Put, $"/api/v1/platform/memberships/{membershipId}/role", request with { ActorReference = request.ActorReference ?? DevActor }, ct);
    public Task<ApiCallResult<OrganizationMembershipDto>> SuspendMembershipAsync(Guid membershipId, MembershipLifecycleRequest request, CancellationToken ct = default) =>
        SendAsync<OrganizationMembershipDto>(HttpMethod.Post, $"/api/v1/platform/memberships/{membershipId}/suspend", WithActor(request), ct);
    public Task<ApiCallResult<OrganizationMembershipDto>> ReactivateMembershipAsync(Guid membershipId, MembershipLifecycleRequest request, CancellationToken ct = default) =>
        SendAsync<OrganizationMembershipDto>(HttpMethod.Post, $"/api/v1/platform/memberships/{membershipId}/reactivate", WithActor(request), ct);
    public Task<ApiCallResult<OrganizationMembershipDto>> RevokeMembershipAsync(Guid membershipId, MembershipLifecycleRequest request, CancellationToken ct = default) =>
        SendAsync<OrganizationMembershipDto>(HttpMethod.Post, $"/api/v1/platform/memberships/{membershipId}/revoke", WithActor(request), ct);

    public Task<ApiCallResult<PagedResult<OrganizationInvitationDto>>> GetOrganizationInvitationsAsync(Guid organizationId, int page = 1, int pageSize = 20, string? status = null, CancellationToken ct = default) =>
        GetAsync<PagedResult<OrganizationInvitationDto>>($"/api/v1/platform/organizations/{organizationId}/invitations?{Query(("page", page), ("pageSize", pageSize), ("status", status))}", ct);
    public Task<ApiCallResult<OrganizationInvitationDto>> CreateOrganizationInvitationAsync(Guid organizationId, CreateInvitationRequest request, CancellationToken ct = default) =>
        SendAsync<OrganizationInvitationDto>(HttpMethod.Post, $"/api/v1/platform/organizations/{organizationId}/invitations", request, ct);
    public Task<ApiCallResult<OrganizationInvitationDto>> ResendOrganizationInvitationAsync(Guid invitationId, CancellationToken ct = default) =>
        SendAsync<OrganizationInvitationDto>(HttpMethod.Post, $"/api/v1/platform/invitations/{invitationId}/resend", null, ct);
    public Task<ApiCallResult<OrganizationInvitationDto>> RevokeOrganizationInvitationAsync(Guid invitationId, CancellationToken ct = default) =>
        SendAsync<OrganizationInvitationDto>(HttpMethod.Post, $"/api/v1/platform/invitations/{invitationId}/revoke", null, ct);
    public Task<ApiCallResult<OrganizationMembershipDto>> AcceptOrganizationInvitationAsync(string token, CancellationToken ct = default) =>
        SendAsync<OrganizationMembershipDto>(HttpMethod.Post, "/api/v1/platform/invitations/accept", new { token }, ct);

    public Task<ApiCallResult<PagedResult<ProductAccessAssignmentDto>>> GetOrganizationProductAccessAsync(Guid organizationId, int page = 1, int pageSize = 20, string? status = null, CancellationToken ct = default) =>
        GetAsync<PagedResult<ProductAccessAssignmentDto>>($"/api/v1/platform/organizations/{organizationId}/product-access?{Query(("page", page), ("pageSize", pageSize), ("status", status))}", ct);
    public Task<ApiCallResult<PagedResult<ProductAccessAssignmentDto>>> GetUserProductAccessAsync(Guid userId, int page = 1, int pageSize = 20, string? status = null, CancellationToken ct = default) =>
        GetAsync<PagedResult<ProductAccessAssignmentDto>>($"/api/v1/platform/users/{userId}/product-access?{Query(("page", page), ("pageSize", pageSize), ("status", status))}", ct);
    public Task<ApiCallResult<ProductAccessAssignmentDto>> GrantProductAccessAsync(Guid organizationId, GrantProductAccessRequest request, CancellationToken ct = default) =>
        SendAsync<ProductAccessAssignmentDto>(HttpMethod.Post, $"/api/v1/platform/organizations/{organizationId}/product-access",
            request with { GrantedByActor = string.IsNullOrWhiteSpace(request.GrantedByActor) ? DevActor : request.GrantedByActor }, ct);
    public Task<ApiCallResult<ProductAccessAssignmentDto>> RevokeProductAccessAsync(Guid assignmentId, RevokeProductAccessRequest request, CancellationToken ct = default) =>
        SendAsync<ProductAccessAssignmentDto>(HttpMethod.Post, $"/api/v1/platform/product-access/{assignmentId}/revoke",
            request with { RevokedByActor = string.IsNullOrWhiteSpace(request.RevokedByActor) ? DevActor : request.RevokedByActor }, ct);
    public Task<ApiCallResult<EffectiveProductAccessResultDto>> EvaluateAccessAsync(Guid userId, Guid organizationId, string productCode, CancellationToken ct = default) =>
        GetAsync<EffectiveProductAccessResultDto>($"/api/v1/platform/access/evaluate?{Query(("userId", userId), ("organizationId", organizationId), ("productCode", productCode))}", ct);

    public Task<ApiCallResult<PagedResult<SubscriptionDto>>> GetOrganizationSubscriptionsAsync(Guid organizationId, string? status = null, int page = 1, int pageSize = 20, CancellationToken ct = default) =>
        GetAsync<PagedResult<SubscriptionDto>>($"/api/v1/platform/organizations/{organizationId}/subscriptions?{Query(("status", status), ("page", page), ("pageSize", pageSize))}", ct);
    public Task<ApiCallResult<SubscriptionDto>> GetCurrentSubscriptionAsync(Guid organizationId, string productCode, CancellationToken ct = default) =>
        GetAsync<SubscriptionDto>($"/api/v1/platform/organizations/{organizationId}/subscriptions/current?productCode={Escape(productCode)}", ct);
    public Task<ApiCallResult<SubscriptionDto>> StartTrialAsync(Guid organizationId, StartTrialRequest request, CancellationToken ct = default) =>
        SendAsync<SubscriptionDto>(HttpMethod.Post, $"/api/v1/platform/organizations/{organizationId}/subscriptions/trials", request, ct);
    public Task<ApiCallResult<SubscriptionDto>> ActivateSubscriptionAsync(Guid subscriptionId, ActivateSubscriptionRequest request, CancellationToken ct = default) =>
        SendAsync<SubscriptionDto>(HttpMethod.Post, $"/api/v1/platform/subscriptions/{subscriptionId}/activate", request, ct);
    public Task<ApiCallResult<SubscriptionDto>> EnterGracePeriodAsync(Guid subscriptionId, GracePeriodRequest request, CancellationToken ct = default) =>
        SendAsync<SubscriptionDto>(HttpMethod.Post, $"/api/v1/platform/subscriptions/{subscriptionId}/grace-period", request, ct);
    public Task<ApiCallResult<SubscriptionDto>> MarkPastDueAsync(Guid subscriptionId, CancellationToken ct = default) =>
        SendAsync<SubscriptionDto>(HttpMethod.Post, $"/api/v1/platform/subscriptions/{subscriptionId}/past-due", null, ct);
    public Task<ApiCallResult<SubscriptionDto>> SuspendSubscriptionAsync(Guid subscriptionId, CancellationToken ct = default) =>
        SendAsync<SubscriptionDto>(HttpMethod.Post, $"/api/v1/platform/subscriptions/{subscriptionId}/suspend", null, ct);
    public Task<ApiCallResult<SubscriptionDto>> ReactivateSubscriptionAsync(Guid subscriptionId, ReactivateSubscriptionRequest request, CancellationToken ct = default) =>
        SendAsync<SubscriptionDto>(HttpMethod.Post, $"/api/v1/platform/subscriptions/{subscriptionId}/reactivate", request, ct);
    public Task<ApiCallResult<SubscriptionDto>> CancelSubscriptionAsync(Guid subscriptionId, CancellationToken ct = default) =>
        SendAsync<SubscriptionDto>(HttpMethod.Post, $"/api/v1/platform/subscriptions/{subscriptionId}/cancel", null, ct);
    public Task<ApiCallResult<SubscriptionDto>> ExpireSubscriptionAsync(Guid subscriptionId, CancellationToken ct = default) =>
        SendAsync<SubscriptionDto>(HttpMethod.Post, $"/api/v1/platform/subscriptions/{subscriptionId}/expire", null, ct);

    public Task<ApiCallResult<PaymentDto>> CreateManualPaymentAsync(CreateManualPaymentRequest request, CancellationToken ct = default) =>
        SendAsync<PaymentDto>(HttpMethod.Post, "/api/v1/platform/payments/manual", request, ct);
    public Task<ApiCallResult<PaymentDto>> ConfirmPaymentAsync(Guid paymentId, ConfirmPaymentRequest request, CancellationToken ct = default) =>
        SendAsync<PaymentDto>(HttpMethod.Post, $"/api/v1/platform/payments/{paymentId}/confirm",
            request with { ConfirmedBy = string.IsNullOrWhiteSpace(request.ConfirmedBy) ? DevActor : request.ConfirmedBy }, ct);
    public Task<ApiCallResult<PaymentDto>> RejectPaymentAsync(Guid paymentId, RejectPaymentRequest request, CancellationToken ct = default) =>
        SendAsync<PaymentDto>(HttpMethod.Post, $"/api/v1/platform/payments/{paymentId}/reject",
            request with { RejectedBy = string.IsNullOrWhiteSpace(request.RejectedBy) ? DevActor : request.RejectedBy }, ct);
    public Task<ApiCallResult<PaymentDto>> VoidPaymentAsync(Guid paymentId, VoidPaymentRequest request, CancellationToken ct = default) =>
        SendAsync<PaymentDto>(HttpMethod.Post, $"/api/v1/platform/payments/{paymentId}/void",
            request with { VoidedBy = string.IsNullOrWhiteSpace(request.VoidedBy) ? DevActor : request.VoidedBy }, ct);
    public Task<ApiCallResult<PaymentActivationResultDto>> ConfirmPaymentAndActivateAsync(Guid paymentId, ActivateSubscriptionForPaymentRequest request, CancellationToken ct = default) =>
        SendAsync<PaymentActivationResultDto>(HttpMethod.Post, $"/api/v1/platform/payments/{paymentId}/activate-subscription",
            request with { ConfirmedBy = string.IsNullOrWhiteSpace(request.ConfirmedBy) ? DevActor : request.ConfirmedBy }, ct);

    public Task<ApiCallResult<PagedResult<AuditRecordDto>>> GetAuditRecordsAsync(AuditQuery query, CancellationToken ct = default) =>
        GetAsync<PagedResult<AuditRecordDto>>($"/api/v1/platform/audit?{Query(
            ("fromUtc", query.OccurredFromUtc?.ToString("o")),
            ("toUtc", query.OccurredToUtc?.ToString("o")),
            ("actor", query.ActorIdentifier),
            ("action", query.ActionCode),
            ("organizationId", query.OrganizationId),
            ("productCode", query.ProductCode),
            ("outcome", query.Outcome),
            ("correlationId", query.CorrelationId),
            ("page", query.Page),
            ("pageSize", query.PageSize))}", ct);
    public Task<ApiCallResult<AuditRecordDto>> GetAuditRecordAsync(Guid id, CancellationToken ct = default) =>
        GetAsync<AuditRecordDto>($"/api/v1/platform/audit/{id}", ct);
    public Task<ApiCallResult<ResolvedPermissionsDto>> GetMyAuthorizationAsync(Guid? organizationId = null, CancellationToken ct = default) =>
        GetAsync<ResolvedPermissionsDto>($"/api/v1/platform/authorization/me?{Query(("organizationId", organizationId))}", ct);
    public Task<ApiCallResult<IReadOnlyList<PlatformRoleCatalogEntryDto>>> GetAuthorizationRolesAsync(CancellationToken ct = default) =>
        GetAsync<IReadOnlyList<PlatformRoleCatalogEntryDto>>("/api/v1/platform/authorization/roles", ct);

    public Task<ApiCallResult<PlatformCredentialStatusDto>> GetUserCredentialsAsync(Guid userId, CancellationToken ct = default) =>
        GetAsync<PlatformCredentialStatusDto>($"/api/v1/platform/users/{userId}/credentials", ct);
    public Task<ApiCallResult<PlatformCredentialStatusDto>> SetUserPasswordAsync(Guid userId, SetUserPasswordRequest request, CancellationToken ct = default) =>
        SendAsync<PlatformCredentialStatusDto>(HttpMethod.Put, $"/api/v1/platform/users/{userId}/credentials/password", request, ct);
    public Task<ApiCallResult<PlatformCredentialStatusDto>> UnlockUserCredentialAsync(Guid userId, CancellationToken ct = default) =>
        SendAsync<PlatformCredentialStatusDto>(HttpMethod.Post, $"/api/v1/platform/users/{userId}/credentials/unlock", null, ct);
    public Task<ApiCallResult<PlatformCredentialStatusDto>> MarkUserEmailVerifiedAsync(Guid userId, CancellationToken ct = default) =>
        SendAsync<PlatformCredentialStatusDto>(HttpMethod.Post, $"/api/v1/platform/users/{userId}/credentials/email-verified", null, ct);
    public Task<ApiCallResult<PlatformCredentialStatusDto>> ChangePasswordAsync(ChangePasswordRequest request, CancellationToken ct = default) =>
        SendAsync<PlatformCredentialStatusDto>(HttpMethod.Post, "/api/v1/platform/auth/change-password", request, ct);
    public Task<ApiCallResult<CredentialWorkflowAckDto>> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken ct = default) =>
        SendAsync<CredentialWorkflowAckDto>(HttpMethod.Post, "/api/v1/platform/auth/forgot-password", request, ct);
    public Task<ApiCallResult<PlatformCredentialStatusDto>> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct = default) =>
        SendAsync<PlatformCredentialStatusDto>(HttpMethod.Post, "/api/v1/platform/auth/reset-password", request, ct);
    public Task<ApiCallResult<CredentialWorkflowAckDto>> RequestEmailVerificationAsync(CancellationToken ct = default) =>
        SendAsync<CredentialWorkflowAckDto>(HttpMethod.Post, "/api/v1/platform/auth/email-verification/request", null, ct);
    public Task<ApiCallResult<PlatformCredentialStatusDto>> ConfirmEmailVerificationAsync(ConfirmEmailVerificationRequest request, CancellationToken ct = default) =>
        SendAsync<PlatformCredentialStatusDto>(HttpMethod.Post, "/api/v1/platform/auth/email-verification/confirm", request, ct);
    public Task<ApiCallResult<PlatformCredentialStatusDto>> GetMyCredentialsAsync(CancellationToken ct = default) =>
        GetAsync<PlatformCredentialStatusDto>("/api/v1/platform/auth/credentials", ct);
    public Task<ApiCallResult<CredentialWorkflowAckDto>> RequestRecoveryEmailAsync(RequestRecoveryEmailRequest request, CancellationToken ct = default) =>
        SendAsync<CredentialWorkflowAckDto>(HttpMethod.Post, "/api/v1/platform/auth/recovery-email/request", request, ct);
    public Task<ApiCallResult<PlatformCredentialStatusDto>> ConfirmRecoveryEmailAsync(ConfirmRecoveryEmailRequest request, CancellationToken ct = default) =>
        SendAsync<PlatformCredentialStatusDto>(HttpMethod.Post, "/api/v1/platform/auth/recovery-email/confirm", request, ct);
    public Task<ApiCallResult<PlatformCredentialStatusDto>> SkipRecoveryEmailAsync(CancellationToken ct = default) =>
        SendAsync<PlatformCredentialStatusDto>(HttpMethod.Post, "/api/v1/platform/auth/recovery-email/skip", null, ct);
    public Task<ApiCallResult<PlatformCredentialStatusDto>> ClearRecoveryEmailAsync(CancellationToken ct = default) =>
        SendAsync<PlatformCredentialStatusDto>(HttpMethod.Post, "/api/v1/platform/auth/recovery-email/clear", null, ct);

    public Task<ApiCallResult<AuthSessionInfoDto>> GetAuthMeAsync(CancellationToken ct = default) =>
        GetAsync<AuthSessionInfoDto>("/api/v1/platform/auth/me", ct);

    public Task<ApiCallResult<IReadOnlyList<EligibleOrganizationDto>>> GetEligibleOrganizationsAsync(CancellationToken ct = default) =>
        GetAsync<IReadOnlyList<EligibleOrganizationDto>>("/api/v1/platform/auth/organizations", ct);

    public Task<ApiCallResult<OrganizationContextResultDto>> SetOrganizationContextAsync(SetOrganizationContextRequest request, CancellationToken ct = default) =>
        SendAsync<OrganizationContextResultDto>(HttpMethod.Put, "/api/v1/platform/auth/organization-context", request, ct);

    public Task<ApiCallResult<AccessTokenIssueDto>> IssueAccessTokenAsync(IssueAccessTokenRequest request, CancellationToken ct = default) =>
        SendAsync<AccessTokenIssueDto>(HttpMethod.Post, "/api/v1/platform/auth/token", request, ct);

    public Task<ApiCallResult<AccessTokenIntrospectionDto>> IntrospectAccessTokenAsync(IntrospectAccessTokenRequest request, CancellationToken ct = default) =>
        SendAsync<AccessTokenIntrospectionDto>(HttpMethod.Post, "/api/v1/platform/auth/introspect", request, ct);

    private static MembershipLifecycleRequest WithActor(MembershipLifecycleRequest request) =>
        request with { ActorReference = string.IsNullOrWhiteSpace(request.ActorReference) ? DevActor : request.ActorReference };

    private async Task<ApiCallResult<T>> GetAsync<T>(string path, CancellationToken ct) =>
        await SendAsync<T>(HttpMethod.Get, path, null, ct).ConfigureAwait(false);

    private async Task<ApiCallResult<T>> SendAsync<T>(HttpMethod method, string path, object? body, CancellationToken ct)
    {
        try
        {
            using var request = new HttpRequestMessage(method, path);
            if (body is not null)
            {
                request.Content = JsonContent.Create(body);
            }

            var sessionToken = await ResolveSessionTokenAsync().ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(sessionToken)
                && !request.Headers.Contains(SessionTokenHeader))
            {
                request.Headers.TryAddWithoutValidation(SessionTokenHeader, sessionToken);
            }

            using var response = await httpClient.SendAsync(request, ct).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<T>(JsonOptions, ct).ConfigureAwait(false);
                return data is null
                    ? ApiCallResult<T>.Failed(new PlatformApiException(response.StatusCode, "Invalid API response", "The API returned no content."))
                    : ApiCallResult<T>.Success(data);
            }

            var error = await ToExceptionAsync(response, ct).ConfigureAwait(false);
            return response.StatusCode switch
            {
                HttpStatusCode.NotFound => ApiCallResult<T>.NotFound(error),
                HttpStatusCode.BadRequest or HttpStatusCode.UnprocessableEntity => ApiCallResult<T>.Validation(error),
                HttpStatusCode.Conflict => ApiCallResult<T>.Failed(error),
                _ => ApiCallResult<T>.Failed(error)
            };
        }
        catch (HttpRequestException ex) { return ApiCallResult<T>.Unavailable(new PlatformApiException(null, "Platform API unavailable", ex.Message, innerException: ex)); }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested) { return ApiCallResult<T>.Unavailable(new PlatformApiException(null, "Platform API timed out", ex.Message, innerException: ex)); }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
    }

    private static async Task<PlatformApiException> ToExceptionAsync(HttpResponseMessage response, CancellationToken ct)
    {
        string? title = null, detail = null;
        try
        {
            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false));
            title = document.RootElement.TryGetProperty("title", out var titleElement) ? titleElement.GetString() : null;
            detail = document.RootElement.TryGetProperty("detail", out var detailElement) ? detailElement.GetString() : null;
        }
        catch (JsonException) { }
        response.Headers.TryGetValues("X-Correlation-ID", out var ids);
        return new PlatformApiException(response.StatusCode, title ?? response.ReasonPhrase ?? "Platform API request failed", detail, ids?.FirstOrDefault());
    }

    private async Task<string?> ResolveSessionTokenAsync()
    {
        if (!string.IsNullOrWhiteSpace(circuitSession.SessionToken))
        {
            return circuitSession.SessionToken;
        }

        try
        {
            var fromHttp = httpContextAccessor.HttpContext is { } http
                ? PlatformBrowserSessionService.ResolveSessionToken(http)
                : null;
            if (!string.IsNullOrWhiteSpace(fromHttp))
            {
                circuitSession.SessionToken = fromHttp;
                return fromHttp;
            }
        }
        catch
        {
            // HttpContext can be disposed mid-circuit.
        }

        try
        {
            var state = await authenticationStateProvider.GetAuthenticationStateAsync().ConfigureAwait(false);
            var fromAuth = state.User.FindFirstValue(PlatformBrowserSessionService.SessionTokenClaimType);
            if (!string.IsNullOrWhiteSpace(fromAuth))
            {
                circuitSession.SessionToken = fromAuth;
            }

            return fromAuth;
        }
        catch
        {
            return circuitSession.SessionToken;
        }
    }

    private static string Escape(string value) => Uri.EscapeDataString(value);
    private static string Query(params (string Key, object? Value)[] values) =>
        string.Join("&", values.Where(v => v.Value is not null && !string.IsNullOrWhiteSpace(v.Value.ToString())).Select(v => $"{Escape(v.Key)}={Escape(v.Value!.ToString()!)}"));
}
