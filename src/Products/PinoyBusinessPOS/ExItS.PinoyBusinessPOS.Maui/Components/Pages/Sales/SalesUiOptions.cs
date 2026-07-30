using ExItS.DesignSystem.Components.Primitives;
using ExItS.PinoyBusinessPOS.Application.Sales;
using ExItS.PinoyBusinessPOS.Maui.Localization;
using Microsoft.Extensions.Localization;

namespace ExItS.PinoyBusinessPOS.Maui.Components.Pages.Sales;

/// <summary>
/// Localized labels for the controlled sale option sets. Stable codes come from the Application
/// layer; only the display text is localized here.
/// </summary>
internal static class SalesUiOptions
{
    public static IReadOnlyList<SelectOption> PaymentMethods(IStringLocalizer<PosResources> localizer) =>
        PosSaleOptions.PaymentMethodCodes
            .Select(code => new SelectOption(code, PaymentMethodLabel(localizer, code)))
            .ToList();

    public static string PaymentMethodLabel(IStringLocalizer<PosResources> localizer, string? code) =>
        string.IsNullOrWhiteSpace(code) ? string.Empty : localizer[$"Sales_Payment_{code}"].Value;

    public static string StatusLabel(IStringLocalizer<PosResources> localizer, string? status) =>
        string.Equals(status, PosSaleOptions.VoidedStatus, StringComparison.Ordinal)
            ? localizer["Sales_Status_Voided"].Value
            : localizer["Sales_Status_Completed"].Value;

    public static IReadOnlyList<SelectOption> StatusFilters(IStringLocalizer<PosResources> localizer) =>
    [
        new(string.Empty, localizer["Sales_Filter_AllStatuses"].Value),
        new(PosSaleOptions.CompletedStatus, localizer["Sales_Status_Completed"].Value),
        new(PosSaleOptions.VoidedStatus, localizer["Sales_Status_Voided"].Value)
    ];

    public static IReadOnlyList<SelectOption> PaymentMethodFilters(IStringLocalizer<PosResources> localizer)
    {
        var options = new List<SelectOption>
        {
            new(string.Empty, localizer["Sales_Filter_AllPayments"].Value)
        };
        options.AddRange(PaymentMethods(localizer));
        return options;
    }

    public static string UnitOfMeasureLabel(IStringLocalizer<PosResources> localizer, string? code) =>
        string.IsNullOrWhiteSpace(code) ? string.Empty : localizer[$"Catalog_Uom_{code}"].Value;
}
