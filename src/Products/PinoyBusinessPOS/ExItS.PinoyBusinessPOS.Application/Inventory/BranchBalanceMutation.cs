using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;

namespace ExItS.PinoyBusinessPOS.Application.Inventory;

/// <summary>
/// Applies a signed branch-balance physical delta or reservation overlay.
/// Always materializes the target balance from PRE-MUTATION organization on-hand via
/// <see cref="BranchStockResolver.EnsureBalance"/> before applying the effect.
/// Structural primary must be resolved by the caller (once per operation).
/// </summary>
public static class BranchBalanceMutation
{
    public static async Task ApplyAsync(
        IInventoryBranchBalanceRepository branchBalances,
        PosOrganizationId organizationId,
        PosBranchId branchId,
        Guid? primaryBranchId,
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

        var balances = (await branchBalances
                .ListByProductIdsAsync(organizationId, [productId], cancellationToken)
                .ConfigureAwait(false))
            .ToList();

        var balance = BranchStockResolver.EnsureBalance(
            organizationId,
            branchId,
            productId,
            organizationOnHandBeforeDelta,
            primaryBranchId,
            balances,
            utcNow);
        balance.Apply(signedQuantity, utcNow);
        await branchBalances.UpsertAsync(balance, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Compatibility overload. Prefer the primary-id overload to avoid per-line Platform lookups.
    /// </summary>
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
        Guid? primaryId = null;
        if (branches is not null)
        {
            primaryId = await branches
                .GetPrimaryBranchIdAsync(organizationId.Value, cancellationToken)
                .ConfigureAwait(false);
        }

        await ApplyAsync(
                branchBalances,
                organizationId,
                branchId,
                primaryId,
                productId,
                organizationOnHandBeforeDelta,
                signedQuantity,
                utcNow,
                cancellationToken)
            .ConfigureAwait(false);
    }

    public static async Task ApplyReservationAsync(
        IInventoryBranchBalanceRepository branchBalances,
        PosOrganizationId organizationId,
        PosBranchId branchId,
        Guid? primaryBranchId,
        CatalogProductId productId,
        decimal organizationOnHandBeforeReservation,
        decimal quantity,
        BranchReservationEffect effect,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(branchBalances);
        if (quantity == 0m)
        {
            return;
        }

        var balances = (await branchBalances
                .ListByProductIdsAsync(organizationId, [productId], cancellationToken)
                .ConfigureAwait(false))
            .ToList();

        var balance = BranchStockResolver.EnsureBalance(
            organizationId,
            branchId,
            productId,
            organizationOnHandBeforeReservation,
            primaryBranchId,
            balances,
            utcNow);

        switch (effect)
        {
            case BranchReservationEffect.Reserve:
                balance.Reserve(quantity, utcNow);
                break;
            case BranchReservationEffect.Release:
                balance.Release(quantity, utcNow);
                break;
            case BranchReservationEffect.Consume:
                balance.ConsumeReservation(quantity, utcNow);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(effect), effect, null);
        }

        await branchBalances.UpsertAsync(balance, cancellationToken).ConfigureAwait(false);
    }
}

public enum BranchReservationEffect
{
    Reserve = 0,
    Release = 1,
    Consume = 2
}
