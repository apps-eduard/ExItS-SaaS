namespace ExItS.PinoyBusinessPOS.Application.ConnectedSuppliers;

/// <summary>
/// Publishes customer-order lifecycle events into a Personal Platform in-app notification inbox.
/// Best-effort: order mutations must not fail when Platform notify is unavailable.
/// </summary>
public interface IPersonalBusinessNotificationPublisher
{
    Task PublishAsync(
        Guid sourceOrganizationId,
        Guid recipientPlatformUserId,
        string relatedType,
        string relatedId,
        string title,
        string preview,
        CancellationToken cancellationToken = default);
}

/// <summary>No-op publisher for unit tests and hosts without Platform wiring.</summary>
public sealed class NoOpPersonalBusinessNotificationPublisher : IPersonalBusinessNotificationPublisher
{
    public Task PublishAsync(
        Guid sourceOrganizationId,
        Guid recipientPlatformUserId,
        string relatedType,
        string relatedId,
        string title,
        string preview,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
