using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Inventory;

/// <summary>
/// First Expire, First Out allocation for expiration-tracked products.
/// Normal sales additionally honor <see cref="InventoryLotSaleEligibility"/> stop-selling days.
/// Non-sale physical flows (transfer, stock-use, waste) should pass stopSellingDaysBeforeExpiry=0
/// so only absolute expiry is excluded.
/// </summary>
public static class InventoryLotFefo
{
    public static IReadOnlyList<InventoryLotAllocation> AllocateSellable(
        IReadOnlyList<InventoryLot> lots,
        decimal quantity,
        DateOnly today,
        int stopSellingDaysBeforeExpiry = InventoryLotSaleEligibility.DefaultStopSellingDays)
    {
        if (quantity <= 0m)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidInventoryQuantity,
                "Allocated quantity must be greater than zero.");
        }

        var remaining = quantity;
        var result = new List<InventoryLotAllocation>();
        foreach (var lot in lots
                     .Where(l =>
                         l.QuantityOnHand > 0m
                         && InventoryLotSaleEligibility.IsSellable(
                             l.ExpirationDate,
                             today,
                             stopSellingDaysBeforeExpiry))
                     .OrderBy(l => l.ExpirationDate)
                     .ThenBy(l => l.CreatedAtUtc)
                     .ThenBy(l => l.Id.Value))
        {
            var take = Math.Min(lot.QuantityOnHand, remaining);
            if (take <= 0m)
            {
                continue;
            }

            result.Add(new InventoryLotAllocation(lot, take));
            remaining -= take;
            if (remaining == 0m)
            {
                break;
            }
        }

        if (remaining > 0m)
        {
            throw new DomainException(
                DomainErrorCodes.InventoryInsufficientStock,
                "Insufficient sellable stock for this quantity.");
        }

        return result;
    }

    public static decimal SellableQuantity(
        IEnumerable<InventoryLot> lots,
        DateOnly today,
        int stopSellingDaysBeforeExpiry = InventoryLotSaleEligibility.DefaultStopSellingDays) =>
        lots.Where(l =>
                l.QuantityOnHand > 0m
                && InventoryLotSaleEligibility.IsSellable(l.ExpirationDate, today, stopSellingDaysBeforeExpiry))
            .Sum(l => l.QuantityOnHand);

    public static decimal SalePolicyBlockedQuantity(
        IEnumerable<InventoryLot> lots,
        DateOnly today,
        int stopSellingDaysBeforeExpiry) =>
        lots.Where(l =>
                l.QuantityOnHand > 0m
                && InventoryLotSaleEligibility.IsPolicyBlocked(
                    l.ExpirationDate,
                    today,
                    stopSellingDaysBeforeExpiry))
            .Sum(l => l.QuantityOnHand);

    public static decimal ExpiredQuantity(IEnumerable<InventoryLot> lots, DateOnly today) =>
        lots.Where(l => l.IsExpired(today)).Sum(l => l.QuantityOnHand);

    public static decimal NearExpiryQuantity(IEnumerable<InventoryLot> lots, DateOnly today, int warningDays) =>
        lots.Where(l => l.IsNearExpiry(today, warningDays)).Sum(l => l.QuantityOnHand);

    public static decimal TotalOnHand(IEnumerable<InventoryLot> lots) =>
        lots.Sum(l => l.QuantityOnHand);

    /// <summary>
    /// Lot-partition invariant for expiration-tracked stock:
    /// sellable + policy-blocked + expired = physical lot on-hand.
    /// </summary>
    public static (
        decimal Sellable,
        decimal PolicyBlocked,
        decimal Expired,
        decimal Physical) ProjectSaleBuckets(
        IEnumerable<InventoryLot> lots,
        DateOnly today,
        int stopSellingDaysBeforeExpiry)
    {
        decimal sellable = 0m;
        decimal blocked = 0m;
        decimal expired = 0m;
        decimal physical = 0m;
        foreach (var lot in lots)
        {
            if (lot.QuantityOnHand <= 0m)
            {
                continue;
            }

            physical += lot.QuantityOnHand;
            switch (InventoryLotSaleEligibility.Evaluate(
                        lot.ExpirationDate,
                        today,
                        stopSellingDaysBeforeExpiry))
            {
                case InventoryLotSaleEligibilityStatus.Sellable:
                    sellable += lot.QuantityOnHand;
                    break;
                case InventoryLotSaleEligibilityStatus.SaleBlockedByExpiryPolicy:
                    blocked += lot.QuantityOnHand;
                    break;
                default:
                    expired += lot.QuantityOnHand;
                    break;
            }
        }

        return (sellable, blocked, expired, physical);
    }
}

public sealed record InventoryLotAllocation(InventoryLot Lot, decimal Quantity);
