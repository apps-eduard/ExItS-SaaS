using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.CustomerOrdering;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Purchasing;
using ExItS.PinoyBusinessPOS.Domain.Returns;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.UnitTests.TestDoubles;

/// <summary>Minimal inventory fake for cost-resolver and utang ledger tests.</summary>
internal class CostResolverInventoryStub : IInventoryRepository
{
    public Dictionary<Guid, decimal> Costs { get; init; } = [];

    public Task<decimal?> GetLatestAcquisitionUnitCostAsync(
        PosOrganizationId organizationId,
        CatalogProductId productId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Costs.TryGetValue(productId.Value, out var cost) ? (decimal?)cost : null);

    public virtual async Task<IReadOnlyDictionary<Guid, decimal?>> GetLatestAcquisitionUnitCostsAsync(
        PosOrganizationId organizationId,
        IReadOnlyCollection<CatalogProductId> productIds,
        CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<Guid, decimal?>();
        foreach (var productId in productIds)
        {
            var cost = await GetLatestAcquisitionUnitCostAsync(organizationId, productId, cancellationToken)
                .ConfigureAwait(false);
            if (cost is not null)
            {
                result[productId.Value] = cost;
            }
        }

        return result;
    }

    public virtual Task<InventoryAccount?> GetByProductIdAsync(
        PosOrganizationId organizationId,
        CatalogProductId productId,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public virtual Task<IReadOnlyList<InventoryAccount>> ListByProductIdsAsync(
        PosOrganizationId organizationId,
        IReadOnlyCollection<CatalogProductId> productIds,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<InventoryAccount>>([]);

    public Task<(IReadOnlyList<InventoryAccount> Items, int TotalCount)> ListAsync(
        PosOrganizationId organizationId,
        InventoryAccountFilter filter,
        int skip,
        int take,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<(IReadOnlyList<InventoryAccount> Items, int TotalCount)> ListLowStockAsync(
        PosOrganizationId organizationId,
        string? search,
        int skip,
        int take,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<IReadOnlyList<InventoryAccount>> ListAllAccountsAsync(
        PosOrganizationId organizationId,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task AddAccountAsync(InventoryAccount account, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public virtual Task UpdateAccountAsync(InventoryAccount account, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task ExecuteWithProductReservationLocksAsync(
        PosOrganizationId organizationId,
        IReadOnlyCollection<CatalogProductId> productIds,
        Func<IReadOnlyList<InventoryAccount>, CancellationToken, Task> action,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task AddMovementAsync(StockMovement movement, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<StockMovement?> GetMovementByIdAsync(
        PosOrganizationId organizationId,
        StockMovementId movementId,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<bool> HasAnyMovementAsync(
        PosOrganizationId organizationId,
        CatalogProductId productId,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<bool> HasOpeningStockAsync(
        PosOrganizationId organizationId,
        CatalogProductId productId,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<(IReadOnlyList<StockMovement> Items, int TotalCount)> ListMovementsAsync(
        PosOrganizationId organizationId,
        CatalogProductId productId,
        StockMovementFilter filter,
        int skip,
        int take,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<decimal> SumMovementEffectsAsync(
        PosOrganizationId organizationId,
        CatalogProductId productId,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<(IReadOnlyList<InventoryAccount> Items, int TotalCount)> ListReorderSuggestionsAsync(
        PosOrganizationId organizationId,
        string? search,
        int skip,
        int take,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<bool> HasStockCountVarianceAsync(
        PosOrganizationId organizationId,
        StockCountId stockCountId,
        CatalogProductId productId,
        StockMovementType movementType,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<IReadOnlyList<StockMovement>> ListMovementsForReportAsync(
        PosOrganizationId organizationId,
        DateOnly fromDateUtc,
        DateOnly toDateUtc,
        Guid? branchId = null,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<IReadOnlyList<StockMovement>> ListSaleDeductionsAsync(
        PosOrganizationId organizationId,
        SaleId saleId,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<bool> HasSaleDeductionAsync(
        PosOrganizationId organizationId,
        SaleId saleId,
        CatalogProductId productId,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<bool> HasCustomerOrderDeductionAsync(
        PosOrganizationId organizationId,
        CustomerOrderId orderId,
        CatalogProductId productId,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<bool> HasSaleVoidRestorationAsync(
        PosOrganizationId organizationId,
        SaleId saleId,
        CatalogProductId productId,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<bool> HasPurchaseReceiptAsync(
        PosOrganizationId organizationId,
        GoodsReceiptId goodsReceiptId,
        CatalogProductId productId,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<bool> HasDirectPurchaseReceiptAsync(
        PosOrganizationId organizationId,
        DirectPurchaseReceiptId receiptId,
        CatalogProductId productId,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<bool> HasStockUseAsync(
        PosOrganizationId organizationId,
        StockUseId stockUseId,
        CatalogProductId productId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(false);

    public Task<bool> HasStockUseVoidRestorationAsync(
        PosOrganizationId organizationId,
        StockUseId stockUseId,
        CatalogProductId productId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(false);

    public Task<bool> HasProductionMaterialConsumptionAsync(
        PosOrganizationId organizationId,
        ProductionRunId productionRunId,
        CatalogProductId productId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(false);

    public Task<bool> HasProductionMaterialRestorationAsync(
        PosOrganizationId organizationId,
        ProductionRunId productionRunId,
        CatalogProductId productId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(false);

    public Task<bool> HasProductionOutputAsync(
        PosOrganizationId organizationId,
        ProductionRunId productionRunId,
        CatalogProductId productId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(false);

    public Task<bool> HasProductionOutputReversalAsync(
        PosOrganizationId organizationId,
        ProductionRunId productionRunId,
        CatalogProductId productId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(false);

    public Task<bool> HasWasteLossAsync(
        PosOrganizationId organizationId,
        WasteLossId wasteLossId,
        CatalogProductId productId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(false);

    public Task<bool> HasWasteLossVoidRestorationAsync(
        PosOrganizationId organizationId,
        WasteLossId wasteLossId,
        CatalogProductId productId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(false);

    public Task<bool> HasPurchaseReceiptReversalAsync(
        PosOrganizationId organizationId,
        GoodsReceiptId goodsReceiptId,
        CatalogProductId productId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(false);

    public Task<bool> HasDirectPurchaseReceiptReversalAsync(
        PosOrganizationId organizationId,
        DirectPurchaseReceiptId receiptId,
        CatalogProductId productId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(false);

    public Task<bool> HasSaleReturnRestockAsync(
        PosOrganizationId organizationId,
        SaleReturnId saleReturnId,
        CatalogProductId productId,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<bool> HasInventoryTransferMovementAsync(
        PosOrganizationId organizationId,
        InventoryTransferId transferId,
        CatalogProductId productId,
        StockMovementType movementType,
        InventoryLotId? lotId = null,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<(DateTimeOffset? LatestAt, int Count)> GetMovementSummaryAsync(
        PosOrganizationId organizationId,
        CatalogProductId productId,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<IReadOnlyDictionary<Guid, (DateTimeOffset? LatestAt, int Count)>> GetMovementSummariesAsync(
        PosOrganizationId organizationId,
        IReadOnlyCollection<CatalogProductId> productIds,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
}