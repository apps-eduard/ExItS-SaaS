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
    Task<ApplicationResult> EnsureAvailableAsync(
        PosOrganizationId organizationId,
        IReadOnlyList<CustomerOrderLineDraft> lines,
        CancellationToken cancellationToken = default);

    Task<ApplicationResult> EnsureAvailableAsync(
        PosOrganizationId organizationId,
        IReadOnlyList<CustomerOrderLineDraft> lines,
        Guid fulfillmentBranchId,
        CancellationToken cancellationToken = default) =>
        EnsureAvailableAsync(organizationId, lines, cancellationToken);

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
    private readonly IInventoryBranchBalanceRepository? _branchBalances;
    private readonly IOrganizationBranchDirectory? _branches;

    public CustomerOrderStockService(
        IInventoryRepository inventory,
        IInventoryBranchBalanceRepository? branchBalances = null,
        IOrganizationBranchDirectory? branches = null)
    {
        _inventory = inventory;
        _branchBalances = branchBalances;
        _branches = branches;
    }

    public Task<ApplicationResult> EnsureAvailableAsync(
        PosOrganizationId organizationId,
        IReadOnlyList<CustomerOrderLineDraft> lines,
        CancellationToken cancellationToken = default) =>
        EnsureAvailableAsync(organizationId, lines, fulfillmentBranchId: Guid.Empty, cancellationToken);

    public async Task<ApplicationResult> EnsureAvailableAsync(
        PosOrganizationId organizationId,
        IReadOnlyList<CustomerOrderLineDraft> lines,
        Guid fulfillmentBranchId,
        CancellationToken cancellationToken = default)
    {
        var productIds = lines.Select(l => l.ProductId).Distinct().ToList();
        var accounts = await _inventory
            .ListByProductIdsAsync(organizationId, productIds, cancellationToken)
            .ConfigureAwait(false);
        var byProduct = accounts.ToDictionary(a => a.ProductId.Value);
        var balances = await LoadBalancesAsync(organizationId, productIds, cancellationToken).ConfigureAwait(false);
        var primaryId = await ResolvePrimaryAsync(organizationId.Value, cancellationToken).ConfigureAwait(false);
        var branchId = fulfillmentBranchId == Guid.Empty ? (PosBranchId?)null : PosBranchId.From(fulfillmentBranchId);

        foreach (var group in lines.GroupBy(l => l.ProductId.Value))
        {
            if (!byProduct.TryGetValue(group.Key, out var account) || !account.IsTracked)
            {
                continue;
            }

            var needed = group.Sum(l => l.Quantity);
            var available = account.AvailableQuantity;
            if (branchId is not null)
            {
                var onHand = BranchStockResolver.ResolveOnHand(
                    branchId,
                    primaryId,
                    account.OnHandQuantity,
                    balances,
                    CatalogProductId.From(group.Key));
                available = BranchStockResolver.ResolveAvailable(onHand, account.AvailableQuantity);
            }

            if (available < needed)
            {
                var first = group.First();
                return ApplicationResult.Failure(
                    ApplicationErrorCodes.InsufficientStock,
                    "Stock changed for one or more products.",
                    new Dictionary<string, string>
                    {
                        ["productId"] = group.Key.ToString("D"),
                        ["productName"] = first.NameSnapshot,
                        ["requestedQuantity"] = needed.ToString(System.Globalization.CultureInfo.InvariantCulture),
                        ["availableQuantity"] = available.ToString(
                            System.Globalization.CultureInfo.InvariantCulture)
                    });
            }
        }

        return ApplicationResult.Success();
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
                    var balances = await LoadBalanceListAsync(order.SellerOrganizationId, productIds, ct)
                        .ConfigureAwait(false);
                    var primaryId = await ResolvePrimaryAsync(order.SellerOrganizationId.Value, ct)
                        .ConfigureAwait(false);
                    var branchId = PosBranchId.From(order.FulfillmentBranchId);
                    foreach (var line in order.Lines.OrderBy(l => l.LineNumber))
                    {
                        if (!byProduct.TryGetValue(line.ProductId.Value, out var account) || !account.IsTracked)
                        {
                            continue;
                        }

                        var onHand = BranchStockResolver.ResolveOnHand(
                            branchId,
                            primaryId,
                            account.OnHandQuantity,
                            balances,
                            line.ProductId);
                        if (BranchStockResolver.ResolveAvailable(onHand, account.AvailableQuantity) < line.Quantity)
                        {
                            throw new DomainException(
                                ApplicationErrorCodes.InsufficientStock,
                                "Insufficient available stock for this fulfillment branch.");
                        }

                        account.Reserve(line.Quantity);
                        account.Touch(utcNow);
                        await _inventory.UpdateAccountAsync(account, ct).ConfigureAwait(false);
                        await ApplyBranchDeltaAsync(
                                order.SellerOrganizationId,
                                branchId,
                                line.ProductId,
                                account.OnHandQuantity,
                                primaryId,
                                balances,
                                -line.Quantity,
                                utcNow,
                                ct)
                            .ConfigureAwait(false);
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
                    var balances = await LoadBalanceListAsync(order.SellerOrganizationId, productIds, ct)
                        .ConfigureAwait(false);
                    var primaryId = await ResolvePrimaryAsync(order.SellerOrganizationId.Value, ct)
                        .ConfigureAwait(false);
                    var branchId = PosBranchId.From(order.FulfillmentBranchId);
                    foreach (var line in order.Lines.OrderBy(l => l.LineNumber))
                    {
                        if (!byProduct.TryGetValue(line.ProductId.Value, out var account) || !account.IsTracked)
                        {
                            continue;
                        }

                        account.Release(line.Quantity);
                        account.Touch(utcNow);
                        await _inventory.UpdateAccountAsync(account, ct).ConfigureAwait(false);
                        await ApplyBranchDeltaAsync(
                                order.SellerOrganizationId,
                                branchId,
                                line.ProductId,
                                account.OnHandQuantity,
                                primaryId,
                                balances,
                                line.Quantity,
                                utcNow,
                                ct)
                            .ConfigureAwait(false);
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

    private async Task<IReadOnlyList<InventoryBranchBalance>> LoadBalancesAsync(
        PosOrganizationId organizationId,
        IReadOnlyList<CatalogProductId> productIds,
        CancellationToken cancellationToken)
    {
        if (_branchBalances is null || productIds.Count == 0)
        {
            return [];
        }

        return await _branchBalances
            .ListByProductIdsAsync(organizationId, productIds, cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<List<InventoryBranchBalance>> LoadBalanceListAsync(
        PosOrganizationId organizationId,
        IReadOnlyList<CatalogProductId> productIds,
        CancellationToken cancellationToken)
    {
        var loaded = await LoadBalancesAsync(organizationId, productIds, cancellationToken).ConfigureAwait(false);
        return loaded.ToList();
    }

    private async Task<Guid?> ResolvePrimaryAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        if (_branches is null)
        {
            return null;
        }

        return await _branches.GetPrimaryBranchIdAsync(organizationId, cancellationToken).ConfigureAwait(false);
    }

    private async Task ApplyBranchDeltaAsync(
        PosOrganizationId organizationId,
        PosBranchId branchId,
        CatalogProductId productId,
        decimal organizationOnHand,
        Guid? primaryId,
        List<InventoryBranchBalance> balances,
        decimal signedQuantity,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        if (_branchBalances is null || signedQuantity == 0m)
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
        balance.Apply(signedQuantity, utcNow);
        await _branchBalances.UpsertAsync(balance, cancellationToken).ConfigureAwait(false);
    }
}
