using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;

namespace ExItS.PinoyBusinessPOS.Application.Inventory;

/// <summary>
/// Replenishment supply sources must be Warehouses. Retail→Retail movement uses InventoryTransfer.
/// </summary>
public static class SupplyRouteSourceRules
{
    public const string WarehouseBranchType = "Warehouse";

    public static bool IsWarehouseBranchType(string? branchType) =>
        string.Equals(branchType, WarehouseBranchType, StringComparison.OrdinalIgnoreCase);
}

public sealed class UpsertSupplyCoverageBySource
{
    private readonly ISupplyRouteRepository _routes;
    private readonly IOrganizationBranchDirectory _branches;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public UpsertSupplyCoverageBySource(
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
        UpsertSupplyCoverageBySourceRequest request,
        CancellationToken cancellationToken = default)
    {
        var orgId = PosOrganizationId.From(organizationId);
        var source = PosBranchId.From(request.SourceLocationId);

        if (!await _branches.ExistsInOrganizationAsync(organizationId, source.Value, cancellationToken).ConfigureAwait(false))
        {
            return ApplicationResult<IReadOnlyList<SupplyRouteDto>>.Failure(
                ApplicationErrorCodes.InventoryTransferBranchNotFound,
                "Supply warehouse was not found in this organization.");
        }

        if (!await _branches.IsActiveInOrganizationAsync(organizationId, source.Value, cancellationToken).ConfigureAwait(false))
        {
            return ApplicationResult<IReadOnlyList<SupplyRouteDto>>.Failure(
                DomainErrorCodes.SupplyRouteSourceInactive,
                "Supply warehouse must be an active location.");
        }

        var sourceType = await _branches.GetBranchTypeAsync(organizationId, source.Value, cancellationToken).ConfigureAwait(false);
        if (!SupplyRouteSourceRules.IsWarehouseBranchType(sourceType))
        {
            return ApplicationResult<IReadOnlyList<SupplyRouteDto>>.Failure(
                DomainErrorCodes.SupplyRouteSourceMustBeWarehouse,
                "Only Warehouse locations may be replenishment supply sources.");
        }

        var destinationIds = (request.DestinationLocationIds ?? Array.Empty<Guid>())
            .Distinct()
            .ToArray();
        var preferredSet = new HashSet<Guid>(request.SetPreferredForDestinationIds ?? Array.Empty<Guid>());

        try
        {
            var utcNow = _clock.UtcNow;
            var existingFromSource = await _routes.ListBySourceAsync(orgId, source, cancellationToken).ConfigureAwait(false);
            var byDestination = existingFromSource.ToDictionary(r => r.DestinationLocationId.Value);

            foreach (var destinationId in destinationIds)
            {
                if (destinationId == source.Value)
                {
                    return ApplicationResult<IReadOnlyList<SupplyRouteDto>>.Failure(
                        DomainErrorCodes.SupplyRouteSameLocation,
                        "Supply route source and destination must be different.");
                }

                if (!await _branches.ExistsInOrganizationAsync(organizationId, destinationId, cancellationToken).ConfigureAwait(false))
                {
                    return ApplicationResult<IReadOnlyList<SupplyRouteDto>>.Failure(
                        ApplicationErrorCodes.InventoryTransferBranchNotFound,
                        "Destination location was not found in this organization.");
                }

                if (!await _branches.IsActiveInOrganizationAsync(organizationId, destinationId, cancellationToken).ConfigureAwait(false))
                {
                    return ApplicationResult<IReadOnlyList<SupplyRouteDto>>.Failure(
                        DomainErrorCodes.SupplyRouteDestinationInactive,
                        "Destination location must be active.");
                }

                if (byDestination.TryGetValue(destinationId, out var existing))
                {
                    if (!existing.IsActive)
                    {
                        existing.Activate(utcNow);
                        await _routes.UpdateAsync(existing, cancellationToken).ConfigureAwait(false);
                    }
                }
                else
                {
                    var created = SupplyRoute.Create(
                        orgId,
                        source,
                        PosBranchId.From(destinationId),
                        utcNow,
                        isPreferred: false,
                        isActive: true);
                    await _routes.AddAsync(created, cancellationToken).ConfigureAwait(false);
                }
            }

            foreach (var unused in existingFromSource.Where(r =>
                         r.IsActive && !destinationIds.Contains(r.DestinationLocationId.Value)))
            {
                unused.Deactivate(utcNow);
                await _routes.UpdateAsync(unused, cancellationToken).ConfigureAwait(false);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            // Preferred: never steal an existing preferred from another warehouse.
            // Explicit setPreferredForDestinationIds only when this warehouse is connected.
            // Auto-prefer when destination ends with exactly one active route and none preferred.
            foreach (var destinationId in destinationIds)
            {
                var destRoutes = await _routes
                    .ListByDestinationAsync(orgId, PosBranchId.From(destinationId), cancellationToken)
                    .ConfigureAwait(false);
                var active = destRoutes.Where(r => r.IsActive).ToList();
                var thisRoute = active.FirstOrDefault(r => r.SourceLocationId.Value == source.Value);
                if (thisRoute is null)
                {
                    continue;
                }

                var hasPreferred = active.Any(r => r.IsPreferred);
                var forcePreferred = preferredSet.Contains(destinationId);
                var autoPreferred = !hasPreferred && active.Count == 1;

                if ((forcePreferred || autoPreferred) && !thisRoute.IsPreferred)
                {
                    foreach (var other in active.Where(r => r.IsPreferred && r.Id != thisRoute.Id))
                    {
                        other.SetPreferred(false, utcNow);
                        await _routes.UpdateAsync(other, cancellationToken).ConfigureAwait(false);
                    }

                    thisRoute.SetPreferred(true, utcNow);
                    await _routes.UpdateAsync(thisRoute, cancellationToken).ConfigureAwait(false);
                }
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            var refreshed = await _routes.ListBySourceAsync(orgId, source, cancellationToken).ConfigureAwait(false);
            return ApplicationResult<IReadOnlyList<SupplyRouteDto>>.Success(
                refreshed.Select(SupplyRouteQueryService.Map).ToList());
        }
        catch (DomainException ex)
        {
            return ApplicationResult<IReadOnlyList<SupplyRouteDto>>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

/// <summary>
/// Soft-deactivates active SupplyRoutes whose source is not a Warehouse (V1 leftovers).
/// Non-destructive: historical StockRequest / Transfer rows remain.
/// </summary>
public sealed class DeactivateNonWarehouseSupplySources
{
    private readonly ISupplyRouteRepository _routes;
    private readonly IOrganizationBranchDirectory _branches;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public DeactivateNonWarehouseSupplySources(
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

    public async Task<int> ExecuteAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        var orgId = PosOrganizationId.From(organizationId);
        var all = await _routes.ListAllAsync(orgId, cancellationToken).ConfigureAwait(false);
        var utcNow = _clock.UtcNow;
        var deactivated = 0;
        foreach (var route in all.Where(r => r.IsActive))
        {
            var type = await _branches
                .GetBranchTypeAsync(organizationId, route.SourceLocationId.Value, cancellationToken)
                .ConfigureAwait(false);
            if (SupplyRouteSourceRules.IsWarehouseBranchType(type))
            {
                continue;
            }

            route.Deactivate(utcNow);
            await _routes.UpdateAsync(route, cancellationToken).ConfigureAwait(false);
            deactivated++;
        }

        if (deactivated > 0)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return deactivated;
    }
}
