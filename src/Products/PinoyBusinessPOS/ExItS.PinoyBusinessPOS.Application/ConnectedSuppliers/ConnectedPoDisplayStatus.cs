using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Purchasing;

namespace ExItS.PinoyBusinessPOS.Application.ConnectedSuppliers;

/// <summary>
/// Buyer/supplier friendly connected-PO display labels derived from buyer PO + supplier CPO state.
/// Does not replace <see cref="PurchaseOrderStatus"/> for external suppliers.
/// </summary>
public static class ConnectedPoDisplayStatus
{
    public const string Draft = "Draft";
    public const string Sent = "Sent";
    public const string WaitingForSupplier = "WaitingForSupplier";
    public const string SupplierAccepted = "SupplierAccepted";
    public const string SupplierDeclined = "SupplierDeclined";
    public const string Preparing = "Preparing";
    public const string Ready = "Ready";
    public const string PartiallyReceived = "PartiallyReceived";
    public const string Received = "Received";
    public const string ReceivedWithIssues = "ReceivedWithIssues";
    public const string Withdrawn = "Withdrawn";
    public const string Cancelled = "Cancelled";

    public static string ForBuyer(PurchaseOrder po, ConnectedPurchaseOrder? connected)
    {
        if (po.Status == PurchaseOrderStatus.Draft)
        {
            return Draft;
        }

        if (po.Status == PurchaseOrderStatus.Cancelled)
        {
            return connected?.Status == ConnectedPurchaseOrderStatus.Withdrawn ? Withdrawn : Cancelled;
        }

        if (connected is null)
        {
            return po.Status switch
            {
                PurchaseOrderStatus.Ordered => Sent,
                PurchaseOrderStatus.PartiallyReceived => PartiallyReceived,
                PurchaseOrderStatus.Received => Received,
                _ => po.Status.ToString()
            };
        }

        if (connected.Status == ConnectedPurchaseOrderStatus.Declined)
        {
            return SupplierDeclined;
        }

        if (connected.Status == ConnectedPurchaseOrderStatus.Withdrawn)
        {
            return Withdrawn;
        }

        if (po.Status == PurchaseOrderStatus.PartiallyReceived)
        {
            return PartiallyReceived;
        }

        if (po.Status == PurchaseOrderStatus.Received)
        {
            return po.HasReceivingIssues ? ReceivedWithIssues : Received;
        }

        return connected.Status switch
        {
            ConnectedPurchaseOrderStatus.New => WaitingForSupplier,
            ConnectedPurchaseOrderStatus.Accepted => SupplierAccepted,
            ConnectedPurchaseOrderStatus.Preparing => Preparing,
            ConnectedPurchaseOrderStatus.Fulfilled => Ready,
            _ => WaitingForSupplier
        };
    }

    public static string ForSupplier(ConnectedPurchaseOrder connected, PurchaseOrder? buyerPo = null)
    {
        if (connected.Status == ConnectedPurchaseOrderStatus.Declined)
        {
            return "Declined";
        }

        if (connected.Status == ConnectedPurchaseOrderStatus.Withdrawn)
        {
            return Withdrawn;
        }

        if (buyerPo is not null)
        {
            if (buyerPo.Status == PurchaseOrderStatus.PartiallyReceived)
            {
                return PartiallyReceived;
            }

            if (buyerPo.Status == PurchaseOrderStatus.Received)
            {
                return buyerPo.HasReceivingIssues ? ReceivedWithIssues : "ReceivedByBuyer";
            }
        }

        return connected.Status switch
        {
            ConnectedPurchaseOrderStatus.New => "New",
            ConnectedPurchaseOrderStatus.Accepted => "Accepted",
            ConnectedPurchaseOrderStatus.Preparing => Preparing,
            ConnectedPurchaseOrderStatus.Fulfilled => "Fulfilled",
            _ => connected.Status.ToString()
        };
    }

    public static bool IsValidConnectedStatusTransition(
        ConnectedPurchaseOrderStatus from,
        ConnectedPurchaseOrderStatus to)
    {
        if (from == to)
        {
            return true;
        }

        return (from, to) switch
        {
            (ConnectedPurchaseOrderStatus.New, ConnectedPurchaseOrderStatus.Accepted) => true,
            (ConnectedPurchaseOrderStatus.New, ConnectedPurchaseOrderStatus.Declined) => true,
            (ConnectedPurchaseOrderStatus.New, ConnectedPurchaseOrderStatus.Withdrawn) => true,
            (ConnectedPurchaseOrderStatus.Accepted, ConnectedPurchaseOrderStatus.Preparing) => true,
            (ConnectedPurchaseOrderStatus.Accepted, ConnectedPurchaseOrderStatus.Fulfilled) => true,
            (ConnectedPurchaseOrderStatus.Preparing, ConnectedPurchaseOrderStatus.Fulfilled) => true,
            _ => false
        };
    }
}

public static class ConnectedPurchaseOrderNotificationTypes
{
    public const string Submitted = "ConnectedPurchaseOrderSubmitted";
    public const string Accepted = "ConnectedPurchaseOrderAccepted";
    public const string Declined = "ConnectedPurchaseOrderDeclined";
    public const string Preparing = "ConnectedPurchaseOrderPreparing";
    public const string Fulfilled = "ConnectedPurchaseOrderFulfilled";
    public const string Withdrawn = "ConnectedPurchaseOrderWithdrawn";
    public const string Received = "ConnectedPurchaseOrderReceived";
    public const string ReceivingIssue = "ConnectedPurchaseOrderReceivingIssue";
}
