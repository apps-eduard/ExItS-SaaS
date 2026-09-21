using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Application.Purchasing;
using ExItS.PinoyBusinessPOS.Application.Returns;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Purchasing;
using ExItS.PinoyBusinessPOS.Domain.Returns;

namespace ExItS.PinoyBusinessPOS.Application.ConnectedSuppliers;

public sealed record ConnectedPoReceivingIssueLineDto(
    Guid ReceivingIssueLineId,
    Guid GoodsReceiptLineId,
    Guid PurchaseOrderLineId,
    Guid SupplierProductId,
    Guid? BuyerProductId,
    string NameSnapshot,
    string UomSnapshot,
    Guid FulfillmentSourceId,
    decimal ShippedQty,
    decimal GoodQty,
    decimal DamagedQty,
    decimal MissingQty,
    string LineKind,
    string BuyerDiscrepancyKind,
    string? BuyerDiscrepancyNote,
    string? MissingResolution,
    string? DamagedResolution,
    decimal ResolutionQty,
    string? SellerNote,
    Guid? InventoryMovementId,
    Guid? ReturnBatchId,
    DateTimeOffset? ResolvedAtUtc,
    Guid? ResolvedByUserId,
    bool IsResolved,
    string InventoryEffectPreview);

public sealed record ConnectedPoReceivingIssueDto(
    Guid ReceivingIssueId,
    Guid ConnectedPurchaseOrderId,
    Guid PurchaseOrderId,
    Guid GoodsReceiptId,
    Guid BuyerOrganizationId,
    Guid SellerOrganizationId,
    Guid FulfillmentSourceId,
    string Status,
    DateTimeOffset CreatedAtUtc,
    Guid CreatedByUserId,
    DateTimeOffset? ResolvedAtUtc,
    Guid? ResolvedByUserId,
    string? SellerNotes,
    int UnresolvedLineCount,
    IReadOnlyList<ConnectedPoReceivingIssueLineDto> Lines);

public sealed record ResolveConnectedPoReceivingIssueLineRequest(
    Guid ReceivingIssueLineId,
    string? MissingResolution = null,
    string? DamagedResolution = null,
    decimal? ResolutionQty = null,
    string? SellerNote = null);

public sealed record ResolveConnectedPoReceivingIssueRequest(
    IReadOnlyList<ResolveConnectedPoReceivingIssueLineRequest> Lines,
    string? SellerNotes = null);

public static class ConnectedPoReceivingIssueMapper
{
    public static ConnectedPoReceivingIssueDto Map(ConnectedPoReceivingIssue issue) =>
        new(
            issue.Id.Value,
            issue.ConnectedPurchaseOrderId.Value,
            issue.PurchaseOrderId.Value,
            issue.GoodsReceiptId.Value,
            issue.BuyerOrganizationId.Value,
            issue.SellerOrganizationId.Value,
            issue.FulfillmentSourceId,
            ConnectedPoReceivingIssueStatuses.ToCode(issue.Status),
            issue.CreatedAtUtc,
            issue.CreatedByUserId,
            issue.ResolvedAtUtc,
            issue.ResolvedByUserId,
            issue.SellerNotes,
            issue.UnresolvedLineCount,
            issue.Lines.Select(MapLine).ToList());

    public static ConnectedPoReceivingIssueLineDto MapLine(ConnectedPoReceivingIssueLine line) =>
        new(
            line.Id.Value,
            line.GoodsReceiptLineId.Value,
            line.PurchaseOrderLineId.Value,
            line.SupplierProductId.Value,
            line.BuyerProductId?.Value,
            line.NameSnapshot,
            line.UomSnapshot,
            line.FulfillmentSourceId,
            line.ShippedQty,
            line.GoodQty,
            line.DamagedQty,
            line.MissingQty,
            ConnectedPoReceivingIssueLineKinds.ToCode(line.LineKind),
            line.BuyerDiscrepancyKind.ToString(),
            line.BuyerDiscrepancyNote,
            line.MissingResolution is { } mr ? ConnectedPoMissingResolutions.ToCode(mr) : null,
            line.DamagedResolution is { } dr ? ConnectedPoDamagedResolutions.ToCode(dr) : null,
            line.ResolutionQty,
            line.SellerNote,
            line.InventoryMovementId,
            line.ReturnBatchId,
            line.ResolvedAtUtc,
            line.ResolvedByUserId,
            line.IsResolved,
            PreviewInventoryEffect(line));

    public static string PreviewInventoryEffect(ConnectedPoReceivingIssueLine line)
    {
        if (line.IsResolved)
        {
            return line.InventoryMovementId is null
                ? "No stock change."
                : $"Seller stock restored (+{FormatQty(line.ResolutionQty)}).";
        }

        return "Select a resolution to preview inventory effect.";
    }

    public static string PreviewInventoryEffectForMissing(ConnectedPoMissingResolution resolution, decimal qty) =>
        ConnectedPoMissingResolutions.RestoresSellerStock(resolution)
            ? $"Inventory adjustment: +{FormatQty(qty)} will be returned to seller stock."
            : "Inventory adjustment: No stock change.";

    public static string PreviewInventoryEffectForDamaged(ConnectedPoDamagedResolution resolution) =>
        resolution == ConnectedPoDamagedResolution.ReturnRequested
            ? "Inventory adjustment: No immediate stock change. Physical return uses the connected PO return workflow."
            : "Inventory adjustment: No stock change.";

    private static string FormatQty(decimal value) =>
        value == decimal.Truncate(value)
            ? decimal.Truncate(value).ToString(System.Globalization.CultureInfo.InvariantCulture)
            : value.ToString("0.####", System.Globalization.CultureInfo.InvariantCulture);
}

/// <summary>
/// Creates a seller-review receiving issue from a posted connected-PO goods receipt when
/// damaged or missing quantities are reported. Idempotent per goods receipt.
/// </summary>
public static class ConnectedPoReceivingIssueFactory
{
    public static ConnectedPoReceivingIssue? TryCreateFromReceipt(
        ConnectedPurchaseOrder connected,
        GoodsReceipt receipt,
        PurchaseOrder buyerPo,
        Guid fulfillmentSourceId,
        Guid actorId,
        DateTimeOffset utcNow,
        ConnectedIncomingOrderFulfillmentProjection.ProductLinkMaps? productLinks = null)
    {
        ArgumentNullException.ThrowIfNull(connected);
        ArgumentNullException.ThrowIfNull(receipt);
        ArgumentNullException.ThrowIfNull(buyerPo);

        var links = productLinks ?? ConnectedIncomingOrderFulfillmentProjection.ProductLinkMaps.Empty;
        var drafts = new List<ConnectedPoReceivingIssueLineDraft>();

        foreach (var grnLine in receipt.Lines)
        {
            if (grnLine.DamagedQty <= 0m && grnLine.RejectedQty <= 0m)
            {
                continue;
            }

            var supplierProductId = ResolveSupplierProductId(grnLine, buyerPo, connected, links);
            var cpoLine = connected.Lines.FirstOrDefault(l => l.ProductId == supplierProductId);
            var shipped = Math.Max(
                grnLine.QuantityReceived + grnLine.DamagedQty + grnLine.RejectedQty,
                cpoLine?.FulfillmentQty ?? 0m);
            var name = grnLine.NameSnapshot;
            var uom = UnitOfMeasures.ToCode(grnLine.UomSnapshot);

            if (grnLine.RejectedQty > 0m)
            {
                drafts.Add(new ConnectedPoReceivingIssueLineDraft(
                    grnLine.Id,
                    grnLine.PurchaseOrderLineId,
                    supplierProductId,
                    grnLine.ProductId,
                    name,
                    uom,
                    shipped,
                    grnLine.QuantityReceived,
                    grnLine.DamagedQty,
                    grnLine.RejectedQty,
                    ConnectedPoReceivingIssueLineKind.Missing,
                    grnLine.DiscrepancyKind == ConnectedPoReceivingDiscrepancyKind.None
                        ? ConnectedPoReceivingDiscrepancyKind.Short
                        : grnLine.DiscrepancyKind,
                    grnLine.DiscrepancyNote));
            }

            if (grnLine.DamagedQty > 0m)
            {
                drafts.Add(new ConnectedPoReceivingIssueLineDraft(
                    grnLine.Id,
                    grnLine.PurchaseOrderLineId,
                    supplierProductId,
                    grnLine.ProductId,
                    name,
                    uom,
                    shipped,
                    grnLine.QuantityReceived,
                    grnLine.DamagedQty,
                    grnLine.RejectedQty,
                    ConnectedPoReceivingIssueLineKind.Damaged,
                    grnLine.DiscrepancyKind == ConnectedPoReceivingDiscrepancyKind.None
                        ? ConnectedPoReceivingDiscrepancyKind.Damaged
                        : grnLine.DiscrepancyKind,
                    grnLine.DiscrepancyNote));
            }
        }

        if (drafts.Count == 0)
        {
            return null;
        }

        return ConnectedPoReceivingIssue.CreateFromReceipt(
            connected.Id,
            buyerPo.Id,
            receipt.Id,
            connected.BuyerOrganizationId,
            connected.SupplierOrganizationId,
            fulfillmentSourceId,
            actorId,
            utcNow,
            drafts);
    }

    public static Guid ResolveFulfillmentSourceIdForReceipt(ConnectedPurchaseOrder order)
    {
        // Reconstruct candidates for waves that may have deducted stock, then prefer
        // order.Id (first wave) when revision indicates no reopen-after-fulfill wave.
        // Application layer may override via movement lookup; this is the deterministic fallback.
        if (order.FulfilledAtUtc is null)
        {
            return order.Id.Value;
        }

        // After first fulfill FulfilledAtUtc is set. Reopen increments revision while keeping FulfilledAtUtc.
        // First-wave fulfill always used order.Id (FulfilledAtUtc was null at that moment).
        // Subsequent fulfills used Wave(orderId, revision) because FulfilledAtUtc was already set.
        // Heuristic: if revision is 0, only first-wave source exists.
        if (order.InventoryReservationRevision <= 0)
        {
            return order.Id.Value;
        }

        // Prefer the wave id for the current revision (most recent reopen+fulfill path).
        return ConnectedPurchaseOrderFulfillStock.WaveFulfillmentSourceId(
            order.Id.Value,
            order.InventoryReservationRevision);
    }

    private static CatalogProductId ResolveSupplierProductId(
        GoodsReceiptLine grnLine,
        PurchaseOrder buyerPo,
        ConnectedPurchaseOrder connected,
        ConnectedIncomingOrderFulfillmentProjection.ProductLinkMaps links)
    {
        var poLine = buyerPo.Lines.FirstOrDefault(l => l.Id == grnLine.PurchaseOrderLineId);
        if (poLine?.SupplierProductId is { } snap)
        {
            return snap;
        }

        if (links.BuyerToSupplierProductId.TryGetValue(grnLine.ProductId.Value, out var linked))
        {
            return CatalogProductId.From(linked);
        }

        var match = connected.Lines.FirstOrDefault(l =>
            string.Equals(l.NameSnapshot, grnLine.NameSnapshot, StringComparison.OrdinalIgnoreCase));
        return match?.ProductId ?? grnLine.ProductId;
    }
}

public sealed class ListIncomingOrderReceivingIssues
{
    private readonly IConnectedPurchaseOrderRepository _orders;
    private readonly IConnectedPoReceivingIssueRepository _issues;
    private readonly IPosCommercialAccessAccessor _access;

    public ListIncomingOrderReceivingIssues(
        IConnectedPurchaseOrderRepository orders,
        IConnectedPoReceivingIssueRepository issues,
        IPosCommercialAccessAccessor access)
    {
        _orders = orders;
        _issues = issues;
        _access = access;
    }

    public async Task<ApplicationResult<IReadOnlyList<ConnectedPoReceivingIssueDto>>> ExecuteAsync(
        Guid sellerOrganizationId,
        Guid connectedPurchaseOrderId,
        CancellationToken cancellationToken = default)
    {
        var gate = ConnectedSupplierUseCaseGuard.Access(_access, UtangCapability.ViewPurchasing);
        if (!gate.IsSuccess)
        {
            return ConnectedSupplierUseCaseGuard.Failure<IReadOnlyList<ConnectedPoReceivingIssueDto>>(
                gate.ErrorCode!,
                gate.ErrorMessage!);
        }

        var order = await _orders
            .GetAsync(ConnectedPurchaseOrderId.From(connectedPurchaseOrderId), cancellationToken)
            .ConfigureAwait(false);
        if (order is null || order.SupplierOrganizationId != PosOrganizationId.From(sellerOrganizationId))
        {
            return ConnectedSupplierUseCaseGuard.Failure<IReadOnlyList<ConnectedPoReceivingIssueDto>>(
                ConnectedSupplierErrorCodes.IncomingOrderNotFound,
                "Incoming order was not found.");
        }

        var list = await _issues
            .ListByConnectedOrderAsync(order.Id, cancellationToken)
            .ConfigureAwait(false);
        return ApplicationResult<IReadOnlyList<ConnectedPoReceivingIssueDto>>.Success(
            list.Select(ConnectedPoReceivingIssueMapper.Map).ToList());
    }
}

public sealed class ResolveIncomingOrderReceivingIssue
{
    private readonly IConnectedPurchaseOrderRepository _orders;
    private readonly IConnectedPoReceivingIssueRepository _issues;
    private readonly IConnectedSupplierRelationshipRepository _relationships;
    private readonly IPurchaseOrderRepository _buyerOrders;
    private readonly IInventoryRepository _inventory;
    private readonly ICatalogProductRepository _products;
    private readonly IInventoryBranchBalanceRepository _branchBalances;
    private readonly BranchInventoryMutationService _branchMutations;
    private readonly IOrganizationBranchDirectory? _branches;
    private readonly IReturnBatchRepository? _returnBatches;
    private readonly IPosUnitOfWork _uow;
    private readonly IPosCommercialAccessAccessor _access;
    private readonly TimeProvider _clock;

    public ResolveIncomingOrderReceivingIssue(
        IConnectedPurchaseOrderRepository orders,
        IConnectedPoReceivingIssueRepository issues,
        IConnectedSupplierRelationshipRepository relationships,
        IPurchaseOrderRepository buyerOrders,
        IInventoryRepository inventory,
        ICatalogProductRepository products,
        IInventoryBranchBalanceRepository branchBalances,
        BranchInventoryMutationService branchMutations,
        IPosUnitOfWork uow,
        IPosCommercialAccessAccessor access,
        IOrganizationBranchDirectory? branches = null,
        IReturnBatchRepository? returnBatches = null,
        TimeProvider? clock = null)
    {
        _orders = orders;
        _issues = issues;
        _relationships = relationships;
        _buyerOrders = buyerOrders;
        _inventory = inventory;
        _products = products;
        _branchBalances = branchBalances;
        _branchMutations = branchMutations;
        _uow = uow;
        _access = access;
        _branches = branches;
        _returnBatches = returnBatches;
        _clock = clock ?? TimeProvider.System;
    }

    public async Task<ApplicationResult<ConnectedPoReceivingIssueDto>> ExecuteAsync(
        Guid sellerOrganizationId,
        Guid connectedPurchaseOrderId,
        Guid receivingIssueId,
        ResolveConnectedPoReceivingIssueRequest request,
        Guid actorId,
        CancellationToken cancellationToken = default)
    {
        var gate = ConnectedSupplierUseCaseGuard.Access(_access, UtangCapability.ManagePurchasing);
        if (!gate.IsSuccess)
        {
            return ConnectedSupplierUseCaseGuard.Failure<ConnectedPoReceivingIssueDto>(
                gate.ErrorCode!,
                gate.ErrorMessage!);
        }

        if (actorId == Guid.Empty)
        {
            return ConnectedSupplierUseCaseGuard.Failure<ConnectedPoReceivingIssueDto>(
                ApplicationErrorCodes.ActorRequired,
                "An actor identifier is required to resolve receiving issues.");
        }

        if (request.Lines is null || request.Lines.Count == 0)
        {
            return ConnectedSupplierUseCaseGuard.Failure<ConnectedPoReceivingIssueDto>(
                DomainErrorCodes.InvalidConnectedPoReceivingIssueLine,
                "At least one receiving-issue line resolution is required.");
        }

        try
        {
            var sellerOrg = PosOrganizationId.From(sellerOrganizationId);
            var order = await _orders
                .GetAsync(ConnectedPurchaseOrderId.From(connectedPurchaseOrderId), cancellationToken)
                .ConfigureAwait(false);
            if (order is null || order.SupplierOrganizationId != sellerOrg)
            {
                return ConnectedSupplierUseCaseGuard.Failure<ConnectedPoReceivingIssueDto>(
                    ConnectedSupplierErrorCodes.IncomingOrderNotFound,
                    "Incoming order was not found.");
            }

            var issue = await _issues
                .GetAsync(ConnectedPoReceivingIssueId.From(receivingIssueId), cancellationToken)
                .ConfigureAwait(false);
            if (issue is null
                || issue.ConnectedPurchaseOrderId != order.Id
                || issue.SellerOrganizationId != sellerOrg)
            {
                return ConnectedSupplierUseCaseGuard.Failure<ConnectedPoReceivingIssueDto>(
                    DomainErrorCodes.ConnectedPoReceivingIssueNotFound,
                    "Receiving issue was not found.");
            }

            issue.EnsureSellerOrganization(sellerOrg);
            var utcNow = _clock.GetUtcNow();
            var rel = await _relationships.GetAsync(order.RelationshipId, cancellationToken).ConfigureAwait(false);
            Guid? supplierBranchGuid = rel?.SupplierBranchId;
            PosBranchId? supplierBranch = supplierBranchGuid is Guid bid && bid != Guid.Empty
                ? PosBranchId.From(bid)
                : null;
            Guid? primaryId = _branches is null
                ? null
                : await _branches.GetPrimaryBranchIdAsync(sellerOrg.Value, cancellationToken).ConfigureAwait(false);

            foreach (var lineReq in request.Lines)
            {
                var line = issue.GetLine(ConnectedPoReceivingIssueLineId.From(lineReq.ReceivingIssueLineId));
                if (line.IsResolved)
                {
                    // Idempotent path: validate same decision via domain Resolve* below.
                }

                if (line.LineKind == ConnectedPoReceivingIssueLineKind.Missing)
                {
                    if (string.IsNullOrWhiteSpace(lineReq.MissingResolution)
                        || !ConnectedPoMissingResolutions.TryParse(lineReq.MissingResolution, out var missingRes))
                    {
                        return ConnectedSupplierUseCaseGuard.Failure<ConnectedPoReceivingIssueDto>(
                            DomainErrorCodes.InvalidConnectedPoMissingResolution,
                            "A valid missing resolution is required.");
                    }

                    var qty = lineReq.ResolutionQty ?? line.MissingQty;
                    Guid? movementId = null;
                    if (ConnectedPoMissingResolutions.RestoresSellerStock(missingRes))
                    {
                        movementId = await RestoreSellerStockAsync(
                                sellerOrg,
                                line,
                                qty,
                                actorId,
                                utcNow,
                                supplierBranch,
                                primaryId,
                                cancellationToken)
                            .ConfigureAwait(false);
                    }

                    line.ResolveMissing(missingRes, qty, lineReq.SellerNote, actorId, utcNow, movementId);
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(lineReq.DamagedResolution)
                        || !ConnectedPoDamagedResolutions.TryParse(lineReq.DamagedResolution, out var damagedRes))
                    {
                        return ConnectedSupplierUseCaseGuard.Failure<ConnectedPoReceivingIssueDto>(
                            DomainErrorCodes.InvalidConnectedPoDamagedResolution,
                            "A valid damaged resolution is required.");
                    }

                    var qty = lineReq.ResolutionQty ?? line.DamagedQty;
                    Guid? returnBatchId = null;
                    if (damagedRes == ConnectedPoDamagedResolution.ReturnRequested && _returnBatches is not null)
                    {
                        returnBatchId = await TryCreateReturnBatchAsync(
                                order,
                                line,
                                qty,
                                actorId,
                                utcNow,
                                cancellationToken)
                            .ConfigureAwait(false);
                    }

                    line.ResolveDamaged(damagedRes, qty, lineReq.SellerNote, actorId, utcNow, inventoryMovementId: null, returnBatchId);
                }
            }

            issue.RefreshStatus(actorId, utcNow, request.SellerNotes);
            await _issues.UpdateAsync(issue, cancellationToken).ConfigureAwait(false);
            await _uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<ConnectedPoReceivingIssueDto>.Success(ConnectedPoReceivingIssueMapper.Map(issue));
        }
        catch (DomainException ex)
        {
            return ConnectedSupplierUseCaseGuard.Failure<ConnectedPoReceivingIssueDto>(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ConnectedSupplierUseCaseGuard.Failure<ConnectedPoReceivingIssueDto>(ex.ErrorCode, ex.Message);
        }
    }

    private async Task<Guid?> RestoreSellerStockAsync(
        PosOrganizationId sellerOrg,
        ConnectedPoReceivingIssueLine line,
        decimal purchaseQty,
        Guid actorId,
        DateTimeOffset utcNow,
        PosBranchId? supplierBranch,
        Guid? primaryId,
        CancellationToken cancellationToken)
    {
        if (await _inventory
                .HasConnectedPurchaseFulfillmentReconciliationAsync(
                    sellerOrg,
                    line.Id.Value,
                    line.SupplierProductId,
                    cancellationToken)
                .ConfigureAwait(false))
        {
            // Already restored — look up existing movement id is optional; domain idempotency handles retry.
            return line.InventoryMovementId;
        }

        var product = await _products
            .GetByIdAsync(sellerOrg, line.SupplierProductId, cancellationToken)
            .ConfigureAwait(false);
        if (product is null)
        {
            throw new DomainException(
                ApplicationErrorCodes.SaleProductNotFound,
                $"Supplier product '{line.NameSnapshot}' was not found.");
        }

        Guid? movementId = null;
        await _inventory
            .ExecuteWithProductReservationLocksAsync(
                sellerOrg,
                [line.SupplierProductId],
                async (accounts, ct) =>
                {
                    var account = accounts.FirstOrDefault(a => a.ProductId == line.SupplierProductId);
                    if (account is null || !account.IsTracked)
                    {
                        // Untracked: retain resolution without fabricating stock.
                        return;
                    }

                    if (supplierBranch is null)
                    {
                        throw new DomainException(
                            ConnectedSupplierErrorCodes.InsufficientSupplierStock,
                            "Supplier branch is required to restore tracked inventory.");
                    }

                    if (await _inventory
                            .HasConnectedPurchaseFulfillmentReconciliationAsync(
                                sellerOrg,
                                line.Id.Value,
                                line.SupplierProductId,
                                ct)
                            .ConfigureAwait(false))
                    {
                        return;
                    }

                    var branch = supplierBranch;
                    var movement = StockMovement.ConnectedPurchaseFulfillmentReconciliation(
                            sellerOrg,
                            line.SupplierProductId,
                            account.Id,
                            purchaseQty,
                            product.UnitOfMeasure,
                            line.Id.Value,
                            line.FulfillmentSourceId,
                            actorId,
                            utcNow,
                            sellingMode: product.SellingMode,
                            branchId: branch.Value)
                        .WithBranch(branch.Value);

                    var orgOnHandBefore = account.OnHandQuantity;
                    account.ApplyMovementEffect(movement.QuantityEffect);
                    account.Touch(utcNow);
                    await _branchMutations
                        .ApplyBranchDeltaAsync(
                            _branchBalances,
                            sellerOrg,
                            branch,
                            primaryId,
                            line.SupplierProductId,
                            orgOnHandBefore,
                            movement.QuantityEffect,
                            utcNow,
                            ct)
                        .ConfigureAwait(false);
                    await _inventory.UpdateAccountAsync(account, ct).ConfigureAwait(false);
                    await _inventory.AddMovementAsync(movement, ct).ConfigureAwait(false);
                    movementId = movement.Id.Value;
                },
                cancellationToken)
            .ConfigureAwait(false);

        return movementId;
    }

    private async Task<Guid?> TryCreateReturnBatchAsync(
        ConnectedPurchaseOrder order,
        ConnectedPoReceivingIssueLine line,
        decimal qty,
        Guid actorId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        if (_returnBatches is null)
        {
            return null;
        }

        var buyerPo = await _buyerOrders
            .GetByIdAsync(order.BuyerOrganizationId, order.BuyerPurchaseOrderId, cancellationToken)
            .ConfigureAwait(false);
        if (buyerPo is null || buyerPo.Status != PurchaseOrderStatus.Received)
        {
            return null;
        }

        try
        {
            var prior = ConnectedPoReturnQueryService.SumReturnedByPurchaseOrderLine(
                await _returnBatches
                    .ListByPurchaseOrderIdAsync(order.BuyerOrganizationId, order.BuyerPurchaseOrderId, cancellationToken)
                    .ConfigureAwait(false));
            var created = await _returnBatches
                .CreateAsync(
                    order.SupplierOrganizationId,
                    ReturnBatchNumbers.BusinessDateOf(utcNow),
                    number => ReturnBatch.CreateAcceptedForConnectedPurchaseOrder(
                        order.SupplierOrganizationId,
                        order.BuyerOrganizationId,
                        number,
                        buyerPo,
                        [new ReturnBatchConnectedPoLineDraft(line.PurchaseOrderLineId, qty)],
                        prior,
                        reason: "Damaged goods — seller requested return from receiving issue",
                        actorId,
                        utcNow,
                        order.Id,
                        order.EffectivePaymentTiming,
                        buyerBranchId: buyerPo.IntendedReceivingBranchId is { } branch
                            ? PosBranchId.From(branch)
                            : null,
                        sellerBranchId: buyerPo.SupplierBranchId is { } supplierBranch
                            ? PosBranchId.From(supplierBranch)
                            : null,
                        notes: line.BuyerDiscrepancyNote),
                    afterCreated: null,
                    cancellationToken)
                .ConfigureAwait(false);
            return created.Id.Value;
        }
        catch (DomainException)
        {
            return null;
        }
    }
}
