using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Application.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Purchasing;
using ExItS.PinoyBusinessPOS.Domain.Suppliers;

namespace ExItS.PinoyBusinessPOS.Application.Purchasing;

public sealed record PosPurchaseOrderLineDto(
    Guid LineId,
    Guid ProductId,
    int LineNumber,
    string? NameSnapshot,
    string? UomSnapshot,
    decimal OrderedQty,
    decimal UnitPurchaseCost,
    decimal LineTotal,
    decimal ReceivedQty,
    decimal OutstandingQty,
    string? LineNotes,
    decimal ClosedShortQty = 0m);

public sealed record PosPurchaseOrderDto(
    Guid PurchaseOrderId,
    Guid OrganizationId,
    string? PoNumber,
    Guid SupplierId,
    string Status,
    DateOnly OrderDate,
    DateOnly? ExpectedDeliveryDate,
    string? SupplierReference,
    string? Notes,
    DateTimeOffset? OrderedAtUtc,
    Guid? OrderedBy,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    IReadOnlyList<PosPurchaseOrderLineDto> Lines,
    string DisplayStatus = "",
    string? ConnectedStatus = null,
    Guid? ConnectedPurchaseOrderId = null,
    DateTimeOffset? SupplierAcceptedAtUtc = null,
    DateTimeOffset? SupplierDeclinedAtUtc = null,
    DateTimeOffset? SupplierPreparingAtUtc = null,
    DateTimeOffset? SupplierFulfilledAtUtc = null,
    DateTimeOffset? WithdrawnAtUtc = null,
    string? DeclineReason = null,
    string? DeclineNote = null,
    bool HasReceivingIssues = false,
    bool CanWithdrawConnected = false,
    bool CanReceiveConnected = true,
    string PaymentTerm = "Cash",
    string PaymentTermLabel = "Cash",
    decimal? ProposedTotalAmount = null,
    decimal? ConfirmedTotalAmount = null,
    IReadOnlyList<ConnectedPurchaseOrderLineDto>? ConnectedLines = null,
    DateTimeOffset? ChangesProposedAtUtc = null,
    string? SupplierName = null);

public sealed record PosGoodsReceiptLineDto(
    Guid LineId,
    Guid PurchaseOrderLineId,
    Guid ProductId,
    int LineNumber,
    string NameSnapshot,
    string UomSnapshot,
    decimal QuantityReceived,
    decimal UnitPurchaseCostSnapshot,
    decimal LineTotalSnapshot,
    Guid? InventoryMovementId,
    decimal DamagedQty = 0m,
    decimal RejectedQty = 0m,
    decimal ShortClosedQty = 0m,
    string DiscrepancyKind = "None",
    string? DiscrepancyNote = null)
{
    /// <summary>Alias for clients that still expect ReceivedQty.</summary>
    public decimal ReceivedQty => QuantityReceived;
}

public sealed record PosGoodsReceiptDto(
    Guid GoodsReceiptId,
    Guid OrganizationId,
    Guid PurchaseOrderId,
    Guid SupplierId,
    string GrnNumber,
    DateOnly ReceivedDate,
    string? DeliveryReference,
    string? Notes,
    DateTimeOffset ReceivedAtUtc,
    Guid ReceivedBy,
    IReadOnlyList<PosGoodsReceiptLineDto> Lines);

public sealed record CreatePurchaseOrderLineRequest(
    Guid ProductId,
    decimal OrderedQty,
    decimal UnitPurchaseCost,
    string? LineNotes = null,
    Guid? PurchaseUnitId = null);

public sealed record CreatePurchaseOrderRequest(
    Guid SupplierId,
    DateOnly OrderDate,
    IReadOnlyList<CreatePurchaseOrderLineRequest> Lines,
    DateOnly? ExpectedDeliveryDate = null,
    string? SupplierReference = null,
    string? Notes = null,
    string? PaymentTerm = null,
    Guid? PurchaseOrderId = null);

public sealed record UpdatePurchaseOrderRequest(
    Guid SupplierId,
    DateOnly OrderDate,
    IReadOnlyList<CreatePurchaseOrderLineRequest> Lines,
    DateTimeOffset ExpectedUpdatedAtUtc,
    DateOnly? ExpectedDeliveryDate = null,
    string? SupplierReference = null,
    string? Notes = null,
    string? PaymentTerm = null);

public sealed record ReceivePurchaseOrderLineRequest(
    Guid ProductId,
    decimal ReceiveQty,
    decimal DamagedQty = 0m,
    decimal RejectedQty = 0m,
    decimal ShortClosedQty = 0m,
    string? DiscrepancyKind = null,
    string? DiscrepancyNote = null);

public sealed record ReceivePurchaseOrderRequest(
    IReadOnlyList<ReceivePurchaseOrderLineRequest> Lines,
    Guid? GoodsReceiptId = null,
    DateOnly? ReceivedDate = null,
    string? DeliveryReference = null,
    string? Notes = null);

public static class PurchaseMapper
{
    public static PosPurchaseOrderDto Map(
        PurchaseOrder po,
        ConnectedPurchaseOrder? connected = null,
        string? supplierName = null,
        IReadOnlyDictionary<Guid, CatalogProduct>? products = null)
    {
        var display = string.IsNullOrEmpty(ConnectedPoDisplayStatus.ForBuyer(po, connected))
            ? po.Status.ToString()
            : ConnectedPoDisplayStatus.ForBuyer(po, connected);
        return new(
            po.Id.Value,
            po.OrganizationId.Value,
            po.PoNumber,
            po.SupplierId.Value,
            po.Status.ToString(),
            po.OrderDate,
            po.ExpectedDeliveryDate,
            po.SupplierReference,
            po.Notes,
            po.OrderedAtUtc,
            po.OrderedBy,
            po.CreatedAtUtc,
            po.UpdatedAtUtc,
            po.Lines.Select(line => MapLine(line, products)).ToList(),
            DisplayStatus: display,
            ConnectedStatus: connected?.Status.ToString(),
            ConnectedPurchaseOrderId: connected?.Id.Value,
            SupplierAcceptedAtUtc: connected?.AcceptedAtUtc,
            SupplierDeclinedAtUtc: connected?.DeclinedAtUtc,
            SupplierPreparingAtUtc: connected?.PreparingAtUtc,
            SupplierFulfilledAtUtc: connected?.FulfilledAtUtc,
            WithdrawnAtUtc: connected?.WithdrawnAtUtc,
            DeclineReason: connected?.DeclineReason?.ToString(),
            DeclineNote: connected?.DeclineNote,
            HasReceivingIssues: po.HasReceivingIssues,
            CanWithdrawConnected: connected?.CanBuyerWithdraw == true,
            CanReceiveConnected: connected is null || connected.CanBuyerReceive,
            PaymentTerm: ConnectedPoPaymentTerms.ToApi(connected?.PaymentTerm ?? po.PaymentTerm),
            PaymentTermLabel: ConnectedPoPaymentTerms.ToUiLabel(connected?.PaymentTerm ?? po.PaymentTerm),
            ProposedTotalAmount: connected?.ProposedTotalAmount,
            ConfirmedTotalAmount: connected is null ? null : connected.ConfirmedTotalAmount,
            ConnectedLines: connected?.Lines.Select(ConnectedSupplierMapper.MapLine).ToList(),
            ChangesProposedAtUtc: connected?.ChangesProposedAtUtc,
            SupplierName: supplierName);
    }

    public static async Task<PosPurchaseOrderDto> MapWithNamesAsync(
        PurchaseOrder po,
        ConnectedPurchaseOrder? connected,
        ISupplierRepository suppliers,
        ICatalogProductRepository products,
        CancellationToken cancellationToken)
    {
        var supplier = await suppliers
            .GetByIdAsync(po.OrganizationId, po.SupplierId, cancellationToken)
            .ConfigureAwait(false);
        var productIds = po.Lines.Select(l => l.ProductId).Distinct().ToList();
        var catalog = productIds.Count == 0
            ? Array.Empty<CatalogProduct>()
            : await products.ListByIdsAsync(po.OrganizationId, productIds, cancellationToken).ConfigureAwait(false);
        return Map(po, connected, supplier?.Name, catalog.ToDictionary(p => p.Id.Value));
    }

    public static PosPurchaseOrderLineDto MapLine(
        PurchaseOrderLine line,
        IReadOnlyDictionary<Guid, CatalogProduct>? products = null)
    {
        var product = products is not null && products.TryGetValue(line.ProductId.Value, out var found)
            ? found
            : null;
        var name = string.IsNullOrWhiteSpace(line.NameSnapshot) ? product?.Name : line.NameSnapshot;
        var uom = line.UomSnapshot?.ToString()
            ?? (string.IsNullOrWhiteSpace(line.PurchaseUnitNameSnapshot) ? null : line.PurchaseUnitNameSnapshot)
            ?? product?.UnitOfMeasure.ToString();
        return new(
            line.Id.Value,
            line.ProductId.Value,
            line.LineNumber,
            name,
            uom,
            line.OrderedQty,
            line.UnitPurchaseCost,
            line.LineTotal,
            line.ReceivedQty,
            line.OutstandingQty,
            line.LineNotes,
            line.ClosedShortQty);
    }

    public static PosGoodsReceiptDto Map(GoodsReceipt receipt) =>
        new(
            receipt.Id.Value,
            receipt.OrganizationId.Value,
            receipt.PurchaseOrderId.Value,
            receipt.SupplierId.Value,
            receipt.GrnNumber,
            receipt.ReceivedDate,
            receipt.DeliveryReference,
            receipt.Notes,
            receipt.ReceivedAtUtc,
            receipt.ReceivedBy,
            receipt.Lines.Select(l => new PosGoodsReceiptLineDto(
                l.Id.Value,
                l.PurchaseOrderLineId.Value,
                l.ProductId.Value,
                l.LineNumber,
                l.NameSnapshot,
                l.UomSnapshot.ToString(),
                l.QuantityReceived,
                l.UnitPurchaseCostSnapshot,
                l.LineTotalSnapshot,
                l.InventoryMovementId,
                l.DamagedQty,
                l.RejectedQty,
                l.ShortClosedQty,
                l.DiscrepancyKind.ToString(),
                l.DiscrepancyNote)).ToList());
}

public sealed class PurchaseOrderQueryService
{
    private readonly IPurchaseOrderRepository _orders;
    private readonly IConnectedPurchaseOrderRepository _connectedOrders;
    private readonly ISupplierRepository _suppliers;
    private readonly ICatalogProductRepository _products;

    public PurchaseOrderQueryService(
        IPurchaseOrderRepository orders,
        IConnectedPurchaseOrderRepository connectedOrders,
        ISupplierRepository suppliers,
        ICatalogProductRepository products)
    {
        _orders = orders;
        _connectedOrders = connectedOrders;
        _suppliers = suppliers;
        _products = products;
    }

    public async Task<PosPurchaseOrderDto?> GetByIdAsync(
        Guid organizationId,
        Guid purchaseOrderId,
        CancellationToken cancellationToken = default)
    {
        var po = await _orders
            .GetByIdAsync(PosOrganizationId.From(organizationId), PurchaseOrderId.From(purchaseOrderId), cancellationToken)
            .ConfigureAwait(false);
        if (po is null)
        {
            return null;
        }

        var connected = await _connectedOrders
            .GetByBuyerPurchaseOrderAsync(po.Id, cancellationToken)
            .ConfigureAwait(false);
        if (connected is not null
            && connected.BuyerOrganizationId != PosOrganizationId.From(organizationId))
        {
            connected = null;
        }

        return await PurchaseMapper
            .MapWithNamesAsync(po, connected, _suppliers, _products, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<PagedResult<PosPurchaseOrderDto>> ListAsync(
        Guid organizationId,
        PurchaseOrderFilter filter,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken = default)
    {
        var (skip, take) = PosPagination.Normalize(page, pageSize);
        var org = PosOrganizationId.From(organizationId);
        var (items, total) = await _orders
            .ListAsync(org, filter, skip, take, cancellationToken)
            .ConfigureAwait(false);

        var mapped = new List<PosPurchaseOrderDto>(items.Count);
        foreach (var po in items)
        {
            var connected = await _connectedOrders
                .GetByBuyerPurchaseOrderAsync(po.Id, cancellationToken)
                .ConfigureAwait(false);
            if (connected is not null
                && connected.BuyerOrganizationId != org)
            {
                connected = null;
            }

            mapped.Add(await PurchaseMapper
                .MapWithNamesAsync(po, connected, _suppliers, _products, cancellationToken)
                .ConfigureAwait(false));
        }

        return new PagedResult<PosPurchaseOrderDto>(
            mapped,
            total,
            Math.Max(page ?? 1, 1),
            take);
    }
}

public sealed class GoodsReceiptQueryService
{
    private readonly IPurchaseOrderRepository _orders;

    public GoodsReceiptQueryService(IPurchaseOrderRepository orders) => _orders = orders;

    public async Task<PosGoodsReceiptDto?> GetByIdAsync(
        Guid organizationId,
        Guid goodsReceiptId,
        CancellationToken cancellationToken = default)
    {
        var receipt = await _orders
            .GetGoodsReceiptByIdAsync(
                PosOrganizationId.From(organizationId),
                GoodsReceiptId.From(goodsReceiptId),
                cancellationToken)
            .ConfigureAwait(false);
        return receipt is null ? null : PurchaseMapper.Map(receipt);
    }

    public async Task<IReadOnlyList<PosGoodsReceiptDto>> ListForPurchaseOrderAsync(
        Guid organizationId,
        Guid purchaseOrderId,
        CancellationToken cancellationToken = default)
    {
        var receipts = await _orders
            .ListGoodsReceiptsForPurchaseOrderAsync(
                PosOrganizationId.From(organizationId),
                PurchaseOrderId.From(purchaseOrderId),
                cancellationToken)
            .ConfigureAwait(false);
        return receipts.Select(PurchaseMapper.Map).ToList();
    }
}

internal static class PurchaseProductGuard
{
    public static async Task<(ApplicationResult? Error, IReadOnlyDictionary<Guid, CatalogProduct> Products)> ResolveProductsAsync(
        ICatalogProductRepository products,
        PosOrganizationId organizationId,
        IReadOnlyList<Guid> productIds,
        CancellationToken cancellationToken)
    {
        var ids = productIds.Select(CatalogProductId.From).ToList();
        var found = await products.ListByIdsAsync(organizationId, ids, cancellationToken).ConfigureAwait(false);
        var byId = found.ToDictionary(p => p.Id.Value);
        foreach (var id in productIds)
        {
            if (!byId.ContainsKey(id))
            {
                return (ApplicationResult.Failure(
                    ApplicationErrorCodes.PurchaseProductNotFound,
                    "One or more products were not found in this organization."), byId);
            }

            if (byId[id].Status != CatalogProductStatus.Active)
            {
                return (ApplicationResult.Failure(
                    ApplicationErrorCodes.PurchaseProductNotActive,
                    "Only active catalog products can be added to a purchase order."), byId);
            }
        }

        return (null, byId);
    }
}

internal static class PurchaseSupplierGuard
{
    public static async Task<ApplicationResult?> EnsureActiveSupplierAsync(
        ISupplierRepository suppliers,
        PosOrganizationId organizationId,
        SupplierId supplierId,
        CancellationToken cancellationToken)
    {
        var supplier = await suppliers.GetByIdAsync(organizationId, supplierId, cancellationToken).ConfigureAwait(false);
        if (supplier is null)
        {
            return ApplicationResult.Failure(
                ApplicationErrorCodes.SupplierNotFound,
                "Supplier was not found in this organization.");
        }

        if (supplier.Status != SupplierStatus.Active)
        {
            return ApplicationResult.Failure(
                ApplicationErrorCodes.PurchaseSupplierNotActive,
                "Only active suppliers can be used on purchase orders.");
        }

        return null;
    }
}

/// <summary>
/// Connected-supplier PO line eligibility: Active relationship, buyer link for this
/// relationship only, shared + orderable exposure, and effective PO price
/// (buyer-specific else Default PO). Never uses buyer retail SellingPrice.
/// </summary>
public static class ConnectedPurchaseOrderLineEligibility
{
    public sealed record Outcome(
        ConnectedSupplierRelationship Relationship,
        IReadOnlyDictionary<Guid, BuyerSupplierProductLink> LinksByBuyerProductId,
        IReadOnlyDictionary<Guid, decimal> EffectivePriceByBuyerProductId);

    /// <summary>
    /// Returns null when the supplier is external (no connected checks).
    /// Returns Failure when connected and any line is invalid.
    /// Returns Success with effective prices keyed by buyer product id when connected and valid.
    /// </summary>
    public static async Task<ApplicationResult<Outcome>?> ValidateIfConnectedAsync(
        PosOrganizationId buyerOrganizationId,
        Supplier supplier,
        IReadOnlyList<Guid> buyerProductIds,
        IConnectedSupplierRelationshipRepository relationships,
        IBuyerSupplierProductLinkRepository links,
        ISupplierProductExposureRepository? exposures,
        IConnectedBuyerProductShareRepository? shares,
        CancellationToken cancellationToken)
    {
        if (supplier.ConnectionType != SupplierConnectionType.ConnectedOrganization)
        {
            return null;
        }

        if (supplier.ConnectedRelationshipId is null || exposures is null || shares is null)
        {
            return ApplicationResult<Outcome>.Failure(
                ConnectedSupplierErrorCodes.RelationshipInactive,
                "The connected supplier relationship is not available.");
        }

        var relationship = await relationships
            .GetAsync(supplier.ConnectedRelationshipId, cancellationToken)
            .ConfigureAwait(false);
        if (relationship is null
            || relationship.Status != ConnectedSupplierRelationshipStatus.Active
            || relationship.BuyerOrganizationId != buyerOrganizationId)
        {
            return ApplicationResult<Outcome>.Failure(
                ConnectedSupplierErrorCodes.RelationshipInactive,
                "The connected supplier relationship is not active.");
        }

        if (supplier.ConnectedRelationshipId is null
            || supplier.ConnectedRelationshipId.Value != relationship.Id.Value)
        {
            return ApplicationResult<Outcome>.Failure(
                ConnectedSupplierErrorCodes.RelationshipInactive,
                "The supplier is not bound to the expected connected relationship.");
        }

        var linkList = await links
            .ListAsync(relationship.Id, buyerOrganizationId, cancellationToken)
            .ConfigureAwait(false);
        var linksByBuyerProduct = linkList
            .Where(x => x.IsActive)
            .ToDictionary(x => x.BuyerProductId.Value);

        var prices = new Dictionary<Guid, decimal>();
        foreach (var buyerProductId in buyerProductIds.Distinct())
        {
            if (!linksByBuyerProduct.TryGetValue(buyerProductId, out var link))
            {
                return ApplicationResult<Outcome>.Failure(
                    ConnectedSupplierErrorCodes.LinkNotFound,
                    "Every connected purchase-order line must have an active supplier product link for this supplier.");
            }

            if (link.RelationshipId.Value != relationship.Id.Value
                || link.BuyerOrganizationId != buyerOrganizationId
                || link.SupplierOrganizationId != relationship.SupplierOrganizationId)
            {
                return ApplicationResult<Outcome>.Failure(
                    ConnectedSupplierErrorCodes.LinkNotFound,
                    "A purchase-order line is linked to a different connected supplier relationship.");
            }

            var exposure = await exposures
                .GetByProductAsync(relationship.SupplierOrganizationId, link.SupplierProductId, cancellationToken)
                .ConfigureAwait(false);
            var share = await shares
                .FindAsync(relationship.Id, link.SupplierProductId, cancellationToken)
                .ConfigureAwait(false);
            if (exposure is null
                || !ConnectedPoPricing.TryResolveEffectivePrice(exposure, share, out var effectivePrice))
            {
                return ApplicationResult<Outcome>.Failure(
                    ConnectedSupplierErrorCodes.ExposureNotFound,
                    "A connected supplier product is no longer shared or orderable.");
            }

            prices[buyerProductId] = effectivePrice;
        }

        return ApplicationResult<Outcome>.Success(new(relationship, linksByBuyerProduct, prices));
    }
}

public sealed class CreatePurchaseOrder
{
    private readonly IPurchaseOrderRepository _orders;
    private readonly ISupplierRepository _suppliers;
    private readonly ICatalogProductRepository _products;
    private readonly ICatalogProductUnitRepository _units;
    private readonly IConnectedSupplierRelationshipRepository _connectedRelationships;
    private readonly IBuyerSupplierProductLinkRepository _connectedLinks;
    private readonly ISupplierProductExposureRepository? _connectedExposures;
    private readonly IConnectedBuyerProductShareRepository? _connectedShares;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IPosCommercialAccessAccessor _access;
    private readonly TimeProvider _clock;

    public CreatePurchaseOrder(
        IPurchaseOrderRepository orders,
        ISupplierRepository suppliers,
        ICatalogProductRepository products,
        ICatalogProductUnitRepository units,
        IConnectedSupplierRelationshipRepository connectedRelationships,
        IBuyerSupplierProductLinkRepository connectedLinks,
        IPosUnitOfWork unitOfWork,
        IPosCommercialAccessAccessor access,
        TimeProvider? clock = null,
        ISupplierProductExposureRepository? connectedExposures = null,
        IConnectedBuyerProductShareRepository? connectedShares = null)
    {
        _orders = orders;
        _suppliers = suppliers;
        _products = products;
        _units = units;
        _connectedRelationships = connectedRelationships;
        _connectedLinks = connectedLinks;
        _connectedExposures = connectedExposures;
        _connectedShares = connectedShares;
        _unitOfWork = unitOfWork;
        _access = access;
        _clock = clock ?? TimeProvider.System;
    }

    public async Task<ApplicationResult<PosPurchaseOrderDto>> ExecuteAsync(
        Guid organizationId,
        CreatePurchaseOrderRequest request,
        CancellationToken cancellationToken = default,
        Guid actorId = default)
    {
        var gate = CommercialAccessGuard.Require(_access, UtangCapability.ManagePurchasing);
        if (!gate.IsSuccess)
        {
            return ApplicationResult<PosPurchaseOrderDto>.Failure(gate.ErrorCode!, gate.ErrorMessage!);
        }

        try
        {
            var org = PosOrganizationId.From(organizationId);

            if (request.PurchaseOrderId is Guid clientPoId && clientPoId != Guid.Empty)
            {
                var existingById = await _orders
                    .GetByIdAsync(org, PurchaseOrderId.From(clientPoId), cancellationToken)
                    .ConfigureAwait(false);
                if (existingById is not null)
                {
                    var existingSupplier = await _suppliers
                        .GetByIdAsync(org, existingById.SupplierId, cancellationToken)
                        .ConfigureAwait(false);
                    return ApplicationResult<PosPurchaseOrderDto>.Success(
                        PurchaseMapper.Map(existingById, supplierName: existingSupplier?.Name));
                }
            }

            var supplierId = SupplierId.From(request.SupplierId);
            var supplierError = await PurchaseSupplierGuard
                .EnsureActiveSupplierAsync(_suppliers, org, supplierId, cancellationToken)
                .ConfigureAwait(false);
            if (supplierError is not null)
            {
                return ApplicationResult<PosPurchaseOrderDto>.Failure(supplierError.ErrorCode!, supplierError.ErrorMessage!);
            }

            var supplier = await _suppliers.GetByIdAsync(org, supplierId, cancellationToken).ConfigureAwait(false);
            if (supplier is null)
            {
                return ApplicationResult<PosPurchaseOrderDto>.Failure(
                    ApplicationErrorCodes.SupplierNotFound,
                    "Supplier was not found in this organization.");
            }

            var productIds = request.Lines.Select(l => l.ProductId).ToList();
            var (productError, products) = await PurchaseProductGuard
                .ResolveProductsAsync(_products, org, productIds, cancellationToken)
                .ConfigureAwait(false);
            if (productError is not null)
            {
                return ApplicationResult<PosPurchaseOrderDto>.Failure(productError.ErrorCode!, productError.ErrorMessage!);
            }

            var connectedEligibility = await ConnectedPurchaseOrderLineEligibility
                .ValidateIfConnectedAsync(
                    org,
                    supplier,
                    productIds,
                    _connectedRelationships,
                    _connectedLinks,
                    _connectedExposures,
                    _connectedShares,
                    cancellationToken)
                .ConfigureAwait(false);
            if (connectedEligibility is not null && !connectedEligibility.IsSuccess)
            {
                return ApplicationResult<PosPurchaseOrderDto>.Failure(
                    connectedEligibility.ErrorCode!,
                    connectedEligibility.ErrorMessage!);
            }

            var effectiveConnectedPrices = connectedEligibility?.Value?.EffectivePriceByBuyerProductId;

            var utcNow = _clock.GetUtcNow();
            var lineDrafts = new List<PurchaseOrderLineDraft>();
            foreach (var l in request.Lines)
            {
                ProductUnitId? purchaseUnitId = null;
                string? purchaseUnitName = null;
                var multiplier = 1m;
                if (l.PurchaseUnitId is not null)
                {
                    var unit = await _units
                        .GetByIdAsync(org, ProductUnitId.From(l.PurchaseUnitId.Value), cancellationToken)
                        .ConfigureAwait(false);
                    if (unit is null
                        || unit.ProductId.Value != l.ProductId
                        || !unit.IsActive
                        || unit.Kind != ProductUnitKind.Purchase)
                    {
                        return ApplicationResult<PosPurchaseOrderDto>.Failure(
                            DomainErrorCodes.InvalidProductUnitId,
                            "Purchase unit was not found or is not an active purchase unit for the product.");
                    }

                    purchaseUnitId = unit.Id;
                    purchaseUnitName = unit.DisplayName;
                    multiplier = unit.MultiplierToBase;
                }

                var unitCost = effectiveConnectedPrices is not null
                    ? effectiveConnectedPrices[l.ProductId]
                    : l.UnitPurchaseCost;

                lineDrafts.Add(new PurchaseOrderLineDraft(
                    CatalogProductId.From(l.ProductId),
                    l.OrderedQty,
                    unitCost,
                    l.LineNotes,
                    purchaseUnitId,
                    purchaseUnitName,
                    multiplier));
            }

            var po = PurchaseOrder.CreateDraft(
                org,
                supplierId,
                request.OrderDate,
                lineDrafts,
                utcNow,
                request.ExpectedDeliveryDate,
                request.SupplierReference,
                request.Notes,
                id: request.PurchaseOrderId is Guid poId && poId != Guid.Empty
                    ? PurchaseOrderId.From(poId)
                    : null,
                paymentTerm: ConnectedPoPaymentTerms.Parse(request.PaymentTerm),
                createdBy: actorId == Guid.Empty ? null : actorId);

            await _orders.AddAsync(po, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<PosPurchaseOrderDto>.Success(
                PurchaseMapper.Map(po, supplierName: supplier.Name, products: products));
        }
        catch (DomainException ex)
        {
            return ApplicationResult<PosPurchaseOrderDto>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<PosPurchaseOrderDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class UpdatePurchaseOrder
{
    private readonly IPurchaseOrderRepository _orders;
    private readonly ISupplierRepository _suppliers;
    private readonly ICatalogProductRepository _products;
    private readonly ICatalogProductUnitRepository _units;
    private readonly IConnectedSupplierRelationshipRepository _connectedRelationships;
    private readonly IBuyerSupplierProductLinkRepository _connectedLinks;
    private readonly ISupplierProductExposureRepository? _connectedExposures;
    private readonly IConnectedBuyerProductShareRepository? _connectedShares;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IPosCommercialAccessAccessor _access;
    private readonly TimeProvider _clock;

    public UpdatePurchaseOrder(
        IPurchaseOrderRepository orders,
        ISupplierRepository suppliers,
        ICatalogProductRepository products,
        ICatalogProductUnitRepository units,
        IConnectedSupplierRelationshipRepository connectedRelationships,
        IBuyerSupplierProductLinkRepository connectedLinks,
        IPosUnitOfWork unitOfWork,
        IPosCommercialAccessAccessor access,
        TimeProvider? clock = null,
        ISupplierProductExposureRepository? connectedExposures = null,
        IConnectedBuyerProductShareRepository? connectedShares = null)
    {
        _orders = orders;
        _suppliers = suppliers;
        _products = products;
        _units = units;
        _connectedRelationships = connectedRelationships;
        _connectedLinks = connectedLinks;
        _connectedExposures = connectedExposures;
        _connectedShares = connectedShares;
        _unitOfWork = unitOfWork;
        _access = access;
        _clock = clock ?? TimeProvider.System;
    }

    public async Task<ApplicationResult<PosPurchaseOrderDto>> ExecuteAsync(
        Guid organizationId,
        Guid purchaseOrderId,
        UpdatePurchaseOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        var gate = CommercialAccessGuard.Require(_access, UtangCapability.ManagePurchasing);
        if (!gate.IsSuccess)
        {
            return ApplicationResult<PosPurchaseOrderDto>.Failure(gate.ErrorCode!, gate.ErrorMessage!);
        }

        try
        {
            var org = PosOrganizationId.From(organizationId);
            var id = PurchaseOrderId.From(purchaseOrderId);
            var existing = await _orders.GetByIdAsync(org, id, cancellationToken).ConfigureAwait(false);
            if (existing is null)
            {
                return ApplicationResult<PosPurchaseOrderDto>.Failure(
                    ApplicationErrorCodes.PurchaseOrderNotFound,
                    "Purchase order was not found in this organization.");
            }

            if (existing.UpdatedAtUtc != request.ExpectedUpdatedAtUtc)
            {
                return ApplicationResult<PosPurchaseOrderDto>.Failure(
                    ApplicationErrorCodes.PurchaseOrderConcurrencyConflict,
                    "Purchase order was modified by another request. Refresh and retry.");
            }

            var supplierId = SupplierId.From(request.SupplierId);
            var supplierError = await PurchaseSupplierGuard
                .EnsureActiveSupplierAsync(_suppliers, org, supplierId, cancellationToken)
                .ConfigureAwait(false);
            if (supplierError is not null)
            {
                return ApplicationResult<PosPurchaseOrderDto>.Failure(supplierError.ErrorCode!, supplierError.ErrorMessage!);
            }

            var supplier = await _suppliers.GetByIdAsync(org, supplierId, cancellationToken).ConfigureAwait(false);
            if (supplier is null)
            {
                return ApplicationResult<PosPurchaseOrderDto>.Failure(
                    ApplicationErrorCodes.SupplierNotFound,
                    "Supplier was not found in this organization.");
            }

            var productIds = request.Lines.Select(l => l.ProductId).ToList();
            var (productError, products) = await PurchaseProductGuard
                .ResolveProductsAsync(_products, org, productIds, cancellationToken)
                .ConfigureAwait(false);
            if (productError is not null)
            {
                return ApplicationResult<PosPurchaseOrderDto>.Failure(productError.ErrorCode!, productError.ErrorMessage!);
            }

            var connectedEligibility = await ConnectedPurchaseOrderLineEligibility
                .ValidateIfConnectedAsync(
                    org,
                    supplier,
                    productIds,
                    _connectedRelationships,
                    _connectedLinks,
                    _connectedExposures,
                    _connectedShares,
                    cancellationToken)
                .ConfigureAwait(false);
            if (connectedEligibility is not null && !connectedEligibility.IsSuccess)
            {
                return ApplicationResult<PosPurchaseOrderDto>.Failure(
                    connectedEligibility.ErrorCode!,
                    connectedEligibility.ErrorMessage!);
            }

            var effectiveConnectedPrices = connectedEligibility?.Value?.EffectivePriceByBuyerProductId;

            var lineDrafts = new List<PurchaseOrderLineDraft>();
            foreach (var l in request.Lines)
            {
                ProductUnitId? purchaseUnitId = null;
                string? purchaseUnitName = null;
                var multiplier = 1m;
                if (l.PurchaseUnitId is not null)
                {
                    var unit = await _units
                        .GetByIdAsync(org, ProductUnitId.From(l.PurchaseUnitId.Value), cancellationToken)
                        .ConfigureAwait(false);
                    if (unit is null
                        || unit.ProductId.Value != l.ProductId
                        || !unit.IsActive
                        || unit.Kind != ProductUnitKind.Purchase)
                    {
                        return ApplicationResult<PosPurchaseOrderDto>.Failure(
                            DomainErrorCodes.InvalidProductUnitId,
                            "Purchase unit was not found or is not an active purchase unit for the product.");
                    }

                    purchaseUnitId = unit.Id;
                    purchaseUnitName = unit.DisplayName;
                    multiplier = unit.MultiplierToBase;
                }

                var unitCost = effectiveConnectedPrices is not null
                    ? effectiveConnectedPrices[l.ProductId]
                    : l.UnitPurchaseCost;

                lineDrafts.Add(new PurchaseOrderLineDraft(
                    CatalogProductId.From(l.ProductId),
                    l.OrderedQty,
                    unitCost,
                    l.LineNotes,
                    purchaseUnitId,
                    purchaseUnitName,
                    multiplier));
            }

            existing.UpdateDraft(
                supplierId,
                request.OrderDate,
                lineDrafts,
                _clock.GetUtcNow(),
                request.ExpectedDeliveryDate,
                request.SupplierReference,
                request.Notes,
                request.PaymentTerm is null ? null : ConnectedPoPaymentTerms.Parse(request.PaymentTerm));

            await _orders.UpdateAsync(existing, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<PosPurchaseOrderDto>.Success(
                PurchaseMapper.Map(existing, supplierName: supplier.Name, products: products));
        }
        catch (DomainException ex)
        {
            return ApplicationResult<PosPurchaseOrderDto>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<PosPurchaseOrderDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class SubmitPurchaseOrder
{
    private readonly IPurchaseOrderRepository _orders;
    private readonly ICatalogProductRepository _products;
    private readonly ISupplierRepository _suppliers;
    private readonly IConnectedSupplierRelationshipRepository _connectedRelationships;
    private readonly IBuyerSupplierProductLinkRepository _connectedLinks;
    private readonly IConnectedPurchaseOrderRepository _connectedOrders;
    private readonly ISupplierProductExposureRepository? _connectedExposures;
    private readonly IConnectedBuyerProductShareRepository? _connectedShares;
    private readonly IOrganizationBusinessNotificationPublisher _notifications;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IPosCommercialAccessAccessor _access;
    private readonly TimeProvider _clock;

    public SubmitPurchaseOrder(
        IPurchaseOrderRepository orders,
        ICatalogProductRepository products,
        ISupplierRepository suppliers,
        IConnectedSupplierRelationshipRepository connectedRelationships,
        IBuyerSupplierProductLinkRepository connectedLinks,
        IConnectedPurchaseOrderRepository connectedOrders,
        IPosUnitOfWork unitOfWork,
        IPosCommercialAccessAccessor access,
        TimeProvider? clock = null,
        ISupplierProductExposureRepository? connectedExposures = null,
        IConnectedBuyerProductShareRepository? connectedShares = null,
        IOrganizationBusinessNotificationPublisher? notifications = null)
    {
        _orders = orders;
        _products = products;
        _suppliers = suppliers;
        _connectedRelationships = connectedRelationships;
        _connectedLinks = connectedLinks;
        _connectedOrders = connectedOrders;
        _connectedExposures = connectedExposures;
        _connectedShares = connectedShares;
        _notifications = notifications ?? new NoOpOrganizationBusinessNotificationPublisher();
        _unitOfWork = unitOfWork;
        _access = access;
        _clock = clock ?? TimeProvider.System;
    }

    public async Task<ApplicationResult<PosPurchaseOrderDto>> ExecuteAsync(
        Guid organizationId,
        Guid purchaseOrderId,
        Guid actorId,
        CancellationToken cancellationToken = default)
    {
        var gate = CommercialAccessGuard.Require(_access, UtangCapability.ManagePurchasing);
        if (!gate.IsSuccess)
        {
            return ApplicationResult<PosPurchaseOrderDto>.Failure(gate.ErrorCode!, gate.ErrorMessage!);
        }

        if (actorId == Guid.Empty)
        {
            return ApplicationResult<PosPurchaseOrderDto>.Failure(
                ApplicationErrorCodes.ActorRequired,
                "An actor identifier is required to submit a purchase order.");
        }

        try
        {
            var org = PosOrganizationId.From(organizationId);
            var id = PurchaseOrderId.From(purchaseOrderId);
            var existing = await _orders.GetByIdAsync(org, id, cancellationToken).ConfigureAwait(false);
            if (existing is null)
            {
                return ApplicationResult<PosPurchaseOrderDto>.Failure(
                    ApplicationErrorCodes.PurchaseOrderNotFound,
                    "Purchase order was not found in this organization.");
            }

            // Idempotent retry: already Ordered with existing connected order → success.
            if (existing.Status == PurchaseOrderStatus.Ordered)
            {
                var existingConnected = await _connectedOrders
                    .GetByBuyerPurchaseOrderAsync(id, cancellationToken)
                    .ConfigureAwait(false);
                if (existingConnected is not null)
                {
                    return ApplicationResult<PosPurchaseOrderDto>.Success(
                        PurchaseMapper.Map(existing, existingConnected));
                }

                var supplierProbe = await _suppliers.GetByIdAsync(org, existing.SupplierId, cancellationToken)
                    .ConfigureAwait(false);
                if (supplierProbe?.ConnectionType == SupplierConnectionType.ConnectedOrganization)
                {
                    return ApplicationResult<PosPurchaseOrderDto>.Failure(
                        ConnectedSupplierErrorCodes.IncomingOrderNotFound,
                        "Connected purchase order delivery is incomplete. Retry submission.");
                }

                return ApplicationResult<PosPurchaseOrderDto>.Success(PurchaseMapper.Map(existing));
            }

            var productIds = existing.Lines.Select(l => l.ProductId.Value).ToList();
            var (productError, products) = await PurchaseProductGuard
                .ResolveProductsAsync(_products, org, productIds, cancellationToken)
                .ConfigureAwait(false);
            if (productError is not null)
            {
                return ApplicationResult<PosPurchaseOrderDto>.Failure(productError.ErrorCode!, productError.ErrorMessage!);
            }

            var utcNow = _clock.GetUtcNow();
            var businessDate = PurchaseOrderNumbers.BusinessDateOf(utcNow);
            var snapshots = existing.Lines
                .OrderBy(l => l.LineNumber)
                .Select(l =>
                {
                    var product = products[l.ProductId.Value];
                    return new PurchaseOrderLineSnapshotInput(
                        l.ProductId,
                        product.Name,
                        product.UnitOfMeasure,
                        l.OrderedQty,
                        l.UnitPurchaseCost,
                        l.LineNotes,
                        product.SellingMode);
                })
                .ToList();

            ConnectedSupplierRelationship? connectedRelationship = null;
            Dictionary<Guid, BuyerSupplierProductLink>? connectedLinksByBuyerProduct = null;
            Dictionary<Guid, decimal>? connectedPricesByBuyerProduct = null;
            ConnectedPurchaseOrder? createdConnected = null;
            var supplier = await _suppliers.GetByIdAsync(org, existing.SupplierId, cancellationToken).ConfigureAwait(false);
            if (supplier is null)
            {
                return ApplicationResult<PosPurchaseOrderDto>.Failure(
                    ApplicationErrorCodes.SupplierNotFound,
                    "Supplier was not found in this organization.");
            }

            var connectedEligibility = await ConnectedPurchaseOrderLineEligibility
                .ValidateIfConnectedAsync(
                    org,
                    supplier,
                    productIds,
                    _connectedRelationships,
                    _connectedLinks,
                    _connectedExposures,
                    _connectedShares,
                    cancellationToken)
                .ConfigureAwait(false);
            if (connectedEligibility is not null && !connectedEligibility.IsSuccess)
            {
                return ApplicationResult<PosPurchaseOrderDto>.Failure(
                    connectedEligibility.ErrorCode!,
                    connectedEligibility.ErrorMessage!);
            }

            if (connectedEligibility?.Value is { } connectedOutcome)
            {
                connectedRelationship = connectedOutcome.Relationship;
                connectedLinksByBuyerProduct = connectedOutcome.LinksByBuyerProductId
                    .Where(x => productIds.Contains(x.Key))
                    .ToDictionary(x => x.Key, x => x.Value);
                connectedPricesByBuyerProduct = connectedOutcome.EffectivePriceByBuyerProductId
                    .ToDictionary(x => x.Key, x => x.Value);
            }

            var submitted = await _orders.SubmitAsync(
                    org,
                    id,
                    businessDate,
                    poNumber =>
                    {
                        existing.Submit(poNumber, snapshots, actorId, utcNow);
                        return existing;
                    },
                    connectedRelationship is null
                        ? null
                        : async (po, ct) =>
                        {
                            var already = await _connectedOrders.GetByBuyerPurchaseOrderAsync(po.Id, ct).ConfigureAwait(false);
                            if (already is not null)
                            {
                                createdConnected = already;
                                return;
                            }

                            var connectedLines = po.Lines.OrderBy(x => x.LineNumber).Select(x =>
                            {
                                var link = connectedLinksByBuyerProduct![x.ProductId.Value];
                                return ConnectedPurchaseOrderLine.Create(
                                    link.SupplierProductId,
                                    link.SupplierNameSnapshot,
                                    link.SupplierSkuSnapshot,
                                    x.OrderedQty,
                                    connectedPricesByBuyerProduct![x.ProductId.Value],
                                    link.UnitOfMeasureCode);
                            }).ToList();
                            createdConnected = ConnectedPurchaseOrder.CreateFromBuyerSubmission(
                                connectedRelationship, po.Id, po.PoNumber, po.OrderDate, po.Notes, connectedLines, utcNow,
                                paymentTerm: po.PaymentTerm);
                            await _connectedOrders.AddAsync(createdConnected, ct).ConfigureAwait(false);
                        },
                    cancellationToken)
                .ConfigureAwait(false);

            if (createdConnected is null && connectedRelationship is not null)
            {
                createdConnected = await _connectedOrders
                    .GetByBuyerPurchaseOrderAsync(submitted.Id, cancellationToken)
                    .ConfigureAwait(false);
            }

            if (createdConnected is not null)
            {
                var buyerName = connectedRelationship?.BuyerDisplayNameSnapshot ?? "Buyer";
                var poLabel = submitted.PoNumber ?? submitted.Id.Value.ToString("D");
                await _notifications.PublishAsync(
                    org.Value,
                    createdConnected.SupplierOrganizationId.Value,
                    ConnectedPurchaseOrderNotificationTypes.Submitted,
                    createdConnected.Id.Value.ToString("D"),
                    "New purchase order",
                    $"{buyerName} submitted PO {poLabel}.",
                    cancellationToken).ConfigureAwait(false);
            }

            return ApplicationResult<PosPurchaseOrderDto>.Success(
                await PurchaseMapper.MapWithNamesAsync(
                    submitted,
                    createdConnected,
                    _suppliers,
                    _products,
                    cancellationToken).ConfigureAwait(false));
        }
        catch (DomainException ex)
        {
            return ApplicationResult<PosPurchaseOrderDto>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<PosPurchaseOrderDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class CancelPurchaseOrder
{
    private readonly IPurchaseOrderRepository _orders;
    private readonly IConnectedPurchaseOrderRepository _connectedOrders;
    private readonly IOrganizationBusinessNotificationPublisher _notifications;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IPosCommercialAccessAccessor _access;
    private readonly TimeProvider _clock;

    public CancelPurchaseOrder(
        IPurchaseOrderRepository orders,
        IPosUnitOfWork unitOfWork,
        IPosCommercialAccessAccessor access,
        IConnectedPurchaseOrderRepository connectedOrders,
        TimeProvider? clock = null,
        IOrganizationBusinessNotificationPublisher? notifications = null)
    {
        _orders = orders;
        _connectedOrders = connectedOrders;
        _notifications = notifications ?? new NoOpOrganizationBusinessNotificationPublisher();
        _unitOfWork = unitOfWork;
        _access = access;
        _clock = clock ?? TimeProvider.System;
    }

    public async Task<ApplicationResult<PosPurchaseOrderDto>> ExecuteAsync(
        Guid organizationId,
        Guid purchaseOrderId,
        CancellationToken cancellationToken = default)
    {
        var gate = CommercialAccessGuard.Require(_access, UtangCapability.ManagePurchasing);
        if (!gate.IsSuccess)
        {
            return ApplicationResult<PosPurchaseOrderDto>.Failure(gate.ErrorCode!, gate.ErrorMessage!);
        }

        try
        {
            var org = PosOrganizationId.From(organizationId);
            var id = PurchaseOrderId.From(purchaseOrderId);
            var existing = await _orders.GetByIdAsync(org, id, cancellationToken).ConfigureAwait(false);
            if (existing is null)
            {
                return ApplicationResult<PosPurchaseOrderDto>.Failure(
                    ApplicationErrorCodes.PurchaseOrderNotFound,
                    "Purchase order was not found in this organization.");
            }

            ConnectedPurchaseOrder? connected = await _connectedOrders
                .GetByBuyerPurchaseOrderAsync(id, cancellationToken)
                .ConfigureAwait(false);
            if (connected is not null && connected.BuyerOrganizationId != org)
            {
                return ApplicationResult<PosPurchaseOrderDto>.Failure(
                    ApplicationErrorCodes.PurchaseOrderNotFound,
                    "Purchase order was not found in this organization.");
            }

            var utcNow = _clock.GetUtcNow();
            var rejectedProposedChanges = false;
            if (connected is not null)
            {
                if (!connected.CanBuyerWithdraw)
                {
                    return ApplicationResult<PosPurchaseOrderDto>.Failure(
                        ConnectedSupplierDomainErrorCodes.InvalidTransition,
                        "This connected purchase order can no longer be withdrawn after the supplier responded.");
                }

                rejectedProposedChanges = connected.Status == ConnectedPurchaseOrderStatus.ChangesProposed;
                if (rejectedProposedChanges)
                {
                    connected.RejectProposedChanges(utcNow);
                }
                else
                {
                    connected.WithdrawByBuyer(utcNow);
                }

                await _connectedOrders.UpdateAsync(connected, cancellationToken).ConfigureAwait(false);
            }

            existing.Cancel(utcNow);
            await _orders.UpdateAsync(existing, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            if (connected is not null)
            {
                var poLabel = existing.PoNumber ?? existing.Id.Value.ToString("D");
                await _notifications.PublishAsync(
                    org.Value,
                    connected.SupplierOrganizationId.Value,
                    rejectedProposedChanges
                        ? ConnectedPurchaseOrderNotificationTypes.ChangesRejected
                        : ConnectedPurchaseOrderNotificationTypes.Withdrawn,
                    connected.Id.Value.ToString("D"),
                    rejectedProposedChanges ? "Purchase order changes rejected" : "Purchase order withdrawn",
                    rejectedProposedChanges
                        ? $"Buyer rejected proposed changes for PO {poLabel}."
                        : $"Buyer withdrew PO {poLabel}.",
                    cancellationToken).ConfigureAwait(false);
            }

            return ApplicationResult<PosPurchaseOrderDto>.Success(PurchaseMapper.Map(existing, connected));
        }
        catch (DomainException ex)
        {
            return ApplicationResult<PosPurchaseOrderDto>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<PosPurchaseOrderDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class AcceptConnectedPoChanges
{
    private readonly IPurchaseOrderRepository _orders;
    private readonly IConnectedPurchaseOrderRepository _connectedOrders;
    private readonly IBuyerSupplierProductLinkRepository _links;
    private readonly IOrganizationBusinessNotificationPublisher _notifications;
    private readonly IConnectedSupplierRelationshipRepository _relationships;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IPosCommercialAccessAccessor _access;
    private readonly TimeProvider _clock;

    public AcceptConnectedPoChanges(
        IPurchaseOrderRepository orders,
        IConnectedPurchaseOrderRepository connectedOrders,
        IBuyerSupplierProductLinkRepository links,
        IPosUnitOfWork unitOfWork,
        IPosCommercialAccessAccessor access,
        TimeProvider? clock = null,
        IOrganizationBusinessNotificationPublisher? notifications = null,
        IConnectedSupplierRelationshipRepository? relationships = null)
    {
        _orders = orders;
        _connectedOrders = connectedOrders;
        _links = links;
        _unitOfWork = unitOfWork;
        _access = access;
        _clock = clock ?? TimeProvider.System;
        _notifications = notifications ?? new NoOpOrganizationBusinessNotificationPublisher();
        _relationships = relationships!;
    }

    public async Task<ApplicationResult<PosPurchaseOrderDto>> ExecuteAsync(
        Guid organizationId,
        Guid purchaseOrderId,
        Guid actorId,
        CancellationToken cancellationToken = default)
    {
        var gate = CommercialAccessGuard.Require(_access, UtangCapability.ManagePurchasing);
        if (!gate.IsSuccess)
        {
            return ApplicationResult<PosPurchaseOrderDto>.Failure(gate.ErrorCode!, gate.ErrorMessage!);
        }

        try
        {
            var org = PosOrganizationId.From(organizationId);
            var id = PurchaseOrderId.From(purchaseOrderId);
            var existing = await _orders.GetByIdAsync(org, id, cancellationToken).ConfigureAwait(false);
            if (existing is null)
            {
                return ApplicationResult<PosPurchaseOrderDto>.Failure(
                    ApplicationErrorCodes.PurchaseOrderNotFound,
                    "Purchase order was not found in this organization.");
            }

            var connected = await _connectedOrders
                .GetByBuyerPurchaseOrderAsync(id, cancellationToken)
                .ConfigureAwait(false);
            if (connected is null || connected.BuyerOrganizationId != org)
            {
                return ApplicationResult<PosPurchaseOrderDto>.Failure(
                    ConnectedSupplierErrorCodes.IncomingOrderNotFound,
                    "Connected purchase order was not found.");
            }

            var utcNow = _clock.GetUtcNow();
            if (connected.Status == ConnectedPurchaseOrderStatus.Accepted)
            {
                return ApplicationResult<PosPurchaseOrderDto>.Success(PurchaseMapper.Map(existing, connected));
            }

            connected.AcceptProposedChanges(utcNow, actorId == Guid.Empty ? null : actorId);
            await ConnectedPoConfirmation
                .AlignBuyerOutstandingAsync(existing, connected, _links, utcNow, cancellationToken)
                .ConfigureAwait(false);

            await _connectedOrders.UpdateAsync(connected, cancellationToken).ConfigureAwait(false);
            await _orders.UpdateAsync(existing, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            var poLabel = existing.PoNumber ?? existing.Id.Value.ToString("D");
            string? buyerName = null;
            if (_relationships is not null)
            {
                var rel = await _relationships.GetAsync(connected.RelationshipId, cancellationToken).ConfigureAwait(false);
                buyerName = rel?.BuyerDisplayNameSnapshot;
            }

            await _notifications.PublishAsync(
                org.Value,
                connected.SupplierOrganizationId.Value,
                ConnectedPurchaseOrderNotificationTypes.ChangesAccepted,
                connected.Id.Value.ToString("D"),
                "Purchase order changes accepted",
                $"{buyerName ?? "Buyer"} accepted revised quantities for PO {poLabel}.",
                cancellationToken).ConfigureAwait(false);

            return ApplicationResult<PosPurchaseOrderDto>.Success(PurchaseMapper.Map(existing, connected));
        }
        catch (DomainException ex)
        {
            return ApplicationResult<PosPurchaseOrderDto>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<PosPurchaseOrderDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class ReceivePurchaseOrder
{
    private readonly IPurchaseOrderRepository _orders;
    private readonly ICatalogProductRepository _products;
    private readonly IPurchaseStockService _purchaseStock;
    private readonly IConnectedPurchaseOrderRepository _connectedOrders;
    private readonly IBuyerSupplierProductLinkRepository? _links;
    private readonly IOrganizationBusinessNotificationPublisher _notifications;
    private readonly IPosCommercialAccessAccessor _access;
    private readonly TimeProvider _clock;

    public ReceivePurchaseOrder(
        IPurchaseOrderRepository orders,
        ICatalogProductRepository products,
        IPurchaseStockService purchaseStock,
        IPosCommercialAccessAccessor access,
        IConnectedPurchaseOrderRepository connectedOrders,
        TimeProvider? clock = null,
        IOrganizationBusinessNotificationPublisher? notifications = null,
        IBuyerSupplierProductLinkRepository? links = null)
    {
        _orders = orders;
        _products = products;
        _purchaseStock = purchaseStock;
        _connectedOrders = connectedOrders;
        _notifications = notifications ?? new NoOpOrganizationBusinessNotificationPublisher();
        _links = links;
        _access = access;
        _clock = clock ?? TimeProvider.System;
    }

    public async Task<ApplicationResult<PosGoodsReceiptDto>> ExecuteAsync(
        Guid organizationId,
        Guid purchaseOrderId,
        ReceivePurchaseOrderRequest request,
        Guid actorId,
        CancellationToken cancellationToken = default)
    {
        var gate = CommercialAccessGuard.Require(_access, UtangCapability.ManagePurchasing);
        if (!gate.IsSuccess)
        {
            return ApplicationResult<PosGoodsReceiptDto>.Failure(gate.ErrorCode!, gate.ErrorMessage!);
        }

        if (actorId == Guid.Empty)
        {
            return ApplicationResult<PosGoodsReceiptDto>.Failure(
                ApplicationErrorCodes.ActorRequired,
                "An actor identifier is required to receive goods.");
        }

        try
        {
            var org = PosOrganizationId.From(organizationId);
            var id = PurchaseOrderId.From(purchaseOrderId);
            var existing = await _orders.GetByIdAsync(org, id, cancellationToken).ConfigureAwait(false);
            if (existing is null)
            {
                return ApplicationResult<PosGoodsReceiptDto>.Failure(
                    ApplicationErrorCodes.PurchaseOrderNotFound,
                    "Purchase order was not found in this organization.");
            }

            var connected = await _connectedOrders
                .GetByBuyerPurchaseOrderAsync(id, cancellationToken)
                .ConfigureAwait(false);
            if (connected is not null)
            {
                if (connected.BuyerOrganizationId != org)
                {
                    return ApplicationResult<PosGoodsReceiptDto>.Failure(
                        ApplicationErrorCodes.PurchaseOrderNotFound,
                        "Purchase order was not found in this organization.");
                }

                if (!connected.CanBuyerReceive)
                {
                    return ApplicationResult<PosGoodsReceiptDto>.Failure(
                        ConnectedSupplierDomainErrorCodes.InvalidTransition,
                        "Goods receipt is only allowed after the supplier accepts the connected order.");
                }

                if (_links is not null)
                {
                    foreach (var line in request.Lines)
                    {
                        var remaining = await ConnectedPoConfirmation
                            .RemainingConfirmedQtyAsync(
                                existing,
                                connected,
                                CatalogProductId.From(line.ProductId),
                                _links,
                                cancellationToken)
                            .ConfigureAwait(false);
                        if (remaining is decimal cap && line.ReceiveQty > cap)
                        {
                            return ApplicationResult<PosGoodsReceiptDto>.Failure(
                                DomainErrorCodes.PurchaseOverReceipt,
                                "Receive quantity cannot exceed the supplier-confirmed remaining quantity.");
                        }
                    }
                }
            }

            var productIds = request.Lines.Select(l => l.ProductId).Distinct().ToList();
            var (productError, products) = await PurchaseProductGuard
                .ResolveProductsAsync(_products, org, productIds, cancellationToken)
                .ConfigureAwait(false);
            if (productError is not null)
            {
                return ApplicationResult<PosGoodsReceiptDto>.Failure(
                    productError.ErrorCode!,
                    productError.ErrorMessage!);
            }

            var receiveLines = request.Lines
                .Select(l =>
                {
                    var product = products[l.ProductId];
                    var kind = ConnectedPoReceivingDiscrepancyKind.None;
                    if (!string.IsNullOrWhiteSpace(l.DiscrepancyKind)
                        && Enum.TryParse<ConnectedPoReceivingDiscrepancyKind>(l.DiscrepancyKind, true, out var parsed))
                    {
                        kind = parsed;
                    }

                    return new PurchaseOrderReceiveLineDraft(
                        CatalogProductId.From(l.ProductId),
                        l.ReceiveQty,
                        product.SellingMode,
                        l.DamagedQty,
                        l.RejectedQty,
                        l.ShortClosedQty,
                        kind,
                        l.DiscrepancyNote);
                })
                .ToList();

            var utcNow = _clock.GetUtcNow();
            var businessDate = GoodsReceiptNumbers.BusinessDateOf(utcNow);

            var (_, receipt) = await _orders.ReceiveAsync(
                    org,
                    id,
                    businessDate,
                    grnNumber =>
                    {
                        existing.ApplyReceiptLines(receiveLines, utcNow);
                        var grn = GoodsReceipt.Create(
                            org,
                            id,
                            grnNumber,
                            existing,
                            receiveLines,
                            actorId,
                            utcNow,
                            receivedDate: request.ReceivedDate,
                            deliveryReference: request.DeliveryReference,
                            notes: request.Notes,
                            id: request.GoodsReceiptId is Guid grnId && grnId != Guid.Empty
                                ? GoodsReceiptId.From(grnId)
                                : null);
                        return (existing, grn);
                    },
                    async (grn, po, ct) =>
                        await _purchaseStock
                            .ApplyReceiptAsync(org, grn, po, actorId, utcNow, ct)
                            .ConfigureAwait(false),
                    cancellationToken)
                .ConfigureAwait(false);

            if (connected is not null)
            {
                var poLabel = existing.PoNumber ?? existing.Id.Value.ToString("D");
                var hasIssues = receipt.Lines.Any(l =>
                    l.DamagedQty > 0m || l.RejectedQty > 0m || l.ShortClosedQty > 0m
                    || l.DiscrepancyKind != ConnectedPoReceivingDiscrepancyKind.None)
                    || existing.HasReceivingIssues;
                var relatedType = hasIssues
                    ? ConnectedPurchaseOrderNotificationTypes.ReceivingIssue
                    : existing.Status == PurchaseOrderStatus.Received
                        ? ConnectedPurchaseOrderNotificationTypes.Received
                        : ConnectedPurchaseOrderNotificationTypes.PartiallyReceived;
                await _notifications.PublishAsync(
                    org.Value,
                    connected.SupplierOrganizationId.Value,
                    relatedType,
                    connected.Id.Value.ToString("D"),
                    hasIssues
                        ? "Receiving issue reported"
                        : existing.Status == PurchaseOrderStatus.Received
                            ? "Purchase order received"
                            : "Purchase order partially received",
                    hasIssues
                        ? $"Buyer reported a receiving issue for PO {poLabel}."
                        : existing.Status == PurchaseOrderStatus.Received
                            ? $"Buyer received PO {poLabel}."
                            : $"Buyer recorded a partial receipt for PO {poLabel}.",
                    cancellationToken).ConfigureAwait(false);
            }

            return ApplicationResult<PosGoodsReceiptDto>.Success(PurchaseMapper.Map(receipt));
        }
        catch (DomainException ex)
        {
            return ApplicationResult<PosGoodsReceiptDto>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<PosGoodsReceiptDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}
