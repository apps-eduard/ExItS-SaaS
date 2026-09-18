using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Returns;

/// <summary>
/// Origin of a return batch. Sale = customer return against a completed POS sale.
/// ConnectedPurchaseOrder = buyer-to-seller return against a completed connected PO.
/// Receipt discrepancy (damage/missing at goods receipt) is never a return batch.
/// </summary>
public enum ReturnBatchSourceType
{
    Sale = 0,
    ConnectedPurchaseOrder = 1
}

public static class ReturnBatchSourceTypes
{
    public const int CodeMaxLength = 32;

    public static IReadOnlyList<string> Codes { get; } =
    [
        nameof(ReturnBatchSourceType.Sale),
        nameof(ReturnBatchSourceType.ConnectedPurchaseOrder)
    ];

    public static string ToCode(ReturnBatchSourceType sourceType) => sourceType.ToString();

    public static bool TryParse(string? code, out ReturnBatchSourceType sourceType)
    {
        sourceType = ReturnBatchSourceType.Sale;
        if (string.IsNullOrWhiteSpace(code))
        {
            return false;
        }

        var match = Codes.FirstOrDefault(c => string.Equals(c, code.Trim(), StringComparison.OrdinalIgnoreCase));
        if (match is null)
        {
            return false;
        }

        sourceType = Enum.Parse<ReturnBatchSourceType>(match, ignoreCase: false);
        return true;
    }

    public static ReturnBatchSourceType Parse(string? code)
    {
        if (!TryParse(code, out var sourceType))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidReturnBatchSourceType,
                $"Return batch source type must be one of: {string.Join(", ", Codes)}.");
        }

        return sourceType;
    }
}
