using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.SupplierPayables;

/// <summary>
/// Posted-receipt origin for a supplier payable. Obligation arises on goods/direct receipt post,
/// not on PO create/submit (PAYABLE_ORIGIN=POSTED_RECEIPT).
/// </summary>
public enum SupplierPayableSourceType
{
    GoodsReceipt = 0,
    DirectPurchaseReceipt = 1
}

public static class SupplierPayableSourceTypes
{
    public const int CodeMaxLength = 32;

    public static IReadOnlyList<string> Codes { get; } =
    [
        nameof(SupplierPayableSourceType.GoodsReceipt),
        nameof(SupplierPayableSourceType.DirectPurchaseReceipt)
    ];

    public static string ToCode(SupplierPayableSourceType sourceType) => sourceType.ToString();

    public static bool TryParse(string? code, out SupplierPayableSourceType sourceType)
    {
        sourceType = SupplierPayableSourceType.GoodsReceipt;
        if (string.IsNullOrWhiteSpace(code))
        {
            return false;
        }

        var match = Codes.FirstOrDefault(c => string.Equals(c, code.Trim(), StringComparison.OrdinalIgnoreCase));
        if (match is null)
        {
            return false;
        }

        sourceType = Enum.Parse<SupplierPayableSourceType>(match, ignoreCase: false);
        return true;
    }

    public static SupplierPayableSourceType Parse(string? code)
    {
        if (!TryParse(code, out var sourceType))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidSupplierPayableSourceType,
                $"Source type must be one of: {string.Join(", ", Codes)}.");
        }

        return sourceType;
    }
}
