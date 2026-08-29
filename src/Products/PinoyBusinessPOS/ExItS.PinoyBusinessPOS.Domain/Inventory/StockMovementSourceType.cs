using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Inventory;

/// <summary>Origin of a stock movement. Member names are stable persistence codes.</summary>
public enum StockMovementSourceType
{
    None = 0,
    Sale = 1,
    Manual = 2,
    Opening = 3,
    PurchaseReceipt = 4,
    StockCount = 5,
    SaleReturn = 6,
    InventoryTransfer = 7,
    CustomerOrder = 8,
    DirectPurchase = 9,
    StockUse = 10,
    Production = 11
}

public static class StockMovementSourceTypes
{
    public const int CodeMaxLength = 32;

    public static IReadOnlyList<string> Codes { get; } =
    [
        nameof(StockMovementSourceType.None),
        nameof(StockMovementSourceType.Sale),
        nameof(StockMovementSourceType.Manual),
        nameof(StockMovementSourceType.Opening),
        nameof(StockMovementSourceType.PurchaseReceipt),
        nameof(StockMovementSourceType.StockCount),
        nameof(StockMovementSourceType.SaleReturn),
        nameof(StockMovementSourceType.InventoryTransfer),
        nameof(StockMovementSourceType.CustomerOrder),
        nameof(StockMovementSourceType.DirectPurchase),
        nameof(StockMovementSourceType.StockUse),
        nameof(StockMovementSourceType.Production)
    ];

    public static string ToCode(StockMovementSourceType type) => type.ToString();

    public static bool TryParse(string? code, out StockMovementSourceType type)
    {
        type = StockMovementSourceType.None;
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

        type = Enum.Parse<StockMovementSourceType>(match, ignoreCase: false);
        return true;
    }

    public static StockMovementSourceType Parse(string? code)
    {
        if (!TryParse(code, out var type))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidInventorySourceType,
                $"Source type must be one of: {string.Join(", ", Codes)}.");
        }

        return type;
    }
}
