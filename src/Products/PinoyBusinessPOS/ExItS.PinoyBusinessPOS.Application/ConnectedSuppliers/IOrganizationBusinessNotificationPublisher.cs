using ExItS.PinoyBusinessPOS.Application.Common;

namespace ExItS.PinoyBusinessPOS.Application.ConnectedSuppliers;

/// <summary>
/// Publishes Connected Supplier lifecycle events into the Platform Organization in-app notification inbox.
/// Best-effort: relationship mutations must not fail when Platform notify is unavailable.
/// </summary>
public interface IOrganizationBusinessNotificationPublisher
{
    Task PublishAsync(
        Guid sourceOrganizationId,
        Guid recipientOrganizationId,
        string relatedType,
        string relatedId,
        string title,
        string preview,
        CancellationToken cancellationToken = default);

    Task MarkRelatedReadAsync(
        Guid organizationId,
        string relatedType,
        string relatedId,
        CancellationToken cancellationToken = default);
}

public static class SupplierConnectionNotificationTypes
{
    public const string Requested = "SupplierConnectionRequested";
    public const string Accepted = "SupplierConnectionAccepted";
    public const string Declined = "SupplierConnectionDeclined";
    public const string AcceptedConfirmation = "SupplierConnectionAcceptedConfirmation";
    public const string DeclinedConfirmation = "SupplierConnectionDeclinedConfirmation";

    public static bool IsSupplierConnection(string? relatedType) =>
        string.Equals(relatedType, Requested, StringComparison.Ordinal)
        || string.Equals(relatedType, Accepted, StringComparison.Ordinal)
        || string.Equals(relatedType, Declined, StringComparison.Ordinal)
        || string.Equals(relatedType, AcceptedConfirmation, StringComparison.Ordinal)
        || string.Equals(relatedType, DeclinedConfirmation, StringComparison.Ordinal);

    public static bool IsSupplierLocalConfirmation(string? relatedType) =>
        string.Equals(relatedType, AcceptedConfirmation, StringComparison.Ordinal)
        || string.Equals(relatedType, DeclinedConfirmation, StringComparison.Ordinal);
}

/// <summary>No-op publisher for unit tests and hosts without Platform wiring.</summary>
public sealed class NoOpOrganizationBusinessNotificationPublisher : IOrganizationBusinessNotificationPublisher
{
    public Task PublishAsync(
        Guid sourceOrganizationId,
        Guid recipientOrganizationId,
        string relatedType,
        string relatedId,
        string title,
        string preview,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task MarkRelatedReadAsync(
        Guid organizationId,
        string relatedType,
        string relatedId,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
