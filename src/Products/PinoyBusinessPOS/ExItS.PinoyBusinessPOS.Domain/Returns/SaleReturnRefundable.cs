using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.Domain.Returns;

/// <summary>
/// Refundable quantity and amount helpers for partial returns. Money uses net
/// <see cref="SaleLine.LineTotal"/> cumulative allocation — never <see cref="SaleLine.UnitPrice"/>.
/// </summary>
public static class SaleReturnRefundable
{
    public static decimal RefundableQuantity(SaleLine saleLine, decimal previouslyReturnedQuantity) =>
        saleLine.Quantity - previouslyReturnedQuantity;

    public static decimal RefundableAmount(SaleLine saleLine, decimal previouslyRefundedAmount) =>
        SaleMoney.RoundMoney(saleLine.LineTotal - previouslyRefundedAmount);

    /// <summary>
    /// Computes refund for a return line from net line total proportional to cumulative returned qty.
    /// The final slice sets cumulative refund equal to <see cref="SaleLine.LineTotal"/> so remainder
    /// rounding is absorbed without using unit price.
    /// </summary>
    public static decimal ComputeRefundAmount(
        SaleLine saleLine,
        decimal quantityReturned,
        decimal previouslyReturnedQuantity,
        decimal previouslyRefundedAmount)
    {
        var totalQty = saleLine.Quantity;
        var netLineTotal = saleLine.LineTotal;
        var previousQty = previouslyReturnedQuantity;
        var newCumulativeQty = previousQty + quantityReturned;

        decimal targetCumulative;
        if (newCumulativeQty >= totalQty)
        {
            targetCumulative = netLineTotal;
        }
        else
        {
            targetCumulative = SaleMoney.RoundMoney(netLineTotal * newCumulativeQty / totalQty);
        }

        return SaleMoney.RoundMoney(targetCumulative - previouslyRefundedAmount);
    }
}
