namespace ExItS.PinoyBuyNowPayLater.Domain.Access;

/// <summary>
/// Product identity literal for BNPL. Matches Platform catalog code
/// without referencing Platform Domain (isolation).
/// </summary>
public static class BnplProductIdentity
{
    public const string ProductCode = "pinoy-buy-now-pay-later";

    public static bool IsPinoyBuyNowPayLater(string? productCode) =>
        !string.IsNullOrWhiteSpace(productCode)
        && string.Equals(productCode.Trim(), ProductCode, StringComparison.OrdinalIgnoreCase);
}
