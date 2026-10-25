namespace ExItS.PinoyBusinessPOS.Domain.CustomerOrdering;

/// <summary>Seller or buyer cancellation reason (distinct from reject-at-submit).</summary>
public enum CustomerOrderCancelReason
{
    CustomerRequest = 0,
    CustomerRefusedPickup = 1,
    CustomerRefusedDelivery = 2,
    SellerUnableToFulfill = 3,
    AllItemsCancelled = 4,
    Other = 5
}
