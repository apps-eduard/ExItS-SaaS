using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ExItS.Platform.Api.Personal;

[Authorize]
public sealed class PersonalProfileHub : Hub
{
    public static string UserGroup(Guid userId) => $"personal-profile:{userId:D}";

    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(userId, out var parsed))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, UserGroup(parsed)).ConfigureAwait(false);
        }

        await base.OnConnectedAsync().ConfigureAwait(false);
    }
}
