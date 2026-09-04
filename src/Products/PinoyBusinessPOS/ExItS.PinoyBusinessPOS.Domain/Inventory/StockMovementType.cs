using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Inventory;

/// <summary>
/// Immutable stock movement kinds for basic inventory. Member names are stable persistence codes.
/// </summary>
public enum StockMovementType
{
    OpeningStock = 0,
    ManualIncrease = 1,
    ManualDecrease = 2,
    SaleDeduction = 3,
    SaleVoidRestoration = 4,
    PurchaseReceipt = 5,
    StockCountVarianceIncrease = 6,
    StockCountVarianceDecrease = 7,
    SaleReturnRestock = 8,
    TransferOut = 9,
    TransferIn = 10,
    TransferCancelRestore = 11,
    DirectPurchaseReceipt = 12,
    /// <summary>
    /// Lot-ledger-only allocation of existing on-hand into expiration lots.
    /// Does not change product <see cref="InventoryAccount.OnHandQuantity"/>;
    /// never written as a product-level <see cref="StockMovement"/>.
    /// </summary>
    ExpirationInitialization = 13,
    /// <summary>Internal / non-sale stock consumption (stock use document).</summary>
    StockUse = 14,
    /// <summary>Compensating restoration when a stock use document is voided.</summary>
    StockUseVoidRestoration = 15,
    /// <summary>Material consumed by a production run.</summary>
    ProductionMaterialConsumption = 16,
    /// <summary>Compensating material restoration when a production run is voided.</summary>
    ProductionMaterialRestoration = 17,
    /// <summary>Finished goods added by a production run.</summary>
    ProductionOutput = 18,
    /// <summary>Compensating output reversal when a production run is voided.</summary>
    ProductionOutputReversal = 19,
    /// <summary>Stock written off as waste/loss (waste/loss document).</summary>
    WasteLoss = 20,
    /// <summary>Compensating restoration when a waste/loss document is voided.</summary>
    WasteLossVoidRestoration = 21,
    /// <summary>Compensating reversal when a PO goods receipt is voided.</summary>
    PurchaseReceiptReversal = 22,
    /// <summary>Compensating reversal when a direct purchase receipt is voided.</summary>
    DirectPurchaseReceiptReversal = 23,
    /// <summary>Supplier stock decrease when a connected purchase order is fulfilled/delivered.</summary>
    ConnectedPurchaseFulfillment = 24
}

public static class StockMovementTypes
{
    public const int CodeMaxLength = 32;

    public static IReadOnlyList<string> Codes { get; } =
    [
        nameof(StockMovementType.OpeningStock),
        nameof(StockMovementType.ManualIncrease),
        nameof(StockMovementType.ManualDecrease),
        nameof(StockMovementType.SaleDeduction),
        nameof(StockMovementType.SaleVoidRestoration),
        nameof(StockMovementType.PurchaseReceipt),
        nameof(StockMovementType.StockCountVarianceIncrease),
        nameof(StockMovementType.StockCountVarianceDecrease),
        nameof(StockMovementType.SaleReturnRestock),
        nameof(StockMovementType.TransferOut),
        nameof(StockMovementType.TransferIn),
        nameof(StockMovementType.TransferCancelRestore),
        nameof(StockMovementType.DirectPurchaseReceipt),
        nameof(StockMovementType.ExpirationInitialization),
        nameof(StockMovementType.StockUse),
        nameof(StockMovementType.StockUseVoidRestoration),
        nameof(StockMovementType.ProductionMaterialConsumption),
        nameof(StockMovementType.ProductionMaterialRestoration),
        nameof(StockMovementType.ProductionOutput),
        nameof(StockMovementType.ProductionOutputReversal),
        nameof(StockMovementType.WasteLoss),
        nameof(StockMovementType.WasteLossVoidRestoration),
        nameof(StockMovementType.PurchaseReceiptReversal),
        nameof(StockMovementType.DirectPurchaseReceiptReversal),
        nameof(StockMovementType.ConnectedPurchaseFulfillment)
    ];

    public static string ToCode(StockMovementType type) => type.ToString();

    public static bool TryParse(string? code, out StockMovementType type)
    {
        type = StockMovementType.OpeningStock;
        if (string.IsNullOrWhiteSpace(code))
        {
            return false;
        }

        var trimmed = code.Trim();
        var match = Codes.FirstOrDefault(c => string.Equals(c, trimmed, StringComparison.OrdinalIgnoreCase));
        if (match is null)
        {
            return false;
        }

        type = Enum.Parse<StockMovementType>(match, ignoreCase: false);
        return true;
    }

    public static StockMovementType Parse(string? code)
    {
        if (!TryParse(code, out var type))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidInventoryMovementType,
                $"Movement type must be one of: {string.Join(", ", Codes)}.");
        }

        return type;
    }
}
