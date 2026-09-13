using ExItS.PinoyBusinessPOS.Domain.Payments;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.Application.Commercial;

/// <summary>Centralized payment capability checks via feature codes (never planKey compares).</summary>
public static class PaymentCapabilityPolicy
{
    public static string FeatureCodeFor(PaymentCapability capability) =>
        capability switch
        {
            PaymentCapability.BasicPayments => PosFeatureCodes.StoreBasicPayments,
            PaymentCapability.PaymentManagement => PosFeatureCodes.StorePaymentManagement,
            PaymentCapability.OnlinePayments => PosFeatureCodes.StoreOnlinePayments,
            _ => throw new ArgumentOutOfRangeException(nameof(capability), capability, null)
        };

    public static bool HasCapability(
        PaymentCapability capability,
        string? subscriptionStatus,
        IReadOnlyCollection<string>? enabledFeatureCodes)
    {
        if (!UtangCapabilityPolicy.IsFullCommercialState(subscriptionStatus)
            && !UtangCapabilityPolicy.CanEnter(subscriptionStatus, enabledFeatureCodes))
        {
            // Continuity: allow basic payments when POS entry is allowed (same pattern as sales create).
            if (capability != PaymentCapability.BasicPayments)
            {
                return false;
            }
        }

        if (!UtangCapabilityPolicy.CanEnter(subscriptionStatus, enabledFeatureCodes))
        {
            return false;
        }

        return HasFeature(enabledFeatureCodes, FeatureCodeFor(capability));
    }

    /// <summary>
    /// When feature codes are absent (legacy session), BasicPayments is assumed granted;
    /// PaymentManagement / OnlinePayments are denied.
    /// </summary>
    public static bool HasCapabilityOrDefaultBasic(
        PaymentCapability capability,
        string? subscriptionStatus,
        IReadOnlyCollection<string>? enabledFeatureCodes)
    {
        var codes = Normalize(enabledFeatureCodes);
        if (codes.Count == 0)
        {
            return capability == PaymentCapability.BasicPayments
                && UtangCapabilityPolicy.CanEnter(subscriptionStatus, enabledFeatureCodes);
        }

        return HasCapability(capability, subscriptionStatus, enabledFeatureCodes);
    }

    public static bool IsCheckoutMethodAllowedByEntitlement(
        SalePaymentMethod method,
        string? subscriptionStatus,
        IReadOnlyCollection<string>? enabledFeatureCodes)
    {
        var def = PaymentMethodCatalog.Find(SalePaymentMethods.ToCode(method));
        if (def is null || !def.IsCheckoutSaleMethod)
        {
            return false;
        }

        if (def.Availability == PaymentMethodAvailability.ComingSoon
            || def.IntegrationMode == PaymentIntegrationMode.Online)
        {
            return false;
        }

        return HasCapabilityOrDefaultBasic(def.RequiredCapability, subscriptionStatus, enabledFeatureCodes);
    }

    private static bool HasFeature(IReadOnlyCollection<string>? enabledFeatureCodes, string featureCode)
    {
        var codes = Normalize(enabledFeatureCodes);
        return codes.Contains(featureCode);
    }

    private static HashSet<string> Normalize(IReadOnlyCollection<string>? enabledFeatureCodes) =>
        new(
            (enabledFeatureCodes ?? Array.Empty<string>())
                .Select(c => c.Trim().ToLowerInvariant())
                .Where(c => c.Length > 0),
            StringComparer.Ordinal);
}
