using System.Globalization;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;

namespace ExItS.PinoyBusinessPOS.Application.Purchasing;

/// <summary>
/// Soft supplier-branch availability for connected PO create/update/submit.
/// Does not reserve or mutate inventory. Untracked products are never blocked.
/// </summary>
public static class ConnectedPurchaseOrderSupplierStock
{
    public sealed record DemandLine(
        Guid SupplierProductId,
        decimal OrderedQty,
        decimal MultiplierToBase,
        string ProductName);

    public sealed record StockSnapshot(bool IsTracked, decimal AvailableBaseQuantity);

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
                result[productId] = new StockSnapshot(IsTracked: false, AvailableBaseQuantity: 0m);
                continue;
            }

            if (supplierBranchId is not Guid branchGuid || branchGuid == Guid.Empty)
            {
                // Fail closed for tracked stock when the relationship has no supplier branch.
                result[productId] = new StockSnapshot(IsTracked: true, AvailableBaseQuantity: 0m);
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
            var available = BranchStockResolver.ResolveAvailable(onHand, reserved);
            result[productId] = new StockSnapshot(IsTracked: true, AvailableBaseQuantity: available);
        }

        return result;
    }

    /// <summary>
    /// Returns null when all tracked demands fit available supplier-branch stock.
    /// </summary>
    public static async Task<ApplicationResult?> ValidateDemandsAsync(
        ConnectedSupplierRelationship relationship,
        IReadOnlyList<DemandLine> lines,
        IInventoryRepository inventory,
        IInventoryBranchBalanceRepository balances,
        IOrganizationBranchDirectory? branches,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(relationship);
        ArgumentNullException.ThrowIfNull(lines);

        var positive = lines.Where(l => l.OrderedQty > 0m && l.SupplierProductId != Guid.Empty).ToList();
        if (positive.Count == 0)
        {
            return null;
        }

        var snapshots = await LoadSnapshotsAsync(
                relationship.SupplierOrganizationId,
                relationship.SupplierBranchId,
                positive.Select(l => l.SupplierProductId).ToList(),
                inventory,
                balances,
                branches,
                cancellationToken)
            .ConfigureAwait(false);

        foreach (var group in positive.GroupBy(l => l.SupplierProductId))
        {
            if (!snapshots.TryGetValue(group.Key, out var snapshot) || !snapshot.IsTracked)
            {
                continue;
            }

            var multiplier = group.First().MultiplierToBase > 0m ? group.First().MultiplierToBase : 1m;
            var neededBase = group.Sum(l => ProductUnitConversion.ToBaseQuantity(l.OrderedQty, l.MultiplierToBase > 0m ? l.MultiplierToBase : 1m));
            var name = group.First().ProductName.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                name = "Product";
            }

            var availableBase = snapshot.AvailableBaseQuantity;
            var availablePurchase = availableBase / multiplier;
            var requestedPurchase = neededBase / multiplier;

            if (availableBase <= 0m)
            {
                return ApplicationResult.Failure(
                    ConnectedSupplierErrorCodes.OutOfStockSupplierProduct,
                    $"{name} is out of stock.",
                    BuildDetails(group.Key, name, requestedPurchase, availablePurchase, neededBase, availableBase));
            }

            if (neededBase > availableBase)
            {
                return ApplicationResult.Failure(
                    ConnectedSupplierErrorCodes.InsufficientSupplierStock,
                    $"{name} has only {FormatQty(availablePurchase)} available; {FormatQty(requestedPurchase)} was requested.",
                    BuildDetails(group.Key, name, requestedPurchase, availablePurchase, neededBase, availableBase));
            }
        }

        return null;
    }

    private static Dictionary<string, string> BuildDetails(
        Guid supplierProductId,
        string productName,
        decimal requestedPurchase,
        decimal availablePurchase,
        decimal requestedBase,
        decimal availableBase) =>
        new()
        {
            ["supplierProductId"] = supplierProductId.ToString("D"),
            ["productName"] = productName,
            ["requestedQuantity"] = FormatQty(requestedPurchase),
            ["availableQuantity"] = FormatQty(availablePurchase),
            ["requestedBaseQuantity"] = FormatQty(requestedBase),
            ["availableBaseQuantity"] = FormatQty(availableBase),
        };

    private static string FormatQty(decimal value) =>
        value == decimal.Truncate(value)
            ? decimal.Truncate(value).ToString(CultureInfo.InvariantCulture)
            : value.ToString("0.####", CultureInfo.InvariantCulture);
}
