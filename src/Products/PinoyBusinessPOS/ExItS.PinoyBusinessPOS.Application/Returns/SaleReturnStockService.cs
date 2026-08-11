using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
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

    public SaleReturnStockService(IInventoryRepository inventory, ICatalogProductRepository products)
    {
        _inventory = inventory;
        _products = products;
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

        var productIds = restockLines.Select(l => l.ProductId).Distinct().ToList();
        var accounts = await _inventory
            .ListByProductIdsAsync(organizationId, productIds, cancellationToken)
            .ConfigureAwait(false);
        var accountsByProduct = accounts.ToDictionary(a => a.ProductId.Value);

        var saleLineById = originalSale.Lines.ToDictionary(l => l.Id.Value);
        var needsProductFallback = restockLines.Any(l => !saleLineById.ContainsKey(l.SaleLineId.Value));
        IReadOnlyDictionary<Guid, SellingMode> productModes = needsProductFallback
            ? (await _products.ListByIdsAsync(organizationId, productIds, cancellationToken).ConfigureAwait(false))
                .ToDictionary(p => p.Id.Value, p => p.SellingMode)
            : new Dictionary<Guid, SellingMode>();

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

            SellingMode sellingMode;
            if (saleLineById.TryGetValue(line.SaleLineId.Value, out var saleLine))
            {
                sellingMode = saleLine.SellingModeSnapshot;
            }
            else if (productModes.TryGetValue(line.ProductId.Value, out var productMode))
            {
                sellingMode = productMode;
            }
            else
            {
                sellingMode = SellingMode.PerItem;
            }

            var movement = StockMovement.SaleReturnRestock(
                organizationId,
                line.ProductId,
                account.Id,
                line.QuantityReturned,
                line.UomSnapshot,
                saleReturn.Id.Value,
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
