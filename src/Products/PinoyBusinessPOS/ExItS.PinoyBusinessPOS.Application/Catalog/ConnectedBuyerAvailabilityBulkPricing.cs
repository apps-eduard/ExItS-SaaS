using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.Application.Catalog;

public enum ConnectedBuyerAvailabilityPricingMode
{
    SetFromRetail = 0,
    DiscountFromRetailPercent = 1,
    AdjustFromRetailAmount = 2,
    FixedPrice = 3
}

/// <summary>
/// Explicit Level-1 Default PO pricing rules. Baseline is each product's current retail SellingPrice
/// (or an exact fixed value). This is NOT the Level-2 buyer-specific pricing helper.
/// </summary>
public static class ConnectedBuyerAvailabilityBulkPricing
{
    public const int MaxBulkProductIds = 500;
    public const int MaxSelectAllMatching = 5_000;
    public const int PreviewItemLimit = 50;

    public static bool TryComputeDefaultPoPrice(
        ConnectedBuyerAvailabilityPricingMode mode,
        decimal sellingPrice,
        decimal? percent,
        decimal? amount,
        decimal? fixedPrice,
        out decimal defaultPoPrice,
        out string? error)
    {
        defaultPoPrice = 0m;
        error = null;
        var retail = SaleMoney.RoundMoney(sellingPrice);

        switch (mode)
        {
            case ConnectedBuyerAvailabilityPricingMode.SetFromRetail:
                if (retail <= 0m)
                {
                    error = "Retail selling price must be greater than zero.";
                    return false;
                }

                defaultPoPrice = retail;
                return true;
            case ConnectedBuyerAvailabilityPricingMode.DiscountFromRetailPercent:
            {
                if (retail <= 0m)
                {
                    error = "Retail selling price must be greater than zero.";
                    return false;
                }

                if (percent is null || percent < 0m || percent > 100m)
                {
                    error = "Discount percent must be between 0 and 100.";
                    return false;
                }

                var discounted = SaleMoney.RoundMoney(retail * (1m - (percent.Value / 100m)));
                if (discounted <= 0m)
                {
                    error = "Discount would produce an invalid Default PO price.";
                    return false;
                }

                defaultPoPrice = discounted;
                return true;
            }
            case ConnectedBuyerAvailabilityPricingMode.AdjustFromRetailAmount:
            {
                if (amount is null)
                {
                    error = "Adjustment amount is required.";
                    return false;
                }

                var adjusted = SaleMoney.RoundMoney(retail + amount.Value);
                if (adjusted <= 0m)
                {
                    error = "Adjustment would produce an invalid Default PO price.";
                    return false;
                }

                defaultPoPrice = adjusted;
                return true;
            }
            case ConnectedBuyerAvailabilityPricingMode.FixedPrice:
            {
                if (fixedPrice is null)
                {
                    error = "Fixed price is required.";
                    return false;
                }

                var money = SaleMoney.RoundMoney(fixedPrice.Value);
                if (money <= 0m)
                {
                    error = "Default PO price must be greater than zero.";
                    return false;
                }

                defaultPoPrice = money;
                return true;
            }
            default:
                error = "Unsupported pricing mode.";
                return false;
        }
    }
}
