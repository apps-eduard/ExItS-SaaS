using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Offline;

namespace ExItS.PinoyBusinessPOS.ApiClient;

/// <summary>
/// MB2-01C-H1: permanently reject any legacy offline CatalogProductCreate outbox rows.
/// Canonical product create is ONLINE_REQUIRED; drafts are deferred and not ProductId authority.
/// Constructor dependencies retained for existing DI registrations.
/// </summary>
public sealed class CatalogProductCreateOfflineDispatcher(
    IPosCatalogClient client,
    PendingProductImageStore pendingImages) : IOfflineOperationDispatcher
{
    public bool CanHandle(string operationType) =>
        string.Equals(operationType, OfflineOperationTypes.CatalogProductCreate, StringComparison.Ordinal);

    public Task<OfflineDispatchResult> DispatchAsync(
        OfflineOperationEnvelope envelope,
        ReadOnlyMemory<byte> plaintextPayload,
        CancellationToken ct = default)
    {
        _ = client;
        _ = pendingImages;
        _ = envelope;
        _ = plaintextPayload;
        _ = ct;
        return Task.FromResult(new OfflineDispatchResult(
            false,
            OfflineFailureClass.Permanent,
            CatalogProductOfflineSyncService.OnlineRequiredErrorCode,
            "Creating a product requires an internet connection so we can check for duplicates across your organization.",
            null));
    }
}
