using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Inventory;

/// <summary>
/// Sale-eligibility status for one expiration lot under an effective stop-selling policy.
/// Distinct from <see cref="InventoryLot.IsExpired"/> (absolute calendar expiry) and near-expiry warnings.
/// </summary>
public enum InventoryLotSaleEligibilityStatus
{
    Sellable = 0,
    SaleBlockedByExpiryPolicy = 1,
    Expired = 2,
}

/// <summary>
/// Pure sale-eligibility rules for expiration lots.
/// Past-expiry is always non-sellable; StopSellingDaysBeforeExpiry=0 allows the expiry date itself.
/// </summary>
public static class InventoryLotSaleEligibility
{
    public const int DefaultStopSellingDays = 0;
    public const int MinStopSellingDays = 0;
    public const int MaxStopSellingDays = 365;

    public static InventoryLotSaleEligibilityStatus Evaluate(
        DateOnly expirationDate,
        DateOnly businessDate,
        int stopSellingDaysBeforeExpiry)
    {
        var days = NormalizeStopSellingDays(stopSellingDaysBeforeExpiry);
        if (expirationDate < businessDate)
        {
            return InventoryLotSaleEligibilityStatus.Expired;
        }

        if (days > 0 && expirationDate <= businessDate.AddDays(days))
        {
            return InventoryLotSaleEligibilityStatus.SaleBlockedByExpiryPolicy;
        }

        return InventoryLotSaleEligibilityStatus.Sellable;
    }

    public static bool IsSellable(
        DateOnly expirationDate,
        DateOnly businessDate,
        int stopSellingDaysBeforeExpiry) =>
        Evaluate(expirationDate, businessDate, stopSellingDaysBeforeExpiry)
            == InventoryLotSaleEligibilityStatus.Sellable;

    public static bool IsExpired(DateOnly expirationDate, DateOnly businessDate) =>
        expirationDate < businessDate;

    public static bool IsPolicyBlocked(
        DateOnly expirationDate,
        DateOnly businessDate,
        int stopSellingDaysBeforeExpiry) =>
        Evaluate(expirationDate, businessDate, stopSellingDaysBeforeExpiry)
            == InventoryLotSaleEligibilityStatus.SaleBlockedByExpiryPolicy;

    public static int NormalizeStopSellingDays(int? days)
    {
        if (days is null)
        {
            return DefaultStopSellingDays;
        }

        if (days.Value < MinStopSellingDays || days.Value > MaxStopSellingDays)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidStopSellingDaysBeforeExpiry,
                $"Stop-selling days before expiry must be between {MinStopSellingDays} and {MaxStopSellingDays}.");
        }

        return days.Value;
    }
}
