using System.Globalization;
using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Purchasing;

namespace ExItS.PinoyBusinessPOS.Application.Purchasing;

/// <summary>
/// Supplier connected-PO inventory holds. Reservations reduce AvailableToPromise only;
/// on-hand decreases solely via <see cref="InventoryAccount.ConsumeReservation"/> on fulfill.
/// </summary>
public sealed class ConnectedPoInventoryReservationService
{
    private readonly IInventoryRepository _inventory;
    private readonly IInventoryBranchBalanceRepository _branchBalances;
    private readonly ICatalogProductRepository _products;
    private readonly ICatalogProductUnitRepository _units;
    private readonly IConnectedPoInventoryReservationRepository _reservations;
    private readonly IOrganizationBranchDirectory? _branches;

    public ConnectedPoInventoryReservationService(
        IInventoryRepository inventory,
        IInventoryBranchBalanceRepository branchBalances,
        ICatalogProductRepository products,
        ICatalogProductUnitRepository units,
        IConnectedPoInventoryReservationRepository reservations,
        IOrganizationBranchDirectory? branches = null)
    {
        _inventory = inventory;
        _branchBalances = branchBalances;
        _products = products;
        _units = units;
        _reservations = reservations;
        _branches = branches;
    }

    public async Task ReserveConfirmedOnAcceptAsync(
        ConnectedPurchaseOrder order,
        ConnectedSupplierRelationship relationship,
        Guid actorId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(order);
        ArgumentNullException.ThrowIfNull(relationship);
        _ = actorId;

        if (order.InventoryReservationState == ConnectedPoInventoryReservationState.Confirmed)
        {
            return;
        }

        await ExpireIfNeededAsync(order, utcNow, cancellationToken).ConfigureAwait(false);

        var demands = BuildDemands(
            order,
            line => line.FulfillmentQty > 0m ? line.FulfillmentQty : 0m);
        if (demands.Count == 0)
        {
            order.MarkInventoryConfirmed(utcNow);
            return;
        }

        await ReserveAsync(
                order,
                relationship,
                demands,
                temporary: false,
                utcNow,
                cancellationToken)
            .ConfigureAwait(false);
        order.MarkInventoryConfirmed(utcNow);
    }

    /// <summary>
    /// Re-reserves buyer outstanding qty for a remaining fulfillment wave after partial receipt.
    /// </summary>
    public async Task ReserveOutstandingRemainingAsync(
        ConnectedPurchaseOrder order,
        ConnectedSupplierRelationship relationship,
        PurchaseOrder buyerPo,
        Guid actorId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken = default,
        IBuyerSupplierProductLinkRepository? links = null)
    {
        ArgumentNullException.ThrowIfNull(order);
        ArgumentNullException.ThrowIfNull(relationship);
        ArgumentNullException.ThrowIfNull(buyerPo);
        _ = actorId;

        await ExpireIfNeededAsync(order, utcNow, cancellationToken).ConfigureAwait(false);

        if (order.InventoryReservationState is ConnectedPoInventoryReservationState.TemporaryProposal
            or ConnectedPoInventoryReservationState.Confirmed)
        {
            await ReleaseActiveAsync(order, utcNow, cancellationToken, markOrderReleased: false)
                .ConfigureAwait(false);
        }

        var outstandingBySupplier = await ConnectedPurchaseOrderFulfillStock
            .ResolveOutstandingShipQuantitiesAsync(order, buyerPo, links, cancellationToken)
            .ConfigureAwait(false);

        var demands = BuildDemands(
            order,
            line => outstandingBySupplier.TryGetValue(line.ProductId.Value, out var q) ? q : 0m);
        if (demands.Count == 0)
        {
            order.MarkInventoryReleased(utcNow);
            return;
        }

        await ReserveAsync(
                order,
                relationship,
                demands,
                temporary: false,
                utcNow,
                cancellationToken)
            .ConfigureAwait(false);
        order.MarkInventoryConfirmed(utcNow);
    }

    public async Task ReserveTemporaryOnProposeAsync(
        ConnectedPurchaseOrder order,
        ConnectedSupplierRelationship relationship,
        Guid actorId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(order);
        ArgumentNullException.ThrowIfNull(relationship);
        _ = actorId;

        await ExpireIfNeededAsync(order, utcNow, cancellationToken).ConfigureAwait(false);

        if (order.InventoryReservationState is ConnectedPoInventoryReservationState.TemporaryProposal
            or ConnectedPoInventoryReservationState.Confirmed)
        {
            await ReleaseActiveAsync(order, utcNow, cancellationToken, markOrderReleased: false)
                .ConfigureAwait(false);
        }

        var demands = BuildDemands(
            order,
            line =>
                line.Availability != ConnectedPoLineAvailability.Unavailable
                && line.EffectiveProposedQty > 0m
                    ? line.EffectiveProposedQty
                    : 0m);
        if (demands.Count == 0)
        {
            order.MarkInventoryReleased(utcNow);
            return;
        }

        var expires = utcNow.Add(ConnectedPoInventoryReservationOptions.DefaultProposalHoldDuration);
        await ReserveAsync(
                order,
                relationship,
                demands,
                temporary: true,
                utcNow,
                cancellationToken,
                expires)
            .ConfigureAwait(false);
        order.MarkInventoryTemporaryHold(expires, utcNow);
    }

    public async Task ConfirmTemporaryOnBuyerAcceptAsync(
        ConnectedPurchaseOrder order,
        Guid actorId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(order);
        _ = actorId;

        await ExpireIfNeededAsync(order, utcNow, cancellationToken).ConfigureAwait(false);

        if (order.InventoryReservationState == ConnectedPoInventoryReservationState.Confirmed)
        {
            return;
        }

        if (order.InventoryReservationState != ConnectedPoInventoryReservationState.TemporaryProposal)
        {
            // Buyer accepted without a prior temporary hold (e.g. only payment change) — nothing to convert.
            order.MarkInventoryConfirmed(utcNow);
            return;
        }

        var active = await _reservations
            .ListActiveByOrderAsync(order.Id, cancellationToken)
            .ConfigureAwait(false);
        foreach (var row in active.Where(r => r.Type == ConnectedPoReservationType.TemporaryProposal))
        {
            row.ConfirmFromTemporary(utcNow);
            await _reservations.UpdateAsync(row, cancellationToken).ConfigureAwait(false);
        }

        order.MarkInventoryConfirmed(utcNow);
    }

    public async Task ReleaseActiveAsync(
        ConnectedPurchaseOrder order,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(order);
        await ReleaseActiveAsync(order, utcNow, cancellationToken, markOrderReleased: true)
            .ConfigureAwait(false);
    }

    public async Task ExpireIfNeededAsync(
        ConnectedPurchaseOrder order,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(order);
        if (utcNow.Offset != TimeSpan.Zero)
        {
            throw new DomainException(DomainErrorCodes.InvalidUtcTimestamp, "Timestamp must be UTC.");
        }

        if (order.InventoryReservationState != ConnectedPoInventoryReservationState.TemporaryProposal)
        {
            return;
        }

        var expires = order.InventoryReservationExpiresAtUtc;
        if (expires is null || utcNow < expires.Value)
        {
            return;
        }

        await ReleaseInventoryHoldsAsync(order, utcNow, cancellationToken, expireLedger: true)
            .ConfigureAwait(false);

        if (order.Status == ConnectedPurchaseOrderStatus.ChangesProposed)
        {
            order.RejectProposedChanges(utcNow);
        }

        order.MarkInventoryReleased(utcNow);
    }

    /// <summary>
    /// Consumes remaining confirmed reservation quantity for a product (ledger + inventory),
    /// returning false when no active confirmed hold exists for that product.
    /// </summary>
    public async Task<bool> TryConsumeConfirmedHoldAsync(
        ConnectedPurchaseOrder order,
        CatalogProductId productId,
        decimal baseQuantity,
        InventoryAccount account,
        PosBranchId branchId,
        Guid? primaryBranchId,
        List<InventoryBranchBalance> balances,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(order);
        ArgumentNullException.ThrowIfNull(account);
        if (baseQuantity <= 0m)
        {
            return false;
        }

        if (order.InventoryReservationState != ConnectedPoInventoryReservationState.Confirmed)
        {
            return false;
        }

        var active = await _reservations
            .ListActiveByOrderAsync(order.Id, cancellationToken)
            .ConfigureAwait(false);
        var row = active.FirstOrDefault(r =>
            r.ProductId == productId
            && r.Type == ConnectedPoReservationType.ConfirmedOrder
            && r.Status == ConnectedPoReservationStatus.Active
            && r.RemainingQuantity > 0m);
        if (row is null)
        {
            return false;
        }

        var consumeQty = baseQuantity <= row.RemainingQuantity ? baseQuantity : row.RemainingQuantity;
        row.Consume(consumeQty);
        await _reservations.UpdateAsync(row, cancellationToken).ConfigureAwait(false);

        account.ConsumeReservation(consumeQty);
        account.Touch(utcNow);
        await ApplyBranchReservationAsync(
                order.SupplierOrganizationId,
                branchId,
                productId,
                account.OnHandQuantity + consumeQty,
                primaryBranchId,
                balances,
                consumeQty,
                BranchReservationEffect.Consume,
                utcNow,
                cancellationToken)
            .ConfigureAwait(false);

        return true;
    }

    private async Task ReleaseActiveAsync(
        ConnectedPurchaseOrder order,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken,
        bool markOrderReleased)
    {
        if (order.InventoryReservationState is ConnectedPoInventoryReservationState.None
            or ConnectedPoInventoryReservationState.Released
            or ConnectedPoInventoryReservationState.Consumed)
        {
            return;
        }

        await ReleaseInventoryHoldsAsync(order, utcNow, cancellationToken, expireLedger: false)
            .ConfigureAwait(false);
        if (markOrderReleased)
        {
            order.MarkInventoryReleased(utcNow);
        }
    }

    private async Task ReleaseInventoryHoldsAsync(
        ConnectedPurchaseOrder order,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken,
        bool expireLedger)
    {
        var active = await _reservations
            .ListActiveByOrderAsync(order.Id, cancellationToken)
            .ConfigureAwait(false);
        if (active.Count == 0)
        {
            return;
        }

        var productIds = active.Select(r => r.ProductId).Distinct().ToList();
        await _inventory
            .ExecuteWithProductReservationLocksAsync(
                order.SupplierOrganizationId,
                productIds,
                async (accounts, ct) =>
                {
                    var byProduct = accounts.ToDictionary(a => a.ProductId.Value);
                    var balances = (await _branchBalances
                            .ListByProductIdsAsync(order.SupplierOrganizationId, productIds, ct)
                            .ConfigureAwait(false))
                        .ToList();
                    var primaryId = await ResolvePrimaryAsync(order.SupplierOrganizationId.Value, ct)
                        .ConfigureAwait(false);

                    foreach (var row in active.OrderBy(r => r.ProductId.Value))
                    {
                        if (row.RemainingQuantity <= 0m)
                        {
                            if (expireLedger)
                            {
                                row.Expire(utcNow);
                            }
                            else
                            {
                                row.Release(utcNow);
                            }

                            await _reservations.UpdateAsync(row, ct).ConfigureAwait(false);
                            continue;
                        }

                        if (byProduct.TryGetValue(row.ProductId.Value, out var account) && account.IsTracked)
                        {
                            account.Release(row.RemainingQuantity);
                            account.Touch(utcNow);
                            await _inventory.UpdateAccountAsync(account, ct).ConfigureAwait(false);
                            await ApplyBranchReservationAsync(
                                    order.SupplierOrganizationId,
                                    row.BranchId,
                                    row.ProductId,
                                    account.OnHandQuantity,
                                    primaryId,
                                    balances,
                                    row.RemainingQuantity,
                                    BranchReservationEffect.Release,
                                    utcNow,
                                    ct)
                                .ConfigureAwait(false);
                        }

                        if (expireLedger)
                        {
                            row.Expire(utcNow);
                        }
                        else
                        {
                            row.Release(utcNow);
                        }

                        await _reservations.UpdateAsync(row, ct).ConfigureAwait(false);
                    }
                },
                cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task ReserveAsync(
        ConnectedPurchaseOrder order,
        ConnectedSupplierRelationship relationship,
        IReadOnlyList<Demand> demands,
        bool temporary,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken,
        DateTimeOffset? expiresAtUtc = null)
    {
        var branchGuid = relationship.SupplierBranchId;
        if (branchGuid is null || branchGuid == Guid.Empty)
        {
            var first = demands[0];
            throw new DomainException(
                ConnectedSupplierErrorCodes.InsufficientSupplierStock,
                $"{first.Name} has only 0 available; {FormatQty(first.PurchaseQty)} required.");
        }

        var branchId = PosBranchId.From(branchGuid.Value);
        var productIds = demands.Select(d => d.ProductId).ToList();
        var catalog = await _products
            .ListByIdsAsync(order.SupplierOrganizationId, productIds, cancellationToken)
            .ConfigureAwait(false);
        var productsById = catalog.ToDictionary(p => p.Id.Value);
        var unitsByProduct = await _units
            .ListByProductIdsAsync(order.SupplierOrganizationId, productIds, cancellationToken)
            .ConfigureAwait(false);

        var revision = order.InventoryReservationRevision;
        if (revision < 1)
        {
            revision = 1;
            // Domain mark methods will align revision when they run after this.
        }

        await _inventory
            .ExecuteWithProductReservationLocksAsync(
                order.SupplierOrganizationId,
                productIds,
                async (accounts, ct) =>
                {
                    var byProduct = accounts.ToDictionary(a => a.ProductId.Value);
                    var balances = (await _branchBalances
                            .ListByProductIdsAsync(order.SupplierOrganizationId, productIds, ct)
                            .ConfigureAwait(false))
                        .ToList();
                    var primaryId = await ResolvePrimaryAsync(order.SupplierOrganizationId.Value, ct)
                        .ConfigureAwait(false);

                    foreach (var demand in demands.OrderBy(d => d.Name, StringComparer.OrdinalIgnoreCase))
                    {
                        if (!byProduct.TryGetValue(demand.ProductId.Value, out var account) || !account.IsTracked)
                        {
                            continue;
                        }

                        if (!productsById.TryGetValue(demand.ProductId.Value, out var product))
                        {
                            throw new DomainException(
                                ApplicationErrorCodes.SaleProductNotFound,
                                $"Supplier product '{demand.Name}' was not found.");
                        }

                        var multiplier = ResolveMultiplier(
                            product,
                            demand.Uom,
                            unitsByProduct.TryGetValue(demand.ProductId.Value, out var units)
                                ? units
                                : Array.Empty<CatalogProductUnit>());
                        var neededBase = ProductUnitConversion.ToBaseQuantity(demand.PurchaseQty, multiplier);

                        var onHand = BranchStockResolver.ResolveOnHand(
                            branchId,
                            primaryId,
                            account.OnHandQuantity,
                            balances,
                            demand.ProductId);
                        var reserved = BranchStockResolver.ResolveReserved(branchId, balances, demand.ProductId);
                        var availableBase = BranchStockResolver.ResolveAvailable(onHand, reserved);
                        var availablePurchase = availableBase / multiplier;

                        if (neededBase > availableBase)
                        {
                            throw new DomainException(
                                ConnectedSupplierErrorCodes.InsufficientSupplierStock,
                                $"{demand.Name} has only {FormatQty(availablePurchase)} available; {FormatQty(demand.PurchaseQty)} required.");
                        }

                        account.Reserve(neededBase);
                        account.Touch(utcNow);
                        await _inventory.UpdateAccountAsync(account, ct).ConfigureAwait(false);
                        await ApplyBranchReservationAsync(
                                order.SupplierOrganizationId,
                                branchId,
                                demand.ProductId,
                                account.OnHandQuantity,
                                primaryId,
                                balances,
                                neededBase,
                                BranchReservationEffect.Reserve,
                                utcNow,
                                ct)
                            .ConfigureAwait(false);

                        ConnectedPoInventoryReservation ledger = temporary
                            ? ConnectedPoInventoryReservation.CreateTemporary(
                                order.SupplierOrganizationId,
                                branchId,
                                demand.ProductId,
                                order.Id,
                                revision,
                                neededBase,
                                utcNow,
                                expiresAtUtc
                                ?? utcNow.Add(ConnectedPoInventoryReservationOptions.DefaultProposalHoldDuration))
                            : ConnectedPoInventoryReservation.CreateConfirmed(
                                order.SupplierOrganizationId,
                                branchId,
                                demand.ProductId,
                                order.Id,
                                revision,
                                neededBase,
                                utcNow);

                        await _reservations.AddAsync(ledger, ct).ConfigureAwait(false);
                    }
                },
                cancellationToken)
            .ConfigureAwait(false);
    }

    private static List<Demand> BuildDemands(
        ConnectedPurchaseOrder order,
        Func<ConnectedPurchaseOrderLine, decimal> qtySelector)
    {
        return order.Lines
            .Select(l => new { Line = l, Qty = qtySelector(l) })
            .Where(x => x.Qty > 0m)
            .GroupBy(x => x.Line.ProductId.Value)
            .Select(g =>
            {
                var first = g.First().Line;
                return new Demand(
                    first.ProductId,
                    first.NameSnapshot,
                    first.UnitOfMeasureCode,
                    g.Sum(x => x.Qty));
            })
            .ToList();
    }

    private async Task ApplyBranchReservationAsync(
        PosOrganizationId organizationId,
        PosBranchId branchId,
        CatalogProductId productId,
        decimal organizationOnHand,
        Guid? primaryId,
        List<InventoryBranchBalance> balances,
        decimal quantity,
        BranchReservationEffect effect,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        if (quantity == 0m)
        {
            return;
        }

        var balance = BranchStockResolver.EnsureBalance(
            organizationId,
            branchId,
            productId,
            organizationOnHand,
            primaryId,
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

    private static decimal ResolveMultiplier(
        CatalogProduct product,
        string lineUom,
        IReadOnlyList<CatalogProductUnit> units)
    {
        var baseCode = UnitOfMeasures.ToCode(product.UnitOfMeasure);
        if (string.IsNullOrWhiteSpace(lineUom)
            || string.Equals(lineUom.Trim(), baseCode, StringComparison.OrdinalIgnoreCase))
        {
            return 1m;
        }

        var needle = lineUom.Trim();
        foreach (var unit in units.Where(u => u.IsActive))
        {
            if (string.Equals(unit.DisplayName, needle, StringComparison.OrdinalIgnoreCase)
                || string.Equals(unit.ShortLabel, needle, StringComparison.OrdinalIgnoreCase))
            {
                return unit.MultiplierToBase > 0m ? unit.MultiplierToBase : 1m;
            }
        }

        return 1m;
    }

    private static string FormatQty(decimal value) =>
        value == decimal.Truncate(value)
            ? decimal.Truncate(value).ToString(CultureInfo.InvariantCulture)
            : value.ToString("0.####", CultureInfo.InvariantCulture);

    private sealed record Demand(
        CatalogProductId ProductId,
        string Name,
        string Uom,
        decimal PurchaseQty);
}
