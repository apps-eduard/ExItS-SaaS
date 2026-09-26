using System.Security.Cryptography;
using System.Text;
using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;

namespace ExItS.PinoyBusinessPOS.Application.Inventory;

public sealed class SupplyRouteQueryService
{
    private readonly ISupplyRouteRepository _routes;

    public SupplyRouteQueryService(ISupplyRouteRepository routes) => _routes = routes;

    public async Task<IReadOnlyList<SupplyRouteDto>> ListAllAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        var items = await _routes
            .ListAllAsync(PosOrganizationId.From(organizationId), cancellationToken)
            .ConfigureAwait(false);
        return items.Select(Map).ToList();
    }

    public async Task<IReadOnlyList<SupplyRouteDto>> ListByDestinationAsync(
        Guid organizationId,
        Guid destinationLocationId,
        CancellationToken cancellationToken = default)
    {
        var items = await _routes
            .ListByDestinationAsync(
                PosOrganizationId.From(organizationId),
                PosBranchId.From(destinationLocationId),
                cancellationToken)
            .ConfigureAwait(false);
        return items.Select(Map).ToList();
    }

    public async Task<SupplyRouteDto?> GetByIdAsync(
        Guid organizationId,
        Guid routeId,
        CancellationToken cancellationToken = default)
    {
        var item = await _routes
            .GetByIdAsync(PosOrganizationId.From(organizationId), SupplyRouteId.From(routeId), cancellationToken)
            .ConfigureAwait(false);
        return item is null ? null : Map(item);
    }

    internal static SupplyRouteDto Map(SupplyRoute route) =>
        new(
            route.Id.Value,
            route.OrganizationId.Value,
            route.SourceLocationId.Value,
            route.DestinationLocationId.Value,
            route.IsPreferred,
            route.IsActive,
            route.Notes,
            route.CreatedAtUtc,
            route.UpdatedAtUtc);
}

public sealed class UpsertSupplyRoutes
{
    private readonly ISupplyRouteRepository _routes;
    private readonly IOrganizationBranchDirectory _branches;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public UpsertSupplyRoutes(
        ISupplyRouteRepository routes,
        IOrganizationBranchDirectory branches,
        IPosUnitOfWork unitOfWork,
        IClock clock)
    {
        _routes = routes;
        _branches = branches;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<IReadOnlyList<SupplyRouteDto>>> ExecuteAsync(
        Guid organizationId,
        UpsertSupplyRoutesRequest request,
        CancellationToken cancellationToken = default)
    {
        var orgId = PosOrganizationId.From(organizationId);
        var destination = PosBranchId.From(request.DestinationLocationId);
        if (!await _branches.ExistsInOrganizationAsync(organizationId, destination.Value, cancellationToken).ConfigureAwait(false))
        {
            return ApplicationResult<IReadOnlyList<SupplyRouteDto>>.Failure(
                ApplicationErrorCodes.InventoryTransferBranchNotFound,
                "Destination location was not found in this organization.");
        }

        var preferredCount = request.Routes.Count(r => r.IsPreferred && r.IsActive);
        if (preferredCount > 1)
        {
            return ApplicationResult<IReadOnlyList<SupplyRouteDto>>.Failure(
                ApplicationErrorCodes.DomainViolation,
                "Only one active preferred route is allowed per destination.");
        }

        try
        {
            var utcNow = _clock.UtcNow;
            var existing = await _routes
                .ListByDestinationAsync(orgId, destination, cancellationToken)
                .ConfigureAwait(false);
            var existingBySource = existing.ToDictionary(r => r.SourceLocationId.Value);

            var seen = new HashSet<Guid>();
            foreach (var item in request.Routes)
            {
                if (!seen.Add(item.SourceLocationId))
                {
                    return ApplicationResult<IReadOnlyList<SupplyRouteDto>>.Failure(
                        DomainErrorCodes.SupplyRouteDuplicateSource,
                        "Route source locations must be unique per destination.");
                }

                if (!await _branches.ExistsInOrganizationAsync(organizationId, item.SourceLocationId, cancellationToken).ConfigureAwait(false))
                {
                    return ApplicationResult<IReadOnlyList<SupplyRouteDto>>.Failure(
                        ApplicationErrorCodes.InventoryTransferBranchNotFound,
                        "Route source location was not found in this organization.");
                }

                if (item.IsActive)
                {
                    if (!await _branches.IsActiveInOrganizationAsync(organizationId, item.SourceLocationId, cancellationToken).ConfigureAwait(false))
                    {
                        return ApplicationResult<IReadOnlyList<SupplyRouteDto>>.Failure(
                            DomainErrorCodes.SupplyRouteSourceInactive,
                            "Supply warehouse must be an active location.");
                    }

                    var sourceType = await _branches
                        .GetBranchTypeAsync(organizationId, item.SourceLocationId, cancellationToken)
                        .ConfigureAwait(false);
                    if (!SupplyRouteSourceRules.IsWarehouseBranchType(sourceType))
                    {
                        return ApplicationResult<IReadOnlyList<SupplyRouteDto>>.Failure(
                            DomainErrorCodes.SupplyRouteSourceMustBeWarehouse,
                            "Only Warehouse locations may be replenishment supply sources.");
                    }
                }

                if (existingBySource.TryGetValue(item.SourceLocationId, out var existingRoute))
                {
                    existingRoute.UpdateNotes(item.Notes, utcNow);
                    if (item.IsActive)
                    {
                        existingRoute.Activate(utcNow);
                    }
                    else
                    {
                        existingRoute.Deactivate(utcNow);
                    }

                    // Clear preferred first so unique partial index stays valid across the batch.
                    existingRoute.SetPreferred(false, utcNow);
                    await _routes.UpdateAsync(existingRoute, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    var created = SupplyRoute.Create(
                        orgId,
                        PosBranchId.From(item.SourceLocationId),
                        destination,
                        utcNow,
                        isPreferred: false,
                        item.IsActive,
                        item.Notes);
                    await _routes.AddAsync(created, cancellationToken).ConfigureAwait(false);
                }
            }

            foreach (var unused in existing.Where(r => !seen.Contains(r.SourceLocationId.Value)))
            {
                unused.Deactivate(utcNow);
                await _routes.UpdateAsync(unused, cancellationToken).ConfigureAwait(false);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            var afterDeactivate = await _routes.ListByDestinationAsync(orgId, destination, cancellationToken).ConfigureAwait(false);
            foreach (var item in request.Routes.Where(r => r.IsPreferred && r.IsActive))
            {
                var preferred = afterDeactivate.FirstOrDefault(r => r.SourceLocationId.Value == item.SourceLocationId && r.IsActive);
                if (preferred is null)
                {
                    continue;
                }

                preferred.SetPreferred(true, utcNow);
                await _routes.UpdateAsync(preferred, cancellationToken).ConfigureAwait(false);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            var refreshed = await _routes.ListByDestinationAsync(orgId, destination, cancellationToken).ConfigureAwait(false);
            return ApplicationResult<IReadOnlyList<SupplyRouteDto>>.Success(refreshed.Select(SupplyRouteQueryService.Map).ToList());
        }
        catch (DomainException ex)
        {
            return ApplicationResult<IReadOnlyList<SupplyRouteDto>>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class SetPreferredSupplyRoute
{
    private readonly ISupplyRouteRepository _routes;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public SetPreferredSupplyRoute(
        ISupplyRouteRepository routes,
        IPosUnitOfWork unitOfWork,
        IClock clock)
    {
        _routes = routes;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<IReadOnlyList<SupplyRouteDto>>> ExecuteAsync(
        Guid organizationId,
        Guid destinationLocationId,
        SetPreferredSupplyRouteRequest request,
        CancellationToken cancellationToken = default)
    {
        var orgId = PosOrganizationId.From(organizationId);
        var destination = PosBranchId.From(destinationLocationId);
        var routes = await _routes.ListByDestinationAsync(orgId, destination, cancellationToken).ConfigureAwait(false);
        if (routes.Count == 0)
        {
            return ApplicationResult<IReadOnlyList<SupplyRouteDto>>.Failure(
                ApplicationErrorCodes.InventoryTransferNotFound,
                "No supply routes were found for this destination.");
        }

        var utcNow = _clock.UtcNow;
        var target = routes.FirstOrDefault(r => r.SourceLocationId.Value == request.SourceLocationId);
        if (target is null)
        {
            return ApplicationResult<IReadOnlyList<SupplyRouteDto>>.Failure(
                ApplicationErrorCodes.InventoryTransferNotFound,
                "Route source was not found for this destination.");
        }

        foreach (var route in routes.Where(r => r.IsPreferred))
        {
            route.SetPreferred(false, utcNow);
            await _routes.UpdateAsync(route, cancellationToken).ConfigureAwait(false);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        target.Activate(utcNow);
        target.SetPreferred(true, utcNow);
        await _routes.UpdateAsync(target, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        var refreshed = await _routes.ListByDestinationAsync(orgId, destination, cancellationToken).ConfigureAwait(false);
        return ApplicationResult<IReadOnlyList<SupplyRouteDto>>.Success(refreshed.Select(SupplyRouteQueryService.Map).ToList());
    }
}

public sealed class DisableSupplyRoute
{
    private readonly ISupplyRouteRepository _routes;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public DisableSupplyRoute(
        ISupplyRouteRepository routes,
        IPosUnitOfWork unitOfWork,
        IClock clock)
    {
        _routes = routes;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<SupplyRouteDto>> ExecuteAsync(
        Guid organizationId,
        Guid routeId,
        CancellationToken cancellationToken = default)
    {
        var route = await _routes
            .GetByIdAsync(PosOrganizationId.From(organizationId), SupplyRouteId.From(routeId), cancellationToken)
            .ConfigureAwait(false);
        if (route is null)
        {
            return ApplicationResult<SupplyRouteDto>.Failure(
                ApplicationErrorCodes.InventoryTransferNotFound,
                "Supply route was not found.");
        }

        route.Deactivate(_clock.UtcNow);
        await _routes.UpdateAsync(route, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return ApplicationResult<SupplyRouteDto>.Success(SupplyRouteQueryService.Map(route));
    }
}

public sealed class StockRequestQueryService
{
    private readonly IStockRequestRepository _requests;
    private readonly IInventoryTransferRepository _transfers;
    private readonly IOrganizationBranchDirectory _branches;

    public StockRequestQueryService(
        IStockRequestRepository requests,
        IInventoryTransferRepository transfers,
        IOrganizationBranchDirectory branches)
    {
        _requests = requests;
        _transfers = transfers;
        _branches = branches;
    }

    public async Task<StockRequestDto?> GetByIdAsync(
        Guid organizationId,
        Guid stockRequestId,
        CancellationToken cancellationToken = default)
    {
        var request = await _requests
            .GetByIdAsync(PosOrganizationId.From(organizationId), StockRequestId.From(stockRequestId), cancellationToken)
            .ConfigureAwait(false);
        if (request is null)
        {
            return null;
        }

        var linked = await _transfers
            .ListByStockRequestIdAsync(PosOrganizationId.From(organizationId), request.Id, cancellationToken)
            .ConfigureAwait(false);
        var names = await _branches
            .GetNamesAsync(
                organizationId,
                [request.DestinationLocationId.Value, request.RequestedSourceLocationId.Value],
                cancellationToken)
            .ConfigureAwait(false);
        return Map(request, linked, names);
    }

    public async Task<PagedResult<StockRequestListItemDto>> ListOutgoingAsync(
        Guid organizationId,
        Guid actingBranchId,
        int? page,
        int? pageSize,
        IReadOnlyCollection<StockRequestStatus>? statuses = null,
        CancellationToken cancellationToken = default)
    {
        var (skip, take) = PosPagination.Normalize(page, pageSize);
        var (items, total) = await _requests
            .ListByDestinationAsync(
                PosOrganizationId.From(organizationId),
                PosBranchId.From(actingBranchId),
                skip,
                take,
                statuses,
                cancellationToken)
            .ConfigureAwait(false);
        var branchIds = items
            .SelectMany(r => new[] { r.DestinationLocationId.Value, r.RequestedSourceLocationId.Value })
            .Distinct()
            .ToList();
        var names = await _branches.GetNamesAsync(organizationId, branchIds, cancellationToken).ConfigureAwait(false);
        return new PagedResult<StockRequestListItemDto>(
            items.Select(r => MapList(r, names)).ToList(),
            total,
            Math.Max(page ?? 1, 1),
            take);
    }

    public async Task<StockRequestOutgoingSummaryDto> GetOutgoingSummaryAsync(
        Guid organizationId,
        Guid actingBranchId,
        CancellationToken cancellationToken = default)
    {
        var orgId = PosOrganizationId.From(organizationId);
        var destination = PosBranchId.From(actingBranchId);
        var counts = await _requests
            .CountByDestinationStatusAsync(orgId, destination, cancellationToken)
            .ConfigureAwait(false);
        var recent = await _requests
            .ListRecentByDestinationAsync(orgId, destination, take: 5, cancellationToken)
            .ConfigureAwait(false);

        static int CountOf(IReadOnlyDictionary<string, int> map, params string[] codes)
        {
            var total = 0;
            foreach (var code in codes)
            {
                if (map.TryGetValue(code, out var c))
                {
                    total += c;
                }
            }

            return total;
        }

        var submitted = CountOf(counts, nameof(StockRequestStatus.Pending));
        var inProgress = CountOf(
            counts,
            nameof(StockRequestStatus.Approved),
            nameof(StockRequestStatus.Preparing),
            nameof(StockRequestStatus.InProgress));
        var inTransit = CountOf(counts, nameof(StockRequestStatus.InTransit));

        var branchIds = recent
            .SelectMany(r => new[] { r.DestinationLocationId.Value, r.RequestedSourceLocationId.Value })
            .Distinct()
            .ToList();
        var names = await _branches.GetNamesAsync(organizationId, branchIds, cancellationToken).ConfigureAwait(false);
        return new StockRequestOutgoingSummaryDto(
            submitted,
            inProgress,
            inTransit,
            recent.Select(r => MapList(r, names)).ToList());
    }

    public async Task<PagedResult<StockRequestListItemDto>> ListIncomingAsync(
        Guid organizationId,
        Guid actingBranchId,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken = default)
    {
        var (skip, take) = PosPagination.Normalize(page, pageSize);
        var (items, total) = await _requests
            .ListBySourceAsync(
                PosOrganizationId.From(organizationId),
                PosBranchId.From(actingBranchId),
                skip,
                take,
                cancellationToken)
            .ConfigureAwait(false);
        var branchIds = items
            .SelectMany(r => new[] { r.DestinationLocationId.Value, r.RequestedSourceLocationId.Value })
            .Distinct()
            .ToList();
        var names = await _branches.GetNamesAsync(organizationId, branchIds, cancellationToken).ConfigureAwait(false);
        return new PagedResult<StockRequestListItemDto>(
            items.Select(r => MapList(r, names)).ToList(),
            total,
            Math.Max(page ?? 1, 1),
            take);
    }

    internal static StockRequestDto Map(
        StockRequest request,
        IReadOnlyList<InventoryTransfer> linkedTransfers,
        IReadOnlyDictionary<Guid, string> names)
    {
        var coverage = StockRequestDispatchCoverage.Compute(request, linkedTransfers);
        var activeTransfers = linkedTransfers
            .Where(t => t.Status != InventoryTransferStatus.Cancelled)
            .ToList();
        var damagedByProduct = activeTransfers
            .SelectMany(t => t.Receipts)
            .SelectMany(r => r.Lines)
            .GroupBy(l => l.ProductId.Value)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.QuantityDamaged));

        return new(
            request.Id.Value,
            request.OrganizationId.Value,
            request.DestinationLocationId.Value,
            names.GetValueOrDefault(request.DestinationLocationId.Value),
            request.RequestedSourceLocationId.Value,
            names.GetValueOrDefault(request.RequestedSourceLocationId.Value),
            request.RequestNumber,
            StockRequestStatuses.ToCode(request.Status),
            request.Notes,
            request.RequestedBy,
            request.CreatedAtUtc,
            request.UpdatedAtUtc,
            request.ApprovedBy,
            request.ApprovedAtUtc,
            request.PreparingStartedBy,
            request.PreparingStartedAtUtc,
            request.DispatchedBy,
            request.DispatchedAtUtc,
            request.LinkedInventoryTransferId,
            request.RejectedBy,
            request.RejectedAtUtc,
            request.RejectionReason,
            request.CancelledBy,
            request.CancelledAtUtc,
            request.Lines.Select(line =>
            {
                var productId = line.ProductId.Value;
                var received = coverage.ReceivedByProduct.GetValueOrDefault(productId);
                var openInTransit = coverage.OpenInTransitByProduct.GetValueOrDefault(productId);
                var remaining = coverage.RemainingToDispatchByProduct.GetValueOrDefault(productId);
                var waived = coverage.WaivedByProduct.GetValueOrDefault(productId);
                return new StockRequestLineDto(
                    line.Id.Value,
                    productId,
                    line.LineNumber,
                    line.RequestedQuantity,
                    line.ApprovedQuantity,
                    received,
                    openInTransit,
                    remaining,
                    waived,
                    line.NameSnapshot,
                    UnitOfMeasures.ToCode(line.UnitOfMeasure),
                    damagedByProduct.GetValueOrDefault(productId),
                    line.FulfillmentTargetQuantity);
            }).ToList(),
            linkedTransfers
                .OrderByDescending(t => t.UpdatedAtUtc)
                .Select(t => new StockRequestLinkedTransferDto(
                    t.Id.Value,
                    t.TransferNumber,
                    InventoryTransferStatuses.ToCode(t.Status),
                    t.TotalSentQty,
                    t.TotalReceivedQty,
                    t.Status is InventoryTransferStatus.InTransit or InventoryTransferStatus.PartiallyReceived
                        ? t.Lines.Sum(l => Math.Max(0m, l.OutstandingQty))
                        : 0m,
                    t.TotalClosedQty,
                    t.CreatedAtUtc,
                    t.CreatedBy,
                    t.UpdatedAtUtc,
                    t.DispatchedAtUtc,
                    t.DispatchedBy,
                    t.ClosedAtUtc,
                    t.ClosedBy))
                .ToList());
    }

    private static StockRequestListItemDto MapList(
        StockRequest request,
        IReadOnlyDictionary<Guid, string> names) =>
        new(
            request.Id.Value,
            request.RequestNumber,
            StockRequestStatuses.ToCode(request.Status),
            request.DestinationLocationId.Value,
            names.GetValueOrDefault(request.DestinationLocationId.Value),
            request.RequestedSourceLocationId.Value,
            names.GetValueOrDefault(request.RequestedSourceLocationId.Value),
            request.Lines.Count,
            request.UpdatedAtUtc);
}

public sealed class ListReplenishmentCatalog
{
    public const int DefaultPageSize = 40;

    private readonly IBranchInventoryQueryRepository _branchInventory;
    private readonly ISupplyRouteRepository _routes;
    private readonly IOrganizationBranchDirectory _branches;
    private readonly ICatalogProductRepository _products;
    private readonly IEffectivePriceResolver _effectivePrices;
    private readonly InventoryCostResolver _costs;

    public ListReplenishmentCatalog(
        IBranchInventoryQueryRepository branchInventory,
        ISupplyRouteRepository routes,
        IOrganizationBranchDirectory branches,
        IInventoryRepository inventory,
        ICatalogProductRepository products,
        IEffectivePriceResolver effectivePrices,
        InventoryCostResolver? costs = null)
    {
        _branchInventory = branchInventory;
        _routes = routes;
        _branches = branches;
        _products = products;
        _effectivePrices = effectivePrices;
        _costs = costs ?? new InventoryCostResolver(inventory);
    }

    public async Task<ApplicationResult<ReplenishmentCatalogResultDto>> ExecuteAsync(
        BranchInventoryContext retailContext,
        Guid supplyWarehouseBranchId,
        string? search,
        string? stockFilter,
        Guid? categoryId,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken = default)
    {
        if (supplyWarehouseBranchId == Guid.Empty)
        {
            return ApplicationResult<ReplenishmentCatalogResultDto>.Failure(
                ApplicationErrorCodes.InventoryTransferBranchNotFound,
                "Supply warehouse branch id is required.");
        }

        if (!ReplenishmentStockFilters.TryNormalize(stockFilter, out var normalizedStockFilter))
        {
            return ApplicationResult<ReplenishmentCatalogResultDto>.Failure(
                ApplicationErrorCodes.DomainViolation,
                "stockFilter must be one of: all, low, out.");
        }

        if (!await _branches
                .ExistsInOrganizationAsync(retailContext.OrganizationId, supplyWarehouseBranchId, cancellationToken)
                .ConfigureAwait(false))
        {
            return ApplicationResult<ReplenishmentCatalogResultDto>.Failure(
                ApplicationErrorCodes.InventoryTransferBranchNotFound,
                "Supply warehouse was not found in this organization.");
        }

        if (!await _branches
                .IsActiveInOrganizationAsync(retailContext.OrganizationId, supplyWarehouseBranchId, cancellationToken)
                .ConfigureAwait(false))
        {
            return ApplicationResult<ReplenishmentCatalogResultDto>.Failure(
                DomainErrorCodes.SupplyRouteSourceInactive,
                "Supply warehouse must be an active location.");
        }

        var sourceType = await _branches
            .GetBranchTypeAsync(retailContext.OrganizationId, supplyWarehouseBranchId, cancellationToken)
            .ConfigureAwait(false);
        if (!SupplyRouteSourceRules.IsWarehouseBranchType(sourceType))
        {
            return ApplicationResult<ReplenishmentCatalogResultDto>.Failure(
                DomainErrorCodes.StockRequestSourceMustBeWarehouse,
                "Only Warehouse locations may be replenishment supply sources.");
        }

        var routes = await _routes
            .ListByDestinationAsync(
                PosOrganizationId.From(retailContext.OrganizationId),
                PosBranchId.From(retailContext.BranchId),
                cancellationToken)
            .ConfigureAwait(false);
        var hasActiveRoute = routes.Any(r =>
            r.SourceLocationId.Value == supplyWarehouseBranchId && r.IsActive);
        if (!hasActiveRoute)
        {
            return ApplicationResult<ReplenishmentCatalogResultDto>.Failure(
                DomainErrorCodes.StockRequestRouteRequired,
                "An active supply route is required from the warehouse to this retail branch.");
        }

        var (skip, take) = PosPagination.Normalize(page, pageSize ?? DefaultPageSize);
        var (rows, total) = await _branchInventory
            .ListReplenishmentCatalogAsync(
                retailContext,
                new ReplenishmentCatalogFilter(
                    supplyWarehouseBranchId,
                    search,
                    normalizedStockFilter,
                    categoryId),
                skip,
                take,
                cancellationToken)
            .ConfigureAwait(false);

        var names = await _branches
            .GetNamesAsync(retailContext.OrganizationId, [supplyWarehouseBranchId], cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyDictionary<Guid, decimal?> costs = new Dictionary<Guid, decimal?>();
        IReadOnlyDictionary<EffectivePriceKey, EffectivePriceResult> prices =
            new Dictionary<EffectivePriceKey, EffectivePriceResult>();
        if (rows.Count > 0)
        {
            var orgId = PosOrganizationId.From(retailContext.OrganizationId);
            var productIds = rows.Select(r => CatalogProductId.From(r.ProductId)).ToList();
            costs = await _costs.ResolveUnitCostsAsync(orgId, productIds, cancellationToken).ConfigureAwait(false);
            var products = await _products.ListByIdsAsync(orgId, productIds, cancellationToken).ConfigureAwait(false);
            prices = await _effectivePrices
                .ResolveAsync(orgId, PosBranchId.From(retailContext.BranchId), products, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }

        return ApplicationResult<ReplenishmentCatalogResultDto>.Success(
            new ReplenishmentCatalogResultDto(
                rows.Select(r =>
                {
                    var priceKey = EffectivePriceKeys.ForBaseProduct(r.ProductId);
                    prices.TryGetValue(priceKey, out var price);
                    return new ReplenishmentCatalogItemDto(
                        r.ProductId,
                        r.Name,
                        r.Sku,
                        r.Barcode,
                        r.CategoryId,
                        r.CategoryName,
                        r.UnitOfMeasure,
                        r.BranchOnHandQuantity,
                        r.WarehouseAvailableQuantity,
                        r.IsLowStock,
                        r.IsTracked,
                        r.SellingMode,
                        costs.GetValueOrDefault(r.ProductId),
                        price?.EffectivePrice);
                }).ToList(),
                total,
                Math.Max(page ?? 1, 1),
                take,
                supplyWarehouseBranchId,
                names.GetValueOrDefault(supplyWarehouseBranchId)));
    }
}

public sealed class CreateStockRequest
{
    private readonly IStockRequestRepository _requests;
    private readonly ISupplyRouteRepository _routes;
    private readonly ICatalogProductRepository _products;
    private readonly IOrganizationBranchDirectory _branches;
    private readonly IInventoryBranchBalanceRepository _balances;
    private readonly StockRequestQueryService _queries;
    private readonly IOrganizationBusinessNotificationPublisher _notifications;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public CreateStockRequest(
        IStockRequestRepository requests,
        ISupplyRouteRepository routes,
        ICatalogProductRepository products,
        IOrganizationBranchDirectory branches,
        IInventoryBranchBalanceRepository balances,
        StockRequestQueryService queries,
        IOrganizationBusinessNotificationPublisher notifications,
        IPosUnitOfWork unitOfWork,
        IClock clock)
    {
        _requests = requests;
        _routes = routes;
        _products = products;
        _branches = branches;
        _balances = balances;
        _queries = queries;
        _notifications = notifications;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<StockRequestDto>> ExecuteAsync(
        Guid organizationId,
        CreateStockRequestRequest request,
        Guid actorId,
        Guid actingBranchId,
        CancellationToken cancellationToken = default)
    {
        if (actorId == Guid.Empty)
        {
            return ApplicationResult<StockRequestDto>.Failure(
                ApplicationErrorCodes.ActorRequired,
                "An actor identifier is required to create a stock request.");
        }

        if (actingBranchId != request.DestinationLocationId)
        {
            return ApplicationResult<StockRequestDto>.Failure(
                ApplicationErrorCodes.InventoryTransferBranchForbidden,
                "Only the destination branch can create this stock request.");
        }

        if (!await _branches.ExistsInOrganizationAsync(organizationId, request.DestinationLocationId, cancellationToken).ConfigureAwait(false)
            || !await _branches.ExistsInOrganizationAsync(organizationId, request.RequestedSourceLocationId, cancellationToken).ConfigureAwait(false))
        {
            return ApplicationResult<StockRequestDto>.Failure(
                ApplicationErrorCodes.InventoryTransferBranchNotFound,
                "Requested source and destination locations must belong to the same organization.");
        }

        var orgId = PosOrganizationId.From(organizationId);
        var routes = await _routes
            .ListByDestinationAsync(orgId, PosBranchId.From(request.DestinationLocationId), cancellationToken)
            .ConfigureAwait(false);
        var hasActiveRoute = routes.Any(r =>
            r.SourceLocationId.Value == request.RequestedSourceLocationId && r.IsActive);
        if (!hasActiveRoute)
        {
            return ApplicationResult<StockRequestDto>.Failure(
                DomainErrorCodes.StockRequestRouteRequired,
                "An active supply route is required for the requested source and destination.");
        }

        var sourceType = await _branches
            .GetBranchTypeAsync(organizationId, request.RequestedSourceLocationId, cancellationToken)
            .ConfigureAwait(false);
        if (!SupplyRouteSourceRules.IsWarehouseBranchType(sourceType))
        {
            return ApplicationResult<StockRequestDto>.Failure(
                DomainErrorCodes.StockRequestSourceMustBeWarehouse,
                "Stock requests may only be sourced from Warehouse locations.");
        }

        if (!await _branches.IsActiveInOrganizationAsync(organizationId, request.RequestedSourceLocationId, cancellationToken).ConfigureAwait(false))
        {
            return ApplicationResult<StockRequestDto>.Failure(
                DomainErrorCodes.SupplyRouteSourceInactive,
                "Supply warehouse must be an active location.");
        }

        if (request.Lines is null || request.Lines.Count == 0)
        {
            return ApplicationResult<StockRequestDto>.Failure(
                DomainErrorCodes.StockRequestRequiresLines,
                "At least one stock request line is required.");
        }

        var productIds = request.Lines.Select(l => CatalogProductId.From(l.ProductId)).ToList();
        var products = (await _products.ListByIdsAsync(orgId, productIds, cancellationToken).ConfigureAwait(false))
            .ToDictionary(p => p.Id.Value);
        var drafts = new List<StockRequestLineDraft>(request.Lines.Count);
        foreach (var line in request.Lines)
        {
            if (!products.TryGetValue(line.ProductId, out var product))
            {
                return ApplicationResult<StockRequestDto>.Failure(
                    ApplicationErrorCodes.InventoryProductNotFound,
                    "Product was not found.");
            }

            if (product.Status != CatalogProductStatus.Active)
            {
                return ApplicationResult<StockRequestDto>.Failure(
                    DomainErrorCodes.ProductNotActive,
                    $"Product '{product.Name}' is not active.");
            }

            drafts.Add(new StockRequestLineDraft(
                product.Id,
                line.RequestedQuantity,
                product.Name,
                product.UnitOfMeasure,
                product.SellingMode));
        }

        var sourceBranchId = PosBranchId.From(request.RequestedSourceLocationId);
        var warehouseBalances = (await _balances
                .ListByBranchAndProductIdsAsync(orgId, sourceBranchId, productIds, cancellationToken)
                .ConfigureAwait(false))
            .ToDictionary(b => b.ProductId.Value);
        var warehouseNames = await _branches
            .GetNamesAsync(organizationId, [request.RequestedSourceLocationId], cancellationToken)
            .ConfigureAwait(false);
        var warehouseName = warehouseNames.TryGetValue(request.RequestedSourceLocationId, out var name)
            && !string.IsNullOrWhiteSpace(name)
            ? name.Trim()
            : "the supply warehouse";

        foreach (var draft in drafts)
        {
            var available = warehouseBalances.TryGetValue(draft.ProductId.Value, out var balance)
                ? Math.Max(0m, balance.AvailableQuantity)
                : 0m;
            var unit = UnitOfMeasures.ToCode(draft.UnitOfMeasure);
            var displayUnit = string.Equals(unit, "Kilogram", StringComparison.OrdinalIgnoreCase)
                ? "kg"
                : unit;

            if (available <= 0m)
            {
                return ApplicationResult<StockRequestDto>.Failure(
                    ApplicationErrorCodes.InsufficientStock,
                    $"{draft.NameSnapshot} is out of stock at {warehouseName}.");
            }

            if (draft.RequestedQuantity > available)
            {
                return ApplicationResult<StockRequestDto>.Failure(
                    ApplicationErrorCodes.InsufficientStock,
                    $"{draft.NameSnapshot} requested {draft.RequestedQuantity:0.###} {displayUnit}, but only {available:0.###} {displayUnit} is available at {warehouseName}.");
            }
        }

        try
        {
            var utcNow = _clock.UtcNow;
            var number = await _requests
                .AllocateNextNumberAsync(orgId, StockRequestNumbers.BusinessDateOf(utcNow), cancellationToken)
                .ConfigureAwait(false);
            var stockRequest = StockRequest.Create(
                orgId,
                PosBranchId.From(request.DestinationLocationId),
                PosBranchId.From(request.RequestedSourceLocationId),
                drafts,
                actorId,
                utcNow,
                number,
                request.Notes);

            await _requests.AddAsync(stockRequest, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            await StockRequestNotificationHelper
                .PublishAsync(
                    _notifications,
                    organizationId,
                    StockRequestNotificationTypes.Submitted,
                    stockRequest,
                    stockRequest.RequestedSourceLocationId.Value,
                    "Stock request submitted",
                    $"{stockRequest.RequestNumber ?? stockRequest.Id.Value.ToString("D")} awaiting warehouse review.",
                    cancellationToken)
                .ConfigureAwait(false);

            var dto = await _queries.GetByIdAsync(organizationId, stockRequest.Id.Value, cancellationToken).ConfigureAwait(false);
            return dto is null
                ? ApplicationResult<StockRequestDto>.Failure("pos.inventory.stock_request.not_found", "Stock request was not found.")
                : ApplicationResult<StockRequestDto>.Success(dto);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<StockRequestDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class ApproveStockRequest
{
    private readonly IStockRequestRepository _requests;
    private readonly StockRequestQueryService _queries;
    private readonly IOrganizationBusinessNotificationPublisher _notifications;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public ApproveStockRequest(
        IStockRequestRepository requests,
        StockRequestQueryService queries,
        IOrganizationBusinessNotificationPublisher notifications,
        IPosUnitOfWork unitOfWork,
        IClock clock)
    {
        _requests = requests;
        _queries = queries;
        _notifications = notifications;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<StockRequestDto>> ExecuteAsync(
        Guid organizationId,
        Guid stockRequestId,
        ApproveStockRequestRequest body,
        Guid actorId,
        Guid actingBranchId,
        CancellationToken cancellationToken = default)
    {
        var request = await _requests
            .GetByIdAsync(PosOrganizationId.From(organizationId), StockRequestId.From(stockRequestId), cancellationToken)
            .ConfigureAwait(false);
        if (request is null)
        {
            return ApplicationResult<StockRequestDto>.Failure(
                "pos.inventory.stock_request.not_found",
                "Stock request was not found.");
        }

        if (actingBranchId != request.RequestedSourceLocationId.Value)
        {
            return ApplicationResult<StockRequestDto>.Failure(
                ApplicationErrorCodes.InventoryTransferBranchForbidden,
                "Only the requested source warehouse can approve this stock request.");
        }

        if (body.LineApprovals is null || body.LineApprovals.Count == 0)
        {
            return ApplicationResult<StockRequestDto>.Failure(
                DomainErrorCodes.StockRequestRequiresLines,
                "At least one line approval is required.");
        }

        try
        {
            var approvals = body.LineApprovals.ToDictionary(l => l.ProductId, l => l.ApprovedQuantity);
            request.Approve(actorId, _clock.UtcNow, approvals);
            await _requests.UpdateAsync(request, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            await StockRequestNotificationHelper
                .PublishAsync(
                    _notifications,
                    organizationId,
                    StockRequestNotificationTypes.Approved,
                    request,
                    request.DestinationLocationId.Value,
                    "Stock request approved",
                    $"{request.RequestNumber ?? request.Id.Value.ToString("D")} was approved.",
                    cancellationToken)
                .ConfigureAwait(false);

            var dto = await _queries.GetByIdAsync(organizationId, request.Id.Value, cancellationToken).ConfigureAwait(false);
            return dto is null
                ? ApplicationResult<StockRequestDto>.Failure("pos.inventory.stock_request.not_found", "Stock request was not found.")
                : ApplicationResult<StockRequestDto>.Success(dto);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<StockRequestDto>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<StockRequestDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class StartPreparingStockRequest
{
    private readonly IStockRequestRepository _requests;
    private readonly StockRequestQueryService _queries;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public StartPreparingStockRequest(
        IStockRequestRepository requests,
        StockRequestQueryService queries,
        IPosUnitOfWork unitOfWork,
        IClock clock)
    {
        _requests = requests;
        _queries = queries;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<StockRequestDto>> ExecuteAsync(
        Guid organizationId,
        Guid stockRequestId,
        Guid actorId,
        Guid actingBranchId,
        CancellationToken cancellationToken = default)
    {
        var request = await _requests
            .GetByIdAsync(PosOrganizationId.From(organizationId), StockRequestId.From(stockRequestId), cancellationToken)
            .ConfigureAwait(false);
        if (request is null)
        {
            return ApplicationResult<StockRequestDto>.Failure(
                "pos.inventory.stock_request.not_found",
                "Stock request was not found.");
        }

        if (actingBranchId != request.RequestedSourceLocationId.Value)
        {
            return ApplicationResult<StockRequestDto>.Failure(
                ApplicationErrorCodes.InventoryTransferBranchForbidden,
                "Only the requested source warehouse can prepare this stock request.");
        }

        try
        {
            request.StartPreparing(actorId, _clock.UtcNow);
            await _requests.UpdateAsync(request, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            var dto = await _queries.GetByIdAsync(organizationId, request.Id.Value, cancellationToken).ConfigureAwait(false);
            return dto is null
                ? ApplicationResult<StockRequestDto>.Failure("pos.inventory.stock_request.not_found", "Stock request was not found.")
                : ApplicationResult<StockRequestDto>.Success(dto);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<StockRequestDto>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<StockRequestDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

/// <summary>
/// Builds a draft inventory transfer for remaining stock-request quantity without dispatching or marking the request in transit.
/// </summary>
public sealed class PrepareStockRequestTransfer
{
    private readonly IStockRequestRepository _requests;
    private readonly IInventoryTransferRepository _transfers;
    private readonly IInventoryTransferDamageCustodyRepository _damageCustodies;
    private readonly CreateInventoryTransfer _createTransfer;
    private readonly InventoryTransferQueryService _transferQueries;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public PrepareStockRequestTransfer(
        IStockRequestRepository requests,
        IInventoryTransferRepository transfers,
        IInventoryTransferDamageCustodyRepository damageCustodies,
        CreateInventoryTransfer createTransfer,
        InventoryTransferQueryService transferQueries,
        IPosUnitOfWork unitOfWork,
        IClock clock)
    {
        _requests = requests;
        _transfers = transfers;
        _damageCustodies = damageCustodies;
        _createTransfer = createTransfer;
        _transferQueries = transferQueries;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<InventoryTransferDto>> ExecuteAsync(
        Guid organizationId,
        Guid stockRequestId,
        Guid actorId,
        Guid actingBranchId,
        CancellationToken cancellationToken = default)
    {
        var orgId = PosOrganizationId.From(organizationId);
        var stockRequest = await _requests
            .GetByIdAsync(orgId, StockRequestId.From(stockRequestId), cancellationToken)
            .ConfigureAwait(false);
        if (stockRequest is null)
        {
            return ApplicationResult<InventoryTransferDto>.Failure(
                "pos.inventory.stock_request.not_found",
                "Stock request was not found.");
        }

        if (actingBranchId != stockRequest.RequestedSourceLocationId.Value)
        {
            return ApplicationResult<InventoryTransferDto>.Failure(
                ApplicationErrorCodes.InventoryTransferBranchForbidden,
                "Only the requested source warehouse can prepare a transfer for this stock request.");
        }

        try
        {
            return await _unitOfWork.ExecuteInSerializableTransactionAsync(
                ct => PrepareCoreAsync(organizationId, orgId, stockRequestId, actorId, actingBranchId, ct),
                cancellationToken).ConfigureAwait(false);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<InventoryTransferDto>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<InventoryTransferDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }

    private async Task<ApplicationResult<InventoryTransferDto>> PrepareCoreAsync(
        Guid organizationId,
        PosOrganizationId orgId,
        Guid stockRequestId,
        Guid actorId,
        Guid actingBranchId,
        CancellationToken cancellationToken)
    {
        var stockRequest = await _requests
            .GetByIdAsync(orgId, StockRequestId.From(stockRequestId), cancellationToken)
            .ConfigureAwait(false);
        if (stockRequest is null)
        {
            return ApplicationResult<InventoryTransferDto>.Failure(
                "pos.inventory.stock_request.not_found",
                "Stock request was not found.");
        }

        if (stockRequest.Status == StockRequestStatus.Approved)
        {
            stockRequest.StartPreparing(actorId, _clock.UtcNow);
            await _requests.UpdateAsync(stockRequest, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        if (stockRequest.Status is not (
            StockRequestStatus.Approved
            or StockRequestStatus.Preparing
            or StockRequestStatus.InTransit
            or StockRequestStatus.PartiallyFulfilled))
        {
            return ApplicationResult<InventoryTransferDto>.Failure(
                DomainErrorCodes.InvalidStockRequestStatusTransition,
                "Stock request is not open for transfer preparation.");
        }

        var linkedTransfers = (await _transfers
                .ListByStockRequestIdAsync(orgId, stockRequest.Id, cancellationToken)
                .ConfigureAwait(false))
            .Where(t => t.Status != InventoryTransferStatus.Cancelled)
            .OrderByDescending(t => t.UpdatedAtUtc)
            .ToList();

        var existingDraft = linkedTransfers.FirstOrDefault(t => t.Status == InventoryTransferStatus.Draft);
        if (existingDraft is not null)
        {
            var existingDto = await _transferQueries
                .GetByIdAsync(organizationId, existingDraft.Id.Value, cancellationToken)
                .ConfigureAwait(false);
            return existingDto is null
                ? ApplicationResult<InventoryTransferDto>.Failure(
                    ApplicationErrorCodes.InventoryTransferNotFound,
                    "Inventory transfer was not found.")
                : ApplicationResult<InventoryTransferDto>.Success(existingDto);
        }

        var utcNow = _clock.UtcNow;
        var closedAny = false;
        foreach (var member in linkedTransfers)
        {
            if (InventoryTransferCoverageMath.TryCloseExpectedLaterOpen(member, actorId, utcNow))
            {
                await _transfers.UpdateAsync(member, cancellationToken).ConfigureAwait(false);
                closedAny = true;
            }
        }

        if (closedAny)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            linkedTransfers = (await _transfers
                    .ListByStockRequestIdAsync(orgId, stockRequest.Id, cancellationToken)
                    .ConfigureAwait(false))
                .Where(t => t.Status != InventoryTransferStatus.Cancelled)
                .OrderByDescending(t => t.UpdatedAtUtc)
                .ToList();
        }

        var remainingLines = StockRequestDispatchCoverage.BuildRemainingDispatchLines(
            stockRequest,
            linkedTransfers,
            await _damageCustodies
                .ListByStockRequestIdAsync(orgId, stockRequest.Id, cancellationToken)
                .ConfigureAwait(false));
        if (remainingLines.Count == 0)
        {
            var openCovering = linkedTransfers.FirstOrDefault(t =>
                t.Status is InventoryTransferStatus.InTransit or InventoryTransferStatus.PartiallyReceived);
            if (openCovering is not null)
            {
                var openQty = openCovering.Lines.Sum(l => Math.Max(0m, l.OutstandingQty));
                var transferLabel = openCovering.TransferNumber ?? openCovering.Id.Value.ToString("D");
                return ApplicationResult<InventoryTransferDto>.Failure(
                    DomainErrorCodes.StockRequestNoRemainingToDispatch,
                    $"No remaining stock is available to prepare. Outstanding quantity is already covered by an open transfer ({transferLabel}; {openQty} still in transit). Receive or close that transfer before preparing replacement stock.");
            }

            return ApplicationResult<InventoryTransferDto>.Failure(
                DomainErrorCodes.StockRequestNoRemainingToDispatch,
                "No remaining stock is available to prepare. Outstanding quantity is already covered by an open transfer.");
        }

        var root = linkedTransfers
            .Where(t => t.RootTransferId is null)
            .OrderBy(t => t.CreatedAtUtc)
            .FirstOrDefault();
        var rootId = root?.Id ?? linkedTransfers
            .Select(t => t.RootTransferId)
            .FirstOrDefault(id => id is not null);
        var nextSequence = rootId is null
            ? (int?)null
            : linkedTransfers
                .Where(t => t.RootTransferId == rootId || t.Id == rootId)
                .Select(t => t.ReplacementSequence ?? 0)
                .DefaultIfEmpty(0)
                .Max() + 1;
        var damagePolicy = root is null
            ? null
            : InventoryTransferDamageHandlingPolicies.ToCode(root.DamageHandlingPolicy);

        var createRequest = new CreateInventoryTransferRequest(
            stockRequest.RequestedSourceLocationId.Value,
            stockRequest.DestinationLocationId.Value,
            remainingLines,
            stockRequest.Notes,
            stockRequest.Id.Value,
            RootTransferId: rootId?.Value,
            ReplacementSequence: nextSequence,
            ReplacementReason: nextSequence is null ? null : "Replacement for remaining / discrepancy fulfillment",
            DamageHandlingPolicy: damagePolicy);
        var created = await _createTransfer
            .ExecuteAsync(organizationId, createRequest, actorId, actingBranchId, cancellationToken)
            .ConfigureAwait(false);
        if (!created.IsSuccess)
        {
            return ApplicationResult<InventoryTransferDto>.Failure(created.ErrorCode!, created.ErrorMessage!);
        }

        var dto = await _transferQueries
            .GetByIdAsync(organizationId, created.Value!.Id.Value, cancellationToken)
            .ConfigureAwait(false);
        return dto is null
            ? ApplicationResult<InventoryTransferDto>.Failure(
                ApplicationErrorCodes.InventoryTransferNotFound,
                "Inventory transfer was not found.")
            : ApplicationResult<InventoryTransferDto>.Success(dto);
    }
}

/// <summary>
/// LEGACY one-shot dispatch: creates a draft when needed and immediately dispatches it.
/// Preferred flow: <see cref="PrepareStockRequestTransfer"/> then POST /transfers/{id}/dispatch.
/// </summary>
public sealed class DispatchStockRequest
{
    private readonly IStockRequestRepository _requests;
    private readonly IInventoryTransferRepository _transfers;
    private readonly IInventoryTransferDamageCustodyRepository _damageCustodies;
    private readonly CreateInventoryTransfer _createTransfer;
    private readonly DispatchInventoryTransfer _dispatchTransfer;
    private readonly InventoryTransferQueryService _transferQueries;
    private readonly IOrganizationBusinessNotificationPublisher _notifications;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public DispatchStockRequest(
        IStockRequestRepository requests,
        IInventoryTransferRepository transfers,
        IInventoryTransferDamageCustodyRepository damageCustodies,
        CreateInventoryTransfer createTransfer,
        DispatchInventoryTransfer dispatchTransfer,
        InventoryTransferQueryService transferQueries,
        IOrganizationBusinessNotificationPublisher notifications,
        IPosUnitOfWork unitOfWork,
        IClock clock)
    {
        _requests = requests;
        _transfers = transfers;
        _damageCustodies = damageCustodies;
        _createTransfer = createTransfer;
        _dispatchTransfer = dispatchTransfer;
        _transferQueries = transferQueries;
        _notifications = notifications;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<InventoryTransferDto>> ExecuteAsync(
        Guid organizationId,
        Guid stockRequestId,
        Guid actorId,
        Guid actingBranchId,
        CancellationToken cancellationToken = default)
    {
        var orgId = PosOrganizationId.From(organizationId);
        var stockRequest = await _requests
            .GetByIdAsync(orgId, StockRequestId.From(stockRequestId), cancellationToken)
            .ConfigureAwait(false);
        if (stockRequest is null)
        {
            return ApplicationResult<InventoryTransferDto>.Failure(
                "pos.inventory.stock_request.not_found",
                "Stock request was not found.");
        }

        if (actingBranchId != stockRequest.RequestedSourceLocationId.Value)
        {
            return ApplicationResult<InventoryTransferDto>.Failure(
                ApplicationErrorCodes.InventoryTransferBranchForbidden,
                "Only the requested source warehouse can dispatch this stock request.");
        }

        try
        {
            return await _unitOfWork.ExecuteInSerializableTransactionAsync(
                ct => DispatchCoreAsync(organizationId, orgId, stockRequestId, actorId, actingBranchId, ct),
                cancellationToken).ConfigureAwait(false);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<InventoryTransferDto>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<InventoryTransferDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }

    private async Task<ApplicationResult<InventoryTransferDto>> DispatchCoreAsync(
        Guid organizationId,
        PosOrganizationId orgId,
        Guid stockRequestId,
        Guid actorId,
        Guid actingBranchId,
        CancellationToken cancellationToken)
    {
            // Reload inside the serializable transaction so RemainingToDispatch cannot race.
            var stockRequest = await _requests
                .GetByIdAsync(orgId, StockRequestId.From(stockRequestId), cancellationToken)
                .ConfigureAwait(false);
            if (stockRequest is null)
            {
                return ApplicationResult<InventoryTransferDto>.Failure(
                    "pos.inventory.stock_request.not_found",
                    "Stock request was not found.");
            }

            if (stockRequest.Status == StockRequestStatus.Approved)
            {
                stockRequest.StartPreparing(actorId, _clock.UtcNow);
                await _requests.UpdateAsync(stockRequest, cancellationToken).ConfigureAwait(false);
                await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }

            if (stockRequest.Status is not (
                StockRequestStatus.Approved
                or StockRequestStatus.Preparing
                or StockRequestStatus.InTransit
                or StockRequestStatus.PartiallyFulfilled))
            {
                return ApplicationResult<InventoryTransferDto>.Failure(
                    DomainErrorCodes.InvalidStockRequestStatusTransition,
                    "Stock request is not open for dispatch.");
            }

            var linkedTransfers = (await _transfers
                    .ListByStockRequestIdAsync(orgId, stockRequest.Id, cancellationToken)
                    .ConfigureAwait(false))
                .Where(t => t.Status != InventoryTransferStatus.Cancelled)
                .OrderByDescending(t => t.UpdatedAtUtc)
                .ToList();

            // Prefer an existing draft so prepare/dispatch stays idempotent (draft has no stock effect,
            // so it does not reduce RemainingToDispatch — we finish this draft instead of creating another).
            InventoryTransfer? transfer = linkedTransfers
                .FirstOrDefault(t => t.Status == InventoryTransferStatus.Draft);

            var createdNewTransfer = false;
            if (transfer is null)
            {
                var custodies = await _damageCustodies
                    .ListByStockRequestIdAsync(orgId, stockRequest.Id, cancellationToken)
                    .ConfigureAwait(false);
                var remainingLines = StockRequestDispatchCoverage
                    .BuildRemainingDispatchLines(stockRequest, linkedTransfers, custodies);
                if (remainingLines.Count == 0)
                {
                    var openCovering = linkedTransfers.FirstOrDefault(t =>
                        t.Status is InventoryTransferStatus.InTransit or InventoryTransferStatus.PartiallyReceived);
                    var anyReceived = linkedTransfers.SelectMany(t => t.Lines).Any(l => l.ReceivedQty > 0m);

                    // Idempotent re-dispatch before any receipt: return the existing open transfer.
                    if (openCovering is not null
                        && stockRequest.Status == StockRequestStatus.InTransit
                        && !anyReceived)
                    {
                        transfer = openCovering;
                    }
                    else if (openCovering is not null)
                    {
                        var openQty = openCovering.Lines.Sum(l => Math.Max(0m, l.OutstandingQty));
                        var transferLabel = openCovering.TransferNumber ?? openCovering.Id.Value.ToString("D");
                        return ApplicationResult<InventoryTransferDto>.Failure(
                            DomainErrorCodes.StockRequestNoRemainingToDispatch,
                            $"No remaining stock is available to dispatch. Outstanding quantity is already covered by an open transfer ({transferLabel}; {openQty} still in transit). Receive or close that transfer before sending replacement stock.");
                    }
                    else
                    {
                        return ApplicationResult<InventoryTransferDto>.Failure(
                            DomainErrorCodes.StockRequestNoRemainingToDispatch,
                            "No remaining stock is available to dispatch. Outstanding quantity is already covered by an open transfer.");
                    }
                }
                else
                {
                    var root = linkedTransfers
                        .Where(t => t.RootTransferId is null)
                        .OrderBy(t => t.CreatedAtUtc)
                        .FirstOrDefault();
                    var rootId = root?.Id ?? linkedTransfers
                        .Where(t => t.RootTransferId is not null)
                        .Select(t => t.RootTransferId!)
                        .FirstOrDefault();
                    var nextSequence = rootId is null
                        ? (int?)null
                        : linkedTransfers
                            .Where(t => t.RootTransferId == rootId || t.Id == rootId)
                            .Select(t => t.ReplacementSequence ?? 0)
                            .DefaultIfEmpty(0)
                            .Max() + 1;
                    var damagePolicy = root is null
                        ? null
                        : InventoryTransferDamageHandlingPolicies.ToCode(root.DamageHandlingPolicy);

                    var createRequest = new CreateInventoryTransferRequest(
                        stockRequest.RequestedSourceLocationId.Value,
                        stockRequest.DestinationLocationId.Value,
                        remainingLines,
                        stockRequest.Notes,
                        stockRequest.Id.Value,
                        RootTransferId: rootId?.Value,
                        ReplacementSequence: nextSequence,
                        ReplacementReason: nextSequence is null ? null : "Replacement for remaining / discrepancy fulfillment",
                        DamageHandlingPolicy: damagePolicy);
                    var created = await _createTransfer
                        .ExecuteAsync(organizationId, createRequest, actorId, actingBranchId, cancellationToken)
                        .ConfigureAwait(false);
                    if (!created.IsSuccess)
                    {
                        return ApplicationResult<InventoryTransferDto>.Failure(created.ErrorCode!, created.ErrorMessage!);
                    }

                    transfer = created.Value!;
                    createdNewTransfer = true;
                    linkedTransfers.Insert(0, transfer);
                }
            }

            if (transfer.Status == InventoryTransferStatus.Draft)
            {
                var dispatched = await _dispatchTransfer
                    .ExecuteAsync(organizationId, transfer.Id.Value, actorId, actingBranchId, cancellationToken)
                    .ConfigureAwait(false);
                if (!dispatched.IsSuccess)
                {
                    return ApplicationResult<InventoryTransferDto>.Failure(dispatched.ErrorCode!, dispatched.ErrorMessage!);
                }

                transfer = dispatched.Value!;
            }
            else if (transfer.Status is not (
                InventoryTransferStatus.InTransit
                or InventoryTransferStatus.Received
                or InventoryTransferStatus.PartiallyReceived
                or InventoryTransferStatus.ClosedWithDiscrepancy))
            {
                return ApplicationResult<InventoryTransferDto>.Failure(
                    DomainErrorCodes.InvalidStockRequestStatusTransition,
                    $"Linked transfer status '{InventoryTransferStatuses.ToCode(transfer.Status)}' cannot fulfill dispatch.");
            }

            // LinkedInventoryTransferId remains the first dispatched transfer for backward compatibility.
            var markedDispatchedNow = false;
            if (stockRequest.Status is StockRequestStatus.Approved or StockRequestStatus.Preparing)
            {
                stockRequest.MarkDispatched(actorId, _clock.UtcNow, transfer.Id.Value);
                await _requests.UpdateAsync(stockRequest, cancellationToken).ConfigureAwait(false);
                await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                markedDispatchedNow = true;
            }

            if (createdNewTransfer || markedDispatchedNow)
            {
                await StockRequestNotificationHelper
                    .PublishAsync(
                        _notifications,
                        organizationId,
                        StockRequestNotificationTypes.Dispatched,
                        stockRequest,
                        stockRequest.DestinationLocationId.Value,
                        "Stock request dispatched",
                        $"{stockRequest.RequestNumber ?? stockRequest.Id.Value.ToString("D")} is in transit.",
                        cancellationToken)
                    .ConfigureAwait(false);
            }

            var dto = await _transferQueries
                .GetByIdAsync(organizationId, transfer.Id.Value, cancellationToken)
                .ConfigureAwait(false);
            return dto is null
                ? ApplicationResult<InventoryTransferDto>.Failure(
                    ApplicationErrorCodes.InventoryTransferNotFound,
                    "Inventory transfer was not found.")
                : ApplicationResult<InventoryTransferDto>.Success(dto);
    }
}

/// <summary>
/// Authoritative stock-request dispatch coverage (used by query + prepare/dispatch).
/// SatisfiedGood = SUM(GoodReceived) across non-cancelled linked transfers.
/// OpenInTransit = outstanding on InTransit / PartiallyReceived members, excluding historical
/// ExpectedLater open (those count toward RemainingToDispatch / Needs fulfillment).
/// Waived = accepted shortage / accepted damage / accepted other on transfer lines.
/// RemainingToDispatch = MAX(0, FulfillmentTarget − SatisfiedGood − OpenInTransit − Waived).
/// Damaged physical inventory, destination hold, and source recovery never satisfy destination demand.
/// </summary>
internal static class StockRequestDispatchCoverage
{
    internal sealed record Snapshot(
        IReadOnlyDictionary<Guid, decimal> ReceivedByProduct,
        IReadOnlyDictionary<Guid, decimal> OpenInTransitByProduct,
        IReadOnlyDictionary<Guid, decimal> WaivedByProduct,
        IReadOnlyDictionary<Guid, decimal> RemainingToDispatchByProduct);

    internal static Snapshot Compute(
        StockRequest stockRequest,
        IReadOnlyList<InventoryTransfer> linkedTransfers,
        IReadOnlyList<InventoryTransferDamageCustody>? damageCustodies = null)
    {
        // damageCustodies retained for call-site compatibility; they never adjust Remaining.
        _ = damageCustodies;

        var active = linkedTransfers.Where(t => t.Status != InventoryTransferStatus.Cancelled).ToList();

        var receivedByProduct = active
            .SelectMany(t => t.Lines)
            .GroupBy(l => l.ProductId.Value)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.ReceivedQty));

        var openInTransitByProduct = stockRequest.Lines
            .Select(l => l.ProductId.Value)
            .Distinct()
            .ToDictionary(
                productId => productId,
                productId => InventoryTransferCoverageMath.OpenInTransitQtyForProduct(active, productId));

        var waivedByProduct = active
            .SelectMany(t => t.Lines)
            .GroupBy(l => l.ProductId.Value)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.WaivedQty));

        var remainingByProduct = new Dictionary<Guid, decimal>();
        foreach (var line in stockRequest.Lines)
        {
            var productId = line.ProductId.Value;
            var remaining = Math.Max(
                0m,
                line.FulfillmentTargetQuantity
                - receivedByProduct.GetValueOrDefault(productId)
                - openInTransitByProduct.GetValueOrDefault(productId)
                - waivedByProduct.GetValueOrDefault(productId));
            remainingByProduct[productId] = remaining;
        }

        return new Snapshot(
            receivedByProduct,
            openInTransitByProduct,
            waivedByProduct,
            remainingByProduct);
    }

    internal static List<InventoryTransferLineRequest> BuildRemainingDispatchLines(
        StockRequest stockRequest,
        IReadOnlyList<InventoryTransfer> linkedTransfers,
        IReadOnlyList<InventoryTransferDamageCustody>? damageCustodies = null)
    {
        var coverage = Compute(stockRequest, linkedTransfers, damageCustodies);
        var lines = new List<InventoryTransferLineRequest>();
        foreach (var line in stockRequest.Lines)
        {
            var remaining = coverage.RemainingToDispatchByProduct.GetValueOrDefault(line.ProductId.Value);
            if (remaining > 0m)
            {
                lines.Add(new InventoryTransferLineRequest(line.ProductId.Value, remaining));
            }
        }

        return lines;
    }
}

public sealed class RejectStockRequest
{
    private readonly IStockRequestRepository _requests;
    private readonly StockRequestQueryService _queries;
    private readonly IOrganizationBusinessNotificationPublisher _notifications;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public RejectStockRequest(
        IStockRequestRepository requests,
        StockRequestQueryService queries,
        IOrganizationBusinessNotificationPublisher notifications,
        IPosUnitOfWork unitOfWork,
        IClock clock)
    {
        _requests = requests;
        _queries = queries;
        _notifications = notifications;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<StockRequestDto>> ExecuteAsync(
        Guid organizationId,
        Guid stockRequestId,
        RejectStockRequestRequest body,
        Guid actorId,
        Guid actingBranchId,
        CancellationToken cancellationToken = default)
    {
        var request = await _requests
            .GetByIdAsync(PosOrganizationId.From(organizationId), StockRequestId.From(stockRequestId), cancellationToken)
            .ConfigureAwait(false);
        if (request is null)
        {
            return ApplicationResult<StockRequestDto>.Failure(
                "pos.inventory.stock_request.not_found",
                "Stock request was not found.");
        }

        if (actingBranchId != request.RequestedSourceLocationId.Value)
        {
            return ApplicationResult<StockRequestDto>.Failure(
                ApplicationErrorCodes.InventoryTransferBranchForbidden,
                "Only the requested source branch can reject this stock request.");
        }

        try
        {
            request.Reject(actorId, _clock.UtcNow, body.Reason);
            await _requests.UpdateAsync(request, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            await StockRequestNotificationHelper
                .PublishAsync(
                    _notifications,
                    organizationId,
                    StockRequestNotificationTypes.Declined,
                    request,
                    request.DestinationLocationId.Value,
                    "Stock request declined",
                    $"{request.RequestNumber ?? request.Id.Value.ToString("D")}: {request.RejectionReason}",
                    cancellationToken)
                .ConfigureAwait(false);

            var dto = await _queries.GetByIdAsync(organizationId, request.Id.Value, cancellationToken).ConfigureAwait(false);
            return dto is null
                ? ApplicationResult<StockRequestDto>.Failure("pos.inventory.stock_request.not_found", "Stock request was not found.")
                : ApplicationResult<StockRequestDto>.Success(dto);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<StockRequestDto>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<StockRequestDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class CancelStockRequest
{
    private readonly IStockRequestRepository _requests;
    private readonly StockRequestQueryService _queries;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public CancelStockRequest(
        IStockRequestRepository requests,
        StockRequestQueryService queries,
        IPosUnitOfWork unitOfWork,
        IClock clock)
    {
        _requests = requests;
        _queries = queries;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<StockRequestDto>> ExecuteAsync(
        Guid organizationId,
        Guid stockRequestId,
        Guid actorId,
        Guid actingBranchId,
        CancellationToken cancellationToken = default)
    {
        var request = await _requests
            .GetByIdAsync(PosOrganizationId.From(organizationId), StockRequestId.From(stockRequestId), cancellationToken)
            .ConfigureAwait(false);
        if (request is null)
        {
            return ApplicationResult<StockRequestDto>.Failure(
                "pos.inventory.stock_request.not_found",
                "Stock request was not found.");
        }

        if (actingBranchId != request.DestinationLocationId.Value)
        {
            return ApplicationResult<StockRequestDto>.Failure(
                ApplicationErrorCodes.InventoryTransferBranchForbidden,
                "Only the destination branch can cancel this stock request.");
        }

        try
        {
            request.Cancel(actorId, _clock.UtcNow);
            await _requests.UpdateAsync(request, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            var dto = await _queries.GetByIdAsync(organizationId, request.Id.Value, cancellationToken).ConfigureAwait(false);
            return dto is null
                ? ApplicationResult<StockRequestDto>.Failure("pos.inventory.stock_request.not_found", "Stock request was not found.")
                : ApplicationResult<StockRequestDto>.Success(dto);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<StockRequestDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class FulfillStockRequestViaTransfer
{
    private readonly DispatchStockRequest _dispatch;

    public FulfillStockRequestViaTransfer(DispatchStockRequest dispatch) => _dispatch = dispatch;

    /// <summary>
    /// Legacy endpoint: delegates to <see cref="DispatchStockRequest"/> (create + dispatch using approved quantities).
    /// Custom line quantities on the request body are ignored.
    /// </summary>
    public Task<ApplicationResult<InventoryTransferDto>> ExecuteAsync(
        Guid organizationId,
        Guid stockRequestId,
        FulfillStockRequestViaTransferRequest request,
        Guid actorId,
        Guid actingBranchId,
        InventoryTransferQueryService transferQueries,
        CancellationToken cancellationToken = default) =>
        _dispatch.ExecuteAsync(organizationId, stockRequestId, actorId, actingBranchId, cancellationToken);
}

public static class StockRequestActivityEventTypes
{
    public const string Requested = "Requested";
    public const string Approved = "Approved";
    public const string PreparingStarted = "PreparingStarted";
    public const string TransferPrepared = "TransferPrepared";
    public const string TransferDispatched = "TransferDispatched";
    public const string TransferReceipt = "TransferReceipt";
    public const string TransferCompleted = "TransferCompleted";
    public const string TransferRemainderClosed = "TransferRemainderClosed";
    public const string TransferCancelled = "TransferCancelled";
    public const string RequestFulfilled = "RequestFulfilled";
    public const string RequestCancelled = "RequestCancelled";
    public const string RequestRejected = "RequestRejected";
}

public sealed class GetStockRequestActivity
{
    private readonly IStockRequestRepository _requests;
    private readonly IInventoryTransferRepository _transfers;

    public GetStockRequestActivity(IStockRequestRepository requests, IInventoryTransferRepository transfers)
    {
        _requests = requests;
        _transfers = transfers;
    }

    public async Task<ApplicationResult<IReadOnlyList<StockRequestActivityEventDto>>> ExecuteAsync(
        Guid organizationId,
        Guid stockRequestId,
        CancellationToken cancellationToken = default)
    {
        var orgId = PosOrganizationId.From(organizationId);
        var request = await _requests
            .GetByIdAsync(orgId, StockRequestId.From(stockRequestId), cancellationToken)
            .ConfigureAwait(false);
        if (request is null)
        {
            return ApplicationResult<IReadOnlyList<StockRequestActivityEventDto>>.Failure(
                "pos.inventory.stock_request.not_found",
                "Stock request was not found.");
        }

        var linkedTransfers = await _transfers
            .ListByStockRequestIdAsync(orgId, request.Id, cancellationToken)
            .ConfigureAwait(false);

        var events = StockRequestActivityBuilder.Build(request, linkedTransfers);
        return ApplicationResult<IReadOnlyList<StockRequestActivityEventDto>>.Success(events);
    }
}

internal static class StockRequestActivityBuilder
{
    private static readonly IReadOnlyDictionary<string, int> EventTypeOrder =
        new Dictionary<string, int>(StringComparer.Ordinal)
        {
            [StockRequestActivityEventTypes.Requested] = 0,
            [StockRequestActivityEventTypes.Approved] = 1,
            [StockRequestActivityEventTypes.PreparingStarted] = 2,
            [StockRequestActivityEventTypes.TransferPrepared] = 3,
            [StockRequestActivityEventTypes.TransferDispatched] = 4,
            [StockRequestActivityEventTypes.TransferReceipt] = 5,
            [StockRequestActivityEventTypes.TransferCompleted] = 6,
            [StockRequestActivityEventTypes.TransferRemainderClosed] = 7,
            [StockRequestActivityEventTypes.TransferCancelled] = 8,
            [StockRequestActivityEventTypes.RequestFulfilled] = 9,
            [StockRequestActivityEventTypes.RequestCancelled] = 10,
            [StockRequestActivityEventTypes.RequestRejected] = 11,
        };

    internal static IReadOnlyList<StockRequestActivityEventDto> Build(
        StockRequest request,
        IReadOnlyList<InventoryTransfer> linkedTransfers)
    {
        var events = new List<StockRequestActivityEventDto>();

        events.Add(new(
            EventId(request.Id.Value, StockRequestActivityEventTypes.Requested, request.CreatedAtUtc),
            StockRequestActivityEventTypes.Requested,
            request.CreatedAtUtc,
            request.RequestedBy,
            null,
            null,
            null,
            null,
            null,
            null,
            null));

        if (request.ApprovedAtUtc is DateTimeOffset approvedAt)
        {
            events.Add(new(
                EventId(request.Id.Value, StockRequestActivityEventTypes.Approved, approvedAt),
                StockRequestActivityEventTypes.Approved,
                approvedAt,
                request.ApprovedBy,
                null,
                null,
                null,
                null,
                null,
                null,
                null));
        }

        if (request.PreparingStartedAtUtc is DateTimeOffset preparingAt)
        {
            events.Add(new(
                EventId(request.Id.Value, StockRequestActivityEventTypes.PreparingStarted, preparingAt),
                StockRequestActivityEventTypes.PreparingStarted,
                preparingAt,
                request.PreparingStartedBy,
                null,
                null,
                null,
                null,
                null,
                null,
                null));
        }

        foreach (var transfer in linkedTransfers.OrderBy(t => t.CreatedAtUtc))
        {
            var transferId = transfer.Id.Value;
            events.Add(new(
                EventId(transferId, StockRequestActivityEventTypes.TransferPrepared, transfer.CreatedAtUtc),
                StockRequestActivityEventTypes.TransferPrepared,
                transfer.CreatedAtUtc,
                transfer.CreatedBy,
                transferId,
                transfer.TransferNumber,
                null,
                null,
                transfer.TotalSentQty,
                null,
                null));

            if (transfer.DispatchedAtUtc is DateTimeOffset dispatchedAt)
            {
                events.Add(new(
                    EventId(transferId, StockRequestActivityEventTypes.TransferDispatched, dispatchedAt),
                    StockRequestActivityEventTypes.TransferDispatched,
                    dispatchedAt,
                    transfer.DispatchedBy,
                    transferId,
                    transfer.TransferNumber,
                    null,
                    null,
                    transfer.TotalSentQty,
                    null,
                    null));
            }

            foreach (var receipt in transfer.Receipts.OrderBy(r => r.Sequence))
            {
                var qty = receipt.Lines.Sum(l => l.QuantityReceived);
                events.Add(new(
                    EventId(receipt.Id.Value, StockRequestActivityEventTypes.TransferReceipt, receipt.ReceivedAtUtc),
                    StockRequestActivityEventTypes.TransferReceipt,
                    receipt.ReceivedAtUtc,
                    receipt.ReceivedBy,
                    transferId,
                    transfer.TransferNumber,
                    receipt.Id.Value,
                    receipt.Sequence,
                    qty,
                    null,
                    null));
            }

            if (transfer.Status == InventoryTransferStatus.Received && transfer.ReceivedAtUtc is DateTimeOffset receivedAt)
            {
                events.Add(new(
                    EventId(transferId, StockRequestActivityEventTypes.TransferCompleted, receivedAt),
                    StockRequestActivityEventTypes.TransferCompleted,
                    receivedAt,
                    transfer.ReceivedBy,
                    transferId,
                    transfer.TransferNumber,
                    null,
                    null,
                    transfer.TotalReceivedQty,
                    null,
                    null));
            }

            if (transfer.ClosedAtUtc is DateTimeOffset closedAt)
            {
                var reason = transfer.Lines
                    .Where(l => l.DiscrepancyReason is not null)
                    .Select(l => InventoryTransferDiscrepancyReasons.ToCode(l.DiscrepancyReason!.Value))
                    .FirstOrDefault();
                var note = transfer.Lines.Select(l => l.DiscrepancyNote).FirstOrDefault(n => !string.IsNullOrWhiteSpace(n));
                events.Add(new(
                    EventId(transferId, StockRequestActivityEventTypes.TransferRemainderClosed, closedAt),
                    StockRequestActivityEventTypes.TransferRemainderClosed,
                    closedAt,
                    transfer.ClosedBy,
                    transferId,
                    transfer.TransferNumber,
                    null,
                    null,
                    transfer.TotalClosedQty,
                    reason,
                    note));
            }

            if (transfer.CancelledAtUtc is DateTimeOffset cancelledAt)
            {
                events.Add(new(
                    EventId(transferId, StockRequestActivityEventTypes.TransferCancelled, cancelledAt),
                    StockRequestActivityEventTypes.TransferCancelled,
                    cancelledAt,
                    transfer.CancelledBy,
                    transferId,
                    transfer.TransferNumber,
                    null,
                    null,
                    null,
                    null,
                    null));
            }
        }

        if (request.Status == StockRequestStatus.Fulfilled)
        {
            events.Add(new(
                EventId(request.Id.Value, StockRequestActivityEventTypes.RequestFulfilled, request.UpdatedAtUtc),
                StockRequestActivityEventTypes.RequestFulfilled,
                request.UpdatedAtUtc,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null));
        }

        if (request.CancelledAtUtc is DateTimeOffset requestCancelledAt)
        {
            events.Add(new(
                EventId(request.Id.Value, StockRequestActivityEventTypes.RequestCancelled, requestCancelledAt),
                StockRequestActivityEventTypes.RequestCancelled,
                requestCancelledAt,
                request.CancelledBy,
                null,
                null,
                null,
                null,
                null,
                null,
                null));
        }

        if (request.RejectedAtUtc is DateTimeOffset rejectedAt)
        {
            events.Add(new(
                EventId(request.Id.Value, StockRequestActivityEventTypes.RequestRejected, rejectedAt),
                StockRequestActivityEventTypes.RequestRejected,
                rejectedAt,
                request.RejectedBy,
                null,
                null,
                null,
                null,
                null,
                request.RejectionReason,
                null));
        }

        return events
            .OrderBy(e => e.OccurredAtUtc)
            .ThenBy(e => EventTypeOrder.GetValueOrDefault(e.EventType, 99))
            .ThenBy(e => e.TransferId)
            .ThenBy(e => e.ReceiptSequence ?? 0)
            .ToList();
    }

    private static Guid EventId(Guid scopeId, string eventType, DateTimeOffset occurredAtUtc)
    {
        var seed = $"{scopeId:D}|{eventType}|{occurredAtUtc.UtcTicks}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(seed));
        var bytes = new byte[16];
        hash.AsSpan(0, 16).CopyTo(bytes);
        return new Guid(bytes);
    }
}

internal static class StockRequestNotificationHelper
{
    public static Task PublishAsync(
        IOrganizationBusinessNotificationPublisher notifications,
        Guid organizationId,
        string relatedType,
        StockRequest request,
        Guid targetBranchId,
        string title,
        string preview,
        CancellationToken cancellationToken) =>
        notifications.PublishAsync(
            organizationId,
            organizationId,
            relatedType,
            request.Id.Value.ToString("D"),
            title,
            preview,
            cancellationToken,
            targetBranchId);
}
