using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.CustomerOrdering;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Purchasing;
using ExItS.PinoyBusinessPOS.Domain.Returns;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.Application.Inventory;

public interface IInventoryRepository
{
    Task<InventoryAccount?> GetByProductIdAsync(
        PosOrganizationId organizationId,
        CatalogProductId productId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InventoryAccount>> ListByProductIdsAsync(
        PosOrganizationId organizationId,
        IReadOnlyCollection<CatalogProductId> productIds,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<InventoryAccount> Items, int TotalCount)> ListAsync(
        PosOrganizationId organizationId,
        InventoryAccountFilter filter,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<InventoryAccount> Items, int TotalCount)> ListLowStockAsync(
        PosOrganizationId organizationId,
        string? search,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    /// <summary>All inventory accounts for the organization (report projection; MVP-scale).</summary>
    Task<IReadOnlyList<InventoryAccount>> ListAllAccountsAsync(
        PosOrganizationId organizationId,
        CancellationToken cancellationToken = default);

    Task AddAccountAsync(InventoryAccount account, CancellationToken cancellationToken = default);

    Task UpdateAccountAsync(InventoryAccount account, CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs <paramref name="action"/> under a transaction with advisory locks keyed by
    /// organization + product. Reloads accounts inside the lock before invoking the action.
    /// When an ambient transaction exists, locks join that transaction; otherwise a new
    /// ReadCommitted transaction is opened and committed after SaveChanges.
    /// </summary>
    Task ExecuteWithProductReservationLocksAsync(
        PosOrganizationId organizationId,
        IReadOnlyCollection<CatalogProductId> productIds,
        Func<IReadOnlyList<InventoryAccount>, CancellationToken, Task> action,
        CancellationToken cancellationToken = default);

    Task AddMovementAsync(StockMovement movement, CancellationToken cancellationToken = default);

    Task<StockMovement?> GetMovementByIdAsync(
        PosOrganizationId organizationId,
        StockMovementId movementId,
        CancellationToken cancellationToken = default);

    Task<bool> HasAnyMovementAsync(
        PosOrganizationId organizationId,
        CatalogProductId productId,
        CancellationToken cancellationToken = default);

    Task<bool> HasOpeningStockAsync(
        PosOrganizationId organizationId,
        CatalogProductId productId,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<StockMovement> Items, int TotalCount)> ListMovementsAsync(
        PosOrganizationId organizationId,
        CatalogProductId productId,
        StockMovementFilter filter,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task<decimal> SumMovementEffectsAsync(
        PosOrganizationId organizationId,
        CatalogProductId productId,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<InventoryAccount> Items, int TotalCount)> ListReorderSuggestionsAsync(
        PosOrganizationId organizationId,
        string? search,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task<bool> HasStockCountVarianceAsync(
        PosOrganizationId organizationId,
        StockCountId stockCountId,
        CatalogProductId productId,
        StockMovementType movementType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Organization-wide stock movements whose UTC calendar <c>RecordedAtUtc</c> falls in the
    /// inclusive range. Callers must enforce report max span.
    /// </summary>
    Task<IReadOnlyList<StockMovement>> ListMovementsForReportAsync(
        PosOrganizationId organizationId,
        DateOnly fromDateUtc,
        DateOnly toDateUtc,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StockMovement>> ListSaleDeductionsAsync(
        PosOrganizationId organizationId,
        SaleId saleId,
        CancellationToken cancellationToken = default);

    Task<bool> HasSaleDeductionAsync(
        PosOrganizationId organizationId,
        SaleId saleId,
        CatalogProductId productId,
        CancellationToken cancellationToken = default);

    Task<bool> HasCustomerOrderDeductionAsync(
        PosOrganizationId organizationId,
        CustomerOrderId orderId,
        CatalogProductId productId,
        CancellationToken cancellationToken = default);

    Task<bool> HasSaleVoidRestorationAsync(
        PosOrganizationId organizationId,
        SaleId saleId,
        CatalogProductId productId,
        CancellationToken cancellationToken = default);

    Task<bool> HasPurchaseReceiptAsync(
        PosOrganizationId organizationId,
        GoodsReceiptId goodsReceiptId,
        CatalogProductId productId,
        CancellationToken cancellationToken = default);

    Task<bool> HasDirectPurchaseReceiptAsync(
        PosOrganizationId organizationId,
        DirectPurchaseReceiptId receiptId,
        CatalogProductId productId,
        CancellationToken cancellationToken = default);

    Task<bool> HasStockUseAsync(
        PosOrganizationId organizationId,
        StockUseId stockUseId,
        CatalogProductId productId,
        CancellationToken cancellationToken = default);

    Task<bool> HasStockUseVoidRestorationAsync(
        PosOrganizationId organizationId,
        StockUseId stockUseId,
        CatalogProductId productId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Latest non-null acquisition <see cref="StockMovement.UnitCost"/> for the product
    /// (opening / purchase receipt / direct purchase), newest first. Null when unknown.
    /// </summary>
    Task<decimal?> GetLatestAcquisitionUnitCostAsync(
        PosOrganizationId organizationId,
        CatalogProductId productId,
        CancellationToken cancellationToken = default);

    Task<bool> HasSaleReturnRestockAsync(
        PosOrganizationId organizationId,
        SaleReturnId saleReturnId,
        CatalogProductId productId,
        CancellationToken cancellationToken = default);

    Task<bool> HasInventoryTransferMovementAsync(
        PosOrganizationId organizationId,
        InventoryTransferId transferId,
        CatalogProductId productId,
        StockMovementType movementType,
        InventoryLotId? lotId = null,
        CancellationToken cancellationToken = default);

    Task<(DateTimeOffset? LatestAt, int Count)> GetMovementSummaryAsync(
        PosOrganizationId organizationId,
        CatalogProductId productId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<Guid, (DateTimeOffset? LatestAt, int Count)>> GetMovementSummariesAsync(
        PosOrganizationId organizationId,
        IReadOnlyCollection<CatalogProductId> productIds,
        CancellationToken cancellationToken = default);
}
