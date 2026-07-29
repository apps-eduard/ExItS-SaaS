using ExItS.Platform.Domain.Catalog;
using ExItS.Platform.Domain.Common;

namespace ExItS.Platform.Domain.Entitlements;

/// <summary>One feature assignment in a snapshot or composition result.</summary>
public sealed class EntitlementGrant
{
    public FeatureCode FeatureCode { get; }
    public bool Enabled { get; }
    public int? NumericLimit { get; }
    public EntitlementGrantSource Source { get; }
    public DateTimeOffset EffectiveAtUtc { get; }
    public DateTimeOffset? ExpiresAtUtc { get; }

    public EntitlementGrant(
        FeatureCode featureCode,
        bool enabled,
        EntitlementGrantSource source,
        DateTimeOffset effectiveAtUtc,
        int? numericLimit = null,
        DateTimeOffset? expiresAtUtc = null)
    {
        ArgumentNullException.ThrowIfNull(featureCode);
        DomainTime.EnsureUtc(effectiveAtUtc);
        if (expiresAtUtc is not null)
        {
            DomainTime.EnsureUtc(expiresAtUtc.Value);
        }

        if (numericLimit is < 0)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidEntitlementLimit,
                "Numeric limits cannot be negative.");
        }

        if (!Enum.IsDefined(source))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidFeatureValueType,
                "Entitlement grant source is not defined.");
        }

        FeatureCode = featureCode;
        Enabled = enabled;
        NumericLimit = numericLimit;
        Source = source;
        EffectiveAtUtc = effectiveAtUtc;
        ExpiresAtUtc = expiresAtUtc;
    }

    public bool IsActiveAt(DateTimeOffset utcNow)
    {
        DomainTime.EnsureUtc(utcNow);
        if (ExpiresAtUtc is not null && utcNow >= ExpiresAtUtc.Value)
        {
            return false;
        }

        return utcNow >= EffectiveAtUtc;
    }
}
