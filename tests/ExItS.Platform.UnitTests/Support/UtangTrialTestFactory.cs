using ExItS.Platform.Domain.Catalog;
using ExItS.Platform.Domain.Products;

namespace ExItS.Platform.UnitTests.Support;

/// <summary>
/// Test-only helpers for Utang commercial feature grants.
/// Does not hard-code trial length; callers supply duration.
/// </summary>
internal static class UtangTrialTestFactory
{
    public static FeatureGrantSpec[] ActiveGrants() =>
    [
        FeatureGrantSpec.Boolean(FeatureCode.Create(FeatureCode.CustomerCreditView), true),
        FeatureGrantSpec.Boolean(FeatureCode.Create(FeatureCode.CustomerCreditRepay), true),
        FeatureGrantSpec.Boolean(FeatureCode.Create(FeatureCode.CustomerCreditCreate), true)
    ];

    public static FeatureGrantSpec[] PostExpiryGrants() =>
    [
        FeatureGrantSpec.Boolean(FeatureCode.Create(FeatureCode.CustomerCreditView), true),
        FeatureGrantSpec.Boolean(FeatureCode.Create(FeatureCode.CustomerCreditRepay), true),
        FeatureGrantSpec.Boolean(FeatureCode.Create(FeatureCode.CustomerCreditCreate), false)
    ];

    public static TrialDefinition CreateConfigured(
        DateTimeOffset utcNow,
        TimeSpan duration,
        PlanId? planId = null) =>
        TrialDefinition.Create(
            ProductCode.Create(ProductCode.PinoyBusinessPos),
            "Utang Trial",
            duration,
            ActiveGrants(),
            PostExpiryGrants(),
            utcNow,
            planId);
}
