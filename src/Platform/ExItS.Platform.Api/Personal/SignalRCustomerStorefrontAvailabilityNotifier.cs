using ExItS.Platform.Application.Organizations;
using Microsoft.AspNetCore.SignalR;

namespace ExItS.Platform.Api.Personal;

public sealed class SignalRCustomerStorefrontAvailabilityNotifier(
    IHubContext<CustomerStorefrontHub> hub) : ICustomerStorefrontAvailabilityNotifier
{
    public Task AvailabilityChangedAsync(
        CustomerStorefrontAvailabilityChanged change,
        CancellationToken cancellationToken = default) =>
        hub.Clients
            .Group(CustomerStorefrontHub.SellerGroup(change.OrganizationId))
            .SendAsync(
                "StorefrontAvailabilityChanged",
                new
                {
                    organizationId = change.OrganizationId,
                    branchId = change.BranchId,
                    storeStatusMessage = change.StoreStatusMessage,
                    customerOrderingOperational = change.CustomerOrderingOperational,
                    pickupOperational = change.PickupOperational,
                    deliveryOperational = change.DeliveryOperational,
                    onlineOrdersPaused = change.OnlineOrdersPaused,
                    storeIsOpenNow = change.StoreIsOpenNow,
                },
                cancellationToken);
}
