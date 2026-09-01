using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;

namespace ExItS.PinoyBusinessPOS.Application.Inventory;

/// <summary>
/// Applies a signed branch-balance delta.
/// Always materializes the target balance from PRE-MUTATION organization on-hand via
/// <see cref="BranchStockResolver.EnsureBalance"/> before applying the delta.
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
    }
}
