using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.Domain.CustomerOrdering;

/// <summary>
/// Manual V1 settlement choice for a customer order. Persisted as the enum name.
/// GCash is stored as <see cref="ManualGCash"/> — no gateway, QR, or PaymentAttempt.
/// Utang is a requested manual settlement method only; it does not post ledger debt.
/// </summary>
public enum CustomerOrderPaymentMethod
{
    Cash = 0,
    ManualGCash = 1,
    Utang = 2
}

public static class CustomerOrderPaymentMethods
{
    public const int CodeMaxLength = 32;

    public static IReadOnlyList<string> Codes { get; } =
    [
        nameof(CustomerOrderPaymentMethod.Cash),
        nameof(CustomerOrderPaymentMethod.ManualGCash),
        nameof(CustomerOrderPaymentMethod.Utang)
    ];

    public static string ToCode(CustomerOrderPaymentMethod method) => method.ToString();

    /// <summary>Merchant/customer-facing label. GCash is never implied to be gateway-verified.</summary>
    public static string ToUiLabel(CustomerOrderPaymentMethod method) =>
        method == CustomerOrderPaymentMethod.ManualGCash ? "GCash" : method.ToString();

    /// <summary>
    /// Parses a request value. Null/blank defaults to Cash. Accepts <c>GCash</c> as ManualGCash.
    /// </summary>
    public static CustomerOrderPaymentMethod Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return CustomerOrderPaymentMethod.Cash;
        }

        var trimmed = value.Trim();
        if (trimmed.Equals(nameof(CustomerOrderPaymentMethod.Cash), StringComparison.OrdinalIgnoreCase))
        {
            return CustomerOrderPaymentMethod.Cash;
        }

        if (trimmed.Equals("GCash", StringComparison.OrdinalIgnoreCase)
            || trimmed.Equals(nameof(CustomerOrderPaymentMethod.ManualGCash), StringComparison.OrdinalIgnoreCase))
        {
            return CustomerOrderPaymentMethod.ManualGCash;
        }

        if (trimmed.Equals(nameof(CustomerOrderPaymentMethod.Utang), StringComparison.OrdinalIgnoreCase))
        {
            return CustomerOrderPaymentMethod.Utang;
        }

        throw new DomainException(
            DomainErrorCodes.InvalidCustomerOrderPaymentMethod,
            "Payment method must be Cash, GCash, or Utang.");
    }

    public static SalePaymentMethod ToSalePaymentMethod(CustomerOrderPaymentMethod method) =>
        method switch
        {
            CustomerOrderPaymentMethod.Cash => SalePaymentMethod.Cash,
            CustomerOrderPaymentMethod.ManualGCash => SalePaymentMethod.ManualGCash,
            CustomerOrderPaymentMethod.Utang => SalePaymentMethod.Utang,
            _ => throw new DomainException(
                DomainErrorCodes.InvalidCustomerOrderPaymentMethod,
                "Payment method must be Cash, GCash, or Utang.")
        };
}
