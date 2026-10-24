namespace ExItS.Platform.Application.Organizations;

/// <summary>
/// Pushed to Personal customers watching a seller storefront when branch hours / pause / fulfillment
/// readiness changes (SignalR). Clients refetch POS storefront after this event.
/// </summary>
public sealed record CustomerStorefrontAvailabilityChanged(
    Guid OrganizationId,
    Guid BranchId,
    string? StoreStatusMessage,
    bool CustomerOrderingOperational,
    bool PickupOperational,
    bool DeliveryOperational,
    bool OnlineOrdersPaused,
    bool StoreIsOpenNow);

public interface ICustomerStorefrontAvailabilityNotifier
{
    Task AvailabilityChangedAsync(
        CustomerStorefrontAvailabilityChanged change,
        CancellationToken cancellationToken = default);
}
