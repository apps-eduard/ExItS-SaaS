using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.Application.Sales;

/// <summary>
/// Controlled sale option sets surfaced to clients and UI. Stable codes only — localized labels
/// belong to the presentation resource files.
/// </summary>
public static class PosSaleOptions
{
    public static IReadOnlyList<string> PaymentMethodCodes { get; } = SalePaymentMethods.Codes;

    public static IReadOnlyList<string> SaleStatuses { get; } =
    [
        nameof(SaleStatus.Completed),
        nameof(SaleStatus.Voided),
        nameof(SaleStatus.AwaitingPayment)
    ];

    public const string CompletedStatus = nameof(SaleStatus.Completed);
    public const string VoidedStatus = nameof(SaleStatus.Voided);
    public const string AwaitingPaymentStatus = nameof(SaleStatus.AwaitingPayment);
    public const string CashPaymentMethod = nameof(SalePaymentMethod.Cash);
    public const string ManualGCashPaymentMethod = nameof(SalePaymentMethod.ManualGCash);
    public const string UtangPaymentMethod = nameof(SalePaymentMethod.Utang);
    public const string CardPaymentMethod = nameof(SalePaymentMethod.Card);
    public const string GCashPaymentMethod = nameof(SalePaymentMethod.GCash);

    public const int GCashReferenceMaxLength = Sale.GCashReferenceMaxLength;
    public const int VoidReasonMaxLength = Sale.VoidReasonMaxLength;

    /// <summary>
    /// Code-based mirror of the domain quantity rule so a client can keep its own input in step with
    /// the server without referencing the domain enum. The server still validates every quantity.
    /// </summary>
    public static bool RequiresWholeQuantity(string? unitOfMeasureCode) =>
        RequiresWholeQuantity(unitOfMeasureCode, nameof(SellingMode.PerItem));

    public static bool RequiresWholeQuantity(string? unitOfMeasureCode, string? sellingModeCode) =>
        UnitOfMeasures.TryParse(unitOfMeasureCode, out var unit)
        && SaleMoney.RequiresWholeQuantity(unit, SellingModes.Parse(sellingModeCode));

    public static int MaxQuantityDecimals(string? unitOfMeasureCode) =>
        MaxQuantityDecimals(unitOfMeasureCode, nameof(SellingMode.PerItem));

    public static int MaxQuantityDecimals(string? unitOfMeasureCode, string? sellingModeCode)
    {
        var sellingMode = SellingModes.Parse(sellingModeCode);
        if (sellingMode == SellingMode.ByWeight)
        {
            return SaleMoney.MeasuredQuantityDecimals;
        }

        return UnitOfMeasures.TryParse(unitOfMeasureCode, out var unit)
            ? SaleMoney.MaxQuantityDecimals(unit, sellingMode)
            : SaleMoney.MeasuredQuantityDecimals;
    }

    public static bool IsValidQuantity(decimal quantity, string? unitOfMeasureCode) =>
        IsValidQuantity(quantity, unitOfMeasureCode, nameof(SellingMode.PerItem));

    public static bool IsValidQuantity(decimal quantity, string? unitOfMeasureCode, string? sellingModeCode) =>
        quantity > 0m
        && SaleMoney.HasAtMostDecimals(quantity, MaxQuantityDecimals(unitOfMeasureCode, sellingModeCode));

    /// <summary>Client-side preview rounding. Matches the server rule exactly (2dp, away from zero).</summary>
    public static decimal RoundMoney(decimal amount) => SaleMoney.RoundMoney(amount);
}
