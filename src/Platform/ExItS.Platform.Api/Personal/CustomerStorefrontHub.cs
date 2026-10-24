using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ExItS.Platform.Api.Personal;

/// <summary>
/// Personal customers join a seller organization group to receive storefront availability
/// updates (store hours, pause, fulfillment readiness).
/// </summary>
[Authorize]
public sealed class CustomerStorefrontHub : Hub
{
    public static string SellerGroup(Guid organizationId) =>
        $"customer-storefront:seller:{organizationId:D}";

    public Task JoinSeller(Guid organizationId)
    {
        if (organizationId == Guid.Empty)
        {
            return Task.CompletedTask;
        }

        return Groups.AddToGroupAsync(Context.ConnectionId, SellerGroup(organizationId));
    }

    public Task LeaveSeller(Guid organizationId)
    {
        if (organizationId == Guid.Empty)
        {
            return Task.CompletedTask;
        }

        return Groups.RemoveFromGroupAsync(Context.ConnectionId, SellerGroup(organizationId));
    }
}
