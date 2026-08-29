using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Inventory;

/// <summary>Lifecycle of a stock-use document. Member names are stable persistence codes.</summary>
public enum StockUseStatus
{
    Posted = 0,
    Voided = 1
}

public static class StockUseStatuses
{
    public const int CodeMaxLength = 16;

    public static IReadOnlyList<string> Codes { get; } =
    [
        nameof(StockUseStatus.Posted),
        nameof(StockUseStatus.Voided)
    ];

    public static string ToCode(StockUseStatus status) => status.ToString();

    public static bool TryParse(string? code, out StockUseStatus status)
    {
        status = StockUseStatus.Posted;
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

        status = Enum.Parse<StockUseStatus>(match, ignoreCase: false);
        return true;
    }

    public static StockUseStatus Parse(string? code)
    {
        if (!TryParse(code, out var status))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidStockUseStatus,
                $"Stock use status must be one of: {string.Join(", ", Codes)}.");
        }

        return status;
    }
}
