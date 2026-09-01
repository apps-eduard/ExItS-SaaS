using ExItS.PinoyBusinessPOS.Application.Common;
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
        CancellationToken cancellationToken = default,
        Guid? primaryBranchId = null)
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
        if (InventoryLotCompatibility.IncludeLegacyNullLots(primaryBranchId, branchId))
        {
            var legacy = await _lots
                .ListOrgLevelOnHandAsync(organizationId, productId, includeDepleted: false, cancellationToken)
                .ConfigureAwait(false);
            lots = InventoryLotCompatibility.UnionByLotId(lots, legacy);
        }
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

    /// <summary>
    /// Allocates existing product on-hand into new expiration lots without changing
    /// <see cref="InventoryAccount.OnHandQuantity"/>. Creates lots at full line quantity and
    /// records lot-ledger <see cref="StockMovementType.ExpirationInitialization"/> only
    /// (no product-level stock movement, no <c>ApplyMovementEffect</c>).
    /// Requires no existing positive on-hand lots for the product.
    /// </summary>
    public async Task<IReadOnlyList<InventoryLot>> AllocateExistingOnHandLotsAsync(
        PosOrganizationId organizationId,
        CatalogProductId productId,
        IReadOnlyList<ExistingStockLotInput> lines,
        Guid actorId,
        DateTimeOffset utcNow,
        PosBranchId? branchId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(lines);

        var existing = await _lots
            .ListOnHandAsync(organizationId, productId, branchId, includeDepleted: false, cancellationToken)
            .ConfigureAwait(false);
        if (existing.Any(l => l.QuantityOnHand > 0m))
        {
            throw new DomainException(
                ApplicationErrorCodes.ExpirationTrackingAlreadyEnabled,
                "Cannot allocate existing on-hand into lots while positive lot quantities already exist.");
        }

        foreach (var line in lines)
        {
            if (line.Quantity <= 0m)
            {
                throw new DomainException(
                    ApplicationErrorCodes.ExpirationLotQuantityInvalid,
                    "Each existing-stock lot quantity must be greater than zero.");
            }

            if (line.ExpiryDate is null)
            {
                throw new DomainException(
                    DomainErrorCodes.InventoryExpirationRequired,
                    "Expiration date is required for each existing-stock lot.");
            }
        }

        var created = new List<InventoryLot>(lines.Count);
        foreach (var line in lines)
        {
            var lot = InventoryLot.Create(
                organizationId,
                productId,
                line.ExpiryDate!.Value,
                line.Quantity,
                utcNow,
                branchId,
                line.LotNumber);
            await _lots.AddAsync(lot, cancellationToken).ConfigureAwait(false);
            await AddMovementAsync(
                    organizationId,
                    lot.Id,
                    productId,
                    StockMovementType.ExpirationInitialization,
                    line.Quantity,
                    StockMovementSourceType.Manual,
                    actorId,
                    utcNow,
                    sourceId: null,
                    stockMovementId: null,
                    cancellationToken)
                .ConfigureAwait(false);
            created.Add(lot);
        }

        return created;
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

    /// <summary>
    /// Reverses prior positive lot receives for a source (e.g. production output) by decreasing
    /// each lot. Blocks when attributable on-hand on a lot is insufficient.
    /// </summary>
    public async Task ReverseReceiveSourceAsync(
        PosOrganizationId organizationId,
        Guid sourceId,
        StockMovementType receiveType,
        StockMovementType reverseType,
        Guid actorId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken = default)
    {
        var received = await _lots
            .ListBySourceAsync(organizationId, sourceId, receiveType, cancellationToken)
            .ConfigureAwait(false);
        foreach (var movement in received)
        {
            if (await _lots
                    .HasMovementAsync(organizationId, sourceId, movement.LotId, reverseType, cancellationToken)
                    .ConfigureAwait(false))
            {
                continue;
            }

            var lot = await _lots.GetByIdAsync(organizationId, movement.LotId, cancellationToken).ConfigureAwait(false);
            if (lot is null)
            {
                throw new DomainException(
                    DomainErrorCodes.ProductionVoidOutputInsufficient,
                    "Production output lot was not found for void reversal.");
            }

            var qty = Math.Abs(movement.QuantityEffect);
            if (lot.QuantityOnHand < qty)
            {
                throw new DomainException(
                    DomainErrorCodes.ProductionVoidOutputInsufficient,
                    "Cannot void production: attributable output stock has already been consumed.");
            }

            lot.Apply(-qty, utcNow);
            await _lots.UpdateAsync(lot, cancellationToken).ConfigureAwait(false);
            await AddMovementAsync(
                    organizationId,
                    lot.Id,
                    lot.ProductId,
                    reverseType,
                    -qty,
                    movement.SourceType,
                    actorId,
                    utcNow,
                    sourceId,
                    stockMovementId: null,
                    cancellationToken)
                .ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Partial sale-return restock onto original sale-consumed lots only (earliest expiration first).
    /// Never exceeds each lot's original consumed quantity net of prior SaleReturnRestock restores.
    /// Expired lots may receive quantity but remain expired / not sellable.
    /// </summary>
    public async Task RestoreForSaleReturnAsync(
        PosOrganizationId organizationId,
        Guid saleId,
        Guid saleReturnId,
        CatalogProductId productId,
        decimal quantityToRestore,
        IReadOnlyList<Guid> priorSaleReturnIds,
        Guid actorId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken = default)
    {
        if (quantityToRestore <= 0m)
        {
            return;
        }

        var existingForReturn = await _lots
            .ListBySourceAsync(organizationId, saleReturnId, StockMovementType.SaleReturnRestock, cancellationToken)
            .ConfigureAwait(false);
        if (existingForReturn.Any(m => m.ProductId == productId))
        {
            return;
        }

        var deductions = (await _lots
                .ListBySourceAsync(organizationId, saleId, StockMovementType.SaleDeduction, cancellationToken)
                .ConfigureAwait(false))
            .Where(m => m.ProductId == productId)
            .ToList();

        var consumedByLot = new Dictionary<Guid, decimal>();
        foreach (var movement in deductions)
        {
            var lotKey = movement.LotId.Value;
            consumedByLot.TryGetValue(lotKey, out var prior);
            consumedByLot[lotKey] = prior + Math.Abs(movement.QuantityEffect);
        }

        var restoredByLot = new Dictionary<Guid, decimal>();
        foreach (var priorReturnId in priorSaleReturnIds)
        {
            if (priorReturnId == saleReturnId || priorReturnId == Guid.Empty)
            {
                continue;
            }

            var priorRestocks = await _lots
                .ListBySourceAsync(
                    organizationId,
                    priorReturnId,
                    StockMovementType.SaleReturnRestock,
                    cancellationToken)
                .ConfigureAwait(false);
            foreach (var movement in priorRestocks.Where(m => m.ProductId == productId))
            {
                var lotKey = movement.LotId.Value;
                restoredByLot.TryGetValue(lotKey, out var prior);
                restoredByLot[lotKey] = prior + Math.Abs(movement.QuantityEffect);
            }
        }

        var remainingByLot = new Dictionary<Guid, decimal>();
        foreach (var (lotKey, consumed) in consumedByLot)
        {
            restoredByLot.TryGetValue(lotKey, out var restored);
            var remaining = consumed - restored;
            if (remaining > 0m)
            {
                remainingByLot[lotKey] = remaining;
            }
        }

        if (remainingByLot.Count == 0)
        {
            throw new DomainException(
                ApplicationErrorCodes.SaleReturnLotRestoreInsufficient,
                "No remaining original sale lots are available to restore for this return quantity.");
        }

        var lots = new List<InventoryLot>();
        foreach (var lotKey in remainingByLot.Keys)
        {
            var lot = await _lots
                .GetByIdAsync(organizationId, InventoryLotId.From(lotKey), cancellationToken)
                .ConfigureAwait(false);
            if (lot is null || lot.ProductId != productId)
            {
                throw new DomainException(
                    ApplicationErrorCodes.SaleReturnLotRestoreInsufficient,
                    "An original sale lot required for return restock is missing.");
            }

            lots.Add(lot);
        }

        var ordered = lots
            .OrderBy(l => l.ExpirationDate)
            .ThenBy(l => l.NormalizedLotNumber, StringComparer.Ordinal)
            .ThenBy(l => l.Id.Value)
            .ToList();

        var remainingToAllocate = quantityToRestore;
        foreach (var lot in ordered)
        {
            if (remainingToAllocate <= 0m)
            {
                break;
            }

            var restorable = remainingByLot[lot.Id.Value];
            var take = Math.Min(restorable, remainingToAllocate);
            if (take <= 0m)
            {
                continue;
            }

            lot.Apply(take, utcNow);
            await _lots.UpdateAsync(lot, cancellationToken).ConfigureAwait(false);
            await AddMovementAsync(
                    organizationId,
                    lot.Id,
                    productId,
                    StockMovementType.SaleReturnRestock,
                    take,
                    StockMovementSourceType.SaleReturn,
                    actorId,
                    utcNow,
                    saleReturnId,
                    stockMovementId: null,
                    cancellationToken)
                .ConfigureAwait(false);
            remainingToAllocate -= take;
        }

        if (remainingToAllocate > 0m)
        {
            throw new DomainException(
                ApplicationErrorCodes.SaleReturnLotRestoreInsufficient,
                $"Cannot restore {quantityToRestore} to original sale lots; insufficient remaining restorable quantity.");
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
