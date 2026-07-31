using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.Domain.Returns;

/// <summary>
/// Refundable quantity and amount helpers for partial returns. Money fidelity prefers remaining
/// line total minus prior refunds for the last partial return on a line.
/// </summary>
public static class SaleReturnRefundable
{
    public static decimal RefundableQuantity(SaleLine saleLine, decimal previouslyReturnedQuantity) =>
        saleLine.Quantity - previouslyReturnedQuantity;

    public static decimal RefundableAmount(SaleLine saleLine, decimal previouslyRefundedAmount) =>
        SaleMoney.RoundMoney(saleLine.LineTotal - previouslyRefundedAmount);

    /// <summary>
    /// Computes refund for a return line. When returning all remaining quantity, uses the remaining
    /// refundable amount so cumulative refunds never exceed the original line total.
    /// </summary>
    public static decimal ComputeRefundAmount(
        SaleLine saleLine,
        decimal quantityReturned,
        decimal previouslyReturnedQuantity,
        decimal previouslyRefundedAmount)
    {
        var remainingQty = RefundableQuantity(saleLine, previouslyReturnedQuantity);
        var remainingAmount = RefundableAmount(saleLine, previouslyRefundedAmount);

        if (quantityReturned >= remainingQty)
        {
            return remainingAmount;
        }

        return SaleMoney.RoundMoney(quantityReturned * saleLine.UnitPrice);
    }
}
