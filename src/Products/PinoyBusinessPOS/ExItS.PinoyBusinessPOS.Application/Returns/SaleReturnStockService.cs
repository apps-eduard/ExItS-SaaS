using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Returns;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.Application.Returns;

/// <summary>
/// Return restock hook. Applied atomically inside return create for ReturnToStock tracked products only.
/// </summary>
public interface ISaleReturnStockService
{
    Task RestockForReturnAsync(
        PosOrganizationId organizationId,
        SaleReturn saleReturn,
        Sale originalSale,
        Guid actorId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken = default);
}

public sealed class SaleReturnStockService : ISaleReturnStockService
{
    private readonly IInventoryRepository _inventory;
    private readonly ICatalogProductRepository _products;
    private readonly InventoryLotStockService _lots;
    private readonly IInventoryLotRepository _lotRepository;
    private readonly ISaleReturnRepository _returns;
    private readonly IInventoryBranchBalanceRepository? _branchBalances;
    private readonly IOrganizationBranchDirectory? _branches;

    public SaleReturnStockService(
        IInventoryRepository inventory,
        ICatalogProductRepository products,
        InventoryLotStockService lots,
        IInventoryLotRepository lotRepository,
        ISaleReturnRepository returns,
        IInventoryBranchBalanceRepository? branchBalances = null,
        IOrganizationBranchDirectory? branches = null)
    {
        _inventory = inventory;
        _products = products;
        _lots = lots;
        _lotRepository = lotRepository;
        _returns = returns;
        _branchBalances = branchBalances;
        _branches = branches;
    }

    public async Task RestockForReturnAsync(
        PosOrganizationId organizationId,
        SaleReturn saleReturn,
        Sale originalSale,
        Guid actorId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken = default)
    {
        var restockLines = saleReturn.Lines
            .Where(l => l.RestockDisposition == RestockDisposition.ReturnToStock)
            .ToList();
        if (restockLines.Count == 0)
        {
            return;
        }

        // Aggregate by product so duplicate sale lines for the same SKU restock once (idempotent key).
        var restockGroups = restockLines
            .GroupBy(l => l.ProductId.Value)
            .Select(g => new RestockGroup(
                g.First().ProductId,
                g.Sum(l => l.QuantityReturned),
                g.ToList()))
            .ToList();

        var productIds = restockGroups.Select(g => g.ProductId).ToList();
        var accounts = await _inventory
            .ListByProductIdsAsync(organizationId, productIds, cancellationToken)
            .ConfigureAwait(false);
        var accountsByProduct = accounts.ToDictionary(a => a.ProductId.Value);

        var products = await _products
            .ListByIdsAsync(organizationId, productIds, cancellationToken)
            .ConfigureAwait(false);
        var productsById = products.ToDictionary(p => p.Id.Value);

        var saleLineById = originalSale.Lines.ToDictionary(l => l.Id.Value);

        var priorReturns = await _returns
            .ListBySaleIdAsync(organizationId, originalSale.Id, cancellationToken)
            .ConfigureAwait(false);
        var priorReturnIds = priorReturns
            .Where(r => r.Id != saleReturn.Id)
            .Select(r => r.Id.Value)
            .ToList();

        foreach (var group in restockGroups)
        {
            if (!accountsByProduct.TryGetValue(group.ProductId.Value, out var account) || !account.IsTracked)
            {
                continue;
            }

            if (await _inventory
                    .HasSaleReturnRestockAsync(
                        organizationId,
                        saleReturn.Id,
                        group.ProductId,
                        cancellationToken)
                    .ConfigureAwait(false))
            {
                continue;
            }

            productsById.TryGetValue(group.ProductId.Value, out var product);
            var tracksExpiration = product?.TracksExpiration == true;

            if (tracksExpiration)
            {
                await EnsureExpiryHistoryReconciledAsync(
                        organizationId,
                        group.ProductId,
                        priorReturns,
                        saleReturn.Id,
                        cancellationToken)
                    .ConfigureAwait(false);
            }

            SellingMode sellingMode = SellingMode.PerItem;
            UnitOfMeasure uom = group.Lines[0].UomSnapshot;
            var firstSaleLine = group.Lines
                .Select(l => saleLineById.TryGetValue(l.SaleLineId.Value, out var sl) ? sl : null)
                .FirstOrDefault(sl => sl is not null);
            if (firstSaleLine is not null)
            {
                sellingMode = firstSaleLine.SellingModeSnapshot;
                uom = firstSaleLine.UnitOfMeasureSnapshot;
            }
            else if (product is not null)
            {
                sellingMode = product.SellingMode;
                uom = product.UnitOfMeasure;
            }

            var movement = StockMovement.SaleReturnRestock(
                organizationId,
                group.ProductId,
                account.Id,
                group.Quantity,
                uom,
                saleReturn.Id.Value,
                actorId,
                utcNow,
                sellingMode: sellingMode);
            if (originalSale.BranchId is not null)
            {
                movement = movement.WithBranch(originalSale.BranchId.Value);
            }

            foreach (var line in group.Lines)
            {
                line.AttachInventoryMovement(movement.Id);
            }

            account.ApplyMovementEffect(movement.QuantityEffect);
            account.Touch(utcNow);
            await _inventory.UpdateAccountAsync(account, cancellationToken).ConfigureAwait(false);
            await _inventory.AddMovementAsync(movement, cancellationToken).ConfigureAwait(false);

            await ApplyBranchDeltaForReturnAsync(
                    organizationId,
                    originalSale,
                    group.ProductId,
                    account.OnHandQuantity - movement.QuantityEffect,
                    movement.QuantityEffect,
                    utcNow,
                    cancellationToken)
                .ConfigureAwait(false);

            if (tracksExpiration)
            {
                await _lots
                    .RestoreForSaleReturnAsync(
                        organizationId,
                        originalSale.Id.Value,
                        saleReturn.Id.Value,
                        group.ProductId,
                        group.Quantity,
                        priorReturnIds,
                        actorId,
                        utcNow,
                        cancellationToken)
                    .ConfigureAwait(false);
            }
        }
    }

    private async Task EnsureExpiryHistoryReconciledAsync(
        PosOrganizationId organizationId,
        CatalogProductId productId,
        IReadOnlyList<SaleReturn> priorReturns,
        SaleReturnId currentReturnId,
        CancellationToken cancellationToken)
    {
        foreach (var prior in priorReturns)
        {
            if (prior.Id == currentReturnId)
            {
                continue;
            }

            var restockedProduct = prior.Lines.Any(l =>
                l.ProductId == productId && l.RestockDisposition == RestockDisposition.ReturnToStock);
            if (!restockedProduct)
            {
                continue;
            }

            var hasAccountRestock = await _inventory
                .HasSaleReturnRestockAsync(organizationId, prior.Id, productId, cancellationToken)
                .ConfigureAwait(false);
            if (!hasAccountRestock)
            {
                continue;
            }

            var lotRestocks = await _lotRepository
                .ListBySourceAsync(
                    organizationId,
                    prior.Id.Value,
                    StockMovementType.SaleReturnRestock,
                    cancellationToken)
                .ConfigureAwait(false);
            if (!lotRestocks.Any(m => m.ProductId == productId))
            {
                throw new DomainException(
                    ApplicationErrorCodes.ExpiryReturnHistoryReconciliationGap,
                    "A prior return restocked this expiration-tracked product at account level without lot restore evidence. "
                    + "Further returns are blocked until history is reconciled.");
            }
        }
    }

    private async Task ApplyBranchDeltaForReturnAsync(
        PosOrganizationId organizationId,
        Sale originalSale,
        CatalogProductId productId,
        decimal organizationOnHandBeforeDelta,
        decimal signedQuantity,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        if (_branchBalances is null || signedQuantity == 0m)
        {
            return;
        }

        var branchId = originalSale.BranchId?.Value;
        var existingBalances = await _branchBalances
            .ListByProductIdsAsync(organizationId, [productId], cancellationToken)
            .ConfigureAwait(false);

        if (branchId is null || branchId == Guid.Empty)
        {
            if (existingBalances.Count > 0)
            {
                throw new DomainException(
                    ApplicationErrorCodes.SaleReturnBranchRequired,
                    "Sale branch is required to restock branch inventory balances for this return.");
            }

            return;
        }

        await ApplyBranchDeltaAsync(
                organizationId,
                branchId,
                productId,
                organizationOnHandBeforeDelta,
                signedQuantity,
                utcNow,
                cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task ApplyBranchDeltaAsync(
        PosOrganizationId organizationId,
        Guid? branchId,
        CatalogProductId productId,
        decimal organizationOnHandBeforeDelta,
        decimal signedQuantity,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        if (_branchBalances is null || branchId is not Guid location || location == Guid.Empty || signedQuantity == 0m)
        {
            return;
        }

        var primaryId = await ResolvePrimaryAsync(organizationId.Value, cancellationToken).ConfigureAwait(false);
        var existing = await _branchBalances
            .GetAsync(organizationId, PosBranchId.From(location), productId, cancellationToken)
            .ConfigureAwait(false);
        var balances = existing is null ? new List<InventoryBranchBalance>() : [existing];
        if (existing is null)
        {
            var related = await _branchBalances
                .ListByProductIdsAsync(organizationId, [productId], cancellationToken)
                .ConfigureAwait(false);
            balances = related.ToList();
        }

        var balance = BranchStockResolver.EnsureBalance(
            organizationId,
            PosBranchId.From(location),
            productId,
            organizationOnHandBeforeDelta,
            primaryId,
            balances,
            utcNow);
        balance.Apply(signedQuantity, utcNow);
        await _branchBalances.UpsertAsync(balance, cancellationToken).ConfigureAwait(false);
    }

    private async Task<Guid?> ResolvePrimaryAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        if (_branches is null)
        {
            return null;
        }

        return await _branches.GetPrimaryBranchIdAsync(organizationId, cancellationToken).ConfigureAwait(false);
    }

    private sealed record RestockGroup(
        CatalogProductId ProductId,
        decimal Quantity,
        IReadOnlyList<SaleReturnLine> Lines);
}
