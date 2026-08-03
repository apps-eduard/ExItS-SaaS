using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Auth;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Platform;

namespace ExItS.PinoyBusinessPOS.Application.Auth;

public sealed class ProductAccessResolver(IPlatformAccessClient accessClient) : IProductAccessResolver
{
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
        var authOrgs = await accessClient.GetAuthEligibleOrganizationsAsync(ct).ConfigureAwait(false);
        if (authOrgs.IsSuccess && authOrgs.Data is not null)
        {
            return authOrgs.Data
                .Select(o => new EligibleOrganization(
                    o.OrganizationId,
                    o.DisplayName,
                    o.MembershipId,
                    "Active",
                    AccessAllowed: true,
                    AccessReasonCode: "allowed"))
                .ToArray();
        }

        var memberships = await accessClient.GetUserMembershipsAsync(userId, ct).ConfigureAwait(false);
        if (!memberships.IsSuccess || memberships.Data is null)
        {
            return Array.Empty<EligibleOrganization>();
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

            // Show only orgs that pass commercial POS access (fail closed for others).
            if (!allowed)
            {
                continue;
            }

            list.Add(new EligibleOrganization(
                membership.OrganizationId,
                displayName,
                membership.Id,
                membership.Status,
                true,
                reason));
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
        ApiCallStatus.Unauthorized => new AuthResult(false, AuthFailureReason.InvalidCredentials, SafeMessageKey: "Auth_InvalidCredentials"),
        ApiCallStatus.Forbidden => new AuthResult(false, AuthFailureReason.AccessDenied, SafeMessageKey: "Access_Denied"),
        ApiCallStatus.Cancelled => new AuthResult(false, AuthFailureReason.Cancelled, SafeMessageKey: "Auth_Cancelled"),
        _ => new AuthResult(false, AuthFailureReason.ApiUnavailable, SafeMessageKey: "Auth_ApiUnavailable")
    };
}
