using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Platform;

namespace ExItS.PinoyBusinessPOS.Application.Auth;

public sealed class ProductAccessResolver(IPlatformAccessClient accessClient) : IProductAccessResolver
{
    private static readonly TimeSpan EligibleListCacheTtl = TimeSpan.FromSeconds(45);
    private readonly SemaphoreSlim _listGate = new(1, 1);
    private readonly TimeProvider _clock = TimeProvider.System;
    private Guid? _cachedUserId;
    private IReadOnlyList<EligibleOrganization>? _cachedOrgs;
    private DateTimeOffset _cachedAt;

    public async Task<AuthResult> EvaluateAsync(Guid userId, Guid organizationId, CancellationToken ct = default)
    {
        var result = await accessClient
            .EvaluateAccessAsync(userId, organizationId, PosProductCodes.PinoyBusinessPos, ct)
            .ConfigureAwait(false);

        if (!result.IsSuccess || result.Data is null)
        {
            return MapTransportFailure(result.Status);
        }

        if (!result.Data.Allowed)
        {
            return new AuthResult(false, AuthFailureReason.AccessDenied, SafeMessageKey: MapReasonKey(result.Data.ReasonCode));
        }

        return new AuthResult(true, AuthFailureReason.None);
    }

    public async Task<IReadOnlyList<EligibleOrganization>> ListEligibleOrganizationsAsync(Guid userId, CancellationToken ct = default)
    {
        await _listGate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (_cachedOrgs is not null
                && _cachedUserId == userId
                && _clock.GetUtcNow() - _cachedAt < EligibleListCacheTtl)
            {
                return _cachedOrgs;
            }

            var list = await FetchEligibleOrganizationsAsync(userId, ct).ConfigureAwait(false);
            if (list is null)
            {
                return Array.Empty<EligibleOrganization>();
            }

            _cachedUserId = userId;
            _cachedOrgs = list;
            _cachedAt = _clock.GetUtcNow();
            return list;
        }
        finally
        {
            _listGate.Release();
        }
    }

    /// <summary>
    /// Returns null when both listing paths failed (do not cache a transport miss).
    /// Empty success (no memberships) is cached so login/org-select/header share one round-trip.
    /// </summary>
    private async Task<IReadOnlyList<EligibleOrganization>?> FetchEligibleOrganizationsAsync(
        Guid userId,
        CancellationToken ct)
    {
        var authOrgs = await accessClient.GetAuthEligibleOrganizationsAsync(ct).ConfigureAwait(false);
        if (authOrgs.IsSuccess && authOrgs.Data is not null)
        {
            // Membership eligibility only. POS entitlement is confirmed on SelectOrganization/bind.
            return authOrgs.Data
                .Select(o => new EligibleOrganization(
                    o.OrganizationId,
                    o.DisplayName,
                    o.MembershipId,
                    "Active",
                    AccessAllowed: true,
                    AccessReasonCode: "membership_eligible",
                    MembershipRole: o.MembershipRole))
                .ToArray();
        }

        var memberships = await accessClient.GetUserMembershipsAsync(userId, ct).ConfigureAwait(false);
        if (!memberships.IsSuccess || memberships.Data is null)
        {
            return null;
        }

        var list = new List<EligibleOrganization>();
        foreach (var membership in memberships.Data.Items.Where(m =>
                     string.Equals(m.Status, "Active", StringComparison.OrdinalIgnoreCase)))
        {
            var org = await accessClient.GetOrganizationAsync(membership.OrganizationId, ct).ConfigureAwait(false);
            var displayName = org.IsSuccess && org.Data is not null
                ? org.Data.DisplayName
                : membership.OrganizationId.ToString("D");

            var evaluation = await accessClient
                .EvaluateAccessAsync(userId, membership.OrganizationId, PosProductCodes.PinoyBusinessPos, ct)
                .ConfigureAwait(false);

            var allowed = evaluation.IsSuccess && evaluation.Data?.Allowed == true;
            var reason = evaluation.Data?.ReasonCode ?? "unknown";

            list.Add(new EligibleOrganization(
                membership.OrganizationId,
                displayName,
                membership.Id,
                membership.Status,
                allowed,
                reason,
                membership.Role));
        }

        return list;
    }

    public static string MapReasonKey(string? reasonCode) => reasonCode switch
    {
        "user_inactive" => "Access_UserInactive",
        "organization_inactive" => "Access_OrganizationInactive",
        "membership_missing" => "Access_MembershipMissing",
        "membership_inactive" => "Access_MembershipInactive",
        "product_assignment_missing" => "Access_AssignmentMissing",
        "product_assignment_inactive" => "Access_AssignmentRevoked",
        "product_inactive" => "Access_ProductInactive",
        "subscription_ineligible" => "Access_SubscriptionIneligible",
        "entitlement_missing" => "Access_EntitlementMissing",
        "entitlement_stale" => "Access_EntitlementStale",
        "entitlement_denied" => "Access_EntitlementDenied",
        "product_local_role_missing" => "Access_RoleMissing",
        "product_local_role_inactive" => "Access_RoleMissing",
        "product_role_missing" => "Access_RoleMissing",
        _ => "Access_Denied"
    };

    private static AuthResult MapTransportFailure(ApiCallStatus status) => status switch
    {
        ApiCallStatus.Offline => new AuthResult(false, AuthFailureReason.Offline, SafeMessageKey: "Auth_Offline"),
        ApiCallStatus.Timeout => new AuthResult(false, AuthFailureReason.Timeout, SafeMessageKey: "Auth_Timeout"),
        ApiCallStatus.RateLimited => new AuthResult(false, AuthFailureReason.RateLimited, SafeMessageKey: "Auth_RateLimited"),
        ApiCallStatus.Unauthorized => new AuthResult(false, AuthFailureReason.InvalidCredentials, SafeMessageKey: "Auth_InvalidCredentials"),
        ApiCallStatus.Forbidden => new AuthResult(false, AuthFailureReason.AccessDenied, SafeMessageKey: "Access_Denied"),
        ApiCallStatus.Cancelled => new AuthResult(false, AuthFailureReason.Cancelled, SafeMessageKey: "Auth_Cancelled"),
        _ => new AuthResult(false, AuthFailureReason.ApiUnavailable, SafeMessageKey: "Auth_ApiUnavailable")
    };
}
