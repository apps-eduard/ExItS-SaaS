using System.Security.Claims;
using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.CustomerOrdering;
using ExItS.PinoyBusinessPOS.Application.Realtime;
using Microsoft.AspNetCore.SignalR;

namespace ExItS.PinoyBusinessPOS.Api.Realtime;

/// <summary>
/// POS-owned realtime hub for customer orders and payment status. Clients join server-authorized groups only.
/// Anonymous connections are aborted (POS uses bearer middleware, not ASP.NET authentication schemes).
/// </summary>
public sealed class ExitsPosRealtimeHub(
    IPosCommercialAccessAccessor commercialAccess,
    ICustomerOrderRepository orders) : Hub
{
    public const string HubPath = "/hubs/pos-realtime";
    public const string ClientMethod = "PosRealtimeEvent";

    public override async Task OnConnectedAsync()
    {
        if (!TryGetUserId(out var userId))
        {
            Context.Abort();
            return;
        }

        await Groups
            .AddToGroupAsync(Context.ConnectionId, PosRealtimeGroupNames.PersonalBuyer(userId))
            .ConfigureAwait(false);

        await base.OnConnectedAsync().ConfigureAwait(false);
    }

    public async Task JoinOrganization(Guid organizationId)
    {
        if (!PosRealtimeHubAuthorization.TryAuthorizeSellerOrganization(
                Context.GetHttpContext(),
                commercialAccess,
                organizationId))
        {
            return;
        }

        await Groups
            .AddToGroupAsync(Context.ConnectionId, PosRealtimeGroupNames.Organization(organizationId))
            .ConfigureAwait(false);
    }

    public Task LeaveOrganization(Guid organizationId)
    {
        if (organizationId == Guid.Empty)
        {
            return Task.CompletedTask;
        }

        return Groups.RemoveFromGroupAsync(
            Context.ConnectionId,
            PosRealtimeGroupNames.Organization(organizationId));
    }

    public async Task JoinOrder(Guid organizationId, Guid orderId)
    {
        if (!await PosRealtimeHubAuthorization
                .TryAuthorizeOrderWatchAsync(
                    Context.GetHttpContext(),
                    commercialAccess,
                    orders,
                    organizationId,
                    orderId,
                    Context.ConnectionAborted)
                .ConfigureAwait(false))
        {
            return;
        }

        await Groups
            .AddToGroupAsync(Context.ConnectionId, PosRealtimeGroupNames.Order(organizationId, orderId))
            .ConfigureAwait(false);
    }

    public Task LeaveOrder(Guid organizationId, Guid orderId)
    {
        if (organizationId == Guid.Empty || orderId == Guid.Empty)
        {
            return Task.CompletedTask;
        }

        return Groups.RemoveFromGroupAsync(
            Context.ConnectionId,
            PosRealtimeGroupNames.Order(organizationId, orderId));
    }

    private bool TryGetUserId(out Guid userId)
    {
        var raw = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(raw, out userId) && userId != Guid.Empty;
    }
}
