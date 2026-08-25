namespace ExItS.PinoyBusinessPOS.Api.Common;

/// <summary>HttpContext.Items keys written by <see cref="PosPlatformBearerMiddleware"/>.</summary>
internal static class PosAuthItems
{
    public const string UserId = "PosAuth:UserId";
    public const string OrganizationId = "PosAuth:OrganizationId";
    public const string ProductAccessAllowed = "PosAuth:ProductAccessAllowed";
    public const string SubscriptionStatus = "PosAuth:SubscriptionStatus";
    public const string EnabledFeatureCodes = "PosAuth:EnabledFeatureCodes";
    public const string Denied = "PosAuth:Denied";
    /// <summary>Platform introspection failed due to infrastructure (rate limit / 5xx), not an inactive token.</summary>
    public const string IntrospectionUnavailable = "PosAuth:IntrospectionUnavailable";
    public const string CommercialBound = "PosAuth:CommercialBound";
    public const string ProductLocalRoleCode = "PosAuth:ProductLocalRoleCode";
    public const string MappedPosRoleCode = "PosAuth:MappedPosRoleCode";
    public const string MembershipRole = "PosAuth:MembershipRole";
    public const string OrganizationManagementAuthority = "PosAuth:OrganizationManagementAuthority";
}
