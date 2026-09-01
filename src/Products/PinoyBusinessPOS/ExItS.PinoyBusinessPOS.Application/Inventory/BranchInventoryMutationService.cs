using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;

namespace ExItS.PinoyBusinessPOS.Application.Inventory;

/// <summary>
/// Central coordinator for physical branch overlay mutations.
/// Materializes target branch balances from PRE-MUTATION organization on-hand before applying deltas.
/// Organization aggregate updates remain on <see cref="InventoryAccount"/> in the calling use case.
/// </summary>
public sealed class BranchInventoryMutationService
{
    /// <summary>
    /// Applies a signed branch overlay delta after materializing the target balance from pre-mutation org stock.
    /// </summary>
    public Task ApplyBranchDeltaAsync(
        IInventoryBranchBalanceRepository branchBalances,
        IOrganizationBranchDirectory branches,
        PosOrganizationId organizationId,
        PosBranchId branchId,
        CatalogProductId productId,
        decimal organizationOnHandBeforeDelta,
        decimal signedBranchDelta,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken = default) =>
        BranchBalanceMutation.ApplyAsync(
            branchBalances,
            branches,
            organizationId,
            branchId,
            productId,
            organizationOnHandBeforeDelta,
            signedBranchDelta,
            utcNow,
            cancellationToken);

    /// <summary>
    /// Resolves persisted branch provenance, falling back to structural Primary for legacy null rows only.
    /// </summary>
    public static async Task<ApplicationResult<PosBranchId>> ResolvePhysicalBranchAsync(
        Guid? persistedBranchId,
        IOrganizationBranchDirectory branches,
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(branches);

        if (persistedBranchId is Guid branch && branch != Guid.Empty)
        {
            return ApplicationResult<PosBranchId>.Success(PosBranchId.From(branch));
        }

        var primary = await branches
            .GetPrimaryBranchIdAsync(organizationId, cancellationToken)
            .ConfigureAwait(false);
        if (primary is null)
        {
            return ApplicationResult<PosBranchId>.Failure(
                ApplicationErrorCodes.InventoryBranchRequired,
                "Physical branch provenance is unavailable and primary branch cannot be resolved.");
        }

        return ApplicationResult<PosBranchId>.Success(PosBranchId.From(primary.Value));
    }

    /// <summary>
    /// Branch-resolved on-hand snapshots for stock-count start (not organization aggregate).
    /// </summary>
    public static async Task<IReadOnlyDictionary<Guid, decimal>> ResolveBranchOnHandSnapshotsAsync(
        PosOrganizationId organizationId,
        PosBranchId branchId,
        Guid? primaryBranchId,
        IReadOnlyCollection<CatalogProductId> productIds,
        IInventoryRepository inventory,
        IInventoryBranchBalanceRepository branchBalances,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(inventory);
        ArgumentNullException.ThrowIfNull(branchBalances);

        var ids = productIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return new Dictionary<Guid, decimal>();
        }

        var accounts = await inventory
            .ListByProductIdsAsync(organizationId, ids, cancellationToken)
            .ConfigureAwait(false);
        var balances = await branchBalances
            .ListByProductIdsAsync(organizationId, ids, cancellationToken)
            .ConfigureAwait(false);
        var balancesByProduct = balances
            .GroupBy(b => b.ProductId.Value)
            .ToDictionary(g => g.Key, g => (IEnumerable<InventoryBranchBalance>)g.ToList());

        var result = new Dictionary<Guid, decimal>(accounts.Count);
        foreach (var account in accounts)
        {
            balancesByProduct.TryGetValue(account.ProductId.Value, out var productBalances);
            result[account.ProductId.Value] = BranchStockResolver.ResolveOnHand(
                branchId,
                primaryBranchId,
                account.OnHandQuantity,
                productBalances ?? [],
                account.ProductId);
        }

        return result;
    }
}
