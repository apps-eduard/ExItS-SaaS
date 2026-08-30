using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Purchasing;

/// <summary>Lifecycle of a goods receipt. Member names are stable persistence codes.</summary>
public enum GoodsReceiptStatus
{
    Posted = 0,
    Voided = 1
}

public static class GoodsReceiptStatuses
{
    public const int CodeMaxLength = 16;

    public static IReadOnlyList<string> Codes { get; } =
    [
        nameof(GoodsReceiptStatus.Posted),
        nameof(GoodsReceiptStatus.Voided)
    ];

    public static string ToCode(GoodsReceiptStatus status) => status.ToString();

    public static bool TryParse(string? code, out GoodsReceiptStatus status)
    {
        status = GoodsReceiptStatus.Posted;
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

        status = Enum.Parse<GoodsReceiptStatus>(match, ignoreCase: false);
        return true;
    }

    public static GoodsReceiptStatus Parse(string? code)
    {
        if (!TryParse(code, out var status))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidGoodsReceiptStatus,
                $"Goods receipt status must be one of: {string.Join(", ", Codes)}.");
        }

        return status;
    }
}
