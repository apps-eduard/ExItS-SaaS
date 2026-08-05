using ExItS.PinoyBusinessPOS.Domain.Payments;
using ExItS.PinoyBusinessPOS.Maui.Localization;
using Microsoft.Extensions.Localization;

namespace ExItS.PinoyBusinessPOS.Maui.Components.Pages.Sales;

/// <summary>Localized labels for payment attempt lifecycle states.</summary>
internal static class PaymentsUiOptions
{
    public static string AttemptStatusLabel(IStringLocalizer<PosResources> localizer, string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return string.Empty;
        }

        var key = status switch
        {
            nameof(PaymentAttemptStatus.Created) => "Sales_EPayment_Status_Created",
            nameof(PaymentAttemptStatus.Pending) => "Sales_EPayment_Status_Pending",
            nameof(PaymentAttemptStatus.RequiresCustomerAction) => "Sales_EPayment_Status_RequiresCustomerAction",
            nameof(PaymentAttemptStatus.Processing) => "Sales_EPayment_Status_Processing",
            nameof(PaymentAttemptStatus.Paid) => "Sales_EPayment_Status_Paid",
            nameof(PaymentAttemptStatus.Failed) => "Sales_EPayment_Status_Failed",
            nameof(PaymentAttemptStatus.Cancelled) => "Sales_EPayment_Status_Cancelled",
            nameof(PaymentAttemptStatus.Expired) => "Sales_EPayment_Status_Expired",
            _ => null
        };

        return key is null ? status : localizer[key].Value;
    }

    public static bool IsTerminalAttemptStatus(string? status) =>
        status is nameof(PaymentAttemptStatus.Paid)
            or nameof(PaymentAttemptStatus.Failed)
            or nameof(PaymentAttemptStatus.Cancelled)
            or nameof(PaymentAttemptStatus.Expired);

    public static bool IsActiveAttemptStatus(string? status) =>
        !string.IsNullOrWhiteSpace(status) && !IsTerminalAttemptStatus(status);
}
