using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Expenses;

/// <summary>
/// Payment methods for a store expense. Exactly one per expense — Cash or ManualGCash only.
/// <c>ManualGCash</c> is a manually confirmed transfer: no gateway, QR, or verification is performed.
/// Member names are the stable persistence codes; localized labels live in UI resource files only.
/// </summary>
public enum ExpensePaymentMethod
{
    Cash = 0,
    ManualGCash = 1
}

public static class ExpensePaymentMethods
{
    public const int CodeMaxLength = 32;

    /// <summary>Stable persistence codes in canonical display order.</summary>
    public static IReadOnlyList<string> Codes { get; } =
    [
        nameof(ExpensePaymentMethod.Cash),
        nameof(ExpensePaymentMethod.ManualGCash)
    ];

    public static string ToCode(ExpensePaymentMethod method) => method.ToString();

    public static bool TryParse(string? code, out ExpensePaymentMethod method)
    {
        method = ExpensePaymentMethod.Cash;
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

        method = Enum.Parse<ExpensePaymentMethod>(match, ignoreCase: false);
        return true;
    }

    public static ExpensePaymentMethod Parse(string? code)
    {
        if (!TryParse(code, out var method))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidExpensePaymentMethod,
                $"Payment method must be one of: {string.Join(", ", Codes)}.");
        }

        return method;
    }
}
