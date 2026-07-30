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
    StockCountVarianceDecrease = 7
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
        nameof(StockMovementType.StockCountVarianceDecrease)
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
