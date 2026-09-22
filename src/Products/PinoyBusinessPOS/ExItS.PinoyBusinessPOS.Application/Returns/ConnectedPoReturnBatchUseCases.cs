using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Application.Credit;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Application.Purchasing;
using ExItS.PinoyBusinessPOS.Application.SupplierPayables;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Credit;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Purchasing;
using ExItS.PinoyBusinessPOS.Domain.Returns;
using ExItS.PinoyBusinessPOS.Domain.Sales;
using ExItS.PinoyBusinessPOS.Domain.SupplierPayables;

namespace ExItS.PinoyBusinessPOS.Application.Returns;

/// <summary>
/// Dual-org inventory effects for connected-PO returns.
/// Request: buyer pending-return up (available drops, on-hand unchanged).
/// Seller receipt: buyer pending-return down and on-hand down; seller pending-return up.
/// Finalize: seller pending-return down, on-hand up by sellable only, damaged written off.
/// </summary>
public sealed class ConnectedPoReturnInventoryService
{
    private readonly IInventoryRepository _inventory;
    private readonly IInventoryBranchBalanceRepository? _branchBalances;
    private readonly IOrganizationBranchDirectory? _branches;
    private readonly ICatalogProductRepository? _products;

    public ConnectedPoReturnInventoryService(
        IInventoryRepository inventory,
        IInventoryBranchBalanceRepository? branchBalances = null,
        IOrganizationBranchDirectory? branches = null,
        ICatalogProductRepository? products = null)
    {
        _inventory = inventory;
        _branchBalances = branchBalances;
        _branches = branches;
        _products = products;
    }

    public Task IncreaseBuyerPendingReturnAsync(
        ReturnBatch batch,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken) =>
        AdjustPendingReturnAsync(
            batch.BuyerOrganizationId!,
            batch.BuyerBranchId,
            QuantitiesByProduct(batch, seller: false),
            increase: true,
            utcNow,
            cancellationToken);

    /// <summary>Buyer hand-off is confirmed: pending-return clears and the goods leave buyer on-hand.</summary>
    public async Task ApplySellerReceiptAsync(
        ReturnBatch batch,
        Guid actorId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        var buyerQuantities = QuantitiesByProduct(batch, seller: false);
        await AdjustPendingReturnAsync(
                batch.BuyerOrganizationId!,
                batch.BuyerBranchId,
                buyerQuantities,
                increase: false,
                utcNow,
                cancellationToken)
            .ConfigureAwait(false);
        await ApplyOnHandDeltaAsync(
                batch,
                batch.BuyerOrganizationId!,
                batch.BuyerBranchId,
                buyerQuantities,
                StockMovementType.ConnectedPoReturnDispatch,
                actorId,
                utcNow,
                cancellationToken)
            .ConfigureAwait(false);
        await AdjustPendingReturnAsync(
                batch.SellerOrganizationId!,
                batch.SellerBranchId,
                QuantitiesByProduct(batch, seller: true),
                increase: true,
                utcNow,
                cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>Seller disposition after inspection: sellable restocks, damaged is written off.</summary>
    public async Task ApplySellerDispositionAsync(
        ReturnBatch batch,
        Guid actorId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        var sellerOrg = batch.SellerOrganizationId!;
        await AdjustPendingReturnAsync(
                sellerOrg,
                batch.SellerBranchId,
                QuantitiesByProduct(batch, seller: true),
                increase: false,
                utcNow,
                cancellationToken)
            .ConfigureAwait(false);

        var sellable = QuantitiesByProduct(batch, seller: true, l => l.SellableQuantity ?? 0m);
        var damaged = QuantitiesByProduct(batch, seller: true, l => l.DamagedQuantity ?? 0m);

        await ApplyOnHandDeltaAsync(
                batch,
                sellerOrg,
                batch.SellerBranchId,
                sellable,
                StockMovementType.ConnectedPoReturnRestock,
                actorId,
                utcNow,
                cancellationToken)
            .ConfigureAwait(false);
        await RecordWriteOffsAsync(batch, sellerOrg, damaged, actorId, utcNow, cancellationToken)
            .ConfigureAwait(false);
    }

    private static Dictionary<CatalogProductId, decimal> QuantitiesByProduct(
        ReturnBatch batch,
        bool seller,
        Func<ReturnBatchLine, decimal>? quantitySelector = null)
    {
        var selector = quantitySelector ?? (l => l.AcceptedQuantity);
        var result = new Dictionary<CatalogProductId, decimal>();
        foreach (var line in batch.Lines)
        {
            var productId = seller ? line.SupplierProductId : line.ProductId;
            if (productId is null)
            {
                continue;
            }

            var quantity = selector(line);
            if (quantity <= 0m)
            {
                continue;
            }

            result[productId] = result.TryGetValue(productId, out var existing)
                ? existing + quantity
                : quantity;
        }

        return result;
    }

    private async Task AdjustPendingReturnAsync(
        PosOrganizationId organizationId,
        PosBranchId? branchId,
        Dictionary<CatalogProductId, decimal> quantities,
        bool increase,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        if (quantities.Count == 0)
        {
            return;
        }

        var accounts = await _inventory
            .ListByProductIdsAsync(organizationId, quantities.Keys.ToList(), cancellationToken)
            .ConfigureAwait(false);
        var tracked = accounts.Where(a => a.IsTracked).ToDictionary(a => a.ProductId);

        foreach (var (productId, quantity) in quantities)
        {
            if (!tracked.TryGetValue(productId, out var account))
            {
                continue;
            }

            if (increase)
            {
                account.IncreasePendingReturn(quantity);
            }
            else
            {
                account.DecreasePendingReturn(quantity);
            }

            account.Touch(utcNow);
            await _inventory.UpdateAccountAsync(account, cancellationToken).ConfigureAwait(false);
        }

        if (_branchBalances is null || branchId is null)
        {
            return;
        }

        var balances = await _branchBalances
            .ListByBranchAndProductIdsAsync(organizationId, branchId, quantities.Keys.ToList(), cancellationToken)
            .ConfigureAwait(false);
        var balanceByProduct = balances.ToDictionary(b => b.ProductId);
        foreach (var (productId, quantity) in quantities)
        {
            if (!tracked.ContainsKey(productId))
            {
                continue;
            }

            if (!balanceByProduct.TryGetValue(productId, out var balance))
            {
                balance = InventoryBranchBalance.Create(organizationId, branchId, productId, 0m, utcNow);
            }

            if (increase)
            {
                balance.IncreasePendingReturn(quantity, utcNow);
            }
            else
            {
                balance.DecreasePendingReturn(quantity, utcNow);
            }

            await _branchBalances.UpsertAsync(balance, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task ApplyOnHandDeltaAsync(
        ReturnBatch batch,
        PosOrganizationId organizationId,
        PosBranchId? branchId,
        Dictionary<CatalogProductId, decimal> quantities,
        StockMovementType movementType,
        Guid actorId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        if (quantities.Count == 0)
        {
            return;
        }

        var accounts = await _inventory
            .ListByProductIdsAsync(organizationId, quantities.Keys.ToList(), cancellationToken)
            .ConfigureAwait(false);
        var tracked = accounts.Where(a => a.IsTracked).ToDictionary(a => a.ProductId);
        var uomByProduct = UomByProduct(batch);
        var sellingModes = await ResolveSellingModesAsync(organizationId, quantities.Keys.ToList(), cancellationToken)
            .ConfigureAwait(false);

        foreach (var (productId, quantity) in quantities)
        {
            if (!tracked.TryGetValue(productId, out var account))
            {
                continue;
            }

            var movement = StockMovement.ConnectedPoReturn(
                organizationId,
                productId,
                account.Id,
                movementType,
                quantity,
                uomByProduct.TryGetValue(productId, out var uom) ? uom : UnitOfMeasure.Piece,
                batch.Id.Value,
                actorId,
                utcNow,
                sellingMode: sellingModes.TryGetValue(productId, out var mode) ? mode : SellingMode.PerItem,
                branchId: branchId?.Value);

            var onHandBefore = account.OnHandQuantity;
            account.ApplyMovementEffect(movement.QuantityEffect);
            account.Touch(utcNow);
            await _inventory.UpdateAccountAsync(account, cancellationToken).ConfigureAwait(false);
            await _inventory.AddMovementAsync(movement, cancellationToken).ConfigureAwait(false);

            if (_branchBalances is not null && branchId is not null)
            {
                await BranchBalanceMutation
                    .ApplyAsync(
                        _branchBalances,
                        _branches,
                        organizationId,
                        branchId,
                        productId,
                        onHandBefore,
                        movement.QuantityEffect,
                        utcNow,
                        cancellationToken)
                    .ConfigureAwait(false);
            }
        }
    }

    private async Task RecordWriteOffsAsync(
        ReturnBatch batch,
        PosOrganizationId organizationId,
        Dictionary<CatalogProductId, decimal> quantities,
        Guid actorId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        if (quantities.Count == 0)
        {
            return;
        }

        var accounts = await _inventory
            .ListByProductIdsAsync(organizationId, quantities.Keys.ToList(), cancellationToken)
            .ConfigureAwait(false);
        var tracked = accounts.Where(a => a.IsTracked).ToDictionary(a => a.ProductId);
        var uomByProduct = UomByProduct(batch);
        var sellingModes = await ResolveSellingModesAsync(organizationId, quantities.Keys.ToList(), cancellationToken)
            .ConfigureAwait(false);

        foreach (var (productId, quantity) in quantities)
        {
            if (!tracked.TryGetValue(productId, out var account))
            {
                continue;
            }

            var movement = StockMovement.ConnectedPoReturn(
                organizationId,
                productId,
                account.Id,
                StockMovementType.ConnectedPoReturnWriteOff,
                quantity,
                uomByProduct.TryGetValue(productId, out var uom) ? uom : UnitOfMeasure.Piece,
                batch.Id.Value,
                actorId,
                utcNow,
                sellingMode: sellingModes.TryGetValue(productId, out var mode) ? mode : SellingMode.PerItem,
                branchId: batch.SellerBranchId?.Value);
            await _inventory.AddMovementAsync(movement, cancellationToken).ConfigureAwait(false);
        }
    }

    private static Dictionary<CatalogProductId, UnitOfMeasure> UomByProduct(ReturnBatch batch)
    {
        var result = new Dictionary<CatalogProductId, UnitOfMeasure>();
        foreach (var line in batch.Lines)
        {
            result[line.ProductId] = line.UomSnapshot;
            if (line.SupplierProductId is not null)
            {
                result[line.SupplierProductId] = line.UomSnapshot;
            }
        }

        return result;
    }

    private async Task<Dictionary<CatalogProductId, SellingMode>> ResolveSellingModesAsync(
        PosOrganizationId organizationId,
        IReadOnlyCollection<CatalogProductId> productIds,
        CancellationToken cancellationToken)
    {
        if (_products is null || productIds.Count == 0)
        {
            return [];
        }

        var products = await _products
            .ListByIdsAsync(organizationId, productIds, cancellationToken)
            .ConfigureAwait(false);
        return products.ToDictionary(p => p.Id, p => p.SellingMode);
    }
}

/// <summary>
/// Read model for the buyer "Return items" flow and the seller returns inbox.
/// </summary>
public sealed class ConnectedPoReturnQueryService
{
    private readonly IReturnBatchRepository _batches;
    private readonly IPurchaseOrderRepository _orders;
    private readonly IConnectedPurchaseOrderRepository _connectedOrders;
    private readonly ISupplierPayableRepository? _payables;
    private readonly IConnectedPoReturnEligibilityBucketRepository? _eligibilityBuckets;
    private readonly TimeProvider _clock;

    public ConnectedPoReturnQueryService(
        IReturnBatchRepository batches,
        IPurchaseOrderRepository orders,
        IConnectedPurchaseOrderRepository connectedOrders,
        ISupplierPayableRepository? payables = null,
        IConnectedPoReturnEligibilityBucketRepository? eligibilityBuckets = null,
        TimeProvider? clock = null)
    {
        _batches = batches;
        _orders = orders;
        _connectedOrders = connectedOrders;
        _payables = payables;
        _eligibilityBuckets = eligibilityBuckets;
        _clock = clock ?? TimeProvider.System;
    }

    public async Task<ConnectedPoReturnEligibilityDto?> GetEligibilityAsync(
        Guid buyerOrganizationId,
        Guid purchaseOrderId,
        CancellationToken cancellationToken = default)
    {
        var orgId = PosOrganizationId.From(buyerOrganizationId);
        var poId = PurchaseOrderId.From(purchaseOrderId);
        var po = await _orders.GetByIdAsync(orgId, poId, cancellationToken).ConfigureAwait(false);
        if (po is null)
        {
            return null;
        }

        var connected = await _connectedOrders
            .GetByBuyerPurchaseOrderAsync(poId, cancellationToken)
            .ConfigureAwait(false);
        var existing = await _batches
            .ListByPurchaseOrderIdAsync(orgId, poId, cancellationToken)
            .ConfigureAwait(false);
        var priorByLine = SumReturnedByPurchaseOrderLine(existing);
        var utcNow = _clock.GetUtcNow();

        var buckets = _eligibilityBuckets is null
            ? []
            : await _eligibilityBuckets
                .ListByPurchaseOrderAsync(orgId, poId, cancellationToken)
                .ConfigureAwait(false);
        var bucketsByLine = buckets
            .GroupBy(b => b.PurchaseOrderLineId.Value)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<ConnectedPoReturnEligibilityBucket>)g.ToList());

        var blockedReason = connected is null
            ? "not_connected"
            : po.Status != PurchaseOrderStatus.Received
                ? "not_completed"
                : po.FinancialSettlementStatus == ConnectedPoFinancialSettlementStatus.AwaitingPayment
                    ? "awaiting_payment"
                    : null;

        var lines = po.Lines
            .Where(l => l.ReceivedQty > 0m)
            .OrderBy(l => l.LineNumber)
            .Select(l =>
            {
                priorByLine.TryGetValue(l.Id.Value, out var prior);
                bucketsByLine.TryGetValue(l.Id.Value, out var lineBuckets);
                lineBuckets ??= Array.Empty<ConnectedPoReturnEligibilityBucket>();

                var eligibleRemaining = lineBuckets
                    .Where(b => b.IsVoluntarilyEligibleAt(utcNow))
                    .Sum(b => b.RemainingQuantity);
                // Secondary over-return guard: never exceed ReceivedQty - prior across all batches.
                var qtyCap = Math.Max(0m, l.ReceivedQty - prior);
                var returnable = Math.Min(eligibleRemaining, qtyCap);

                var anyNonReturnable = lineBuckets.Count > 0
                    && lineBuckets.All(b => !b.PolicyReturnsAllowed);
                var anyExpired = lineBuckets.Count > 0
                    && lineBuckets.All(b =>
                        b.PolicyReturnsAllowed
                        && b.ReturnExpiresAtUtc is not null
                        && utcNow > b.ReturnExpiresAtUtc.Value);
                string? lineBlocked = null;
                if (returnable <= 0m)
                {
                    if (anyNonReturnable)
                    {
                        lineBlocked = "non_returnable";
                    }
                    else if (anyExpired)
                    {
                        lineBlocked = "window_expired";
                    }
                    else if (qtyCap <= 0m)
                    {
                        lineBlocked = "already_returned";
                    }
                    else if (lineBuckets.Count == 0)
                    {
                        // Legacy/missing buckets: fall back to qty-only compatibility.
                        returnable = qtyCap;
                    }
                    else
                    {
                        lineBlocked = "nothing_returnable";
                    }
                }

                var eligibleBuckets = lineBuckets
                    .Select(b => new ConnectedPoReturnEligibilityBucketDto(
                        b.Id.Value,
                        b.GoodsReceiptId.Value,
                        b.GoodsReceiptLineId.Value,
                        b.QuantityReceived,
                        b.QuantityAllocated,
                        b.RemainingQuantity,
                        b.ReceivedAtUtc,
                        b.ReturnExpiresAtUtc,
                        b.PolicyReturnsAllowed,
                        b.PolicyReturnWindowDays,
                        b.PolicySource.ToString(),
                        b.IsVoluntarilyEligibleAt(utcNow)))
                    .ToList();

                var policySource = lineBuckets.FirstOrDefault()?.PolicySource.ToString();
                var windowDays = lineBuckets.FirstOrDefault()?.PolicyReturnWindowDays;
                var returnsAllowed = lineBuckets.Count == 0 || lineBuckets.Any(b => b.PolicyReturnsAllowed);
                DateTimeOffset? earliest = null;
                DateTimeOffset? latest = null;
                foreach (var b in lineBuckets.Where(x => x.IsVoluntarilyEligibleAt(utcNow)))
                {
                    if (b.ReturnExpiresAtUtc is null)
                    {
                        continue;
                    }

                    earliest = earliest is null || b.ReturnExpiresAtUtc < earliest
                        ? b.ReturnExpiresAtUtc
                        : earliest;
                    latest = latest is null || b.ReturnExpiresAtUtc > latest
                        ? b.ReturnExpiresAtUtc
                        : latest;
                }

                return new ConnectedPoReturnableLineDto(
                    l.Id.Value,
                    l.ProductId?.Value,
                    l.SupplierProductId?.Value,
                    l.NameSnapshot ?? string.Empty,
                    l.UomSnapshot is null ? string.Empty : UnitOfMeasures.ToCode(l.UomSnapshot.Value),
                    l.UnitPurchaseCost,
                    l.ReceivedQty,
                    prior,
                    returnable,
                    returnsAllowed,
                    windowDays,
                    earliest,
                    latest,
                    policySource,
                    lineBlocked,
                    eligibleBuckets);
            })
            .ToList();

        var payables = await LoadPayablesAsync(orgId, poId, cancellationToken).ConfigureAwait(false);
        return new ConnectedPoReturnEligibilityDto(
            po.Id.Value,
            connected?.Id.Value,
            po.PoNumber,
            po.Status.ToString(),
            blockedReason is null && lines.Any(l => l.ReturnableQuantity > 0m),
            blockedReason ?? (lines.Any(l => l.ReturnableQuantity > 0m) ? null : "nothing_returnable"),
            po.OrganizationId.Value,
            connected?.SupplierOrganizationId.Value,
            (connected?.EffectivePaymentTiming ?? po.PaymentTiming).ToString(),
            ConnectedPoReturnSettlementResolver.ResolveGoodReceivedValue(po),
            ConnectedPoReturnSettlementResolver.ResolveSettledPayments(po, payables),
            lines,
            existing.Select(b => ReturnBatchQueryService.Map(b, BuildSummary(b, po, existing))).ToList());
    }

    public async Task<IReadOnlyList<ReturnBatchDto>> ListSellerInboxAsync(
        Guid sellerOrganizationId,
        CancellationToken cancellationToken = default)
    {
        var items = await _batches
            .ListOpenConnectedForSellerAsync(PosOrganizationId.From(sellerOrganizationId), cancellationToken)
            .ConfigureAwait(false);
        return items.Select(ReturnBatchQueryService.Map).ToList();
    }

    public async Task<IReadOnlyList<ReturnBatchDto>> ListByPurchaseOrderAsync(
        Guid organizationId,
        Guid purchaseOrderId,
        CancellationToken cancellationToken = default)
    {
        var items = await _batches
            .ListByPurchaseOrderIdAsync(
                PosOrganizationId.From(organizationId),
                PurchaseOrderId.From(purchaseOrderId),
                cancellationToken)
            .ConfigureAwait(false);
        return items.Select(ReturnBatchQueryService.Map).ToList();
    }

    internal static Dictionary<Guid, decimal> SumReturnedByPurchaseOrderLine(IReadOnlyList<ReturnBatch> batches)
    {
        var result = new Dictionary<Guid, decimal>();
        foreach (var line in batches.SelectMany(b => b.Lines))
        {
            if (line.PurchaseOrderLineId is null)
            {
                continue;
            }

            var key = line.PurchaseOrderLineId.Value.Value;
            result[key] = result.TryGetValue(key, out var existing)
                ? existing + line.AcceptedQuantity
                : line.AcceptedQuantity;
        }

        return result;
    }

    internal async Task<IReadOnlyList<SupplierPayable>> LoadPayablesAsync(
        PosOrganizationId buyerOrganizationId,
        PurchaseOrderId purchaseOrderId,
        CancellationToken cancellationToken)
    {
        if (_payables is null)
        {
            return [];
        }

        var receipts = await _orders
            .ListGoodsReceiptsForPurchaseOrderAsync(buyerOrganizationId, purchaseOrderId, cancellationToken)
            .ConfigureAwait(false);
        var rows = new List<SupplierPayable>();
        foreach (var posted in receipts.Where(r => r.Status == GoodsReceiptStatus.Posted))
        {
            var payable = await _payables
                .FindBySourceAsync(
                    buyerOrganizationId,
                    SupplierPayableSourceType.GoodsReceipt,
                    posted.Id.Value,
                    cancellationToken)
                .ConfigureAwait(false);
            if (payable is not null)
            {
                rows.Add(payable);
            }
        }

        return rows;
    }

    internal static ReturnBatchFinancialSummaryDto BuildSummary(
        ReturnBatch batch,
        PurchaseOrder purchaseOrder,
        IReadOnlyList<ReturnBatch> allBatches,
        decimal settledPayments = 0m)
    {
        var priorFinalized = allBatches
            .Where(b => b.Id != batch.Id && b.Status == ReturnBatchStatus.Finalized)
            .ToList();
        var settlement = ConnectedPoReturnSettlementResolver.Evaluate(
            ConnectedPoReturnSettlementResolver.ResolveGoodReceivedValue(purchaseOrder),
            settledPayments,
            batch.AcceptedReturnValue,
            priorFinalized.Sum(b => b.AcceptedReturnValue),
            priorFinalized.Sum(b => b.RefundedAmount));
        var totals = batch.Lines.Aggregate(
            (Returned: 0m, Sellable: 0m, Damaged: 0m),
            (acc, line) => (
                acc.Returned + line.AcceptedQuantity,
                acc.Sellable + (line.SellableQuantity ?? 0m),
                acc.Damaged + (line.DamagedQuantity ?? 0m)));

        return new ReturnBatchFinancialSummaryDto(
            ConnectedPoReturnSettlementResolver.ResolveGoodReceivedValue(purchaseOrder),
            batch.AcceptedReturnValue,
            settledPayments,
            settlement.ObligationReducedAmount,
            settlement.RemainingDue,
            batch.RefundDueAmount,
            batch.RefundedAmount,
            Math.Max(0m, batch.RefundDueAmount - batch.RefundedAmount),
            totals.Returned,
            totals.Sellable,
            totals.Damaged);
    }
}

/// <summary>
/// Buyer requests a return against a completed connected PO. The resulting batch is owned by the
/// seller organization and starts awaiting seller physical receipt.
/// </summary>
public sealed class RequestConnectedPoReturnBatch
{
    private readonly IReturnBatchRepository _batches;
    private readonly IPurchaseOrderRepository _orders;
    private readonly IConnectedPurchaseOrderRepository _connectedOrders;
    private readonly ConnectedPoReturnInventoryService _inventory;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IConnectedPoReturnEligibilityBucketRepository? _eligibilityBuckets;

    public RequestConnectedPoReturnBatch(
        IReturnBatchRepository batches,
        IPurchaseOrderRepository orders,
        IConnectedPurchaseOrderRepository connectedOrders,
        ConnectedPoReturnInventoryService inventory,
        IPosUnitOfWork unitOfWork,
        IClock clock,
        IConnectedPoReturnEligibilityBucketRepository? eligibilityBuckets = null)
    {
        _batches = batches;
        _orders = orders;
        _connectedOrders = connectedOrders;
        _inventory = inventory;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _eligibilityBuckets = eligibilityBuckets;
    }

    public async Task<ApplicationResult<ReturnBatch>> ExecuteAsync(
        Guid buyerOrganizationId,
        Guid purchaseOrderId,
        string reason,
        IReadOnlyList<RequestConnectedPoReturnLineRequest>? lines,
        Guid actorId,
        string? notes = null,
        Guid? clientReturnBatchId = null,
        CancellationToken cancellationToken = default)
    {
        if (actorId == Guid.Empty)
        {
            return ApplicationResult<ReturnBatch>.Failure(
                ApplicationErrorCodes.ActorRequired,
                "An actor identifier is required to request a return.");
        }

        if (lines is null || lines.Count == 0)
        {
            return ApplicationResult<ReturnBatch>.Failure(
                DomainErrorCodes.ReturnBatchRequiresAtLeastOneLine,
                "A return batch must contain at least one line.");
        }

        var buyerOrg = PosOrganizationId.From(buyerOrganizationId);
        var poId = PurchaseOrderId.From(purchaseOrderId);
        var drafts = lines
            .Select(l => new ReturnBatchConnectedPoLineDraft(
                PurchaseOrderLineId.From(l.PurchaseOrderLineId),
                l.Quantity))
            .ToList();

        try
        {
            return await _unitOfWork.ExecuteInSerializableTransactionAsync(async ct =>
            {
                if (clientReturnBatchId is not null)
                {
                    var existingBatch = await _batches
                        .GetByIdAsync(buyerOrg, ReturnBatchId.From(clientReturnBatchId.Value), ct)
                        .ConfigureAwait(false);
                    if (existingBatch is not null)
                    {
                        return ApplicationResult<ReturnBatch>.Success(existingBatch);
                    }
                }

                var po = await _orders.GetByIdAsync(buyerOrg, poId, ct).ConfigureAwait(false);
                if (po is null)
                {
                    return ApplicationResult<ReturnBatch>.Failure(
                        ApplicationErrorCodes.PurchaseOrderNotFound,
                        "Purchase order was not found.");
                }

                var connected = await _connectedOrders
                    .GetByBuyerPurchaseOrderAsync(poId, ct)
                    .ConfigureAwait(false);
                if (connected is null)
                {
                    return ApplicationResult<ReturnBatch>.Failure(
                        DomainErrorCodes.ReturnBatchSourceMismatch,
                        "Only connected supplier purchase orders support supplier returns.");
                }

                var prior = ConnectedPoReturnQueryService.SumReturnedByPurchaseOrderLine(
                    await _batches.ListByPurchaseOrderIdAsync(buyerOrg, poId, ct).ConfigureAwait(false));
                var utcNow = _clock.UtcNow;
                var sellerOrg = connected.SupplierOrganizationId;

                var plannedAllocations =
                    new List<(PurchaseOrderLineId PoLineId, IReadOnlyList<ConnectedPoReturnBucketAllocator.AllocationSlice> Slices)>();
                if (_eligibilityBuckets is not null)
                {
                    var allBuckets = await _eligibilityBuckets
                        .ListByPurchaseOrderAsync(buyerOrg, poId, ct)
                        .ConfigureAwait(false);
                    foreach (var draft in drafts)
                    {
                        var lineBuckets = allBuckets
                            .Where(b => b.PurchaseOrderLineId == draft.PurchaseOrderLineId)
                            .ToList();
                        if (lineBuckets.Count == 0)
                        {
                            // Compatibility: no buckets yet (legacy GRN). Qty guard via prior remains.
                            continue;
                        }

                        if (lineBuckets.All(b => !b.PolicyReturnsAllowed))
                        {
                            return ApplicationResult<ReturnBatch>.Failure(
                                DomainErrorCodes.ConnectedPoReturnNotAllowed,
                                "This product is non-returnable under the supplier return policy.");
                        }

                        var slices = ConnectedPoReturnBucketAllocator.AllocateFifo(
                            lineBuckets,
                            draft.AcceptedQuantity,
                            utcNow);
                        plannedAllocations.Add((draft.PurchaseOrderLineId, slices));
                    }
                }

                var created = await _batches
                    .CreateAsync(
                        sellerOrg,
                        ReturnBatchNumbers.BusinessDateOf(utcNow),
                        number => ReturnBatch.CreateAcceptedForConnectedPurchaseOrder(
                            sellerOrg,
                            buyerOrg,
                            number,
                            po,
                            drafts,
                            prior,
                            reason,
                            actorId,
                            utcNow,
                            connected.Id,
                            connected.EffectivePaymentTiming,
                            buyerBranchId: po.IntendedReceivingBranchId is { } branch
                                ? PosBranchId.From(branch)
                                : null,
                            sellerBranchId: po.SupplierBranchId is { } supplierBranch
                                ? PosBranchId.From(supplierBranch)
                                : null,
                            notes,
                            clientReturnBatchId is null ? null : ReturnBatchId.From(clientReturnBatchId.Value)),
                        (batch, afterCt) => _inventory.IncreaseBuyerPendingReturnAsync(batch, utcNow, afterCt),
                        ct)
                    .ConfigureAwait(false);

                if (_eligibilityBuckets is not null && plannedAllocations.Count > 0)
                {
                    foreach (var (poLineId, slices) in plannedAllocations)
                    {
                        var batchLine = created.Lines.First(l => l.PurchaseOrderLineId == poLineId);
                        foreach (var slice in slices)
                        {
                            slice.Bucket.Allocate(slice.Quantity);
                            await _eligibilityBuckets.UpdateAsync(slice.Bucket, ct).ConfigureAwait(false);
                            await _eligibilityBuckets
                                .AddAllocationAsync(
                                    ConnectedPoReturnAllocation.Create(
                                        batchLine.Id,
                                        slice.Bucket.Id,
                                        slice.Quantity,
                                        utcNow),
                                    ct)
                                .ConfigureAwait(false);
                        }
                    }
                }

                await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
                return ApplicationResult<ReturnBatch>.Success(created);
            }, cancellationToken).ConfigureAwait(false);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<ReturnBatch>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<ReturnBatch>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

/// <summary>Seller confirms physical receipt so inspection can start.</summary>
public sealed class ReceiveConnectedPoReturnBatch
{
    private readonly IReturnBatchRepository _batches;
    private readonly ConnectedPoReturnInventoryService _inventory;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public ReceiveConnectedPoReturnBatch(
        IReturnBatchRepository batches,
        ConnectedPoReturnInventoryService inventory,
        IPosUnitOfWork unitOfWork,
        IClock clock)
    {
        _batches = batches;
        _inventory = inventory;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<ReturnBatch>> ExecuteAsync(
        Guid sellerOrganizationId,
        Guid returnBatchId,
        Guid actorId,
        DateTimeOffset? expectedUpdatedAtUtc = null,
        CancellationToken cancellationToken = default)
    {
        var sellerOrg = PosOrganizationId.From(sellerOrganizationId);
        var batchId = ReturnBatchId.From(returnBatchId);
        try
        {
            return await _unitOfWork.ExecuteInSerializableTransactionAsync(async ct =>
            {
                var batch = await _batches.GetByIdAsync(sellerOrg, batchId, ct).ConfigureAwait(false);
                if (batch is null)
                {
                    return ApplicationResult<ReturnBatch>.Failure(
                        ApplicationErrorCodes.ReturnBatchNotFound,
                        "Return batch was not found.");
                }

                if (!batch.IsConnectedPurchaseOrderReturn || batch.SellerOrganizationId != sellerOrg)
                {
                    return ApplicationResult<ReturnBatch>.Failure(
                        DomainErrorCodes.ReturnBatchSourceMismatch,
                        "Only the seller organization can receive this return.");
                }

                if (ReturnBatchConcurrency.IsMismatch(batch.UpdatedAtUtc, expectedUpdatedAtUtc))
                {
                    return ApplicationResult<ReturnBatch>.Failure(
                        ApplicationErrorCodes.ConcurrencyConflict,
                        "Return batch has changed. Reload before receiving.");
                }

                var utcNow = _clock.UtcNow;
                if (!batch.MarkReceivedBySeller(actorId, utcNow))
                {
                    return ApplicationResult<ReturnBatch>.Success(batch);
                }

                await _inventory.ApplySellerReceiptAsync(batch, actorId, utcNow, ct).ConfigureAwait(false);
                await _batches.UpdateAsync(batch, ct).ConfigureAwait(false);
                await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
                return ApplicationResult<ReturnBatch>.Success(batch);
            }, cancellationToken).ConfigureAwait(false);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<ReturnBatch>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<ReturnBatch>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

/// <summary>
/// Seller finalizes a classified connected-PO return: applies seller disposition and resolves
/// settlement against the buyer's good-received value and settled payments.
/// </summary>
public sealed class FinalizeConnectedPoReturnBatch
{
    private readonly IReturnBatchRepository _batches;
    private readonly IPurchaseOrderRepository _orders;
    private readonly ConnectedPoReturnQueryService _queries;
    private readonly ConnectedPoReturnInventoryService _inventory;
    private readonly ISupplierPayableRepository? _payables;
    private readonly IBusinessCreditEntryRepository? _businessCredits;
    private readonly IConnectedPurchaseOrderRepository? _connectedOrders;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public FinalizeConnectedPoReturnBatch(
        IReturnBatchRepository batches,
        IPurchaseOrderRepository orders,
        ConnectedPoReturnQueryService queries,
        ConnectedPoReturnInventoryService inventory,
        IPosUnitOfWork unitOfWork,
        IClock clock,
        ISupplierPayableRepository? payables = null,
        IBusinessCreditEntryRepository? businessCredits = null,
        IConnectedPurchaseOrderRepository? connectedOrders = null)
    {
        _batches = batches;
        _orders = orders;
        _queries = queries;
        _inventory = inventory;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _payables = payables;
        _businessCredits = businessCredits;
        _connectedOrders = connectedOrders;
    }

    public async Task<ApplicationResult<ReturnBatch>> ExecuteAsync(
        Guid sellerOrganizationId,
        Guid returnBatchId,
        DateTimeOffset expectedUpdatedAtUtc,
        Guid actorId,
        CancellationToken cancellationToken = default)
    {
        var sellerOrg = PosOrganizationId.From(sellerOrganizationId);
        var batchId = ReturnBatchId.From(returnBatchId);
        try
        {
            return await _unitOfWork.ExecuteInSerializableTransactionAsync(async ct =>
            {
                var batch = await _batches.GetByIdAsync(sellerOrg, batchId, ct).ConfigureAwait(false);
                if (batch is null)
                {
                    return ApplicationResult<ReturnBatch>.Failure(
                        ApplicationErrorCodes.ReturnBatchNotFound,
                        "Return batch was not found.");
                }

                if (batch.Status == ReturnBatchStatus.Finalized)
                {
                    return ApplicationResult<ReturnBatch>.Success(batch);
                }

                if (!batch.IsConnectedPurchaseOrderReturn || batch.SellerOrganizationId != sellerOrg)
                {
                    return ApplicationResult<ReturnBatch>.Failure(
                        DomainErrorCodes.ReturnBatchSourceMismatch,
                        "Only the seller organization can finalize this return.");
                }

                if (ReturnBatchConcurrency.IsMismatch(batch.UpdatedAtUtc, expectedUpdatedAtUtc))
                {
                    return ApplicationResult<ReturnBatch>.Failure(
                        ApplicationErrorCodes.ConcurrencyConflict,
                        "Return batch has changed. Reload before finalizing.");
                }

                if (!batch.AllLinesClassified)
                {
                    return ApplicationResult<ReturnBatch>.Failure(
                        DomainErrorCodes.ReturnBatchNotReadyForFinalize,
                        "All lines must be classified before finalizing.");
                }

                var buyerOrg = batch.BuyerOrganizationId!;
                var poId = batch.PurchaseOrderId!.Value;
                var po = await _orders.GetByIdAsync(buyerOrg, poId, ct).ConfigureAwait(false);
                if (po is null)
                {
                    return ApplicationResult<ReturnBatch>.Failure(
                        ApplicationErrorCodes.PurchaseOrderNotFound,
                        "Originating purchase order was not found.");
                }

                var payables = await _queries.LoadPayablesAsync(buyerOrg, poId, ct).ConfigureAwait(false);
                var settled = ConnectedPoReturnSettlementResolver.ResolveSettledPayments(po, payables);
                var allBatches = await _batches
                    .ListByPurchaseOrderIdAsync(sellerOrg, poId, ct)
                    .ConfigureAwait(false);
                var priorFinalized = allBatches
                    .Where(b => b.Id != batch.Id && b.Status == ReturnBatchStatus.Finalized)
                    .ToList();
                var settlement = ConnectedPoReturnSettlementResolver.Evaluate(
                    ConnectedPoReturnSettlementResolver.ResolveGoodReceivedValue(po),
                    settled,
                    batch.AcceptedReturnValue,
                    priorFinalized.Sum(b => b.AcceptedReturnValue),
                    priorFinalized.Sum(b => b.RefundedAmount));
                var refundStatus = ConnectedPoReturnSettlementResolver.ResolveRefundStatus(
                    batch.PaymentTimingSnapshot ?? po.PaymentTiming,
                    po.PaymentTerm,
                    settlement);

                var utcNow = _clock.UtcNow;
                await _inventory.ApplySellerDispositionAsync(batch, actorId, utcNow, ct).ConfigureAwait(false);

                if (settlement.ObligationReducedAmount > 0m
                    || refundStatus is ReturnBatchRefundStatus.CreditReduced or ReturnBatchRefundStatus.ObligationReduced)
                {
                    await ApplyObligationAndCreditReductionAsync(
                            batch,
                            po,
                            payables,
                            settlement.ObligationReducedAmount > 0m
                                ? settlement.ObligationReducedAmount
                                : batch.AcceptedReturnValue,
                            utcNow,
                            ct)
                        .ConfigureAwait(false);
                }

                batch.MarkFinalized(
                    saleReturnId: null,
                    refundStatus,
                    refundStatus == ReturnBatchRefundStatus.RefundDue ? settlement.RefundDue : 0m,
                    refundedAmount: 0m,
                    actorId,
                    utcNow);

                await _batches.UpdateAsync(batch, ct).ConfigureAwait(false);
                await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
                return ApplicationResult<ReturnBatch>.Success(batch);
            }, cancellationToken).ConfigureAwait(false);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<ReturnBatch>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<ReturnBatch>.Failure(ex.ErrorCode, ex.Message);
        }
    }

    private async Task ApplyObligationAndCreditReductionAsync(
        ReturnBatch batch,
        PurchaseOrder po,
        IReadOnlyList<SupplierPayable> payables,
        decimal reductionAmount,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        var remaining = SaleMoney.RoundMoney(Math.Max(0m, reductionAmount));
        if (remaining <= 0m)
        {
            return;
        }

        var appliedToPayables = 0m;
        if (_payables is not null)
        {
            foreach (var payable in payables
                .Where(p => p.Status is not (SupplierPayableStatus.Voided or SupplierPayableStatus.Paid))
                .OrderBy(p => p.CreatedAtUtc))
            {
                if (remaining <= 0m)
                {
                    break;
                }

                var applied = payable.ReduceOutstandingForReturn(remaining, utcNow);
                if (applied <= 0m)
                {
                    continue;
                }

                appliedToPayables = SaleMoney.RoundMoney(appliedToPayables + applied);
                remaining = SaleMoney.RoundMoney(remaining - applied);
                await _payables.UpdateAsync(payable, cancellationToken).ConfigureAwait(false);
            }
        }

        var creditReduce = SaleMoney.RoundMoney(Math.Max(appliedToPayables, reductionAmount - remaining));
        if (creditReduce <= 0m)
        {
            creditReduce = SaleMoney.RoundMoney(reductionAmount);
        }

        if (_connectedOrders is not null && batch.ConnectedPurchaseOrderId is ConnectedPurchaseOrderId cpoId)
        {
            var connected = await _connectedOrders.GetAsync(cpoId, cancellationToken).ConfigureAwait(false);
            if (connected is not null)
            {
                connected.UnpostUtangCreditFromReceipt(creditReduce, utcNow);
                await _connectedOrders.UpdateAsync(connected, cancellationToken).ConfigureAwait(false);
            }
        }

        if (_businessCredits is null || batch.SellerOrganizationId is null || batch.BuyerOrganizationId is null)
        {
            return;
        }

        var credits = await _businessCredits
            .ListChronologicalForBuyerAsync(
                batch.SellerOrganizationId,
                batch.BuyerOrganizationId,
                cancellationToken)
            .ConfigureAwait(false);
        var leftover = creditReduce;
        var poIdToken = po.Id.Value.ToString("D");
        var matched = credits
            .Where(c => c.Status == CreditEntryStatus.Active)
            .Where(c =>
                (!string.IsNullOrWhiteSpace(po.PoNumber)
                    && c.Remarks.Contains(po.PoNumber, StringComparison.OrdinalIgnoreCase))
                || c.Remarks.Contains(poIdToken, StringComparison.OrdinalIgnoreCase))
            .ToList();
        var fallback = credits
            .Where(c => c.Status == CreditEntryStatus.Active)
            .Where(c => matched.All(m => m.Id != c.Id))
            .ToList();

        foreach (var credit in matched.Concat(fallback))
        {
            if (leftover <= 0m)
            {
                break;
            }

            var apply = leftover > credit.Amount ? credit.Amount : leftover;
            credit.ReduceForConnectedPoReturn(apply, utcNow);
            leftover = SaleMoney.RoundMoney(leftover - apply);
            await _businessCredits.UpdateAsync(credit, cancellationToken).ConfigureAwait(false);
        }
    }
}
