using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Inventory;

/// <summary>Why stock was consumed outside of a sale. Member names are stable persistence codes.</summary>
public enum StockUseReason
{
    InternalOperations = 0,
    StaffUse = 1,
    SampleOrTesting = 2,
    Other = 3
}

public static class StockUseReasons
{
    public const int CodeMaxLength = 32;

    public static IReadOnlyList<string> Codes { get; } =
    [
        nameof(StockUseReason.InternalOperations),
        nameof(StockUseReason.StaffUse),
        nameof(StockUseReason.SampleOrTesting),
        nameof(StockUseReason.Other)
    ];

    public static string ToCode(StockUseReason reason) => reason.ToString();

    public static bool TryParse(string? code, out StockUseReason reason)
    {
        reason = StockUseReason.Other;
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

        reason = Enum.Parse<StockUseReason>(match, ignoreCase: false);
        return true;
    }

    public static StockUseReason Parse(string? code)
    {
        if (!TryParse(code, out var reason))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidStockUseReason,
                $"Stock use reason must be one of: {string.Join(", ", Codes)}.");
        }

        return reason;
    }
}
