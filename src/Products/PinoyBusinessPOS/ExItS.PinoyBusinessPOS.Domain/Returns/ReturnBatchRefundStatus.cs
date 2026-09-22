using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Returns;

public enum ReturnBatchRefundStatus
{
    None = 0,
    RefundDue = 1,
    Refunded = 2,
    ObligationReduced = 3,
    CreditReduced = 4
}

public static class ReturnBatchRefundStatuses
{
    public const int CodeMaxLength = 32;

    public static IReadOnlyList<string> Codes { get; } =
    [
        nameof(ReturnBatchRefundStatus.None),
        nameof(ReturnBatchRefundStatus.RefundDue),
        nameof(ReturnBatchRefundStatus.Refunded),
        nameof(ReturnBatchRefundStatus.ObligationReduced),
        nameof(ReturnBatchRefundStatus.CreditReduced)
    ];

    public static string ToCode(ReturnBatchRefundStatus status) => status.ToString();

    public static bool TryParse(string? code, out ReturnBatchRefundStatus status)
    {
        status = ReturnBatchRefundStatus.None;
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

        status = Enum.Parse<ReturnBatchRefundStatus>(match, ignoreCase: false);
        return true;
    }

    public static ReturnBatchRefundStatus Parse(string? code)
    {
        if (!TryParse(code, out var status))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidReturnBatchRefundStatus,
                $"Return batch refund status must be one of: {string.Join(", ", Codes)}.");
        }

        return status;
    }
}
