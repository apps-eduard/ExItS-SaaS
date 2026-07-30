using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Inventory;

public enum StockCountStatus
{
    Draft = 0,
    InProgress = 1,
    Completed = 2,
    Cancelled = 3
}

public static class StockCountStatuses
{
    public static string ToCode(StockCountStatus status) => status.ToString();

    public static StockCountStatus Parse(string? code)
    {
        if (string.IsNullOrWhiteSpace(code)
            || !Enum.TryParse<StockCountStatus>(code.Trim(), ignoreCase: true, out var status))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidStockCountStatus,
                "Stock count status is invalid.");
        }

        return status;
    }
}
