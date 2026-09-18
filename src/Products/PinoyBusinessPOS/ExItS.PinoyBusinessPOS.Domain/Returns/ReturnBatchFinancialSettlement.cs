using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.Domain.Returns;

public sealed record ReturnBatchFinancialSettlementResult(
    decimal AdjustedObligation,
    decimal RefundDue,
    decimal RemainingDue,
    decimal ObligationReducedAmount,
    decimal PreReturnRemainingDue);

public static class ReturnBatchFinancialSettlement
{
    public static ReturnBatchFinancialSettlementResult Evaluate(
        decimal originalSaleTotal,
        decimal settledPayments,
        decimal acceptedReturnValue,
        decimal priorAcceptedReturnValues,
        decimal alreadyRefundedAcrossBatches)
    {
        var original = SaleMoney.RoundMoney(Math.Max(0m, originalSaleTotal));
        var settled = SaleMoney.RoundMoney(Math.Max(0m, settledPayments));
        var accepted = SaleMoney.RoundMoney(Math.Max(0m, acceptedReturnValue));
        var priorAccepted = SaleMoney.RoundMoney(Math.Max(0m, priorAcceptedReturnValues));
        var refunded = SaleMoney.RoundMoney(Math.Max(0m, alreadyRefundedAcrossBatches));

        var preReturnObligation = SaleMoney.RoundMoney(Math.Max(0m, original - priorAccepted));
        var adjustedObligation = SaleMoney.RoundMoney(Math.Max(0m, original - priorAccepted - accepted));
        var preReturnRemainingDue = SaleMoney.RoundMoney(Math.Max(0m, preReturnObligation - settled));
        var remainingDue = SaleMoney.RoundMoney(Math.Max(0m, adjustedObligation - settled));
        var refundDue = SaleMoney.RoundMoney(Math.Max(0m, settled - adjustedObligation - refunded));
        var obligationReduced = SaleMoney.RoundMoney(Math.Max(0m, preReturnRemainingDue - remainingDue));

        return new ReturnBatchFinancialSettlementResult(
            adjustedObligation,
            refundDue,
            remainingDue,
            obligationReduced,
            preReturnRemainingDue);
    }
}
