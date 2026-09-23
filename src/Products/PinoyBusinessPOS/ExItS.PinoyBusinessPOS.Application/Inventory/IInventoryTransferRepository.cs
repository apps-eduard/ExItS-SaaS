using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;

namespace ExItS.PinoyBusinessPOS.Application.Inventory;

public interface IInventoryTransferRepository
{
    Task<InventoryTransfer?> GetByIdAsync(
        PosOrganizationId organizationId,
        InventoryTransferId transferId,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<InventoryTransfer> Items, int TotalCount)> ListAsync(
        PosOrganizationId organizationId,
        InventoryTransferFilter filter,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InventoryTransfer>> ListByStockRequestIdAsync(
        PosOrganizationId organizationId,
        StockRequestId stockRequestId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InventoryTransfer>> ListByRootTransferIdAsync(
        PosOrganizationId organizationId,
        InventoryTransferId rootTransferId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Open (InTransit / PartiallyReceived) transfer line outstanding for a branch,
    /// optionally limited to product ids. Used for inventory reserved/in-transit badges.
    /// </summary>
    Task<IReadOnlyList<InventoryTransferOpenCommitment>> ListOpenCommitmentsForBranchAsync(
        PosOrganizationId organizationId,
        PosBranchId branchId,
        IReadOnlyCollection<CatalogProductId>? productIds = null,
        CancellationToken cancellationToken = default);

    Task AddAsync(InventoryTransfer transfer, CancellationToken cancellationToken = default);

    Task UpdateAsync(InventoryTransfer transfer, CancellationToken cancellationToken = default);

    Task<string> AllocateNextNumberAsync(
        PosOrganizationId organizationId,
        DateOnly businessDateUtc,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves authoritative inventory-transfer transaction refs for stock movements.
    /// SourceId may be transfer id, receipt id, receipt-line id, or damage-custody id
    /// depending on <see cref="StockMovement.MovementType"/>.
    /// Keyed by movement id.
    /// </summary>
    Task<IReadOnlyDictionary<Guid, InventoryTransferTransactionRef>> ResolveStockMovementTransactionRefsAsync(
        PosOrganizationId organizationId,
        IReadOnlyList<StockMovement> movements,
        CancellationToken cancellationToken = default);
}

/// <summary>Authoritative transfer document linked from a stock movement.</summary>
public sealed record InventoryTransferTransactionRef(
    Guid TransferId,
    string? TransferNumber);

/// <summary>Open transfer commitment for inventory badge / reservation drawer.</summary>
public sealed record InventoryTransferOpenCommitment(
    Guid TransferId,
    string? TransferNumber,
    Guid ProductId,
    decimal OutstandingQuantity,
    /// <summary><c>Outbound</c> when acting branch is source; <c>Inbound</c> when destination.</summary>
    string Direction,
    Guid PeerBranchId,
    DateTimeOffset CreatedAtUtc);

public interface IInventoryBranchBalanceRepository
{
    Task<InventoryBranchBalance?> GetAsync(
        PosOrganizationId organizationId,
        PosBranchId branchId,
        Domain.Catalog.CatalogProductId productId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InventoryBranchBalance>> ListByProductIdsAsync(
        PosOrganizationId organizationId,
        IReadOnlyCollection<Domain.Catalog.CatalogProductId> productIds,
        CancellationToken cancellationToken = default);

    async Task<IReadOnlyList<InventoryBranchBalance>> ListByBranchAndProductIdsAsync(
        PosOrganizationId organizationId,
        PosBranchId branchId,
        IReadOnlyCollection<Domain.Catalog.CatalogProductId> productIds,
        CancellationToken cancellationToken = default)
    {
        var all = await ListByProductIdsAsync(organizationId, productIds, cancellationToken).ConfigureAwait(false);
        return all.Where(b => b.BranchId == branchId).ToList();
    }

    Task UpsertAsync(InventoryBranchBalance balance, CancellationToken cancellationToken = default);
}

public interface IOrganizationBranchDirectory
{
    Task<bool> ExistsInOrganizationAsync(
        Guid organizationId,
        Guid branchId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<Guid, string>> GetNamesAsync(
        Guid organizationId,
        IReadOnlyCollection<Guid> branchIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Organization structural primary branch id (not staff assignment-filtered).
    /// </summary>
    Task<Guid?> GetPrimaryBranchIdAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<Guid?>(null);

    Task<bool> IsActiveInOrganizationAsync(
        Guid organizationId,
        Guid branchId,
        CancellationToken cancellationToken = default) =>
        ExistsInOrganizationAsync(organizationId, branchId, cancellationToken);

    /// <summary>
    /// Platform branch type code (<c>Retail</c> / <c>Warehouse</c>). Defaults to Retail when unknown.
    /// </summary>
    Task<string> GetBranchTypeAsync(
        Guid organizationId,
        Guid branchId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult("Retail");
}

public interface IInventoryTransferAlertSink
{
    Task PublishAsync(InventoryTransferAlert alert, CancellationToken cancellationToken = default);
}
