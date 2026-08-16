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
    bool CanReceiveConnected = true);

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
    string? Notes = null);

public sealed record UpdatePurchaseOrderRequest(
    Guid SupplierId,
    DateOnly OrderDate,
    IReadOnlyList<CreatePurchaseOrderLineRequest> Lines,
    DateTimeOffset ExpectedUpdatedAtUtc,
    DateOnly? ExpectedDeliveryDate = null,
    string? SupplierReference = null,
    string? Notes = null);

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
    public static PosPurchaseOrderDto Map(PurchaseOrder po, ConnectedPurchaseOrder? connected = null)
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
            po.Lines.Select(MapLine).ToList(),
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
            CanReceiveConnected: connected is null || connected.CanBuyerReceive);
    }

    public static PosPurchaseOrderLineDto MapLine(PurchaseOrderLine line) =>
        new(
            line.Id.Value,
            line.ProductId.Value,
            line.LineNumber,
            line.NameSnapshot,
            line.UomSnapshot?.ToString(),
            line.OrderedQty,
            line.UnitPurchaseCost,
            line.LineTotal,
            line.ReceivedQty,
            line.OutstandingQty,
            line.LineNotes,
            line.ClosedShortQty);

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

    public PurchaseOrderQueryService(
        IPurchaseOrderRepository orders,
        IConnectedPurchaseOrderRepository connectedOrders)
    {
        _orders = orders;
        _connectedOrders = connectedOrders;
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

        return PurchaseMapper.Map(po, connected);
    }

    public async Task<PagedResult<PosPurchaseOrderDto>> ListAsync(
        Guid organizationId,
        PurchaseOrderFilter filter,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken = default)
    {
        var (skip, take) = PosPagination.Normalize(page, pageSize);
        var (items, total) = await _orders
            .ListAsync(PosOrganizationId.From(organizationId), filter, skip, take, cancellationToken)
            .ConfigureAwait(false);

        var mapped = new List<PosPurchaseOrderDto>(items.Count);
        foreach (var po in items)
        {
            var connected = await _connectedOrders
                .GetByBuyerPurchaseOrderAsync(po.Id, cancellationToken)
                .ConfigureAwait(false);
            if (connected is not null
                && connected.BuyerOrganizationId != PosOrganizationId.From(organizationId))
            {
                connected = null;
            }

            mapped.Add(PurchaseMapper.Map(po, connected));
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

public sealed class CreatePurchaseOrder
{
    private readonly IPurchaseOrderRepository _orders;
    private readonly ISupplierRepository _suppliers;
    private readonly ICatalogProductRepository _products;
    private readonly ICatalogProductUnitRepository _units;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IPosCommercialAccessAccessor _access;
    private readonly TimeProvider _clock;

    public CreatePurchaseOrder(
        IPurchaseOrderRepository orders,
        ISupplierRepository suppliers,
        ICatalogProductRepository products,
        ICatalogProductUnitRepository units,
        IPosUnitOfWork unitOfWork,
        IPosCommercialAccessAccessor access,
        TimeProvider? clock = null)
    {
        _orders = orders;
        _suppliers = suppliers;
        _products = products;
        _units = units;
        _unitOfWork = unitOfWork;
        _access = access;
        _clock = clock ?? TimeProvider.System;
    }

    public async Task<ApplicationResult<PosPurchaseOrderDto>> ExecuteAsync(
        Guid organizationId,
        CreatePurchaseOrderRequest request,
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
            var supplierId = SupplierId.From(request.SupplierId);
            var supplierError = await PurchaseSupplierGuard
                .EnsureActiveSupplierAsync(_suppliers, org, supplierId, cancellationToken)
                .ConfigureAwait(false);
            if (supplierError is not null)
            {
                return ApplicationResult<PosPurchaseOrderDto>.Failure(supplierError.ErrorCode!, supplierError.ErrorMessage!);
            }

            var productIds = request.Lines.Select(l => l.ProductId).ToList();
            var (productError, _) = await PurchaseProductGuard
                .ResolveProductsAsync(_products, org, productIds, cancellationToken)
                .ConfigureAwait(false);
            if (productError is not null)
            {
                return ApplicationResult<PosPurchaseOrderDto>.Failure(productError.ErrorCode!, productError.ErrorMessage!);
            }

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

                lineDrafts.Add(new PurchaseOrderLineDraft(
                    CatalogProductId.From(l.ProductId),
                    l.OrderedQty,
                    l.UnitPurchaseCost,
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
                request.Notes);

            await _orders.AddAsync(po, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<PosPurchaseOrderDto>.Success(PurchaseMapper.Map(po));
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
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IPosCommercialAccessAccessor _access;
    private readonly TimeProvider _clock;

    public UpdatePurchaseOrder(
        IPurchaseOrderRepository orders,
        ISupplierRepository suppliers,
        ICatalogProductRepository products,
        ICatalogProductUnitRepository units,
        IPosUnitOfWork unitOfWork,
        IPosCommercialAccessAccessor access,
        TimeProvider? clock = null)
    {
        _orders = orders;
        _suppliers = suppliers;
        _products = products;
        _units = units;
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

            var productIds = request.Lines.Select(l => l.ProductId).ToList();
            var (productError, _) = await PurchaseProductGuard
                .ResolveProductsAsync(_products, org, productIds, cancellationToken)
                .ConfigureAwait(false);
            if (productError is not null)
            {
                return ApplicationResult<PosPurchaseOrderDto>.Failure(productError.ErrorCode!, productError.ErrorMessage!);
            }

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

                lineDrafts.Add(new PurchaseOrderLineDraft(
                    CatalogProductId.From(l.ProductId),
                    l.OrderedQty,
                    l.UnitPurchaseCost,
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
                request.Notes);

            await _orders.UpdateAsync(existing, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<PosPurchaseOrderDto>.Success(PurchaseMapper.Map(existing));
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
            if (supplier?.ConnectionType == SupplierConnectionType.ConnectedOrganization)
            {
                if (supplier.ConnectedRelationshipId is null || _connectedExposures is null || _connectedShares is null)
                {
                    return ApplicationResult<PosPurchaseOrderDto>.Failure(
                        ConnectedSupplierErrorCodes.RelationshipInactive,
                        "The connected supplier relationship is not available.");
                }

                connectedRelationship = await _connectedRelationships.GetAsync(supplier.ConnectedRelationshipId, cancellationToken).ConfigureAwait(false);
                if (connectedRelationship is null || connectedRelationship.Status != ConnectedSupplierRelationshipStatus.Active
                    || connectedRelationship.BuyerOrganizationId != org)
                {
                    return ApplicationResult<PosPurchaseOrderDto>.Failure(
                        ConnectedSupplierErrorCodes.RelationshipInactive,
                        "The connected supplier relationship is not active.");
                }

                var links = await _connectedLinks.ListAsync(connectedRelationship.Id, org, cancellationToken).ConfigureAwait(false);
                connectedLinksByBuyerProduct = links.Where(x => x.IsActive).ToDictionary(x => x.BuyerProductId.Value);
                connectedPricesByBuyerProduct = [];
                foreach (var line in existing.Lines)
                {
                    if (!connectedLinksByBuyerProduct.TryGetValue(line.ProductId.Value, out var link))
                    {
                        return ApplicationResult<PosPurchaseOrderDto>.Failure(
                            ConnectedSupplierErrorCodes.LinkNotFound,
                            "Every connected purchase-order line must have an active supplier product link.");
                    }

                    var exposure = await _connectedExposures.GetByProductAsync(
                        connectedRelationship.SupplierOrganizationId, link.SupplierProductId, cancellationToken).ConfigureAwait(false);
                    var share = await _connectedShares.FindAsync(connectedRelationship.Id, link.SupplierProductId, cancellationToken).ConfigureAwait(false);
                    if (exposure is null || !ConnectedPoPricing.TryResolveEffectivePrice(exposure, share, out var effectivePrice))
                    {
                        return ApplicationResult<PosPurchaseOrderDto>.Failure(
                            ConnectedSupplierErrorCodes.ExposureNotFound,
                            "A connected supplier product is no longer shared or orderable.");
                    }
                    connectedPricesByBuyerProduct[line.ProductId.Value] = effectivePrice;
                }
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
                                connectedRelationship, po.Id, po.PoNumber, po.OrderDate, po.Notes, connectedLines, utcNow);
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

            return ApplicationResult<PosPurchaseOrderDto>.Success(PurchaseMapper.Map(submitted, createdConnected));
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
            if (connected is not null)
            {
                if (!connected.CanBuyerWithdraw)
                {
                    return ApplicationResult<PosPurchaseOrderDto>.Failure(
                        ConnectedSupplierDomainErrorCodes.InvalidTransition,
                        "This connected purchase order can no longer be withdrawn after the supplier responded.");
                }

                connected.WithdrawByBuyer(utcNow);
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
                    ConnectedPurchaseOrderNotificationTypes.Withdrawn,
                    connected.Id.Value.ToString("D"),
                    "Purchase order withdrawn",
                    $"Buyer withdrew PO {poLabel}.",
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

public sealed class ReceivePurchaseOrder
{
    private readonly IPurchaseOrderRepository _orders;
    private readonly ICatalogProductRepository _products;
    private readonly IPurchaseStockService _purchaseStock;
    private readonly IConnectedPurchaseOrderRepository _connectedOrders;
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
        IOrganizationBusinessNotificationPublisher? notifications = null)
    {
        _orders = orders;
        _products = products;
        _purchaseStock = purchaseStock;
        _connectedOrders = connectedOrders;
        _notifications = notifications ?? new NoOpOrganizationBusinessNotificationPublisher();
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
                await _notifications.PublishAsync(
                    org.Value,
                    connected.SupplierOrganizationId.Value,
                    hasIssues
                        ? ConnectedPurchaseOrderNotificationTypes.ReceivingIssue
                        : ConnectedPurchaseOrderNotificationTypes.Received,
                    connected.Id.Value.ToString("D"),
                    hasIssues ? "Receiving issue reported" : "Purchase order received",
                    hasIssues
                        ? $"Buyer reported a receiving issue for PO {poLabel}."
                        : $"Buyer received PO {poLabel}.",
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
