using ExItS.Platform.Domain.Identity;

namespace ExItS.Platform.Application.Personal;

/// <summary>No-vendor push sink — always reports skipped/no-op delivery for foundation wiring.</summary>
public sealed class NullPersonalPushNotificationSink : IPersonalPushNotificationSink
{
    public Task<bool> TryDeliverAsync(
        PlatformUserId recipientUserIdentityId,
        string title,
        string minimizedPreview,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(false);
}
