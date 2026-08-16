namespace ExItS.PinoyBusinessPOS.Domain.CustomerOrdering;

public enum CustomerOrderFulfillmentStatus
{
    Pending = 0,
    Preparing = 1,
    Ready = 2,
    OutForDelivery = 3,
    Delivered = 4,
    ReadyForPickup = 5,
    Collected = 6
}
