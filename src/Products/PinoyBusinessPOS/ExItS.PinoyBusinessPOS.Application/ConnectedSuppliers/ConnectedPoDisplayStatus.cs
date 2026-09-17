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
    public const string Shipped = "Shipped";
    public const string PartiallyReceived = "PartiallyReceived";
    public const string Received = "Received";
    public const string ReceivedWithIssues = "ReceivedWithIssues";
    /// <summary>Supplier-facing: buyer outstanding is zero (good received and/or cancelled remaining).</summary>
    public const string Completed = "Completed";
    /// <summary>User-facing short-close completion (remaining quantity cancelled).</summary>
    public const string CompletedRemainingCancelled = "CompletedRemainingCancelled";
    public const string AwaitingBuyerReceipt = "AwaitingBuyerReceipt";
    public const string Withdrawn = "Withdrawn";
    public const string Cancelled = "Cancelled";
    public const string ChangesNeedApproval = "ChangesNeedApproval";

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

        if (connected.Status == ConnectedPurchaseOrderStatus.ChangesProposed)
        {
            return ChangesNeedApproval;
        }

        if (po.Status == PurchaseOrderStatus.PartiallyReceived)
        {
            return PartiallyReceived;
        }

        if (po.Status == PurchaseOrderStatus.Received)
        {
            if (po.RemainingClosedAtUtc is not null)
            {
                return CompletedRemainingCancelled;
            }

            if (po.HasReceivingIssues)
            {
                return ReceivedWithIssues;
            }

            return Received;
        }

        return connected.Status switch
        {
            ConnectedPurchaseOrderStatus.New => WaitingForSupplier,
            ConnectedPurchaseOrderStatus.ChangesProposed => ChangesNeedApproval,
            ConnectedPurchaseOrderStatus.Accepted => SupplierAccepted,
            ConnectedPurchaseOrderStatus.Preparing => Preparing,
            // Ready/Shipped = awaiting buyer receipt; never treat as Completed.
            ConnectedPurchaseOrderStatus.Fulfilled => Shipped,
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
                // Completion is buyer-outstanding-driven, not seller Ready/Ship.
                if (buyerPo.RemainingClosedAtUtc is not null)
                {
                    return CompletedRemainingCancelled;
                }

                if (buyerPo.HasReceivingIssues)
                {
                    return ReceivedWithIssues;
                }

                return Completed;
            }
        }

        return connected.Status switch
        {
            ConnectedPurchaseOrderStatus.New => "New",
            ConnectedPurchaseOrderStatus.ChangesProposed => "ChangesProposed",
            ConnectedPurchaseOrderStatus.Accepted =>
                connected.FulfilledAtUtc is not null ? PartiallyReceived : "Accepted",
            ConnectedPurchaseOrderStatus.Preparing => Preparing,
            ConnectedPurchaseOrderStatus.Fulfilled => AwaitingBuyerReceipt,
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
            (ConnectedPurchaseOrderStatus.New, ConnectedPurchaseOrderStatus.ChangesProposed) => true,
            (ConnectedPurchaseOrderStatus.ChangesProposed, ConnectedPurchaseOrderStatus.Accepted) => true,
            (ConnectedPurchaseOrderStatus.ChangesProposed, ConnectedPurchaseOrderStatus.New) => true,
            (ConnectedPurchaseOrderStatus.ChangesProposed, ConnectedPurchaseOrderStatus.Withdrawn) => true,
            (ConnectedPurchaseOrderStatus.Accepted, ConnectedPurchaseOrderStatus.Preparing) => true,
            (ConnectedPurchaseOrderStatus.Accepted, ConnectedPurchaseOrderStatus.Fulfilled) => true,
            (ConnectedPurchaseOrderStatus.Preparing, ConnectedPurchaseOrderStatus.Fulfilled) => true,
            // Remaining fulfillment after partial buyer receipt.
            (ConnectedPurchaseOrderStatus.Fulfilled, ConnectedPurchaseOrderStatus.Accepted) => true,
            (ConnectedPurchaseOrderStatus.Fulfilled, ConnectedPurchaseOrderStatus.Preparing) => true,
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
    public const string PartiallyReceived = "ConnectedPurchaseOrderPartiallyReceived";
    public const string ReceivingIssue = "ConnectedPurchaseOrderReceivingIssue";
    public const string ChangesProposed = "ConnectedPurchaseOrderChangesProposed";
    public const string ChangesAccepted = "ConnectedPurchaseOrderChangesAccepted";
    public const string ChangesRejected = "ConnectedPurchaseOrderChangesRejected";
    public const string RemainingClosed = "ConnectedPurchaseOrderRemainingClosed";

    public static bool IsKnown(string? relatedType) =>
        string.Equals(relatedType, Submitted, StringComparison.Ordinal)
        || string.Equals(relatedType, Accepted, StringComparison.Ordinal)
        || string.Equals(relatedType, Declined, StringComparison.Ordinal)
        || string.Equals(relatedType, Preparing, StringComparison.Ordinal)
        || string.Equals(relatedType, Fulfilled, StringComparison.Ordinal)
        || string.Equals(relatedType, Withdrawn, StringComparison.Ordinal)
        || string.Equals(relatedType, Received, StringComparison.Ordinal)
        || string.Equals(relatedType, PartiallyReceived, StringComparison.Ordinal)
        || string.Equals(relatedType, ReceivingIssue, StringComparison.Ordinal)
        || string.Equals(relatedType, ChangesProposed, StringComparison.Ordinal)
        || string.Equals(relatedType, ChangesAccepted, StringComparison.Ordinal)
        || string.Equals(relatedType, ChangesRejected, StringComparison.Ordinal)
        || string.Equals(relatedType, RemainingClosed, StringComparison.Ordinal);

    public static bool IsBuyerFacing(string? relatedType) =>
        string.Equals(relatedType, Accepted, StringComparison.Ordinal)
        || string.Equals(relatedType, Declined, StringComparison.Ordinal)
        || string.Equals(relatedType, Preparing, StringComparison.Ordinal)
        || string.Equals(relatedType, Fulfilled, StringComparison.Ordinal)
        || string.Equals(relatedType, ChangesProposed, StringComparison.Ordinal)
        || string.Equals(relatedType, RemainingClosed, StringComparison.Ordinal);

    public static bool IsSupplierFacing(string? relatedType) =>
        string.Equals(relatedType, Submitted, StringComparison.Ordinal)
        || string.Equals(relatedType, Withdrawn, StringComparison.Ordinal)
        || string.Equals(relatedType, Received, StringComparison.Ordinal)
        || string.Equals(relatedType, PartiallyReceived, StringComparison.Ordinal)
        || string.Equals(relatedType, ReceivingIssue, StringComparison.Ordinal)
        || string.Equals(relatedType, ChangesAccepted, StringComparison.Ordinal)
        || string.Equals(relatedType, ChangesRejected, StringComparison.Ordinal);
}
