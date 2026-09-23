using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;

namespace ExItS.PinoyBusinessPOS.Application.Purchasing;

/// <summary>
/// Soft supplier-branch availability for connected PO create UX (display only).
/// Does not reserve, mutate, or block purchase-order create/update/submit.
/// Hard stock checks remain on supplier fulfillment (<see cref="ConnectedPurchaseOrderFulfillStock"/>).
/// </summary>
public static class ConnectedPurchaseOrderSupplierStock
{
    public sealed record DemandLine(
        Guid SupplierProductId,
        decimal OrderedQty,
        decimal MultiplierToBase,
        string ProductName);

    public sealed record StockSnapshot(
        bool IsTracked,
        decimal AvailableBaseQuantity,
        decimal OnHandBaseQuantity = 0m,
        decimal ReservedBaseQuantity = 0m);

    public static async Task<IReadOnlyDictionary<Guid, StockSnapshot>> LoadSnapshotsAsync(
        PosOrganizationId supplierOrganizationId,
        Guid? supplierBranchId,
        IReadOnlyCollection<Guid> supplierProductIds,
        IInventoryRepository inventory,
        IInventoryBranchBalanceRepository balances,
        IOrganizationBranchDirectory? branches,
        CancellationToken cancellationToken)
    {
        var distinct = supplierProductIds.Where(id => id != Guid.Empty).Distinct().ToList();
        if (distinct.Count == 0)
        {
            return new Dictionary<Guid, StockSnapshot>();
        }

        var productIds = distinct.Select(CatalogProductId.From).ToList();
        var accounts = await inventory
            .ListByProductIdsAsync(supplierOrganizationId, productIds, cancellationToken)
            .ConfigureAwait(false);
        var byProduct = accounts.ToDictionary(a => a.ProductId.Value);
        var balanceRows = await balances
            .ListByProductIdsAsync(supplierOrganizationId, productIds, cancellationToken)
            .ConfigureAwait(false);
        Guid? primaryId = branches is null
            ? null
            : await branches
                .GetPrimaryBranchIdAsync(supplierOrganizationId.Value, cancellationToken)
                .ConfigureAwait(false);

        var result = new Dictionary<Guid, StockSnapshot>(distinct.Count);
        foreach (var productId in distinct)
        {
            if (!byProduct.TryGetValue(productId, out var account) || !account.IsTracked)
            {
                result[productId] = new StockSnapshot(
                    IsTracked: false,
                    AvailableBaseQuantity: 0m,
                    OnHandBaseQuantity: 0m,
                    ReservedBaseQuantity: 0m);
                continue;
            }

            if (supplierBranchId is not Guid branchGuid || branchGuid == Guid.Empty)
            {
                // Fail closed for tracked stock when the relationship has no supplier branch.
                result[productId] = new StockSnapshot(
                    IsTracked: true,
                    AvailableBaseQuantity: 0m,
                    OnHandBaseQuantity: 0m,
                    ReservedBaseQuantity: 0m);
                continue;
            }

            var branchId = PosBranchId.From(branchGuid);
            var catalogProductId = CatalogProductId.From(productId);
            var onHand = BranchStockResolver.ResolveOnHand(
                branchId,
                primaryId,
                account.OnHandQuantity,
                balanceRows,
                catalogProductId);
            var reserved = BranchStockResolver.ResolveReserved(branchId, balanceRows, catalogProductId);
            var available = BranchStockResolver.ResolveAvailable(
                branchId,
                balanceRows,
                catalogProductId,
                onHand,
                reserved);
            result[productId] = new StockSnapshot(
                IsTracked: true,
                AvailableBaseQuantity: available,
                OnHandBaseQuantity: onHand,
                ReservedBaseQuantity: reserved);
        }

        return result;
    }

    /// <summary>
    /// Connected PO create/update/submit no longer rejects over-order or zero stock.
    /// Availability is informational; fulfillment enforces real inventory.
    /// Always returns null.
    /// </summary>
    public static Task<ApplicationResult?> ValidateDemandsAsync(
        ConnectedSupplierRelationship relationship,
        IReadOnlyList<DemandLine> lines,
        IInventoryRepository inventory,
        IInventoryBranchBalanceRepository balances,
        IOrganizationBranchDirectory? branches,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(relationship);
        ArgumentNullException.ThrowIfNull(lines);
        _ = inventory;
        _ = balances;
        _ = branches;
        _ = cancellationToken;
        return Task.FromResult<ApplicationResult?>(null);
    }
}
