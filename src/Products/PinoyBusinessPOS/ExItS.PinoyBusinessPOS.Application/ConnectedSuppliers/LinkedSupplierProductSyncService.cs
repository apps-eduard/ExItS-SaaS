using ExItS.PinoyBusinessPOS.Application.Abstractions;

namespace ExItS.PinoyBusinessPOS.Application.ConnectedSuppliers;

/// <summary>
/// Applies the server's linked-product delta to the selective local projection.
/// Supplier catalog search remains server-only and is deliberately not used here.
/// </summary>
public sealed class LinkedSupplierProductSyncService(
    IPosConnectedSupplierClient client,
    ILinkedSupplierProductStore store,
    TimeProvider timeProvider) : ILinkedSupplierProductSyncService
{
    public async Task<LinkedSupplierProductSyncResult> SyncAsync(
        Guid relationshipId,
        CancellationToken ct = default)
    {
        var sinceVersion = await store.GetSyncVersionAsync(relationshipId, ct).ConfigureAwait(false);
        var result = await client.SyncLinksAsync(relationshipId, sinceVersion, ct).ConfigureAwait(false);
        if (!result.IsSuccess || result.Data is null)
        {
            return new(false, 0, 0, sinceVersion, result.Error?.ErrorCode);
        }

        var syncedAt = timeProvider.GetUtcNow();
        var changed = result.Data.Changed.Select(item => new LocalLinkedSupplierProduct(
            item.LinkId,
            item.RelationshipId,
            item.SupplierOrganizationId,
            item.BuyerProductId,
            item.SupplierProductId,
            item.SupplierSkuSnapshot,
            item.SupplierNameSnapshot,
            item.UnitOfMeasureCode,
            item.LastKnownOrderPrice,
            IsOrderable: item.IsActive,
            item.IsActive,
            item.SyncVersion,
            item.UpdatedAtUtc,
            syncedAt,
            item.MultiplierToBase,
            item.PackageLabel)).ToList();

        await store.UpsertRangeAsync(changed, ct).ConfigureAwait(false);
        await store.RemoveIdsAsync(relationshipId, result.Data.RemovedIds, ct).ConfigureAwait(false);
        await store.SetSyncVersionAsync(relationshipId, result.Data.Cursor, syncedAt, ct).ConfigureAwait(false);

        return new(true, changed.Count, result.Data.RemovedIds.Count, result.Data.Cursor);
    }
}
