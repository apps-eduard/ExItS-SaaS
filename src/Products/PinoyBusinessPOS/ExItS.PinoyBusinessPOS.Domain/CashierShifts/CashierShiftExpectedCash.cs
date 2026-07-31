using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.Domain.CashierShifts;

/// <summary>Computes expected physical cash for an open or closing shift.</summary>
public static class CashierShiftExpectedCash
{
    /// <summary>
    /// ExpectedCash = OpeningCash + NetCashSales + Sum(CashIn) − Sum(CashOut).
    /// NetCashSales counts Completed Cash sales minus Voided Cash sales (ManualGCash and Utang excluded).
    /// </summary>
    public static decimal Compute(
        decimal openingCashAmount,
        decimal netCashSales,
        IEnumerable<CashierShiftMovement> movements)
    {
        var cashIn = movements
            .Where(m => m.MovementType == CashierShiftMovementType.CashIn)
            .Sum(m => m.Amount);
        var cashOut = movements
            .Where(m => m.MovementType == CashierShiftMovementType.CashOut)
            .Sum(m => m.Amount);

        return SaleMoney.RoundMoney(openingCashAmount + netCashSales + cashIn - cashOut);
    }
}
