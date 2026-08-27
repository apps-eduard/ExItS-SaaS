namespace ExItS.PinoyBuyNowPayLater.Domain;

/// <summary>
/// Product identity literal for BNPL. Matches Platform catalog code
/// without referencing Platform Domain (isolation).
/// </summary>
public static class BnplProductIdentity
{
    public const string ProductCode = "pinoy-buy-now-pay-later";
}
