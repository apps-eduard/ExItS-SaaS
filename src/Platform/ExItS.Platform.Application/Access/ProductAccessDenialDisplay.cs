namespace ExItS.Platform.Application.Access;

/// <summary>
/// Maps technical product-access reason codes to Organization-facing explanations.
/// Internal codes remain on DTOs for support/diagnostics; UI must show the display text.
/// </summary>
public static class ProductAccessDenialDisplay
{
    public static string ToDisplay(string? reasonCode)
    {
        if (string.IsNullOrWhiteSpace(reasonCode)
            || string.Equals(reasonCode, EffectiveAccessReasonCodes.Allowed, StringComparison.OrdinalIgnoreCase))
        {
            return string.Empty;
        }

        return reasonCode.Trim() switch
        {
            EffectiveAccessReasonCodes.ProductLocalRoleMissing =>
                "You do not have a role assigned for this Product.",
            EffectiveAccessReasonCodes.ProductAssignmentMissing =>
                "Product access has not been granted for your account in this Organization.",
            EffectiveAccessReasonCodes.ProductAssignmentInactive =>
                "Your Product access grant is no longer active.",
            EffectiveAccessReasonCodes.EntitlementMissing =>
                "This Organization is not entitled to use this Product.",
            EffectiveAccessReasonCodes.EntitlementDenied =>
                "This Organization’s entitlement for this Product is not active.",
            EffectiveAccessReasonCodes.EntitlementStale =>
                "Product entitlement needs to be refreshed before launch.",
            EffectiveAccessReasonCodes.SubscriptionIneligible =>
                "The Organization subscription does not currently allow this Product.",
            EffectiveAccessReasonCodes.MembershipMissing =>
                "You are not a member of this Organization.",
            EffectiveAccessReasonCodes.MembershipInactive =>
                "Your Organization membership is not active.",
            EffectiveAccessReasonCodes.UserInactive =>
                "Your account is not active.",
            EffectiveAccessReasonCodes.OrganizationInactive =>
                "This Organization is not active.",
            EffectiveAccessReasonCodes.ProductInactive =>
                "This Product is not available.",
            _ => "You cannot open this Product right now."
        };
    }
}
