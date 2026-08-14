namespace ExItS.PinoyBusinessPOS.Application.Abstractions;

public sealed record LocalLinkedSupplierProduct(
    Guid LinkId,
    Guid RelationshipId,
    Guid SupplierOrganizationId,
    Guid BuyerProductId,
    Guid SupplierProductId,
    string? SupplierSku,
    string ProductName,
    string UnitOfMeasure,
    decimal LastKnownOrderPrice,
    bool IsOrderable,
    bool IsActive,
    long SyncVersion,
    DateTimeOffset SupplierUpdatedAtUtc,
    DateTimeOffset SyncedAtUtc,
    decimal MultiplierToBase = 1m,
    string? PackageLabel = null);

public sealed record LocalConnectedSupplier(
    Guid RelationshipId,
    Guid SupplierOrganizationId,
    Guid? BuyerSupplierId,
    string DisplayName,
    string Status,
    DateTimeOffset? LastSyncedUtc);

public interface ILocalConnectedSupplierStore
{
    Task UpsertConnectedSuppliersAsync(IReadOnlyList<LocalConnectedSupplier> suppliers, CancellationToken ct = default);
    Task<IReadOnlyList<LocalConnectedSupplier>> ListConnectedSuppliersAsync(CancellationToken ct = default);
}

/// <summary>
/// Selective offline projection of products explicitly linked by the buyer.
/// This store must never contain a supplier's complete catalog.
/// </summary>
public interface ILinkedSupplierProductStore
{
    Task UpsertRangeAsync(IReadOnlyList<LocalLinkedSupplierProduct> products, CancellationToken ct = default);
    Task RemoveIdsAsync(Guid relationshipId, IReadOnlyList<Guid> linkIds, CancellationToken ct = default);
    Task<IReadOnlyList<LocalLinkedSupplierProduct>> SearchLocalAsync(
        Guid relationshipId,
        string? query,
        int take,
        CancellationToken ct = default);
    Task<IReadOnlyList<LocalLinkedSupplierProduct>> ListByRelationshipAsync(
        Guid relationshipId,
        CancellationToken ct = default);
    Task<long> GetSyncVersionAsync(Guid relationshipId, CancellationToken ct = default);
    Task SetSyncVersionAsync(Guid relationshipId, long syncVersion, DateTimeOffset syncedAtUtc, CancellationToken ct = default);
}

public interface ILinkedSupplierProductSyncService
{
    Task<LinkedSupplierProductSyncResult> SyncAsync(Guid relationshipId, CancellationToken ct = default);
}

public sealed record LinkedSupplierProductSyncResult(
    bool Succeeded,
    int ChangedCount,
    int RemovedCount,
    long Cursor,
    string? ErrorCode = null);

public sealed record LocalConnectedPurchaseOrderDraft(
    Guid LocalId,
    Guid RelationshipId,
    Guid SupplierId,
    string PayloadJson,
    LocalEntitySyncState SyncState,
    DateTimeOffset UpdatedAtUtc);

/// <summary>Device-local draft only. Saving never means the order was submitted to the supplier.</summary>
public interface IConnectedPurchaseOrderDraftStore
{
    Task SaveAsync(LocalConnectedPurchaseOrderDraft draft, CancellationToken ct = default);
    Task<LocalConnectedPurchaseOrderDraft?> GetAsync(Guid localId, CancellationToken ct = default);
    Task DeleteAsync(Guid localId, CancellationToken ct = default);
}
