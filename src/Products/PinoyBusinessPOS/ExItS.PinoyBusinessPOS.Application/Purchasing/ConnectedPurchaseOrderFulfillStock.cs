using System.Globalization;
using System.Security.Cryptography;
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
/// Deducts supplier tracked stock when a connected PO is fulfilled/delivered.
/// When a confirmed reservation exists, consumes that hold (on-hand decreases via ConsumeReservation).
/// Untracked products are skipped (never auto-tracked). Idempotent per CPO + product.
/// </summary>
public sealed class ConnectedPurchaseOrderFulfillStock
{
    private readonly IInventoryRepository _inventory;
    private readonly ICatalogProductRepository _products;
    private readonly ICatalogProductUnitRepository _units;
    private readonly IInventoryBranchBalanceRepository _branchBalances;
    private readonly BranchInventoryMutationService _branchMutations;
    private readonly ConnectedPoInventoryReservationService? _reservations;
    private readonly InventoryLotStockService? _lots;
    private readonly IOrganizationBranchDirectory? _branches;

    public ConnectedPurchaseOrderFulfillStock(
        IInventoryRepository inventory,
        ICatalogProductRepository products,
        ICatalogProductUnitRepository units,
        IInventoryBranchBalanceRepository branchBalances,
        BranchInventoryMutationService branchMutations,
        ConnectedPoInventoryReservationService? reservations = null,
        InventoryLotStockService? lots = null,
        IOrganizationBranchDirectory? branches = null)
    {
        _inventory = inventory;
        _products = products;
        _units = units;
        _branchBalances = branchBalances;
        _branchMutations = branchMutations;
        _reservations = reservations;
        _lots = lots;
        _branches = branches;
    }

    public async Task ApplyAsync(
        ConnectedPurchaseOrder order,
        ConnectedSupplierRelationship relationship,
        Guid actorId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken = default,
        IReadOnlyDictionary<Guid, decimal>? shipQtyBySupplierProduct = null)
    {
        ArgumentNullException.ThrowIfNull(order);
        ArgumentNullException.ThrowIfNull(relationship);

        if (actorId == Guid.Empty)
        {
            throw new DomainException(
                ApplicationErrorCodes.ActorRequired,
                "An actor identifier is required to fulfill a connected purchase order.");
        }

        // Remaining wave after prior Ready/Ship: FulfilledAtUtc set while status is Preparing/Accepted.
        // Each wave needs a distinct stock-movement SourceId (ux_stock_movements_connected_po_fulfill_source).
        var fulfillmentSourceId = ResolveFulfillmentSourceId(order);

        var demands = order.Lines
            .Select(l =>
            {
                var qty = shipQtyBySupplierProduct is not null
                    && shipQtyBySupplierProduct.TryGetValue(l.ProductId.Value, out var overrideQty)
                        ? overrideQty
                        : l.FulfillmentQty;
                return new { Line = l, Qty = qty };
            })
            .Where(x => x.Qty > 0m)
            .GroupBy(x => x.Line.ProductId.Value)
            .Select(g =>
            {
                var first = g.First().Line;
                return new
                {
                    ProductId = first.ProductId,
                    Name = first.NameSnapshot,
                    Uom = first.UnitOfMeasureCode,
                    PurchaseQty = g.Sum(x => x.Qty),
                };
            })
            .ToList();

        if (demands.Count == 0)
        {
            if (order.InventoryReservationState == ConnectedPoInventoryReservationState.Confirmed)
            {
                order.MarkInventoryConsumed(utcNow);
            }

            return;
        }

        var productIds = demands.Select(d => d.ProductId).ToList();
        var catalog = await _products
            .ListByIdsAsync(order.SupplierOrganizationId, productIds, cancellationToken)
            .ConfigureAwait(false);
        var productsById = catalog.ToDictionary(p => p.Id.Value);
        var unitsByProduct = await _units
            .ListByProductIdsAsync(order.SupplierOrganizationId, productIds, cancellationToken)
            .ConfigureAwait(false);

        Guid? primaryId = _branches is null
            ? null
            : await _branches
                .GetPrimaryBranchIdAsync(order.SupplierOrganizationId.Value, cancellationToken)
                .ConfigureAwait(false);

        var supplierBranchGuid = relationship.SupplierBranchId;
        PosBranchId? supplierBranch = supplierBranchGuid is Guid bid && bid != Guid.Empty
            ? PosBranchId.From(bid)
            : null;

        var consumedAnyHold = false;

        await _inventory
            .ExecuteWithProductReservationLocksAsync(
                order.SupplierOrganizationId,
                productIds,
                async (accounts, ct) =>
                {
                    var byProduct = accounts.ToDictionary(a => a.ProductId.Value);
                    var balanceRows = (await _branchBalances
                            .ListByProductIdsAsync(order.SupplierOrganizationId, productIds, ct)
                            .ConfigureAwait(false))
                        .ToList();

                    foreach (var demand in demands.OrderBy(d => d.Name, StringComparer.OrdinalIgnoreCase))
                    {
                        if (!byProduct.TryGetValue(demand.ProductId.Value, out var account) || !account.IsTracked)
                        {
                            continue;
                        }

                        // Idempotent per fulfillment SourceId (order id for first wave; revision wave id after).
                        if (await _inventory
                                .HasConnectedPurchaseFulfillmentAsync(
                                    order.SupplierOrganizationId,
                                    ConnectedPurchaseOrderId.From(fulfillmentSourceId),
                                    demand.ProductId,
                                    ct)
                                .ConfigureAwait(false))
                        {
                            continue;
                        }

                        if (!productsById.TryGetValue(demand.ProductId.Value, out var product))
                        {
                            throw new DomainException(
                                ApplicationErrorCodes.SaleProductNotFound,
                                $"Supplier product '{demand.Name}' was not found.");
                        }

                        if (supplierBranch is null)
                        {
                            throw new DomainException(
                                ConnectedSupplierErrorCodes.InsufficientSupplierStock,
                                $"{demand.Name} has only 0 available; {FormatQty(demand.PurchaseQty)} required.");
                        }

                        var multiplier = ResolveMultiplier(
                            product,
                            demand.Uom,
                            unitsByProduct.TryGetValue(demand.ProductId.Value, out var units)
                                ? units
                                : Array.Empty<CatalogProductUnit>());
                        var neededBase = ProductUnitConversion.ToBaseQuantity(demand.PurchaseQty, multiplier);

                        var onHand = BranchStockResolver.ResolveOnHand(
                            supplierBranch,
                            primaryId,
                            account.OnHandQuantity,
                            balanceRows,
                            demand.ProductId);
                        var reserved = BranchStockResolver.ResolveReserved(
                            supplierBranch,
                            balanceRows,
                            demand.ProductId);
                        var availableBase = BranchStockResolver.ResolveAvailable(onHand, reserved);
                        var availablePurchase = availableBase / multiplier;

                        var useReservation = _reservations is not null
                            && order.InventoryReservationState == ConnectedPoInventoryReservationState.Confirmed;

                        if (!useReservation && neededBase > availableBase)
                        {
                            throw new DomainException(
                                ConnectedSupplierErrorCodes.InsufficientSupplierStock,
                                $"{demand.Name} has only {FormatQty(availablePurchase)} available; {FormatQty(demand.PurchaseQty)} required.");
                        }

                        if (useReservation && onHand < neededBase)
                        {
                            throw new DomainException(
                                ConnectedSupplierErrorCodes.InsufficientSupplierStock,
                                $"{demand.Name} has only {FormatQty(onHand / multiplier)} on hand; {FormatQty(demand.PurchaseQty)} required.");
                        }

                        if (product.TracksExpiration && _lots is not null)
                        {
                            var today = InventoryLot.BusinessDateOf(utcNow);
                            try
                            {
                                await _lots
                                    .ConsumeFefoAsync(
                                        order.SupplierOrganizationId,
                                        demand.ProductId,
                                        neededBase,
                                        today,
                                        actorId,
                                        utcNow,
                                        StockMovementType.ConnectedPurchaseFulfillment,
                                        StockMovementSourceType.ConnectedPurchaseOrder,
                                        branchId: supplierBranch,
                                        sourceId: fulfillmentSourceId,
                                        cancellationToken: ct,
                                        primaryBranchId: primaryId)
                                    .ConfigureAwait(false);
                            }
                            catch (DomainException)
                            {
                                throw new DomainException(
                                    ConnectedSupplierErrorCodes.InsufficientSupplierStock,
                                    $"{demand.Name} has only {FormatQty(availablePurchase)} available; {FormatQty(demand.PurchaseQty)} required.");
                            }
                        }

                        var movement = StockMovement.ConnectedPurchaseFulfillment(
                                order.SupplierOrganizationId,
                                demand.ProductId,
                                account.Id,
                                neededBase,
                                product.UnitOfMeasure,
                                order.Id.Value,
                                actorId,
                                utcNow,
                                sellingMode: product.SellingMode,
                                branchId: supplierBranch.Value,
                                fulfillmentSourceId: fulfillmentSourceId)
                            .WithBranch(supplierBranch.Value);

                        if (useReservation)
                        {
                            var consumed = await _reservations!
                                .TryConsumeConfirmedHoldAsync(
                                    order,
                                    demand.ProductId,
                                    neededBase,
                                    account,
                                    supplierBranch,
                                    primaryId,
                                    balanceRows,
                                    utcNow,
                                    ct)
                                .ConfigureAwait(false);
                            if (!consumed)
                            {
                                var orgOnHandBefore = account.OnHandQuantity;
                                account.ApplyMovementEffect(movement.QuantityEffect);
                                account.Touch(utcNow);
                                await _branchMutations
                                    .ApplyBranchDeltaAsync(
                                        _branchBalances,
                                        order.SupplierOrganizationId,
                                        supplierBranch,
                                        primaryId,
                                        demand.ProductId,
                                        orgOnHandBefore,
                                        movement.QuantityEffect,
                                        utcNow,
                                        ct)
                                    .ConfigureAwait(false);
                            }
                            else
                            {
                                consumedAnyHold = true;
                            }
                        }
                        else
                        {
                            var orgOnHandBefore = account.OnHandQuantity;
                            account.ApplyMovementEffect(movement.QuantityEffect);
                            account.Touch(utcNow);
                            await _branchMutations
                                .ApplyBranchDeltaAsync(
                                    _branchBalances,
                                    order.SupplierOrganizationId,
                                    supplierBranch,
                                    primaryId,
                                    demand.ProductId,
                                    orgOnHandBefore,
                                    movement.QuantityEffect,
                                    utcNow,
                                    ct)
                                .ConfigureAwait(false);
                        }

                        await _inventory.UpdateAccountAsync(account, ct).ConfigureAwait(false);
                        await _inventory.AddMovementAsync(movement, ct).ConfigureAwait(false);
                    }
                },
                cancellationToken)
            .ConfigureAwait(false);

        if (consumedAnyHold
            || order.InventoryReservationState == ConnectedPoInventoryReservationState.Confirmed)
        {
            order.MarkInventoryConsumed(utcNow);
        }
    }

    /// <summary>
    /// Maps buyer outstanding qty onto supplier product ids for a remaining fulfillment wave.
    /// </summary>
    public static async Task<IReadOnlyDictionary<Guid, decimal>> ResolveOutstandingShipQuantitiesAsync(
        ConnectedPurchaseOrder order,
        PurchaseOrder buyerPo,
        IBuyerSupplierProductLinkRepository? links,
        CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<Guid, decimal>();
        IReadOnlyDictionary<Guid, BuyerSupplierProductLink>? byBuyerProduct = null;
        IReadOnlyDictionary<Guid, BuyerSupplierProductLink>? bySupplierProduct = null;
        if (links is not null)
        {
            var list = await links
                .ListAsync(order.RelationshipId, order.BuyerOrganizationId, cancellationToken)
                .ConfigureAwait(false);
            var active = list.Where(x => x.IsActive).ToList();
            byBuyerProduct = active.GroupBy(x => x.BuyerProductId.Value).ToDictionary(g => g.Key, g => g.First());
            bySupplierProduct = active.GroupBy(x => x.SupplierProductId.Value).ToDictionary(g => g.Key, g => g.First());
        }

        foreach (var cpoLine in order.Lines.Where(l => l.FulfillmentQty > 0m))
        {
            var poLine = buyerPo.Lines.FirstOrDefault(l =>
                l.SupplierProductId == cpoLine.ProductId
                || (bySupplierProduct is not null
                    && bySupplierProduct.TryGetValue(cpoLine.ProductId.Value, out var link)
                    && l.ProductId == link.BuyerProductId)
                || (byBuyerProduct is not null
                    && l.ProductId is not null
                    && byBuyerProduct.TryGetValue(l.ProductId.Value, out var link2)
                    && link2.SupplierProductId == cpoLine.ProductId));

            var outstanding = poLine?.OutstandingQty ?? 0m;
            if (outstanding > 0m)
            {
                result[cpoLine.ProductId.Value] = outstanding;
            }
        }

        return result;
    }

    /// <summary>
    /// First wave uses the connected PO id. Remaining waves use a revision-scoped id so
    /// <c>ux_stock_movements_connected_po_fulfill_source</c> allows another deduction.
    /// </summary>
    public static Guid ResolveFulfillmentSourceId(ConnectedPurchaseOrder order)
    {
        ArgumentNullException.ThrowIfNull(order);
        if (order.FulfilledAtUtc is null)
        {
            return order.Id.Value;
        }

        return WaveFulfillmentSourceId(order.Id.Value, order.InventoryReservationRevision);
    }

    /// <summary>
    /// Deterministic SourceId for a remaining fulfillment wave (order + reservation revision).
    /// </summary>
    public static Guid WaveFulfillmentSourceId(Guid connectedPurchaseOrderId, int reservationRevision)
    {
        Span<byte> data = stackalloc byte[36];
        WaveFulfillmentNamespace.TryWriteBytes(data[..16]);
        connectedPurchaseOrderId.TryWriteBytes(data[16..32]);
        BitConverter.TryWriteBytes(data[32..], reservationRevision);
        var hash = SHA256.HashData(data);
        return new Guid(hash.AsSpan(0, 16));
    }

    private static readonly Guid WaveFulfillmentNamespace =
        Guid.Parse("a3f0c8e1-5b2d-4e9a-8c17-6d4f2b9e1a05");

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
}
