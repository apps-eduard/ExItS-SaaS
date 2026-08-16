namespace ExItS.PinoyBusinessPOS.Domain.CustomerOrdering;

public enum CustomerOrderRejectReason
{
    OutOfStock = 1,
    StoreTooBusy = 2,
    DeliveryUnavailable = 3,
    UnableToFulfill = 4,
    Other = 5
}
