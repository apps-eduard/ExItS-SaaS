using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Payments;

/// <summary>
/// Manual Utang/credit collection tender. Not an online payment gateway method.
/// Check is first-class for receivable settlement and does not require gateway support.
/// </summary>
public enum UtangPaymentMethod
{
    Cash = 0,
    ManualGCash = 1,
    Check = 2
}

public static class UtangPaymentMethods
{
    public static IReadOnlyList<string> Codes { get; } =
    [
        nameof(UtangPaymentMethod.Cash),
        nameof(UtangPaymentMethod.ManualGCash),
        nameof(UtangPaymentMethod.Check)
    ];

    public static bool TryParse(string? code, out UtangPaymentMethod method)
    {
        method = UtangPaymentMethod.Cash;
        if (string.IsNullOrWhiteSpace(code))
        {
            return false;
        }

        var match = Codes.FirstOrDefault(c =>
            string.Equals(c, code.Trim(), StringComparison.OrdinalIgnoreCase));
        if (match is null)
        {
            return false;
        }

        method = Enum.Parse<UtangPaymentMethod>(match, ignoreCase: false);
        return true;
    }

    public static UtangPaymentMethod ParseRequired(string? code)
    {
        if (!TryParse(code, out var method))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidUtangPaymentMethod,
                "Payment method must be Cash, ManualGCash, or Check.");
        }

        return method;
    }
}
