using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Payments;
using ExItS.PinoyBusinessPOS.Domain.Purchasing;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.Application.Purchasing;

/// <summary>
/// Decides the commercial settlement state of a fully received purchase order.
/// Goods receipt alone never completes a pay-on-delivery/receipt order commercially:
/// the seller must confirm settlement while any amount remains due.
/// </summary>
public static class ConnectedPoPostReceiptFinancialCompletion
{
    public sealed record Outcome(
        ConnectedPoFinancialSettlementStatus Status,
        decimal RemainingDue,
        bool NoPaymentDue);

    /// <summary>
    /// Settled amount that may count toward the amount due. A pending (uncleared) check is
    /// not money in hand, so it is excluded; a cleared check counts.
    /// </summary>
    public static decimal EffectiveSettledAmount(
        decimal settledAmount,
        decimal pendingCheckAmount) =>
        SaleMoney.RoundMoney(Math.Max(0m, SaleMoney.RoundMoney(settledAmount) - SaleMoney.RoundMoney(Math.Max(0m, pendingCheckAmount))));

    public static bool CountsAsSettled(
        ConnectedPoPaymentTerm paymentTerm,
        UtangCheckClearingStatus? checkClearingStatus) =>
        paymentTerm != ConnectedPoPaymentTerm.Check
        || checkClearingStatus == UtangCheckClearingStatus.Cleared;

    public static Outcome Evaluate(
        ConnectedPoPaymentTiming paymentTiming,
        ConnectedPoPaymentTerm paymentTerm,
        decimal finalAcceptedValue,
        decimal settledAmount,
        decimal pendingCheckAmount = 0m)
    {
        var accepted = SaleMoney.RoundMoney(Math.Max(0m, finalAcceptedValue));
        var settled = EffectiveSettledAmount(settledAmount, pendingCheckAmount);
        var remainingDue = accepted > settled ? SaleMoney.RoundMoney(accepted - settled) : 0m;

        // PayBefore settlement happened before fulfillment; supplier credit / utang moves the
        // obligation onto the payable/receivable ledger. Neither gates commercial completion here.
        if (paymentTiming != ConnectedPoPaymentTiming.PayOnDeliveryOrReceipt)
        {
            return new Outcome(ConnectedPoFinancialSettlementStatus.NotRequired, remainingDue, false);
        }

        if (paymentTerm == ConnectedPoPaymentTerm.Utang)
        {
            // Utang is an explicit credit arrangement, tracked on the credit ledger.
            return new Outcome(ConnectedPoFinancialSettlementStatus.NotRequired, remainingDue, false);
        }

        if (remainingDue > 0m)
        {
            return new Outcome(ConnectedPoFinancialSettlementStatus.AwaitingPayment, remainingDue, false);
        }

        return new Outcome(
            ConnectedPoFinancialSettlementStatus.Settled,
            0m,
            NoPaymentDue: accepted <= 0m || settled >= accepted);
    }

    /// <summary>
    /// Applies the evaluated settlement state onto a fully received purchase order.
    /// Idempotent: an already settled order is never reopened.
    /// </summary>
    public static Outcome Apply(
        PurchaseOrder purchaseOrder,
        ConnectedPoPaymentTiming paymentTiming,
        ConnectedPoPaymentTerm paymentTerm,
        decimal finalAcceptedValue,
        decimal settledAmount,
        DateTimeOffset utcNow,
        decimal pendingCheckAmount = 0m)
    {
        ArgumentNullException.ThrowIfNull(purchaseOrder);
        var outcome = Evaluate(
            paymentTiming,
            paymentTerm,
            finalAcceptedValue,
            settledAmount,
            pendingCheckAmount);

        if (purchaseOrder.Status != PurchaseOrderStatus.Received)
        {
            return outcome;
        }

        switch (outcome.Status)
        {
            case ConnectedPoFinancialSettlementStatus.AwaitingPayment:
                purchaseOrder.MarkAwaitingPayment(utcNow, paymentTiming);
                break;
            case ConnectedPoFinancialSettlementStatus.Settled:
                purchaseOrder.MarkNoPaymentDue(utcNow);
                break;
        }

        return outcome;
    }
}
