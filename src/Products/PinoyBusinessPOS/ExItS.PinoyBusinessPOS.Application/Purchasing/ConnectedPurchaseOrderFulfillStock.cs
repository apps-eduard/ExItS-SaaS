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

namespace ExItS.PinoyBusinessPOS.Application.Purchasing;

/// <summary>
/// Deducts supplier tracked stock when a connected PO is fulfilled/delivered.
/// Untracked products are skipped (never auto-tracked). Idempotent per CPO + product.
/// </summary>
public sealed class ConnectedPurchaseOrderFulfillStock
{
    private readonly IInventoryRepository _inventory;
    private readonly ICatalogProductRepository _products;
    private readonly ICatalogProductUnitRepository _units;
    private readonly IInventoryBranchBalanceRepository _branchBalances;
    private readonly BranchInventoryMutationService _branchMutations;
    private readonly InventoryLotStockService? _lots;
    private readonly IOrganizationBranchDirectory? _branches;

    public ConnectedPurchaseOrderFulfillStock(
        IInventoryRepository inventory,
        ICatalogProductRepository products,
        ICatalogProductUnitRepository units,
        IInventoryBranchBalanceRepository branchBalances,
        BranchInventoryMutationService branchMutations,
        InventoryLotStockService? lots = null,
        IOrganizationBranchDirectory? branches = null)
    {
        _inventory = inventory;
        _products = products;
        _units = units;
        _branchBalances = branchBalances;
        _branchMutations = branchMutations;
        _lots = lots;
        _branches = branches;
    }

    public async Task ApplyAsync(
        ConnectedPurchaseOrder order,
        ConnectedSupplierRelationship relationship,
        Guid actorId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(order);
        ArgumentNullException.ThrowIfNull(relationship);

        if (actorId == Guid.Empty)
        {
            throw new DomainException(
                ApplicationErrorCodes.ActorRequired,
                "An actor identifier is required to fulfill a connected purchase order.");
        }

        var demands = order.Lines
            .Where(l => l.FulfillmentQty > 0m)
            .GroupBy(l => l.ProductId.Value)
            .Select(g =>
            {
                var first = g.First();
                return new
                {
                    ProductId = first.ProductId,
                    Name = first.NameSnapshot,
                    Uom = first.UnitOfMeasureCode,
                    PurchaseQty = g.Sum(x => x.FulfillmentQty),
                };
            })
            .ToList();

        if (demands.Count == 0)
        {
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

        await _inventory
            .ExecuteWithProductReservationLocksAsync(
                order.SupplierOrganizationId,
                productIds,
                async (accounts, ct) =>
                {
                    var byProduct = accounts.ToDictionary(a => a.ProductId.Value);
                    var balanceRows = await _branchBalances
                        .ListByProductIdsAsync(order.SupplierOrganizationId, productIds, ct)
                        .ConfigureAwait(false);

                    foreach (var demand in demands.OrderBy(d => d.Name, StringComparer.OrdinalIgnoreCase))
                    {
                        if (!byProduct.TryGetValue(demand.ProductId.Value, out var account) || !account.IsTracked)
                        {
                            continue;
                        }

                        if (await _inventory
                                .HasConnectedPurchaseFulfillmentAsync(
                                    order.SupplierOrganizationId,
                                    order.Id,
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

                        if (neededBase > availableBase)
                        {
                            throw new DomainException(
                                ConnectedSupplierErrorCodes.InsufficientSupplierStock,
                                $"{demand.Name} has only {FormatQty(availablePurchase)} available; {FormatQty(demand.PurchaseQty)} required.");
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
                                        sourceId: order.Id.Value,
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

                        var orgOnHandBefore = account.OnHandQuantity;
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
                                branchId: supplierBranch.Value)
                            .WithBranch(supplierBranch.Value);

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
                        await _inventory.UpdateAccountAsync(account, ct).ConfigureAwait(false);
                        await _inventory.AddMovementAsync(movement, ct).ConfigureAwait(false);
                    }
                },
                cancellationToken)
            .ConfigureAwait(false);
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
}
