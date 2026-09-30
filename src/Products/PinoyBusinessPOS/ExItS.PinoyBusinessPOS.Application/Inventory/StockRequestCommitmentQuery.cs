using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;

namespace ExItS.PinoyBusinessPOS.Application.Inventory;

/// <summary>
/// Derived warehouse stock-request commitment:
/// SUM(<see cref="StockRequestDispatchCoverage"/> RemainingToDispatch) for open requests
/// at a source warehouse × product. Not a ledger <c>ReservedQuantity</c>.
/// </summary>
public sealed class StockRequestCommitmentQuery
{
    /// <summary>
    /// Statuses that still hold RemainingToDispatch commitment at the source warehouse.
    /// </summary>
    public static readonly IReadOnlyList<StockRequestStatus> OpenCommittingStatuses =
    [
        StockRequestStatus.Approved,
        StockRequestStatus.Preparing,
        StockRequestStatus.InProgress,
        StockRequestStatus.InTransit,
        StockRequestStatus.PartiallyFulfilled,
    ];

    private readonly IStockRequestRepository _requests;
    private readonly IInventoryTransferRepository _transfers;

    public StockRequestCommitmentQuery(
        IStockRequestRepository requests,
        IInventoryTransferRepository transfers)
    {
        _requests = requests;
        _transfers = transfers;
    }

    /// <summary>
    /// Batch SUM of RemainingToDispatch by product for open committing stock requests
    /// sourced from <paramref name="sourceLocationId"/>.
    /// </summary>
    /// <param name="excludeStockRequestId">
    /// When approving, exclude the request being approved so its lines are not
    /// double-counted against other open commitments.
    /// </param>
    public async Task<IReadOnlyDictionary<Guid, decimal>> SumRemainingToDispatchByProductAsync(
        PosOrganizationId organizationId,
        PosBranchId sourceLocationId,
        IReadOnlyCollection<CatalogProductId> productIds,
        Guid? excludeStockRequestId = null,
        CancellationToken cancellationToken = default)
    {
        if (productIds.Count == 0)
        {
            return new Dictionary<Guid, decimal>();
        }

        var productFilter = productIds.Select(p => p.Value).ToHashSet();
        var openRequests = await _requests
            .ListOpenCommittingBySourceAndProductIdsAsync(
                organizationId,
                sourceLocationId,
                productIds,
                cancellationToken)
            .ConfigureAwait(false);

        if (excludeStockRequestId is Guid excludeId)
        {
            openRequests = openRequests
                .Where(r => r.Id.Value != excludeId)
                .ToList();
        }

        if (openRequests.Count == 0)
        {
            return productFilter.ToDictionary(id => id, _ => 0m);
        }

        var requestIds = openRequests.Select(r => r.Id).ToList();
        var linkedTransfers = await _transfers
            .ListByStockRequestIdsAsync(organizationId, requestIds, cancellationToken)
            .ConfigureAwait(false);
        var transfersByRequest = linkedTransfers
            .Where(t => t.StockRequestId is not null)
            .GroupBy(t => t.StockRequestId!.Value)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<InventoryTransfer>)g.ToList());

        var committed = productFilter.ToDictionary(id => id, _ => 0m);
        foreach (var request in openRequests)
        {
            transfersByRequest.TryGetValue(request.Id, out var linked);
            linked ??= [];
            var coverage = StockRequestDispatchCoverage.Compute(request, linked);
            foreach (var (productId, remaining) in coverage.RemainingToDispatchByProduct)
            {
                if (!productFilter.Contains(productId) || remaining <= 0m)
                {
                    continue;
                }

                committed[productId] = committed.GetValueOrDefault(productId) + remaining;
            }
        }

        return committed;
    }
}
