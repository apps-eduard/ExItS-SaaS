namespace ExItS.PinoyBusinessPOS.Api.Common;

/// <summary>
/// Development/Local Validation commercial entitlement behavior.
/// Production always uses bearer introspection and fails closed; <see cref="IsStrict"/> affects Dev/Testing only.
/// </summary>
internal static class PosCommercialValidation
{
    public const string StrictConfigKey = "CommercialValidation:Strict";

    public static bool IsStrict(IConfiguration configuration) =>
        configuration.GetValue(StrictConfigKey, false);

    /// <summary>
    /// When true, merge Platform introspection grants with the full development grant set.
    /// Disabled in strict mode and in Production.
    /// </summary>
    public static bool ShouldMergeDevelopmentGrants(IHostEnvironment environment, IConfiguration configuration)
    {
        if (IsStrict(configuration))
        {
            return false;
        }

        if (PosDevelopmentEnvironment.IsApprovedDevelopmentEnvironment(environment))
        {
            return true;
        }

        return configuration.GetValue("LocalValidation:Enabled", false) && !environment.IsProduction();
    }

    /// <summary>
    /// When true, missing commercial headers fall back to <see cref="Application.Commercial.PosCommercialAccess.DevelopmentDefault"/>.
    /// </summary>
    public static bool AllowsDevelopmentDefaultHeaders(IHostEnvironment environment, IConfiguration configuration) =>
        PosDevelopmentEnvironment.IsApprovedDevelopmentEnvironment(environment) && !IsStrict(configuration);
}
