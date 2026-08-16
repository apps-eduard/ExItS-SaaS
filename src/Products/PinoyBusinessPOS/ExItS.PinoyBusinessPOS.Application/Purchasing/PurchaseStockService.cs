using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
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

    public PurchaseStockService(IInventoryRepository inventory, ICatalogProductRepository products)
    {
        _inventory = inventory;
        _products = products;
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
        var sellingModeByProduct = catalogProducts.ToDictionary(p => p.Id.Value, p => p.SellingMode);

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

            var sellingMode = sellingModeByProduct.TryGetValue(line.ProductId.Value, out var mode)
                ? mode
                : SellingMode.PerItem;

            // Inventory ledger is always in base units. Purchase-unit receipts convert via
            // GoodsReceiptLine.BaseQuantity (QuantityReceived × MultiplierToBaseSnapshot).
            var movement = StockMovement.PurchaseReceipt(
                organizationId,
                line.ProductId,
                account.Id,
                line.BaseQuantity,
                line.UomSnapshot,
                receipt.Id.Value,
                actorId,
                utcNow,
                sellingMode: sellingMode);
            line.AttachInventoryMovement(movement.Id);
            account.ApplyMovementEffect(movement.QuantityEffect);
            account.Touch(utcNow);
            await _inventory.UpdateAccountAsync(account, cancellationToken).ConfigureAwait(false);
            await _inventory.AddMovementAsync(movement, cancellationToken).ConfigureAwait(false);
        }
    }
}
