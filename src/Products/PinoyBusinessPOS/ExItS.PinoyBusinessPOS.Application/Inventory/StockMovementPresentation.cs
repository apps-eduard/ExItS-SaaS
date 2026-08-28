using ExItS.PinoyBusinessPOS.Domain.Inventory;

namespace ExItS.PinoyBusinessPOS.Application.Inventory;

/// <summary>
/// Friendly display labels for stock-movement codes. Persistence and API values stay on
/// <see cref="StockMovementType"/> member names.
/// </summary>
public static class StockMovementPresentation
{
    public static string ToFriendlyLabel(string? movementTypeCode)
    {
        if (string.IsNullOrWhiteSpace(movementTypeCode))
        {
            return string.Empty;
        }

        if (!StockMovementTypes.TryParse(movementTypeCode, out var type))
        {
            return movementTypeCode.Trim();
        }

        return type switch
        {
            StockMovementType.ManualIncrease => "Stock added",
            StockMovementType.ManualDecrease => "Stock removed",
            StockMovementType.OpeningStock => "Opening stock",
            StockMovementType.SaleDeduction => "Sold",
            StockMovementType.SaleVoidRestoration => "Sale voided",
            StockMovementType.PurchaseReceipt => "Purchase received",
            StockMovementType.StockCountVarianceIncrease => "Count increase",
            StockMovementType.StockCountVarianceDecrease => "Count decrease",
            StockMovementType.SaleReturnRestock => "Return restocked",
            StockMovementType.TransferOut => "Transfer out",
            StockMovementType.TransferIn => "Transfer in",
            StockMovementType.TransferCancelRestore => "Transfer cancelled",
            StockMovementType.DirectPurchaseReceipt => "Direct purchase",
            StockMovementType.ExpirationInitialization => "Expiration initialization",
            _ => type.ToString()
        };
    }
}
