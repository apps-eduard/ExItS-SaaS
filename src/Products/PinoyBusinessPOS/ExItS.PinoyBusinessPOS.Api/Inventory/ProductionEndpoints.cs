using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Application.Offline;

namespace ExItS.PinoyBusinessPOS.Api.Inventory;

internal static class ProductionEndpoints
{
    public static IEndpointRouteBuilder MapProductionEndpoints(this IEndpointRouteBuilder app)
    {
        var definitions = app.MapGroup("/api/v1/pos/inventory/production/definitions");
        var runs = app.MapGroup("/api/v1/pos/inventory/production/runs");

        definitions.MapGet("/", async (
            HttpRequest request,
            string? search,
            Guid? outputProductId,
            bool? isActive,
            int? page,
            int? pageSize,
            ProductionDefinitionQueryService queries,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ViewInventory, out var organizationId, out var problem))
            {
                return problem!;
            }

            var filter = new ProductionDefinitionFilter(search, outputProductId, isActive);
            var result = await queries.ListAsync(organizationId, filter, page, pageSize, ct).ConfigureAwait(false);
            return Results.Ok(result);
        });

        definitions.MapGet("/{definitionId:guid}", async (
            HttpRequest request,
            Guid definitionId,
            ProductionDefinitionQueryService queries,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ViewInventory, out var organizationId, out var problem))
            {
                return problem!;
            }

            var definition = await queries.GetByIdAsync(organizationId, definitionId, ct).ConfigureAwait(false);
            return definition is null
                ? PosApiResults.Problem(
                    ApplicationErrorCodes.ProductionDefinitionNotFound,
                    "Production definition was not found.",
                    StatusCodes.Status404NotFound)
                : Results.Ok(definition);
        });

        definitions.MapPost("/", async (
            HttpRequest request,
            CreateProductionDefinitionRequest body,
            CreateProductionDefinition useCase,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ManageInventory, out var organizationId, out var problem)
                || !PosOrganizationScope.TryGetActorId(request, out var actorId, out problem))
            {
                return problem!;
            }

            var result = await useCase.ExecuteAsync(organizationId, body, actorId, ct).ConfigureAwait(false);
            return PosApiResults.FromResult(
                result,
                dto => Results.Created($"/api/v1/pos/inventory/production/definitions/{dto.ProductionDefinitionId:D}", dto));
        });

        definitions.MapPut("/{definitionId:guid}", async (
            HttpRequest request,
            Guid definitionId,
            UpdateProductionDefinitionRequest body,
            UpdateProductionDefinition useCase,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ManageInventory, out var organizationId, out var problem)
                || !PosOrganizationScope.TryGetActorId(request, out var actorId, out problem))
            {
                return problem!;
            }

            var result = await useCase.ExecuteAsync(organizationId, definitionId, body, actorId, ct).ConfigureAwait(false);
            return PosApiResults.FromResult(result, Results.Ok);
        });

        definitions.MapPost("/{definitionId:guid}/set-active", async (
            HttpRequest request,
            Guid definitionId,
            SetProductionDefinitionActiveRequest body,
            SetProductionDefinitionActive useCase,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ManageInventory, out var organizationId, out var problem)
                || !PosOrganizationScope.TryGetActorId(request, out var actorId, out problem))
            {
                return problem!;
            }

            var result = await useCase
                .ExecuteAsync(organizationId, definitionId, body.IsActive, actorId, ct)
                .ConfigureAwait(false);
            return PosApiResults.FromResult(result, Results.Ok);
        });

        runs.MapGet("/", async (
            HttpRequest request,
            DateTimeOffset? fromProducedAtUtc,
            DateTimeOffset? toProducedAtUtc,
            string? status,
            Guid? branchId,
            Guid? outputProductId,
            Guid? productionDefinitionId,
            string? referenceNumber,
            int? page,
            int? pageSize,
            ProductionRunQueryService queries,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ViewInventory, out var organizationId, out var problem))
            {
                return problem!;
            }

            var filter = new ProductionRunFilter(
                fromProducedAtUtc,
                toProducedAtUtc,
                status,
                branchId,
                outputProductId,
                productionDefinitionId,
                referenceNumber);
            var result = await queries.ListAsync(organizationId, filter, page, pageSize, ct).ConfigureAwait(false);
            return Results.Ok(result);
        });

        runs.MapGet("/{productionRunId:guid}", async (
            HttpRequest request,
            Guid productionRunId,
            ProductionRunQueryService queries,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ViewInventory, out var organizationId, out var problem))
            {
                return problem!;
            }

            var run = await queries.GetByIdAsync(organizationId, productionRunId, ct).ConfigureAwait(false);
            return run is null
                ? PosApiResults.Problem(
                    ApplicationErrorCodes.ProductionRunNotFound,
                    "Production run was not found.",
                    StatusCodes.Status404NotFound)
                : Results.Ok(run);
        });

        runs.MapPost("/", async (
            HttpRequest request,
            CreateProductionRunRequest body,
            CreateProductionRun useCase,
            IPosIdempotencyService idempotency,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ManageInventory, out var organizationId, out var problem)
                || !PosOrganizationScope.TryGetActorId(request, out var actorId, out problem))
            {
                return problem!;
            }

            var branchResolved = InventoryBranchBodyResolver.ResolveMutationBranch(request, body.BranchId);
            if (!branchResolved.Success)
            {
                return branchResolved.Problem!;
            }

            if (branchResolved.BranchId is Guid branchId)
            {
                body = body with { BranchId = branchId };
            }

            return await PosIdempotencyEndpointHelper.ExecuteMutationAsync(
                    request,
                    organizationId,
                    OfflineOperationTypes.ProductionRun,
                    idempotency,
                    ct2 => useCase.ExecuteAsync(organizationId, body, actorId, ct2),
                    dto => dto,
                    dto => Results.Created($"/api/v1/pos/inventory/production/runs/{dto.ProductionRunId:D}", dto),
                    ct)
                .ConfigureAwait(false);
        });

        runs.MapPost("/{productionRunId:guid}/void", async (
            HttpRequest request,
            Guid productionRunId,
            VoidProductionRun useCase,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ManageInventory, out var organizationId, out var problem)
                || !PosOrganizationScope.TryGetActorId(request, out var actorId, out problem))
            {
                return problem!;
            }

            var result = await useCase.ExecuteAsync(organizationId, productionRunId, actorId, ct).ConfigureAwait(false);
            return PosApiResults.FromResult(result, Results.Ok);
        });

        return app;
    }

    private static bool TryAuthorize(
        HttpRequest request,
        IPosCommercialAccessAccessor access,
        UtangCapability capability,
        out Guid organizationId,
        out IResult? problem)
    {
        if (!PosOrganizationScope.TryGetOrganizationId(request, out organizationId, out problem))
        {
            return false;
        }

        return PosCommercialScope.TryAuthorize(access, capability, out problem);
    }
}
