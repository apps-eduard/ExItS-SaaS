using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;

namespace ExItS.PinoyBusinessPOS.Application.Inventory;

public interface IInventoryLotRepository
{
    Task<InventoryLot?> GetByIdAsync(
        PosOrganizationId organizationId,
        InventoryLotId lotId,
        CancellationToken cancellationToken = default);

    Task<InventoryLot?> FindAsync(
        PosOrganizationId organizationId,
        CatalogProductId productId,
        DateOnly expirationDate,
        string normalizedLotNumber,
        PosBranchId? branchId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InventoryLot>> ListOnHandAsync(
        PosOrganizationId organizationId,
        CatalogProductId productId,
        PosBranchId? branchId,
        bool includeDepleted,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<InventoryLot> Items, int TotalCount)> ListPagedAsync(
        PosOrganizationId organizationId,
        CatalogProductId productId,
        PosBranchId? branchId,
        bool includeDepleted,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<InventoryLot> Items, int TotalCount)> ListExpiringPagedAsync(
        PosOrganizationId organizationId,
        PosBranchId? branchId,
        DateOnly expireOnOrBefore,
        DateOnly? expireOnOrAfter,
        string? search,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts on-hand expired lots (date-based) and near-expiry lots using each product's
    /// <c>EffectiveExpirationWarningDays</c> (joined from catalog products).
    /// </summary>
    Task<(int ExpiredCount, int NearExpiryCount)> CountExpiryAsync(
        PosOrganizationId organizationId,
        DateOnly today,
        CancellationToken cancellationToken = default);

    Task AddAsync(InventoryLot lot, CancellationToken cancellationToken = default);

    /// <summary>
    /// When operational branch is bound and lots were created org-wide (BranchId null),
    /// reassign them to the branch so branch-scoped queries match allocation.
    /// </summary>
    Task AdoptOrgLevelLotsForBranchAsync(
        PosOrganizationId organizationId,
        CatalogProductId productId,
        PosBranchId branchId,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(InventoryLot lot, CancellationToken cancellationToken = default);

    Task AddMovementAsync(InventoryLotMovement movement, CancellationToken cancellationToken = default);

    Task<bool> HasMovementAsync(
        PosOrganizationId organizationId,
        Guid sourceId,
        InventoryLotId lotId,
        StockMovementType movementType,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InventoryLotMovement>> ListBySourceAsync(
        PosOrganizationId organizationId,
        Guid sourceId,
        StockMovementType movementType,
        CancellationToken cancellationToken = default);
}
