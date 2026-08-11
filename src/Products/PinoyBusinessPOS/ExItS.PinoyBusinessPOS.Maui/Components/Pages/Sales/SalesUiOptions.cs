using ExItS.DesignSystem.Components.Primitives;
using ExItS.PinoyBusinessPOS.Application.Sales;
using ExItS.PinoyBusinessPOS.Maui.Localization;
using Microsoft.Extensions.Localization;

namespace ExItS.PinoyBusinessPOS.Maui.Components.Pages.Sales;

/// <summary>
/// Localized labels for the controlled sale option sets. Stable codes come from the Application
/// layer; only the display text is localized here.
/// </summary>
public static class SalesUiOptions
{
    public static IReadOnlyList<SelectOption> PaymentMethods(IStringLocalizer<PosResources> localizer) =>
        PosSaleOptions.PaymentMethodCodes
            .Select(code => new SelectOption(code, PaymentMethodLabel(localizer, code)))
            .ToList();

    public static string PaymentMethodLabel(IStringLocalizer<PosResources> localizer, string? code) =>
        string.IsNullOrWhiteSpace(code) ? string.Empty : localizer[$"Sales_Payment_{code}"].Value;

    public static string StatusLabel(IStringLocalizer<PosResources> localizer, string? status) =>
        status switch
        {
            _ when string.Equals(status, PosSaleOptions.VoidedStatus, StringComparison.Ordinal)
                => localizer["Sales_Status_Voided"].Value,
            _ when string.Equals(status, PosSaleOptions.AwaitingPaymentStatus, StringComparison.Ordinal)
                => localizer["Sales_Status_AwaitingPayment"].Value,
            _ => localizer["Sales_Status_Completed"].Value
        };

    public static IReadOnlyList<SelectOption> StatusFilters(IStringLocalizer<PosResources> localizer) =>
    [
        new(string.Empty, localizer["Sales_Filter_AllStatuses"].Value),
        new(PosSaleOptions.CompletedStatus, localizer["Sales_Status_Completed"].Value),
        new(PosSaleOptions.AwaitingPaymentStatus, localizer["Sales_Status_AwaitingPayment"].Value),
        new(PosSaleOptions.VoidedStatus, localizer["Sales_Status_Voided"].Value)
    ];

    public static bool IsElectronicPaymentMethod(string? code) =>
        string.Equals(code, PosSaleOptions.CardPaymentMethod, StringComparison.Ordinal)
        || string.Equals(code, PosSaleOptions.GCashPaymentMethod, StringComparison.Ordinal);

    public static IReadOnlyList<string> CheckoutPaymentMethodCodes { get; } =
    [
        PosSaleOptions.CashPaymentMethod,
        // Manual GCash (reference required). Electronic Card / GCash are not checkout options.
        PosSaleOptions.ManualGCashPaymentMethod,
        PosSaleOptions.UtangPaymentMethod
    ];

    /// <summary>Checkout tile label — ManualGCash is shown as plain "GCash".</summary>
    public static string CheckoutPaymentMethodLabel(IStringLocalizer<PosResources> localizer, string? code) =>
        string.Equals(code, PosSaleOptions.ManualGCashPaymentMethod, StringComparison.Ordinal)
            ? localizer["Sales_Payment_GCash"].Value
            : PaymentMethodLabel(localizer, code);

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

    /// <summary>
    /// Uses inventory account <c>IsTracked</c>. Untracked products
    /// never surface In/Low/Out of stock labels.
    /// </summary>
    public static string StockStateLabel(
        IStringLocalizer<PosResources> localizer,
        bool isTracked,
        string? stockStatus)
    {
        if (!isTracked)
        {
            return localizer["Sales_Checkout_StockNotTracked"].Value;
        }

        if (string.Equals(stockStatus, "OutOfStock", StringComparison.OrdinalIgnoreCase))
        {
            return localizer["Sales_Checkout_OutOfStock"].Value;
        }

        if (string.Equals(stockStatus, "LowStock", StringComparison.OrdinalIgnoreCase))
        {
            return localizer["Sales_Checkout_LowStock"].Value;
        }

        return localizer["Sales_Checkout_InStock"].Value;
    }

    public static string ProductRowMeta(
        IStringLocalizer<PosResources> localizer,
        string? unitOfMeasure,
        bool isTracked,
        string? stockStatus) =>
        string.Format(
            localizer["Sales_Checkout_UnitStock"].Value,
            UnitOfMeasureLabel(localizer, unitOfMeasure),
            StockStateLabel(localizer, isTracked, stockStatus));

    public static bool IsByWeight(string? sellingMode) =>
        WeightEntry.IsByWeight(sellingMode);

    public static string PriceUnitSuffix(IStringLocalizer<PosResources> localizer, string? sellingMode) =>
        IsByWeight(sellingMode) ? localizer["Sales_Checkout_PricePerKg"].Value : string.Empty;

    public static string FormatCartQuantity(
        IStringLocalizer<PosResources> localizer,
        decimal quantity,
        string? unitOfMeasure,
        string? sellingMode)
    {
        if (IsByWeight(sellingMode))
        {
            return string.Format(
                localizer["Sales_Checkout_QuantityKg"].Value,
                WeightEntry.FormatKilograms(quantity));
        }

        var uom = UnitOfMeasureLabel(localizer, unitOfMeasure);
        return string.IsNullOrWhiteSpace(uom)
            ? quantity.ToString("0.###")
            : $"{quantity.ToString("0.###")} {uom}";
    }

    /// <summary>
    /// Matches <c>SaleStockService</c>: only tracked inventory enforces on-hand; untracked sells freely.
    /// </summary>
    public static bool CanAcceptQuantity(bool isTracked, decimal onHandQuantity, decimal requestedQuantity) =>
        !isTracked || requestedQuantity <= onHandQuantity;

    public static bool IsOutOfStock(bool isTracked, string? stockStatus, decimal onHandQuantity) =>
        isTracked
        && (onHandQuantity <= 0m
            || string.Equals(stockStatus, "OutOfStock", StringComparison.OrdinalIgnoreCase));
}
