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
        nameof(SaleStatus.Voided)
    ];

    public const string CompletedStatus = nameof(SaleStatus.Completed);
    public const string VoidedStatus = nameof(SaleStatus.Voided);
    public const string CashPaymentMethod = nameof(SalePaymentMethod.Cash);
    public const string ManualGCashPaymentMethod = nameof(SalePaymentMethod.ManualGCash);

    public const int GCashReferenceMaxLength = Sale.GCashReferenceMaxLength;
    public const int VoidReasonMaxLength = Sale.VoidReasonMaxLength;

    /// <summary>
    /// Code-based mirror of the domain quantity rule so a client can keep its own input in step with
    /// the server without referencing the domain enum. The server still validates every quantity.
    /// </summary>
    public static bool RequiresWholeQuantity(string? unitOfMeasureCode) =>
        UnitOfMeasures.TryParse(unitOfMeasureCode, out var unit) && SaleMoney.IsWholeUnit(unit);

    public static int MaxQuantityDecimals(string? unitOfMeasureCode) =>
        UnitOfMeasures.TryParse(unitOfMeasureCode, out var unit)
            ? SaleMoney.MaxQuantityDecimals(unit)
            : SaleMoney.MeasuredQuantityDecimals;

    public static bool IsValidQuantity(decimal quantity, string? unitOfMeasureCode) =>
        quantity > 0m && SaleMoney.HasAtMostDecimals(quantity, MaxQuantityDecimals(unitOfMeasureCode));

    /// <summary>Client-side preview rounding. Matches the server rule exactly (2dp, away from zero).</summary>
    public static decimal RoundMoney(decimal amount) => SaleMoney.RoundMoney(amount);
}
