using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Returns;

public enum ReturnBatchAuditEventType
{
    Accepted = 0,
    ClassificationSaved = 1,
    Finalized = 2,
    RefundRecorded = 3,
    /// <summary>Connected-PO only: seller confirmed physical receipt of the returned goods.</summary>
    ReceivedBySeller = 4
}

public static class ReturnBatchAuditEventTypes
{
    public static IReadOnlyList<string> Codes { get; } =
    [
        nameof(ReturnBatchAuditEventType.Accepted),
        nameof(ReturnBatchAuditEventType.ClassificationSaved),
        nameof(ReturnBatchAuditEventType.Finalized),
        nameof(ReturnBatchAuditEventType.RefundRecorded),
        nameof(ReturnBatchAuditEventType.ReceivedBySeller)
    ];

    public static string ToCode(ReturnBatchAuditEventType type) => type.ToString();

    public static ReturnBatchAuditEventType Parse(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidReturnBatchAuditEventType,
                "Return batch audit event type is required.");
        }

        var match = Codes.FirstOrDefault(c => string.Equals(c, code.Trim(), StringComparison.OrdinalIgnoreCase));
        if (match is null)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidReturnBatchAuditEventType,
                $"Return batch audit event type must be one of: {string.Join(", ", Codes)}.");
        }

        return Enum.Parse<ReturnBatchAuditEventType>(match, ignoreCase: false);
    }
}
