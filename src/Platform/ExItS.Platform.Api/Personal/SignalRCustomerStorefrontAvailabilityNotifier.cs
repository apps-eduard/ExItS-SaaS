using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Application.Realtime;
using Microsoft.AspNetCore.SignalR;

namespace ExItS.Platform.Api.Personal;

public sealed class SignalRCustomerStorefrontAvailabilityNotifier(
    IHubContext<CustomerStorefrontHub> hub,
    IRealtimeChangePublisher realtime) : ICustomerStorefrontAvailabilityNotifier
{
    public async Task AvailabilityChangedAsync(
        CustomerStorefrontAvailabilityChanged change,
        CancellationToken cancellationToken = default)
    {
        await hub.Clients
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
                cancellationToken)
            .ConfigureAwait(false);

        await RealtimePublish
            .BranchSettingsChangedAsync(
                realtime,
                change.OrganizationId,
                change.BranchId,
                RealtimeEventTypes.StorefrontAvailabilityChanged,
                data: new Dictionary<string, string>
                {
                    ["organizationId"] = change.OrganizationId.ToString("D"),
                    ["branchId"] = change.BranchId.ToString("D"),
                    ["customerOrderingOperational"] = change.CustomerOrderingOperational ? "true" : "false",
                    ["onlineOrdersPaused"] = change.OnlineOrdersPaused ? "true" : "false",
                    ["storeIsOpenNow"] = change.StoreIsOpenNow ? "true" : "false",
                },
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }
}
