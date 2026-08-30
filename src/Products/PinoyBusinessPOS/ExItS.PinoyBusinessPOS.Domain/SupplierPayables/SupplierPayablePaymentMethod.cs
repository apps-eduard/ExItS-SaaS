using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.SupplierPayables;

/// <summary>
/// Settlement method for supplier payables. Record-only — no payment gateway execution.
/// </summary>
public enum SupplierPayablePaymentMethod
{
    Cash = 0,
    BankTransfer = 1,
    GCash = 2,
    Other = 3
}

public static class SupplierPayablePaymentMethods
{
    public const int CodeMaxLength = 32;

    public static IReadOnlyList<string> Codes { get; } =
    [
        nameof(SupplierPayablePaymentMethod.Cash),
        nameof(SupplierPayablePaymentMethod.BankTransfer),
        nameof(SupplierPayablePaymentMethod.GCash),
        nameof(SupplierPayablePaymentMethod.Other)
    ];

    public static string ToCode(SupplierPayablePaymentMethod method) => method.ToString();

    public static bool TryParse(string? code, out SupplierPayablePaymentMethod method)
    {
        method = SupplierPayablePaymentMethod.Cash;
        if (string.IsNullOrWhiteSpace(code))
        {
            return false;
        }

        var match = Codes.FirstOrDefault(c => string.Equals(c, code.Trim(), StringComparison.OrdinalIgnoreCase));
        if (match is null)
        {
            return false;
        }

        method = Enum.Parse<SupplierPayablePaymentMethod>(match, ignoreCase: false);
        return true;
    }

    public static SupplierPayablePaymentMethod Parse(string? code)
    {
        if (!TryParse(code, out var method))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidSupplierPayablePaymentMethod,
                $"Payment method must be one of: {string.Join(", ", Codes)}.");
        }

        return method;
    }
}
