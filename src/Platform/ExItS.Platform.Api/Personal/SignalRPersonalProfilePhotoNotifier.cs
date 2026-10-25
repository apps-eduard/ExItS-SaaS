using ExItS.Platform.Application.Personal;
using ExItS.Platform.Application.Realtime;
using Microsoft.AspNetCore.SignalR;

namespace ExItS.Platform.Api.Personal;

public sealed class SignalRPersonalProfilePhotoNotifier(
    IHubContext<PersonalProfileHub> hub,
    IRealtimeChangePublisher realtime) : IPersonalProfilePhotoNotifier
{
    public async Task PhotoUpdatedAsync(Guid userId, string profilePhotoUrl, CancellationToken cancellationToken = default)
    {
        await hub.Clients
            .Group(PersonalProfileHub.UserGroup(userId))
            .SendAsync("ProfilePhotoUpdated", new { profilePhotoUrl }, cancellationToken)
            .ConfigureAwait(false);

        await RealtimePublish
            .PersonalSettingsChangedAsync(
                realtime,
                userId,
                RealtimeEventTypes.PersonalProfilePhotoChanged,
                data: new Dictionary<string, string> { ["profilePhotoUrl"] = profilePhotoUrl },
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }
}
