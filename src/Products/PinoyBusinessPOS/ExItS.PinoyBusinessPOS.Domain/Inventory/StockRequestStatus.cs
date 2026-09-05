using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Inventory;

public enum StockRequestStatus
{
    Pending = 0,
    InProgress = 1,
    PartiallyFulfilled = 2,
    Fulfilled = 3,
    Rejected = 4,
    Cancelled = 5
}

public static class StockRequestStatuses
{
    public const int CodeMaxLength = 32;

    public static IReadOnlyList<string> Codes { get; } =
    [
        nameof(StockRequestStatus.Pending),
        nameof(StockRequestStatus.InProgress),
        nameof(StockRequestStatus.PartiallyFulfilled),
        nameof(StockRequestStatus.Fulfilled),
        nameof(StockRequestStatus.Rejected),
        nameof(StockRequestStatus.Cancelled)
    ];

    public static string ToCode(StockRequestStatus status) => status.ToString();

    public static bool TryParse(string? code, out StockRequestStatus status)
    {
        status = StockRequestStatus.Pending;
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

        status = Enum.Parse<StockRequestStatus>(match, ignoreCase: false);
        return true;
    }

    public static StockRequestStatus Parse(string? code)
    {
        if (!TryParse(code, out var status))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidStockRequestStatus,
                $"Stock request status must be one of: {string.Join(", ", Codes)}.");
        }

        return status;
    }
}
