using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Inventory;

public enum StockRequestStatus
{
    Pending = 0,
    /// <summary>Deprecated: prefer <see cref="Preparing"/>. Kept for legacy rows / parsing.</summary>
    InProgress = 1,
    PartiallyFulfilled = 2,
    Fulfilled = 3,
    Rejected = 4,
    Cancelled = 5,
    Approved = 6,
    Preparing = 7,
    InTransit = 8
}

public static class StockRequestStatuses
{
    public const int CodeMaxLength = 32;

    public static IReadOnlyList<string> Codes { get; } =
    [
        nameof(StockRequestStatus.Pending),
        nameof(StockRequestStatus.Approved),
        nameof(StockRequestStatus.Preparing),
        nameof(StockRequestStatus.InTransit),
        nameof(StockRequestStatus.PartiallyFulfilled),
        nameof(StockRequestStatus.Fulfilled),
        nameof(StockRequestStatus.Rejected),
        nameof(StockRequestStatus.Cancelled),
        // Legacy code retained for parse/compat; new writes should use Preparing.
        nameof(StockRequestStatus.InProgress)
    ];

    public static string ToCode(StockRequestStatus status) =>
        status == StockRequestStatus.InProgress
            ? nameof(StockRequestStatus.Preparing)
            : status.ToString();

    public static bool TryParse(string? code, out StockRequestStatus status)
    {
        status = StockRequestStatus.Pending;
        if (string.IsNullOrWhiteSpace(code))
        {
            return false;
        }

        var trimmed = code.Trim();
        if (string.Equals(trimmed, nameof(StockRequestStatus.InProgress), StringComparison.OrdinalIgnoreCase))
        {
            status = StockRequestStatus.Preparing;
            return true;
        }

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
