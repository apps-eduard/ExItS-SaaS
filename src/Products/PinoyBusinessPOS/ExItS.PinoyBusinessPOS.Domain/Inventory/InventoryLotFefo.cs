using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Inventory;

/// <summary>
/// First Expire, First Out allocation for expiration-tracked products.
/// Expired lots are never allocated for sale.
/// </summary>
public static class InventoryLotFefo
{
    public static IReadOnlyList<InventoryLotAllocation> AllocateSellable(
        IReadOnlyList<InventoryLot> lots,
        decimal quantity,
        DateOnly today)
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
                     .Where(l => l.IsSellable(today))
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
                "Insufficient non-expired stock for this quantity.");
        }

        return result;
    }

    public static decimal SellableQuantity(IEnumerable<InventoryLot> lots, DateOnly today) =>
        lots.Where(l => l.IsSellable(today)).Sum(l => l.QuantityOnHand);

    public static decimal ExpiredQuantity(IEnumerable<InventoryLot> lots, DateOnly today) =>
        lots.Where(l => l.IsExpired(today)).Sum(l => l.QuantityOnHand);

    public static decimal NearExpiryQuantity(IEnumerable<InventoryLot> lots, DateOnly today, int warningDays) =>
        lots.Where(l => l.IsNearExpiry(today, warningDays)).Sum(l => l.QuantityOnHand);

    public static decimal TotalOnHand(IEnumerable<InventoryLot> lots) =>
        lots.Sum(l => l.QuantityOnHand);
}

public sealed record InventoryLotAllocation(InventoryLot Lot, decimal Quantity);
