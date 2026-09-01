using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;

namespace ExItS.PinoyBusinessPOS.Application.Inventory;

/// <summary>
/// Resolves sellable on-hand for one branch without cloning organization catalog or
/// attributing another branch's stock. Missing rows are zero except unallocated org
/// stock on the <em>known</em> primary branch until transferred or received.
/// When <paramref name="primaryBranchId"/> is unknown, fail closed (0) — never treat
/// an arbitrary branch as primary.
/// </summary>
public static class BranchStockResolver
{
    public static decimal ResolveOnHand(
        PosBranchId targetBranchId,
        Guid? primaryBranchId,
        decimal organizationOnHand,
        IEnumerable<InventoryBranchBalance> balances,
        CatalogProductId productId)
    {
        ArgumentNullException.ThrowIfNull(balances);
        var forProduct = balances.Where(b => b.ProductId == productId).ToList();
        var explicitBalance = forProduct.FirstOrDefault(b => b.BranchId == targetBranchId);
        if (explicitBalance is not null)
        {
            return explicitBalance.OnHandQuantity;
        }

        var other = forProduct
            .Where(b => b.BranchId != targetBranchId)
            .Sum(b => b.OnHandQuantity);
        var unallocated = Math.Max(0m, organizationOnHand - other);
        if (primaryBranchId is null)
        {
            return 0m;
        }

        if (primaryBranchId.Value == targetBranchId.Value)
        {
            return unallocated;
        }

        return 0m;
    }

    public static decimal ResolveAvailable(
        decimal branchOnHand,
        decimal organizationAvailable) =>
        Math.Min(Math.Max(0m, branchOnHand), Math.Max(0m, organizationAvailable));

    public static InventoryBranchBalance EnsureBalance(
        PosOrganizationId organizationId,
        PosBranchId branchId,
        CatalogProductId productId,
        decimal organizationOnHand,
        Guid? primaryBranchId,
        List<InventoryBranchBalance> balances,
        DateTimeOffset utcNow)
    {
        var existing = balances.FirstOrDefault(b => b.BranchId == branchId && b.ProductId == productId);
        if (existing is not null)
        {
            return existing;
        }

        var seed = ResolveOnHand(branchId, primaryBranchId, organizationOnHand, balances, productId);
        var created = InventoryBranchBalance.Create(organizationId, branchId, productId, seed, utcNow);
        balances.Add(created);
        return created;
    }
}
