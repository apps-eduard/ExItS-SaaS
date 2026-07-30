using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Sales;

/// <summary>
/// Payment methods for a simple retail sale. Exactly one per sale — no split or partial tender.
/// <c>ManualGCash</c> is a manually confirmed transfer: no gateway, QR, or verification is performed.
/// Member names are the stable persistence codes; localized labels live in UI resource files only.
/// </summary>
public enum SalePaymentMethod
{
    Cash = 0,
    ManualGCash = 1
}

public static class SalePaymentMethods
{
    public const int CodeMaxLength = 32;

    /// <summary>Stable persistence codes in canonical display order.</summary>
    public static IReadOnlyList<string> Codes { get; } =
    [
        nameof(SalePaymentMethod.Cash),
        nameof(SalePaymentMethod.ManualGCash)
    ];

    public static string ToCode(SalePaymentMethod method) => method.ToString();

    public static bool TryParse(string? code, out SalePaymentMethod method)
    {
        method = SalePaymentMethod.Cash;
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

        method = Enum.Parse<SalePaymentMethod>(match, ignoreCase: false);
        return true;
    }

    public static SalePaymentMethod Parse(string? code)
    {
        if (!TryParse(code, out var method))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidSalePaymentMethod,
                $"Payment method must be one of: {string.Join(", ", Codes)}.");
        }

        return method;
    }
}
