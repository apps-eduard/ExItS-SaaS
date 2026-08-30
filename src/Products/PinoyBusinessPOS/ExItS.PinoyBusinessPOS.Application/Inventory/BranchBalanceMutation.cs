using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;

namespace ExItS.PinoyBusinessPOS.Application.Inventory;

/// <summary>
/// Applies a signed branch-balance delta.
/// Outflows (negative) use <see cref="BranchStockResolver.EnsureBalance"/> so unallocated
/// organization stock is attributed to the primary/default branch (same as sales).
/// Inflows (positive) never invent primary attribution — missing rows start at zero then credit.
/// </summary>
public static class BranchBalanceMutation
{
    public static async Task ApplyAsync(
        IInventoryBranchBalanceRepository branchBalances,
        IOrganizationBranchDirectory? branches,
        PosOrganizationId organizationId,
        PosBranchId branchId,
        CatalogProductId productId,
        decimal organizationOnHandBeforeDelta,
        decimal signedQuantity,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(branchBalances);
        if (signedQuantity == 0m)
        {
            return;
        }

        if (signedQuantity < 0m)
        {
            Guid? primaryId = null;
            if (branches is not null)
            {
                primaryId = await branches
                    .GetPrimaryBranchIdAsync(organizationId.Value, cancellationToken)
                    .ConfigureAwait(false);
            }

            var balances = (await branchBalances
                    .ListByProductIdsAsync(organizationId, [productId], cancellationToken)
                    .ConfigureAwait(false))
                .ToList();

            var balance = BranchStockResolver.EnsureBalance(
                organizationId,
                branchId,
                productId,
                organizationOnHandBeforeDelta,
                primaryId,
                balances,
                utcNow);
            balance.Apply(signedQuantity, utcNow);
            await branchBalances.UpsertAsync(balance, cancellationToken).ConfigureAwait(false);
            return;
        }

        var existing = await branchBalances
            .GetAsync(organizationId, branchId, productId, cancellationToken)
            .ConfigureAwait(false);
        var inflow = existing
            ?? InventoryBranchBalance.Create(organizationId, branchId, productId, 0m, utcNow);
        inflow.Apply(signedQuantity, utcNow);
        await branchBalances.UpsertAsync(inflow, cancellationToken).ConfigureAwait(false);
    }
}
