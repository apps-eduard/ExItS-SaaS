using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Returns;

namespace ExItS.PinoyBusinessPOS.Application.Returns;

/// <summary>
/// Return restock hook. Applied atomically inside return create for ReturnToStock tracked products only.
/// </summary>
public interface ISaleReturnStockService
{
    Task RestockForReturnAsync(
        PosOrganizationId organizationId,
        SaleReturn saleReturn,
        Guid actorId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken = default);
}

public sealed class SaleReturnStockService : ISaleReturnStockService
{
    private readonly IInventoryRepository _inventory;

    public SaleReturnStockService(IInventoryRepository inventory) => _inventory = inventory;

    public async Task RestockForReturnAsync(
        PosOrganizationId organizationId,
        SaleReturn saleReturn,
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

        var productIds = restockLines.Select(l => l.ProductId).Distinct().ToList();
        var accounts = await _inventory
            .ListByProductIdsAsync(organizationId, productIds, cancellationToken)
            .ConfigureAwait(false);
        var accountsByProduct = accounts.ToDictionary(a => a.ProductId.Value);

        foreach (var line in restockLines)
        {
            if (!accountsByProduct.TryGetValue(line.ProductId.Value, out var account) || !account.IsTracked)
            {
                continue;
            }

            if (await _inventory
                    .HasSaleReturnRestockAsync(
                        organizationId,
                        saleReturn.Id,
                        line.ProductId,
                        cancellationToken)
                    .ConfigureAwait(false))
            {
                continue;
            }

            var movement = StockMovement.SaleReturnRestock(
                organizationId,
                line.ProductId,
                account.Id,
                line.QuantityReturned,
                line.UomSnapshot,
                saleReturn.Id.Value,
                actorId,
                utcNow);
            line.AttachInventoryMovement(movement.Id);
            account.ApplyMovementEffect(movement.QuantityEffect);
            account.Touch(utcNow);
            await _inventory.UpdateAccountAsync(account, cancellationToken).ConfigureAwait(false);
            await _inventory.AddMovementAsync(movement, cancellationToken).ConfigureAwait(false);
        }
    }
}
