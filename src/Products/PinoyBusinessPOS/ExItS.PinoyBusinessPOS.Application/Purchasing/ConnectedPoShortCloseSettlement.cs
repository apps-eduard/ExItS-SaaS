using ExItS.PinoyBusinessPOS.Domain.Purchasing;
using ExItS.PinoyBusinessPOS.Domain.Sales;
using ExItS.PinoyBusinessPOS.Domain.SupplierPayables;

namespace ExItS.PinoyBusinessPOS.Application.Purchasing;

/// <summary>
/// Authoritative settlement after short-closing remaining PO quantity.
/// Final payable value is good-received only; cancelled/damaged/not-delivered are not charged.
/// </summary>
public static class ConnectedPoShortCloseSettlement
{
    public sealed record Snapshot(
        decimal FinalAcceptedValue,
        decimal CancelledRemainingValue,
        decimal AmountPaid,
        decimal RefundDue,
        decimal BalanceDue,
        decimal GoodReceivedQty,
        decimal CancelledRemainingQty,
        decimal OutstandingQty);

    public static Snapshot Compute(
        PurchaseOrder buyerPo,
        IReadOnlyList<SupplierPayable> payablesForReceipts,
        bool treatOutstandingAsCancelled = false)
    {
        ArgumentNullException.ThrowIfNull(buyerPo);
        ArgumentNullException.ThrowIfNull(payablesForReceipts);

        var finalAccepted = 0m;
        var cancelledValue = 0m;
        var goodQty = 0m;
        var cancelledQty = 0m;
        var outstanding = 0m;

        foreach (var line in buyerPo.Lines)
        {
            goodQty += line.ReceivedQty;
            var cancelledOnLine = line.ClosedShortQty
                + (treatOutstandingAsCancelled ? line.OutstandingQty : 0m);
            cancelledQty += cancelledOnLine;
            outstanding += treatOutstandingAsCancelled ? 0m : line.OutstandingQty;
            finalAccepted += SaleMoney.RoundMoney(line.ReceivedQty * line.UnitPurchaseCost);
            cancelledValue += SaleMoney.RoundMoney(cancelledOnLine * line.UnitPurchaseCost);
        }

        finalAccepted = SaleMoney.RoundMoney(finalAccepted);
        cancelledValue = SaleMoney.RoundMoney(cancelledValue);

        var amountPaid = 0m;
        foreach (var payable in payablesForReceipts.Where(p => p.Status != SupplierPayableStatus.Voided))
        {
            amountPaid += payable.PaidAmount;
        }

        amountPaid = SaleMoney.RoundMoney(amountPaid);
        var refundDue = amountPaid > finalAccepted
            ? SaleMoney.RoundMoney(amountPaid - finalAccepted)
            : 0m;
        var balanceDue = finalAccepted > amountPaid
            ? SaleMoney.RoundMoney(finalAccepted - amountPaid)
            : 0m;

        return new Snapshot(
            finalAccepted,
            cancelledValue,
            amountPaid,
            refundDue,
            balanceDue,
            goodQty,
            cancelledQty,
            outstanding);
    }
}
