using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Purchasing;

namespace ExItS.PinoyBusinessPOS.Application.Purchasing;

/// <summary>
/// Purchase receive stock hook. Applied atomically inside receive transaction for tracked products only.
/// Online-only; no offline purchasing queue.
/// </summary>
public interface IPurchaseStockService
{
    Task ApplyReceiptAsync(
        PosOrganizationId organizationId,
        GoodsReceipt receipt,
        PurchaseOrder purchaseOrder,
        Guid actorId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken = default);
}

public sealed class PurchaseStockService : IPurchaseStockService
{
    private readonly IInventoryRepository _inventory;
    private readonly ICatalogProductRepository _products;
    private readonly InventoryLotStockService _lots;

    public PurchaseStockService(
        IInventoryRepository inventory,
        ICatalogProductRepository products,
        InventoryLotStockService lots)
    {
        _inventory = inventory;
        _products = products;
        _lots = lots;
    }

    public async Task ApplyReceiptAsync(
        PosOrganizationId organizationId,
        GoodsReceipt receipt,
        PurchaseOrder purchaseOrder,
        Guid actorId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken = default)
    {
        var productIds = receipt.Lines.Select(l => l.ProductId).Distinct().ToList();
        var accounts = await _inventory
            .ListByProductIdsAsync(organizationId, productIds, cancellationToken)
            .ConfigureAwait(false);
        var accountsByProduct = accounts.ToDictionary(a => a.ProductId.Value);

        var catalogProducts = await _products
            .ListByIdsAsync(organizationId, productIds, cancellationToken)
            .ConfigureAwait(false);
        var productsById = catalogProducts.ToDictionary(p => p.Id.Value);

        foreach (var line in receipt.Lines.OrderBy(l => l.LineNumber))
        {
            if (line.QuantityReceived <= 0m)
            {
                // Damaged/rejected/short-only lines never enter usable inventory.
                continue;
            }

            if (!accountsByProduct.TryGetValue(line.ProductId.Value, out var account) || !account.IsTracked)
            {
                continue;
            }

            if (await _inventory
                    .HasPurchaseReceiptAsync(organizationId, receipt.Id, line.ProductId, cancellationToken)
                    .ConfigureAwait(false))
            {
                continue;
            }

            if (!productsById.TryGetValue(line.ProductId.Value, out var product))
            {
                throw new DomainException(
                    DomainErrorCodes.InvalidGoodsReceiptLine,
                    "Product was not found for goods receipt stock apply.");
            }

            if (product.TracksExpiration && line.ExpiryDate is null)
            {
                throw new DomainException(
                    DomainErrorCodes.InventoryExpirationRequired,
                    "Expiry date is required when receiving expiration-tracked stock.");
            }

            var sellingMode = product.SellingMode;

            // Inventory ledger is always in base units. Purchase-unit receipts convert via
            // GoodsReceiptLine.BaseQuantity; UnitCost is cost per base unit (snapshot ÷ multiplier).
            var movement = StockMovement.PurchaseReceipt(
                organizationId,
                line.ProductId,
                account.Id,
                line.BaseQuantity,
                line.UomSnapshot,
                receipt.Id.Value,
                actorId,
                utcNow,
                sellingMode: sellingMode,
                unitCost: line.BaseUnitCost);

            if (product.TracksExpiration)
            {
                var receivedLot = await _lots
                    .ReceiveAsync(
                        organizationId,
                        line.ProductId,
                        line.ExpiryDate!.Value,
                        line.BaseQuantity,
                        actorId,
                        utcNow,
                        StockMovementType.PurchaseReceipt,
                        StockMovementSourceType.PurchaseReceipt,
                        branchId: null,
                        lotNumber: line.LotNumber,
                        sourceId: receipt.Id.Value,
                        stockMovementId: movement.Id.Value,
                        cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
                movement = movement.WithLot(receivedLot.Id);
            }

            line.AttachInventoryMovement(movement.Id);
            account.ApplyMovementEffect(movement.QuantityEffect);
            account.Touch(utcNow);
            await _inventory.UpdateAccountAsync(account, cancellationToken).ConfigureAwait(false);
            await _inventory.AddMovementAsync(movement, cancellationToken).ConfigureAwait(false);
        }
    }
}
