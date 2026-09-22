using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Returns;

public enum ReturnBatchStatus
{
    PendingInspection = 0,
    ReadyForFinalize = 1,
    Finalized = 2,
    /// <summary>
    /// Connected-PO only: buyer has requested the return and physically handed off goods,
    /// but the seller has not yet confirmed physical receipt. Inspection cannot start yet.
    /// </summary>
    AwaitingSellerReceipt = 3
}

public static class ReturnBatchStatuses
{
    public const int CodeMaxLength = 32;

    public static IReadOnlyList<string> Codes { get; } =
    [
        nameof(ReturnBatchStatus.PendingInspection),
        nameof(ReturnBatchStatus.ReadyForFinalize),
        nameof(ReturnBatchStatus.Finalized),
        nameof(ReturnBatchStatus.AwaitingSellerReceipt)
    ];

    public static string ToCode(ReturnBatchStatus status) => status.ToString();

    public static bool TryParse(string? code, out ReturnBatchStatus status)
    {
        status = ReturnBatchStatus.PendingInspection;
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

        status = Enum.Parse<ReturnBatchStatus>(match, ignoreCase: false);
        return true;
    }

    public static ReturnBatchStatus Parse(string? code)
    {
        if (!TryParse(code, out var status))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidReturnBatchStatus,
                $"Return batch status must be one of: {string.Join(", ", Codes)}.");
        }

        return status;
    }
}
