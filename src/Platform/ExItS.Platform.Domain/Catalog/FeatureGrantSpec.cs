using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Products;

namespace ExItS.Platform.Domain.Catalog;

/// <summary>One commercial feature grant inside a plan version or trial (not an operational setting).</summary>
public sealed class FeatureGrantSpec
{
    public FeatureCode FeatureCode { get; }
    public bool Enabled { get; }
    public int? NumericLimit { get; }

    public FeatureGrantSpec(FeatureCode featureCode, bool enabled, int? numericLimit = null)
    {
        ArgumentNullException.ThrowIfNull(featureCode);
        if (numericLimit is < 0)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidEntitlementLimit,
                "Numeric limits cannot be negative.");
        }

        FeatureCode = featureCode;
        Enabled = enabled;
        NumericLimit = numericLimit;
    }

    public static FeatureGrantSpec Boolean(FeatureCode code, bool enabled) =>
        new(code, enabled, null);

    public static FeatureGrantSpec Limit(FeatureCode code, int limit)
    {
        if (limit < 0)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidEntitlementLimit,
                "Numeric limits cannot be negative.");
        }

        return new FeatureGrantSpec(code, enabled: true, numericLimit: limit);
    }
}
