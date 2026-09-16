using ExItS.PinoyBusinessPOS.Domain.Credit;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;

/// <summary>
/// Connected PO Utang credit reservation math.
/// Available = CreditLimit − OutstandingUtang − ActivePoReservations (when Approved).
/// Draft buyer POs do not reserve; reservation starts on submit (CPO exists).
/// </summary>
public static class ConnectedPoUtangCredit
{
    /// <summary>
    /// Statuses that keep an Utang PO amount reserved against available credit.
    /// Declined / Withdrawn release immediately. Fully converted credit (posted == base) yields 0 reserved.
    /// </summary>
    public static bool IsActiveReservingStatus(ConnectedPurchaseOrderStatus status) =>
        status is ConnectedPurchaseOrderStatus.New
            or ConnectedPurchaseOrderStatus.ChangesProposed
            or ConnectedPurchaseOrderStatus.Accepted
            or ConnectedPurchaseOrderStatus.Preparing
            or ConnectedPurchaseOrderStatus.Fulfilled;

    public static bool UsesUtang(ConnectedPoPaymentTerm term) =>
        term == ConnectedPoPaymentTerm.Utang;

    /// <summary>
    /// Gross amount this PO would reserve before subtracting already-posted Utang (receipt conversions).
    /// Prefers confirmed total when the supplier has accepted / confirmed lines.
    /// </summary>
    public static decimal ReservationBaseAmount(ConnectedPurchaseOrder order)
    {
        if (!UsesUtang(order.EffectivePaymentTerm))
        {
            return 0m;
        }

        if (!IsActiveReservingStatus(order.Status))
        {
            return 0m;
        }

        var confirmed = order.ConfirmedTotalAmount;
        if (order.Status is ConnectedPurchaseOrderStatus.Accepted
            or ConnectedPurchaseOrderStatus.Preparing
            or ConnectedPurchaseOrderStatus.Fulfilled
            || order.Lines.Any(l => l.ConfirmedQty is not null))
        {
            return SaleMoney.RoundMoney(confirmed);
        }

        if (order.Status == ConnectedPurchaseOrderStatus.ChangesProposed)
        {
            return SaleMoney.RoundMoney(order.ProposedTotalAmount);
        }

        return SaleMoney.RoundMoney(order.TotalAmount);
    }

    /// <summary>
    /// Remaining active reservation: base − already converted to Outstanding Utang via receipts.
    /// </summary>
    public static decimal ActiveReservationAmount(ConnectedPurchaseOrder order)
    {
        var bas = ReservationBaseAmount(order);
        if (bas <= 0m)
        {
            return 0m;
        }

        var remaining = bas - order.CreditPostedAmount;
        return remaining <= 0m ? 0m : SaleMoney.RoundMoney(remaining);
    }

    /// <summary>
    /// Available = Approved ? max(0, limit − outstanding − activePoReservations) : 0.
    /// </summary>
    public static decimal AvailableCredit(
        CustomerCreditPolicyStatus status,
        decimal creditLimit,
        decimal outstandingUtang,
        decimal activePoReservations)
    {
        if (status != CustomerCreditPolicyStatus.Approved)
        {
            return 0m;
        }

        var available = creditLimit - outstandingUtang - activePoReservations;
        return available <= 0m ? 0m : SaleMoney.RoundMoney(available);
    }

    public static decimal SumActiveReservations(
        IEnumerable<ConnectedPurchaseOrder> orders,
        Guid? excludeConnectedPurchaseOrderId = null)
    {
        decimal sum = 0m;
        foreach (var order in orders)
        {
            if (excludeConnectedPurchaseOrderId is Guid exclude
                && order.Id.Value == exclude)
            {
                continue;
            }

            sum += ActiveReservationAmount(order);
        }

        return SaleMoney.RoundMoney(sum);
    }
}
