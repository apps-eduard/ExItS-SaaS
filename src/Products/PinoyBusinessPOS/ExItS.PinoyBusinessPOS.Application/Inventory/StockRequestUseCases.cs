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
        var activeTransfers = linkedTransfers.Where(t => t.Status != InventoryTransferStatus.Cancelled).ToList();
        var fulfilledByProduct = activeTransfers
            .SelectMany(t => t.Lines)
            .GroupBy(l => l.ProductId.Value)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.ReceivedQty));
        var inProgressByProduct = activeTransfers
            .SelectMany(t => t.Lines)
            .GroupBy(l => l.ProductId.Value)
            .ToDictionary(g => g.Key, g => g.Sum(x => Math.Max(0m, x.SentQty - x.ReceivedQty)));

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
            request.Lines.Select(line => new StockRequestLineDto(
                line.Id.Value,
                line.ProductId.Value,
                line.LineNumber,
                line.RequestedQuantity,
                line.ApprovedQuantity,
                fulfilledByProduct.GetValueOrDefault(line.ProductId.Value),
                inProgressByProduct.GetValueOrDefault(line.ProductId.Value),
                line.NameSnapshot,
                UnitOfMeasures.ToCode(line.UnitOfMeasure))).ToList(),
            linkedTransfers
                .OrderByDescending(t => t.UpdatedAtUtc)
                .Select(t => new StockRequestLinkedTransferDto(
                    t.Id.Value,
                    t.TransferNumber,
                    InventoryTransferStatuses.ToCode(t.Status),
                    t.TotalSentQty,
                    t.TotalReceivedQty,
                    t.UpdatedAtUtc))
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

public sealed class DispatchStockRequest
{
    private readonly IStockRequestRepository _requests;
    private readonly IInventoryTransferRepository _transfers;
    private readonly CreateInventoryTransfer _createTransfer;
    private readonly DispatchInventoryTransfer _dispatchTransfer;
    private readonly InventoryTransferQueryService _transferQueries;
    private readonly IOrganizationBusinessNotificationPublisher _notifications;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public DispatchStockRequest(
        IStockRequestRepository requests,
        IInventoryTransferRepository transfers,
        CreateInventoryTransfer createTransfer,
        DispatchInventoryTransfer dispatchTransfer,
        InventoryTransferQueryService transferQueries,
        IOrganizationBusinessNotificationPublisher notifications,
        IPosUnitOfWork unitOfWork,
        IClock clock)
    {
        _requests = requests;
        _transfers = transfers;
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
            if (stockRequest.Status == StockRequestStatus.Approved)
            {
                stockRequest.StartPreparing(actorId, _clock.UtcNow);
                await _requests.UpdateAsync(stockRequest, cancellationToken).ConfigureAwait(false);
                await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }

            if (stockRequest.Status is not (StockRequestStatus.Approved or StockRequestStatus.Preparing or StockRequestStatus.InTransit))
            {
                return ApplicationResult<InventoryTransferDto>.Failure(
                    DomainErrorCodes.InvalidStockRequestStatusTransition,
                    "Stock request is not open for dispatch.");
            }

            InventoryTransfer? transfer = null;
            if (stockRequest.LinkedInventoryTransferId is Guid linkedId)
            {
                transfer = await _transfers
                    .GetByIdAsync(orgId, InventoryTransferId.From(linkedId), cancellationToken)
                    .ConfigureAwait(false);
            }

            if (transfer is null)
            {
                var linked = await _transfers
                    .ListByStockRequestIdAsync(orgId, stockRequest.Id, cancellationToken)
                    .ConfigureAwait(false);
                transfer = linked
                    .Where(t => t.Status != InventoryTransferStatus.Cancelled)
                    .OrderByDescending(t => t.UpdatedAtUtc)
                    .FirstOrDefault();
            }

            if (transfer is null)
            {
                if (stockRequest.Status is not (StockRequestStatus.Approved or StockRequestStatus.Preparing))
                {
                    return ApplicationResult<InventoryTransferDto>.Failure(
                        DomainErrorCodes.InvalidStockRequestStatusTransition,
                        "Stock request is not open for dispatch.");
                }

                var lines = stockRequest.Lines
                    .Select(line => new InventoryTransferLineRequest(
                        line.ProductId.Value,
                        line.FulfillmentTargetQuantity))
                    .ToList();
                var createRequest = new CreateInventoryTransferRequest(
                    stockRequest.RequestedSourceLocationId.Value,
                    stockRequest.DestinationLocationId.Value,
                    lines,
                    stockRequest.Notes,
                    stockRequest.Id.Value);
                var created = await _createTransfer
                    .ExecuteAsync(organizationId, createRequest, actorId, actingBranchId, cancellationToken)
                    .ConfigureAwait(false);
                if (!created.IsSuccess)
                {
                    return ApplicationResult<InventoryTransferDto>.Failure(created.ErrorCode!, created.ErrorMessage!);
                }

                transfer = created.Value!;
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
                or InventoryTransferStatus.PartiallyReceived))
            {
                return ApplicationResult<InventoryTransferDto>.Failure(
                    DomainErrorCodes.InvalidStockRequestStatusTransition,
                    $"Linked transfer status '{InventoryTransferStatuses.ToCode(transfer.Status)}' cannot fulfill dispatch.");
            }

            var wasAlreadyLinked = stockRequest.LinkedInventoryTransferId == transfer.Id.Value
                && stockRequest.Status == StockRequestStatus.InTransit;
            if (stockRequest.Status is StockRequestStatus.Approved or StockRequestStatus.Preparing)
            {
                stockRequest.MarkDispatched(actorId, _clock.UtcNow, transfer.Id.Value);
                await _requests.UpdateAsync(stockRequest, cancellationToken).ConfigureAwait(false);
                await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }

            if (!wasAlreadyLinked)
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
        catch (DomainException ex)
        {
            return ApplicationResult<InventoryTransferDto>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<InventoryTransferDto>.Failure(ex.ErrorCode, ex.Message);
        }
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
