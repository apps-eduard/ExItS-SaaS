namespace ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;

/// <summary>
/// Per-customer override for Delivery allowance.
/// <see cref="Inherit"/> (null in persistence) follows organization Offer Delivery.
/// </summary>
public enum CustomerDeliveryOverride
{
    /// <summary>No override — inherit organization Offer Delivery.</summary>
    Inherit = 0,
    /// <summary>Explicit allow (same effective result as inherit when org Delivery is ON).</summary>
    Allow = 1,
    /// <summary>Explicit block — Delivery unavailable for this customer even when org offers it.</summary>
    Block = 2,
}

/// <summary>
/// Effective buyer Delivery availability for a connected relationship.
/// Does not invent readiness — callers supply org/branch/customer facts.
/// </summary>
public static class EffectiveDeliveryAllowance
{
    /// <summary>
    /// EffectiveDelivery = orgDeliveryEnabled AND ready delivery branch exists AND override != Block.
    /// </summary>
    public static bool IsAllowed(
        bool orgOfferDelivery,
        bool readyDeliveryBranchExists,
        CustomerDeliveryOverride customerOverride) =>
        orgOfferDelivery
        && readyDeliveryBranchExists
        && customerOverride != CustomerDeliveryOverride.Block;

    public static CustomerDeliveryOverride ParseOverride(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return CustomerDeliveryOverride.Inherit;
        }

        return raw.Trim().ToLowerInvariant() switch
        {
            "allow" => CustomerDeliveryOverride.Allow,
            "block" => CustomerDeliveryOverride.Block,
            "inherit" or "null" => CustomerDeliveryOverride.Inherit,
            _ => CustomerDeliveryOverride.Inherit,
        };
    }

    public static string? ToPersistence(CustomerDeliveryOverride value) =>
        value switch
        {
            CustomerDeliveryOverride.Allow => "allow",
            CustomerDeliveryOverride.Block => "block",
            _ => null,
        };
}
