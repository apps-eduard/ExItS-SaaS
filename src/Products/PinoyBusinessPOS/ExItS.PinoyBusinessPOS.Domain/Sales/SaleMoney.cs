using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Sales;

/// <summary>
/// Monetary and quantity rounding rules for simple retail sales.
///
/// Money is rounded to <see cref="MonetaryDecimals"/> decimal places with
/// <see cref="MidpointRounding.AwayFromZero"/>. This is deliberately the same convention already used
/// by <c>CreditEntry</c> and <c>Repayment</c> so a peso amount means the same thing everywhere in the
/// product: a half-centavo always rounds up in magnitude and never toward the nearest even value.
/// Applying one convention across sales and Utang keeps sale totals reconcilable against credit and
/// repayment records without per-feature rounding drift.
///
/// Quantity precision is driven by <see cref="SellingMode"/> when known, otherwise by unit of measure:
/// ByWeight always admits up to <see cref="MeasuredQuantityDecimals"/> decimal places in kilograms;
/// PerItem countable units admit whole numbers only; PerItem measured units (Liter, Kilogram bags, etc.)
/// keep the historical measured-UOM rule (up to 3 dp). Quantities are never rounded silently —
/// an over-precise quantity is rejected instead.
/// </summary>
public static class SaleMoney
{
    public const int MonetaryDecimals = 2;
    public const int MeasuredQuantityDecimals = 3;

    /// <summary>Rounds a monetary amount to 2 decimals, away from zero at the midpoint.</summary>
    public static decimal RoundMoney(decimal amount) =>
        decimal.Round(amount, MonetaryDecimals, MidpointRounding.AwayFromZero);

    /// <summary>True when the unit of measure is counted in whole, indivisible units.</summary>
    public static bool IsWholeUnit(UnitOfMeasure unitOfMeasure) => unitOfMeasure switch
    {
        UnitOfMeasure.Piece
            or UnitOfMeasure.Pack
            or UnitOfMeasure.Box
            or UnitOfMeasure.Bottle
            or UnitOfMeasure.Can
            or UnitOfMeasure.Sachet => true,
        _ => false
    };

    /// <summary>
    /// Maximum decimal places a quantity may carry.
    /// SellingMode is authoritative when ByWeight; otherwise UOM rules apply.
    /// </summary>
    public static int MaxQuantityDecimals(
        UnitOfMeasure unitOfMeasure,
        SellingMode sellingMode = SellingMode.PerItem) =>
        sellingMode == SellingMode.ByWeight
            ? MeasuredQuantityDecimals
            : IsWholeUnit(unitOfMeasure) ? 0 : MeasuredQuantityDecimals;

    /// <summary>True when a PerItem product with this UOM must be sold in whole increments.</summary>
    public static bool RequiresWholeQuantity(
        UnitOfMeasure unitOfMeasure,
        SellingMode sellingMode = SellingMode.PerItem) =>
        sellingMode != SellingMode.ByWeight && IsWholeUnit(unitOfMeasure);

    /// <summary>True when <paramref name="value"/> carries no more than <paramref name="decimals"/> decimal places.</summary>
    public static bool HasAtMostDecimals(decimal value, int decimals) =>
        decimal.Round(value, decimals, MidpointRounding.ToZero) == value;

    internal static void EnsureUtc(DateTimeOffset utcNow)
    {
        if (utcNow.Offset != TimeSpan.Zero)
        {
            throw new DomainException(DomainErrorCodes.InvalidUtcTimestamp, "Timestamp must be UTC.");
        }
    }

    internal static void EnsureActor(Guid actorId)
    {
        if (actorId == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidSaleActor,
                "A non-empty actor identifier is required for sale recording and voiding.");
        }
    }
}
