using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.Application.ConnectedSuppliers;

public enum BulkBuyerPricingMode
{
    UseDefault = 0,
    DiscountPercent = 1,
    AdjustAmount = 2,
    FixedPrice = 3
}

/// <summary>
/// Pure pricing rules for per-buyer bulk PO price changes.
/// Always bases calculations on Default PO Price (exposure), never retail SellingPrice.
/// </summary>
public static class BuyerProductShareBulkPricing
{
    public const int MaxBulkProductIds = 500;
    public const int MaxSelectAllMatching = 5_000;
    public const int PreviewItemLimit = 50;

    public static bool TryComputeBuyerPrice(
        BulkBuyerPricingMode mode,
        decimal defaultPoPrice,
        decimal? percent,
        decimal? amount,
        decimal? fixedPrice,
        out decimal? buyerSpecificPoPrice,
        out string? error)
    {
        buyerSpecificPoPrice = null;
        error = null;
        var baseline = SaleMoney.RoundMoney(defaultPoPrice);
        if (baseline <= 0m)
        {
            error = "Default PO price must be greater than zero.";
            return false;
        }

        switch (mode)
        {
            case BulkBuyerPricingMode.UseDefault:
                buyerSpecificPoPrice = null;
                return true;
            case BulkBuyerPricingMode.DiscountPercent:
            {
                if (percent is null || percent < 0m || percent > 100m)
                {
                    error = "Discount percent must be between 0 and 100.";
                    return false;
                }

                var discounted = SaleMoney.RoundMoney(baseline * (1m - (percent.Value / 100m)));
                if (discounted <= 0m)
                {
                    error = "Discount would produce an invalid buyer PO price.";
                    return false;
                }

                buyerSpecificPoPrice = discounted;
                return true;
            }
            case BulkBuyerPricingMode.AdjustAmount:
            {
                if (amount is null)
                {
                    error = "Adjustment amount is required.";
                    return false;
                }

                var adjusted = SaleMoney.RoundMoney(baseline + amount.Value);
                if (adjusted <= 0m)
                {
                    error = "Adjustment would produce an invalid buyer PO price.";
                    return false;
                }

                buyerSpecificPoPrice = adjusted;
                return true;
            }
            case BulkBuyerPricingMode.FixedPrice:
            {
                if (fixedPrice is null)
                {
                    error = "Fixed price is required.";
                    return false;
                }

                var money = SaleMoney.RoundMoney(fixedPrice.Value);
                if (money <= 0m)
                {
                    error = "Fixed buyer PO price must be greater than zero.";
                    return false;
                }

                buyerSpecificPoPrice = money;
                return true;
            }
            default:
                error = "Unsupported pricing mode.";
                return false;
        }
    }
}
