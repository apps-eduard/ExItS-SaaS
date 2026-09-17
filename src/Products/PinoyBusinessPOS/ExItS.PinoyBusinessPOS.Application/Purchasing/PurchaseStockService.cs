using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Purchasing;

namespace ExItS.PinoyBusinessPOS.Application.Purchasing;

/// <summary>
/// Purchase receive stock hook. Applied atomically inside receive transaction.
/// Tracked products always receive stock. Untracked products receive stock only when
/// <c>enableTrackingIfNeeded</c> is true (explicit caller confirmation).
/// </summary>
public interface IPurchaseStockService
{
    Task<IReadOnlyList<PurchaseReceiptStockOutcome>> ApplyReceiptAsync(
        PosOrganizationId organizationId,
        GoodsReceipt receipt,
        PurchaseOrder purchaseOrder,
        Guid actorId,
        DateTimeOffset utcNow,
        bool enableTrackingIfNeeded = false,
        CancellationToken cancellationToken = default);
}

public sealed record PurchaseReceiptStockOutcome(
    Guid ProductId,
    Guid LineId,
    bool Applied,
    bool TrackingWasEnabled,
    decimal? PreviousOnHand,
    decimal? NewOnHand);

public sealed class PurchaseStockService : IPurchaseStockService
{
    private readonly IInventoryRepository _inventory;
    private readonly ICatalogProductRepository _products;
    private readonly IInventoryBranchBalanceRepository _branchBalances;
    private readonly InventoryLotStockService _lots;
    private readonly BranchInventoryMutationService _branchMutations;
    private readonly IOrganizationBranchDirectory? _branches;

    public PurchaseStockService(
        IInventoryRepository inventory,
        ICatalogProductRepository products,
        IInventoryBranchBalanceRepository branchBalances,
        InventoryLotStockService lots,
        BranchInventoryMutationService branchMutations,
        IOrganizationBranchDirectory? branches = null)
    {
        _inventory = inventory;
        _products = products;
        _branchBalances = branchBalances;
        _lots = lots;
        _branchMutations = branchMutations;
        _branches = branches;
    }

    public async Task<IReadOnlyList<PurchaseReceiptStockOutcome>> ApplyReceiptAsync(
        PosOrganizationId organizationId,
        GoodsReceipt receipt,
        PurchaseOrder purchaseOrder,
        Guid actorId,
        DateTimeOffset utcNow,
        bool enableTrackingIfNeeded = false,
        CancellationToken cancellationToken = default)
    {
        if (receipt.ReceivingBranchId is null)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidBranchId,
                "Goods receipt is missing receiving branch provenance.");
        }

        var receivingBranch = receipt.ReceivingBranchId;
        var productIds = receipt.Lines.Select(l => l.ProductId).Distinct().ToList();
        var accounts = await _inventory
            .ListByProductIdsAsync(organizationId, productIds, cancellationToken)
            .ConfigureAwait(false);
        var accountsByProduct = accounts.ToDictionary(a => a.ProductId.Value);

        var catalogProducts = await _products
            .ListByIdsAsync(organizationId, productIds, cancellationToken)
            .ConfigureAwait(false);
        var productsById = catalogProducts.ToDictionary(p => p.Id.Value);
        Guid? primaryBranchId = _branches is null
            ? null
            : await _branches.GetPrimaryBranchIdAsync(organizationId.Value, cancellationToken).ConfigureAwait(false);

        var outcomes = new List<PurchaseReceiptStockOutcome>();

        foreach (var line in receipt.Lines.OrderBy(l => l.LineNumber))
        {
            if (line.QuantityReceived <= 0m)
            {
                continue;
            }

            if (!productsById.TryGetValue(line.ProductId.Value, out var product))
            {
                throw new DomainException(
                    DomainErrorCodes.InvalidGoodsReceiptLine,
                    "Product was not found for goods receipt stock apply.");
            }

            var trackingWasEnabled = false;
            if (!accountsByProduct.TryGetValue(line.ProductId.Value, out var account))
            {
                if (!enableTrackingIfNeeded)
                {
                    continue;
                }

                account = InventoryAccount.CreateUntracked(organizationId, line.ProductId, utcNow);
                account.Enable(
                    openingQuantity: null,
                    product.UnitOfMeasure,
                    actorId,
                    utcNow,
                    hasOpeningStockAlready: false,
                    product.SellingMode);
                await _inventory.AddAccountAsync(account, cancellationToken).ConfigureAwait(false);
                accountsByProduct[line.ProductId.Value] = account;
                trackingWasEnabled = true;
            }
            else if (!account.IsTracked)
            {
                if (!enableTrackingIfNeeded)
                {
                    continue;
                }

                account.Enable(
                    openingQuantity: null,
                    product.UnitOfMeasure,
                    actorId,
                    utcNow,
                    hasOpeningStockAlready: await _inventory
                        .HasOpeningStockAsync(organizationId, line.ProductId, cancellationToken)
                        .ConfigureAwait(false),
                    product.SellingMode);
                await _inventory.UpdateAccountAsync(account, cancellationToken).ConfigureAwait(false);
                trackingWasEnabled = true;
            }

            if (await _inventory
                    .HasPurchaseReceiptAsync(organizationId, receipt.Id, line.ProductId, cancellationToken)
                    .ConfigureAwait(false))
            {
                outcomes.Add(new PurchaseReceiptStockOutcome(
                    line.ProductId.Value,
                    line.Id.Value,
                    Applied: false,
                    trackingWasEnabled,
                    PreviousOnHand: account.OnHandQuantity,
                    NewOnHand: account.OnHandQuantity));
                continue;
            }

            if (product.TracksExpiration && line.ExpiryDate is null)
            {
                throw new DomainException(
                    DomainErrorCodes.InventoryExpirationRequired,
                    "Expiry date is required when receiving expiration-tracked stock.");
            }

            var orgOnHandBefore = account.OnHandQuantity;
            var movement = StockMovement.PurchaseReceipt(
                    organizationId,
                    line.ProductId,
                    account.Id,
                    line.BaseQuantity,
                    line.UomSnapshot,
                    receipt.Id.Value,
                    actorId,
                    utcNow,
                    sellingMode: product.SellingMode,
                    unitCost: line.BaseUnitCost)
                .WithBranch(receivingBranch.Value);

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
                        receivingBranch,
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
            await _branchMutations
                .ApplyBranchDeltaAsync(
                    _branchBalances,
                    organizationId,
                    receivingBranch,
                    primaryBranchId,
                    line.ProductId,
                    orgOnHandBefore,
                    movement.QuantityEffect,
                    utcNow,
                    cancellationToken)
                .ConfigureAwait(false);
            await _inventory.UpdateAccountAsync(account, cancellationToken).ConfigureAwait(false);
            await _inventory.AddMovementAsync(movement, cancellationToken).ConfigureAwait(false);

            outcomes.Add(new PurchaseReceiptStockOutcome(
                line.ProductId.Value,
                line.Id.Value,
                Applied: true,
                trackingWasEnabled,
                PreviousOnHand: orgOnHandBefore,
                NewOnHand: account.OnHandQuantity));
        }

        return outcomes;
    }
}
