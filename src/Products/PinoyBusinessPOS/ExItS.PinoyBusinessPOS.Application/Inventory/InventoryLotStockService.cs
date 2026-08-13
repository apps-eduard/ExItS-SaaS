using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;

namespace ExItS.PinoyBusinessPOS.Application.Inventory;

/// <summary>
/// Lot receive/consume helpers. Product-level <see cref="InventoryAccount"/> remains the on-hand total.
/// </summary>
public sealed class InventoryLotStockService
{
    private readonly IInventoryLotRepository _lots;

    public InventoryLotStockService(IInventoryLotRepository lots) => _lots = lots;

    public async Task<InventoryLot> ReceiveAsync(
        PosOrganizationId organizationId,
        CatalogProductId productId,
        DateOnly expirationDate,
        decimal quantity,
        Guid actorId,
        DateTimeOffset utcNow,
        StockMovementType movementType,
        StockMovementSourceType sourceType,
        PosBranchId? branchId = null,
        string? lotNumber = null,
        Guid? sourceId = null,
        Guid? stockMovementId = null,
        CancellationToken cancellationToken = default)
    {
        var (_, normalized) = InventoryLot.NormalizeLotNumber(lotNumber);
        var existing = await _lots
            .FindAsync(organizationId, productId, expirationDate, normalized, branchId, cancellationToken)
            .ConfigureAwait(false);
        if (existing is not null
            && sourceId is Guid existingSource
            && await _lots
                .HasMovementAsync(organizationId, existingSource, existing.Id, movementType, cancellationToken)
                .ConfigureAwait(false))
        {
            return existing;
        }

        if (existing is null)
        {
            existing = InventoryLot.Create(
                organizationId,
                productId,
                expirationDate,
                0m,
                utcNow,
                branchId,
                lotNumber);
            existing.Apply(quantity, utcNow);
            await _lots.AddAsync(existing, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            existing.Apply(quantity, utcNow);
            await _lots.UpdateAsync(existing, cancellationToken).ConfigureAwait(false);
        }

        await AddMovementAsync(
                organizationId,
                existing.Id,
                productId,
                movementType,
                quantity,
                sourceType,
                actorId,
                utcNow,
                sourceId,
                stockMovementId,
                cancellationToken)
            .ConfigureAwait(false);
        return existing;
    }

    public async Task ConsumeSpecificAsync(
        PosOrganizationId organizationId,
        InventoryLot lot,
        decimal quantity,
        Guid actorId,
        DateTimeOffset utcNow,
        StockMovementType movementType,
        StockMovementSourceType sourceType,
        Guid? sourceId = null,
        Guid? stockMovementId = null,
        CancellationToken cancellationToken = default)
    {
        if (sourceId is Guid existingSource
            && await _lots
                .HasMovementAsync(organizationId, existingSource, lot.Id, movementType, cancellationToken)
                .ConfigureAwait(false))
        {
            return;
        }

        lot.Apply(-quantity, utcNow);
        await _lots.UpdateAsync(lot, cancellationToken).ConfigureAwait(false);
        await AddMovementAsync(
                organizationId,
                lot.Id,
                lot.ProductId,
                movementType,
                -quantity,
                sourceType,
                actorId,
                utcNow,
                sourceId,
                stockMovementId,
                cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<InventoryLotAllocation>> ConsumeFefoAsync(
        PosOrganizationId organizationId,
        CatalogProductId productId,
        decimal quantity,
        DateOnly today,
        Guid actorId,
        DateTimeOffset utcNow,
        StockMovementType movementType,
        StockMovementSourceType sourceType,
        PosBranchId? branchId = null,
        Guid? sourceId = null,
        Guid? stockMovementId = null,
        CancellationToken cancellationToken = default)
    {
        if (sourceId is Guid existingSource)
        {
            var already = await _lots
                .ListBySourceAsync(organizationId, existingSource, movementType, cancellationToken)
                .ConfigureAwait(false);
            if (already.Count > 0)
            {
                return [];
            }
        }

        var lots = await _lots
            .ListOnHandAsync(organizationId, productId, branchId, includeDepleted: false, cancellationToken)
            .ConfigureAwait(false);
        var allocations = InventoryLotFefo.AllocateSellable(lots, quantity, today);
        foreach (var allocation in allocations)
        {
            await ConsumeSpecificAsync(
                    organizationId,
                    allocation.Lot,
                    allocation.Quantity,
                    actorId,
                    utcNow,
                    movementType,
                    sourceType,
                    sourceId,
                    stockMovementId,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        return allocations;
    }

    public async Task RestoreSourceAsync(
        PosOrganizationId organizationId,
        Guid sourceId,
        StockMovementType deductedType,
        StockMovementType restoreType,
        Guid actorId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken = default)
    {
        var deducted = await _lots
            .ListBySourceAsync(organizationId, sourceId, deductedType, cancellationToken)
            .ConfigureAwait(false);
        foreach (var movement in deducted)
        {
            if (await _lots
                    .HasMovementAsync(organizationId, sourceId, movement.LotId, restoreType, cancellationToken)
                    .ConfigureAwait(false))
            {
                continue;
            }

            var lot = await _lots.GetByIdAsync(organizationId, movement.LotId, cancellationToken).ConfigureAwait(false);
            if (lot is null)
            {
                continue;
            }

            var qty = Math.Abs(movement.QuantityEffect);
            lot.Apply(qty, utcNow);
            await _lots.UpdateAsync(lot, cancellationToken).ConfigureAwait(false);
            await AddMovementAsync(
                    organizationId,
                    lot.Id,
                    lot.ProductId,
                    restoreType,
                    qty,
                    movement.SourceType,
                    actorId,
                    utcNow,
                    sourceId,
                    stockMovementId: null,
                    cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private async Task AddMovementAsync(
        PosOrganizationId organizationId,
        InventoryLotId lotId,
        CatalogProductId productId,
        StockMovementType movementType,
        decimal quantityEffect,
        StockMovementSourceType sourceType,
        Guid actorId,
        DateTimeOffset utcNow,
        Guid? sourceId,
        Guid? stockMovementId,
        CancellationToken cancellationToken)
    {
        if (sourceId is Guid id
            && await _lots.HasMovementAsync(organizationId, id, lotId, movementType, cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        var movement = InventoryLotMovement.Create(
            organizationId,
            lotId,
            productId,
            movementType,
            quantityEffect,
            sourceType,
            actorId,
            utcNow,
            sourceId,
            stockMovementId);
        await _lots.AddMovementAsync(movement, cancellationToken).ConfigureAwait(false);
    }
}
