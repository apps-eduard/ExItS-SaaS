using ExItS.PinoyBusinessPOS.Application.Inventory;
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

    public PurchaseStockService(IInventoryRepository inventory) => _inventory = inventory;

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

        foreach (var line in receipt.Lines.OrderBy(l => l.LineNumber))
        {
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

            var movement = StockMovement.PurchaseReceipt(
                organizationId,
                line.ProductId,
                account.Id,
                line.ReceivedQty,
                line.UomSnapshot,
                receipt.Id.Value,
                actorId,
                utcNow);
            account.ApplyMovementEffect(movement.QuantityEffect);
            account.Touch(utcNow);
            await _inventory.UpdateAccountAsync(account, cancellationToken).ConfigureAwait(false);
            await _inventory.AddMovementAsync(movement, cancellationToken).ConfigureAwait(false);
        }
    }
}
