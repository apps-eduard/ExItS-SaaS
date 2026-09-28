using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;

namespace ExItS.PinoyBusinessPOS.Application.Inventory;

/// <summary>
/// Expands product+qty transfer lines into FEFO source-lot lines for expiration-tracked products.
/// Shared by stock-request prepare/dispatch and prepare-remaining.
/// </summary>
internal static class InventoryTransferFefoLineAllocator
{
    public static async Task<ApplicationResult<IReadOnlyList<InventoryTransferLineRequest>>> ExpandWithCurrentFefoAsync(
        ICatalogProductRepository products,
        IInventoryLotRepository lots,
        BranchExpirationPolicyResolver expirationPolicies,
        PosOrganizationId organizationId,
        PosBranchId sourceBranchId,
        IReadOnlyList<InventoryTransferLineRequest> remainingLines,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        var today = InventoryLot.BusinessDateOf(utcNow);
        var productIds = remainingLines.Select(l => CatalogProductId.From(l.ProductId)).Distinct().ToList();
        var catalog = (await products.ListByIdsAsync(organizationId, productIds, cancellationToken).ConfigureAwait(false))
            .ToDictionary(p => p.Id.Value);
        var sourcePolicies = await expirationPolicies
            .ResolveManyAsync(organizationId, sourceBranchId, productIds, cancellationToken)
            .ConfigureAwait(false);

        var expanded = new List<InventoryTransferLineRequest>();
        foreach (var group in remainingLines.GroupBy(l => l.ProductId))
        {
            var quantity = group.Sum(l => l.Quantity);
            if (!(quantity > 0m))
            {
                continue;
            }

            if (!catalog.TryGetValue(group.Key, out var product))
            {
                return ApplicationResult<IReadOnlyList<InventoryTransferLineRequest>>.Failure(
                    ApplicationErrorCodes.InventoryProductNotFound,
                    "Product was not found.");
            }

            if (!sourcePolicies.GetValueOrDefault(group.Key).TracksExpiration)
            {
                expanded.Add(new InventoryTransferLineRequest(group.Key, quantity, SourceLotId: null));
                continue;
            }

            var onHand = await lots
                .ListOnHandAsync(organizationId, product.Id, sourceBranchId, includeDepleted: false, cancellationToken)
                .ConfigureAwait(false);
            try
            {
                // Physical transfer: stop-selling window is not applied (absolute expiry only).
                var allocations = InventoryLotFefo.AllocateSellable(
                    onHand,
                    quantity,
                    today,
                    stopSellingDaysBeforeExpiry: 0);
                foreach (var allocation in allocations)
                {
                    expanded.Add(new InventoryTransferLineRequest(
                        group.Key,
                        allocation.Quantity,
                        allocation.Lot.Id.Value));
                }
            }
            catch (DomainException ex)
            {
                return ApplicationResult<IReadOnlyList<InventoryTransferLineRequest>>.Failure(
                    ex.ErrorCode,
                    ex.Message);
            }
        }

        return ApplicationResult<IReadOnlyList<InventoryTransferLineRequest>>.Success(expanded);
    }
}
