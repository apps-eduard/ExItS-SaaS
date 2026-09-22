using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Purchasing;
using ExItS.PinoyBusinessPOS.Domain.Returns;
using ExItS.PinoyBusinessPOS.Domain.Sales;
using ExItS.PinoyBusinessPOS.Domain.SupplierPayables;

namespace ExItS.PinoyBusinessPOS.Application.Returns;

/// <summary>
/// Connected-PO mirror of <see cref="SaleReturnSettlementResolver"/>.
/// The obligation base is the good-received value of the purchase order (never the ordered value),
/// and settled payments come from receipt payables plus any PayBefore prepayment snapshot.
/// Pending (uncleared) check payments are excluded so a return never raises a false refund due.
/// </summary>
public static class ConnectedPoReturnSettlementResolver
{
    /// <summary>
    /// Authoritative charged value: short-close/completion snapshot when present,
    /// otherwise recomputed from good-received quantity × locked unit purchase cost.
    /// </summary>
    public static decimal ResolveGoodReceivedValue(PurchaseOrder purchaseOrder)
    {
        ArgumentNullException.ThrowIfNull(purchaseOrder);
        if (purchaseOrder.FinalAcceptedValue is decimal snapshot)
        {
            return SaleMoney.RoundMoney(Math.Max(0m, snapshot));
        }

        var total = 0m;
        foreach (var line in purchaseOrder.Lines)
        {
            total += SaleMoney.RoundMoney(line.ReceivedQty * line.UnitPurchaseCost);
        }

        return SaleMoney.RoundMoney(Math.Max(0m, total));
    }

    /// <summary>
    /// Cash actually settled toward this purchase order. Supplier-credit / Utang obligations are
    /// not settled cash. <paramref name="unclearedCheckAmount"/> removes check payments that have
    /// not cleared, so the return reduces the obligation instead of producing a refund.
    /// </summary>
    public static decimal ResolveSettledPayments(
        PurchaseOrder purchaseOrder,
        IReadOnlyList<SupplierPayable> payablesForReceipts,
        decimal unclearedCheckAmount = 0m)
    {
        ArgumentNullException.ThrowIfNull(purchaseOrder);
        ArgumentNullException.ThrowIfNull(payablesForReceipts);

        var fromPayables = 0m;
        foreach (var payable in payablesForReceipts.Where(p => p.Status != SupplierPayableStatus.Voided))
        {
            fromPayables += payable.PaidAmount;
        }

        var settled = Math.Max(purchaseOrder.AmountPaidSnapshot ?? 0m, SaleMoney.RoundMoney(fromPayables));
        settled -= Math.Max(0m, unclearedCheckAmount);
        return SaleMoney.RoundMoney(Math.Max(0m, settled));
    }

    public static ReturnBatchFinancialSettlementResult Evaluate(
        decimal goodReceivedValue,
        decimal settledPayments,
        decimal acceptedReturnValue,
        decimal priorAcceptedReturnValues,
        decimal alreadyRefundedAcrossBatches) =>
        ReturnBatchFinancialSettlement.Evaluate(
            goodReceivedValue,
            settledPayments,
            acceptedReturnValue,
            priorAcceptedReturnValues,
            alreadyRefundedAcrossBatches);

    /// <summary>
    /// PayBefore settled and POD paid produce RefundDue. Unpaid / partial / pending-check reduce the
    /// obligation first and only raise RefundDue on genuine overpayment. Supplier credit reduces the
    /// mirrored receivable/payable instead of paying cash.
    /// </summary>
    public static ReturnBatchRefundStatus ResolveRefundStatus(
        ConnectedPoPaymentTiming paymentTiming,
        ConnectedPoPaymentTerm paymentTerm,
        ReturnBatchFinancialSettlementResult settlement)
    {
        if (settlement.RefundDue > 0m)
        {
            return ReturnBatchRefundStatus.RefundDue;
        }

        if (UsesSupplierCredit(paymentTiming, paymentTerm))
        {
            return ReturnBatchRefundStatus.CreditReduced;
        }

        return ReturnBatchRefundStatus.ObligationReduced;
    }

    public static bool UsesSupplierCredit(
        ConnectedPoPaymentTiming paymentTiming,
        ConnectedPoPaymentTerm paymentTerm) =>
        paymentTiming == ConnectedPoPaymentTiming.SupplierCredit
        || ConnectedPoUtangCredit.UsesUtang(paymentTerm);
}
