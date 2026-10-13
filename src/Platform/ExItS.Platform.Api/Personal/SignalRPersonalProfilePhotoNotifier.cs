using ExItS.Platform.Application.Personal;
using Microsoft.AspNetCore.SignalR;

namespace ExItS.Platform.Api.Personal;

public sealed class SignalRPersonalProfilePhotoNotifier(IHubContext<PersonalProfileHub> hub) : IPersonalProfilePhotoNotifier
{
    public Task PhotoUpdatedAsync(Guid userId, string profilePhotoUrl, CancellationToken cancellationToken = default) =>
        hub.Clients
            .Group(PersonalProfileHub.UserGroup(userId))
            .SendAsync("ProfilePhotoUpdated", new { profilePhotoUrl }, cancellationToken);
}