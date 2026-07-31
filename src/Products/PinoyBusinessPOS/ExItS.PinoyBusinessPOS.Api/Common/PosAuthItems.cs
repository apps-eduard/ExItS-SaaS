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
    public const string CommercialBound = "PosAuth:CommercialBound";
}
