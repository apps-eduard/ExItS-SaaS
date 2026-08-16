using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.CustomerOrdering;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;

namespace ExItS.PinoyBusinessPOS.Application.CustomerOrdering;

public interface ICustomerOrderStockService
{
    /// <summary>Soft availability check on submit — does not reserve.</summary>
    Task EnsureAvailableAsync(
        PosOrganizationId organizationId,
        IReadOnlyList<CustomerOrderLineDraft> lines,
        CancellationToken cancellationToken = default);

    Task ReserveForAcceptAsync(
        CustomerOrder order,
        Guid actorId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken = default);

    Task ReleaseIfReservedAsync(
        CustomerOrder order,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken = default);

    Task ConsumeOnCompleteAsync(
        CustomerOrder order,
        IReadOnlyDictionary<Guid, CatalogProduct> productsById,
        Guid actorId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken = default);
}

public sealed class CustomerOrderStockService : ICustomerOrderStockService
{
    private readonly IInventoryRepository _inventory;

    public CustomerOrderStockService(IInventoryRepository inventory) => _inventory = inventory;

    public async Task EnsureAvailableAsync(
        PosOrganizationId organizationId,
        IReadOnlyList<CustomerOrderLineDraft> lines,
        CancellationToken cancellationToken = default)
    {
        var productIds = lines.Select(l => l.ProductId).Distinct().ToList();
        var accounts = await _inventory
            .ListByProductIdsAsync(organizationId, productIds, cancellationToken)
            .ConfigureAwait(false);
        var byProduct = accounts.ToDictionary(a => a.ProductId.Value);

        foreach (var group in lines.GroupBy(l => l.ProductId.Value))
        {
            if (!byProduct.TryGetValue(group.Key, out var account) || !account.IsTracked)
            {
                continue;
            }

            var needed = group.Sum(l => l.Quantity);
            if (account.AvailableQuantity < needed)
            {
                throw new DomainException(
                    ApplicationErrorCodes.InsufficientStock,
                    "Insufficient available stock for one or more order lines.");
            }
        }
    }

    public async Task ReserveForAcceptAsync(
        CustomerOrder order,
        Guid actorId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken = default)
    {
        _ = actorId;
        if (order.StockReservationState == CustomerOrderStockReservationState.Reserved)
        {
            return;
        }

        var productIds = order.Lines.Select(l => l.ProductId).Distinct().ToList();
        await _inventory
            .ExecuteWithProductReservationLocksAsync(
                order.SellerOrganizationId,
                productIds,
                async (accounts, ct) =>
                {
                    var byProduct = accounts.ToDictionary(a => a.ProductId.Value);
                    foreach (var line in order.Lines.OrderBy(l => l.LineNumber))
                    {
                        if (!byProduct.TryGetValue(line.ProductId.Value, out var account) || !account.IsTracked)
                        {
                            continue;
                        }

                        account.Reserve(line.Quantity);
                        account.Touch(utcNow);
                        await _inventory.UpdateAccountAsync(account, ct).ConfigureAwait(false);
                    }
                },
                cancellationToken)
            .ConfigureAwait(false);

        order.MarkStockReserved(utcNow);
    }

    public async Task ReleaseIfReservedAsync(
        CustomerOrder order,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken = default)
    {
        if (order.StockReservationState != CustomerOrderStockReservationState.Reserved)
        {
            return;
        }

        var productIds = order.Lines.Select(l => l.ProductId).Distinct().ToList();
        await _inventory
            .ExecuteWithProductReservationLocksAsync(
                order.SellerOrganizationId,
                productIds,
                async (accounts, ct) =>
                {
                    var byProduct = accounts.ToDictionary(a => a.ProductId.Value);
                    foreach (var line in order.Lines.OrderBy(l => l.LineNumber))
                    {
                        if (!byProduct.TryGetValue(line.ProductId.Value, out var account) || !account.IsTracked)
                        {
                            continue;
                        }

                        account.Release(line.Quantity);
                        account.Touch(utcNow);
                        await _inventory.UpdateAccountAsync(account, ct).ConfigureAwait(false);
                    }
                },
                cancellationToken)
            .ConfigureAwait(false);

        order.MarkStockReleased(utcNow);
    }

    public async Task ConsumeOnCompleteAsync(
        CustomerOrder order,
        IReadOnlyDictionary<Guid, CatalogProduct> productsById,
        Guid actorId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken = default)
    {
        if (order.StockReservationState == CustomerOrderStockReservationState.Consumed)
        {
            return;
        }

        if (order.StockReservationState != CustomerOrderStockReservationState.Reserved)
        {
            return;
        }

        var productIds = order.Lines.Select(l => l.ProductId).Distinct().ToList();
        await _inventory
            .ExecuteWithProductReservationLocksAsync(
                order.SellerOrganizationId,
                productIds,
                async (accounts, ct) =>
                {
                    var byProduct = accounts.ToDictionary(a => a.ProductId.Value);
                    foreach (var line in order.Lines.OrderBy(l => l.LineNumber))
                    {
                        if (!byProduct.TryGetValue(line.ProductId.Value, out var account) || !account.IsTracked)
                        {
                            continue;
                        }

                        if (await _inventory
                                .HasCustomerOrderDeductionAsync(
                                    order.SellerOrganizationId,
                                    order.Id,
                                    line.ProductId,
                                    ct)
                                .ConfigureAwait(false))
                        {
                            continue;
                        }

                        if (!productsById.TryGetValue(line.ProductId.Value, out var product))
                        {
                            throw new DomainException(
                                ApplicationErrorCodes.SaleProductNotFound,
                                "One or more products on the order were not found.");
                        }

                        account.ConsumeReservation(line.Quantity);
                        account.Touch(utcNow);
                        var movement = StockMovement.CustomerOrderDeduction(
                            order.SellerOrganizationId,
                            line.ProductId,
                            account.Id,
                            line.Quantity,
                            line.UnitSnapshot,
                            order.Id.Value,
                            actorId,
                            utcNow,
                            sellingMode: product.SellingMode);
                        await _inventory.UpdateAccountAsync(account, ct).ConfigureAwait(false);
                        await _inventory.AddMovementAsync(movement, ct).ConfigureAwait(false);
                    }
                },
                cancellationToken)
            .ConfigureAwait(false);

        order.MarkStockConsumed(utcNow);
    }
}
