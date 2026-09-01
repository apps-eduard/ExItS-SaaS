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

    Task AddAsync(InventoryTransfer transfer, CancellationToken cancellationToken = default);

    Task UpdateAsync(InventoryTransfer transfer, CancellationToken cancellationToken = default);

    Task<string> AllocateNextNumberAsync(
        PosOrganizationId organizationId,
        DateOnly businessDateUtc,
        CancellationToken cancellationToken = default);
}

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
}

public interface IInventoryTransferAlertSink
{
    Task PublishAsync(InventoryTransferAlert alert, CancellationToken cancellationToken = default);
}
