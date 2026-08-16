namespace ExItS.PinoyBusinessPOS.Domain.CustomerOrdering;

/// <summary>
/// V1 payment placeholder kept separate from order and fulfillment status.
/// </summary>
public enum CustomerOrderPaymentStatus
{
    Unpaid = 0,
    Pending = 1,
    Paid = 2
}
