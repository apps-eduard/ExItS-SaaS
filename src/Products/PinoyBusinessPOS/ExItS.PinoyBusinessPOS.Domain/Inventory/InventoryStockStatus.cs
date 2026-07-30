namespace ExItS.PinoyBusinessPOS.Domain.Inventory;

/// <summary>Derived stock state for tracked inventory accounts. Not persisted.</summary>
public enum InventoryStockStatus
{
    InStock = 0,
    LowStock = 1,
    OutOfStock = 2,
    ReorderSuggested = 3
}

public static class InventoryStockStatuses
{
    public static string ToCode(InventoryStockStatus status) => status.ToString();

    /// <summary>
    /// Primary availability state. <see cref="InventoryStockStatus.ReorderSuggested"/> is not used here —
    /// use <see cref="IsReorderSuggested"/> as a separate derived flag (it overlaps OutOfStock/LowStock).
    /// </summary>
    public static InventoryStockStatus Derive(bool isTracked, decimal onHand, decimal? reorderLevel)
    {
        if (!isTracked)
        {
            return InventoryStockStatus.InStock;
        }

        if (onHand == 0m)
        {
            return InventoryStockStatus.OutOfStock;
        }

        if (reorderLevel is not null && onHand <= reorderLevel.Value)
        {
            return InventoryStockStatus.LowStock;
        }

        return InventoryStockStatus.InStock;
    }

    /// <summary>True when a reorder level is configured and on-hand is at or below that level.</summary>
    public static bool IsReorderSuggested(decimal onHand, decimal? reorderLevel) =>
        reorderLevel is not null && onHand <= reorderLevel.Value;

    /// <summary>
    /// Suggested order quantity: configured <paramref name="reorderQuantity"/> when set;
    /// otherwise shortage to reach <paramref name="reorderLevel"/> (never negative).
    /// </summary>
    public static decimal? SuggestedOrderQuantity(decimal onHand, decimal? reorderLevel, decimal? reorderQuantity)
    {
        if (!IsReorderSuggested(onHand, reorderLevel))
        {
            return null;
        }

        if (reorderQuantity is not null && reorderQuantity.Value > 0m)
        {
            return reorderQuantity.Value;
        }

        var shortage = reorderLevel!.Value - onHand;
        return shortage > 0m ? shortage : null;
    }
}
