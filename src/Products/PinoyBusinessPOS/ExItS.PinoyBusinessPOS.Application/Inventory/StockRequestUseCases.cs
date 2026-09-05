using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Common;
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
        CancellationToken cancellationToken = default)
    {
        var (skip, take) = PosPagination.Normalize(page, pageSize);
        var (items, total) = await _requests
            .ListByDestinationAsync(
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

public sealed class CreateStockRequest
{
    private readonly IStockRequestRepository _requests;
    private readonly ISupplyRouteRepository _routes;
    private readonly ICatalogProductRepository _products;
    private readonly IOrganizationBranchDirectory _branches;
    private readonly StockRequestQueryService _queries;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public CreateStockRequest(
        IStockRequestRepository requests,
        ISupplyRouteRepository routes,
        ICatalogProductRepository products,
        IOrganizationBranchDirectory branches,
        StockRequestQueryService queries,
        IPosUnitOfWork unitOfWork,
        IClock clock)
    {
        _requests = requests;
        _routes = routes;
        _products = products;
        _branches = branches;
        _queries = queries;
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

public sealed class RejectStockRequest
{
    private readonly IStockRequestRepository _requests;
    private readonly StockRequestQueryService _queries;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public RejectStockRequest(
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
    private readonly IStockRequestRepository _requests;
    private readonly IInventoryTransferRepository _transfers;
    private readonly CreateInventoryTransfer _createTransfer;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public FulfillStockRequestViaTransfer(
        IStockRequestRepository requests,
        IInventoryTransferRepository transfers,
        CreateInventoryTransfer createTransfer,
        IPosUnitOfWork unitOfWork,
        IClock clock)
    {
        _requests = requests;
        _transfers = transfers;
        _createTransfer = createTransfer;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<InventoryTransferDto>> ExecuteAsync(
        Guid organizationId,
        Guid stockRequestId,
        FulfillStockRequestViaTransferRequest request,
        Guid actorId,
        Guid actingBranchId,
        InventoryTransferQueryService transferQueries,
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
                "Only the requested source branch can fulfill this stock request.");
        }

        if (stockRequest.Status is StockRequestStatus.Rejected or StockRequestStatus.Cancelled or StockRequestStatus.Fulfilled)
        {
            return ApplicationResult<InventoryTransferDto>.Failure(
                DomainErrorCodes.InvalidStockRequestStatusTransition,
                "Stock request is not open for fulfillment.");
        }

        if (request.Lines is null || request.Lines.Count == 0)
        {
            return ApplicationResult<InventoryTransferDto>.Failure(
                DomainErrorCodes.StockRequestRequiresLines,
                "At least one fulfillment line is required.");
        }

        var linkedTransfers = await _transfers
            .ListByStockRequestIdAsync(orgId, stockRequest.Id, cancellationToken)
            .ConfigureAwait(false);
        var allocatedByProduct = linkedTransfers
            .Where(t => t.Status != InventoryTransferStatus.Cancelled)
            .SelectMany(t => t.Lines)
            .GroupBy(l => l.ProductId.Value)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.SentQty));

        var requestLines = stockRequest.Lines.ToDictionary(l => l.ProductId.Value);
        var seen = new HashSet<Guid>();
        foreach (var line in request.Lines)
        {
            if (!seen.Add(line.ProductId))
            {
                return ApplicationResult<InventoryTransferDto>.Failure(
                    DomainErrorCodes.StockRequestDuplicateProduct,
                    "Fulfillment lines cannot contain duplicate products.");
            }

            if (line.Quantity <= 0m)
            {
                return ApplicationResult<InventoryTransferDto>.Failure(
                    DomainErrorCodes.InvalidStockRequestQuantity,
                    "Fulfillment quantity must be greater than zero.");
            }

            if (!requestLines.TryGetValue(line.ProductId, out var stockLine))
            {
                return ApplicationResult<InventoryTransferDto>.Failure(
                    DomainErrorCodes.InvalidStockRequestLine,
                    "Fulfillment product is not part of the stock request.");
            }

            var allocated = allocatedByProduct.GetValueOrDefault(line.ProductId);
            var remaining = stockLine.RequestedQuantity - allocated;
            if (line.Quantity > remaining)
            {
                return ApplicationResult<InventoryTransferDto>.Failure(
                    DomainErrorCodes.InvalidStockRequestQuantity,
                    $"Requested transfer quantity exceeds remaining requested quantity for '{stockLine.NameSnapshot}'.");
            }
        }

        var transferRequest = new CreateInventoryTransferRequest(
            stockRequest.RequestedSourceLocationId.Value,
            stockRequest.DestinationLocationId.Value,
            request.Lines.Select(line => new InventoryTransferLineRequest(line.ProductId, line.Quantity, line.SourceLotId)).ToList(),
            request.Notes,
            stockRequest.Id.Value);
        var created = await _createTransfer
            .ExecuteAsync(organizationId, transferRequest, actorId, actingBranchId, cancellationToken)
            .ConfigureAwait(false);
        if (!created.IsSuccess)
        {
            return ApplicationResult<InventoryTransferDto>.Failure(created.ErrorCode!, created.ErrorMessage!);
        }

        var utcNow = _clock.UtcNow;
        stockRequest.MarkInProgress(utcNow);
        await _requests.UpdateAsync(stockRequest, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var dto = await transferQueries.GetByIdAsync(organizationId, created.Value!.Id.Value, cancellationToken).ConfigureAwait(false);
        return dto is null
            ? ApplicationResult<InventoryTransferDto>.Failure(ApplicationErrorCodes.InventoryTransferNotFound, "Inventory transfer was not found.")
            : ApplicationResult<InventoryTransferDto>.Success(dto);
    }
}
