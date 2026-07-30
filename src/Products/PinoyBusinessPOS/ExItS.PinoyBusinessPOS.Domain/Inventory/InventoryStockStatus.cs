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

    public static InventoryStockStatus Derive(bool isTracked, decimal onHand, decimal? reorderLevel, decimal? reorderQuantity)
    {
        if (!isTracked)
        {
            return InventoryStockStatus.InStock;
        }

        if (onHand == 0m)
        {
            if (IsReorderSuggested(onHand, reorderLevel, reorderQuantity))
            {
                return InventoryStockStatus.ReorderSuggested;
            }

            return InventoryStockStatus.OutOfStock;
        }

        if (reorderLevel is not null && onHand <= reorderLevel.Value)
        {
            if (IsReorderSuggested(onHand, reorderLevel, reorderQuantity))
            {
                return InventoryStockStatus.ReorderSuggested;
            }

            return InventoryStockStatus.LowStock;
        }

        return InventoryStockStatus.InStock;
    }

    public static bool IsReorderSuggested(decimal onHand, decimal? reorderLevel, decimal? reorderQuantity) =>
        reorderLevel is not null
        && reorderQuantity is not null
        && reorderQuantity.Value > 0m
        && onHand <= reorderLevel.Value;
}
