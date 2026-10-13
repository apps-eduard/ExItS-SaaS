namespace ExItS.PinoyBusinessPOS.Domain.CustomerOrdering;

/// <summary>
/// Payment lifecycle, kept separate from order and fulfillment status.
/// Cash starts Unpaid. Manual GCash starts Pending until the seller confirms receipt.
/// Utang stays Unpaid; debt is posted through Business Utang on completion.
/// </summary>
public enum CustomerOrderPaymentStatus
{
    Unpaid = 0,
    Pending = 1,
    Paid = 2
}
