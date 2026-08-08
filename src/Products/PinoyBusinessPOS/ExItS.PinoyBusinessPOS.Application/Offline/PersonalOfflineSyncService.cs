using ExItS.PinoyBusinessPOS.Application.Abstractions;

namespace ExItS.PinoyBusinessPOS.Application.Offline;

public interface IPersonalOfflineSyncService
{
    Task<OfflineProcessBatchResult> TrySyncPendingAsync(CancellationToken ct = default);

    Task<int> GetPendingCountAsync(CancellationToken ct = default);
}

/// <summary>
/// Drives personal outbox sync when online. Uses the shared queue processor — never org POS APIs.
/// </summary>
public sealed class PersonalOfflineSyncService(
    IConnectivityService connectivity,
    IOfflineQueueProcessor queueProcessor,
    ILocalPersonalUtangStore personalStore,
    ILocalContextManager contextManager) : IPersonalOfflineSyncService
{
    public async Task<OfflineProcessBatchResult> TrySyncPendingAsync(CancellationToken ct = default)
    {
        if (!await connectivity.IsConnectedAsync(ct).ConfigureAwait(false))
        {
            return new OfflineProcessBatchResult(0, 0, 0, "SyncStatus_Offline");
        }

        var active = contextManager.ActiveContext;
        if (active is null
            || !PersonalLocalScope.IsPersonalContext(active.Identity.OrganizationId, active.Identity.ProductCode))
        {
            try
            {
                await personalStore.EnsurePersonalContextAsync(ct).ConfigureAwait(false);
            }
            catch
            {
                return new OfflineProcessBatchResult(0, 0, 0, "SyncStatus_Offline");
            }
        }

        return await queueProcessor.ProcessAvailableAsync(ct).ConfigureAwait(false);
    }

    public Task<int> GetPendingCountAsync(CancellationToken ct = default) =>
        personalStore.CountPendingSyncAsync(ct);
}
