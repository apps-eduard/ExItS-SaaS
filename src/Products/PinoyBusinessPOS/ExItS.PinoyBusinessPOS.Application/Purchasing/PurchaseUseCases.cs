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
    string? LineNotes);

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
    IReadOnlyList<PosPurchaseOrderLineDto> Lines);

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
    Guid? InventoryMovementId)
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
    string? LineNotes = null);

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
    decimal ReceiveQty);

public sealed record ReceivePurchaseOrderRequest(
    IReadOnlyList<ReceivePurchaseOrderLineRequest> Lines,
    Guid? GoodsReceiptId = null,
    DateOnly? ReceivedDate = null,
    string? DeliveryReference = null,
    string? Notes = null);

public static class PurchaseMapper
{
    public static PosPurchaseOrderDto Map(PurchaseOrder po) =>
        new(
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
            po.Lines.Select(MapLine).ToList());

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
            line.LineNotes);

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
                l.InventoryMovementId)).ToList());
}

public sealed class PurchaseOrderQueryService
{
    private readonly IPurchaseOrderRepository _orders;

    public PurchaseOrderQueryService(IPurchaseOrderRepository orders) => _orders = orders;

    public async Task<PosPurchaseOrderDto?> GetByIdAsync(
        Guid organizationId,
        Guid purchaseOrderId,
        CancellationToken cancellationToken = default)
    {
        var po = await _orders
            .GetByIdAsync(PosOrganizationId.From(organizationId), PurchaseOrderId.From(purchaseOrderId), cancellationToken)
            .ConfigureAwait(false);
        return po is null ? null : PurchaseMapper.Map(po);
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
        return new PagedResult<PosPurchaseOrderDto>(
            items.Select(PurchaseMapper.Map).ToList(),
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
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IPosCommercialAccessAccessor _access;
    private readonly TimeProvider _clock;

    public CreatePurchaseOrder(
        IPurchaseOrderRepository orders,
        ISupplierRepository suppliers,
        ICatalogProductRepository products,
        IPosUnitOfWork unitOfWork,
        IPosCommercialAccessAccessor access,
        TimeProvider? clock = null)
    {
        _orders = orders;
        _suppliers = suppliers;
        _products = products;
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
            var lineDrafts = request.Lines
                .Select(l => new PurchaseOrderLineDraft(
                    CatalogProductId.From(l.ProductId),
                    l.OrderedQty,
                    l.UnitPurchaseCost,
                    l.LineNotes))
                .ToList();

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
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IPosCommercialAccessAccessor _access;
    private readonly TimeProvider _clock;

    public UpdatePurchaseOrder(
        IPurchaseOrderRepository orders,
        ISupplierRepository suppliers,
        ICatalogProductRepository products,
        IPosUnitOfWork unitOfWork,
        IPosCommercialAccessAccessor access,
        TimeProvider? clock = null)
    {
        _orders = orders;
        _suppliers = suppliers;
        _products = products;
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

            var lineDrafts = request.Lines
                .Select(l => new PurchaseOrderLineDraft(
                    CatalogProductId.From(l.ProductId),
                    l.OrderedQty,
                    l.UnitPurchaseCost,
                    l.LineNotes))
                .ToList();

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
        TimeProvider? clock = null)
    {
        _orders = orders;
        _products = products;
        _suppliers = suppliers;
        _connectedRelationships = connectedRelationships;
        _connectedLinks = connectedLinks;
        _connectedOrders = connectedOrders;
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

            var submitted = await _orders.SubmitAsync(
                    org,
                    id,
                    businessDate,
                    poNumber =>
                    {
                        existing.Submit(poNumber, snapshots, actorId, utcNow);
                        return existing;
                    },
                    cancellationToken)
                .ConfigureAwait(false);

            var supplier = await _suppliers.GetByIdAsync(org, submitted.SupplierId, cancellationToken).ConfigureAwait(false);
            if (supplier?.ConnectionType == SupplierConnectionType.ConnectedOrganization
                && supplier.ConnectedRelationshipId is not null
                && await _connectedOrders.GetByBuyerPurchaseOrderAsync(submitted.Id, cancellationToken).ConfigureAwait(false) is null)
            {
                var relationship = await _connectedRelationships.GetAsync(supplier.ConnectedRelationshipId, cancellationToken).ConfigureAwait(false);
                if (relationship is not null
                    && relationship.Status == ConnectedSupplierRelationshipStatus.Active
                    && relationship.BuyerOrganizationId == org)
                {
                    var links = await _connectedLinks.ListAsync(relationship.Id, org, cancellationToken).ConfigureAwait(false);
                    var linksByBuyerProduct = links.Where(x => x.IsActive).ToDictionary(x => x.BuyerProductId.Value);
                    if (submitted.Lines.All(x => linksByBuyerProduct.ContainsKey(x.ProductId.Value)))
                    {
                        var connectedLines = submitted.Lines.OrderBy(x => x.LineNumber).Select(x =>
                        {
                            var link = linksByBuyerProduct[x.ProductId.Value];
                            return ConnectedPurchaseOrderLine.Create(
                                link.SupplierProductId,
                                link.SupplierNameSnapshot,
                                link.SupplierSkuSnapshot,
                                x.OrderedQty,
                                x.UnitPurchaseCost,
                                link.UnitOfMeasureCode);
                        }).ToList();
                        var connectedOrder = ConnectedPurchaseOrder.CreateFromBuyerSubmission(
                            relationship, submitted.Id, submitted.PoNumber, submitted.OrderDate, submitted.Notes,
                            connectedLines, utcNow);
                        await _connectedOrders.AddAsync(connectedOrder, cancellationToken).ConfigureAwait(false);
                        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                    }
                }
            }

            return ApplicationResult<PosPurchaseOrderDto>.Success(PurchaseMapper.Map(submitted));
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
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IPosCommercialAccessAccessor _access;
    private readonly TimeProvider _clock;

    public CancelPurchaseOrder(
        IPurchaseOrderRepository orders,
        IPosUnitOfWork unitOfWork,
        IPosCommercialAccessAccessor access,
        TimeProvider? clock = null)
    {
        _orders = orders;
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

            existing.Cancel(_clock.GetUtcNow());
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

public sealed class ReceivePurchaseOrder
{
    private readonly IPurchaseOrderRepository _orders;
    private readonly ICatalogProductRepository _products;
    private readonly IPurchaseStockService _purchaseStock;
    private readonly IPosCommercialAccessAccessor _access;
    private readonly TimeProvider _clock;

    public ReceivePurchaseOrder(
        IPurchaseOrderRepository orders,
        ICatalogProductRepository products,
        IPurchaseStockService purchaseStock,
        IPosCommercialAccessAccessor access,
        TimeProvider? clock = null)
    {
        _orders = orders;
        _products = products;
        _purchaseStock = purchaseStock;
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
                    return new PurchaseOrderReceiveLineDraft(
                        CatalogProductId.From(l.ProductId),
                        l.ReceiveQty,
                        product.SellingMode);
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
